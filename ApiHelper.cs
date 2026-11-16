using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FfxivActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using FfxivActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;
using FfxivGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
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

internal static class R
{
    public const uint Slice = 24373u, WaxingSlice = 24374u, InfernalSlice = 24375u;
    public const uint SpinningScythe = 24376u, NightmareScythe = 24377u, ShadowOfDeath = 24378u;
    public const uint WhorlOfDeath = 24379u, SoulSlice = 24380u, SoulScythe = 24381u;
    public const uint Gibbet = 24382u, Gallows = 24383u, PlentifulHarvest = 24385u, Harpe = 24386u;
    public const uint Soulsow = 24387u, HarvestMoon = 24388u, BloodStalk = 24389u;
    public const uint Gluttony = 24393u, Enshroud = 24394u, VoidReaping = 24395u, CrossReaping = 24396u;
    public const uint Communio = 24398u, LemuresSlice = 24399u, LemuresScythe = 24400u, Ingress = 24401u, ArcaneCircle = 24405u;
    public const uint TrueNorth = 7546u, Sacrificium = 36969u, Perfectio = 36973u, Potion = 846u;
    public const uint EnhancedGibbet = 36970u, EnhancedGallows = 36971u;
    private static IBattleChara[] nearbyAttackableEnemies = [];
    private static ulong nearbyEnemySourceId;
    private static long nearbyEnemyCacheAt;

