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
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return No;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Suiton) || !PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)) return No;
        if (me.HasStatus(NinjaActions.RaijuReady) || me.HasStatus(NinjaActions.TcjStatus)) return No;
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1 || NinjaCombatState.Recently(NinjaActions.Ten, 2120)) return No;
        if (me.HasStatus(NinjaActions.KassatsuStatus) || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return No;
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus) || NinjaCombatState.Recently(NinjaActions.Suiton, 15000)) return No;
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 19.5f) return No;
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) > 15f) return No;

        var distance = NinjaTarget.Distance;
        var meleeRange = NinjaTarget.AttackRange;
        if (distance > meleeRange + 17f) return No;
        return distance > meleeRange || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 15f
            || ActionHelper.GetActionCooldown(NinjaActions.Ten) <= 5f ? Ok : No;
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
