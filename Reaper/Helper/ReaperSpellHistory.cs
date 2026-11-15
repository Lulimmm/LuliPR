using System;
using System.Collections.Generic;

namespace Reaper.PR;

internal static class ReaperSpellHistory
{
    private const int RetentionMilliseconds = 5000;
    private static readonly Dictionary<uint, long> spellCastTime = new();

    internal static void RecordAction(uint actionId)
    {
        if (actionId == 0) return;

        var now = Environment.TickCount64;
        RemoveExpired(now);
        spellCastTime[actionId] = now;

        if (actionId == R.BloodStalk || actionId == R.Adjust(R.BloodStalk))
            spellCastTime[R.BloodStalk] = now;
        if (actionId == R.Gluttony || actionId == R.Adjust(R.Gluttony))
            spellCastTime[R.Gluttony] = now;
    }

    internal static bool RecentlyUsed(uint actionId, int spanMs = 1200)
    {
        var now = Environment.TickCount64;
        RemoveExpired(now);
        if (!spellCastTime.TryGetValue(actionId, out var lastUsedAt)) return false;

        // A shorter query window must not erase a cast needed by a later, longer query.
        // RemoveExpired owns record expiry for all callers.
        return now - lastUsedAt < spanMs;
    }

    private static void RemoveExpired(long now)
    {
        List<uint>? expired = null;
        foreach (var entry in spellCastTime)
        {
            if (now - entry.Value >= RetentionMilliseconds)
                (expired ??= new List<uint>()).Add(entry.Key);
        }

        if (expired == null) return;
        foreach (var actionId in expired)
            spellCastTime.Remove(actionId);
    }
}
