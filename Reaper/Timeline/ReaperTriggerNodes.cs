using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using ErosUI;
using PromeRotation.Core;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Reaper.PR.Timeline;

internal static class ReaperTriggerNodeData
{
    internal static string[] QtNames => ReaperRotation.QtList.Keys.ToArray();

    internal static void SetQt(string key, bool value)
    {
        if (ReaperRotation.QtList.ContainsKey(key))
            ErosUIFramework.设置QT(key, value);
    }
}

public sealed class ReaperLegacyQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_trigger_qt";
    private const string ValuesParam = "qtValues";
    private readonly List<QtSetting> settings = new();

    public string NodeDisplayName => "Reaper/newQT";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(ValuesParam, "QT 设置", "按配置顺序批量设置 Reaper QT", "json"),
    };

    public bool Draw()
    {
        var changed = false;
        var names = ReaperTriggerNodeData.QtNames;
        var removeIndex = -1;
        for (var i = 0; i < settings.Count; i++)
        {
            var setting = settings[i];
            ImGui.PushID(i);
            var selected = Array.IndexOf(names, setting.Key);
            if (selected < 0) selected = 0;
            if (names.Length > 0 && ImGui.Combo("QT", ref selected, names, names.Length))
            {
                setting.Key = names[selected];
                changed = true;
            }

            var enabled = setting.Value;
            if (ImGui.Checkbox("开启", ref enabled))
            {
                setting.Value = enabled;
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("移除")) removeIndex = i;
            ImGui.PopID();
        }

        if (removeIndex >= 0)
        {
            settings.RemoveAt(removeIndex);
            changed = true;
        }

        if (ImGui.Button("添加 QT") && names.Length > 0)
        {
            settings.Add(new QtSetting { Key = names[0] });
            changed = true;
        }
        return changed;
    }

    public string GetParam(string fieldName)
        => fieldName == ValuesParam ? JsonSerializer.Serialize(settings) : string.Empty;

    public void SetParam(string fieldName, string value)
    {
        if (fieldName != ValuesParam || string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<List<QtSetting>>(value);
            if (loaded == null) return;
            settings.Clear();
            foreach (var setting in loaded)
            {
                if (setting != null && ReaperRotation.QtList.ContainsKey(setting.Key))
                    settings.Add(setting);
            }
        }
        catch (JsonException)
        {
            settings.Clear();
        }
    }

    public void Execute()
    {
        foreach (var setting in settings)
            ReaperTriggerNodeData.SetQt(setting.Key, setting.Value);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string> { [ValuesParam] = GetParam(ValuesParam) },
    };

    public static void Register(RotationNodeContext context)
        => ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ReaperLegacyQtAction();
            if (dto.Params?.TryGetValue(ValuesParam, out var values) == true)
                action.SetParam(ValuesParam, values);
            return action;
        });

    public sealed class QtSetting
    {
        public string Key { get; set; } = string.Empty;
        public bool Value { get; set; }
    }
}

