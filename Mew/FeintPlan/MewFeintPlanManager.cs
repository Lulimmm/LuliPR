using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using LuliPR.Common;
using Mew.FeintPlan.Events;
using Mew.FeintPlan.Scripts.FRU;
using Mew.FeintPlan.Scripts.TOP;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.LogSystem;
using PromeRotation.Managers;
using PromeRotation.Timeline.Core;

namespace Mew.FeintPlan;

public static class MewFeintPlanManager
{
    private const uint FeintActionId = 7549u;
    private const uint DmuTerritoryId = 1363u;
    private const uint DmuP4UnitBaseId = 19510u;

    private static readonly Dictionary<uint, Dictionary<string, Dictionary<uint, FeintNodeInfo>>> NodesByZone = new();
    private static readonly Dictionary<uint, string> ZoneNames = new();
    private static readonly Dictionary<uint, List<string>> PhasesByZone = new();
    private static readonly HashSet<(uint ZoneId, string Phase, uint NodeId)> ErroredNodes = new();
    private static readonly HashSet<(uint ZoneId, string Phase, uint NodeId)> TriggeredNodes = new();
    private static readonly HashSet<(uint ZoneId, string Phase, uint NodeId)> FailedNodes = new();
    private static readonly HashSet<uint> CompletedZones = new();
    private static readonly Dictionary<uint, HashSet<string>> ActivePhasesByZone = new();
    private static readonly Dictionary<string, uint> OccurrenceCounts = new();
    private static readonly Dictionary<string, (int TimeMs, string Signature)> LastTriggerEvents = new();
    private static readonly HashSet<uint> SeenDmuUnits = new();
    private static readonly List<IDisposable> EventSubscriptions = new();

    private static bool attached;
    private static bool wasInCombat;
    private static uint runtimeZoneId;
    private static uint lastWeatherId = uint.MaxValue;
    private static uint lastAutoPresetZoneId = uint.MaxValue;
    private static string? lastAutoPresetName;
    private static long runtimeStartedAt;
    private static long lastSubscriptionAttemptAt;
    private static (ulong SourceId, uint ActionId, uint Sequence, DateTime Timestamp)? lastActionEffect;

    public static int ReceivedCastCount { get; private set; }
    public static int ReceivedEffectCount { get; private set; }
    public static uint LastCastActionId { get; private set; }
    public static uint LastEffectActionId { get; private set; }
    public static bool EventSourceConnected => EventSubscriptions.Count == 4;

    public static uint CurrentZoneId => Svc.ClientState.TerritoryType;
    private static bool IsRuntimeActive => GameData.IsInCombat()
        || Svc.Condition[ConditionFlag.DutyRecorderPlayback];

    public static void Attach()
    {
        if (attached) return;

        ResetRuntime();
        lastWeatherId = uint.MaxValue;
        attached = true;
        AttachEventSource();
    }

    public static void Dispose()
    {
        foreach (var subscription in EventSubscriptions)
            subscription.Dispose();
        EventSubscriptions.Clear();
        attached = false;
        ResetRuntime();
    }

