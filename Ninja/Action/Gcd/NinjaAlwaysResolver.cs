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

internal sealed class NinjaAlwaysResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        return PromeSettings.Instance.GetQt(NinjaQts.PrepareSuiton) ? Ok : No;
    }

    public override PAction GetAction()
    {
        NinjaSequencePolicy.Begin(NinjaActions.Suiton);
        if (ActionHelper.GetAdjustedActionId(2260u) == NinjaActions.Suiton)
            return Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
