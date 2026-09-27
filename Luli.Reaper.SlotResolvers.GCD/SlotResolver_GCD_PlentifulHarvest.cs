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

public sealed class PlentifulHarvestGcd : Gcd
{
    internal static bool IsReadyForRotation()
    {
        if (!R.Qt("大丰收")) return false;
        if (Core.Me.Level >= 70 && (R.Has(3858u) || R.Has(2587u))) return false;
        if (R.Has(2593u)) return false;
        if (R.Distance > 15f) return false;
        if (R.Has(2593u)) return false;
        if (R.Has(2972u)) return false;
        if (!R.Has(2592u)) return false;
        return R.Ready(R.PlentifulHarvest);
    }

    public override CheckResult Check()
    {
        return IsReadyForRotation() ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.PlentifulHarvest);
}
