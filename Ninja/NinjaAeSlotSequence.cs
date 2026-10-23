using System;
using PromeRotation.Data;

namespace Ninja.PR;

// AE's DontUseGcdOpt is action-local: mudra GCDs remain in the slot until
// the current GCD is ready. PR exposes that policy globally, so scope the
// equivalent non-optimized GCD policy to one Ninja sequence and restore it.
internal static class NinjaAeSlotSequence
{
    private const int SequenceTimeoutMs = 30000;
    private static bool active;
    private static uint terminalAction;
    private static long expiresAt;
    private static bool savedOptimize;

    public static void Begin(uint terminal)
    {
        if (active)
        {
            terminalAction = terminal;
            expiresAt = Environment.TickCount64 + SequenceTimeoutMs;
            return;
        }
        var hacks = PromeSettings.Instance.Hacks;
        savedOptimize = hacks.OptimizeGcdOffset;
        // AE's DontUseGcdOpt disables offset optimization; it does not
        // require a sub-frame client-side recast reading. Let PR use its
        // normal ready window so the sequence cannot get stuck at 1 ms.
        hacks.OptimizeGcdOffset = false;
        terminalAction = terminal;
        expiresAt = Environment.TickCount64 + SequenceTimeoutMs;
        active = true;
    }

    public static void Record(uint actionId)
    {
        if (active && actionId == terminalAction) End();
    }

    public static void Update()
    {
        if (active && Environment.TickCount64 >= expiresAt) End();
    }

    public static void Reset() => End();

    private static void End()
    {
        if (!active) return;
        var hacks = PromeSettings.Instance.Hacks;
        hacks.OptimizeGcdOffset = savedOptimize;
        hacks.NormalizeGcdOffset();
        active = false;
        terminalAction = 0;
        expiresAt = 0;
    }
}
