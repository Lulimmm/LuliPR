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

internal sealed class NinjaKBaseResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!NinjaSettings.Instance.RaitonBackstab
            || me == null || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || NinjaCombatState.IsTcjSequenceActive) return No;

        return (target.HasStatus(NinjaActions.KunaisBaneStatus)
                || NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 1060))
            && ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f
            && !target.HasStatus(NinjaActions.MugDebuff) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var last = ActionHelper.GetLastComboID();
        var me = Core.Me!;
        var target = Core.Target!;
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        var positional = TargetHelper.GetTargetPositional();
        uint action;

        if (last == NinjaActions.GustSlash && !TargetHelper.HasPositionalRequirement(target))
            action = kazematoi == 0 ? NinjaActions.ArmorCrush : NinjaActions.AeolianEdge;
        else if (last == NinjaActions.GustSlash)
        {
            if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }
}
