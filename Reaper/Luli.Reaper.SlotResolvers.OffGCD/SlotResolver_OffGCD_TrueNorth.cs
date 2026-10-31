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

public sealed class TrueNorthOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("自动真北")) return No;
        if (ActionHelper.GetGcdRemain() < .6f) return No;
        if (R.Charges(R.TrueNorth) < 1f) return No;
        if (R.Recently(R.TrueNorth, 2120) || R.Has(1250u) || R.Has(2593u)) return No;

        var gcdTotalMs = (int)(ActionHelper.GetGcdTotal() * 1000f);
        var gcdElapsedMs = (int)(ActionHelper.GetGcdElapsed() * 1000f);
        if (gcdElapsedMs <= gcdTotalMs / 4 * 3) return No;

        var target = R.Target;
        if (target == null || !TargetHelper.HasPositionalRequirement(target)) return No;

        if (!R.Recently(R.Gluttony, 5000)
            && !R.Recently(R.Adjust(R.BloodStalk), 2120)) return No;

        var pos = TargetHelper.GetTargetPositional();
        return (R.Has(2588u) && pos != Positional.Flank) || (R.Has(2589u) && pos != Positional.Rear) ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.TrueNorth, ActionType.OffGcd, ActionTargetType.Self);
}
