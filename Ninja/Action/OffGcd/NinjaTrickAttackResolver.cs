using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

using Ninja.PR.Action;
using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Action.OffGcd;

internal sealed class NinjaTrickAttackResolver : NinjaResolver
{
    public static string? GetBlockReason()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)) return "小背刺 QT 未开启";
        if (me?.HasStatus(NinjaActions.ShadowWalkerStatus) != true) return "缺少自身 3848 状态";
        if (target?.CanUseAttackActionOn() != true) return "没有有效目标";
        if (!InMeleeRange) return "目标超出近战范围";
        if (!Ready(NinjaActions.TrickAttackCurrent)) return "36958 尚未转好或未解锁";
        if (NinjaSettings.Instance.LockTrickAttackIntoMug
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) <= 0f
            && PromeSettings.Instance.GetQt(NinjaQts.BigTrick))
            return "锁定小背刺至百雷铳，等待百雷铳";
        if (NinjaCombatState.Recently(NinjaActions.MugCurrent, 1360)) return "刚释放百雷铳，处于 1360 毫秒保护窗口";

        var elapsed = ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain();
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && !target.HasStatus(NinjaActions.MugDebuff))
            return "延后小背刺至百雷铳，目标尚未有 3849";
        if (NinjaCombatState.Recently(NinjaActions.TcjSuiton, 1000) && elapsed < .7f)
            return "刚完成天地人水遁，等待 GCD 起始窗口";
        if (NinjaCombatState.Recently(NinjaActions.Raiton, 1500) && elapsed < .7f)
            return "刚释放雷遁，等待 GCD 起始窗口";
        // if (ActionHelper.GetActionCooldown(NinjaActions.TenChiJin) < 10f && elapsed < 1.45f)
        //     return "天地人冷却小于 10 秒，等待 GCD 起始窗口";
        return null;
    }

    public override CheckResult Check()
    {
        var reason = GetBlockReason();
        return reason == null ? Ok : Blocked(reason);
    }
    public override PAction GetAction()
    {
        NinjaSettings.Instance.TrickAttackDelayUntilNextMug = false;
        return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.TrickAttackCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}
