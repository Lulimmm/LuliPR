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

internal sealed class NinjaBunshinResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt(NinjaQts.Bunshin)
        && ActionHelper.GetGcdRemain() >= .3f && !NinjaCombatState.IsTcjSequenceActive
        && Ready(NinjaActions.Bunshin) && JobGaugeHelper.NIN.Ninki >= 50
        && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) >= 1.06f ? Ok : No;

    public override PAction GetAction() => Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self);
}
