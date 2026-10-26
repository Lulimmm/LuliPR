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
        && ActionHelper.GetGcdRemain() >= .6f && !NinjaCombatState.IsTcjSequenceActive
        && Ready(NinjaActions.MugCurrent) ? Ok : No;

    public override PAction GetAction()
    {
        if (NinjaSettings.Instance.P5PerfectBunshin && JobGaugeHelper.NIN.Ninki >= 50)
        {
            // AE's CurrGcdAbilityCount is 1 after a completed ninjutsu. Do
            // not enqueue Bunshin + Mug as a pair in that single weave slot;
            // submit the first action and let the next GCD reopen the queue.
            if (!NinjaCombatState.CanQueueMultipleOffGcd)
                return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin),
                    ActionType.OffGcd, ActionTargetType.Self);
            ActionQueueManager.EnqueueOffGcdList(new List<PAction>
            {
                BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self),
                BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target)
            });
            return null!;
        }
        return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}
