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

public sealed class GluttonyOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("暴食") || !R.Ready(R.Gluttony) || !R.Weave || R.Distance > R.Range() + 25 || R.Has(2587u) || R.Has(2593u) || JobGaugeHelper.RPR.灵魂值 < 50) return No;
        if (R.Recently(R.BloodStalk, 2500)) return No;
        if (R.Qt("提高完人优先级") && R.Has(3860u)) return No;
        var battleTime = EngageManager.GetBattleTime();
        if (ReaperRotation.IsSelectedOpener("烙印3G团辅起手") && battleTime < 6) return No;
        if (!R.TargetHas(2586u, 5) && !(ReaperRotation.IsSelectedOpener("特化1g暴食0g团辅") && battleTime < 6)) return No;
        if (R.Has(3905u)) return R.Qt("暴食前置") ? Ok : No;
        if (R.Has(2972u) && !R.HasFor(2972u, 2)) return No;
        if (R.Has(2972u) && JobGaugeHelper.RPR.灵魂值 >= 50) return JobGaugeHelper.RPR.魂衣值 < 50 ? Ok : No;
        if (R.Has(2592u) && R.Qt("大丰收")) return No;
        if (R.Cd(R.Enshroud) > 0 && R.Cd(R.Enshroud) <= 1 && R.Qt("双附体") && R.Qt("附体") && (JobGaugeHelper.RPR.魂衣值 >= 50 || R.Has(3905u))) return No;
        if (R.Qt("灵魂切割") && R.Charges(R.SoulSlice) >= 1.83f && JobGaugeHelper.RPR.灵魂值 == 50 && !R.Has(2599u) && !R.Has(49u)
            && R.NearbyEnemies() < 2 && !R.HasSingleTargetFirewall) return No;
        if (ActionHelper.GetComboLeftTime() is > 0 and < 6 && !R.Has(2972u) && !R.Has(49u)
            && R.NearbyEnemies() < 2 && !R.HasSingleTargetFirewall) return No;
        return R.Cd(R.ArcaneCircle) > 1 ? Ok : No;
    }
    public override PAction GetAction() => R.A(R.Gluttony, ActionType.OffGcd);
}
