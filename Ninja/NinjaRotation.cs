using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using Dalamud.Game.Command;
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
using ECommons.DalamudServices;

using Ninja.PR.Action;
using Ninja.PR.Action.Gcd;
using Ninja.PR.Action.OffGcd;
using Ninja.PR.Data;
using Ninja.PR.Helper;
using Ninja.PR.Opener;
using Ninja.PR.UI;

namespace Ninja.PR;

[RotationMetadata(30u, "Luli Ninja PR", "Cino", "1.1.3", ContentScope = AcrContentScope.HighEnd)]
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
    internal static long staleGcdLockSince;

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
        [NinjaQts.RaitonWhenOutOfRange] = false,
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
        [NinjaOpener.StandardOpenerName] = typeof(NinjaStandardOpener)
    };

    public static IJobNodeProvider? NodeProvider { get; } = new NinjaJobNodeProvider();
    // GCD priority. The pre-pull Suiton resolver runs before TCJ follow-ups.
    private readonly List<IDecisionResolver> gcd = new() { new NinjaAlwaysResolver(), new NinjaTcjResolver(), new NinjaKassatsuJutsuResolver(), new NinjaJutsuResolver(), new NinjaEarlier4Or3Resolver(), new NinjaKBaseResolver(), new NinjaPhantomResolver(), new NinjaRaijuResolver(), new NinjaRaitonResolver(), new NinjaComboResolver() };
    // Always resolvers run independently from the oGCD queue. Preserve order.
    private readonly List<IDecisionResolver> always;
    private readonly List<IDecisionResolver> ogcd;
    private readonly NinjaTenriJindoResolver tenriJindo = new();
    private readonly NinjaTrickAttackResolver trickAttack = new();
    private readonly NinjaPriorityBhavacakraResolver priorityBhavacakra;
    private readonly NinjaEventHandler events = new();
    private bool commandRegistered;

    public NinjaRotation()
    {
        priorityBhavacakra = new NinjaPriorityBhavacakraResolver();
        always = new() { new NinjaKassatsuResolver(), trickAttack };
        // luli order for the oGCD slots:
        // _100B, Mug, Bunshin, Ten Chi Jin, Meisui, Tenri Jindo,
        // Assassinate, Bhavacakra, True North. Kassatsu and Trick Attack
        // remain in the Always list above.
        ogcd = new() { priorityBhavacakra, new NinjaMugResolver(), new NinjaBunshinResolver(), new NinjaTenChiJinResolver(), new NinjaMeisuiResolver(), tenriJindo, new NinjaAssassinateResolver(), new NinjaOgcdResolver(IsComboResolverFirst, IsRaijuResolverFirst), new NinjaTrueNorthResolver(IsComboResolverFirst) };
        ErosUIFramework.Configure(
            jobTag: "NIN",
            jobName: "Ninja",
            qtAll: QtList,
            qtIsMetaKey: _ => false,
            qtIsVisibleInMode: (key, _) => !HiddenQtKeys.Contains(key),
            qtDefault: GetQtDefault,
            qtCascadeRules: new Dictionary<string, (string key, bool invert)[]>(),
            hotkeyNames: new[] { "缩地" },
            buildHotkeys: b =>
            {
                b.Execute("缩地", new NinjaMovementHotkey(), iconActionID: NinjaActions.Shukuchi);
            },
            author: "Cino",
            drawGeneralSettings: DrawGeneralSettings,
            drawDebugSettings: DrawDebugPanel);
        foreach (var qt in QtList)
        {
            PromeSettings.Instance.AddQt(qt.Key, GetQtDefault(qt.Key));
        }

        // Preserve the old NinjaSettings flag once, then let the QT own the
        // setting so its visible state and the resolver cannot diverge.
        if (NinjaSettings.Instance.RaitonWhenOutOfRange)
        {
            PromeSettings.Instance.SetQt(NinjaQts.RaitonWhenOutOfRange, true);
            NinjaSettings.Instance.RaitonWhenOutOfRange = false;
            NinjaSettings.Instance.Save();
        }
    }

    public PAction? NextGcd()
    {
        // While TCJ owns the GCD, no ordinary resolver may consume the slot.
        // This also covers resolvers whose individual checks do not need a
        // TCJ condition because the scheduler owns the GCD while TCJ is active.
        if (NinjaCombatState.IsTcjSequenceActive)
            return NextTcjOnly();

        // Evaluate the ready Tenri Jindo before allowing a normal GCD to
        // occupy the queue. It is especially time-sensitive: if it
        // is ready and the 600ms weave gate is open, queueing a GCD first can
        // leave 36961 behind that GCD for a full cycle.
        if (tenriJindo.Check().Success)
            return null;
        return Next(gcd);
    }

    public PAction? NextOffGcd()
    {
        // Do not weave unrelated abilities during Ten Chi Jin. Keeping this
        // gate at the scheduler boundary prevents a missed per-resolver
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
        foreach (var resolver in GetResolverPriority())
        {
            if (resolver.Check().Success) return resolver is NinjaComboResolver;
        }
        return false;
    }

    private bool IsRaijuResolverFirst()
    {
        foreach (var resolver in GetResolverPriority())
        {
            if (resolver.Check().Success) return resolver is NinjaRaijuResolver;
        }
        return false;
    }

    private IEnumerable<IDecisionResolver> GetResolverPriority()
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
            NinjaOpener.StandardOpenerName => new NinjaStandardOpener(),
            _ => null
        };
    }

    public void DrawSettings()
    {
        ErosUIFramework.DrawSettingsEntry();
    }

    private static void DrawGeneralSettings()
    {
        ImGui.Text("起手设置");
        var selected = NinjaSettings.Instance.Opener;
        if (ImGui.BeginCombo("起手选择", selected))
        {
            foreach (var option in new[] { NinjaOpener.StandardOpenerName, "无" })
            {
                var isSelected = selected == option;
                if (ImGui.Selectable(option, isSelected))
                {
                    NinjaSettings.Instance.Opener = option;
                    NinjaSettings.Instance.Save();
                }
                if (isSelected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        var openerBurst = NinjaSettings.Instance.OpenerBurst;
        if (ImGui.Checkbox("起手使用爆发药", ref openerBurst))
        {
            NinjaSettings.Instance.OpenerBurst = openerBurst;
            NinjaSettings.Instance.Save();
        }
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
        ImGui.Text($"TCJ Suiton recent 15s: {NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000)}");
        ImGui.Separator();
        ImGui.Text($"GCD gate: {(NinjaCombatState.IsTcjSequenceActive ? "TCJ exclusive" : "normal resolvers")}");
        ImGui.Text($"oGCD gate: {(NinjaCombatState.IsTcjSequenceActive ? "blocked by TCJ" : "open")}");
        ImGui.Text($"Always gate: {(NinjaCombatState.IsTcjSequenceActive ? "blocked by TCJ" : "open")}");
        ImGui.Text($"Active command: {ActionUpdater.HasActiveCommand()}");
        ImGui.Text($"Locked GCD: {ActionUpdater.HasLockedGcdAction()}");
        ImGui.Text($"Updater recovery: {LastUpdaterRecovery}");
        ImGui.Text($"Stale GCD lock age: {NinjaEventHandler.GetStaleGcdLockAgeMs()} ms");
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
        RegisterCommands();
        ErosUIFramework.Install();
    }

    public void OnExitAcr()
    {
        events.Dispose();
        UnregisterCommands();
        ErosUIFramework.Uninstall();
    }

    private void RegisterCommands()
    {
        if (commandRegistered) return;
        Svc.Commands.AddHandler("/lldd", new CommandInfo(HandleLldd)
        {
            HelpMessage = "使用忍者缩地"
        });
        commandRegistered = true;
    }

    private void UnregisterCommands()
    {
        if (!commandRegistered) return;
        Svc.Commands.RemoveHandler("/lldd");
        commandRegistered = false;
    }

    private static void HandleLldd(string command, string args) => NinjaMovementHotkey.TryUse();
}

internal sealed class NinjaEventHandler : IRotationEventHandler, IDisposable
{
    private bool attached;

    public void Attach()
    {
        if (attached) return;
        // Advance the TCJ step from the cast-start event; keep action-effect
        // recording as
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
        if (IsTenChiJinEvent(action.ActionId))
        {
            // A GCD decision queued immediately before TCJ must not run in
            // the exclusive TCJ window. The opener's TCJ is the terminal
            // action, so clearing normal queues cannot remove its follow-up.
            // If ActionUpdater already promoted a normal Group to its active
            // command, clearing queues alone is too late; drop that command
            // before the TCJ resolver takes exclusive GCD ownership.
            // TCJ is an instant action. If the host selected it through the
            // verified path, the client may not emit a matching verification
            // id, leaving 7403 as the active command after TCJ has finished.
            // Reset the updater at the confirmed cast boundary so the next
            // TCJ jutsu or the post-TCJ oGCD can be selected.
            ActionUpdaterRouter.Reset();
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
        if (IsTenChiJinEvent(action.ActionId))
        {
            // Some client paths publish the action effect without a matching
            // start-cast event. Apply the same queue handoff in that path.
            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
        }
        NinjaCombatState.Record(action.ActionId);
        // Record both pre- and post-upgrade action aliases so an effect packet
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
        if (IsTenChiJinEvent(action.ActionId))
        {
            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
            NinjaCombatState.Record(action.ActionId);
            return;
        }
        if (action.ActionId is not (NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton
            or NinjaActions.TcjSuiton)
            && !NinjaCombatState.IsExpectedTcjActionAlias(action.ActionId)) return;
        // Some instant TCJ packets expose only the effect entry. Record it as
        // the same cast-success signal used by the full action-effect event.
        NinjaCombatState.Record(action.ActionId);
    }

    private static bool IsTenChiJinEvent(uint actionId)
    {
        if (actionId == NinjaActions.TenChiJin) return true;
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.TenChiJin);
        return adjusted != 0 && actionId == adjusted;
    }

    public void OnUpdate() => DropStaleOffGcdLock();
    public void OnOutOfBattleUpdate()
    {
        if (Core.Target == null) TryPrepareSuiton();
    }
    public void OnBattleStarted()
    {
        NinjaSequencePolicy.Reset();
        NinjaCombatState.Reset();
        // RotationManager enqueues the selected opener before invoking the
        // rotation event handler. Start the strict GCD policy after that
        // queue has been created, so the handler reset cannot erase it.
        if (PromeSettings.Instance.OpenerHasBeenExecuted)
            NinjaSequencePolicy.Begin(NinjaActions.TenChiJin);
    }
    public void OnBattleUpdate()
    {
        NinjaSequencePolicy.Update();
        DropStaleTcjBeforeTenriJindo();
        DropStaleOffGcdLock();
    }

    private static void DropStaleTcjBeforeTenriJindo()
    {
        var me = Core.Me;
        if (me?.HasStatus(NinjaActions.TenriJindoReady) != true
            || me.HasStatus(NinjaActions.TcjStatus)) return;

        try
        {
            var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            var staleActive = active != null && IsTenChiJinAction(active.ActionId);
            var staleQueued = false;
            foreach (var queue in ActionQueueManager.GetQueueStatus().Values)
            {
                foreach (var command in queue)
                {
                    if (command.Contains($": {NinjaActions.TenChiJin}", StringComparison.Ordinal))
                    {
                        staleQueued = true;
                        break;
                    }
                }
                if (staleQueued) break;
            }

            if (!staleActive && !staleQueued) return;

            // 3851 belongs to the post-TCJ Tenri Jindo window. Any remaining
            // 7403 command was created before that window and must not delay
            // 36961. Rebuild the normal queues on the next rotation pulse.
            ActionUpdaterRouter.Reset();
            NinjaSequencePolicy.Reset();
            NinjaRotation.LastUpdaterRecovery = "removed stale TCJ before Tenri Jindo";
        }
        catch
        {
            // Queue inspection is a compatibility cleanup path only.
        }
    }

    private static void DropStaleOffGcdLock()
    {
        if (TryRecoverCompletedTcjCommand()) return;

        if (HasVerificationMismatch())
        {
            // A previous verification can remain while the updater has
            // already selected a different current action. Clear only that
            // stale verification so the selected action/group stays intact.
            RecoverStaleVerification(preserveQueues: !ActionUpdaterRouter.HasLockedGcdAction());
            return;
        }

        if (TryRecoverStaleGcdLock()) return;

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

    private static bool TryRecoverCompletedTcjCommand()
    {
        try
        {
            var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            if (active == null || active.Type != ActionType.OffGcd
                || !IsTenChiJinAction(active.ActionId)) return false;

            // Once TCJ has started its recast is running. If the 1186 status
            // is already gone, an old verified 7403 command cannot execute
            // again and must not block Tenri Jindo or the normal queue.
            if (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true
                || ActionHelper.GetActionCooldown(NinjaActions.TenChiJin) <= 0f)
                return false;

            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
            NinjaRotation.LastUpdaterRecovery = "reset completed TCJ command (7403)";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTenChiJinAction(uint actionId)
    {
        if (actionId == NinjaActions.TenChiJin) return true;
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.TenChiJin);
        return adjusted != 0 && actionId == adjusted;
    }

    private static bool TryRecoverStaleGcdLock()
    {
        if (!ActionUpdaterRouter.HasActiveCommand()
            || !ActionUpdaterRouter.HasLockedGcdAction()
            || ActionQueueManager.HasActionsInQueue()
            || ActionQueueManager.HasHighPriorityAction()
            || ActionHelper.GetGcdRemain() > 0.1f
            || IsActiveActionGroup())
        {
            NinjaRotation.staleGcdLockSince = 0;
            return false;
        }

        PAction? active;
        try
        {
            active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
        }
        catch
        {
            NinjaRotation.staleGcdLockSince = 0;
            return false;
        }

        // A legitimate GCD command is normally attempted immediately. This
        // narrow case is specifically the updater state seen when a command
        // survived without _lastActionAttempted being populated.
        var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
        if (active?.Type != ActionType.Gcd || attempted != null)
        {
            NinjaRotation.staleGcdLockSince = 0;
            return false;
        }

        var now = Environment.TickCount64;
        if (NinjaRotation.staleGcdLockSince == 0) NinjaRotation.staleGcdLockSince = now;
        if (now - NinjaRotation.staleGcdLockSince < 1000) return false;

        try
        {
            // SkipCurrentTask only drops the command for normal actions; the
            // updater's private _gcdLocked flag is cleared by Reset(). The
            // queue checks above make this reset safe for this recovery path.
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = $"reset stale GCD lock ({active.ActionId})";
        }
        catch
        {
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = $"reset stale GCD lock and queues ({active.ActionId})";
        }

        NinjaRotation.staleGcdLockSince = 0;
        return true;
    }

    internal static long GetStaleGcdLockAgeMs()
    {
        if (NinjaRotation.staleGcdLockSince == 0) return 0;
        return Math.Max(0, Environment.TickCount64 - NinjaRotation.staleGcdLockSince);
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
        NinjaSequencePolicy.Reset();
        NinjaCombatState.Reset();
        NinjaRotation.staleGcdLockSince = 0;
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        NinjaSequencePolicy.Reset();
        NinjaCombatState.Reset();
        NinjaRotation.staleGcdLockSince = 0;
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
