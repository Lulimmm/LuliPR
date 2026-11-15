using System;
using System.Collections.Generic;

namespace Mew.FeintPlan;

internal static class FeintHelper
{
    private static readonly Dictionary<uint, long> ReadyAt = new();
    private static readonly HashSet<uint> Triggered = new();

    public static int CalcDelayMs(float totalCastTimeInSec)
        => (int)totalCastTimeInSec * 1000 - 700;

    public static void SetDelay(uint nodeId, int delayMs)
    {
        if (ReadyAt.ContainsKey(nodeId)) return;
        ReadyAt[nodeId] = Environment.TickCount64 + Math.Max(0, delayMs);
    }

    public static bool IsReady(uint nodeId, Func<bool>? readyPredicate = null)
    {
        if (!ReadyAt.TryGetValue(nodeId, out var readyAt)
            || Environment.TickCount64 < readyAt
            || (readyPredicate != null && !readyPredicate())
            || !Triggered.Add(nodeId))
            return false;

        return true;
    }

    public static void Clear()
    {
        ReadyAt.Clear();
        Triggered.Clear();
    }

    public static void ClearNodeId(uint nodeId)
    {
        ReadyAt.Remove(nodeId);
        Triggered.Remove(nodeId);
    }
}
