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

public sealed class TrueNorthOgcd : Ogcd
{
    private static CheckResult Blocked(string reason) => new(false, reason);

    public override CheckResult Check()
    {
        if (!R.Qt("自动真北")) return Blocked("自动真北 QT 关闭");
        var elapsedGcdMs = (int)Math.Max(0f, ActionHelper.GetGcdElapsed() * 1000f);
        var gcdDurationMs = (int)Math.Max(0f, ActionHelper.GetGcdTotal() * 1000f);
        var remainingGcdMs = Math.Max(0, gcdDurationMs - elapsedGcdMs);
        var minimumRemainingGcdMs = gcdDurationMs / 4;
        if (gcdDurationMs > 0 && remainingGcdMs < minimumRemainingGcdMs)
            return Blocked($"GCD 剩余不足25%：剩余 {remainingGcdMs}ms，门槛 {minimumRemainingGcdMs}ms");
        if (R.Charges(R.TrueNorth) < 1f) return Blocked("真北没有可用层数");
        if (ReaperSpellHistory.RecentlyUsed(R.TrueNorth, 2120) || R.Has(1250u) || R.Has(2593u)) return Blocked("真北近期使用或自身状态拦截");

        var target = R.Target;
        if (target == null || !TargetHelper.HasPositionalRequirement(target)) return Blocked("当前目标没有身位要求");

        var adjustedBloodStalk = R.Adjust(R.BloodStalk);
        var afterGluttony = ReaperSpellHistory.RecentlyUsed(R.Gluttony, 5000);
        var afterBloodStalk = ReaperSpellHistory.RecentlyUsed(adjustedBloodStalk, 2120)
            || ReaperSpellHistory.RecentlyUsed(R.BloodStalk, 2120);
        if (!afterGluttony && !afterBloodStalk)
            return Blocked("不在暴食或隐匿挥割后的有效判定窗口；记录未命中或已过期");

        var pos = TargetHelper.GetTargetPositional();
        var wrongFlank = R.Has(2588u) && pos != Positional.Flank;
        var wrongRear = R.Has(2589u) && pos != Positional.Rear;
        if (wrongFlank || wrongRear) return Ok;

        if (!R.Has(2588u) && !R.Has(2589u)) return Blocked("没有 2588/2589 身位状态");
        return Blocked("当前身位符合 2588/2589 要求");
    }
    public override PAction GetAction() => R.A(R.TrueNorth, ActionType.OffGcd, ActionTargetType.Self);
}
