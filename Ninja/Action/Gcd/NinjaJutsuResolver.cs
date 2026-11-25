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

namespace Ninja.PR.Action.Gcd;

internal sealed class NinjaJutsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        if (NinjaSequencePolicy.IsNormalMudraActive) return Blocked("已有普通结印序列，等待当前忍术结束");
        if (!PromeSettings.Instance.GetQt(NinjaQts.Suiton) || !PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)) return Blocked("水遁或小背刺开关关闭");
        if (NinjaCombatState.HasRaijuReadyForRotation || NinjaCombatState.IsTcjSequenceActive) return Blocked("雷兽状态保留或天地人独占，暂不开始水遁");
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1) return Blocked("结印可用次数不足 1");
        if (NinjaCombatState.Recently(NinjaActions.Ten, 2120)) return Blocked("最近 2.12 秒已输入天印，避免重复开始");
        if (me.HasStatus(NinjaActions.KassatsuStatus) || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return Blocked("生杀状态存在或最近 2.12 秒使用生杀，保留给冰晶");
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)) return Blocked("已有水遁赋予的状态（3848），无需重复水遁");
        if (NinjaCombatState.Recently(NinjaActions.Suiton, 15000)) return Blocked("最近 15 秒观测过普通水遁，暂不重复");
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 19.5f) return Blocked("小背刺冷却超过 19.5 秒，尚未到预备水遁窗口");
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug
            && PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) > 15f) return Blocked("设置延后背刺至夺取，夺取冷却仍超过 15 秒");

        var distance = NinjaTarget.Distance;
        var meleeRange = NinjaTarget.AttackRange;
        if (distance > meleeRange + 17f) return Blocked("目标超过水遁分支允许的距离（近战范围加 17 米）");
        return distance > meleeRange || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 15f
            || ActionHelper.GetActionCooldown(NinjaActions.Ten) <= 5f ? Ok : Blocked("近战范围内等待：小背刺冷却不足 15 秒，或结印冷却不超过 5 秒时才开始水遁");
    }

    public override PAction GetAction()
    {
        NinjaSequencePolicy.Begin(NinjaActions.Suiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
