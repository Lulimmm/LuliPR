using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using Dalamud.Bindings.ImGui;
using ErosUI;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Managers.CombatEventManager;
using PromeRotation.Managers.CombatEventManager.Events;
using PromeRotation.PureTimeline;
using PromeRotation.PureTimeline.Runtime;
using PromeRotation.Timeline;
using PromeRotation.Timeline.Core;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Updaters;
using Ninja.PR.Timeline;

namespace Ninja.PR;

[RotationMetadata(30u, "Luli Ninja PR", "Cino", "1.1.0", ContentScope = AcrContentScope.HighEnd)]
public sealed class NinjaRotation : IRotation, IRotationLifecycle
{
    private static readonly HashSet<string> HiddenQtKeys = new(StringComparer.Ordinal)
    {
        "倾斜资源", "斩杀", NinjaQts.PrepareSuiton
    };
    internal static readonly MethodInfo? PeekLockedActionMethod = typeof(ActionUpdater)
        .GetMethod("PeekCurrentLockedAction", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? ActiveCommandField = typeof(ActionUpdater)
        .GetField("_activeCommand", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? LastAttemptedActionField = typeof(ActionUpdater)
        .GetField("_lastActionAttempted", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? LastVerifiedActionField = typeof(ActionUpdater)
        .GetField("_lastVerifiedActionId", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? AepMatchedField = typeof(ActionUpdater)
        .GetField("_aepMatched", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? RetryCountField = typeof(ActionUpdater)
        .GetField("_retryCount", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? StuckTimerField = typeof(ActionUpdater)
        .GetField("_stuckTimer", BindingFlags.Static | BindingFlags.NonPublic);
    internal static readonly FieldInfo? RetryTimerField = typeof(ActionUpdater)
        .GetField("_retryTimer", BindingFlags.Static | BindingFlags.NonPublic);
    internal static string LastUpdaterRecovery = "None";

    public string RotationName => "Luli Ninja PR";
    public uint JobId => 30u;
    public string AuthorName => "Cino";
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        [NinjaQts.BigTrick] = true,
        [NinjaQts.SmallTrick] = true,
        [NinjaQts.TenChiJin] = true,
        ["生杀予夺"] = true,
        ["梦幻三段"] = true,
        [NinjaQts.Suiton] = true,
        [NinjaQts.Raiton] = true,
        [NinjaQts.Phantom] = true,
        [NinjaQts.Raiju] = true,
        [NinjaQts.Bunshin] = true,
        [NinjaQts.Meisui] = true,
        [NinjaQts.Bhavacakra] = true,
        [NinjaQts.TenriJindo] = true,
        [NinjaQts.AutoTrueNorth] = true,
        ["倾斜资源"] = false,
        ["斩杀"] = false,
        [NinjaQts.PrepareSuiton] = false
    };

    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        ["常规斩击背刺起手"] = typeof(NinjaStandardOpener),
        ["ED起手"] = typeof(NinjaEdOpener)
    };

    public static IJobNodeProvider? NodeProvider { get; } = new NinjaJobNodeProvider();
    // AE's GCD SlotResolverManager order. ReadyGoSuiton is the PR equivalent
    // of the pre-pull Suiton resolver; TCJ follows it before Kassatsu jutsu.
    private readonly List<IDecisionResolver> gcd = new() { new NinjaAlwaysResolver(), new NinjaTcjResolver(), new NinjaKassatsuJutsuResolver(), new NinjaJutsuResolver(), new NinjaEarlier4Or3Resolver(), new NinjaKBaseResolver(), new NinjaPhantomResolver(), new NinjaRaijuResolver(), new NinjaRaitonResolver(), new NinjaComboResolver() };
    // AE's SlotMode.Always resolvers are evaluated by the framework's
    // always pass, independently from the oGCD queue. Preserve their order.
    private readonly List<IDecisionResolver> always;
    private readonly List<IDecisionResolver> ogcd;
    private readonly NinjaTrickAttackResolver trickAttack = new();
    private readonly NinjaPriorityBhavacakraResolver priorityBhavacakra;
    private readonly NinjaEventHandler events = new();

    public NinjaRotation()
    {
        priorityBhavacakra = new NinjaPriorityBhavacakraResolver();
        always = new() { new NinjaKassatsuResolver(), trickAttack };
        // Congratulation order for the oGCD slots:
        // _100B, Mug, Bunshin, Ten Chi Jin, Meisui, Tenri Jindo,
        // Assassinate, Bhavacakra, True North. Kassatsu and Trick Attack
        // remain in the Always list above, matching AE's SlotMode.Always.
        ogcd = new() { priorityBhavacakra, new NinjaMugResolver(), new NinjaBunshinResolver(), new NinjaTenChiJinResolver(), new NinjaMeisuiResolver(), new NinjaTenriJindoResolver(), new NinjaAssassinateResolver(), new NinjaOgcdResolver(IsComboResolverFirst, IsRaijuResolverFirst), new NinjaTrueNorthResolver(IsComboResolverFirst) };
        ErosUIFramework.Configure(
            jobTag: "NIN",
            jobName: "Ninja",
            qtAll: QtList,
            qtIsMetaKey: _ => false,
            qtIsVisibleInMode: (key, _) => !HiddenQtKeys.Contains(key),
            qtDefault: GetQtDefault,
            qtCascadeRules: new Dictionary<string, (string key, bool invert)[]>(),
            hotkeyNames: Array.Empty<string>(),
            buildHotkeys: null,
            author: "Cino",
            drawGeneralSettings: null,
            drawDebugSettings: DrawDebugPanel);
        foreach (var qt in QtList)
        {
            MigrateQtKey(qt.Key);
            PromeSettings.Instance.AddQt(qt.Key, GetQtDefault(qt.Key));
        }
    }

    public PAction? NextGcd()
    {
        // While TCJ owns the GCD, no ordinary resolver may consume the slot.
        // This also covers resolvers whose individual checks do not need a
        // TCJ condition because they are normally prevented by AE's slot
        // ordering.
        if (NinjaCombatState.IsTcjSequenceActive)
            return NextTcjOnly();
        return Next(gcd);
    }

    public PAction? NextOffGcd()
    {
        // AE does not weave unrelated abilities during Ten Chi Jin. Keeping
        // this gate at the scheduler boundary prevents a missed per-resolver
        // condition from interrupting the three ninjutsu sequence.
        return NinjaCombatState.IsTcjSequenceActive ? null : Next(ogcd);
    }
    public PAction? NextAlways()
    {
        // TCJ owns all action slots until its third transformed jutsu is
        // confirmed. This prevents an Always resolver from stealing the
        // window between the three GCD packets.
        return NinjaCombatState.IsTcjSequenceActive ? null : Next(always);
    }

    private static PAction? Next(IEnumerable<IDecisionResolver> resolvers)
    {
        foreach (var resolver in resolvers)
        {
            if (!resolver.Check().Success) continue;

            // Sequence resolvers enqueue their complete action group. Returning
            // null stops this decision pass so another resolver cannot overlap it.
            return resolver.GetAction();
        }
        return null;
    }

    private PAction? NextTcjOnly()
    {
        var resolver = gcd[1];
        return resolver.Check().Success ? resolver.GetAction() : null;
    }

    private bool IsComboResolverFirst()
    {
        foreach (var resolver in GetSlotResolverPriority())
        {
            if (resolver.Check().Success) return resolver is NinjaComboResolver;
        }
        return false;
    }

    private bool IsRaijuResolverFirst()
    {
        foreach (var resolver in GetSlotResolverPriority())
        {
            if (resolver.Check().Success) return resolver is NinjaRaijuResolver;
        }
        return false;
    }

    private IEnumerable<IDecisionResolver> GetSlotResolverPriority()
    {
        foreach (var resolver in gcd)
        {
            if (resolver is NinjaAlwaysResolver) continue;
            yield return resolver;
        }
    }

    public void UpdateDebugStatus()
    {
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();
        AddStatus(always, RotationManager.AlwaysSolverStatus);
        AddStatus(gcd, RotationManager.GcdSolverStatus);
        AddStatus(ogcd, RotationManager.OffGcdSolverStatus);
    }

    private static void AddStatus(IEnumerable<IDecisionResolver> resolvers, IList<SolverStatus> output)
    {
        foreach (var resolver in resolvers)
        {
            var result = resolver.Check();
            output.Add(new SolverStatus { Name = resolver.GetType().Name, Success = result.Success, Message = result.Message });
        }
    }

    public IOpener? GetOpener()
    {
        if (Core.Me?.Level != 100) return null;
        if (NinjaSettings.Instance.Opener == "无") return null;
        return NinjaSettings.Instance.Opener switch
        {
            "常规斩击背刺起手" => new NinjaStandardOpener(),
            "ED起手" => new NinjaEdOpener(),
            _ => null
        };
    }

    public void DrawSettings()
    {
        ErosUIFramework.DrawSettingsEntry();
    }

    private static bool GetQtDefault(string key)
        => QtList.TryGetValue(key, out var value) && value;

    private static void MigrateQtKey(string key)
    {
        var oldKey = key switch
        {
            NinjaQts.BigTrick => "大背刺",
            NinjaQts.SmallTrick => "小背刺",
            _ => null
        };
        if (oldKey != null
            && !PromeSettings.Instance.QuickToggles.ContainsKey(key)
            && PromeSettings.Instance.QuickToggles.TryGetValue(oldKey, out var oldValue))
            PromeSettings.Instance.QuickToggles[key] = oldValue;
    }

    private void DrawDebugPanel()
    {
        UpdateDebugStatus();
        ImGui.Text($"TCJ active: {NinjaCombatState.IsTcjSequenceActive}");
        ImGui.Text($"Last TCJ step: {FormatDebugAction(NinjaCombatState.GetLastTcjStep())}");
        ImGui.Text($"Pending TCJ submit: {FormatDebugAction(NinjaCombatState.PendingTcjJutsu)} ({NinjaCombatState.PendingTcjAgeMs} ms)");
        ImGui.Text($"Last observed action: {NinjaCombatState.LastObservedActionId}");
        ImGui.Text($"Last GCD: {FormatDebugAction(NinjaCombatState.LastGcd)}");
        ImGui.Separator();
        ImGui.Text($"GCD gate: {(NinjaCombatState.IsTcjSequenceActive ? "TCJ exclusive" : "normal resolvers")}");
        ImGui.Text($"oGCD gate: {(NinjaCombatState.IsTcjSequenceActive ? "blocked by TCJ" : "open")}");
        ImGui.Text($"Always gate: {(NinjaCombatState.IsTcjSequenceActive ? "blocked by TCJ" : "open")}");
        ImGui.Text($"Active command: {ActionUpdater.HasActiveCommand()}");
        ImGui.Text($"Locked GCD: {ActionUpdater.HasLockedGcdAction()}");
        ImGui.Text($"Updater recovery: {LastUpdaterRecovery}");
        ImGui.Text($"Queued actions: {ActionQueueManager.HasActionsInQueue()}");
        ImGui.Separator();
        ImGui.Text($"Selected Always: {FirstActive(RotationManager.AlwaysSolverStatus)}");
        ImGui.Text($"Selected GCD: {FirstActive(RotationManager.GcdSolverStatus)}");
        ImGui.Text($"Selected oGCD: {FirstActive(RotationManager.OffGcdSolverStatus)}");
        ImGui.Separator();
        DrawDebugGroup("Always", RotationManager.AlwaysSolverStatus);
        DrawDebugGroup("GCD", RotationManager.GcdSolverStatus);
        DrawDebugGroup("oGCD", RotationManager.OffGcdSolverStatus);
        if (ImGui.CollapsingHeader("Scheduler / queue interceptors"))
            DrawSchedulerDebug();
    }

    private static string FormatDebugAction(uint id) => id switch
    {
        NinjaActions.TcjFumaShuriken => "TCJ Fuma (18873)",
        NinjaActions.TcjRaiton => "TCJ Raiton (18877)",
        NinjaActions.TcjSuiton => "TCJ Suiton (18881)",
        0 => "None",
        _ => id.ToString()
    };

    private static void DrawDebugGroup(string name, IList<SolverStatus> statuses)
    {
        ImGui.Text(name);
        foreach (var status in statuses)
            ImGui.Text($"  {(status.Success ? "ACTIVE" : "blocked")}  {status.Name}: {status.Message}");
    }

    private static string FirstActive(IList<SolverStatus> statuses)
    {
        foreach (var status in statuses)
            if (status.Success) return status.Name;
        return "None";
    }

    private static void DrawSchedulerDebug()
    {
        ImGui.Text("ActionUpdater");
        foreach (var entry in ActionUpdater.DebugInfo)
            ImGui.Text($"  {entry.Key}: {entry.Value}");

        ImGui.Text("Action queues");
        foreach (var queue in ActionQueueManager.GetQueueStatus())
        {
            var actions = queue.Value.Count == 0 ? "empty" : string.Join(", ", queue.Value);
            ImGui.Text($"  {queue.Key}: {actions}");
        }
    }

    public void DrawQTs() { }
    public IRotationEventHandler GetEventHandler() => events;
    public void OnEnterAcr()
    {
        events.Attach();
        ErosUIFramework.Install();
    }

    public void OnExitAcr()
    {
        events.Dispose();
        ErosUIFramework.Uninstall();
    }
}

internal sealed class NinjaEventHandler : IRotationEventHandler, IDisposable
{
    private bool attached;

    public void Attach()
    {
        if (attached) return;
        // AE advances TCJ's LastGcd from MemApiSpellCastSuccess. The PR
        // equivalent is the cast-start event; keep action-effect recording as
        // a fallback for instant actions and network packets.
        CombatEventManager.OnStartCast += OnStartCast;
        CombatEventManager.OnActionEffect += OnActionEffect;
        CombatEventManager.OnActionEffectEntry += OnActionEffectEntry;
        attached = true;
    }

    public void Dispose()
    {
        if (!attached) return;
        CombatEventManager.OnStartCast -= OnStartCast;
        CombatEventManager.OnActionEffect -= OnActionEffect;
        CombatEventManager.OnActionEffectEntry -= OnActionEffectEntry;
        attached = false;
    }

    private static void OnStartCast(StartCastEvent action)
    {
        if (action.SourceId != Core.Me?.EntityId) return;
        // Instant oGCDs such as Tenri Jindo can be observed through the
        // start-cast bridge without a subsequent action-effect packet. Feed
        // both verifier paths here as well; the updater ignores the inactive
        // path and still applies adjusted-action matching.
        ActionUpdaterRouter.NotifyUAL(action.ActionId);
        ActionUpdaterRouter.NotifyAEP(action.ActionId);
        if (action.ActionId == NinjaActions.TenChiJin)
        {
            // A GCD decision queued immediately before TCJ must not run in
            // the exclusive TCJ window. The opener's TCJ is the terminal
            // action, so clearing normal queues cannot remove its follow-up.
            // If ActionUpdater already promoted a normal Group to its active
            // command, clearing queues alone is too late; drop that command
            // before the TCJ resolver takes exclusive GCD ownership.
            ActionUpdater.SkipCurrentTask();
            ActionQueueManager.ClearNormalQueues();
        }
        NinjaCombatState.Record(action.ActionId);
    }

    private static void OnActionEffect(ActionEffectEvent action)
    {
        if (action.SourceId != Core.Me?.EntityId) return;
        // ActionFlowManager normally correlates UAL/AEP internally. Some
        // instant or adjusted Ninja actions arrive only through the combat
        // event bridge, so feed both verification paths with the resolved ID
        // as a fallback. The updater itself filters by the active mode and
        // adjusted action ID.
        ActionUpdaterRouter.NotifyUAL(action.ActionId);
        ActionUpdaterRouter.NotifyAEP(action.ActionId);
        if (action.ActionId == NinjaActions.TenChiJin)
        {
            // Some client paths publish the action effect without a matching
            // start-cast event. Apply the same queue handoff in that path.
            ActionUpdater.SkipCurrentTask();
            ActionQueueManager.ClearNormalQueues();
        }
        NinjaCombatState.Record(action.ActionId);
        // AE's RecentlyUsed checks are written against both the pre- and
        // post-upgrade action ids. Record aliases as well so an effect packet
        // reported with either id observes the same recency window.
        switch (action.ActionId)
        {
            case NinjaActions.TrickAttack:
                NinjaCombatState.Record(NinjaActions.TrickAttackCurrent);
                break;
            case NinjaActions.TrickAttackCurrent:
                NinjaCombatState.Record(NinjaActions.TrickAttack);
                break;
            case NinjaActions.Mug:
                NinjaCombatState.Record(NinjaActions.MugCurrent);
                break;
            case NinjaActions.MugCurrent:
                NinjaCombatState.Record(NinjaActions.Mug);
                break;
            case NinjaActions.Bhavacakra:
                NinjaCombatState.Record(36960u);
                break;
            case 36960u:
                NinjaCombatState.Record(NinjaActions.Bhavacakra);
                break;
        }
        if (action.ActionId == NinjaActions.Suiton
            && PromeSettings.Instance.GetQt(NinjaQts.PrepareSuiton))
            PromeSettings.Instance.SetQt(NinjaQts.PrepareSuiton, false);
        if (action.ActionId is NinjaActions.TrickAttack or NinjaActions.TrickAttackCurrent)
            NinjaSettings.Instance.TrickAttackDelayUntilNextMug = false;
    }

    private static void OnActionEffectEntry(ActionEffectEntryEvent action)
    {
        if (action.SourceId != Core.Me?.EntityId) return;
        ActionUpdaterRouter.NotifyUAL(action.ActionId);
        ActionUpdaterRouter.NotifyAEP(action.ActionId);
        if (action.ActionId is not (NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton
            or NinjaActions.TcjSuiton)
            && !NinjaCombatState.IsExpectedTcjActionAlias(action.ActionId)) return;
        // Some instant TCJ packets expose only the effect entry. Record it as
        // the same cast-success signal used by the full action-effect event.
        NinjaCombatState.Record(action.ActionId);
    }

    public void OnUpdate() => DropStaleOffGcdLock();
    public void OnOutOfBattleUpdate()
    {
        if (Core.Target == null) TryPrepareSuiton();
    }
    public void OnBattleStarted()
    {
        NinjaAeSlotSequence.Reset();
        NinjaCombatState.Reset();
        // RotationManager enqueues the selected opener before invoking the
        // rotation event handler. Start the strict GCD policy after that
        // queue has been created, so the handler reset cannot erase it.
        if (PromeSettings.Instance.OpenerHasBeenExecuted)
            NinjaAeSlotSequence.Begin(NinjaActions.TenChiJin);
    }
    public void OnBattleUpdate()
    {
        NinjaAeSlotSequence.Update();
        DropStaleOffGcdLock();
    }

    private static void DropStaleOffGcdLock()
    {
        if (HasVerificationMismatch())
        {
            // A previous verification can remain while the updater has
            // already selected a different current action. Clear only that
            // stale verification so the selected action/group stays intact.
            RecoverStaleVerification(preserveQueues: !ActionUpdaterRouter.HasLockedGcdAction());
            return;
        }

        // ActionUpdater can promote an oGCD during the last weave window and
        // then retain it after the GCD reaches zero. At that point its own
        // canUseOffGcd gate is false, so the command can never submit and no
        // new GCD is selected. Release only a normal, stale oGCD command when
        // every queue is empty; forced actions are left untouched.
        if (!ActionUpdaterRouter.HasActiveCommand()
            || ActionUpdaterRouter.HasLockedGcdAction()
            || ActionQueueManager.HasActionsInQueue()
            || ActionQueueManager.HasHighPriorityAction()
            || ActionHelper.GetGcdRemain() > 0.1f
            || IsActiveActionGroup()
            || !HasActiveOffGcdCommand()) return;

        ActionUpdaterRouter.SkipCurrentTask();
        try
        {
            ClearVerificationState();
            NinjaRotation.LastUpdaterRecovery = "reset stale oGCD lock";
        }
        catch
        {
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = "reset stale oGCD lock and queues";
        }
    }

    private static void RecoverStaleVerification(bool preserveQueues)
    {
        if (!preserveQueues)
        {
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = "reset stale verification and queues";
            return;
        }

        // Remove only stale verifier bookkeeping. Keep the active command and
        // action group intact; its current action is the command that should
        // be retried after the previous verification is discarded.
        try
        {
            ClearVerificationState();
            NinjaRotation.LastUpdaterRecovery = "cleared stale verification; queues preserved";
        }
        catch
        {
            // Reflection is only a compatibility bridge for PR's private
            // updater state. Fall back to the public reset if its internals
            // change in a future host build.
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = "reset stale verification and queues";
        }
    }

    private static void ClearVerificationState()
    {
        if (NinjaRotation.LastAttemptedActionField == null
            || NinjaRotation.LastVerifiedActionField == null
            || NinjaRotation.AepMatchedField == null
            || NinjaRotation.RetryCountField == null
            || NinjaRotation.StuckTimerField == null
            || NinjaRotation.RetryTimerField == null)
            throw new MissingFieldException("ActionUpdater verification fields changed");
        NinjaRotation.LastAttemptedActionField?.SetValue(null, null);
        NinjaRotation.LastVerifiedActionField?.SetValue(null, null);
        NinjaRotation.AepMatchedField?.SetValue(null, false);
        NinjaRotation.RetryCountField?.SetValue(null, 0);
        (NinjaRotation.StuckTimerField?.GetValue(null) as Stopwatch)?.Reset();
        (NinjaRotation.RetryTimerField?.GetValue(null) as Stopwatch)?.Reset();
    }

    private static bool HasVerificationMismatch()
    {
        try
        {
            var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
            // A pending verification without an active command is also stale:
            // the updater has no command left that can consume the result.
            if (attempted != null && active == null) return true;
            if (active == null || attempted == null) return false;
            if (active.ActionId == attempted.ActionId && active.Type == attempted.Type) return false;

            // Upgraded action IDs are valid verification equivalents. Only
            // reset when both the action kind and adjusted ID disagree.
            var attemptedAdjusted = ActionHelper.GetAdjustedActionId(attempted.ActionId);
            var activeAdjusted = ActionHelper.GetAdjustedActionId(active.ActionId);
            var attemptedVerificationId = attemptedAdjusted == 0 ? attempted.ActionId : attemptedAdjusted;
            var activeVerificationId = activeAdjusted == 0 ? active.ActionId : activeAdjusted;
            return active.Type != attempted.Type || activeVerificationId != attemptedVerificationId;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasActiveOffGcdCommand()
    {
        try
        {
            var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            return active?.Type == ActionType.OffGcd;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsActiveActionGroup()
    {
        try
        {
            var command = NinjaRotation.ActiveCommandField?.GetValue(null);
            return command?.GetType().Name == "ActionGroupCommand";
        }
        catch
        {
            return false;
        }
    }
    public void OnBattleEnded()
    {
        NinjaAeSlotSequence.Reset();
        NinjaCombatState.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        NinjaAeSlotSequence.Reset();
        NinjaCombatState.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnNoTarget()
    {
        ErosUIFramework.RestoreQtIfHostCleared();
        TryPrepareSuiton();
    }

    private static void TryPrepareSuiton()
    {
        if (PtlManager.State == PtlState.Running || TimelineEngine.Instance.IsRunning) return;
        var me = Core.Me;
        if (me != null
            && !me.HasStatus(2839u)
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && !PromeSettings.Instance.GetQt(NinjaQts.PrepareSuiton)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 3f
            && !ActionUpdaterRouter.HasActiveCommand())
            PromeSettings.Instance.SetQt(NinjaQts.PrepareSuiton, true);
    }
}
