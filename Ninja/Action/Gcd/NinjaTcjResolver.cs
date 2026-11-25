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
        if (Core.Target?.CanUseAttackActionOn() != true) return Blocked("天地人后续忍术需要可攻击目标，当前目标不可用");
        if (!NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人状态（1186）不存在，不评估天地人后续忍术");

        // 1186 owns the sequence window. Once the client removes it, TCJ is
        // complete and the normal resolvers, including 36961, may run.
        return GetNextTcjAction() == 0 ? Blocked("天地人水遁已被选择/确认，等待天地人状态消失后交回普通循环") : Ok;
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