    public static IBattleChara? Target => Core.Target;
    public static bool Recently(uint actionId, int spanMs = 1200)
        => ReaperSpellHistory.RecentlyUsed(actionId, spanMs);
    public static bool Has(uint id) => Core.Me?.HasStatus(id) == true;
    public static bool HasFor(uint id, float seconds) => Core.Me?.HasStatus(id) == true && Core.Me.GetStatusLeftTime(id) >= seconds;
    public static bool TargetHas(uint id, float seconds = 0)
    {
        var target = Target;
        var me = Core.Me;
        if (target == null || me == null) return false;
        foreach (var status in target.StatusList)
            if (status.StatusId == id && status.SourceId == me.EntityId && status.RemainingTime >= seconds)
                return true;
        return false;
    }
    // Match AE's HasMyAuraWithTimeleft: truncate the absolute remaining time to milliseconds.
    public static bool TargetHasForMs(uint id, int milliseconds)
    {
        var target = Target;
        var me = Core.Me;
        if (target == null || me == null) return false;
        foreach (var status in target.StatusList)
            if (status.StatusId == id && status.SourceId == me.EntityId)
                return (int)(Math.Abs(status.RemainingTime) * 1000f) >= milliseconds;
        return 0 >= milliseconds;
    }
    public static int GetGcdDurationMs() => (int)Math.Max(0f, ActionHelper.GetGcdTotal() * 1000f);
    public static int GetGcdRemainingMs()
        => GetGcdDurationMs() - (int)Math.Max(0f, ActionHelper.GetGcdElapsed() * 1000f);
    public static bool TargetHasAny(uint id, float seconds = 0)
    {
        var target = Target;
        if (target == null || !target.HasStatus(id)) return false;
        return seconds <= 0 || target.GetStatusLeftTime(id) >= seconds;
    }
    public static float Distance => Core.Me == null || Target == null ? float.MaxValue : Core.Me.DistanceToMe();
    // Reaper's melee actions use a fixed 3m base range. Keep this calculation
    // independent from PromeRotation's global range hack so the ACR always
    // follows exactly: disabled = 3m, enabled = 3m + the slider value.
    public static float Range()
    {
        const float reaperMeleeRange = 3f;
        if (!PromeSettings.Instance.GetQt("长臂猿"))
            return reaperMeleeRange;

        return reaperMeleeRange + Math.Clamp(ReaperRotation.ExtraRange, 0f, 3f);
    }
    public static bool Near() => Targetable && Distance <= Range();
    public static bool Ready(uint id) => ActionHelper.IsReady(id);
    // AE's IsReadyWithCanCast for actions directed at a battle target. A usable
    // charge is sufficient; the entire charge group does not have to be ready.
    public static unsafe bool IsReadyWithCanCast(uint id, IBattleChara? target)
    {
        var me = Core.Me;
        var manager = FfxivActionManager.Instance();
        if (me == null || target == null || manager == null || id == 0) return false;
        if (manager->GetActionStatus(FfxivActionType.Action, id) == 573
            || !ActionHelper.IsUnlocked(id)
            || Recently(id, 500)) return false;

        var adjustedId = Adjust(id);
        if (adjustedId == 0 || !ActionHelper.IsActionAvailableByLevelAndQuest(adjustedId)) return false;
        var source = (FfxivGameObject*)me.Address;
        var destination = (FfxivGameObject*)target.Address;
        if (source == null || destination == null
            || !FfxivActionManager.CanUseActionOnTarget(adjustedId, destination)
            || manager->GetActionStatus(FfxivActionType.Action, adjustedId, target.EntityId,
                checkRecastActive: false, checkCastingActive: false) != 0
            || FfxivActionManager.GetActionInRangeOrLoS(id, source, destination) == 566) return false;

        // Match AE's additional collision ray, two metres above both positions.
        var origin = *source->GetPosition();
        var end = *destination->GetPosition();
        origin.Y += 2f;
        end.Y += 2f;
        var ray = end - origin;
        var length = ray.Magnitude;
        if (length > 0f && FFXIVClientStructs.FFXIV.Common.Component.BGCollision.BGCollisionModule
            .RaycastMaterialFilter(origin, ray / length, out _, length)) return false;

        if (ChargesWithElapsedFallback(id) >= 1f) return true;
        var queueWindowMs = ActionHelper.ResolveActionType(id) == ActionType.OffGcd
            ? 100f : PromeSettings.Instance.Hacks.GcdQueueWindowMs;
        return Cd(id) * 1000f <= queueWindowMs;
    }
    public static bool CanCast(uint id, IBattleChara? target = null)
    {
        if (!CanUseAction(id, target)) return false;
        var adjustedId = Adjust(id);
        if (adjustedId == 0) return false;
        if (Charges(adjustedId) >= 1f) return true;

        var queueWindowMs = ActionHelper.ResolveActionType(adjustedId) == ActionType.OffGcd
            ? 100f : PromeSettings.Instance.Hacks.GcdQueueWindowMs;
        return ActionHelper.GetActionCooldown(adjustedId) * 1000f <= queueWindowMs;
    }
    public static bool HasUsableCharge(uint id, IBattleChara? target)
    {
        if (!CanUseAction(id, target)) return false;
        var adjustedId = Adjust(id);
        return adjustedId != 0 && Charges(adjustedId) >= 1f;
    }
    public static bool CooldownBeforeNextWindowEnd(uint id, int windowCount)
    {
        var cooldownEndOffset = ActionHelper.GetGcdElapsed() - .3f + Cd(id);
        var windowEnd = 2.5f * (windowCount + 1)
            - PromeSettings.Instance.Hacks.GcdQueueWindowMs / 1000f;
        return cooldownEndOffset < windowEnd;
    }
    private static unsafe bool CanUseAction(uint id, IBattleChara? target)
    {
        var adjustedId = Adjust(id);
        if (adjustedId == 0 || Core.Me == null || GameData.IsPlayerOccupied()
            || !ActionHelper.IsActionAvailableByLevelAndQuest(adjustedId)
            || ReaperSpellHistory.RecentlyUsed(id, 500)
            || ReaperSpellHistory.RecentlyUsed(adjustedId, 500)) return false;

        target ??= ActionHelper.TryResolveActionTargetType(adjustedId, out var targetType)
            && targetType == ActionTargetType.Self ? Core.Me : Core.Target;
        if (target == null || !target.IsTargetable) return false;

        var nativeTarget = (FfxivGameObject*)target.Address;
        return nativeTarget != null && FfxivActionManager.CanUseActionOnTarget(adjustedId, nativeTarget);
    }
    public static float Cd(uint id) => ActionHelper.GetActionCooldown(id);
    public static float Charges(uint id) => ActionHelper.GetActionCharges(id);
    // PR assumes full charges when the recast detail is missing. AE instead
    // derives fractional charges from elapsed recast time, without a full-charge cap.
    public static unsafe float ChargesWithElapsedFallback(uint id)
    {
        var manager = FfxivActionManager.Instance();
        if (id == 0 || manager == null) return 0f;
        var row = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id);
        if (!row.HasValue) return 0f;
        var action = row.Value;
        if (action.CooldownGroup == 0) return 1f;

