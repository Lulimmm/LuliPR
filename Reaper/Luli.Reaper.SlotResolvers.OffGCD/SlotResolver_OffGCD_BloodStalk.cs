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

public sealed class BloodStalkOgcd : Ogcd
{
    public override CheckResult Check()
    {
        var currentCharges = R.Charges(R.SoulSlice);
        if (!R.Qt("隐匿挥割")) return No;
        if (R.Qt("死亡之影") && !R.TargetHas(2586u, 3.5f)) return No;
        if (!R.Weave) return No;
        if (!R.Near()) return No;
        if (!R.Ready(R.BloodStalk)) return No;
        if (JobGaugeHelper.RPR.灵魂值 < 50) return No;
        if (R.Has(3858u)) return No;
        if (R.Has(2587u)) return No;
        if (R.Recently(R.BloodStalk, 2500) || R.Recently(R.Gluttony, 5000)) return No;
        if (R.Has(3860u)) return No;
        if (R.Has(2593u)) return No;
        if (JobGaugeHelper.RPR.魂衣值 == 100) return No;
        if (R.Cd(R.ArcaneCircle) is >= 99.5f and <= 115f) return No;
        if (R.Has(2972u) && R.Qt("只打大丰收附体"))
        {
            if (R.Cd(24393u)>=2.5f && JobGaugeHelper.RPR.魂衣值 < 100 && JobGaugeHelper.RPR.灵魂值 >= 50)
            {
                return Ok;
            }
        }
        if (R.Has(2972u) && !R.Qt("只打大丰收附体"))
        {
            if (R.Charges(R.SoulSlice) >= 1 && JobGaugeHelper.RPR.魂衣值 < 50 && JobGaugeHelper.RPR.灵魂值 >= 50)
            {
                return Ok;
            }
        }
        if (R.Qt("死亡之影") && !R.TargetHas(2586u, 2)) return No;
        if (R.Qt("隐匿挥割"))
        {
            if (R.Qt("暴食") && ActionHelper.GetActionCooldown(R.Gluttony) <= ActionHelper.GetGcdTotal() * 2 && JobGaugeHelper.RPR.灵魂值 < 100)
            {
                return No;
            }
            if (R.Qt("附体") && R.Qt("双附体") && !R.Qt("三附体") && R.Cd(R.ArcaneCircle) < 17.5f && JobGaugeHelper.RPR.魂衣值 < 50)
            {
                if(JobGaugeHelper.RPR.魂衣值 < 50 && !R.Has(3858u))
                return Ok;
            }
            if (R.Qt("附体") && R.Qt("三附体") && R.Cd(R.ArcaneCircle) is > 15 and < 42.5f)
            {
                if (JobGaugeHelper.RPR.魂衣值 < 100)
                {
                    return Ok;
                }
            }
            if (R.Qt("倾泄隐匿挥割") || R.Has(49u))
            {
                if (JobGaugeHelper.RPR.灵魂值 > 50)
                {
                    return Ok;
                }
            }
            if (!R.Qt("倾泄隐匿挥割"))
            {
                if (JobGaugeHelper.RPR.灵魂值 == 100)
                {
                    return Ok;
                }
            }
            if(currentCharges > 1.9 && !R.Has(2599u) && !R.Has(49u)
                && R.Cd(24393u) >= 4.5f && R.Cd(24405u) < 95f)
            {
                if (JobGaugeHelper.RPR.灵魂值 > 50)
                {
                    return Ok;
                }
            }
                return No;
        }

        return Ok;
    }
    public override PAction GetAction() => R.A(R.Adjust(R.BloodStalk), ActionType.OffGcd);
}
