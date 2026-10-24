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

internal sealed class NinjaAssassinateResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt("梦幻三段")
        && Core.Target?.CanUseAttackActionOn() == true && InMeleeRange
        && ActionHelper.GetGcdRemain() >= .6f && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
        && ActionHelper.IsUnlocked(NinjaActions.Assassinate)
        && ActionHelper.GetActionCooldown(NinjaActions.AssassinateCooldown) <= 0f
        && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) >= 2.12f
        && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) >= 2.12f
        && !NinjaCombatState.IsTcjSequenceActive ? Ok : No;

    public override PAction GetAction()
    {
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Assassinate);
        return Build(adjusted == 0 ? NinjaActions.Assassinate : adjusted,
            ActionType.OffGcd, ActionTargetType.Target);
    }
}
