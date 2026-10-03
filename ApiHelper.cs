using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FfxivActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using FfxivActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;
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

    public static IBattleChara? Target => Core.Target;
    public static bool Has(uint id) => Core.Me?.HasStatus(id) == true;
    public static bool HasFor(uint id, float seconds) => Core.Me?.HasStatus(id) == true && Core.Me.GetStatusLeftTime(id) >= seconds;
    public static bool TargetHas(uint id, float seconds = 0)
    {
        var target = Target;
        var me = Core.Me;
        if (target == null || me == null || !target.StatusList.Any(status => status.StatusId == id && status.SourceId == me.EntityId)) return false;
        return seconds <= 0 || target.GetStatusLeftTime(id) >= seconds;
    }
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
    public static bool Near() => Distance <= Range();
    public static bool Ready(uint id) => ActionHelper.IsReady(id);
    public static float Cd(uint id) => ActionHelper.GetActionCooldown(id);
    public static float Charges(uint id) => ActionHelper.GetActionCharges(id);
    public static bool Qt(string id) => PromeSettings.Instance.GetQt(id);
    public static bool Weave => ActionHelper.GetGcdRemain() >= .6f;
    public static float GcdElapsed => ActionHelper.GetGcdElapsed();
    public static bool InShroud => Has(2593u) || JobGaugeHelper.RPR.夜游魂衣剩余时间 > 0;
    public static bool InEnshroud => JobGaugeHelper.RPR.夜游魂 > 0;
    public static bool Targetable => Target?.CanUseAttackActionOn() == true;
    public static PAction A(uint id, ActionType type = ActionType.Gcd, ActionTargetType target = ActionTargetType.Target) => new(id, type, target);
    public static uint Adjust(uint id) => ActionHelper.GetAdjustedActionId(id);
    public static bool Recently(uint id, int ms) => ActionHelper.RecentlyUsed(id, ms);
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
            return (ActionHelper.GetActionRecastTimeElapsed(action.RowId) + time / 1000f) / recastTime >= 1f;
        }

        var maxCharges = FfxivActionManager.GetMaxCharges(action.RowId, 0u);
        if (!recastGroupDetail->IsActive)
        {
            return maxCharges >= 1;
        }

        return (recastGroupDetail->Elapsed + time / 1000f) / recastTime >= 1f;
    }
    public static int NearbyEnemies(float range = 5) => Target == null ? 0 : (int)TargetHelper.EnemyInRangeTarget(Target, range);
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
    private static bool initialEnhancedGibbet;
    private static bool initialEnhancedGallows;
    private static long firstPositionalQueuedAt;
    private static float firstPositionalGcdElapsed;

    internal static void Begin(PAction[] openerTail)
    {
        tail = openerTail ?? Array.Empty<PAction>();
        firstPositionalAction = 0;
        gluttonyWasReady = false;
        initialEnhancedGibbet = R.Has(2588u);
        initialEnhancedGallows = R.Has(2589u);
        firstPositionalQueuedAt = 0;
        firstPositionalGcdElapsed = 0;
        stage = Stage.WaitingForGluttony;
    }

    internal static void Reset()
    {
        tail = Array.Empty<PAction>();
        firstPositionalAction = 0;
        gluttonyWasReady = false;
        initialEnhancedGibbet = false;
        initialEnhancedGallows = false;
        firstPositionalQueuedAt = 0;
        firstPositionalGcdElapsed = 0;
        stage = Stage.Inactive;
    }

    internal static void Update()
    {
        if (stage == Stage.WaitingForGluttony)
        {
            // Normal opener queues do not call ActionHelper.RecordAction, so
            // RecentlyUsed cannot identify the queued Gluttony cast. Observe
            // its cooldown transition instead.
            if (!gluttonyWasReady)
            {
                gluttonyWasReady = R.Ready(R.Gluttony);
                return;
            }

            if (R.Ready(R.Gluttony) && R.Cd(R.Gluttony) <= .1f)
                return;

            firstPositionalAction = TargetHelper.GetTargetPositional() == Positional.Rear
                ? R.EnhancedGallows
                : R.EnhancedGibbet;
            ActionQueueManager.Enqueue(new PAction(firstPositionalAction, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            });
            firstPositionalQueuedAt = Environment.TickCount64;
            firstPositionalGcdElapsed = ActionHelper.GetGcdElapsed();
            stage = Stage.WaitingForFirstPositional;
            return;
        }

        if (stage != Stage.WaitingForFirstPositional || firstPositionalAction == 0)
            return;

        var now = Environment.TickCount64;
        var gainedEnhancedBuff = (!initialEnhancedGibbet && R.Has(2588u))
            || (!initialEnhancedGallows && R.Has(2589u));
        var gcdWasReset = firstPositionalGcdElapsed > .25f
            && ActionHelper.GetGcdElapsed() < .25f;
        if (!gainedEnhancedBuff
            && !gcdWasReset
            && now - firstPositionalQueuedAt < 1800)
            return;

        var secondPositionalAction = R.Has(2589u)
            ? R.EnhancedGallows
            : R.EnhancedGibbet;
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
}
