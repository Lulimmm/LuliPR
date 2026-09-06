using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.PureTimeline;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Updaters;
using PromeRotation.UI.HotKey;

namespace Reaper.PR;

internal sealed class ReaperEventHandler : IRotationEventHandler
{
    internal static bool InBattle { get; private set; }
    private static long soulsowAttemptAt;

    public void OnUpdate() => ReaperHooks.Update();
    public void OnOutOfBattleUpdate()
    {
        InBattle = false;
        ReaperHooks.Update();
        QueueOutOfCombatSoulsow();
    }
    public void OnBattleStarted()
    {
        InBattle = false;
        soulsowAttemptAt = 0;
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnNoTarget() => ReaperHooks.Update();
    public void OnBattleUpdate()
    {
        InBattle = true;
        ReaperHooks.Update();
    }
    public void OnBattleEnded()
    {
        InBattle = false;
        soulsowAttemptAt = 0;
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        InBattle = false;
        soulsowAttemptAt = 0;
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Dispose();
    }

    private static void QueueOutOfCombatSoulsow()
    {
        // RotationManager skips all normal decisions out of combat when there
        // is no attackable target. AE handles Soulsow in its pre-combat hook,
        // so enqueue the same self-targeted action from the PR event path.
        if (PromeSettings.Instance.EnableAcr != AcrState.On
            || !R.Qt("脱战播魂种")
            || GameData.IsInCombat()
            || PromeSettings.Instance.ShouldExecuteAcr())
            return;

        var me = Core.Me;
        if (me == null
            || ((IGameObject)me).IsDead
            || me.IsCasting
            || GameData.IsPlayerOccupied()
            || ActionHelper.GetAnimationLock() > 0f
            || ActionQueueManager.HasActionsInQueue()
            || ActionUpdaterRouter.HasActiveCommand()
            || !R.Ready(R.Soulsow)
            || R.Has(2594u))
            return;

        var now = Environment.TickCount64;
        if (now - soulsowAttemptAt < 500)
            return;

        // AE's pre-combat handler calls Spell.Cast() directly. A normal PR
        // queue is not consumed while out of combat and no target is active,
        // so submit the action through the updater's direct-use path instead.
        ActionUpdaterRouter.UseAction(R.A(R.Soulsow, ActionType.Gcd, ActionTargetType.Self));
        soulsowAttemptAt = now;
    }
}
