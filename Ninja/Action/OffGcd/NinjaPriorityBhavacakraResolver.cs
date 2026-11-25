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

internal sealed class NinjaPriorityBhavacakraResolver : NinjaResolver
{
    // The first oGCD resolver handles overflow/spend
    // guard and is intentionally separate from the later Bhavacakra resolver.
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra)) return Blocked("六道轮回开关关闭");
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        if (NinjaTarget.Distance > NinjaTarget.AttackRange) return Blocked("目标超出近战范围");
        if (NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960u, 1060)) return Blocked("最近 1.06 秒已使用六道轮回，避免重复");
        if (JobGaugeHelper.NIN.Ninki < 50) return Blocked("忍气不足 50");
        if (NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)) return Blocked("刚使用夺取，处于 1.06 秒保护间隔");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用六道轮回");
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
                && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && me.HasStatus(NinjaActions.ShadowWalkerStatus)) return Blocked("小背刺将在 1.26 秒内就绪且有水遁状态，六道轮回暂时让位");

        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return Ok;
        if (JobGaugeHelper.NIN.Ninki == 100) return Ok;
        return PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
            && !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && JobGaugeHelper.NIN.Ninki > 60 ? Ok : Blocked("尚未命中优先消耗窗口：命水即将就绪、忍气满 100，或夺取前忍气超过 60 的指定设置");
    }

    public override PAction GetAction() =>
        BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
}
