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

internal sealed class NinjaPhantomResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.PhantomReady) != true
            || me.HasStatus(NinjaActions.TcjStatus)
            || NinjaCombatState.IsTcjSequenceActive
            || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.Phantom) || JobGaugeHelper.NIN.Ninki > 90) return No;

        var distance = NinjaTarget.Distance;
        if (distance > NinjaTarget.AttackRange) return Ok;
        if (NinjaSettings.Instance.PhantomOnlyForRange) return No;
        if (me.HasStatus(NinjaActions.BunshinStatus) && target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (NinjaSettings.Instance.RaitonBackstab)
            return target.HasStatus(NinjaActions.MugDebuff) && !target.HasStatus(NinjaActions.KunaisBaneStatus)
                && NinjaCombatState.Recently(NinjaActions.Raiton, 2120) ? Ok : No;
        if (target.HasStatus(NinjaActions.MugDebuff) && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus) && ActionHelper.GetActionCharges(NinjaActions.Ten) < .9f) return Ok;
        if (NinjaCombatState.Recently(NinjaActions.Bunshin, 20000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 18000)
            && !NinjaCombatState.Recently(NinjaActions.MugCurrent, 20000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1.7f) return Ok;
        return NinjaCombatState.Recently(NinjaActions.Bunshin, 22000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 22000)
            && NinjaCombatState.Recently(NinjaActions.MugCurrent, 24000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1f ? Ok : No;
    }

    // Phantom Kamaitachi is a normal GCD action. Do not hold the
    // following oGCD behind a separate UAL verification task here.
    public override PAction GetAction() => BuildNormal(NinjaActions.PhantomKamaitachi, ActionType.Gcd, ActionTargetType.Target);
}
