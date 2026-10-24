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
        if (!PromeSettings.Instance.GetQt(NinjaQts.PrepareSuiton)) return No;

        // A stale pre-pull flag must never take precedence over the TCJ GCD
        // sequence. The TCJ resolver owns every GCD while the 1186 aura is
        // active; otherwise the prep queue (2259/18806/18807/2271) can be
        // selected first and consume the window without submitting 18873.
        if (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true) return No;

        return Ok;
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
