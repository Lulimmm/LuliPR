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

internal sealed class NinjaTrueNorthResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;

    public NinjaTrueNorthResolver(Func<bool> comboIsFirst) => this.comboIsFirst = comboIsFirst;

    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.AutoTrueNorth) || me == null
            || target?.CanUseAttackActionOn() != true
            || NinjaCombatState.Recently(NinjaActions.TrueNorth, 2120)
            || me.HasStatus(1250u)
            || ActionHelper.GetActionCharges(NinjaActions.TrueNorth) < 1
            || ActionHelper.GetGcdRemain() < .6f
            || ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain() <= ActionHelper.GetGcdTotal() * .625f
            || me.HasStatus(NinjaActions.TcjStatus)
            || (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f)
            || !TargetHelper.HasPositionalRequirement(target)
            || !comboIsFirst()) return No;

        var last = ActionHelper.GetLastComboID();
        if (last != NinjaActions.GustSlash) return No;
        var positional = TargetHelper.GetTargetPositional();
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        if (kazematoi == 0 && positional != Positional.Flank) return Ok;
        if (kazematoi > 3 && positional != Positional.Rear) return Ok;
        if (positional != Positional.Flank && positional != Positional.Rear) return Ok;
        if (((ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f && kazematoi <= 1)
                || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f && kazematoi <= 1))
            && positional != Positional.Flank) return Ok;
        return No;
    }

    public override PAction GetAction() => Build(NinjaActions.TrueNorth, ActionType.OffGcd, ActionTargetType.Self);
}
