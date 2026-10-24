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

internal sealed class NinjaOgcdResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;
    private readonly Func<bool> raijuIsFirst;

    public NinjaOgcdResolver(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        this.comboIsFirst = comboIsFirst;
        this.raijuIsFirst = raijuIsFirst;
    }

    private bool CanUseBhavacakra()
    {
        if (!NinjaBhavacakraPolicy.IsEligible() || ActionHelper.GetGcdRemain() < .6f)
            return false;

        var me = Core.Me!;
        var target = Core.Target!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        var lastCombo = ActionHelper.GetLastComboID();
        var comboFinisher = lastCombo == NinjaActions.GustSlash;
        var bunshinCooldown = ActionHelper.GetActionCooldown(NinjaActions.Bunshin);
        var bunshinActive = me.HasStatus(NinjaActions.BunshinStatus);
        if (bunshinCooldown < 20f && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (!bunshinActive)
                return (comboIsFirst() && comboFinisher && ninki > 85)
                    || (comboIsFirst() && ninki > 95)
                    || (raijuIsFirst() && ninki > 95);
            return false;
        }
        if (bunshinActive && ((comboFinisher && ninki > 80) || (comboIsFirst() && ninki > 90))) return true;
        if ((comboFinisher && ninki > 85) || (comboIsFirst() && ninki > 95)) return true;
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f
            || ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f) return false;
        return target.HasStatus(NinjaActions.KunaisBaneStatus);
    }

    public override CheckResult Check()
    {
        var me = Core.Me;
        if (me == null || Core.Target == null || ActionHelper.GetGcdRemain() < .6f
            || me.HasStatus(NinjaActions.TcjStatus)) return No;
        return CanUseBhavacakra() ? Ok : No;
    }

    public override PAction GetAction()
    {
        if (CanUseBhavacakra())
        {
            var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra);
            return Build(adjusted, ActionType.OffGcd, ActionTargetType.Target);
        }
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
    }
}
