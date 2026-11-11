using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommons.DalamudServices;

namespace LuliPR.Common;

/// <summary>Synchronizes GitHub PTLs directly into PromeRotation's PureTimelines directory.</summary>
internal static class BundledTimelineSync
{
    private const string GitHubTimelineBaseUrl = "https://raw.githubusercontent.com/Lulimmm/LuliPR/main/Timeline/NewTimeline/";
    private const int MaximumTimelineBytes = 8 * 1024 * 1024;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly object Sync = new();
    private static readonly Dictionary<string, string> TimelineStatus = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> CloudVersions = new(StringComparer.OrdinalIgnoreCase);
    // Only identities are compiled into the ACR. Timeline contents always come from GitHub.
    private static readonly TimelineDefinition[] Definitions =
    [
        new("伊甸近战减伤-02566ee3.json", "02566ee3-2ba5-49cb-8d05-6853db3a6937", "伊甸近战减伤"),
        new("伊甸镰刀-614f92f2.json", "614f92f2-8e9b-4a66-94f8-9f7bdf8111ca", "伊甸镰刀"),
        new("M12S近战减伤-e53d2162.json", "e53d2162-cc9a-43fb-bfc9-d3f5abb5ebac", "M12S近战减伤"),
        new("M12S镰刀-bc0a5751.json", "bc0a5751-8c84-469a-88bd-98c2289dd601", "M12S镰刀"),
        new("妖星镰刀.json", "396a5f80-f1c4-4e75-90ad-7d60e611bf3a", "妖星镰刀"),
        new("绝欧镰刀.json", "2011beac-efee-44a1-8243-eae2f08ed351", "绝欧镰刀")
    ];
    private static TimelineRow[] rows = [];
    private static int syncStarted;
    private static int remoteCheckStarted;
    private static ulong activeContentId;
    private static bool checkedThisLogin;
    private static int pendingCloudCheck;
    private static bool sessionAttached;
    private static CancellationTokenSource? lifetime;
    private static readonly SemaphoreSlim Operations = new(1);
    private static string? targetDirectory;

    internal static void RunOnce()
    {
        if (Interlocked.Exchange(ref syncStarted, 1) != 0) return;

        try
        {
            Directory.CreateDirectory(GetTargetDirectory());
            CleanupLegacyPackage();
            RefreshRows();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli ACR: failed to initialize cloud timeline synchronization");
        }
    }

