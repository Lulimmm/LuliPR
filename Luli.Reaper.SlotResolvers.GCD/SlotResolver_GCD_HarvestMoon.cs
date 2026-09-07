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

public sealed class HarvestMoonGcd : Gcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("收获月")) return No;
        if (!R.Has(2594u)) return No;
        if (R.Recently(R.BloodStalk, 2500) && R.Recently(R.Gluttony, 5000)) return No;
        if (R.Has(3858u)) return No;
        if (R.Has(2587u)) return No;
        if (R.Has(2593u)) return No;
        if (!R.Near()) return R.Ready(R.HarvestMoon) ? Ok : No;
        if (R.Has(49u)) return R.Ready(R.HarvestMoon) ? Ok : No;
        return No;
    }
    public override PAction GetAction() => R.A(R.HarvestMoon);
}