        var recastTime = FfxivActionManager.GetAdjustedRecastTime(FfxivActionType.Action, action.RowId, true) / 1000f;
        if (recastTime <= 0f) return 0f;
        var detail = manager->GetRecastGroupDetail(action.CooldownGroup - 1);
        if (detail == null)
            return (float)(TimeSpan.FromSeconds(ActionHelper.GetActionRecastTimeElapsed(action.RowId)).TotalSeconds / recastTime);

        var maxCharges = FfxivActionManager.GetMaxCharges(action.RowId, 0u);
        if (!detail->IsActive) return maxCharges;
        var charges = detail->Elapsed / recastTime;
        return charges > maxCharges ? maxCharges : charges;
    }
    public static bool Qt(string id) => PromeSettings.Instance.GetQt(id);
    public static bool Weave => ActionHelper.GetGcdRemain() >= .6f;
    public static float GcdElapsed => ActionHelper.GetGcdElapsed();
    public static bool InShroud => Has(2593u) || JobGaugeHelper.RPR.夜游魂衣剩余时间 > 0;
    public static bool InEnshroud => JobGaugeHelper.RPR.夜游魂 > 0;
    public static bool Targetable
    {
        get
        {
            var me = Core.Me;
            var target = Target;
            return me != null && target != null && target.EntityId != me.EntityId
                && target.CurrentHp > 0 && target.CanUseAttackActionOn();
        }
    }
    public static PAction A(uint id, ActionType type = ActionType.Gcd, ActionTargetType target = ActionTargetType.Target) => new(id, type, target);
    public static uint Adjust(uint id) => ActionHelper.GetAdjustedActionId(id);
    public static unsafe bool Is1ChargesNextMs(uint id, long time = 30L)
    {
        if (id == 0)
        {
            Svc.Log.Error("禁止传id<=0的技能");
            return false;
        }

        var rowOrDefault = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id);
        if (!rowOrDefault.HasValue)
        {
            return false;
        }

        var action = rowOrDefault.Value;
        if (action.CooldownGroup == 0)
        {
            return true;
        }

        var recastTime = FfxivActionManager.GetAdjustedRecastTime(FfxivActionType.Action, action.RowId, true) / 1000f;
        if (recastTime <= 0f)
        {
            return false;
        }

        var actionManager = FfxivActionManager.Instance();
        if (actionManager == null)
        {
            return false;
        }

        var recastGroupDetail = actionManager->GetRecastGroupDetail(action.CooldownGroup - 1);
        if (recastGroupDetail == null)
        {
            return (float)((TimeSpan.FromSeconds(ActionHelper.GetActionRecastTimeElapsed(action.RowId)).TotalSeconds
                + time / 1000) / recastTime) >= 1f;
        }

        var maxCharges = FfxivActionManager.GetMaxCharges(action.RowId, 0u);
        if (!recastGroupDetail->IsActive)
        {
            return maxCharges >= 1;
        }

        return (recastGroupDetail->Elapsed + time / 1000f) / recastTime >= 1f;
    }
    public static int NearbyEnemies(float attackRange, float radius = 5f)
    {
        var me = Core.Me;
        var target = Target;
        if (me == null || target == null || Distance >= attackRange) return 0;

        var enemies = GetNearbyAttackableEnemies(me);
        var count = 0;
        foreach (var enemy in enemies)
            if (Vector2.Distance(target.PositionXY(), enemy.PositionXY()) - target.HitboxRadius <= radius)
                count++;
        return count;
    }
    public static int NearbyEnemies() => NearbyEnemies(Range(), 5f);

    private static IBattleChara[] GetNearbyAttackableEnemies(IBattleChara me)
    {
        var now = Environment.TickCount64;
        if (nearbyEnemySourceId == me.EntityId && now - nearbyEnemyCacheAt < 100)
            return nearbyAttackableEnemies;

        var enemies = new List<IBattleChara>();
        for (var i = 0; i < Svc.Objects.Length; i++)
        {
            if (Svc.Objects[i] is IBattleChara enemy && IsValidAttackUnit(enemy)
                && Vector2.Distance(me.PositionXY(), enemy.PositionXY())
                    < 24f + me.HitboxRadius + enemy.HitboxRadius)
                enemies.Add(enemy);
        }

        nearbyAttackableEnemies = enemies.ToArray();
        nearbyEnemySourceId = me.EntityId;
        nearbyEnemyCacheAt = now;
        return nearbyAttackableEnemies;
    }
    private static unsafe bool IsValidAttackUnit(IBattleChara unit)
    {
        if (!unit.IsValid() || !unit.IsTargetable || unit.CurrentHp == 0) return false;
        var nativeTarget = (FfxivGameObject*)unit.Address;
        return nativeTarget != null && FfxivActionManager.CanUseActionOnTarget(142u, nativeTarget);
    }
    public static bool HasSingleTargetFirewall => Has(3499u) || Has(3500u) || Has(4192u) || Has(4194u);
    public static bool ChargeSoon(uint id, float seconds)
    {
        var charges = Charges(id);
        return charges < ActionHelper.GetMaxCharges(id) && Cd(id) <= seconds;
    }
}

