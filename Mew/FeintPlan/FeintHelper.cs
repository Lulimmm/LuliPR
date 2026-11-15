using System;
using System.Collections.Generic;

namespace Mew.FeintPlan;

internal static class FeintHelper
{
    private sealed class DelayState
    {
        public long ReadyAt;
        public long PendingUntil;
        public bool Triggered;
    }

    private static readonly Dictionary<uint, DelayState> Delays = new();

    public static int CalcDelayMs(float totalCastTimeInSec)
        => (int)totalCastTimeInSec * 1000 - 700;

    public static void SetDelay(uint nodeId, int delayMs)
    {
        var now = Environment.TickCount64;
        if (!Delays.TryGetValue(nodeId, out var state))
            Delays[nodeId] = state = new DelayState();

        if (delayMs <= 0)
        {
            state.ReadyAt = now;
            return;
        }

        // Ignore duplicate positive delays while one is pending,
        // but permit rearming after it completes. No asynchronous continuation
        // survives Clear/ClearNodeId, so a previous phase cannot revive a timer.
        if (now < state.PendingUntil) return;
        state.ReadyAt = state.PendingUntil = now + delayMs;
        state.Triggered = false;
    }

    public static void SetDelayOnce(uint nodeId, int delayMs)
    {
        // FRU intentionally keeps its initial deadline while waiting for Feint.
        if (!Delays.ContainsKey(nodeId)) SetDelay(nodeId, delayMs);
    }

    public static bool IsReady(uint nodeId, Func<bool>? readyPredicate = null)
    {
        if (!Delays.TryGetValue(nodeId, out var state)
            || Environment.TickCount64 < state.ReadyAt
            || (readyPredicate != null && !readyPredicate())
            || state.Triggered)
            return false;

        state.Triggered = true;
        return true;
    }

    public static bool TryGetPendingDelay(uint nodeId, out long remainingMs)
    {
        remainingMs = 0;
        if (!Delays.TryGetValue(nodeId, out var state) || state.Triggered) return false;
        remainingMs = Math.Max(0, state.ReadyAt - Environment.TickCount64);
        return true;
    }

    public static void Clear()
    {
        Delays.Clear();
    }

    public static void ClearNodeId(uint nodeId)
    {
        Delays.Remove(nodeId);
    }
}
