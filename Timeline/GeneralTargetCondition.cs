using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Managers.CombatEventManager;
using PromeRotation.Managers.CombatEventManager.Events;
using PromeRotation.TargetSelector;
using PromeRotation.Timeline.Core;
using RotationCore = PromeRotation.Core.Core;

namespace PromeRotation.CustomTimeline;

internal readonly record struct GeneralTargetRecentMatch(ulong TargetId, uint RuleId, DateTime Time);

/// <summary>通用目标筛选条件。</summary>
public sealed class GeneralTargetCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "luli_general_target_condition";
    private static readonly JsonSerializerOptions FilterJsonOptions = new() { IncludeFields = true };
    private static readonly string[] TargetLabels =
        ["自身状态", "当前选中目标", "从周围单位中过滤"];
    private static readonly int[] TargetValues = [0, 1, 4];
    private static readonly string[] SecondaryLabels = ["无", "最近距离", "最低当前HP", "最高当前HP"];
    private static readonly string[] FilterLabels =
        ["文字ID", "DataId", "目标血量", "存在Buff", "最近点名", "最近连线", "不存在buff"];
    private static readonly int[] FilterValues = [0, 1, 3, 4, 5, 6, 8];
    private static readonly string[] CompareLabels = ["==", "!=", ">", ">=", "<", "<="];
    private static readonly string[] SelectorModeLabels =
        ["关闭", "最近距离", "最远距离", "最低血量", "最高血量", "3米内最低血量", "3米内最高血量", "6米内最低血量", "6米内最高血量"];

    private static int nextEditorId;
    private readonly int editorId = System.Threading.Interlocked.Increment(ref nextEditorId);
    private int targetType = 1;
    private int secondaryFilter;
    private int selectedFilterIndex = -1;
    private int newFilterType = (int)FilterKind.HpPct;
    private bool enabled = true;
    private bool changeSelectorMode;
    private int selectorMode;
    private bool needTargetable;
    private readonly List<FilterRule> filters = [];

    public string NodeDisplayName => "检测目标条件";

    public NodeParamInfo[] Params => Array.Empty<NodeParamInfo>();

    public static void Register(RotationNodeContext context)
    {
        GeneralTargetHistory.Attach();
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new GeneralTargetCondition();
            if (dto.Params == null) return condition;
            foreach (var (key, value) in dto.Params)
                condition.SetParam(key, value);
            return condition;
        });
    }

    public bool DrawJobNodeEditor()
    {
        ImGui.PushID(editorId);
        ImGui.Checkbox("启用目标选择器", ref enabled);
        ImGui.SameLine();
        ImGui.Checkbox("更改目标选择器状态", ref changeSelectorMode);
        if (changeSelectorMode)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(230f);
            DrawCombo("状态", ref selectorMode, SelectorModeLabels);
        }

        if (!enabled)
        {
            ImGui.PopID();
            return true;
        }

        ImGui.Spacing();
        ImGui.Text("目标选择");
        ImGui.Separator();
        ImGui.SetNextItemWidth(Math.Clamp(ImGui.GetContentRegionAvail().X * 0.46f, 220f, 330f));
        DrawMappedCombo("目标来源", ref targetType, TargetLabels, TargetValues);

        if (targetType == 4)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(Math.Clamp(ImGui.GetContentRegionAvail().X * 0.38f, 170f, 230f));
            DrawCombo("二次过滤", ref secondaryFilter, SecondaryLabels);
        }

        ImGui.Spacing();
        ImGui.Checkbox("目标必须可选中", ref needTargetable);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("过滤条件");
        ImGui.SetNextItemWidth(Math.Clamp(ImGui.GetContentRegionAvail().X * 0.22f, 160f, 220f));
        DrawMappedCombo("新增条件类型", ref newFilterType, FilterLabels, FilterValues);
        ImGui.SameLine();
        if (ImGui.Button("添加"))
        {
            filters.Add(new FilterRule { Filter = newFilterType });
            selectedFilterIndex = filters.Count - 1;
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedFilterIndex < 0 || selectedFilterIndex >= filters.Count);
        if (ImGui.Button("删除") && selectedFilterIndex >= 0 && selectedFilterIndex < filters.Count)
        {
            filters.RemoveAt(selectedFilterIndex);
            selectedFilterIndex = filters.Count == 0 ? -1 : Math.Min(selectedFilterIndex, filters.Count - 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("复制") && selectedFilterIndex >= 0 && selectedFilterIndex < filters.Count)
        {
            filters.Insert(selectedFilterIndex + 1, filters[selectedFilterIndex].Clone());
            selectedFilterIndex++;
        }
        ImGui.SameLine();
        if (ImGui.Button("上移") && selectedFilterIndex > 0 && selectedFilterIndex < filters.Count)
        {
            (filters[selectedFilterIndex - 1], filters[selectedFilterIndex]) =
                (filters[selectedFilterIndex], filters[selectedFilterIndex - 1]);
            selectedFilterIndex--;
        }
        ImGui.SameLine();
        if (ImGui.Button("下移") && selectedFilterIndex >= 0 && selectedFilterIndex < filters.Count - 1)
        {
            (filters[selectedFilterIndex + 1], filters[selectedFilterIndex]) =
                (filters[selectedFilterIndex], filters[selectedFilterIndex + 1]);
            selectedFilterIndex++;
        }
        ImGui.EndDisabled();

        ImGui.Spacing();
        var listWidth = Math.Clamp(ImGui.GetContentRegionAvail().X * 0.28f, 190f, 270f);
        ImGui.BeginChild("##filter_list", new Vector2(listWidth, 180f), true);
        for (var i = 0; i < filters.Count; i++)
        {
            var rule = filters[i];
            var label = $"Index {i} : {GetFilterLabel(rule.Filter)}";
            if (!string.IsNullOrWhiteSpace(rule.Remark)) label += $" {rule.Remark}";
            if (ImGui.Selectable(label, selectedFilterIndex == i)) selectedFilterIndex = i;
        }
        if (filters.Count == 0) ImGui.TextDisabled("尚未添加过滤条件");
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("##filter_editor", new Vector2(0f, 180f), true);
        if (selectedFilterIndex >= 0 && selectedFilterIndex < filters.Count)
        {
            ImGui.PushID(selectedFilterIndex);
            var rule = filters[selectedFilterIndex];
            ImGui.SetNextItemWidth(190f);
            DrawMappedCombo("类型", ref rule.Filter, FilterLabels, FilterValues);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(260f);
            ImGui.InputText("备注", ref rule.Remark, 128);
            DrawRule(rule);
            ImGui.PopID();
        }
        else
        {
            ImGui.TextDisabled("选择一条过滤条件进行编辑");
        }
        ImGui.EndChild();

        ImGui.PopID();
        return true;
    }

    private static bool DrawCombo(string label, ref int value, string[] labels)
    {
        var index = Math.Clamp(value, 0, labels.Length - 1);
        if (!ImGui.Combo(label, ref index, labels, labels.Length)) return false;
        value = index;
        return true;
    }

    private static bool DrawMappedCombo(string label, ref int value, string[] labels, int[] values)
    {
        var index = Array.IndexOf(values, value);
        if (index < 0) index = 0;
        if (!ImGui.Combo(label, ref index, labels, labels.Length)) return false;
        value = values[index];
        return true;
    }

    private static string GetFilterLabel(int value)
    {
        var index = Array.IndexOf(FilterValues, value);
        return index < 0 ? "未知过滤条件" : FilterLabels[index];
    }

    private static bool DrawRule(FilterRule rule)
    {
        var changed = false;
        switch ((FilterKind)rule.Filter)
        {
            case FilterKind.Name:
                ImGui.SetNextItemWidth(420f);
                changed |= ImGui.InputText("文字ID", ref rule.StrParam1, 256);
                break;
            case FilterKind.DataId:
                ImGui.SetNextItemWidth(150f);
                changed |= DrawInt("DataId", ref rule.UintParam1);
                ImGui.SameLine();
                changed |= ImGui.Checkbox("优先最近同 DataId", ref rule.Nearest);
                break;
            case FilterKind.HpPct:
                ImGui.SetNextItemWidth(145f);
                changed |= DrawCompare("比较方式", ref rule.CompareType);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(190f);
                changed |= ImGui.InputFloat("目标血量 (%)", ref rule.FloatParam1);
                rule.FloatParam1 = Math.Clamp(rule.FloatParam1, 0f, 100f);
                break;
            case FilterKind.HasBuff:
                ImGui.SetNextItemWidth(130f);
                changed |= DrawInt("BuffId", ref rule.UintParam1);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(145f);
                changed |= DrawCompare("Buff剩余时间比较", ref rule.BuffCompareType);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                changed |= DrawInt("Buff剩余时间(ms)", ref rule.LeftTime);
                break;
            case FilterKind.RecentlyTargetIcon:
                ImGui.SetNextItemWidth(130f);
                changed |= DrawInt("标记 Args0", ref rule.UintParam1);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                changed |= DrawInt("点名窗口(ms)", ref rule.LeftTime);
                break;
            case FilterKind.RecentlyTether:
                ImGui.SetNextItemWidth(130f);
                changed |= DrawInt("TetherId", ref rule.UintParam1);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                changed |= DrawInt("连线窗口(ms)", ref rule.LeftTime);
                break;
            case FilterKind.NoBuff:
                ImGui.SetNextItemWidth(150f);
                changed |= DrawInt("BuffId", ref rule.UintParam1);
                break;
        }
        return changed;
    }

    private static bool DrawCompare(string label, ref int value) => DrawCombo(label, ref value, CompareLabels);

    private static bool DrawInt(string label, ref int value)
    {
        var result = ImGui.InputInt(label, ref value);
        if (value < 0)
        {
            value = 0;
            result = true;
        }
        return result;
    }

    public string GetParam(string fieldName) => fieldName switch
    {
        "target" => targetType.ToString(),
        "enabled" => enabled.ToString(),
        "changeSelectorMode" => changeSelectorMode.ToString(),
        "selectorMode" => selectorMode.ToString(),
        "needTargetable" => needTargetable.ToString(),
        "secondary" => secondaryFilter.ToString(),
        "filters" => JsonSerializer.Serialize(filters, FilterJsonOptions),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case "target" when int.TryParse(value, out var target):
                targetType = TargetValues.Contains(target) ? target : TargetValues[0];
                break;
            case "enabled" when bool.TryParse(value, out var isEnabled):
                enabled = isEnabled;
                break;
            case "changeSelectorMode" when bool.TryParse(value, out var changeMode):
                changeSelectorMode = changeMode;
                break;
            case "selectorMode" when int.TryParse(value, out var mode):
                selectorMode = Math.Clamp(mode, 0, SelectorModeLabels.Length - 1);
                break;
            case "needTargetable" when bool.TryParse(value, out var targetable):
                needTargetable = targetable;
                break;
            case "secondary" when int.TryParse(value, out var secondary):
                secondaryFilter = Math.Clamp(secondary, 0, SecondaryLabels.Length - 1);
                break;
            case "filters":
                try
                {
                    filters.Clear();
                    filters.AddRange((JsonSerializer.Deserialize<List<FilterRule>>(value, FilterJsonOptions) ?? [])
                        .Where(rule => FilterValues.Contains(rule.Filter)));
                    selectedFilterIndex = filters.Count == 0 ? -1 : Math.Min(selectedFilterIndex, filters.Count - 1);
                }
                catch (JsonException)
                {
                    filters.Clear();
                }
                break;
        }
    }

    public bool EvaluateImmediate()
    {
        ApplySelectorMode();
        return SelectTarget() != null;
    }

    public bool EvaluateWait()
    {
        ApplySelectorMode();
        return SelectTarget() != null;
    }

    private void ApplySelectorMode()
    {
        if (!changeSelectorMode) return;
        var mode = (SelectorModeType)selectorMode;
        var settings = PromeSettings.Instance;
        if (settings.TargetSelectorMode == mode) return;

        settings.TargetSelectorMode = mode;
        if (mode == SelectorModeType.None) TargetSelectorService.Clear();
    }

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
        Params = new Dictionary<string, string>
        {
            ["target"] = GetParam("target"),
            ["enabled"] = GetParam("enabled"),
            ["changeSelectorMode"] = GetParam("changeSelectorMode"),
            ["selectorMode"] = GetParam("selectorMode"),
            ["needTargetable"] = GetParam("needTargetable"),
            ["secondary"] = GetParam("secondary"),
            ["filters"] = GetParam("filters"),
        },
    };

    private IBattleChara? SelectTarget()
    {
        var me = RotationCore.Me;
        if (me == null) return null;

        IEnumerable<IBattleChara> candidates = targetType switch
        {
            0 => [me],
            1 => RotationCore.Target is { } target ? [target] : [],
            4 => Svc.Objects.OfType<IBattleChara>(),
            _ => [],
        };

        var matches = candidates.Where(Matches);
        if (targetType != 4) return matches.FirstOrDefault();

        var units = matches.ToList();
        if (units.Count == 0) return null;
        return secondaryFilter switch
        {
            1 => units.MinBy(unit => Vector3.Distance(me.Position, unit.Position)),
            2 => units.MinBy(unit => unit.CurrentHp),
            3 => units.MaxBy(unit => unit.CurrentHp),
            _ => units[0],
        };
    }

    private bool Matches(IBattleChara target)
    {
        if (target == null || !target.IsValid()) return false;
        if (needTargetable && !target.IsTargetable) return false;
        foreach (var rule in filters)
            if (!MatchesRule(target, rule))
                return false;
        return true;
    }

    private static bool MatchesRule(IBattleChara target, FilterRule rule)
    {
        var kind = (FilterKind)rule.Filter;
        switch (kind)
        {
            case FilterKind.Name:
                try { return Regex.IsMatch(target.Name.ToString(), rule.StrParam1); }
                catch (ArgumentException) { return false; }
            case FilterKind.DataId:
                if (rule.Nearest)
                {
                    var nearest = Svc.Objects.OfType<IBattleChara>()
                        .Where(unit => unit.BaseId == (uint)rule.UintParam1)
                        .MinBy(unit => RotationCore.Me == null ? float.MaxValue : Vector3.Distance(RotationCore.Me.Position, unit.Position))
                        ?? RotationCore.Me;
                    return nearest?.EntityId == target.EntityId;
                }
                return target.BaseId == (uint)rule.UintParam1;
            case FilterKind.HpPct:
                return target.IsTargetable && Compare(
                    target.MaxHp == 0 ? 0f : target.CurrentHp * 100f / target.MaxHp,
                    rule.FloatParam1, rule.CompareType, equalTolerance: 0.1f);
            case FilterKind.HasBuff:
                if (!target.IsTargetable) return false;
                var status = target.StatusList.FirstOrDefault(x => x.StatusId == (uint)rule.UintParam1);
                return status != null && Compare((long)(status.RemainingTime * 1000f),
                    rule.LeftTime, rule.BuffCompareType);
            case FilterKind.RecentlyTargetIcon:
                return target.IsTargetable && GeneralTargetHistory.HasRecentIcon(
                    target, (uint)rule.UintParam1, rule.LeftTime);
            case FilterKind.RecentlyTether:
                return GeneralTargetHistory.HasRecentTether(target, rule.LeftTime);
            case FilterKind.NoBuff:
                return target.IsTargetable && !target.StatusList.Any(x => x.StatusId == (uint)rule.UintParam1);
            default:
                return false;
        }
    }

    private static bool Compare(float current, float value, int compare, float equalTolerance = 0f)
        => compare switch
        {
            0 => MathF.Abs(current - value) < equalTolerance,
            1 => MathF.Abs(current - value) > equalTolerance,
            2 => current > value,
            3 => current >= value,
            4 => current < value,
            5 => current <= value,
            _ => false,
        };

    private static bool Compare(long current, int value, int compare)
        => compare switch
        {
            0 => current == value,
            1 => current != value,
            2 => current > value,
            3 => current >= value,
            4 => current < value,
            5 => current <= value,
            _ => false,
        };

    private enum FilterKind
    {
        Name,
        DataId,
        HpPct = 3,
        HasBuff,
        RecentlyTargetIcon,
        RecentlyTether,
        NoBuff = 8,
    }

    private sealed class FilterRule
    {
        public FilterRule() { }

        public FilterRule Clone() => (FilterRule)MemberwiseClone();

        public int Filter;
        public string Remark = string.Empty;
        public string StrParam1 = string.Empty;
        public int UintParam1;
        public float FloatParam1;
        public int LeftTime;
        public int CompareType = 5;
        public int BuffCompareType = 3;
        public bool Nearest;
    }
}
internal static class GeneralTargetHistory
{
    private const int MaxEntries = 4096;
    private static readonly object Gate = new();
    private static readonly List<GeneralTargetRecentMatch> Icons = [];
    private static readonly List<GeneralTargetRecentMatch> Tethers = [];
    private static bool attached;

