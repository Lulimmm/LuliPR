using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace LuliPR.Common;

internal sealed class AcrSettingsDocument
{
    public NinjaSettingsData Ninja { get; set; } = new();
    public ReaperSettingsData Reaper { get; set; } = new();
    public UsageStatisticsData Usage { get; set; } = new();
    public ConcurrentDictionary<string, BundledTimelineSyncState> BundledTimelines { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class BundledTimelineSyncState
{
    public string FileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public bool Disabled { get; set; }
}

internal sealed class NinjaSettingsData
{
    public string Opener = string.Empty;
    public bool OpenerBurst;
    public bool Positional = true;
    public bool LockTrickAttackIntoMug = true;
    public bool TrickAttackDelayUntilNextMug;
    public bool P5PerfectBunshin;
    public bool RaitonBackstab;
    public bool RaitonWhenOutOfRange;
    public bool PhantomOnlyForRange;
    public bool ForceRaiton;
    public float ExtraRange = 3f;
    public float TTK = 1.5f;
}

internal sealed class ReaperSettingsData
{
    public string? Selected { get; set; }
    public bool OpenerBurst { get; set; }
    public bool OpenerRush { get; set; }
    public int PotionType { get; set; }
    public bool HideQtPage { get; set; }
    public bool HideHotkeyPage { get; set; }
    public float ExtraRange { get; set; } = 3f;
    public float MoveCasting { get; set; } = 0.4f;
}

internal static class AcrSettingsStore
{
    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true, WriteIndented = true };
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "Cino");
    private static readonly string SettingsPath = Path.Combine(DirectoryPath, "setting.json");
    private static readonly string LegacyNinjaPath = Path.Combine(DirectoryPath, "ninja.json");
    private static readonly string LegacyReaperPath = Path.Combine(DirectoryPath, "opener.json");

    public static AcrSettingsDocument Current { get; } = Load();

    public static void Save()
    {
        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                Current.Usage.Protect();
                var temporaryPath = SettingsPath + ".tmp";
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Current, JsonOptions));
                File.Move(temporaryPath, SettingsPath, true);
            }
            catch
            {
                // Settings are optional; keep the current in-memory values when persistence is unavailable.
            }
        }
    }

    private static AcrSettingsDocument Load()
    {
        var migrated = false;
        AcrSettingsDocument? settings = null;
        try
        {
            if (File.Exists(SettingsPath))
                settings = JsonSerializer.Deserialize<AcrSettingsDocument>(File.ReadAllText(SettingsPath), JsonOptions);
        }
        catch
        {
            // A damaged settings file should not prevent either ACR from loading.
        }

        var hadValidSettings = settings != null;
        settings ??= new AcrSettingsDocument();
        settings.Ninja ??= new NinjaSettingsData();
        settings.Reaper ??= new ReaperSettingsData();
        settings.Usage ??= new UsageStatisticsData();
        settings.BundledTimelines = new ConcurrentDictionary<string, BundledTimelineSyncState>(
            settings.BundledTimelines ?? [], StringComparer.OrdinalIgnoreCase);
        settings.Usage.Restore();

        // Read the two pre-unification files only when setting.json did not exist.
        // This preserves current settings and makes the migration one-time and deterministic.
        if (!hadValidSettings)
        {
            var ninja = settings.Ninja;
            var reaper = settings.Reaper;
            migrated |= TryReadLegacy(LegacyNinjaPath, ref ninja);
            migrated |= TryReadLegacy(LegacyReaperPath, ref reaper);
            settings.Ninja = ninja;
            settings.Reaper = reaper;
        }

        if (migrated || !hadValidSettings || !File.Exists(SettingsPath))
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
                DeleteLegacyFiles();
            }
            catch
            {
                // The ACR remains usable if the config directory is read-only.
            }
        }
        else
        {
            // A valid unified file is authoritative. Remove stale legacy files
            // left by an earlier plugin version without importing them again.
            DeleteLegacyFiles();
        }

        return settings;
    }

    private static bool TryReadLegacy<T>(string path, ref T target) where T : class
    {
        if (!File.Exists(path)) return false;
        try
        {
            var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
            if (loaded == null) return false;
            target = loaded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteLegacyFiles()
    {
        try { if (File.Exists(LegacyNinjaPath)) File.Delete(LegacyNinjaPath); } catch { }
        try { if (File.Exists(LegacyReaperPath)) File.Delete(LegacyReaperPath); } catch { }
    }
}
