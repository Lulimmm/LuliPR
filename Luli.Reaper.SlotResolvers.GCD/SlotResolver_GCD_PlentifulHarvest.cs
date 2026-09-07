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
    public override CheckResult Check()
    {
        if (!R.Qt("大丰收")) return No;
        if (Core.Me.Level >= 70 && (R.Has(3858u) || R.Has(2587u))) return No;
        if (R.Has(2593u)) return No;
        if (R.Distance > 15f) return No;
        if (R.Has(2593u)) return No;
        if (R.Has(2972u)) return No;
        if (!R.Has(2592u)) return No;
        return R.Ready(R.PlentifulHarvest) ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.PlentifulHarvest);
}
