using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Ninja.PR.Timeline;

public sealed class NinjaJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        NinjaNinkiCondition.Register(context);
        NinjaSettingAction.Register(context);
        NinjaQtAction.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        => new (string, string, Func<ICondition>)[]
        {
            ("Ninja/忍气检测", "忍气达到指定值时通过", () => new NinjaNinkiCondition()),
        };

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        => new (string, string, Func<IAction>)[]
        {
            ("Ninja/设置开关", "在时间轴触发时设置 Ninja 循环选项", () => new NinjaSettingAction()),
            ("Ninja/设置 QT", "在时间轴触发时开关 Ninja QT", () => new NinjaQtAction()),
        };
}

public sealed class NinjaQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_ninja_qt";
    private static string[] QtNames => NinjaRotation.QtList.Keys.ToArray();
    private int qtIndex;
    private bool enabled = true;

    public string NodeDisplayName => "Ninja/设置 QT";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("qt", "QT", "需要修改的 Ninja QT", "enum",
            QtNames.Select(key => (key, key)).ToArray()),
        new NodeParamInfo("enabled", "启用", "开启或关闭所选 QT", "bool"),
    };

    public bool Draw()
    {
        var names = QtNames;
        if (names.Length == 0) return false;
        qtIndex = Math.Clamp(qtIndex, 0, names.Length - 1);
        var changed = ImGui.Combo("QT", ref qtIndex, names, names.Length);
        var value = enabled;
        if (ImGui.Checkbox("启用", ref value))
        {
            enabled = value;
            changed = true;
        }
        return changed;
    }

    public string GetParam(string fieldName) => fieldName switch
    {
        "qt" => QtNames.ElementAtOrDefault(qtIndex) ?? string.Empty,
        "enabled" => enabled.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "qt")
        {
            var index = Array.IndexOf(QtNames, value);
            if (index >= 0) qtIndex = index;
        }
        else if (fieldName == "enabled" && bool.TryParse(value, out var parsed))
        {
            enabled = parsed;
        }
    }

    public void Execute()
    {
        var key = GetParam("qt");
        if (!string.IsNullOrEmpty(key)) PromeSettings.Instance.SetQt(key, enabled);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string>
        {
            ["qt"] = GetParam("qt"),
            ["enabled"] = enabled.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new NinjaQtAction();
            if (dto.Params != null)
            {
                if (dto.Params.TryGetValue("qt", out var key)) action.SetParam("qt", key);
                if (dto.Params.TryGetValue("enabled", out var value)) action.SetParam("enabled", value);
            }
            return action;
        });
    }
}

public sealed class NinjaSettingAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_ninja_setting";
    private static readonly string[] SettingKeys =
    {
        "trickDelay", "forceRaiton", "raitonBackstab", "raitonRange", "p5Bunshin",
    };
    private static readonly string[] SettingLabels =
    {
        "下一个小背刺延后至大背刺", "强制雷遁", "雷遁背刺策略", "雷遁远程止损", "P5分身资源保留",
    };

    private int settingIndex;
    private bool enabled = true;

    public string NodeDisplayName => "Ninja/设置开关";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("setting", "设置", "需要修改的 Ninja 循环选项", "enum",
            new[]
            {
                ("trickDelay", SettingLabels[0]),
                ("forceRaiton", SettingLabels[1]),
                ("raitonBackstab", SettingLabels[2]),
                ("raitonRange", SettingLabels[3]),
                ("p5Bunshin", SettingLabels[4]),
            }),
        new NodeParamInfo("enabled", "启用", "开启或关闭所选选项", "bool"),
    };

    public bool Draw()
    {
        var changed = ImGui.Combo("设置", ref settingIndex, SettingLabels, SettingLabels.Length);
        var value = enabled;
        if (ImGui.Checkbox("启用", ref value))
        {
            enabled = value;
            changed = true;
        }
        return changed;
    }

    public string GetParam(string fieldName) => fieldName switch
    {
        "setting" => SettingKeys[Math.Clamp(settingIndex, 0, SettingKeys.Length - 1)],
        "enabled" => enabled.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "setting")
        {
            var index = Array.IndexOf(SettingKeys, value);
            if (index < 0) index = Array.IndexOf(SettingLabels, value);
            if (index >= 0) settingIndex = index;
        }
        else if (fieldName == "enabled" && bool.TryParse(value, out var parsed))
        {
            enabled = parsed;
        }
    }

    public void Execute()
    {
        var settings = NinjaSettings.Instance;
        switch (SettingKeys[Math.Clamp(settingIndex, 0, SettingKeys.Length - 1)])
        {
            case "trickDelay": settings.TrickAttackDelayUntilNextMug = enabled; break;
            case "forceRaiton": settings.ForceRaiton = enabled; break;
            case "raitonBackstab": settings.RaitonBackstab = enabled; break;
            case "raitonRange": settings.RaitonWhenOutOfRange = enabled; break;
            case "p5Bunshin": settings.P5PerfectBunshin = enabled; break;
        }
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string>
        {
            ["setting"] = GetParam("setting"),
            ["enabled"] = enabled.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new NinjaSettingAction();
            if (dto.Params != null)
            {
                if (dto.Params.TryGetValue("setting", out var setting)) action.SetParam("setting", setting);
                if (dto.Params.TryGetValue("enabled", out var value)) action.SetParam("enabled", value);
            }
            return action;
        });
    }
}

public sealed class NinjaNinkiCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "luli_ninja_ninki";
    private int threshold;

    public string NodeDisplayName => "Ninja/忍气检测";
    public NodeParamInfo[] Params => new[] { new NodeParamInfo("threshold", "忍气", "忍气达到此值时通过", "int") };

    public bool Draw()
    {
        var value = threshold;
        if (!ImGui.InputInt("忍气阈值", ref value)) return false;
        threshold = Math.Clamp(value, 0, 100);
        return true;
    }

    public string GetParam(string fieldName) => fieldName == "threshold" ? threshold.ToString() : string.Empty;

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "threshold" && int.TryParse(value, out var parsed))
            threshold = Math.Clamp(parsed, 0, 100);
    }

    public bool EvaluateImmediate() => JobGaugeHelper.NIN.Ninki >= threshold;
    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
        Params = new Dictionary<string, string> { ["threshold"] = threshold.ToString() },
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new NinjaNinkiCondition();
            if (dto.Params != null && dto.Params.TryGetValue("threshold", out var value))
                condition.SetParam("threshold", value);
            return condition;
        });
    }
}
