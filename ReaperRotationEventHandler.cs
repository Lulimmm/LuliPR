using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using ErosUI;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Managers.CombatEventManager;
using PromeRotation.Managers.CombatEventManager.Events;
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
    // AE's CurrGcdAbilityCount equivalent. Void/Cross Reaping consume one
    // GCD ability slot; all other confirmed self actions use the normal value.
    internal static int CurrGcdAbilityCount { get; private set; } = 2;
    private static long soulsowAttemptAt;
    private bool actionEffectAttached;

    internal void Attach()
    {
        if (actionEffectAttached) return;
        CombatEventManager.OnActionEffect += OnActionEffect;
        actionEffectAttached = true;
    }

    public void Dispose()
    {
        if (!actionEffectAttached) return;
        CombatEventManager.OnActionEffect -= OnActionEffect;
        actionEffectAttached = false;
    }

    private static void OnActionEffect(ActionEffectEvent action)
    {
        var me = Core.Me;
        if (me == null || action.SourceId != me.EntityId) return;

        CurrGcdAbilityCount = action.ActionId is R.VoidReaping or R.CrossReaping ? 1 : 2;
    }

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
        CurrGcdAbilityCount = 2;
        OpenerPositionalRuntime.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnNoTarget()
    {
        ReaperHooks.Update();
        ErosUIFramework.RestoreQtIfHostCleared();
    }
    public void OnBattleUpdate()
    {
        InBattle = true;
        OpenerPositionalRuntime.Update();
        ReaperHooks.Update();
    }
    public void OnBattleEnded()
    {
        InBattle = false;
        soulsowAttemptAt = 0;
        CurrGcdAbilityCount = 2;
        OpenerPositionalRuntime.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
        ReaperHooks.Update();
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        InBattle = false;
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
