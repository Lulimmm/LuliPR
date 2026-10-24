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

internal static class NinjaBhavacakraPolicy
{
    public static bool IsEligible()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra) || me == null
            || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || me.HasStatus(NinjaActions.TcjStatus) || JobGaugeHelper.NIN.Ninki < 50
            || NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960, 1060)
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)
            || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
                && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) && me.HasStatus(NinjaActions.ShadowWalkerStatus)))
            return false;

        return true;
    }

    public static bool ShouldForceSpend(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        if (!IsEligible()) return false;
        var me = Core.Me!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return true;

        if (ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 20f
            && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (me.HasStatus(NinjaActions.BunshinStatus)) return false;
            var comboFinisher = ActionHelper.GetLastComboID() == NinjaActions.GustSlash;
            return (comboIsFirst() && ((comboFinisher && ninki > 85) || ninki > 95))
                || (raijuIsFirst() && ninki > 95);
        }

        if (ninki == 100) return true;
        return !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && ninki > 60;
    }
}