public sealed class ReaperQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_trigger_qt2";
    private const string ValuesParam = "qtValues";
    private readonly Dictionary<string, bool> values = new(StringComparer.Ordinal);

    public string NodeDisplayName => "Reaper/QT";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(ValuesParam, "QT 状态", "点击按钮切换未添加、关闭、启用三种状态", "json"),
    };

    public bool Draw()
    {
        var changed = false;
        var names = ReaperTriggerNodeData.QtNames;
        ImGui.Text("点击 QT 名称切换：未添加 / 已关闭 / 已启用");
        for (var i = 0; i < names.Length; i++)
        {
            var key = names[i];
            ImGui.PushID(key);
            var color = values.TryGetValue(key, out var enabled)
                ? enabled ? new System.Numerics.Vector4(0f, 1f, 0f, 1f) : new System.Numerics.Vector4(1f, 0.4f, 0.7f, 1f)
                : System.Numerics.Vector4.One;
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            if (ImGui.Button(key))
            {
                if (!values.TryGetValue(key, out enabled)) values[key] = false;
                else if (!enabled) values[key] = true;
                else values.Remove(key);
                changed = true;
            }
            ImGui.PopStyleColor();
            ImGui.PopID();
            if ((i + 1) % 5 != 0 && i + 1 < names.Length) ImGui.SameLine();
        }

        if (values.Count == 0) return changed;
        ImGui.Separator();
        List<string>? remove = null;
        foreach (var entry in values.ToArray())
        {
            var enabled = entry.Value;
            ImGui.PushID(entry.Key);
            if (ImGui.Checkbox("##enabled", ref enabled))
            {
                values[entry.Key] = enabled;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.Text(entry.Key);
            ImGui.SameLine();
            ImGui.TextDisabled(enabled ? "（已启用）" : "（已关闭）");
            ImGui.SameLine();
            if (ImGui.SmallButton("删除")) (remove ??= new List<string>()).Add(entry.Key);
            ImGui.PopID();
        }

        if (remove != null)
        {
            foreach (var key in remove) values.Remove(key);
            changed = true;
        }

        if (ImGui.Button("全部启用"))
        {
            foreach (var key in values.Keys.ToArray()) values[key] = true;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.Button("全部关闭"))
        {
            foreach (var key in values.Keys.ToArray()) values[key] = false;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.Button("清除所有"))
        {
            values.Clear();
            changed = true;
        }
        return changed;
    }

    public string GetParam(string fieldName)
        => fieldName == ValuesParam ? JsonSerializer.Serialize(values) : string.Empty;

    public void SetParam(string fieldName, string value)
    {
        if (fieldName != ValuesParam || string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, bool>>(value);
            if (loaded == null) return;
            values.Clear();
            foreach (var entry in loaded)
            {
                if (ReaperRotation.QtList.ContainsKey(entry.Key)) values[entry.Key] = entry.Value;
            }
        }
        catch (JsonException)
        {
            values.Clear();
        }
    }

    public void Execute()
    {
        foreach (var entry in values)
            ReaperTriggerNodeData.SetQt(entry.Key, entry.Value);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string> { [ValuesParam] = GetParam(ValuesParam) },
    };

    public static void Register(RotationNodeContext context)
        => ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ReaperQtAction();
            if (dto.Params?.TryGetValue(ValuesParam, out var values) == true)
                action.SetParam(ValuesParam, values);
            return action;
        });
}

public sealed class ReaperHotkeyAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_trigger_hotkey";
    private string hotkey = string.Empty;
    private static string[] HotkeyNames => ErosUIFramework.GetHotkeyNames();

    public string NodeDisplayName => "Reaper/HotKey";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("hotkey", "热键", "执行 Reaper 热键面板中的指定动作", "enum",
            HotkeyNames.Select(name => (name, name)).ToArray()),
    };

    public bool Draw()
    {
        var names = HotkeyNames;
        if (names.Length == 0) return false;
        var selected = Array.IndexOf(names, hotkey);
        if (selected < 0) selected = 0;
        if (!ImGui.Combo("选择使用 HotKey", ref selected, names, names.Length)) return false;
        hotkey = names[selected];
        return true;
    }

    public string GetParam(string fieldName) => fieldName == "hotkey" ? hotkey : string.Empty;
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "hotkey") hotkey = value;
    }

    public void Execute() => ErosUIFramework.ExecuteHotkey(hotkey);

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string> { ["hotkey"] = hotkey },
    };

    public static void Register(RotationNodeContext context)
        => ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ReaperHotkeyAction();
            if (dto.Params?.TryGetValue("hotkey", out var key) == true) action.SetParam("hotkey", key);
            return action;
        });
}

public sealed class ReaperPotionSettingAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_trigger_potion_setting";
    private static readonly string[] Options = { "0612", "0510", "2814" };
    private int selectedIndex;

    public string NodeDisplayName => "Reaper/PotionSetting";
    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("potionType", "爆发药方案", "选择 Reaper 爆发药时机方案", "enum",
            Options.Select((value, index) => (index.ToString(), value)).ToArray()),
    };

    public bool Draw() => ImGui.Combo("爆发药方案", ref selectedIndex, Options, Options.Length);
    public string GetParam(string fieldName) => fieldName == "potionType" ? selectedIndex.ToString() : string.Empty;
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "potionType" && int.TryParse(value, out var parsed))
            selectedIndex = Math.Clamp(parsed, 0, Options.Length - 1);
    }

    public void Execute() => ReaperRotation.SetPotionType(selectedIndex);

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Enabled = true,
        Params = new Dictionary<string, string> { ["potionType"] = selectedIndex.ToString() },
    };

    public static void Register(RotationNodeContext context)
        => ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ReaperPotionSettingAction();
            if (dto.Params?.TryGetValue("potionType", out var value) == true) action.SetParam("potionType", value);
            return action;
        });
}

public abstract class ReaperTriggerThresholdCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    protected int Threshold;
    protected abstract string TypeKey { get; }
    protected abstract string ThresholdLabel { get; }
    public abstract string NodeDisplayName { get; }
    protected abstract bool Evaluate(int threshold);

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo("threshold", ThresholdLabel, "通过检测所需的阈值", "int"),
    };

    public bool Draw()
    {
        var value = Threshold;
        if (!ImGui.InputInt(ThresholdLabel, ref value)) return false;
        Threshold = value;
        return true;
    }

    public string GetParam(string fieldName) => fieldName == "threshold" ? Threshold.ToString() : string.Empty;
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "threshold" && int.TryParse(value, out var parsed)) Threshold = parsed;
    }

    public bool EvaluateImmediate() => Evaluate(Threshold);
    public bool EvaluateWait() => EvaluateImmediate();
    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
        Params = new Dictionary<string, string> { ["threshold"] = Threshold.ToString() },
    };

    protected static void RegisterType<T>(RotationNodeContext context, string typeKey)
        where T : ReaperTriggerThresholdCondition, new()
        => ConditionFactory.Register(context, typeKey, dto =>
        {
            var condition = new T();
            if (dto.Params?.TryGetValue("threshold", out var value) == true) condition.SetParam("threshold", value);
            return condition;
        });
}

public sealed class ReaperLegacyShroudGaugeCondition : ReaperTriggerThresholdCondition
{
    protected override string TypeKey => "luli_reaper_trigger_shroud_gauge";
    protected override string ThresholdLabel => "蓝量谱";
    public override string NodeDisplayName => "Reaper/蓝量谱";
    protected override bool Evaluate(int threshold) => JobGaugeHelper.RPR.魂衣值 >= threshold;
    public static void Register(RotationNodeContext context) => RegisterType<ReaperLegacyShroudGaugeCondition>(context, "luli_reaper_trigger_shroud_gauge");
}

public sealed class ReaperLegacySoulGaugeCondition : ReaperTriggerThresholdCondition
{
    protected override string TypeKey => "luli_reaper_trigger_soul_gauge";
    protected override string ThresholdLabel => "红量谱";
    public override string NodeDisplayName => "Reaper/红量谱";
    protected override bool Evaluate(int threshold) => JobGaugeHelper.RPR.灵魂值 >= threshold;
    public static void Register(RotationNodeContext context) => RegisterType<ReaperLegacySoulGaugeCondition>(context, "luli_reaper_trigger_soul_gauge");
}

public sealed class ReaperLemureShroudCondition : ReaperTriggerThresholdCondition
{
    protected override string TypeKey => "luli_reaper_trigger_lemure_shroud";
    protected override string ThresholdLabel => "附体量谱阈值";
    public override string NodeDisplayName => "Reaper/夜游魂量谱";
    protected override bool Evaluate(int threshold) => JobGaugeHelper.RPR.夜游魂 == threshold;
    public static void Register(RotationNodeContext context) => RegisterType<ReaperLemureShroudCondition>(context, "luli_reaper_trigger_lemure_shroud");
}

public sealed class ReaperNearbyEnemyCountCondition : ReaperTriggerThresholdCondition
{
    protected override string TypeKey => "luli_reaper_trigger_enemy_count";
    protected override string ThresholdLabel => "周围敌人数";
    public override string NodeDisplayName => "Reaper/敌人数量检测（自身为中心5米）";
    protected override bool Evaluate(int threshold)
    {
        var me = Core.Me;
        return me != null && (int)TargetHelper.EnemyInRangeTarget(me, 5f) >= threshold;
    }
    public static void Register(RotationNodeContext context) => RegisterType<ReaperNearbyEnemyCountCondition>(context, "luli_reaper_trigger_enemy_count");
}