// Shared resolver/opener contracts live with the PR API helpers so the
// project does not need separate base-class source files.
public abstract class Gcd : IDecisionResolver
{
    protected static CheckResult Ok => new(true, string.Empty);
    protected static CheckResult No => new(false, string.Empty);
    public abstract CheckResult Check();
    public abstract PAction GetAction();
}

public abstract class Ogcd : IDecisionResolver
{
    protected static CheckResult Ok => new(true, string.Empty);
    protected static CheckResult No => new(false, string.Empty);
    public abstract CheckResult Check();
    public abstract PAction GetAction();
}

public abstract class ReaperOpener : IOpener
{
    public abstract string OpenerName { get; }
    public abstract List<PAction> InCombatSequence { get; }
    public virtual void InitializeCountdown(CountDownHandler countdownHandler) { }
    protected static PAction G(uint id) => new(id, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true };
    protected static PAction O(uint id, ActionTargetType target = ActionTargetType.Self) => new(id, ActionType.OffGcd, target) { RequiresVerification = true };
    protected static void AddPotion(List<PAction> actions)
    {
        if (!ReaperRotation.OpenerBurst || ReaperRotation.PotionType == 2) return;
        if (!R.Qt("爆发药")) return;
        var potion = GameData.GetBestPotionId();
        if (potion != 0)
            actions.Add(new PAction(potion, ActionType.Item, ActionTargetType.Self) { RequiresVerification = true });
    }
    protected static void AddPositionalPair(List<PAction> actions, params PAction[] tail)
    {
        // AE evaluates both positional actions when they are reached. The PR
        // opener queue is built before combat, so defer both actions until the
        // preceding Gluttony cast and the first positional hit have completed.
        OpenerPositionalRuntime.Begin(tail);
    }
    protected static void AddHarpeCountdown(CountDownHandler countdownHandler, int timeRemainingMs)
    {
        countdownHandler.AddAction(timeRemainingMs, G(R.Harpe));
        if (ReaperRotation.OpenerRush) countdownHandler.AddAction(200, O(R.Ingress, ActionTargetType.Target));
    }
    protected static void AddArcaneCircleCountdown(CountDownHandler countdownHandler, int timeRemainingMs) =>
        countdownHandler.AddAction(timeRemainingMs, O(R.ArcaneCircle));
}

