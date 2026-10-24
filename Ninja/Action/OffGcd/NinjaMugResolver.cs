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

internal sealed class NinjaMugResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
        && Core.Target?.CanUseAttackActionOn() == true && InMeleeRange
        && ActionHelper.GetGcdRemain() >= .6f && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
        && Ready(NinjaActions.MugCurrent) ? Ok : No;

    public override PAction GetAction()
    {
        if (NinjaSettings.Instance.P5PerfectBunshin && JobGaugeHelper.NIN.Ninki >= 50)
        {
            ActionQueueManager.EnqueueOffGcdList(new List<PAction>
            {
                Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self),
                Build(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target)
            });
            return null!;
        }
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}
