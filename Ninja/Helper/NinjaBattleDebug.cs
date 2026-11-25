using System;
using System.Collections.Generic;
using System.Reflection;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

internal static class NinjaBattleDebug
{
    private static readonly FieldInfo? HostOpenerField = typeof(RotationManager)
        .GetField("_isOpenerInProgress", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly Queue<string> history = new();
    private static long nextSampleAt;
    private static bool? lastAttackable;
    private static uint lastTargetId;
    private static long targetChangedAt;
    private static long lastSuitonAt;
    public static string TargetTransition { get; private set; } = "尚未采样";
    public static string LastCollectionError { get; private set; } = "无";
    public static IReadOnlyCollection<string> History => history;

    public static void Reset()
    {
        history.Clear();
        nextSampleAt = targetChangedAt = lastSuitonAt = 0;
        lastAttackable = null;
        lastTargetId = 0;
        TargetTransition = "尚未采样";
        LastCollectionError = "无";
    }

    public static void Update()
    {
        try { SampleTarget(); }
        catch (Exception ex)
        {
            // Diagnostic collection must never stop the combat update callback.
            LastCollectionError = $"目标采样失败：{ex.GetType().Name}：{ex.Message}";
        }
    }

    private static void SampleTarget()
    {
        var now = Environment.TickCount64;
        if (now < nextSampleAt) return;
        nextSampleAt = now + 200;
        var target = Core.Target;
        var attackable = target?.CanUseAttackActionOn() == true;
        var targetId = target?.EntityId ?? 0;
        if (lastAttackable != attackable || lastTargetId != targetId)
        {
            TargetTransition = attackable
                ? $"可攻击目标：{target!.Name}（{targetId}）"
                : target == null ? "没有选中目标" : $"目标存在但不可攻击：{target.Name}（{targetId}）";
            AddHistory(TargetTransition + $"；{(GameData.IsInCombat() ? "仍在战斗中" : "当前不在战斗中")}");
            targetChangedAt = now;
            lastAttackable = attackable;
            lastTargetId = targetId;
        }
    }

    public static void RecordManualSuitonRequest()
    {
        try { AddHistory("预备水遁 Hotkey：已提交一组天→地→人→水遁，完成或失败后不自动重复"); }
        catch (Exception ex) { LastCollectionError = $"水遁热键记录失败：{ex.GetType().Name}：{ex.Message}"; }
    }

    // Observe both effect paths without changing the rotation's own confirmation bookkeeping.
    public static void RecordSuiton(uint id, string source)
    {
        try { RecordSuitonEffect(id, source); }
        catch (Exception ex)
        {
            LastCollectionError = $"水遁记录失败：{ex.GetType().Name}：{ex.Message}";
        }
    }

    private static void RecordSuitonEffect(uint id, string source)
    {
        if (id != NinjaActions.Suiton && id != NinjaActions.TcjSuiton) return;
        var now = Environment.TickCount64;
        // Full effects and entry fragments commonly describe one cast. Two real
        // Suiton casts cannot finish within this short packet deduplication window.
        if (lastSuitonAt != 0 && now - lastSuitonAt <= 250) return;
        var tcj = id == NinjaActions.TcjSuiton
            || (id == NinjaActions.Suiton && (NinjaCombatState.HasTcjBuff
                || NinjaCombatState.PendingTcjJutsu == NinjaActions.TcjSuiton));
        var interval = lastSuitonAt == 0 ? "" : $"；距上一条水遁记录 {(now - lastSuitonAt) / 1000f:F1} 秒";
        AddHistory($"观测到{(tcj ? "天地人水遁" : "普通水遁")}效果（{id}，{source}）{interval}");
        lastSuitonAt = now;
    }

    public static string TargetAge => targetChangedAt == 0 ? "无" : $"{(Environment.TickCount64 - targetChangedAt) / 1000f:F1} 秒前";

    public static NinjaBattleDebugSnapshot Capture(string gcdCandidate, bool phantomUrgent, bool tenriEligible, bool phantomBurst)
    {
        var me = Core.Me;
        var target = Core.Target;
        PAction? active = null;
        PAction? attempted = null;
        bool? hostOpener = null;
        var readable = false;
        var isGroup = false;
        try
        {
            readable = NinjaRotation.PeekLockedActionMethod != null;
            active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
            isGroup = NinjaRotation.ActiveCommandField?.GetValue(null)?.GetType().Name == "ActionGroupCommand";
            hostOpener = HostOpenerField?.GetValue(null) as bool?;
        }
        catch
        {
            readable = false;
        }
        return new NinjaBattleDebugSnapshot
        {
            HasPlayer = me != null, Dead = me?.IsDead == true,
            AcrPaused = PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold,
            HostAllowsAcr = PromeSettings.Instance.ShouldExecuteAcr(), Occupied = GameData.IsPlayerOccupied(),
            Casting = me?.IsCasting == true, Countdown = GameData.GetCountdown(),
            AnimationLock = ActionHelper.GetAnimationLock(), GcdRemain = ActionHelper.GetGcdRemain(),
            HostOpenerActive = hostOpener, HasActiveCommand = ActionUpdaterRouter.HasActiveCommand(),
            HasQueuedActions = ActionQueueManager.HasActionsInQueue(), GcdLocked = ActionUpdaterRouter.HasLockedGcdAction(),
            TargetPresent = target != null, TargetAttackable = target?.CanUseAttackActionOn() == true,
            Distance = NinjaTarget.Distance, MeleeRange = NinjaTarget.AttackRange,
            HoldsNormalDecisions = NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions,
            TcjActive = NinjaCombatState.IsTcjSequenceActive, NormalMudraActive = NinjaSequencePolicy.IsNormalMudraActive,
            ShadowWalker = me?.HasStatus(NinjaActions.ShadowWalkerStatus) == true,
            MudraCharges = ActionHelper.GetActionCharges(NinjaActions.Ten),
            PhantomUrgent = phantomUrgent, TenriEligible = tenriEligible, PhantomBurst = phantomBurst,
            GcdCandidate = gcdCandidate,
            CanUsePostNinjutsuOffGcd = NinjaCombatState.CanUsePostNinjutsuOffGcd,
            CanUseGcdOffGcd = NinjaCombatState.CanUseGcdOffGcd,
            CanUseCurrentGcdAbility = NinjaEventHandler.CanUseCurrentGcdAbility,
            PostNinjutsuUsed = NinjaCombatState.PostNinjutsuOffGcdUsed,
            PostNinjutsuLimit = NinjaCombatState.PostNinjutsuOffGcdLimit,
            GcdOffGcdUsed = NinjaCombatState.GcdOffGcdUsed, GcdOffGcdLimit = NinjaCombatState.MaxGcdOffGcd,
            CommandReadable = readable && active != null,
            ActiveAction = active == null ? "无" : $"{NinjaAttemptDebug.FormatAction(active.ActionId)}（{active.ActionId}，{active.Type}）",
            ActiveTargetsEnemy = active?.Target == ActionTargetType.Target, ActiveIsGroup = isGroup,
            ActiveReady = ReadActionReady(active),
            AwaitingVerification = attempted != null,
            AwaitedAction = attempted == null ? "无" : $"{NinjaAttemptDebug.FormatAction(attempted.ActionId)}（{attempted.ActionId}）",
            AttemptAgeMs = Math.Max(0, NinjaAttemptDebug.CurrentAgeMs), RetryCount = NinjaAttemptDebug.RetryCount,
            SequenceActive = NinjaSequencePolicy.IsActive
        };
    }

    private static bool? ReadActionReady(PAction? action)
    {
        if (action == null || action.Type == ActionType.Item) return null;
        try { return ActionHelper.IsReady(action.ActionId); }
        catch (Exception ex)
        {
            LastCollectionError = $"技能就绪状态读取失败：{ex.GetType().Name}：{ex.Message}";
            return null;
        }
    }

    private static void AddHistory(string text)
    {
        history.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] {text}");
        while (history.Count > 16) history.Dequeue();
    }
}
