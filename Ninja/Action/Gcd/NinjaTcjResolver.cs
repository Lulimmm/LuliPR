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

internal sealed class NinjaTcjResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (Core.Target?.CanUseAttackActionOn() != true
            || !NinjaCombatState.IsTcjSequenceActive)
            return No;

        // 1186 owns the sequence window. Once the client removes it, TCJ is
        // complete and the normal resolvers, including 36961, may run.
        return GetNextTcjAction() == 0 ? No : Ok;
    }
    public override PAction GetAction()
    {
        var id = GetNextTcjAction();
        if (id == 0) id = NinjaActions.TcjSuiton;
        NinjaCombatState.MarkTcjSubmitted(id);
        NinjaSequencePolicy.Begin(NinjaActions.TcjSuiton);
        // Submit the concrete follow-up spell directly. These
        // IDs are already the transformed actions, so resolving them again
        // through ActionManager can collapse the sequence back to Fuma.
        var action = Build(id, ActionType.Gcd, ActionTargetType.Target);
        // The final TCJ Suiton packet is not emitted consistently by every
        // client path. Waiting for UAL/AEP verification of 18881 can leave
        // ActionUpdater in a permanent pending-verification state after the
        // three TCJ actions are visibly complete. Submission is sufficient;
        // 1186 removal and the recorded TCJ step hand off to the opener tail.
        if (id == NinjaActions.TcjSuiton)
            action.RequiresVerification = false;
        return action;
    }

    private static uint GetNextTcjAction()
    {
        return NinjaCombatState.GetTcjStepForDecision() switch
        {
            NinjaActions.TcjFumaShuriken => NinjaActions.TcjRaiton,
            NinjaActions.TcjRaiton => NinjaActions.TcjSuiton,
            NinjaActions.TcjSuiton => 0,
            _ => NinjaActions.TcjFumaShuriken
        };
    }
}
