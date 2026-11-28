using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Reaper.PR.Timeline;

public sealed class ReaperLemureShroudCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_trigger_lemure_shroud";
    private static readonly string[] Operators = ["==", "!=", ">", ">=", "<", "<="];
    private static readonly string[] Types = ["不限", "普通附体", "特殊附体（3859）"];
    private int threshold;
    private int comparison;
    private int enshroudType;
    public string NodeDisplayName => "Reaper/夜游魂量谱";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("threshold", "附体量谱阈值", "夜游魂层数（0～5）", "int"),
        new NodeParamInfo("operator", "运算符", "层数比较方式", "enum",
            new[] { ("0", "=="), ("1", "!="), ("2", ">"), ("3", ">="), ("4", "<"), ("5", "<=") }),
        new NodeParamInfo("enshroudType", "附体类型", "按普通或特殊附体筛选", "enum",
            new[] { ("0", "不限"), ("1", "普通附体"), ("2", "特殊附体（3859）") }),
    };
    public bool Draw()
    {
        var changed = ImGui.Combo("附体类型", ref enshroudType, Types, Types.Length);
        changed |= ImGui.Combo("运算符", ref comparison, Operators, Operators.Length);
        changed |= ImGui.InputInt("附体量谱阈值", ref threshold);
        threshold = Math.Clamp(threshold, 0, 5);
        return changed;
    }
    public string GetParam(string fieldName) => fieldName switch
    {
        "threshold" => threshold.ToString(),
        "operator" => comparison.ToString(),
        "enshroudType" => enshroudType.ToString(),
        _ => string.Empty
    };
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "threshold" && int.TryParse(value, out var layer)) threshold = Math.Clamp(layer, 0, 5);
        else if (fieldName == "operator")
        {
            comparison = int.TryParse(value, out var op) ? Math.Clamp(op, 0, 5) : Array.IndexOf(Operators, value);
            if (comparison < 0) comparison = 0;
        }
        else if (fieldName == "enshroudType" && int.TryParse(value, out var type)) enshroudType = Math.Clamp(type, 0, 2);
    }
    public bool EvaluateImmediate()
    {
        SpecialEnshroudRuntime.Update();
        if (enshroudType == 1 && (!R.Has(2593u) || SpecialEnshroudRuntime.IsSpecial)
            || enshroudType == 2 && (!R.Has(2593u) || !SpecialEnshroudRuntime.IsSpecial)) return false;
        var current = JobGaugeHelper.RPR.夜游魂;
        return comparison switch
        {
            0 => current == threshold,
            1 => current != threshold,
            2 => current > threshold,
            3 => current >= threshold,
            4 => current < threshold,
            5 => current <= threshold,
            _ => false
        };
    }
    public bool EvaluateWait() => EvaluateImmediate();
    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
        Params = new Dictionary<string, string>
        {
            ["threshold"] = threshold.ToString(), ["operator"] = comparison.ToString(), ["enshroudType"] = enshroudType.ToString()
        }
    };
    public static void Register(RotationNodeContext context)
        => ConditionFactory.Register(context, TypeKey, dto =>
        {
            var node = new ReaperLemureShroudCondition();
            if (dto.Params != null) foreach (var item in dto.Params) node.SetParam(item.Key, item.Value);
            return node;
        });
}

public sealed class ReaperSpecialEnshroudCommunioAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_special_enshroud_communio";
    private bool enabled;
    private int layer = 1;
    public string NodeDisplayName => "Reaper/设置特殊附体团契层数";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("enabled", "启用特殊附体团契控制", "关闭时解除团契锁定", "bool"),
        new NodeParamInfo("lemureShrouds", "特殊附体团契层数", "到达或低于指定层数后只释放团契（1～5）", "int"),
    };
    public bool Draw()
    {
        var changed = ImGui.Checkbox("启用特殊附体团契控制", ref enabled);
        if (enabled)
        {
            changed |= ImGui.InputInt("特殊附体团契层数", ref layer);
            layer = Math.Clamp(layer, 1, 5);
        }
        ImGui.TextWrapped("开启后到达指定层数前正常输出，到达后只打团契，直到附体结束。关闭后解除锁定。战斗重置后恢复界面保存的设置。");
        return changed;
    }
    public string GetParam(string fieldName) => fieldName switch
    {
        "enabled" => enabled.ToString(), "lemureShrouds" => layer.ToString(), _ => string.Empty
    };
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "enabled" && bool.TryParse(value, out var state)) enabled = state;
        else if (fieldName == "lemureShrouds" && int.TryParse(value, out var threshold)) layer = Math.Clamp(threshold, 1, 5);
    }
    public void Execute() => SpecialEnshroudRuntime.SetTimelineSettings(enabled, layer);
    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string> { ["enabled"] = enabled.ToString(), ["lemureShrouds"] = layer.ToString() }
    };
    public static void Register(RotationNodeContext context)
        => ActionFactory.Register(context, TypeKey, dto =>
        {
            var node = new ReaperSpecialEnshroudCommunioAction();
            if (dto.Params != null) foreach (var item in dto.Params) node.SetParam(item.Key, item.Value);
            return node;
        });
}
