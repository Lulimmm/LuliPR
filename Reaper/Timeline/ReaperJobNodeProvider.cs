using System;
using System.Collections.Generic;
using PromeRotation.Timeline.Core;

namespace Reaper.PR.Timeline;

/// <summary>
/// Timeline nodes that read the live Reaper job gauge.
/// </summary>
public sealed class ReaperJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        ReaperSoulGaugeCondition.Register(context);
        ReaperShroudGaugeCondition.Register(context);
        ReaperPotionCondition.Register(context);
        ReaperLegacyShroudGaugeCondition.Register(context);
        ReaperLegacySoulGaugeCondition.Register(context);
        ReaperLemureShroudCondition.Register(context);
        ReaperNearbyEnemyCountCondition.Register(context);
        ReaperLegacyQtAction.Register(context);
        ReaperQtAction.Register(context);
        ReaperHotkeyAction.Register(context);
        ReaperPotionSettingAction.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        => new (string, string, Func<ICondition>)[]
        {
            ("Reaper/\u65b0\u91cf\u8c31\u68c0\u6d4b/\u7ea2\u91cf\u68c0\u6d4b",
                "\u5b9e\u65f6\u68c0\u6d4b\u9b42\u91cf\u8c31\uff0c\u652f\u6301\u6bd4\u8f83\u548c\u533a\u95f4\u6a21\u5f0f",
                () => new ReaperSoulGaugeCondition()),
            ("Reaper/\u65b0\u91cf\u8c31\u68c0\u6d4b/\u84dd\u91cf\u68c0\u6d4b",
                "\u5b9e\u65f6\u68c0\u6d4b\u9b42\u8863\u91cf\u8c31\uff0c\u652f\u6301\u6bd4\u8f83\u548c\u533a\u95f4\u6a21\u5f0f",
                () => new ReaperShroudGaugeCondition()),
            ("General/\u68c0\u6d4b\u7206\u53d1\u836f",
                "\u5f53\u524d\u804c\u4e1a\u80cc\u5305\u4e2d\u5b58\u5728\u53ef\u7528\u7206\u53d1\u836f\u65f6\u901a\u8fc7\u68c0\u6d4b",
                () => new ReaperPotionCondition()),
            ("Reaper/蓝量谱", "检测魂衣量谱达到指定下限", () => new ReaperLegacyShroudGaugeCondition()),
            ("Reaper/红量谱", "检测灵魂量谱达到指定下限", () => new ReaperLegacySoulGaugeCondition()),
            ("Reaper/夜游魂量谱", "检测夜游魂量谱等于指定数值", () => new ReaperLemureShroudCondition()),
            ("Reaper/敌人数量检测（自身为中心5米）", "检测玩家周围5米内敌人数达到指定下限", () => new ReaperNearbyEnemyCountCondition()),
        };

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        => new (string, string, Func<IAction>)[]
        {
            ("Reaper/newQT", "批量设置多个 Reaper QT 开关", () => new ReaperLegacyQtAction()),
            ("Reaper/QT", "三态选择多个 Reaper QT 并批量设置", () => new ReaperQtAction()),
            ("Reaper/HotKey", "执行 Reaper 热键面板中的指定动作", () => new ReaperHotkeyAction()),
            ("Reaper/PotionSetting", "设置 Reaper 爆发药时机方案", () => new ReaperPotionSettingAction()),
        };
}
