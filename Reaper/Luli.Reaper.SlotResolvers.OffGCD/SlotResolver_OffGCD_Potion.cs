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

public sealed class PotionOgcd : Ogcd
{
    public override CheckResult Check()
    {
        if (!R.Qt("爆发药") || !R.Weave) return No;
        var potion = GameData.GetBestPotionId();
        if (potion == 0) return No;
        if (ActionHelper.IsItemOnCooldown(potion)) return No;
        var cd = R.Cd(R.ArcaneCircle);
        var battleTime = EngageManager.GetBattleTime();
        if (ReaperRotation.PotionType == 2 && battleTime < 110f) return No;
        if (ReaperRotation.PotionType == 0)
        {
            if (cd is > 2.5f and < 5 && !R.Qt("三附体")) return Ok;
            if (cd is > 7.5f and < 10 && R.Qt("三附体")) return Ok;
        }
        else if (ReaperRotation.PotionType == 1)
        {
            if (cd is > 2.5f and < 5 || JobGaugeHelper.RPR.魂衣值 >= 50) return Ok;
        }
        else if (cd is > 2.5f and < 5 && !R.Qt("三附体")) return Ok;
        else if (cd is > 7.5f and < 10 && R.Qt("三附体")) return Ok;
        return No;
    }
    public override PAction GetAction() => new(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self) { RequiresVerification = true };
}
