using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Reaper.PR.Timeline;

/// <summary>
/// Shared implementation of AE's NewSoulGauge/NewShroudGauge timeline trigger.
/// </summary>
public abstract class ReaperGaugeCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private static readonly string[] OperatorLabels = { "==", "!=", ">", ">=", "<", "<=" };

    private int _operatorIndex;
    private int _value;
    private bool _useRange;
    private int _minValue;
    private int _maxValue;

    protected abstract int CurrentValue { get; }
    protected abstract string TypeKey { get; }
    protected abstract string GaugeLabel { get; }
    protected abstract string NodeLabel { get; }

    public string NodeDisplayName => NodeLabel;

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(
            "operator",
            "\u8fd0\u7b97\u7b26",
            "\u6bd4\u8f83\u65f6\u4f7f\u7528\u7684\u8fd0\u7b97\u7b26",
            "enum",
            new[]
            {
                ("0", "=="),
                ("1", "!="),
                ("2", ">"),
                ("3", ">="),
                ("4", "<"),
                ("5", "<="),
            }),
        new NodeParamInfo(
            "value",
            "\u6570\u503c",
            "\u975e\u533a\u95f4\u6a21\u5f0f\u4e0b\u7684\u91cf\u8c31\u503c\uff080~100\uff0c\u6bcf 10 \u70b9\u4e00\u6863\uff09",
            "int"),
        new NodeParamInfo(
            "useRange",
            "\u533a\u95f4\u6a21\u5f0f",
            "\u5f00\u542f\u540e\u68c0\u6d4b\u4e0b\u9650\u5230\u4e0a\u9650\u7684\u95ed\u533a\u95f4",
            "bool"),
        new NodeParamInfo(
            "minValue",
            "\u4e0b\u9650",
            "\u533a\u95f4\u4e0b\u9650\uff080~100\uff0c\u6bcf 10 \u70b9\u4e00\u6863\uff09",
            "int"),
        new NodeParamInfo(
            "maxValue",
            "\u4e0a\u9650",
            "\u533a\u95f4\u4e0a\u9650\uff080~100\uff0c\u6bcf 10 \u70b9\u4e00\u6863\uff09",
            "int"),
    };

    public bool Draw()
    {
        var changed = false;

        if (ImGui.Combo("\u8fd0\u7b97\u7b26", ref _operatorIndex, OperatorLabels, OperatorLabels.Length))
        {
            _operatorIndex = Math.Clamp(_operatorIndex, 0, OperatorLabels.Length - 1);
            changed = true;
        }

        var useRange = _useRange;
        if (ImGui.Checkbox("\u533a\u95f4\u6a21\u5f0f\uff08\u63a8\u8350\u7acb\u5373\u68c0\u6d4b\u5f53\u505a\u8282\u70b9\uff09", ref useRange))
        {
            _useRange = useRange;
            changed = true;
        }

        if (_useRange)
        {
            var min = _minValue;
            var max = _maxValue;
            if (ImGui.InputInt("\u4e0b\u9650", ref min))
            {
                _minValue = ClampGauge(min);
                changed = true;
            }

            if (ImGui.InputInt("\u4e0a\u9650", ref max))
            {
                _maxValue = ClampGauge(max);
                changed = true;
            }

            if (_value != 0)
            {
                _value = 0;
                changed = true;
            }
        }
        else
        {
            var value = _value;
            if (ImGui.InputInt(GaugeLabel, ref value))
            {
                _value = ClampGauge(value);
                changed = true;
            }

            if (_minValue != 0 || _maxValue != 0)
            {
                _minValue = 0;
                _maxValue = 0;
                changed = true;
            }
        }

        return changed;
    }

    public string GetParam(string fieldName) => fieldName switch
    {
        "operator" => _operatorIndex.ToString(),
        "value" => _value.ToString(),
        "useRange" => _useRange.ToString(),
        "minValue" => _minValue.ToString(),
        "maxValue" => _maxValue.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case "operator":
                if (int.TryParse(value, out var op))
                {
                    _operatorIndex = Math.Clamp(op, 0, OperatorLabels.Length - 1);
                }
                else
                {
                    _operatorIndex = Array.IndexOf(OperatorLabels, value);
                    if (_operatorIndex < 0) _operatorIndex = 0;
                }
                break;
            case "value" when int.TryParse(value, out var single):
                _value = ClampGauge(single);
                break;
            case "useRange" when bool.TryParse(value, out var range):
                _useRange = range;
                break;
            case "minValue" when int.TryParse(value, out var min):
                _minValue = ClampGauge(min);
                break;
            case "maxValue" when int.TryParse(value, out var max):
                _maxValue = ClampGauge(max);
                break;
        }
    }

    public bool EvaluateImmediate() => Evaluate();

    public bool EvaluateWait() => Evaluate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
        Params = new Dictionary<string, string>
        {
            ["operator"] = _operatorIndex.ToString(),
            ["value"] = _value.ToString(),
            ["useRange"] = _useRange.ToString(),
            ["minValue"] = _minValue.ToString(),
            ["maxValue"] = _maxValue.ToString(),
        },
    };

    protected static int ClampGauge(int value)
    {
        var rounded = value / 10 * 10;
        return Math.Clamp(rounded, 0, 100);
    }

    private bool Evaluate()
    {
        var current = Math.Clamp(CurrentValue, 0, 100);
        if (_useRange)
        {
            return current >= _minValue && current <= _maxValue;
        }

        return _operatorIndex switch
        {
            0 => current == _value,
            1 => current != _value,
            2 => current > _value,
            3 => current >= _value,
            4 => current < _value,
            5 => current <= _value,
            _ => false,
        };
    }

    protected static void ReadParams(ReaperGaugeCondition condition, ConditionDto dto)
    {
        if (dto.Params == null) return;

        if (dto.Params.TryGetValue("operator", out var op)) condition.SetParam("operator", op);
        if (dto.Params.TryGetValue("value", out var value)) condition.SetParam("value", value);
        if (dto.Params.TryGetValue("useRange", out var range)) condition.SetParam("useRange", range);
        if (dto.Params.TryGetValue("minValue", out var min)) condition.SetParam("minValue", min);
        if (dto.Params.TryGetValue("maxValue", out var max)) condition.SetParam("maxValue", max);
    }
}

public sealed class ReaperSoulGaugeCondition : ReaperGaugeCondition
{
    protected override int CurrentValue => JobGaugeHelper.RPR.灵魂值;
    protected override string TypeKey => "luli_reaper_soul_gauge";
    protected override string GaugeLabel => "\u7ea2\u91cf\u503c";
    protected override string NodeLabel => "Reaper/\u65b0\u91cf\u8c31\u68c0\u6d4b/\u7ea2\u91cf\u68c0\u6d4b";

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, "luli_reaper_soul_gauge", dto =>
        {
            var condition = new ReaperSoulGaugeCondition();
            ReadParams(condition, dto);
            return condition;
        });
    }
}

public sealed class ReaperShroudGaugeCondition : ReaperGaugeCondition
{
    protected override int CurrentValue => JobGaugeHelper.RPR.魂衣值;
    protected override string TypeKey => "luli_reaper_shroud_gauge";
    protected override string GaugeLabel => "\u84dd\u91cf\u503c";
    protected override string NodeLabel => "Reaper/\u65b0\u91cf\u8c31\u68c0\u6d4b/\u84dd\u91cf\u68c0\u6d4b";

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, "luli_reaper_shroud_gauge", dto =>
        {
            var condition = new ReaperShroudGaugeCondition();
            ReadParams(condition, dto);
            return condition;
        });
    }
}
