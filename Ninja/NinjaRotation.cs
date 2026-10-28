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

[RotationMetadata(30u, "Luli Ninja PR", "Cino", "1.2.2", ContentScope = AcrContentScope.HighEnd)]
public sealed class NinjaRotation : IRotation, IRotationLifecycle
{
    private static readonly HashSet<string> HiddenQtKeys = new(StringComparer.Ordinal)
    {
        "倾斜资源", "斩杀", NinjaQts.PrepareSuiton,
        NinjaQts.Raiju, NinjaQts.Bunshin, NinjaQts.Phantom,
        NinjaQts.Bhavacakra, NinjaQts.AutoTrueNorth, NinjaQts.TenriJindo,
        NinjaQts.Assassinate, NinjaQts.LongArm, NinjaQts.RaitonWhenOutOfRange
    };
    private const string LegacyPositionalQt = "身位提示";
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
    internal static string LastUpdaterRecovery = "无";
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
        [NinjaQts.Assassinate] = true,
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
        [NinjaQts.LongArm] = false,
        ["倾斜资源"] = false,
        ["斩杀"] = false,
        [NinjaQts.PrepareSuiton] = false
    };

    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        [NinjaOpener.StandardOpenerName] = typeof(NinjaStandardOpener),
        [NinjaOpener.NoLoss9GOpenerName] = typeof(NinjaNoLoss9GOpener),
        [NinjaOpener.EdenOpenerName] = typeof(NinjaEdenOpener)
    };

    public static IJobNodeProvider? NodeProvider { get; } = new NinjaJobNodeProvider();
    // GCD priority. The pre-pull Suiton resolver runs before TCJ follow-ups.
    private readonly NinjaPhantomResolver phantom = new();
    private readonly List<IDecisionResolver> gcd;
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
        // Keep the AE GCD order. The Phantom resolver is also checked by the
        // expiry safeguard in NextGcd before this normal list is evaluated.
        gcd = new()
        {
            new NinjaAlwaysResolver(), new NinjaTcjResolver(),
            new NinjaKassatsuJutsuResolver(), new NinjaJutsuResolver(),
            new NinjaEarlier4Or3Resolver(), new NinjaKBaseResolver(), phantom,
            new NinjaRaijuResolver(), new NinjaRaitonResolver(), new NinjaComboResolver()
        };
        // luli order for the oGCD slots:
        // _100B, Mug, Bunshin, Ten Chi Jin, Meisui, Tenri Jindo,
        // Assassinate, Bhavacakra, True North. Kassatsu and Trick Attack
        // remain in the Always list above.
        ogcd = new() { priorityBhavacakra, new NinjaMugResolver(), new NinjaBunshinResolver(), new NinjaTenChiJinResolver(), new NinjaMeisuiResolver(), tenriJindo, new NinjaAssassinateResolver(), new NinjaOgcdResolver(IsComboResolverFirst, IsRaijuResolverFirst), new NinjaTrueNorthResolver(IsComboResolverFirst) };
        MigrateLegacyPositionalQt();
        ErosUIFramework.Configure(
            jobTag: "NIN",
            jobName: "Ninja",
            qtAll: QtList,
            qtIsMetaKey: _ => false,
            qtIsVisibleInMode: (key, _) => !HiddenQtKeys.Contains(key),
            qtDefault: GetQtDefault,
            qtCascadeRules: new Dictionary<string, (string key, bool invert)[]>(),
            hotkeyNames: new[] { "\u7f29\u5730", "\u80cc\u5bf9\u6444\u50cf\u673a\u7f29\u5730", "\u5f3a\u5236\u96f7\u9041" },
            buildHotkeys: b =>
            {
                b.Execute("\u7f29\u5730", new NinjaMovementHotkey(), iconActionID: NinjaActions.Shukuchi);
                b.Execute("\u80cc\u5bf9\u6444\u50cf\u673a\u7f29\u5730", new NinjaMovementHotkey(true), iconActionID: NinjaActions.Shukuchi);
                b.Execute("\u5f3a\u5236\u96f7\u9041", new NinjaRaitonHotkey(), iconActionID: NinjaActions.Raiton);
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
        if (NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions)
            return null;

        // A normal mudra sequence owns the GCD until 496 disappears. The
        // queued Ten/Chi/Jin actions are consumed by ActionUpdater; no other
        // ninjutsu resolver may start a second sequence in the same window.
        if (NinjaSequencePolicy.IsNormalMudraActive)
            return null;

        // Do not let an earlier normal GCD resolver consume the slot when the
        // 2723 proc would expire before the next available GCD.
        if (phantom.IsExpiringSoon())
            return phantom.GetAction();

        // Evaluate the ready Tenri Jindo before allowing a normal GCD to
        // occupy the queue. It is especially time-sensitive: if it
        // is ready and the 600ms weave gate is open, queueing a GCD first can
        // leave 36961 behind that GCD for a full cycle.
        if (tenriJindo.Check().Success)
            return null;

        // During the Kunai's Bane window, a ready Phantom Kamaitachi must be
        // selected before the normal combo. KBase is intentionally earlier in
        // the AE resolver list, so keep this exception at the scheduler edge.
        if (Core.Target?.HasStatus(NinjaActions.KunaisBaneStatus) == true
            && phantom.Check().Success)
            return phantom.GetAction();
        return Next(gcd);
    }

    public PAction? NextOffGcd()
    {
        // Do not weave unrelated abilities during Ten Chi Jin. Keeping this
        // gate at the scheduler boundary prevents a missed per-resolver
        // condition from interrupting the three ninjutsu sequence.
        if (NinjaCombatState.IsTcjSequenceActive
            || NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions
            || !NinjaCombatState.CanUsePostNinjutsuOffGcd
            || !NinjaCombatState.CanUseGcdOffGcd)
            return null;
        return Next(ogcd);
    }
    public PAction? NextAlways()
    {
        // TCJ owns all action slots until its third transformed jutsu is
        // confirmed. This prevents an Always resolver from stealing the
        // window between the three GCD packets.
        if (NinjaCombatState.IsTcjSequenceActive
            || NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions
            || !NinjaCombatState.CanUsePostNinjutsuOffGcd
            || !NinjaCombatState.CanUseGcdOffGcd)
            return null;
        return Next(always);
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
            NinjaOpener.NoLoss9GOpenerName => new NinjaNoLoss9GOpener(),
            NinjaOpener.EdenOpenerName => new NinjaEdenOpener(),
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
            foreach (var option in new[] { NinjaOpener.StandardOpenerName, NinjaOpener.NoLoss9GOpenerName, NinjaOpener.EdenOpenerName, "无" })
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

        ImGui.SameLine(0f, 24f);
        var positional = NinjaSettings.Instance.Positional;
        if (ImGui.Checkbox("身位提示", ref positional))
        {
            NinjaSettings.Instance.Positional = positional;
            NinjaSettings.Instance.Save();
        }

        var extraRange = NinjaSettings.Instance.ExtraRange;
        if (ImGui.DragFloat("长臂猿额外攻击距离", ref extraRange, 0.1f, 0f, 3f, "%.1f m"))
        {
            NinjaSettings.Instance.ExtraRange = Math.Clamp(extraRange, 0f, 3f);
            NinjaSettings.Instance.Save();
            NinjaHooks.Update();
        }

    }

    private static bool GetQtDefault(string key)
        => QtList.TryGetValue(key, out var value) && value;

    private static void MigrateLegacyPositionalQt()
    {
        if (!PromeSettings.Instance.QuickToggles.TryGetValue(LegacyPositionalQt, out var enabled))
            return;

        NinjaSettings.Instance.Positional = enabled;
        PromeSettings.Instance.QuickToggles.Remove(LegacyPositionalQt);
        NinjaSettings.Instance.Save();
    }

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
        ImGui.Text($"天地人状态：{NinjaCombatState.IsTcjSequenceActive}");
        ImGui.Text($"最近天地人忍术：{FormatDebugAction(NinjaCombatState.GetLastTcjStep())}");
        var pendingTcj = NinjaCombatState.GetPendingTcjForDebug();
        ImGui.Text($"待提交天地人忍术：{FormatDebugAction(pendingTcj)}{(pendingTcj == 0 ? string.Empty : $"（{NinjaCombatState.PendingTcjAgeMs} 毫秒）")}");
        ImGui.Text($"最近观测技能：{NinjaAttemptDebug.FormatAction(NinjaCombatState.LastObservedActionId)}（{NinjaCombatState.LastObservedActionId}）");
        ImGui.Text($"最近 GCD：{FormatDebugAction(NinjaCombatState.LastGcd)}");
        ImGui.Text($"当前反复尝试：{NinjaAttemptDebug.FormatCurrent()}");
        ImGui.Text($"尝试类型/重试次数：{NinjaAttemptDebug.FormatType()} / {NinjaAttemptDebug.RetryCount}");
        ImGui.Text($"尝试持续时间：{(NinjaAttemptDebug.CurrentAgeMs < 0 ? "无" : $"{NinjaAttemptDebug.CurrentAgeMs} 毫秒")}");
        ImGui.Text($"连续观察次数：{NinjaAttemptDebug.ConsecutiveObservations}");
        ImGui.Text($"尝试技能历史：{NinjaAttemptDebug.History}");
        ImGui.Text($"无损起手尾段：{NinjaNoLossOpenerRuntime.DebugStage}");
        ImGui.Text($"忍术后能力技：{NinjaCombatState.PostNinjutsuOffGcdUsed}/{NinjaCombatState.PostNinjutsuOffGcdLimit}");
        ImGui.Text($"每 GCD 能力技：{NinjaCombatState.GcdOffGcdUsed}/{NinjaCombatState.MaxGcdOffGcd}");
        ImGui.Text($"天地人水遁最近 15 秒：{NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000)}");
        ImGui.Separator();
        ImGui.Text($"搓兔子次数：{NinjaMudraDebug.RabbitCount}");
        ImGui.Text($"最近搓兔子时间：{(NinjaMudraDebug.LastRabbitAgeMs < 0 ? "无" : $"{NinjaMudraDebug.LastRabbitAgeMs} 毫秒前")}");
        ImGui.Text($"搓兔子原因：{NinjaMudraDebug.LastRabbitReason}");
        ImGui.Text($"最近忍术印输入：{NinjaMudraDebug.RecentInputs}");
        ImGui.Text($"忍术印策略：{(NinjaSequencePolicy.IsActive ? $"终结技能 {NinjaSequencePolicy.TerminalAction}" : "未激活")}");
        ImGui.Separator();
        ImGui.Text($"GCD 拦截：{(NinjaCombatState.IsTcjSequenceActive ? "天地人独占" : "常规解析器")}");
        ImGui.Text($"能力技拦截：{(NinjaCombatState.IsTcjSequenceActive ? "天地人拦截" : "开放")}");
        ImGui.Text($"Always 拦截：{(NinjaCombatState.IsTcjSequenceActive ? "天地人拦截" : "开放")}");
        ImGui.Text($"当前活动命令：{ActionUpdater.HasActiveCommand()}");
        ImGui.Text($"GCD 锁定：{ActionUpdater.HasLockedGcdAction()}");
        ImGui.Text($"调度器恢复：{LastUpdaterRecovery}");
        ImGui.Text($"过期 GCD 锁定时长：{NinjaEventHandler.GetStaleGcdLockAgeMs()} 毫秒");
        ImGui.Text($"队列中有技能：{ActionQueueManager.HasActionsInQueue()}");
        ImGui.Separator();
        ImGui.Text($"当前 Always 解析器：{FirstActive(RotationManager.AlwaysSolverStatus)}");
        ImGui.Text($"当前 GCD 解析器：{FirstActive(RotationManager.GcdSolverStatus)}");
        ImGui.Text($"当前能力技解析器：{FirstActive(RotationManager.OffGcdSolverStatus)}");
        ImGui.Separator();
        DrawDebugGroup("常规能力技", RotationManager.AlwaysSolverStatus);
        DrawDebugGroup("GCD", RotationManager.GcdSolverStatus);
        DrawDebugGroup("能力技", RotationManager.OffGcdSolverStatus);
        if (ImGui.CollapsingHeader("调度器/队列拦截器"))
            DrawSchedulerDebug();
    }

    private static string FormatDebugAction(uint id) => id switch
    {
        NinjaActions.TcjFumaShuriken => "天地人风魔手里剑（18873）",
        NinjaActions.TcjRaiton => "天地人雷遁（18877）",
        NinjaActions.TcjSuiton => "天地人水遁（18881）",
        0 => "无",
        _ => id.ToString()
    };

    private static void DrawDebugGroup(string name, IList<SolverStatus> statuses)
    {
        ImGui.Text(name);
        foreach (var status in statuses)
            ImGui.Text($"  {(status.Success ? "生效" : "拦截")}  {FormatResolverName(status.Name)}：{status.Message}");
    }

    private static string FormatResolverName(string name) => name switch
    {
        "NinjaKassatsuResolver" => "生杀夺予",
        "NinjaTrickAttackResolver" => "背刺",
        "NinjaAlwaysResolver" => "预备水遁",
        "NinjaTcjResolver" => "天地人忍术",
        "NinjaKassatsuJutsuResolver" => "生杀冰晶",
        "NinjaJutsuResolver" => "常规忍术",
        "NinjaEarlier4Or3Resolver" => "早期忍术",
        "NinjaKBaseResolver" => "基础连击优先",
        "NinjaPhantomResolver" => "镰鼬",
        "NinjaRaijuResolver" => "月影雷兽牙",
        "NinjaRaitonResolver" => "雷遁",
        "NinjaComboResolver" => "基础连击",
        "NinjaPriorityBhavacakraResolver" => "六道轮回优先",
        "NinjaMugResolver" => "夺取",
        "NinjaBunshinResolver" => "分身之术",
        "NinjaTenChiJinResolver" => "天地人",
        "NinjaMeisuiResolver" => "命水",
        "NinjaTenriJindoResolver" => "天理人道",
        "NinjaAssassinateResolver" => "梦幻三段",
        "NinjaOgcdResolver" => "常规能力技",
        "NinjaTrueNorthResolver" => "真北",
        _ => name.StartsWith("Ninja", StringComparison.Ordinal) && name.EndsWith("Resolver", StringComparison.Ordinal)
            ? name[5..^8]
            : name
    };

    private static string FirstActive(IList<SolverStatus> statuses)
    {
        foreach (var status in statuses)
            if (status.Success) return FormatResolverName(status.Name);
        return "无";
    }

    private static void DrawSchedulerDebug()
    {
        ImGui.Text("技能调度器");
        foreach (var entry in ActionUpdater.DebugInfo)
            ImGui.Text($"  {entry.Key}: {entry.Value}");

        ImGui.Text("技能队列");
        foreach (var queue in ActionQueueManager.GetQueueStatus())
        {
            var actions = queue.Value.Count == 0 ? "空" : string.Join(", ", queue.Value);
            ImGui.Text($"  {queue.Key}: {actions}");
        }
    }

    public void DrawQTs() { }
    public IRotationEventHandler GetEventHandler() => events;
    public void OnEnterAcr()
    {
        NinjaWeavePolicy.Enter();
        events.Attach();
        NinjaActionInputGuard.Attach();
        NinjaHooks.Update();
        RegisterCommands();
        ErosUIFramework.Install();
    }

    public void OnExitAcr()
    {
        NinjaPositionDrawer.ResetLifecycle();
        NinjaWeavePolicy.Exit();
        NinjaHooks.Dispose();
        NinjaActionInputGuard.Dispose();
        events.Dispose();
        UnregisterCommands();
        ErosUIFramework.Uninstall();
    }

    private void RegisterCommands()
    {
        if (commandRegistered) return;
        Svc.Commands.AddHandler("/lldd", new CommandInfo(HandleLldd)
        {
            HelpMessage = "Use Shukuchi; add 'bd' for the direction opposite the camera"
        });
        commandRegistered = true;
    }

    private void UnregisterCommands()
    {
        if (!commandRegistered) return;
        Svc.Commands.RemoveHandler("/lldd");
        commandRegistered = false;
    }

    private static void HandleLldd(string command, string args)
    {
        if (args.Trim().Equals("bd", StringComparison.OrdinalIgnoreCase))
            NinjaMovementHotkey.TryUseOppositeCamera();
        else
            NinjaMovementHotkey.TryUse();
    }
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
        RecoverManualIntervention(action.ActionId);
        // StartCast is the UAL-side signal. General actions are confirmed
        // through their effect packet below; normal mudra inputs get a
        // separate one-shot acknowledgement immediately after this call.
        ActionUpdaterRouter.NotifyUAL(action.ActionId);
        // Normal mudra actions are accepted at StartCast, while their AEP is
        // not emitted consistently by every client path. Confirm only these
        // six inputs here so the generic AEP retry loop cannot submit the same
        // mudra again after its 500ms recast. The terminal ninjutsu keeps the
        // normal effect-based verification below.
        if (NinjaSequencePolicy.IsNormalMudraSequenceActive
            && IsNormalMudraInput(action.ActionId))
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
        // The cast packet arrives before the action-effect packet on some
        // client paths. Reserve the single post-ninjutsu weave immediately
        // so a delayed effect packet cannot allow a second ability in the
        // same GCD.
        NinjaCombatState.RecordNinjutsuStarted(action.ActionId);
    }

    private static bool IsNormalMudraInput(uint actionId) => actionId is
        NinjaActions.Ten or NinjaActions.Chi or NinjaActions.Jin
        or NinjaActions.TenCombo or NinjaActions.ChiCombo or NinjaActions.JinCombo;

    private static void OnActionEffect(ActionEffectEvent action)
    {
        if (action.SourceId != Core.Me?.EntityId) return;
        // ActionEffect is the completion-side signal. Manual interruption is
        // handled from StartCast only; an effect can arrive later for an old
        // action while the updater is already attempting the next one.
        ActionUpdaterRouter.NotifyAEP(action.ActionId);
        if (IsTenChiJinEvent(action.ActionId))
        {
            // Some client paths publish the action effect without a matching
            // start-cast event. Apply the same queue handoff in that path.
            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
        }
        NinjaCombatState.Record(action.ActionId);
        NinjaCombatState.RecordConfirmed(action.ActionId);
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
        // Entry events are effect fragments, not a second action submission.
        // Feeding UAL here can create a false verification and advance a
        // sequence twice when both event forms are emitted by the client.
        ActionUpdaterRouter.NotifyAEP(action.ActionId);
        // AE's AfterSpell callback applies the ability budget for every
        // confirmed self action. Entry events are a valid fallback path for
        // normal Raiton/Suiton/Hyosho packets as well as TCJ aliases, so feed
        // the same counter before the TCJ-only sequence filter below.
        NinjaCombatState.RecordConfirmed(action.ActionId);
        if (action.ActionId == NinjaActions.Rabbit)
        {
            NinjaCombatState.Record(action.ActionId);
            return;
        }
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

    private static void RecoverManualIntervention(uint actionId)
    {
        // Outside a normal mudra group, a mismatching effect is commonly a
        // delayed packet from the previous GCD. Reaping/Ninja normal actions
        // must not be treated as manual interruption in that state.
        if (!NinjaSequencePolicy.IsNormalMudraActive) return;
        try
        {
            var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
            var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
            // The current command is the authoritative rotation-owned action.
            // A stale LastAttemptedAction from the preceding oGCD must not
            // cancel a newly-created mudra queue.
            if (active != null && ActionIdsMatch(actionId, active.ActionId)) return;
            if (attempted == null || attempted.Type != ActionType.Gcd) return;
            if (active == null || active.Type != ActionType.Gcd) return;
            if (ActionIdsMatch(actionId, attempted.ActionId)) return;

            // An action from the player arrived while the updater was waiting
            // for a different action. Treat it as an intentional interruption
            // and abandon the normal mudra group instead of leaving a stale
            // UAL verification state with no queue to consume it.
            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
            NinjaSequencePolicy.Reset();
            NinjaRotation.LastUpdaterRecovery =
                $"手动技能 {actionId} 中断了 {attempted.ActionId}";
        }
        catch
        {
            // Recovery is best effort; the normal stale-verification path
            // still runs on the next rotation update.
        }
    }

    private static bool ActionIdsMatch(uint observed, uint expected)
    {
        if (observed == expected) return true;
        var observedAdjusted = ActionHelper.GetAdjustedActionId(observed);
        var expectedAdjusted = ActionHelper.GetAdjustedActionId(expected);
        return observedAdjusted != 0 && observedAdjusted == expectedAdjusted;
    }

    public void OnUpdate()
    {
        NinjaHooks.Update();
        NinjaPositionDrawer.Update();
        NinjaAttemptDebug.Update();
        ConfirmSubmittedGcdFromRuntimeState();
        DropStaleOffGcdLock();
    }
    public void OnOutOfBattleUpdate()
    {
        NinjaHooks.Update();
        NinjaPositionDrawer.Update();
        if (Core.Target == null) TryPrepareSuiton();
    }
    public void OnBattleStarted()
    {
        NinjaSequencePolicy.Reset();
        NinjaCombatState.Reset();
        NinjaPositionDrawer.OnBattleStarted();
        // RotationManager enqueues the selected opener before invoking the
        // rotation event handler. Start the strict GCD policy after that
        // queue has been created, so the handler reset cannot erase it.
        if (PromeSettings.Instance.OpenerHasBeenExecuted)
            NinjaSequencePolicy.Begin(NinjaActions.TenChiJin);
        NinjaNoLossOpenerRuntime.OnBattleStarted();
    }
    public void OnBattleUpdate()
    {
        NinjaHooks.Update();
        NinjaAttemptDebug.Update();
        NinjaSequencePolicy.Update();
        NinjaNoLossOpenerRuntime.Update();
        ConfirmSubmittedGcdFromRuntimeState();
        DropStaleTcjBeforeTenriJindo();
        DropStaleOffGcdLock();
    }

    private static void ConfirmSubmittedGcdFromRuntimeState()
    {
        try
        {
            var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
            if (attempted?.Type != ActionType.Gcd) return;

            var me = Core.Me;
            if (me == null) return;

            // UAL/AEP packets can be missed for an instant GCD. Once the
            // client reports the matching cast or starts a new recast, the
            // submission itself is confirmed and the opener may advance.
            var castStarted = me.IsCasting && ActionIdsMatch(me.CastActionId, attempted.ActionId);
            var recastStarted = ActionHelper.GetGcdRemain() > 0.1f;
            if (!castStarted && !recastStarted) return;

            ActionUpdaterRouter.NotifyUAL(attempted.ActionId);
            ActionUpdaterRouter.NotifyAEP(attempted.ActionId);
        }
        catch
        {
            // Reflection is a compatibility bridge for ActionUpdater's
            // private verification state; normal event verification remains
            // the primary path when the host changes its internals.
        }
    }

    private static void DropStaleTcjBeforeTenriJindo()
    {
        var me = Core.Me;
        if (me?.HasStatus(NinjaActions.TenriJindoReady) != true
            || NinjaCombatState.HasTcjBuff) return;

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
            NinjaRotation.LastUpdaterRecovery = "已清除天理人道前的过期天地人命令";
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
            NinjaRotation.LastUpdaterRecovery = "已重置过期能力技锁定";
        }
        catch
        {
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = "已重置过期能力技锁定和队列";
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
            if (NinjaCombatState.HasTcjBuff
                || ActionHelper.GetActionCooldown(NinjaActions.TenChiJin) <= 0f)
                return false;

            ActionUpdaterRouter.Reset();
            ActionQueueManager.ClearNormalQueues();
            NinjaRotation.LastUpdaterRecovery = "已重置完成后的天地人命令（7403）";
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
            NinjaRotation.LastUpdaterRecovery = $"已重置过期 GCD 锁定（{active.ActionId}）";
        }
        catch
        {
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = $"已重置过期 GCD 锁定和队列（{active.ActionId}）";
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
            NinjaRotation.LastUpdaterRecovery = "已重置过期验证和队列";
            return;
        }

        // Remove only stale verifier bookkeeping. Keep the active command and
        // action group intact; its current action is the command that should
        // be retried after the previous verification is discarded.
        try
        {
            ClearVerificationState();
            NinjaRotation.LastUpdaterRecovery = "已清除过期验证，保留队列";
        }
        catch
        {
            // Reflection is only a compatibility bridge for PR's private
            // updater state. Fall back to the public reset if its internals
            // change in a future host build.
            ActionUpdaterRouter.Reset();
            NinjaRotation.LastUpdaterRecovery = "已重置过期验证和队列";
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
        NinjaPositionDrawer.ResetLifecycle();
        NinjaNoLossOpenerRuntime.Reset();
        NinjaSequencePolicy.Reset();
        NinjaCombatState.Reset();
        NinjaRotation.staleGcdLockSince = 0;
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        NinjaPositionDrawer.ResetLifecycle();
        NinjaNoLossOpenerRuntime.Reset();
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