    internal static void Enter()
    {
        if (!sessionAttached)
        {
            targetDirectory = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "PureTimelines");
            lifetime = new CancellationTokenSource();
            Svc.ClientState.Logout += OnLogout;
            sessionAttached = true;
        }
        QueueFileOperation("*", RunOnce);
        UpdateLoginSession();
    }

    internal static void Exit()
    {
        if (!sessionAttached) return;
        sessionAttached = false;
        Svc.ClientState.Logout -= OnLogout;
        lifetime?.Cancel();
        // A queued request must be retried if a job switch cancelled it.
        if (IsChecking) checkedThisLogin = false;
    }

    internal static void UpdateLoginSession()
    {
        if (!sessionAttached) return;
        var contentId = Svc.ClientState.IsLoggedIn ? Svc.PlayerState.ContentId : 0UL;
        var startCheck = false;
        lock (Sync)
        {
            if (contentId == 0)
            {
                activeContentId = 0;
                checkedThisLogin = false;
                return;
            }

            if (contentId != activeContentId)
            {
                activeContentId = contentId;
                checkedThisLogin = false;
            }

            if (!checkedThisLogin)
            {
                checkedThisLogin = true;
                startCheck = true;
            }
        }

        if (startCheck) RequestCloudCheck();
    }

    private static void OnLogout(int type, int code)
    {
        lock (Sync)
        {
            activeContentId = 0;
            checkedThisLogin = false;
        }
    }

    internal static TimelineRow[] GetRows()
        => Volatile.Read(ref rows);

    internal static bool IsChecking => Volatile.Read(ref remoteCheckStarted) != 0;

    internal static void RequestCloudCheck(string? timelineGuid = null)
    {
        if (Interlocked.Exchange(ref remoteCheckStarted, 1) != 0)
        {
            Interlocked.Exchange(ref pendingCloudCheck, 1);
            return;
        }

        var cancellation = lifetime?.Token ?? CancellationToken.None;
        _ = Task.Run(async () =>
        {
            try
            {
                await Operations.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    cancellation.ThrowIfCancellationRequested();
                    await CheckCloudAsync(timelineGuid, cancellation).ConfigureAwait(false);
                }
                finally { Operations.Release(); }
            }
            catch (OperationCanceledException)
            {
                checkedThisLogin = false;
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "MewUI: cloud timeline check failed");
                foreach (var item in GetDefinitions().Where(item => timelineGuid == null || item.TimelineGuid == timelineGuid))
                    SetTimelineStatus(item.TimelineGuid, "检查失败：" + ex.Message, null);
            }
            finally
            {
                Interlocked.Exchange(ref remoteCheckStarted, 0);
                try { RefreshRows(); }
                catch (Exception ex) { Svc.Log.Error(ex, "MewUI: failed to refresh timeline status"); }
                if (Interlocked.Exchange(ref pendingCloudCheck, 0) != 0 && sessionAttached)
                    RequestCloudCheck();
            }
        });
    }

    internal static void RequestDelete(string timelineGuid)
    {
        QueueFileOperation(timelineGuid, () => DeleteTimeline(timelineGuid));
    }

    internal static void RequestRestore(string timelineGuid)
    {
        QueueFileOperation(timelineGuid, cancellation => RestoreTimelineAsync(timelineGuid, cancellation));
    }

    private static void QueueFileOperation(string guid, System.Action operation)
        => QueueFileOperation(guid, _ => { operation(); return Task.CompletedTask; });

    private static void QueueFileOperation(string guid, Func<CancellationToken, Task> operation)
    {
        var cancellation = lifetime?.Token ?? CancellationToken.None;
        _ = Task.Run(async () =>
        {
            var acquired = false;
            try
            {
                await Operations.WaitAsync(cancellation).ConfigureAwait(false);
                acquired = true;
                await operation(cancellation).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetTimelineStatus(guid, "操作已取消，可重新检查或恢复", null);
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "MewUI: timeline operation failed");
                SetTimelineStatus(guid, "操作失败：" + ex.Message, null);
            }
            finally { if (acquired) Operations.Release(); }
        });
    }

    private static async Task CheckCloudAsync(string? timelineGuid, CancellationToken cancellation)
    {
        RunOnce();
        var catalog = GetDefinitions();
        var selected = string.IsNullOrWhiteSpace(timelineGuid)
            ? catalog
            : catalog.Where(item => string.Equals(item.TimelineGuid, timelineGuid, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (selected.Length == 0) return;

        lock (Sync)
        {
            TimelineStatus["*"] = "正在检查 GitHub 时间轴版本";
            foreach (var item in selected)
                TimelineStatus[item.TimelineGuid] = "正在检查云端版本";
            RefreshRowsLocked();
        }

        var checks = await Task.WhenAll(selected.Select(item => FetchCloudAsync(item, cancellation))).ConfigureAwait(false);
        foreach (var check in checks)
        {
            cancellation.ThrowIfCancellationRequested();
            if (check.Bytes == null)
            {
                SetTimelineStatus(check.Definition.TimelineGuid, "云端检查失败：" + check.Error, null);
                continue;
            }

            try
            {
                ApplyCloudTimeline(check.Definition, check.Bytes, check.Metadata);
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, $"MewUI: cloud timeline update failed for {check.Definition.Name}");
                SetTimelineStatus(check.Definition.TimelineGuid, "更新失败：" + ex.Message, check.Metadata.Version);
            }
        }

        lock (Sync)
        {
            TimelineStatus["*"] = "GitHub 时间轴版本检查完成";
            RefreshRowsLocked();
        }
    }

    private static async Task<CloudCheck> FetchCloudAsync(TimelineDefinition definition, CancellationToken cancellation)
    {
        var url = GitHubTimelineBaseUrl + Uri.EscapeDataString(definition.FileName)
            + "?v=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaximumTimelineBytes)
                throw new InvalidDataException("时间轴文件超过大小限制");

            var bytes = await ReadBoundedAsync(response.Content, timeout.Token).ConfigureAwait(false);
            if (bytes.Length == 0)
                throw new InvalidDataException("云端时间轴为空或超过大小限制");
            var metadata = ReadCloudMetadata(definition, bytes);
            return new CloudCheck(definition, bytes, metadata, string.Empty);
        }
        catch (Exception ex)
        {
            return new CloudCheck(definition, null, default, ex.Message);
        }
    }

    private static TimelineMetadata ReadCloudMetadata(TimelineDefinition definition, byte[] bytes)
    {
        if (!TryReadMetadata(bytes, out var metadata)
            || !string.Equals(metadata.TimelineGuid, definition.TimelineGuid, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(metadata.Format, "PTL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("云端时间轴 GUID 或格式与登记的时间轴不匹配");
        return metadata;
    }

    private static void ApplyCloudTimeline(TimelineDefinition definition, byte[] bytes, TimelineMetadata cloudMetadata,
        bool restoring = false)
    {
        var targetDirectory = GetTargetDirectory();
        Directory.CreateDirectory(targetDirectory);
        var cloudHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var updated = false;
        string result;

        lock (Sync)
        {
            var states = AcrSettingsStore.Current.BundledTimelines;
            states.TryGetValue(cloudMetadata.TimelineGuid, out var state);
            if (state?.Disabled == true && !restoring)
            {
                CloudVersions[cloudMetadata.TimelineGuid] = cloudMetadata.Version;
                TimelineStatus[cloudMetadata.TimelineGuid] = "已删除；云端版本 " + cloudMetadata.Version;
                RefreshRowsLocked();
                return;
            }

            var installed = ReadInstalledTimelines(targetDirectory)
                .Where(file => string.Equals(file.TimelineGuid, cloudMetadata.TimelineGuid, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var local = !restoring && state != null
                ? installed.FirstOrDefault(file => string.Equals(file.FileName, state.FileName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(file.TimelineGuid, cloudMetadata.TimelineGuid, StringComparison.OrdinalIgnoreCase))
                : null;
            // Restoring must not adopt or overwrite another copy with the same GUID.
            if (!restoring) local ??= installed.FirstOrDefault();

            CloudVersions[cloudMetadata.TimelineGuid] = cloudMetadata.Version;
            if (local != null && string.Equals(local.Version, cloudMetadata.Version, StringComparison.Ordinal))
            {
                if (string.Equals(local!.Sha256, cloudHash, StringComparison.Ordinal)
                    || SameTimelineText(File.ReadAllBytes(Path.Combine(targetDirectory, local.FileName)), bytes))
                {
                    states[cloudMetadata.TimelineGuid] = new BundledTimelineSyncState
                    {
                        FileName = local.FileName,
                        // Preserve the on-disk byte hash for later local-edit detection.
                        Sha256 = local.Sha256
                    };
                    updated = true;
                    result = "已是最新版本 " + cloudMetadata.Version;
                }
                else
                    result = "版本号一致但内容不同，保留本地文件";
            }
            else
            {
                var preferredName = restoring && state != null && IsSafeFileName(state.FileName)
                    ? state.FileName : definition.FileName;
                var targetName = local?.FileName
                    ?? (IsSafeFileName(preferredName) && !File.Exists(Path.Combine(targetDirectory, preferredName))
                        ? preferredName : CreateUniqueFileName(targetDirectory, cloudMetadata));
                var targetPath = Path.Combine(targetDirectory, targetName);
                var expectedHash = local?.Sha256;
                WriteAtomically(targetPath, bytes, expectedHash);
                states[cloudMetadata.TimelineGuid] = new BundledTimelineSyncState
                {
                    FileName = targetName,
                    Sha256 = cloudHash
                };
                updated = true;
                result = "已更新到云端版本 " + cloudMetadata.Version;
            }

            if (updated)
                CleanupCompletedArtifacts(Path.Combine(targetDirectory, states[cloudMetadata.TimelineGuid].FileName),
                    cloudMetadata.TimelineGuid);
            TimelineStatus[cloudMetadata.TimelineGuid] = result;
            TimelineStatus["*"] = result;
            RefreshRowsLocked();
        }

        if (updated) AcrSettingsStore.Save();
    }

    private static void DeleteTimeline(string timelineGuid)
    {
        lock (Sync)
        {
            var definition = GetDefinitions().FirstOrDefault(item =>
                string.Equals(item.TimelineGuid, timelineGuid, StringComparison.OrdinalIgnoreCase));
            if (definition == null) return;

            var targetDirectory = GetTargetDirectory();
            var files = ReadInstalledTimelines(targetDirectory)
                .Where(file => string.Equals(file.TimelineGuid, timelineGuid, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var state = AcrSettingsStore.Current.BundledTimelines.TryGetValue(timelineGuid, out var savedState)
                ? savedState
                : null;
            if (state?.Disabled == true) return;
            var selected = state != null && !string.IsNullOrWhiteSpace(state.FileName)
                ? files.FirstOrDefault(file => string.Equals(file.FileName, state.FileName, StringComparison.OrdinalIgnoreCase))
                : null;
            selected ??= files.FirstOrDefault();
            if (selected != null)
            {
                var path = Path.Combine(targetDirectory, selected.FileName);
                if (File.Exists(path)) File.Delete(path);
            }

            AcrSettingsStore.Current.BundledTimelines[timelineGuid] = new BundledTimelineSyncState
            {
                FileName = selected?.FileName ?? state?.FileName ?? string.Empty,
                Sha256 = selected?.Sha256 ?? state?.Sha256 ?? string.Empty,
                Disabled = true
            };
            TimelineStatus[timelineGuid] = "已从本地删除";
            TimelineStatus["*"] = "已删除 " + definition.Name;
            RefreshRowsLocked();
        }

        AcrSettingsStore.Save();
    }

    private static async Task RestoreTimelineAsync(string timelineGuid, CancellationToken cancellation)
    {
        var definition = GetDefinitions().FirstOrDefault(item =>
            string.Equals(item.TimelineGuid, timelineGuid, StringComparison.OrdinalIgnoreCase));
        if (definition == null) return;

        lock (Sync)
        {
            if (!AcrSettingsStore.Current.BundledTimelines.TryGetValue(timelineGuid, out var state)
                || !state.Disabled) return;
            TimelineStatus[timelineGuid] = "正在从 GitHub 恢复时间轴";
            RefreshRowsLocked();
        }

        var check = await FetchCloudAsync(definition, cancellation).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        if (check.Bytes == null)
        {
            SetTimelineStatus(timelineGuid, "恢复失败：" + check.Error, null);
            return;
        }
        // Keep Disabled until the validated cloud file has been written successfully.
        ApplyCloudTimeline(definition, check.Bytes, check.Metadata, restoring: true);
    }

    private static void SetTimelineStatus(string guid, string message, string? cloudVersion)
    {
        lock (Sync)
        {
            TimelineStatus[guid] = message;
            if (cloudVersion != null) CloudVersions[guid] = cloudVersion;
            RefreshRowsLocked();
        }
    }

    private static void RefreshRows()
    {
        lock (Sync) RefreshRowsLocked();
    }

    private static void RefreshRowsLocked()
    {
        var catalog = GetDefinitions();
        var targetDirectory = GetTargetDirectory();
        var installed = Directory.Exists(targetDirectory) ? ReadInstalledTimelines(targetDirectory) : [];
        rows = catalog.Select(definition =>
        {
            var guid = definition.TimelineGuid;
            var state = AcrSettingsStore.Current.BundledTimelines.TryGetValue(guid, out var found) ? found : null;
            var local = state != null && !string.IsNullOrWhiteSpace(state.FileName)
                ? installed.FirstOrDefault(file => string.Equals(file.FileName, state.FileName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(file.TimelineGuid, guid, StringComparison.OrdinalIgnoreCase))
                : null;
            local ??= installed.FirstOrDefault(file => string.Equals(file.TimelineGuid, guid, StringComparison.OrdinalIgnoreCase));
            var hasStatus = TimelineStatus.TryGetValue(guid, out var status);
            var isWorking = status != null && status.StartsWith("正在", StringComparison.Ordinal);
            return new TimelineRow(guid, definition.Name,
                state?.Disabled == true ? "已删除" : local?.Version ?? "未安装",
                CloudVersions.TryGetValue(guid, out var cloudVersion) ? cloudVersion : "未检查",
                hasStatus ? status! : "等待检查",
                state?.Disabled == true, isWorking);
        }).ToArray();
    }

    private static TimelineDefinition[] GetDefinitions() => Definitions;

    private static void CleanupLegacyPackage()
    {
        // Migration only: retire copies shipped by older ACR packages. Never use this as a source.
        var directory = Path.Combine(Path.GetDirectoryName(GetTargetDirectory())!, "ACR", "Cino", "Timelines");
        if (!Directory.Exists(directory)) return;
        foreach (var definition in GetDefinitions())
        {
            var path = Path.Combine(directory, definition.FileName);
            try
            {
                if (File.Exists(path) && TryReadMetadata(File.ReadAllBytes(path), out var metadata)
                    && string.Equals(metadata.TimelineGuid, definition.TimelineGuid, StringComparison.OrdinalIgnoreCase))
                    File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Svc.Log.Warning($"MewUI: could not remove legacy timeline copy {definition.FileName}: {ex.Message}");
            }
        }
        try
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Svc.Log.Warning($"MewUI: could not remove empty legacy timeline directory: {ex.Message}");
        }
    }

    private static string GetTargetDirectory()
        => targetDirectory ??= Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "PureTimelines");

    private static void CleanupCompletedArtifacts(string timelinePath, string timelineGuid)
    {
        var directory = Path.GetDirectoryName(timelinePath)!;
        var fileName = Path.GetFileName(timelinePath);
        var backupPrefix = fileName + ".backup-";
        var temporaryPrefix = fileName + ".luli-tmp-";
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(path);
            try
            {
                // Only retire updater-created backups belonging to this exact installed PTL.
                if (name.StartsWith(backupPrefix, StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
                    && TryReadMetadata(File.ReadAllBytes(path), out var backup)
                    && string.Equals(backup.TimelineGuid, timelineGuid, StringComparison.OrdinalIgnoreCase))
                    File.Delete(path);
                else if (name.StartsWith(temporaryPrefix, StringComparison.OrdinalIgnoreCase)
                         && Guid.TryParseExact(name[temporaryPrefix.Length..], "N", out _)
                         && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromMinutes(10))
                    File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Svc.Log.Warning($"MewUI: could not remove completed timeline update file {name}: {ex.Message}");
            }
        }
    }

    private static bool SameTimelineText(byte[] left, byte[] right)
    {
        static string Normalize(byte[] bytes)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd().Replace("\r\n", "\n").Replace('\r', '\n');
        }
        return string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, CancellationToken cancellation)
    {
        await using var input = await content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellation).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > MaximumTimelineBytes)
                throw new InvalidDataException("云端时间轴超过大小限制");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    internal readonly record struct TimelineRow(string Guid, string Name, string LocalVersion,
        string CloudVersion, string Status, bool IsDeleted, bool IsWorking);

    private sealed record TimelineDefinition(string FileName, string TimelineGuid, string Name);
    private sealed record CloudCheck(TimelineDefinition Definition, byte[]? Bytes, TimelineMetadata Metadata, string Error);

    private static InstalledTimeline[] ReadInstalledTimelines(string directory)
    {
        var files = new List<InstalledTimeline>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (!TryReadMetadata(bytes, out var metadata)) continue;
                files.Add(new InstalledTimeline(Path.GetFileName(path), metadata.TimelineGuid, metadata.Version,
                    Convert.ToHexStringLower(SHA256.HashData(bytes))));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (JsonException) { }
        }
        return files.ToArray();
    }

    private static bool TryReadMetadata(byte[] bytes, out TimelineMetadata metadata)
    {
        metadata = default;
        try
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var document = JsonDocument.Parse(reader.ReadToEnd());
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("Version", out var versionElement)
                || versionElement.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("Meta", out var meta)
                || meta.ValueKind != JsonValueKind.Object
                || !meta.TryGetProperty("TimelineGuid", out var guidElement)
                || guidElement.ValueKind != JsonValueKind.String
                || !Guid.TryParse(guidElement.GetString(), out var guid)
                || guid == Guid.Empty) return false;

            var name = meta.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;
            var format = meta.TryGetProperty("Format", out var formatElement) && formatElement.ValueKind == JsonValueKind.String
                ? formatElement.GetString()
                : null;
            var version = versionElement.GetString();
            if (string.IsNullOrWhiteSpace(name) || !Version.TryParse(version, out _)
                || !root.TryGetProperty("Anchors", out var anchors) || anchors.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("Entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
                return false;
            metadata = new TimelineMetadata(guid.ToString("D"), name, version, format ?? "PTL");
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string CreateUniqueFileName(string directory, TimelineMetadata metadata)
    {
        var safeName = string.Concat(metadata.Name.Select(ch =>
            Path.GetInvalidFileNameChars().Contains(ch) || char.IsControl(ch) ? '_' : ch)).Trim().TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "Timeline";
        if (safeName.Length > 80) safeName = safeName[..80];

        var baseName = $"{safeName}-{metadata.TimelineGuid[..8]}";
        var candidate = baseName + ".json";
        var suffix = 2;
        while (File.Exists(Path.Combine(directory, candidate)))
            candidate = $"{baseName}-{suffix++}.json";
        return candidate;
    }

    private static bool IsSafeFileName(string fileName)
        => !string.IsNullOrWhiteSpace(fileName)
            && string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
            && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private static void WriteAtomically(string path, byte[] bytes, string? expectedCurrentHash)
    {
        var temporaryPath = path + ".luli-tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            if (expectedCurrentHash == null)
            {
                if (File.Exists(path)) throw new IOException("Timeline target already exists.");
            }
            else if (!File.Exists(path)
                     || !string.Equals(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))),
                         expectedCurrentHash, StringComparison.Ordinal))
            {
                throw new IOException("Timeline changed while its cloud update was being applied.");
            }
            File.Move(temporaryPath, path, overwrite: expectedCurrentHash != null);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private readonly record struct TimelineMetadata(string TimelineGuid, string Name, string Version, string Format);
    private sealed record InstalledTimeline(string FileName, string TimelineGuid, string Version, string Sha256);
}
