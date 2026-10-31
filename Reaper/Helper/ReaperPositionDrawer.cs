using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Spatial.Drawing;
using PromeRotation.Spatial.Geometry;

namespace Reaper.PR;

internal static class ReaperPositionDrawer
{
    private const float SectorAngle = MathF.PI / 2f;
    private const float PositionalWindow = 500f;
    private const string FlankEastId = "luli_reaper_pos_flank_east";
    private const string FlankWestId = "luli_reaper_pos_flank_west";
    private const string RearId = "luli_reaper_pos_rear";
    private const string ProgressFlankEastId = "luli_reaper_pos_progress_flank_east";
    private const string ProgressFlankWestId = "luli_reaper_pos_progress_flank_west";
    private const string ProgressRearId = "luli_reaper_pos_progress_rear";

    private static DrawHandle flankEast;
    private static DrawHandle flankWest;
    private static DrawHandle rear;
    private static DrawHandle progressFlankEast;
    private static DrawHandle progressFlankWest;
    private static DrawHandle progressRear;
    private static uint targetId;
    private static Positional drawnPosition;
    private static float drawnRadius;

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

    internal static void Update()
    {
        var drawManager = PromeRotation.Plugin.Instance?.DrawManager;
        if (drawManager == null)
        {
            ResetState();
            return;
        }

        var me = Core.Me;
        var target = Core.Target;
        if (me == null
            || target == null
            || !TargetHelper.HasPositionalRequirement(target)
            || R.Has(1250u)
            || !(R.Has(2587u) || R.Has(3858u)))
        {
            Clear(drawManager);
            return;
        }

        if (RecentlyUsedPositionalAction())
        {
            Clear(drawManager);
            return;
        }

        var requiredPosition = R.Has(2589u)
            ? Positional.Rear
            : R.Has(2588u) ? Positional.Flank : Positional.None;
        if (requiredPosition == Positional.None)
        {
            Clear(drawManager);
            return;
        }

        var radius = MathF.Max(3f, target.HitboxRadius + 3f);
        if (target.EntityId != targetId
            || requiredPosition != drawnPosition
            || MathF.Abs(radius - drawnRadius) > 0.05f
            || !HasStaticDrawings())
        {
            Clear(drawManager);
            CreateStaticDrawings(drawManager, target, requiredPosition, radius);
        }

        var style = IsOppositePosition(requiredPosition) ? DangerStyle : SafeStyle;
        var progressStyle = IsOppositePosition(requiredPosition) ? DangerProgressStyle : SafeProgressStyle;
        UpdateStaticStyle(drawManager, style);

        var totalGcd = MathF.Max(0f, ActionHelper.GetGcdTotal());
        var remainGcd = Math.Clamp(ActionHelper.GetGcdRemain(), 0f, totalGcd);
        var remainingProgress = totalGcd <= 0.01f ? 0f : remainGcd / totalGcd;
        var progressRadius = MathF.Max(0.05f, radius * remainingProgress);
        UpdateProgressDrawings(drawManager, target, requiredPosition, radius, progressRadius, progressStyle, remainingProgress > 0f);
    }

    internal static void Clear()
    {
        var drawManager = PromeRotation.Plugin.Instance?.DrawManager;
        if (drawManager != null) Clear(drawManager);
        else ResetState();
    }

    private static bool RecentlyUsedPositionalAction()
    {
        var gibbet = R.Adjust(R.Gibbet);
        var gallows = R.Adjust(R.Gallows);
        return ReaperEventHandler.RecentlyConfirmed(R.Gibbet, (int)PositionalWindow)
            || ReaperEventHandler.RecentlyConfirmed(R.Gallows, (int)PositionalWindow)
            || ReaperEventHandler.RecentlyConfirmed(gibbet, (int)PositionalWindow)
            || ReaperEventHandler.RecentlyConfirmed(gallows, (int)PositionalWindow)
            || R.Recently(R.Gibbet, (int)PositionalWindow)
            || R.Recently(R.Gallows, (int)PositionalWindow)
            || R.Recently(gibbet, (int)PositionalWindow)
            || R.Recently(gallows, (int)PositionalWindow);
    }

    private static void CreateStaticDrawings(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        IBattleChara target,
        Positional requiredPosition,
        float radius)
    {
        var style = IsOppositePosition(requiredPosition) ? DangerStyle : SafeStyle;
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

    private static void UpdateStaticStyle(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        DrawStyle style)
    {
        if (flankEast.IsValid) drawManager.Update(flankEast, style: style);
        if (flankWest.IsValid) drawManager.Update(flankWest, style: style);
        if (rear.IsValid) drawManager.Update(rear, style: style);
    }

    private static void UpdateProgressDrawings(
        PromeRotation.Spatial.Drawing.DrawManager drawManager,
        IBattleChara target,
        Positional requiredPosition,
        float radius,
        float progressRadius,
        DrawStyle style,
        bool visible)
    {
        if (requiredPosition == Positional.Flank)
        {
            progressFlankEast = UpdateProgressSector(
                drawManager, progressFlankEast, ProgressFlankEastId, target,
                MathF.PI / 2f, radius, progressRadius, style, visible);
            progressFlankWest = UpdateProgressSector(
                drawManager, progressFlankWest, ProgressFlankWestId, target,
                MathF.PI * 1.5f, radius, progressRadius, style, visible);
            Remove(drawManager, ref progressRear);
        }
        else
        {
            progressRear = UpdateProgressSector(
                drawManager, progressRear, ProgressRearId, target,
                MathF.PI, radius, progressRadius, style, visible);
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
        => drawManager.AddBound(
            id,
            new FanGeometry(target.Position, target.Rotation + rotationOffset, radius, SectorAngle),
            DrawBinding.Follow(target.EntityId, rotationOffset),
            style);

    private static bool IsOppositePosition(Positional requiredPosition)
    {
        var current = TargetHelper.GetTargetPositional();
        return (requiredPosition == Positional.Flank && current == Positional.Rear)
            || (requiredPosition == Positional.Rear && current == Positional.Flank);
    }

    private static bool HasStaticDrawings() => flankEast.IsValid || flankWest.IsValid || rear.IsValid;

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
