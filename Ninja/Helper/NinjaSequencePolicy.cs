using System;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

// Mudra GCDs remain in the slot until the current GCD is ready. PR exposes
// that policy globally, so scope it to one Ninja sequence and restore it.
internal static class NinjaSequencePolicy
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
        // The normal ready window avoids a sub-frame recast dependency that
        // can leave the sequence stuck at 1 ms.
        hacks.OptimizeGcdOffset = false;
        terminalAction = terminal;
        expiresAt = Environment.TickCount64 + SequenceTimeoutMs;
        active = true;
    }

    public static void Record(uint actionId)
    {
        if (active && actionId == terminalAction
            && (terminalAction == NinjaActions.TcjSuiton
                || Core.Me?.HasStatus(NinjaActions.MudraStatus) != true))
            End();
    }

    public static void Update()
    {
        if (active
            && terminalAction is NinjaActions.Suiton or NinjaActions.Raiton or NinjaActions.HyoshoRanryu
            && Core.Me?.HasStatus(NinjaActions.MudraStatus) != true)
        {
            End();
            return;
        }
        if (active && Environment.TickCount64 >= expiresAt) End();
    }

    public static void Reset() => End();

    public static bool IsNormalMudraActive =>
        active
        && terminalAction is NinjaActions.Suiton or NinjaActions.Raiton or NinjaActions.HyoshoRanryu
        && Core.Me?.HasStatus(NinjaActions.MudraStatus) == true
        && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true;

    public static bool IsActive => active;
    public static uint TerminalAction => terminalAction;

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
