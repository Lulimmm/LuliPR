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
    public static float Range(float fallback = 3)
    {
        var range = GameData.GetCurrentAttackRange(fallback);
        return PromeSettings.Instance.GetQt("长臂猿") ? range + ReaperRotation.ExtraRange : range;
    }
    public static bool Near(float range = 3) => Distance <= Range(range);
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
    protected static PAction StanceFromTarget() =>
        TargetHelper.GetTargetPositional() == Positional.Rear ? G(R.EnhancedGallows) : G(R.EnhancedGibbet);
    protected static PAction StanceFromBuff() => R.Has(2589u) ? G(R.EnhancedGallows) : G(R.EnhancedGibbet);
    protected static void AddHarpeCountdown(CountDownHandler countdownHandler, int timeRemainingMs)
    {
        countdownHandler.AddAction(timeRemainingMs, G(R.Harpe));
        if (ReaperRotation.OpenerRush) countdownHandler.AddAction(200, O(R.Ingress, ActionTargetType.Target));
    }
    protected static void AddArcaneCircleCountdown(CountDownHandler countdownHandler, int timeRemainingMs) =>
        countdownHandler.AddAction(timeRemainingMs, O(R.ArcaneCircle));
}
