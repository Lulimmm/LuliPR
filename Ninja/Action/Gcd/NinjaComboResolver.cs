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

internal sealed class NinjaComboResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        return HasMeleeTarget && me != null && !me.HasStatus(NinjaActions.RaijuReady)
            && !NinjaCombatState.IsTcjSequenceActive ? Ok : No;
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
        else if (last == NinjaActions.GustSlash && me.Level >= 26)
        {
            if (me.Level < 54) action = NinjaActions.AeolianEdge;
            else if ((PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) || PromeSettings.Instance.GetQt(NinjaQts.BigTrick))
                && ((ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f && kazematoi <= 1)
                    || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f && kazematoi <= 1))) action = NinjaActions.ArmorCrush;
            else if (target.HasStatus(NinjaActions.KunaisBaneStatus) && kazematoi != 0) action = NinjaActions.AeolianEdge;
            else if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge && me.Level >= 4 ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }
}
