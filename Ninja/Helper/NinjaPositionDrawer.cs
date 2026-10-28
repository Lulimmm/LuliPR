using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Spatial.Drawing;
using PromeRotation.Spatial.Geometry;
using PromeRotation.Updaters;

using Ninja.PR.Action.Gcd;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

// PR rendering adapter for AE/UMP's Ninja PosHelper. The decision policy is
// intentionally equivalent to UMP.Ninja.PosHelper; only the draw backend is
// different.
internal static class NinjaPositionDrawer
{
    private const float SectorAngle = MathF.PI / 2f;
    private const float PositionRadius = 3.0f;
    private const string ResourceQt = "倾斜资源";
    private const string FlankEastId = "luli_ninja_pos_flank_east";
    private const string FlankWestId = "luli_ninja_pos_flank_west";
    private const string RearId = "luli_ninja_pos_rear";
    private const string ProgressFlankEastId = "luli_ninja_pos_progress_flank_east";
    private const string ProgressFlankWestId = "luli_ninja_pos_progress_flank_west";
    private const string ProgressRearId = "luli_ninja_pos_progress_rear";

    private static DrawHandle flankEast;
    private static DrawHandle flankWest;
    private static DrawHandle rear;
    private static DrawHandle progressFlankEast;
    private static DrawHandle progressFlankWest;
    private static DrawHandle progressRear;
    private static uint targetId;
    private static Positional drawnPosition;
    private static float drawnRadius;
    private static bool openerPending;
    private static bool openerWorkObserved;
    private static bool battleActive;

    private static readonly DrawStyle SafeStyle = new(
        new Vector4(0.18f, 0.90f, 0.52f, 0.08f),
        new Vector4(0.25f, 1.00f, 0.62f, 0.78f),
        1.35f);

    private static readonly DrawStyle DangerStyle = new(
        new Vector4(1.00f, 0.28f, 0.24f, 0.08f),
        new Vector4(1.00f, 0.48f, 0.38f, 0.82f),
        1.35f);

    private static readonly DrawStyle SafeProgressStyle = new(
        new Vector4(0.18f, 0.90f, 0.52f, 0.025f),
        new Vector4(0.25f, 1.00f, 0.62f, 0.48f),
        1.0f);

    private static readonly DrawStyle DangerProgressStyle = new(
        new Vector4(1.00f, 0.28f, 0.24f, 0.025f),
        new Vector4(1.00f, 0.48f, 0.38f, 0.52f),
        1.0f);

    public static void OnBattleStarted()
    {
        // RotationManager creates the opener queue before this callback.
        openerPending = PromeSettings.Instance.OpenerHasBeenExecuted
            && NinjaRotation.Openers.ContainsKey(NinjaSettings.Instance.Opener);
        openerWorkObserved = !openerPending;
        battleActive = true;
        Clear();
    }

    public static void ResetLifecycle()
    {
        openerPending = false;
        openerWorkObserved = false;
        battleActive = false;
        Clear();
    }

    public static void Update()
    {
        if (!battleActive)
        {
            Clear();
            return;
        }

        var drawManager = PromeRotation.Plugin.Instance?.DrawManager;
        if (drawManager == null)
        {
            ResetState();
            return;
        }

        var me = Core.Me;
        var target = Core.Target;
        if (!NinjaSettings.Instance.Positional
            || me == null
            || target?.CanUseAttackActionOn() != true
            || !TargetHelper.HasPositionalRequirement(target))
        {
            Clear(drawManager);
            return;
        }

        if (!IsOpenerComplete() || !ShouldDrawUmpPosition(me, target))
        {
            Clear(drawManager);
            return;
        }

        var requiredPosition = ResolveUmpPosition();
        if (requiredPosition == Positional.None)
        {
            Clear(drawManager);
            return;
        }

        var radius = MathF.Max(PositionRadius, target.HitboxRadius + PositionRadius);
        if (target.EntityId != targetId
            || requiredPosition != drawnPosition
            || MathF.Abs(radius - drawnRadius) > 0.05f
            || !HasStaticDrawings())
        {
            Clear(drawManager);
            CreateStaticDrawings(drawManager, target, requiredPosition, radius);
        }

        UpdateProgressDrawings(drawManager, target, requiredPosition, radius);
    }

    public static void Clear()
    {
        var drawManager = PromeRotation.Plugin.Instance?.DrawManager;
        if (drawManager != null)
            Clear(drawManager);
        else
            ResetState();
    }

    private static bool ShouldDrawUmpPosition(IBattleChara me, IBattleChara target)
    {
        // Equivalent to Ninja_GCD_BaseCombo.Check() >= 0 and UMP's
        // last-combo/positional checks.
        if (!new NinjaComboResolver().Check().Success
            || ActionHelper.GetLastComboID() != NinjaActions.GustSlash)
            return false;

        // Equivalent to UMP's PreNinjutsu, TenChiJin, Hyosho, FleetingRaiju,
        // Suiton, PhantomKamaitachi and Raiton exclusions.
        if (me.HasStatus(NinjaActions.MudraStatus)
            || NinjaCombatState.IsTcjSequenceActive
            || NinjaSequencePolicy.IsNormalMudraActive
            || new NinjaAlwaysResolver().Check().Success
            || new NinjaTcjResolver().Check().Success
            || new NinjaKassatsuJutsuResolver().Check().Success
            || new NinjaRaijuResolver().Check().Success
            || new NinjaJutsuResolver().Check().Success
            || new NinjaPhantomResolver().Check().Success
            || new NinjaRaitonResolver().Check().Success)
            return false;

        return TargetHelper.HasPositionalRequirement(target);
    }

