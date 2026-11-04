using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace LuliPR.Common;

internal sealed class UsageStatisticsData
{
    [JsonIgnore]
    public double TotalSeconds { get; internal set; }

    [JsonPropertyName("TotalSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double LegacyTotalSeconds
    {
        get => 0d;
        set
        {
            if (string.IsNullOrWhiteSpace(TotalSecondsEncrypted))
                TotalSeconds = Math.Max(0d, value);
        }
    }

    [JsonPropertyName("TotalSecondsEncrypted")]
    public string? TotalSecondsEncrypted { get; set; }

    internal void Restore()
    {
        if (!string.IsNullOrWhiteSpace(TotalSecondsEncrypted))
        {
            TotalSeconds = UsageTimeCryptography.Unprotect(TotalSecondsEncrypted) ?? 0d;
        }
    }

    internal void Protect()
    {
        TotalSeconds = Math.Max(0d, TotalSeconds);
        TotalSecondsEncrypted = UsageTimeCryptography.Protect(TotalSeconds);
    }
}

internal static class UsageTimeCryptography
{
    private static readonly byte[] Key = SHA256.HashData(
        "Cino.LuliPR.ACR.UsageStatistics.v1"u8.ToArray());

    internal static string Protect(double seconds)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = BitConverter.GetBytes(seconds);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return Convert.ToBase64String(payload);
    }

    internal static double? Unprotect(string encoded)
    {
        try
        {
            var payload = Convert.FromBase64String(encoded);
            if (payload.Length != 12 + 16 + sizeof(double)) return null;
            var plaintext = new byte[sizeof(double)];
            using var aes = new AesGcm(Key, 16);
            aes.Decrypt(payload[..12], payload[28..], payload[12..28], plaintext);
            var seconds = BitConverter.ToDouble(plaintext);
            return double.IsFinite(seconds) && seconds >= 0d ? seconds : null;
        }
        catch
        {
            return null;
        }
    }
}

internal static class UsageStatistics
{
    private const long PersistIntervalMilliseconds = 10_000;
    private static readonly object Sync = new();
    private static string? activeAcr;
    private static long activeSince;
    private static long lastPersist;

    internal readonly record struct Snapshot(double TotalSeconds, string? ActiveAcr);

    public static void Start(string acrName)
    {
        lock (Sync)
        {
            Accumulate(Environment.TickCount64, persist: false);
            activeAcr = acrName;
            activeSince = Environment.TickCount64;
            lastPersist = activeSince;
            AcrSettingsStore.Save();
        }
    }

    public static void Update()
    {
        lock (Sync)
        {
            if (activeAcr == null) return;
            var now = Environment.TickCount64;
            if (now - lastPersist < PersistIntervalMilliseconds) return;
            Accumulate(now, persist: true);
            lastPersist = now;
        }
    }

    public static void Stop(string acrName)
    {
        lock (Sync)
        {
            if (activeAcr != acrName) return;
            Accumulate(Environment.TickCount64, persist: false);
            activeAcr = null;
            activeSince = 0;
            lastPersist = 0;
            AcrSettingsStore.Save();
        }
    }

    public static Snapshot GetSnapshot()
    {
        lock (Sync)
        {
            var usage = AcrSettingsStore.Current.Usage;
            var total = usage.TotalSeconds;
            if (activeAcr != null && activeSince > 0)
            {
                var seconds = Math.Max(0, Environment.TickCount64 - activeSince) / 1000d;
                total += seconds;
            }

            return new Snapshot(total, activeAcr);
        }
    }

    private static void Accumulate(long now, bool persist)
    {
        if (activeAcr == null || activeSince <= 0) return;
        var elapsedMilliseconds = Math.Max(0, now - activeSince);
        if (elapsedMilliseconds == 0) return;

        var seconds = elapsedMilliseconds / 1000d;
        var usage = AcrSettingsStore.Current.Usage;
        usage.TotalSeconds += seconds;
        activeSince = now;
        if (persist) AcrSettingsStore.Save();
    }
}
