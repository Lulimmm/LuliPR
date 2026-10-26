using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

using Ninja.PR.Action.Gcd;
using Ninja.PR.Action.OffGcd;
using Ninja.PR.Data;
using Ninja.PR.Opener;

namespace Ninja.PR.Helper;

/// <summary>
/// Hands the no-loss opener back to the normal PR queue after TCJ.
///
/// The regular opener is a fixed action list. Keeping a second per-action
/// state machine here made the scheduler hold a GCD while it was waiting for
/// an oGCD, which is the short pause seen immediately after the third TCJ
/// jutsu. Build the remaining opener tail once, just like an IOpener sequence,
/// and let normal resolvers handle anything that is not currently available.
/// </summary>
internal static class NinjaNoLossOpenerRuntime
{
    private enum TailStage
    {
        Inactive,
        WaitingForTcj,
        HandedToQueue
    }

    private static bool waiting;
    private static bool tcjWasActive;
    private static TailStage stage;
    private static string lastTransition = "未激活";

    public static bool ShouldHoldNormalDecisions => waiting;
    public static string DebugStage => $"{FormatStage()} / 保持={waiting} / {lastTransition}";

    public static void OnBattleStarted()
    {
        Reset();
        // RotationManager has already marked the selected opener as executed
        // when the battle callback is raised. Do not activate this bridge for
        // a normal rotation or for a stale setting outside the opener queue.
        waiting = PromeSettings.Instance.OpenerHasBeenExecuted
            && NinjaSettings.Instance.Opener == NinjaOpener.NoLoss9GOpenerName;
        stage = waiting ? TailStage.WaitingForTcj : TailStage.Inactive;
    }

    public static void Update()
    {
        if (!waiting || stage != TailStage.WaitingForTcj) return;

        var tcjActive = NinjaCombatState.HasTcjBuff;
        tcjWasActive |= tcjActive;
        if (tcjActive) return;
        if (!tcjWasActive)
        {
            if (!HasHostWork())
            {
                waiting = false;
                stage = TailStage.Inactive;
                lastTransition = "天地人未开始，已恢复常规循环";
            }
            return;
        }

        // The final TCJ effect and the updater command can be reported on
        // adjacent frames. Let the existing command finish before appending
        // the tail, otherwise the first tail action competes with 7403.
        if (ActionQueueManager.HasActionsInQueue()
            || ActionUpdaterRouter.HasActiveCommand()
            || ActionUpdaterRouter.HasLockedGcdAction()) return;

        QueueTail();
        waiting = false;
        stage = TailStage.HandedToQueue;
        lastTransition = "天地人完成，已加入起手尾段队列";
    }

    public static void Reset()
    {
        waiting = false;
        tcjWasActive = false;
        stage = TailStage.Inactive;
        lastTransition = "未激活";
    }

    private static void QueueTail()
    {
        var tail = new List<PAction>();
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return;

        // Preserve the regular opener priority. Every action is checked at
        // handoff; unavailable actions are left to the normal resolver list.
        var meisui = new NinjaMeisuiResolver();
        if (meisui.Check().Success)
            tail.Add(meisui.GetAction());

        if (me.HasStatus(NinjaActions.RaijuReady))
            tail.Add(G(NinjaActions.FleetingRaiju));

        if (NinjaBhavacakraPolicy.IsEligible())
        {
            var bhavacakra = ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra);
            tail.Add(O(bhavacakra == 0 ? NinjaActions.Bhavacakra : bhavacakra));
        }

        // Combo3 is intentionally resolved at handoff so positional and
        // Kazematoi decisions match the ordinary combo resolver.
        tail.Add(new NinjaComboResolver().GetAction());

        var tenri = new NinjaTenriJindoResolver();
        if (tenri.Check().Success)
            tail.Add(tenri.GetAction());

        // Reuse the normal verified mudra group. This is the same sequence
        // that the regular Raiton resolver queues, without re-entering the
        // opener runtime or holding the next GCD.
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) >= 1)
        {
            NinjaSequencePolicy.Begin(NinjaActions.Raiton);
            tail.Add(M(NinjaActions.Ten));
            tail.Add(M(NinjaActions.ChiCombo));
            tail.Add(G(NinjaActions.Raiton));
        }

        if (tail.Count > 0)
            ActionQueueManager.Enqueue(tail);
    }

    private static bool HasHostWork() => ActionQueueManager.HasActionsInQueue()
        || ActionUpdaterRouter.HasActiveCommand()
        || ActionUpdaterRouter.HasLockedGcdAction();

    private static string FormatStage() => stage switch
    {
        TailStage.WaitingForTcj => "等待天地人完成",
        TailStage.HandedToQueue => "尾段已加入队列",
        _ => "未激活"
    };

    private static PAction G(uint id) =>
        new(id, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true };

    private static PAction M(uint id) =>
        new(id, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true };

    private static PAction O(uint id) =>
        new(id, ActionType.OffGcd, ActionTargetType.Target) { RequiresVerification = true };
}
