using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

using Ninja.PR.Action.Gcd;
using Ninja.PR.Data;
using Ninja.PR.Opener;

namespace Ninja.PR.Helper;

internal static class NinjaNoLossOpenerRuntime
{
    private enum TailStage
    {
        Inactive,
        WaitingForTcj,
        Meisui,
        FleetingRaiju,
        Bhavacakra,
        Combo3,
        TenriJindo,
        Raiton,
        FinalRaiju
    }

    private const int StageTimeoutMs = 2500;
    private static bool waiting;
    private static bool tcjSeen;
    private static bool tcjWasActive;
    private static TailStage stage;
    private static long stageStartedAt;
    private static uint stageAction;

    public static bool ShouldHoldNormalDecisions => waiting;
    public static string DebugStage => $"{stage} / waiting={waiting} / action={stageAction}";

    public static void OnBattleStarted()
    {
        waiting = NinjaSettings.Instance.Opener == NinjaOpener.NoLoss9GOpenerName;
        tcjSeen = false;
        tcjWasActive = false;
        stage = waiting ? TailStage.WaitingForTcj : TailStage.Inactive;
        stageStartedAt = 0;
        stageAction = 0;
    }

    public static void Update()
    {
        if (!waiting) return;

        var tcjActive = NinjaCombatState.IsTcjSequenceActive;
        if (!tcjSeen)
        {
            tcjSeen = tcjActive || NinjaCombatState.Recently(NinjaActions.TenChiJin, 15000);
            if (!tcjSeen) return;
        }

        if (stage == TailStage.WaitingForTcj)
        {
            if (tcjActive)
            {
                tcjWasActive = true;
                return;
            }

            if (!tcjWasActive
                && Core.Me?.HasStatus(NinjaActions.TenriJindoReady) != true
                && !NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000)) return;

            SetStage(TailStage.Meisui);
        }

        if (stageStartedAt != 0)
        {
            if (NinjaCombatState.Recently(stageAction, StageTimeoutMs))
            {
                AdvanceStage();
                return;
            }
            var timeout = stage == TailStage.Raiton ? 8000 : StageTimeoutMs;
            if (Environment.TickCount64 - stageStartedAt <= timeout) return;

            // Drop the stale command that timed out with this stage before
            // moving on, otherwise ActionUpdater can keep retrying it while
            // the runtime has already queued the next step.
            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
            AdvanceStage();
            return;
        }

        if (ActionQueueManager.HasActionsInQueue()) return;

        switch (stage)
        {
            case TailStage.Meisui: SubmitMeisui(); break;
            case TailStage.FleetingRaiju: SubmitFleetingRaiju(); break;
            case TailStage.Bhavacakra: SubmitBhavacakra(); break;
            case TailStage.Combo3: SubmitCombo3(); break;
            case TailStage.TenriJindo: SubmitTenriJindo(); break;
            case TailStage.Raiton: SubmitRaiton(); break;
            case TailStage.FinalRaiju: SubmitFinalRaiju(); break;
        }
    }

    public static void Reset()
    {
        waiting = false;
        tcjSeen = false;
        tcjWasActive = false;
        stage = TailStage.Inactive;
        stageStartedAt = 0;
        stageAction = 0;
    }

    private static void SubmitMeisui()
    {
        if (!Ready(NinjaActions.Meisui)
            || JobGaugeHelper.NIN.Ninki >= 50
            || Core.Me?.HasStatus(NinjaActions.ShadowWalkerStatus) != true
            || Core.Me.HasStatus(NinjaActions.TcjStatus)
            || !NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000)
            || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 20f)
        {
            AdvanceStage();
            return;
        }
        SubmitOffGcd(NinjaActions.Meisui, NinjaActions.Meisui, ActionTargetType.Self);
    }

    private static void SubmitFleetingRaiju()
    {
        if (Core.Me?.HasStatus(NinjaActions.RaijuReady) != true)
        {
            AdvanceStage();
            return;
        }
        SubmitGcd(NinjaActions.FleetingRaiju);
    }

    private static void SubmitBhavacakra()
    {
        if (JobGaugeHelper.NIN.Ninki < 50 || Core.Target?.CanUseAttackActionOn() != true)
        {
            AdvanceStage();
            return;
        }
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra);
        SubmitOffGcd(adjusted == 0 ? NinjaActions.Bhavacakra : adjusted,
            NinjaActions.Bhavacakra, ActionTargetType.Target);
    }

    private static void SubmitCombo3()
    {
        if (Core.Target?.CanUseAttackActionOn() != true)
        {
            AdvanceStage();
            return;
        }
        var action = new NinjaComboResolver().GetAction();
        stageAction = action.ActionId;
        ActionQueueManager.Enqueue(action);
        stageStartedAt = Environment.TickCount64;
    }

    private static void SubmitTenriJindo()
    {
        if (Core.Me?.HasStatus(NinjaActions.TenriJindoReady) != true || !Ready(NinjaActions.TenriJindo))
        {
            AdvanceStage();
            return;
        }
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.TenriJindo);
        SubmitOffGcd(adjusted == 0 ? NinjaActions.TenriJindo : adjusted,
            NinjaActions.TenriJindo, ActionTargetType.Target);
    }

    private static void SubmitRaiton()
    {
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1)
        {
            AdvanceStage();
            return;
        }

        NinjaSequencePolicy.Begin(NinjaActions.Raiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            new(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true },
            new(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true },
            new(NinjaActions.Raiton, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true }
        });
        stageAction = NinjaActions.Raiton;
        stageStartedAt = Environment.TickCount64;
    }

    private static void SubmitFinalRaiju()
    {
        if (Core.Me?.HasStatus(NinjaActions.RaijuReady) == true)
            SubmitGcd(NinjaActions.FleetingRaiju);
        else
            Finish();
    }

    private static void SubmitGcd(uint actionId)
    {
        stageAction = actionId;
        ActionQueueManager.Enqueue(new PAction(actionId, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        });
        stageStartedAt = Environment.TickCount64;
    }

    private static void SubmitOffGcd(uint actionId, uint trackedActionId, ActionTargetType target)
    {
        stageAction = trackedActionId;
        ActionQueueManager.EnqueueOffGcdList(new List<PAction>
        {
            new(actionId, ActionType.OffGcd, target) { RequiresVerification = true }
        });
        stageStartedAt = Environment.TickCount64;
    }

    private static bool Ready(uint actionId) =>
        ActionHelper.IsUnlocked(actionId) && ActionHelper.GetActionCooldown(actionId) <= 0.1f;

    private static void SetStage(TailStage next)
    {
        stage = next;
        stageStartedAt = 0;
        stageAction = 0;
    }

    private static void AdvanceStage()
    {
        SetStage(stage switch
        {
            TailStage.Meisui => TailStage.FleetingRaiju,
            TailStage.FleetingRaiju => TailStage.Bhavacakra,
            TailStage.Bhavacakra => TailStage.Combo3,
            TailStage.Combo3 => TailStage.TenriJindo,
            TailStage.TenriJindo => TailStage.Raiton,
            TailStage.Raiton => TailStage.FinalRaiju,
            TailStage.FinalRaiju => TailStage.Inactive,
            _ => TailStage.Meisui
        });
        if (stage == TailStage.Inactive) Finish();
    }

    private static void Finish()
    {
        waiting = false;
        stage = TailStage.Inactive;
        stageStartedAt = 0;
        stageAction = 0;
    }
}
