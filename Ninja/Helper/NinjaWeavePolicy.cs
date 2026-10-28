using System;
using PromeRotation.Data;

namespace Ninja.PR.Helper;

/// <summary>
/// Narrows the oGCD clipping gate for Ninja while preserving the user's
/// original global settings when the rotation is unloaded.
/// </summary>
internal static class NinjaWeavePolicy
{
    private const int DesiredQueueWindowMs = 300;
    private static bool active;
    private static bool savedPreventOgcdGcdClipping;
    private static int savedGcdQueueWindowMs;
    private static int savedGcdOffsetMs;

    public static void Enter()
    {
        if (active) return;

        var hacks = PromeSettings.Instance.Hacks;
        hacks.NormalizeGcdOffset();
        savedPreventOgcdGcdClipping = hacks.PreventOgcdGcdClipping;
        savedGcdQueueWindowMs = hacks.GcdQueueWindowMs;
        savedGcdOffsetMs = hacks.GcdOffsetMs;

        // The host uses the normal GcdQueueWindowSeconds only when the
        // conservative PreventOgcdGcdClipping switch is disabled. Keep the
        // displayed queue-window field fixed at exactly 300ms.
        hacks.PreventOgcdGcdClipping = false;
        hacks.GcdQueueWindowMs = DesiredQueueWindowMs;
        hacks.NormalizeGcdOffset();
        active = true;
    }

    public static void Exit()
    {
        if (!active) return;

        var hacks = PromeSettings.Instance.Hacks;
        hacks.PreventOgcdGcdClipping = savedPreventOgcdGcdClipping;
        hacks.GcdQueueWindowMs = savedGcdQueueWindowMs;
        hacks.GcdOffsetMs = savedGcdOffsetMs;
        hacks.NormalizeGcdOffset();
        active = false;
    }
}
