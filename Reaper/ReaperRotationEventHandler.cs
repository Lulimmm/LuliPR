using System;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using LuliPR.Common;
using MewUI;
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

internal sealed class ReaperEventHandler : IRotationEventHandler, IDisposable
{
    internal static bool InBattle { get; private set; }
    private static long soulsowAttemptAt;
    private ActionFlowManager? actionFlowManager;
    private bool actionFlowAttached;

    internal void Attach()
    {
        if (actionFlowAttached) return;
        var plugin = PromeRotation.Plugin.Instance;
        var managerField = typeof(PromeRotation.Plugin).GetField("_actionFlowManager", BindingFlags.Instance | BindingFlags.NonPublic);
        actionFlowManager = plugin == null ? null : managerField?.GetValue(plugin) as ActionFlowManager;
        if (actionFlowManager != null)
            actionFlowManager.OnAction += OnActionFlow;
        else
            Svc.Log.Warning("Reaper: unable to access PR ActionFlowManager; RecentlyUsed tracking is unavailable.");
        actionFlowAttached = true;
    }

    public void Dispose()
    {
        if (!actionFlowAttached) return;
        if (actionFlowManager != null)
            actionFlowManager.OnAction -= OnActionFlow;
        actionFlowManager = null;
        actionFlowAttached = false;
    }

    private static void OnActionFlow(ActionFlowManager.ActionEvent action)
    {
        if (action.Accepted)
            ReaperSpellHistory.RecordAction(action.ActionId);
    }

    public void OnUpdate()
    {
        BundledTimelineSync.UpdateLoginSession();
        UsageStatistics.Update();
        ReaperHooks.Update();
    }
    public void OnOutOfBattleUpdate()
    {
        BundledTimelineSync.UpdateLoginSession();
        UsageStatistics.Update();
        InBattle = false;
        ReaperPositionDrawer.Clear();
        ReaperHooks.Update();
        QueueOutOfCombatSoulsow();
    }
    public void OnBattleStarted()
    {
        InBattle = false;
        ReaperPositionDrawer.Clear();
        soulsowAttemptAt = 0;
        OpenerPositionalRuntime.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnNoTarget()
    {
        ReaperPositionDrawer.Clear();
        ReaperHooks.Update();
        MewUIFramework.RestoreQtIfHostCleared();
    }
    public void OnBattleUpdate()
    {
        BundledTimelineSync.UpdateLoginSession();
        UsageStatistics.Update();
        InBattle = true;
        OpenerPositionalRuntime.Update();
        ReaperHooks.Update();
        ReaperPositionDrawer.Update();
    }
    public void OnBattleEnded()
    {
        InBattle = false;
        ReaperPositionDrawer.Clear();
        soulsowAttemptAt = 0;
        OpenerPositionalRuntime.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        InBattle = false;
        ReaperPositionDrawer.Clear();
        soulsowAttemptAt = 0;
        OpenerPositionalRuntime.Reset();
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