    internal static void Attach()
    {
        if (attached) return;
        CombatEventManager.OnActorControlTargetIcon += OnTargetIcon;
        CombatEventManager.OnActorControlTether += OnTether;
        attached = true;
    }

    internal static bool HasRecentIcon(IBattleChara target, uint marker, int windowMs)
    {
        lock (Gate) return HasRecent(Icons, target, marker, windowMs);
    }

    internal static bool HasRecentTether(IBattleChara target, int windowMs)
    {
        lock (Gate) return HasRecent(Tethers, target, null, windowMs);
    }

    private static bool HasRecent(List<GeneralTargetRecentMatch> history, IBattleChara target, uint? ruleId, int windowMs)
    {
        if (windowMs < 0) return false;
        var now = DateTime.Now;
        Prune(history, now);
        return history.Any(item => MatchesId(target, item.TargetId)
            && (!ruleId.HasValue || item.RuleId == ruleId.Value)
            && (now - item.Time).TotalMilliseconds <= windowMs);
    }

    private static bool MatchesId(IBattleChara target, ulong id)
        => id != 0 && (id == target.GameObjectId || id == target.EntityId);

    private static void OnTargetIcon(ActorControlTargetIconEvent ev)
    {
        if (ev.MarkerId == 0 || ev.TargetId == 0) return;
        lock (Gate) Add(Icons, new GeneralTargetRecentMatch(ev.TargetId, ev.MarkerId, ev.Timestamp));
    }

    private static void OnTether(ActorControlTetherEvent ev)
    {
        lock (Gate)
        {
            Add(Tethers, new GeneralTargetRecentMatch(ev.SourceId, ev.TetherId, ev.Timestamp));
            if (ev.TargetId != 0) Add(Tethers, new GeneralTargetRecentMatch(ev.TargetId, ev.TetherId, ev.Timestamp));
            if (ev.TetherTarget != 0) Add(Tethers, new GeneralTargetRecentMatch(ev.TetherTarget, ev.TetherId, ev.Timestamp));
        }
    }

    private static void Add(List<GeneralTargetRecentMatch> list, GeneralTargetRecentMatch match)
    {
        if (match.TargetId == 0) return;
        list.Add(match);
        if (list.Count > MaxEntries) list.RemoveRange(0, list.Count - MaxEntries);
    }

    private static void Prune(List<GeneralTargetRecentMatch> list, DateTime now)
        => list.RemoveAll(item => (now - item.Time).TotalMinutes > 2);
}