internal static class OpenerPositionalRuntime
{
    private enum Stage
    {
        Inactive,
        WaitingForGluttony,
        WaitingForFirstPositional,
        Complete
    }

    private static Stage stage;
    private static PAction[] tail = Array.Empty<PAction>();
    private static uint firstPositionalAction;
    private static bool gluttonyWasReady;
    private static bool gluttonyConfirmed;
    private static bool firstPositionalConfirmed;
    private static uint positionalStatus;
    private static int initialPositionalStacks;

    internal static void Begin(PAction[] openerTail)
    {
        tail = openerTail ?? Array.Empty<PAction>();
        firstPositionalAction = 0;
        gluttonyWasReady = R.Ready(R.Gluttony);
        gluttonyConfirmed = false;
        firstPositionalConfirmed = false;
        positionalStatus = 0;
        initialPositionalStacks = 0;
        stage = Stage.WaitingForGluttony;
    }

    internal static void Reset()
    {
        tail = Array.Empty<PAction>();
        firstPositionalAction = 0;
        gluttonyWasReady = false;
        gluttonyConfirmed = false;
        firstPositionalConfirmed = false;
        positionalStatus = 0;
        initialPositionalStacks = 0;
        stage = Stage.Inactive;
    }

    internal static void RecordConfirmedAction(uint actionId)
    {
        if (stage == Stage.WaitingForGluttony && actionId == R.Gluttony)
            gluttonyConfirmed = true;
        else if (stage == Stage.WaitingForFirstPositional
            && (actionId == firstPositionalAction || actionId == R.Adjust(firstPositionalAction)))
            firstPositionalConfirmed = true;
    }

    internal static void Update()
    {
        if (stage is Stage.Inactive or Stage.Complete || !R.Targetable) return;

        if (stage == Stage.WaitingForGluttony)
        {
            gluttonyWasReady |= R.Ready(R.Gluttony);
            // Prefer the confirmed action; the cooldown and granted stacks
            // also cover hosts where action-flow confirmation is unavailable.
            if (!gluttonyConfirmed && !(gluttonyWasReady && R.Cd(R.Gluttony) > .1f)) return;
            positionalStatus = R.Has(3858u) ? 3858u : 2587u;
            initialPositionalStacks = GetPositionalStacks();
            if (initialPositionalStacks == 0) return;

            var enhancedAction = TargetHelper.GetTargetPositional() == Positional.Rear
                ? R.EnhancedGallows
                : R.EnhancedGibbet;
            firstPositionalAction = GetLevelAdjustedPositionalAction(enhancedAction);
            ActionQueueManager.Enqueue(new PAction(firstPositionalAction, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            });
            stage = Stage.WaitingForFirstPositional;
            return;
        }

        if (stage != Stage.WaitingForFirstPositional || firstPositionalAction == 0)
            return;

        var remainingStacks = GetPositionalStacks();
        var consumedFirstStack = remainingStacks > 0 && remainingStacks < initialPositionalStacks;
        if (!firstPositionalConfirmed && !consumedFirstStack) return;

        // Alternate from the hit that actually completed. Its enhancement buff
        // may arrive a frame later than the action-effect event.
        var enhancedSecondAction = firstPositionalAction is R.Gallows or R.EnhancedGallows
            ? R.EnhancedGibbet
            : R.EnhancedGallows;
        var secondPositionalAction = GetLevelAdjustedPositionalAction(enhancedSecondAction);
        var actions = new List<PAction>
        {
            new(secondPositionalAction, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            }
        };
        actions.AddRange(tail);
        ActionQueueManager.Enqueue(actions);
        stage = Stage.Complete;
    }

    private static int GetPositionalStacks()
        => Core.Me?.GetStatusStackCount(positionalStatus) ?? 0;

    private static uint GetLevelAdjustedPositionalAction(uint actionId)
    {
        var player = Core.Me;
        if (player == null || player.Level >= 96) return actionId;

        return actionId switch
        {
            R.EnhancedGibbet => R.Gibbet,
            R.EnhancedGallows => R.Gallows,
            _ => actionId,
        };
    }
}
