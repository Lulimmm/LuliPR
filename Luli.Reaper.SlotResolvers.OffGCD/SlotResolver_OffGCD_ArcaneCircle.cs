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

public sealed class ArcaneCircleOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("神秘环") || !R.Ready(R.ArcaneCircle) || ActionHelper.GetGcdRemain() < .2f) return No;

        var cooldown = R.Cd(R.ArcaneCircle);
        var gcdRemain = ActionHelper.GetGcdRemain();

        // Keep the AE special-window priority before opener suppression. The
        // special branch only applies when the cooldown will be ready before
        // this GCD ends; otherwise AE falls through to normal release logic.
        if (R.Qt("双附体") && !R.Qt("单附体") && !R.Qt("三附体") && !R.Qt("只打大丰收附体")
            && cooldown - gcdRemain < 0
            && cooldown > 0 && cooldown < 5
            && R.TargetHas(2586u, 22)
            && (JobGaugeHelper.RPR.魂衣值 >= 50 || R.Has(2593u)))
        {
            if (JobGaugeHelper.RPR.夜游魂 <= 4) return Ok;
            if (JobGaugeHelper.RPR.夜游魂 == 5) return No;
        }

        if (ReaperRotation.IsSelectedOpener("烙印3G团辅起手") && EngageManager.GetBattleTime() < 6) return No;
        if (ReaperRotation.IsSelectedOpener("切割2G团辅起手") && EngageManager.GetBattleTime() < 5 && !R.TargetHas(2586u, 2.5f)) return No;

        return Ok;
    }
    public override PAction GetAction() => R.A(R.ArcaneCircle, ActionType.OffGcd, ActionTargetType.Self);
}