    private static void AttachEventSource()
    {
        lastSubscriptionAttemptAt = Environment.TickCount64;
        var events = PromeRotation.Plugin.Instance?.LogSystem?.Events;
        if (events == null) return;

        var subscriptions = new List<IDisposable>();
        try
        {
            // This is the same public source used by PR's timeline conditions.
            subscriptions.Add(events.Subscribe<LogSystemCastStartEvent>(OnStartCast));
            subscriptions.Add(events.Subscribe<LogSystemActionEffectEvent>(OnActionEffect));
            subscriptions.Add(events.Subscribe<LogSystemObjectCreateEvent>(OnCreateObject));
            subscriptions.Add(events.Subscribe<LogSystemWeatherChangedEvent>(OnWeatherChanged));
            EventSubscriptions.AddRange(subscriptions);
            MewFeintPlanDebug.Print("已连接 PR LogSystem：读条、技能效果、对象创建和天气事件");
        }
        catch (Exception ex)
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();
            MewFeintPlanDebug.Error($"连接 PR 事件源失败：{ex.Message}");
        }
    }

    public static void ResetRuntime()
    {
        ErroredNodes.Clear();
        TriggeredNodes.Clear();
        FailedNodes.Clear();
        CompletedZones.Clear();
        ActivePhasesByZone.Clear();
        OccurrenceCounts.Clear();
        LastTriggerEvents.Clear();
        SeenDmuUnits.Clear();
        FRUData.ResetRuntime();
        TOPData.ResetRuntime();
        FeintHelper.Clear();
        lastWeatherId = uint.MaxValue;
        wasInCombat = false;
        runtimeZoneId = CurrentZoneId;
        runtimeStartedAt = Environment.TickCount64;
        ReceivedCastCount = 0;
        ReceivedEffectCount = 0;
        LastCastActionId = 0;
        LastEffectActionId = 0;
        lastActionEffect = null;
        // Recreate stateful nodes so counters from the previous pull are cleared.
        LoadAllNodes();
    }

    public static void Update()
    {
        if (!attached) return;
        if (!EventSourceConnected && Environment.TickCount64 - lastSubscriptionAttemptAt >= 1000)
            AttachEventSource();

        var zoneId = CurrentZoneId;
        if (zoneId != runtimeZoneId)
        {
            ResetRuntime();
            runtimeZoneId = zoneId;
            lastWeatherId = uint.MaxValue;
        }

        SyncAutomaticZonePreset(zoneId);

        if (!IsRuntimeActive)
        {
            if (wasInCombat) ResetRuntime();
            wasInCombat = false;
            return;
        }

        wasInCombat = true;
        var weather = GameData.Weather;
        if (weather != lastWeatherId)
        {
            lastWeatherId = weather;
            CheckTriggers(new WeatherChangedCondParams(weather));
        }

        if (zoneId == DmuTerritoryId)
        {
            foreach (var obj in Svc.Objects.OfType<IGameObject>())
            {
                if (obj.BaseId == DmuP4UnitBaseId && SeenDmuUnits.Add(obj.EntityId))
                    CheckTriggers(new UnitCreateCondParams(obj));
            }
        }

        CheckTriggers(null);
    }

    private static void SyncAutomaticZonePreset(uint zoneId)
    {
        var presetName = TimelineRoleManager.CurrentRole == TimelineRole.D2 ? "D2" : "D1";
        if (zoneId == lastAutoPresetZoneId
            && string.Equals(presetName, lastAutoPresetName, StringComparison.Ordinal))
            return;

        lastAutoPresetZoneId = zoneId;
        lastAutoPresetName = presetName;

        var preset = FeintPresets.GetPresets(zoneId)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, presetName, StringComparison.Ordinal));
        if (preset == null) return;

        var settings = AcrSettingsStore.Current.MewFeintPlan;
        var plan = settings.GetOrCreateZonePlan(zoneId);
        var nodeKeys = ConvertPresetNodesToKeys(zoneId, preset);
        if (nodeKeys.Count == 0) return;

        if (plan.Enabled && plan.EnabledNodes.SequenceEqual(nodeKeys, StringComparer.Ordinal)) return;

        plan.EnabledNodes.Clear();
        plan.EnabledNodes.AddRange(nodeKeys);
        plan.Enabled = true;
        AcrSettingsStore.Save();
    }

    public static IReadOnlyList<uint> GetAllZoneIds()
        => NodesByZone.Keys.OrderBy(id => ZoneNames.GetValueOrDefault(id, string.Empty)).ToArray();

    public static string GetZoneName(uint zoneId)
        => ZoneNames.GetValueOrDefault(zoneId, $"Zone {zoneId}");

    public static IReadOnlyList<string> GetZonePhases(uint zoneId)
        => PhasesByZone.TryGetValue(zoneId, out var phases) ? phases : Array.Empty<string>();

    public static IReadOnlyList<FeintNodeInfo> GetPhaseNodes(uint zoneId, string phase)
    {
        if (!NodesByZone.TryGetValue(zoneId, out var phases)
            || !phases.TryGetValue(phase, out var nodes))
            return Array.Empty<FeintNodeInfo>();

        return nodes.Values
            .OrderBy(node => node.DisplayOrder == 0 ? (long)node.Id : node.DisplayOrder)
            .ThenBy(node => node.Id)
            .ToArray();
    }

    public static List<string> ConvertPresetNodesToKeys(uint zoneId, FeintPreset preset)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var nodeName in preset.Nodes)
            AddNode(GetNodeInfoByName(zoneId, nodeName), nodeName);
        foreach (var node in preset.PhaseNodes)
        {
            var label = string.IsNullOrWhiteSpace(node.Phase) ? node.Name : $"{node.Phase} {node.Name}";
            AddNode(GetNodeInfoByPhaseAndName(zoneId, node.Phase, node.Name), label);
        }
        return result;

        void AddNode(FeintNodeInfo? info, string label)
        {
            if (info == null)
            {
                MewFeintPlanDebug.Print($"找不到节点 '{label}'");
                return;
            }

            var key = new FeintNodeKey(info.Phase, info.Id).ToString();
            if (seen.Add(key)) result.Add(key);
        }
    }

    public static void CleanupInvalidNodeIds(uint zoneId, ZoneFeintPlan plan)
    {
        if (!NodesByZone.TryGetValue(zoneId, out var phases)) return;

        var validKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (phase, nodes) in phases)
            foreach (var id in nodes.Keys)
                validKeys.Add(new FeintNodeKey(phase, id).ToString());

        var removed = plan.EnabledNodes.RemoveAll(key => !validKeys.Contains(key));
        if (removed > 0)
            MewFeintPlanDebug.Print($"已清理 {removed} 个无效节点");
    }

    private static void OnStartCast(LogSystemCastStartEvent action)
    {
        if (!IsRuntimeActive || Core.Me == null || action.SourceId == Core.Me.EntityId) return;
        var source = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(npc => npc.EntityId == action.SourceId);
        if (source == null
            || source.BattleNpcKind is not (BattleNpcSubKind.Combatant or BattleNpcSubKind.BNpcPart)
            || source.IsTargetable && !source.IsEnemy())
            return;

        // Encounter actors can cast while untargetable; pets, players and
        // friendly NPCs must not enter the enemy-cast counters or conditions.
        ReceivedCastCount++;
        LastCastActionId = action.ActionId;
        if (CurrentZoneId == FeintZoneConfig.ZoneIds["FRU"] && FRUData.IsPlanCast(action.ActionId))
            MewFeintPlanDebug.Print(GetRuntimeTimeMs() / 1000d, $"PR 收到读条 {action.ActionId}，读条时长 {action.CastTime:F1}s");
        CheckTriggers(new EnemyCastSpellCondParams(action.ActionId, action.CastTime));
    }

    private static void OnActionEffect(LogSystemActionEffectEvent action)
    {
        if (!IsRuntimeActive || Core.Me == null) return;
        // PR emits one record per affected target. Process each packet once.
        var packet = (action.SourceId, action.ActionId, action.GlobalSequence, action.Timestamp);
        if (lastActionEffect == packet) return;
        lastActionEffect = packet;
        ReceivedEffectCount++;
        LastEffectActionId = action.ActionId;
        if (action.SourceId == Core.Me.EntityId)
        {
            if (action.ActionId == FeintActionId)
                MewFeintPlanDebug.Print(GetRuntimeTimeMs() / 1000d, "PR 已确认牵制（7549）释放");
            CheckTriggers(new SelfActionEffectCondParams(action.ActionId));
            return;
        }
        if (CurrentZoneId == FeintZoneConfig.ZoneIds["FRU"]
            && action.ActionId is FRUData.伤害判定.Sinblaze or FRUData.伤害判定.Sinsmite)
            MewFeintPlanDebug.Print(GetRuntimeTimeMs() / 1000d, $"PR 收到伤害判定 {action.ActionId}");
        CheckTriggers(new ReceviceAbilityEffectCondParams(action.ActionId));
    }

    private static void OnCreateObject(LogSystemObjectCreateEvent action)
    {
        if (CurrentZoneId != DmuTerritoryId || !IsRuntimeActive) return;
        var obj = Svc.Objects.FirstOrDefault(candidate => candidate?.EntityId == action.SourceId);
        if (obj?.BaseId == DmuP4UnitBaseId && SeenDmuUnits.Add(obj.EntityId))
            CheckTriggers(new UnitCreateCondParams(obj));
        // Update also scans the object table when the spawn packet arrives
        // before Dalamud has published the new object.
    }

    private static void OnWeatherChanged(LogSystemWeatherChangedEvent action)
    {
        if (!IsRuntimeActive || lastWeatherId == action.WeatherId) return;
        lastWeatherId = action.WeatherId;
        CheckTriggers(new WeatherChangedCondParams(action.WeatherId));
    }

    private static int GetRuntimeTimeMs()
    {
        var seconds = EngageManager.GetBattleTime();
        if (seconds > 0d && double.IsFinite(seconds))
            return (int)Math.Min(int.MaxValue, seconds * 1000d);

        // The host clock can remain zero in duty playback or after loading an
        // ACR mid-fight. Keep diagnostic timestamps advancing in those cases.
        return (int)Math.Clamp(Environment.TickCount64 - runtimeStartedAt, 0L, int.MaxValue);
    }

    private static void LoadAllNodes()
    {
        NodesByZone.Clear();
        ZoneNames.Clear();
        PhasesByZone.Clear();
        var collected = new Dictionary<uint, Dictionary<string, List<(FeintNodeAttribute Attribute, IFeintNode Node)>>>();

        foreach (var type in typeof(MewFeintPlanManager).Assembly.GetTypes()
                     .Where(type => typeof(IFeintNode).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract))
        {
            try
            {
                var attributes = type.GetCustomAttributes<FeintNodeAttribute>().ToArray();
                if (attributes.Length == 0) continue;

                var folder = ExtractFolderFromNamespace(type.Namespace);
                if (folder == null || !FeintZoneConfig.ZoneIds.TryGetValue(folder, out var zoneId))
                {
                    MewFeintPlanDebug.Print($"找不到副本区域：{type.Name}");
                    continue;
                }

                if (Activator.CreateInstance(type) is not IFeintNode node) continue;
                ZoneNames.TryAdd(zoneId, folder);
                if (!collected.TryGetValue(zoneId, out var zone))
                    collected[zoneId] = zone = new(StringComparer.Ordinal);

                foreach (var attribute in attributes)
                {
                    if (!zone.TryGetValue(attribute.Phase, out var phase))
                        zone[attribute.Phase] = phase = new();
                    phase.Add((attribute, node));
                }
            }
            catch (Exception ex)
            {
                MewFeintPlanDebug.Error($"加载 {type.Name} 失败: {ex.Message}");
            }
        }

        foreach (var (zoneId, zone) in collected)
        {
            var phases = new Dictionary<string, Dictionary<uint, FeintNodeInfo>>(StringComparer.Ordinal);
            foreach (var (phaseName, entries) in zone)
            {
                var sorted = entries.OrderBy(entry => entry.Attribute.TimeInSec)
                    .ThenBy(entry => entry.Attribute.Order).ToArray();
                var nodes = new Dictionary<uint, FeintNodeInfo>();
                for (var index = 0; index < sorted.Length; index++)
                {
                    var (attribute, node) = sorted[index];
                    var id = (uint)(index + 1);
                    nodes[id] = new FeintNodeInfo
                    {
                        Id = id,
                        ZoneId = zoneId,
                        Name = attribute.Name,
                        Description = attribute.Description,
                        TimeInSec = attribute.TimeInSec,
                        Phase = phaseName,
                        Node = node,
                        SerialId = zoneId == FeintZoneConfig.ZoneIds["FRU"] && attribute.SerialId == 0
                            ? 1u : attribute.SerialId,
                        SequenceName = attribute.SequenceName,
                        DisplayOrder = attribute.DisplayOrder,
                        EnabledByDefault = attribute.EnabledByDefault,
                        BaseId = attribute.BaseId
                    };
                }
                phases[phaseName] = nodes;
            }

            NodesByZone[zoneId] = phases;
            PhasesByZone[zoneId] = FeintZoneConfig.HasPhaseEntryDetector(zoneId)
                ? phases.Keys.OrderBy(GetPhaseNumberOrder).ThenBy(name => name, StringComparer.Ordinal).ToList()
                : phases.Keys.OrderBy(GetPhaseOrder).ToList();
        }
    }

    private static string? ExtractFolderFromNamespace(string? ns)
    {
        const string prefix = "Mew.FeintPlan.Scripts.";
        if (string.IsNullOrEmpty(ns) || !ns.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var remainder = ns[prefix.Length..];
        var dot = remainder.IndexOf('.');
        return dot <= 0 ? remainder : remainder[..dot];
    }

    private static int GetPhaseOrder(string phase) => phase switch
    {
        "门神" => 0,
        "本体" => 1,
        "" => 2,
        _ => 3
    };

    private static int GetPhaseNumberOrder(string phase)
        => phase.Length > 1 && phase[0] == 'P' && int.TryParse(phase[1..], out var number) ? number : int.MaxValue;

    private static FeintNodeInfo? GetNodeInfoByName(uint zoneId, string name)
    {
        if (!NodesByZone.TryGetValue(zoneId, out var phases)) return null;
        return phases.Values.SelectMany(nodes => nodes.Values).FirstOrDefault(node => node.Name == name);
    }

    private static FeintNodeInfo? GetNodeInfoByPhaseAndName(uint zoneId, string phase, string name)
    {
        if (!NodesByZone.TryGetValue(zoneId, out var phases)
            || !phases.TryGetValue(phase, out var nodes))
            return null;
        return nodes.Values.FirstOrDefault(node => node.Name == name);
    }

    private static void CheckTriggers(IMewFeintEvent? context)
    {
        if (!IsRuntimeActive) return;
        var settings = AcrSettingsStore.Current.MewFeintPlan;
        if (!settings.GlobalEnabled) return;

        var zoneId = (uint)CurrentZoneId;
        if (!settings.ZonePlans.TryGetValue(zoneId, out var plan)
            || !plan.Enabled
            || plan.EnabledNodes.Count == 0
            || !NodesByZone.TryGetValue(zoneId, out var phases))
            return;

        var currentTimeMs = GetRuntimeTimeMs();
        var phased = FeintZoneConfig.HasPhaseEntryDetector(zoneId);
        var activePhases = phased ? GetActivePhases(zoneId) : null;
        if (phased && context != null)
        {
            try
            {
                ActivatePhase(zoneId, FeintZoneConfig.DetectPhaseEntry(zoneId, context), activePhases!);
            }
            catch (Exception ex)
            {
                MewFeintPlanDebug.Print($"{GetZoneName(zoneId)} 分P检测异常: {ex.Message}");
            }
        }

        var available = new List<(FeintNodeInfo Info, string Phase, uint NodeId)>();
        foreach (var key in plan.GetEnabledNodeKeys())
        {
            if (phased && !activePhases!.Contains(key.Phase)) continue;
            if (!phases.TryGetValue(key.Phase, out var phaseNodes)
                || !phaseNodes.TryGetValue(key.NodeId, out var info))
                continue;

            var nodeKey = (zoneId, key.Phase, key.NodeId);
            if (!ErroredNodes.Contains(nodeKey) && !TriggeredNodes.Contains(nodeKey))
                available.Add((info, key.Phase, key.NodeId));
        }
        if (available.Count == 0) return;

        var timeInSec = currentTimeMs / 1000d;
        foreach (var group in available
                     .GroupBy(node => (node.Phase, BaseName: string.IsNullOrWhiteSpace(node.Info.SequenceName)
                         ? GetNodeBaseName(node.Info.Name)
                         : node.Info.SequenceName))
                     .OrderBy(group => GetPhaseNumberOrder(group.Key.Phase))
                     .ThenBy(group => group.Key.Phase, StringComparer.Ordinal)
                     .ThenBy(group => group.Min(node => node.NodeId)))
        {
            var occurrenceKey = string.IsNullOrEmpty(group.Key.Phase)
                ? group.Key.BaseName
                : $"{group.Key.Phase}:{group.Key.BaseName}";
            // Common plans use a one-second guard for the whole sequence,
            // including frame polls and events with different action IDs. FRU's
            // added delayed sequences retain their event-specific deduplication.
            var useEventDeduplication = zoneId == FeintZoneConfig.ZoneIds["FRU"];
            var eventSignature = useEventDeduplication && context != null ? GetEventSignature(context) : null;
            if (LastTriggerEvents.TryGetValue(occurrenceKey, out var lastEvent)
                && currentTimeMs - lastEvent.TimeMs < 1000
                && (!useEventDeduplication || eventSignature != null && lastEvent.Signature == eventSignature))
                continue;

            var nextCount = OccurrenceCounts.GetValueOrDefault(occurrenceKey) + 1;
            var orderedNodes = group.OrderBy(node => node.Info.SerialId).ThenBy(node => node.NodeId).ToArray();
            var currentNodes = orderedNodes.Where(node => node.Info.SerialId == nextCount).ToArray();
            foreach (var node in currentNodes)
            {
                if (!TryShouldTrigger(node)) continue;
                OccurrenceCounts[occurrenceKey] = nextCount;
                if (!useEventDeduplication || eventSignature != null)
                    LastTriggerEvents[occurrenceKey] = (currentTimeMs, eventSignature ?? string.Empty);
                ExecuteNode(zoneId, plan, phases, node, timeInSec, currentTimeMs);
                return;
            }

            if (currentNodes.Length != 0 || context == null) continue;
            foreach (var node in orderedNodes.Where(node => node.Info.SerialId > nextCount))
            {
                if (!TryShouldTrigger(node)) continue;
                OccurrenceCounts[occurrenceKey] = nextCount;
                if (!useEventDeduplication || eventSignature != null)
                    LastTriggerEvents[occurrenceKey] = (currentTimeMs, eventSignature ?? string.Empty);
                FeintHelper.ClearNodeId(node.NodeId);
                MewFeintPlanDebug.Print(timeInSec,
                    $"({GetZoneName(zoneId)} - {node.Phase} - {node.Info.Name}) 序列拦截：第{nextCount}次读条，需要#{node.Info.SerialId}，已跳过");
                return;
            }
        }

        bool TryShouldTrigger((FeintNodeInfo Info, string Phase, uint NodeId) node)
        {
            var key = (zoneId, node.Phase, node.NodeId);
            try
            {
                return node.Info.Node.ShouldTrigger(node.NodeId, node.Info.SerialId, currentTimeMs, context);
            }
            catch (Exception ex)
            {
                if (ErroredNodes.Add(key))
                    MewFeintPlanDebug.Print($"节点 {node.Info.Name} 异常: {ex.Message}");
                return false;
            }
        }
    }

    private static void ExecuteNode(
        uint zoneId,
        ZoneFeintPlan plan,
        Dictionary<string, Dictionary<uint, FeintNodeInfo>> phases,
        (FeintNodeInfo Info, string Phase, uint NodeId) node,
        double timeInSec,
        int currentTimeMs)
    {
        var key = (zoneId, node.Phase, node.NodeId);
        TriggeredNodes.Add(key);
        // Check the default target before resolving the optional designated
        // target. If it is absent, the default target is retained.
        if (CanUseFeint(Core.Target))
        {
            var target = node.Info.BaseId == 0 ? null : FindFeintTarget(node.Info.BaseId);
            var action = new PAction(FeintActionId, ActionType.OffGcd, ActionTargetType.Target)
            {
                NetworkTid = target?.EntityId ?? 0u
            };
            if (ActionQueueManager.TryEnqueue(action, isHighPriority: true))
            {
                if (zoneId == FeintZoneConfig.ZoneIds["FRU"])
                    FRUData.MarkFeintQueued(node.Info.Name);
                MewFeintPlanDebug.Print(timeInSec,
                    $"({GetZoneName(zoneId)} - {node.Phase} - {node.Info.Name}) 牵制已加入高优先级队列");
            }
            else
            {
                FailedNodes.Add(key);
                MewFeintPlanDebug.Print(timeInSec, $"({GetZoneName(zoneId)} - {node.Info.Name}) 牵制队列提交失败");
            }
        }
        else
        {
            FailedNodes.Add(key);
            MewFeintPlanDebug.Print(timeInSec, $"({GetZoneName(zoneId)} - {node.Info.Name}) 牵制不可用，已跳过");
        }
        CheckCompletion(zoneId, plan, phases, currentTimeMs);
    }

    public static bool IsFeintAvailable(uint baseId = 0)
    {
        var target = baseId == 0 ? Core.Target : FindFeintTarget(baseId) ?? Core.Target;
        return CanUseFeint(target);
    }

    private static bool CanUseFeint(IBattleChara? target)
        => target != null
            && target.CanUseAttackActionOn()
            && ActionHelper.CanCast(FeintActionId, target);

    public static IEnumerable<string> GetPendingDebugLines()
    {
        var zoneId = CurrentZoneId;
        var settings = AcrSettingsStore.Current.MewFeintPlan;
        if (!settings.GlobalEnabled) { yield return "规划已关闭"; yield break; }
        if (!settings.ZonePlans.TryGetValue(zoneId, out var plan) || !plan.Enabled)
        { yield return "当前副本规划未启用"; yield break; }
        if (!NodesByZone.TryGetValue(zoneId, out var phases)) yield break;

        var active = FeintZoneConfig.HasPhaseEntryDetector(zoneId) ? GetActivePhases(zoneId) : null;
        foreach (var key in plan.GetEnabledNodeKeys())
        {
            if (active != null && !active.Contains(key.Phase)) continue;
            if (!phases.TryGetValue(key.Phase, out var nodes) || !nodes.TryGetValue(key.NodeId, out var info)) continue;
            var nodeKey = (zoneId, key.Phase, key.NodeId);
            if (TriggeredNodes.Contains(nodeKey) || ErroredNodes.Contains(nodeKey)) continue;

            if (!FeintHelper.TryGetPendingDelay(key.NodeId, out var remainingMs))
                yield return $"{info.Name}：等待触发条件（{info.Description}）";
            else if (remainingMs > 0)
                yield return $"{info.Name}：延迟剩余 {remainingMs / 1000d:F1}s";
            else
            {
                var target = Core.Target;
                var reason = target == null ? "无当前目标"
                    : !target.CanUseAttackActionOn() ? "当前目标不可攻击"
                    : ActionHelper.GetActionCooldown(FeintActionId) > 0f
                        ? $"牵制 CD 剩余 {ActionHelper.GetActionCooldown(FeintActionId):F1}s"
                    : !ActionHelper.CanCast(FeintActionId, target) ? "PR 判定当前不可释放"
                    : "技能已就绪，等待调度";
                yield return $"{info.Name}：延迟已到，{reason}";
            }
        }
    }

    private static IBattleChara? FindFeintTarget(uint baseId)
    {
        if (baseId == 0) return Core.Target;
        try
        {
            return Svc.Objects.OfType<IBattleNpc>()
                .FirstOrDefault(npc => npc.BaseId == baseId && npc.IsTargetable);
        }
        catch (Exception ex)
        {
            MewFeintPlanDebug.Print($"指定目标 BaseId={baseId} 查找失败: {ex.Message}");
            return null;
        }
    }

    private static HashSet<string> GetActivePhases(uint zoneId)
    {
        if (ActivePhasesByZone.TryGetValue(zoneId, out var phases)) return phases;

        phases = new HashSet<string>(StringComparer.Ordinal);
        if (PhasesByZone.TryGetValue(zoneId, out var known) && known.Count > 0)
            phases.Add(known.Contains("P1") ? "P1" : known.FirstOrDefault(phase => !string.IsNullOrWhiteSpace(phase)) ?? known[0]);
        ActivePhasesByZone[zoneId] = phases;
        return phases;
    }

    private static void ActivatePhase(
        uint zoneId,
        string? phase,
        HashSet<string> activePhases)
    {
        if (string.IsNullOrWhiteSpace(phase)) return;
        phase = phase.Trim();
        if (activePhases.SetEquals(new[] { phase })) return;

        var previous = activePhases.ToArray();
        activePhases.Clear();
        activePhases.Add(phase);
        ClearInactivePhaseRuntime(zoneId, previous);
        MewFeintPlanDebug.Print($"{GetZoneName(zoneId)} 已进入 {phase}");
    }

    private static void ClearInactivePhaseRuntime(uint zoneId, IEnumerable<string> oldPhases)
    {
        if (!NodesByZone.TryGetValue(zoneId, out var phases)) return;
        var old = oldPhases.Where(phase => !string.IsNullOrWhiteSpace(phase)).ToHashSet(StringComparer.Ordinal);
        foreach (var phase in old)
            if (phases.TryGetValue(phase, out var phaseNodes))
                foreach (var id in phaseNodes.Keys)
                    FeintHelper.ClearNodeId(id);

        foreach (var key in OccurrenceCounts.Keys.Where(key => old.Any(phase => key.StartsWith(phase + ":", StringComparison.Ordinal))).ToArray())
            OccurrenceCounts.Remove(key);
        foreach (var key in LastTriggerEvents.Keys.Where(key => old.Any(phase => key.StartsWith(phase + ":", StringComparison.Ordinal))).ToArray())
            LastTriggerEvents.Remove(key);
    }

    private static string GetNodeBaseName(string name)
    {
        var hash = name.IndexOf('#');
        return hash <= 0 ? name : name[..hash];
    }

    private static string? GetEventSignature(IMewFeintEvent context) => context switch
    {
        EnemyCastSpellCondParams cast => $"cast:{cast.SpellId}",
        ReceviceAbilityEffectCondParams effect => $"effect:{effect.ActionId}",
        SelfActionEffectCondParams self => $"self:{self.ActionId}",
        WeatherChangedCondParams weather => $"weather:{weather.NewWeatherId}",
        UnitCreateCondParams unit when unit.BattleChara != null => $"unit:{unit.BattleChara.EntityId}",
        _ => null
    };

    private static void CheckCompletion(
        uint zoneId,
        ZoneFeintPlan plan,
        Dictionary<string, Dictionary<uint, FeintNodeInfo>> phases,
        int currentTimeMs)
    {
        if (CompletedZones.Contains(zoneId)) return;
        var total = 0;
        var completed = 0;
        foreach (var node in plan.GetEnabledNodeKeys())
        {
            if (!phases.TryGetValue(node.Phase, out var nodes) || !nodes.ContainsKey(node.NodeId)) continue;
            total++;
            if (TriggeredNodes.Contains((zoneId, node.Phase, node.NodeId))
                || ErroredNodes.Contains((zoneId, node.Phase, node.NodeId)))
                completed++;
        }
        if (total == 0 || completed < total) return;

        CompletedZones.Add(zoneId);
        var success = plan.GetEnabledNodeKeys()
            .Count(node => TriggeredNodes.Contains((zoneId, node.Phase, node.NodeId))
                && !FailedNodes.Contains((zoneId, node.Phase, node.NodeId)));
        var failed = plan.GetEnabledNodeKeys()
            .Count(node => FailedNodes.Contains((zoneId, node.Phase, node.NodeId)));
        var errors = plan.GetEnabledNodeKeys()
            .Count(node => ErroredNodes.Contains((zoneId, node.Phase, node.NodeId)));
        MewFeintPlanDebug.Print(currentTimeMs / 1000d,
            $"({GetZoneName(zoneId)}) 所有牵制节点已完成！成功: {success}/{total}, 失败: {failed}, 异常: {errors}");
    }
}
