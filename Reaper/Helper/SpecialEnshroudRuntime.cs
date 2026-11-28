using System;
using Dalamud.Game.ClientState.Objects.Types;
using LuliPR.Common;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;
using PromeRotation.Updaters;
using NativeActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using NativeActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Reaper.PR;

internal static class SpecialEnshroudRuntime
{
    private static bool? enabledOverride;
    private static int? layerOverride;
    private static long forcedRetryAfter;
    internal static bool Enabled => enabledOverride ?? AcrSettingsStore.Current.Reaper.SpecialEnshroudCommunioEnabled;
    internal static int Layer => Math.Clamp(layerOverride ?? AcrSettingsStore.Current.Reaper.SpecialEnshroudCommunioLayer, 1, 5);
    internal static bool HasOverride => enabledOverride.HasValue || layerOverride.HasValue;
    internal static bool IsSpecial { get; private set; }
    internal static bool CommunioOnly { get; private set; }

    internal static void Update()
    {
        var me = Core.Me;
        if (me == null || ((IGameObject)me).IsDead || !R.Has(2593u))
        {
            IsSpecial = false;
            CommunioOnly = false;
            return;
        }

        IsSpecial |= R.Has(3859u);
        if (!Enabled)
        {
            CommunioOnly = false;
            return;
        }
        var current = JobGaugeHelper.RPR.夜游魂;
        if (IsSpecial && current > 0 && current <= Layer)
            CommunioOnly = true;
    }

    internal static void ApplyCombatSettings(bool enabled, int layer)
    {
        var settings = AcrSettingsStore.Current.Reaper;
        settings.SpecialEnshroudCommunioEnabled = enabled;
        settings.SpecialEnshroudCommunioLayer = Math.Clamp(layer, 1, 5);
        enabledOverride = null;
        layerOverride = null;
        Update();
        AcrSettingsStore.Save();
    }

    internal static void SetTimelineSettings(bool enabled, int layer)
    {
        if (enabled && layer is < 1 or > 5) return;
        enabledOverride = enabled;
        if (enabled) layerOverride = layer;
        Update();
    }

    internal static void Reset()
    {
        enabledOverride = null;
        layerOverride = null;
        forcedRetryAfter = 0;
        IsSpecial = false;
        CommunioOnly = false;
    }

    internal static bool CanUseCommunio(bool force = false)
    {
        Update();
        if (!R.Has(2593u) || R.Has(2587u) || R.Has(3858u) || R.Has(3860u)) return false;
        var current = JobGaugeHelper.RPR.夜游魂;
        var selected = Enabled && IsSpecial ? CommunioOnly : current == 1;
        return current > 0 && (force || selected) && ActionHelper.IsUnlocked(R.Communio);
    }

    internal static unsafe PAction? GetCommunioAction(bool force = false)
    {
        var me = Core.Me;
        var target = R.Target;
        if (!CanUseCommunio(force) || me == null || ((IGameObject)me).IsDead
            || me.IsCasting || MoveManager.IsLocalPlayerMoving || !R.Targetable
            || target == null || !target.IsTargetable || R.Recently(R.Communio, 1500)) return null;
        var manager = NativeActionManager.Instance();
        var source = (NativeGameObject*)me.Address;
        var destination = (NativeGameObject*)target.Address;
        if (manager == null || source == null || destination == null
            || !NativeActionManager.CanUseActionOnTarget(R.Communio, destination)
            || manager->GetActionStatus(NativeActionType.Action, R.Communio, target.EntityId,
                checkRecastActive: false, checkCastingActive: false) != 0
            || NativeActionManager.GetActionInRangeOrLoS(R.Communio, source, destination) != 0) return null;
        return new PAction(R.Communio, ActionType.Gcd, ActionTargetType.Target)
        {
            NetworkTid = target.EntityId,
            RequiresVerification = true
        };
    }

    internal static bool TryQueueForcedCommunio()
    {
        var now = Environment.TickCount64;
        if (now < forcedRetryAfter) return false;
        var action = GetCommunioAction(force: true);
        if (action == null || !ActionUpdaterRouter.TryEnqueueForcedAction(action)) return false;
        forcedRetryAfter = now + Math.Max(0, R.GetGcdRemainingMs()) + 1500;
        return true;
    }
}

internal sealed class ForcedCommunioHotkey : IHotkeyLogic
{
    public bool IsActive() => false;
    public void OnClick() => SpecialEnshroudRuntime.TryQueueForcedCommunio();
}
