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

public sealed class ShadowOfDeathGcd : Gcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("死亡之影")) return No;
        if (R.Has(2587u) || R.Has(3858u)) return No;
        if (!R.Near()) return No;
        if (R.TargetHas(2586u, 40)) return No;
        var cd = R.Cd(R.ArcaneCircle);
        if (R.Qt("三附体") && cd is >= 10f and <= 15f) return R.Ready(R.ShadowOfDeath) ? Ok : No;
        if (cd >= 10 && !R.TargetHas(2586u, ActionHelper.GetGcdTotal() + ActionHelper.GetGcdRemain() + .2f)) return Ok;
        if (cd < 10 && !R.Qt("三附体") && !R.TargetHas(2586u, 20)) return Ok;
        if (EngageManager.GetBattleTime() < 5 && R.Charges(R.SoulScythe) >= 2) return No;
        return No;
    }
    public override PAction GetAction() => R.A(R.ShadowOfDeath);
}
