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
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) || me?.HasStatus(NinjaActions.ShadowWalkerStatus) != true
            || target?.CanUseAttackActionOn() != true || !InMeleeRange || !Ready(NinjaActions.TrickAttackCurrent)
            || (NinjaSettings.Instance.LockTrickAttackIntoMug
                && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) <= 0f
                && PromeSettings.Instance.GetQt(NinjaQts.BigTrick))
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1360)) return No;

        var elapsed = ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain();
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && !target.HasStatus(NinjaActions.MugDebuff)) return No;
        // The parameterless recent-use check uses a 1 second window.
        if (NinjaCombatState.Recently(NinjaActions.TcjSuiton, 1000) && elapsed < .7f) return No;
        if (NinjaCombatState.Recently(NinjaActions.Raiton, 1500) && elapsed < .7f) return No;
        if (ActionHelper.GetActionCooldown(NinjaActions.TenChiJin) < 10f && elapsed < 1.45f) return No;
        return Ok;
    }

    public override PAction GetAction()
    {
        NinjaSettings.Instance.TrickAttackDelayUntilNextMug = false;
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.TrickAttackCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}
