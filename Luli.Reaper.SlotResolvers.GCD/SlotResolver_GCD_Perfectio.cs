using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.PureTimeline;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Updaters;
using PromeRotation.UI.HotKey;

namespace Reaper.PR;

public sealed class PerfectioGcd : Gcd
{
    internal static bool IsAvailableForRotation()
    {
        // Match AE: Perfectio is gated by unlock, QT, the proc status, and
        // the same conflicting aura checks. Readiness is handled by the GCD
        // decision window, so it must not make Soul Slice consume the proc.
        if (!ActionHelper.IsUnlocked(R.Perfectio)) return false;
        if (R.Has(2587u) || R.Has(3858u)) return false;
        if (!R.Qt("完人")) return false;
        return R.Has(3860u);
    }

    public override CheckResult Check()
    {
        return IsAvailableForRotation() ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.Perfectio);
}