    private static Positional ResolveUmpPosition()
    {
        var retainResources = PromeSettings.Instance.GetQt(ResourceQt);
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;

        // UMP's exact policy: 0 stacks or resource-retention mode uses
        // Armor Crush (flank); four or more uses Aeolian Edge (rear).
        if (kazematoi == 0 || retainResources)
            return Positional.Flank;
        if (kazematoi >= 4 && !retainResources)
            return Positional.Rear;
        return Positional.None;
    }

    private static void CreateStaticDrawings(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        IBattleChara target,
        Positional requiredPosition,
        float radius)
    {
        var style = GetStaticStyle(requiredPosition);
        if (requiredPosition == Positional.Flank)
        {
            flankEast = AddSector(drawManager, FlankEastId, target, MathF.PI / 2f, radius, style);
            flankWest = AddSector(drawManager, FlankWestId, target, MathF.PI * 1.5f, radius, style);
        }
        else
        {
            rear = AddSector(drawManager, RearId, target, MathF.PI, radius, style);
        }

        targetId = target.EntityId;
        drawnPosition = requiredPosition;
        drawnRadius = radius;
    }

    private static void UpdateProgressDrawings(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        IBattleChara target,
        Positional requiredPosition,
        float radius)
    {
        var total = NinjaGcdTiming.TotalSeconds;
        var progress = total <= 0.01f
            ? 0f
            : Math.Clamp(NinjaGcdTiming.ElapsedSeconds / total, 0f, 1f);
        var style = GetProgressStyle(requiredPosition);
        var progressRadius = MathF.Max(0.05f, radius * progress);

        if (requiredPosition == Positional.Flank)
        {
            progressFlankEast = UpdateProgressSector(
                drawManager, progressFlankEast, ProgressFlankEastId, target,
                MathF.PI / 2f, radius, progressRadius, style, progress > 0f);
            progressFlankWest = UpdateProgressSector(
                drawManager, progressFlankWest, ProgressFlankWestId, target,
                MathF.PI * 1.5f, radius, progressRadius, style, progress > 0f);
            Remove(drawManager, ref progressRear);
        }
        else
        {
            progressRear = UpdateProgressSector(
                drawManager, progressRear, ProgressRearId, target,
                MathF.PI, radius, progressRadius, style, progress > 0f);
            Remove(drawManager, ref progressFlankEast);
            Remove(drawManager, ref progressFlankWest);
        }
    }

    private static DrawHandle UpdateProgressSector(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        DrawHandle handle,
        string id,
        IBattleChara target,
        float rotationOffset,
        float fullRadius,
        float progressRadius,
        DrawStyle style,
        bool visible)
    {
        if (!visible)
        {
            Remove(drawManager, ref handle);
            return default;
        }

        var geometry = new FanGeometry(
            target.Position,
            target.Rotation + rotationOffset,
            MathF.Min(fullRadius, progressRadius),
            SectorAngle);
        var binding = DrawBinding.Follow(target.EntityId, rotationOffset);
        if (!handle.IsValid)
            return drawManager.AddBound(id, geometry, binding, style);

        drawManager.Update(handle, geometry: geometry, style: style);
        return handle;
    }

    private static DrawHandle AddSector(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        string id,
        IBattleChara target,
        float rotationOffset,
        float radius,
        DrawStyle style)
    {
        var binding = DrawBinding.Follow(target.EntityId, rotationOffset);
        return drawManager.AddBound(
            id,
            new FanGeometry(target.Position, target.Rotation + rotationOffset, radius, SectorAngle),
            binding,
            style);
    }

    private static bool IsOppositePosition(Positional requiredPosition)
    {
        var current = TargetHelper.GetTargetPositional();
        return (requiredPosition == Positional.Flank && current == Positional.Rear)
            || (requiredPosition == Positional.Rear && current == Positional.Flank);
    }

    private static DrawStyle GetStaticStyle(Positional requiredPosition)
        => IsOppositePosition(requiredPosition) ? DangerStyle : SafeStyle;

    private static DrawStyle GetProgressStyle(Positional requiredPosition)
        => IsOppositePosition(requiredPosition) ? DangerProgressStyle : SafeProgressStyle;

    private static bool IsOpenerComplete()
    {
        if (!openerPending) return true;

        var openerBusy = NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions
            || NinjaSequencePolicy.IsActive
            || NinjaCombatState.IsTcjSequenceActive
            || ActionQueueManager.HasActionsInQueue()
            || ActionUpdaterRouter.HasActiveCommand()
            || ActionUpdaterRouter.HasLockedGcdAction();
        if (openerBusy)
        {
            openerWorkObserved = true;
            return false;
        }

        if (NinjaCombatState.LastGcd != 0)
            openerWorkObserved = true;
        if (!openerWorkObserved)
            return false;

        openerPending = false;
        return true;
    }

    private static bool HasStaticDrawings()
        => flankEast.IsValid || flankWest.IsValid || rear.IsValid;

    private static void Clear(PromeRotation.Spatial.Drawing.DrawManager drawManager)
    {
        Remove(drawManager, ref flankEast);
        Remove(drawManager, ref flankWest);
        Remove(drawManager, ref rear);
        Remove(drawManager, ref progressFlankEast);
        Remove(drawManager, ref progressFlankWest);
        Remove(drawManager, ref progressRear);
        ResetState();
    }

    private static void Remove(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        ref DrawHandle handle)
    {
        if (handle.IsValid)
        {
            try { drawManager.Remove(handle); }
            catch { }
        }
        handle = default;
    }

    private static void ResetState()
    {
        flankEast = default;
        flankWest = default;
        rear = default;
        progressFlankEast = default;
        progressFlankWest = default;
        progressRear = default;
        targetId = 0;
        drawnPosition = Positional.None;
        drawnRadius = 0f;
    }
}
