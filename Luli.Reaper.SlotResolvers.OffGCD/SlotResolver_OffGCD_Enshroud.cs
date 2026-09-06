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

public sealed class EnshroudOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("附体") || !R.Ready(R.Enshroud) || !R.Weave || R.Has(2587u) || R.Has(3858u) || R.Has(2593u) || R.Has(3860u)) return No;
        if (R.Qt("只打大丰收附体") && !R.Has(3905u)) return No;
        if (JobGaugeHelper.RPR.魂衣值 < 50 && !R.Has(3905u)) return No;
        if (R.Has(3905u) && !R.Qt("暴食前置") && !R.Has(2593u)) return Ok;
        if (R.Qt("双附体") && !R.Qt("三附体") && R.Cd(R.ArcaneCircle) < 42.5f)
        {
            if (!R.Ready(R.ArcaneCircle) && R.Cd(R.ArcaneCircle) <= ActionHelper.GetGcdTotal() && JobGaugeHelper.RPR.魂衣值 >= 50 && !R.TargetHas(2586u, 22)) return Ok;
            if (!R.Ready(R.ArcaneCircle) && R.Cd(R.ArcaneCircle) - ActionHelper.GetGcdRemain() <= 1.25f && JobGaugeHelper.RPR.魂衣值 >= 50 && R.TargetHas(2586u, 22)) return Ok;
            return No;
        }
        if (R.Qt("双附体") && !R.Qt("三附体") && R.Cd(R.ArcaneCircle) < 42.5f && R.Cd(R.ArcaneCircle) > 1.3f && !R.TargetHas(2586u, 22)) return Ok;
        if (R.Qt("三附体") && R.Cd(R.ArcaneCircle) is > 9 and < 42.5f && JobGaugeHelper.RPR.魂衣值 == 100) return Ok;
        if (R.Has(2972u) && JobGaugeHelper.RPR.魂衣值 >= 50) return Ok;
        if (R.Cd(R.ArcaneCircle) > 40 && R.Cd(R.Gluttony) < 6) return No;
        if (!R.Has(2972u) && R.Has(2592u)) return No;
        if (R.Qt("灵魂切割") && JobGaugeHelper.RPR.魂衣值 <= 80 && !R.Has(2599u) && !R.Has(49u)
            && R.Near() && R.NearbyEnemies() < 2 && !R.HasSingleTargetFirewall
            && R.Charges(R.SoulSlice) >= 1.7f)
            return No;
        return JobGaugeHelper.RPR.魂衣值 >= 50 ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.Enshroud, ActionType.OffGcd, ActionTargetType.Self);
}
