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

public sealed class SoulSliceGcd : Gcd
{
    public override CheckResult Check()
    {
        var remainGcd = ActionHelper.GetGcdRemain();
        var currentCharges = R.Charges(R.SoulSlice);
        if (!R.Qt("灵魂切割")) return No;
        if (!R.Near()) return No;
        if (!R.Ready(R.SoulSlice)) return No;
        if (currentCharges < 1f) return No;
        if (R.Has(2587u) || R.Has(3858u)) return No;
        if (JobGaugeHelper.RPR.灵魂值 > 50) return No;
        if (R.Qt("死亡之影") && !R.TargetHas(2586u, 2)) return No;
        // AE allows the basic Soul Slice path directly through level 77.
        if (Core.Me.Level <= 77) return Ok;
        if (ReaperRotation.IsSelectedOpener("烙印3G团辅起手") && EngageManager.GetBattleTime() < 5 && !R.TargetHas(2586u, 15)) return No;
        if (ActionHelper.GetComboLeftTime() is > 0 and < 2 && !R.Has(2972u)) return No;
        if (R.Qt("双附体") && !R.Qt("三附体") && R.Cd(R.ArcaneCircle) < 17.5f && JobGaugeHelper.RPR.灵魂值 <= 50) return Ok;
        if (R.Qt("三附体") && R.Cd(R.ArcaneCircle) is > 17 and < 42.5f && JobGaugeHelper.RPR.魂衣值 < 100) return Ok;
        if (R.Has(2972u) && R.Cd(R.Gluttony) >= 1 && JobGaugeHelper.RPR.魂衣值 < 50 && JobGaugeHelper.RPR.灵魂值 <= 50) return Ok;
        if (R.Qt("只打大丰收附体") && JobGaugeHelper.RPR.灵魂值 <= 50) return Ok;
        if (R.ChargeSoon(R.SoulSlice, remainGcd)) return Ok;
        return Ok;
    }
    public override PAction GetAction() => R.A(R.SoulSlice);
}
