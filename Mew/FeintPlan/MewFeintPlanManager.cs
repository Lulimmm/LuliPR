using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using LuliPR.Common;
using Mew.FeintPlan.Events;
using Mew.FeintPlan.Scripts.FRU;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Managers.CombatEventManager;
using PromeRotation.Managers.CombatEventManager.Events;
using PromeRotation.Timeline.Core;
using Reaper.PR;

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
    private static readonly Dictionary<string, long> LastTriggerTimeMs = new();
    private static readonly HashSet<uint> SeenDmuUnits = new();

    private static bool attached;
    private static bool wasInCombat;
    private static uint runtimeZoneId;
    private static uint lastWeatherId = uint.MaxValue;
    private static long lastFeintQueuedAt;
    private static uint lastAutoPresetZoneId = uint.MaxValue;
    private static string? lastAutoPresetName;

    public static uint CurrentZoneId => Svc.ClientState.TerritoryType;

    public static void Attach()
    {
        if (attached) return;

        LoadAllNodes();
        ResetRuntime();
        lastWeatherId = uint.MaxValue;
        CombatEventManager.OnStartCast += OnStartCast;
        CombatEventManager.OnActionEffect += OnActionEffect;
        attached = true;
    }

    public static void Dispose()
    {
        if (attached)
        {
            CombatEventManager.OnStartCast -= OnStartCast;
            CombatEventManager.OnActionEffect -= OnActionEffect;
        }
        attached = false;
        ResetRuntime();
    }

    public static void ResetRuntime()
    {
        ErroredNodes.Clear();
        TriggeredNodes.Clear();
        FailedNodes.Clear();
        CompletedZones.Clear();
        ActivePhasesByZone.Clear();
        OccurrenceCounts.Clear();
        LastTriggerTimeMs.Clear();
        SeenDmuUnits.Clear();
        FRUData.ResetRuntime();
        FeintHelper.Clear();
        lastFeintQueuedAt = 0;
        lastWeatherId = uint.MaxValue;
        wasInCombat = false;
        runtimeZoneId = CurrentZoneId;
    }

    public static void Update()
    {
        if (!attached) return;

        var zoneId = CurrentZoneId;
        if (zoneId != runtimeZoneId)
        {
            ResetRuntime();
            runtimeZoneId = zoneId;
            lastWeatherId = uint.MaxValue;
        }

        SyncAutomaticZonePreset(zoneId);

        if (!GameData.IsInCombat())
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

    private static void OnStartCast(StartCastEvent action)
    {
        if (Core.Me == null || action.SourceId == Core.Me.EntityId) return;
        CheckTriggers(new EnemyCastSpellCondParams(action.ActionId, action.CastTime));
    }

    private static void OnActionEffect(ActionEffectEvent action)
    {
        if (Core.Me == null) return;
        if (action.SourceId == Core.Me.EntityId)
        {
            CheckTriggers(new SelfActionEffectCondParams(action.ActionId));
            return;
        }
        CheckTriggers(new ReceviceAbilityEffectCondParams(action.ActionId));
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
                        SerialId = attribute.SerialId,
                        SequenceName = attribute.SequenceName,
                        DisplayOrder = attribute.DisplayOrder,
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
        if (!GameData.IsInCombat()) return;
        var settings = AcrSettingsStore.Current.MewFeintPlan;
        if (!settings.GlobalEnabled) return;

        var zoneId = (uint)CurrentZoneId;
        if (!settings.ZonePlans.TryGetValue(zoneId, out var plan)
            || !plan.Enabled
            || plan.EnabledNodes.Count == 0
            || !NodesByZone.TryGetValue(zoneId, out var phases))
            return;

        var currentTimeMs = Math.Max(0, (int)(EngageManager.GetBattleTime() * 1000f));
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
                return;
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
            if (LastTriggerTimeMs.TryGetValue(occurrenceKey, out var lastTime)
                && currentTimeMs - lastTime < 1000)
                continue;

            var nextCount = OccurrenceCounts.GetValueOrDefault(occurrenceKey) + 1;
            var orderedNodes = group.OrderBy(node => node.Info.SerialId).ThenBy(node => node.NodeId).ToArray();
            var currentNodes = orderedNodes.Where(node => node.Info.SerialId == nextCount).ToArray();
            foreach (var node in currentNodes)
            {
                if (!TryShouldTrigger(node)) continue;
                OccurrenceCounts[occurrenceKey] = nextCount;
                LastTriggerTimeMs[occurrenceKey] = currentTimeMs;
                ExecuteNode(zoneId, plan, phases, node, timeInSec, currentTimeMs);
                return;
            }

            if (currentNodes.Length != 0 || context == null) continue;
            foreach (var node in orderedNodes.Where(node => node.Info.SerialId > nextCount))
            {
                if (!TryShouldTrigger(node)) continue;
                OccurrenceCounts[occurrenceKey] = nextCount;
                LastTriggerTimeMs[occurrenceKey] = currentTimeMs;
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
        var target = node.Info.BaseId == 0 ? Core.Target : FindFeintTarget(node.Info.BaseId);
        var now = Environment.TickCount64;
        if (target != null
            && (lastFeintQueuedAt == 0 || now - lastFeintQueuedAt >= 500)
            && R.CanCast(FeintActionId, target))
        {
            var action = new PAction(FeintActionId, ActionType.OffGcd, ActionTargetType.Target)
            {
                NetworkTid = node.Info.BaseId == 0 ? 0u : target?.EntityId ?? 0u
            };
            if (ActionQueueManager.TryEnqueue(action, isHighPriority: true))
            {
                lastFeintQueuedAt = now;
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
            var reason = target == null && node.Info.BaseId != 0
                ? $"指定目标 BaseId={node.Info.BaseId} 不可用"
                : "牵制不可用，已跳过";
            MewFeintPlanDebug.Print(timeInSec, $"({GetZoneName(zoneId)} - {node.Info.Name}) {reason}");
        }
        CheckCompletion(zoneId, plan, phases, currentTimeMs);
    }

    public static bool IsFeintAvailable(uint baseId = 0)
    {
        var target = baseId == 0 ? Core.Target : FindFeintTarget(baseId);
        return target != null && R.CanCast(FeintActionId, target);
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
        if (PhasesByZone.TryGetValue(zoneId, out var known)
            && known.FirstOrDefault(phase => phase == "P1") is { } first
            && !string.IsNullOrWhiteSpace(first))
            phases.Add(first);
        else if (known is { Count: > 0 })
            phases.Add(known[0]);
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
        foreach (var key in LastTriggerTimeMs.Keys.Where(key => old.Any(phase => key.StartsWith(phase + ":", StringComparison.Ordinal))).ToArray())
            LastTriggerTimeMs.Remove(key);
    }

    private static string GetNodeBaseName(string name)
    {
        var hash = name.IndexOf('#');
        return hash <= 0 ? name : name[..hash];
    }

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
