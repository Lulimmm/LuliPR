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
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra)
            || me == null || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960u, 1060)
            || JobGaugeHelper.NIN.Ninki < 50
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)
            || me.HasStatus(NinjaActions.TcjStatus)
            || NinjaCombatState.IsTcjSequenceActive
            || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
                && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && me.HasStatus(NinjaActions.ShadowWalkerStatus))) return No;

        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return Ok;
        if (JobGaugeHelper.NIN.Ninki == 100) return Ok;
        return !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && JobGaugeHelper.NIN.Ninki > 60 ? Ok : No;
    }

    public override PAction GetAction() =>
        Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
}
