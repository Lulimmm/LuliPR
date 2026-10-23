using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using PromeRotation.Core;
using PromeRotation.Timeline.Core;

namespace Reaper.PR.Timeline;

/// <summary>
/// Equivalent of AEAssist's TriggerCondition_PotionCheck. It succeeds when
/// the current job has a usable potion configured and present in inventory.
/// </summary>
public sealed class ReaperPotionCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "luli_reaper_potion_check";

    public string NodeDisplayName => "General/\u68c0\u6d4b\u7206\u53d1\u836f";

    public NodeParamInfo[] Params => Array.Empty<NodeParamInfo>();

    public bool Draw()
    {
        ImGui.Text("\u80fd\u7528\u5c31\u901a\u8fc7\u68c0\u6d4b\uff0c\u5305\u5185\u9700\u8981\u6709\u8bbe\u7f6e\u4e2d\u586b\u4e86 ID \u7684\u5bf9\u5e94\u7206\u53d1\u836f");
        return true;
    }

    public string GetParam(string fieldName) => string.Empty;

    public void SetParam(string fieldName, string value)
    {
        // This condition has no configurable parameters.
    }

    public bool EvaluateImmediate() => GameData.GetBestPotionId() != 0;

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Immediate = true,
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, _ => new ReaperPotionCondition());
    }
}
