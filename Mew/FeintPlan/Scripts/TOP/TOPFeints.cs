using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.TOP;

// Source enabled: D1, node 309. Boss cast 31499, then 14 seconds.
[FeintNode("P1 全能之主·D1#1", "启用：读条 31499 后延迟 14 秒", "P1", Order = 1)]
public sealed class P1D1Feint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 14_000, 31499u);
}
// Source enabled: D2, node 347. Boss cast 31499, then 26 seconds.
[FeintNode("P1 全能之主·D2#1", "启用：读条 31499 后延迟 26 秒", "P1", Order = 2)]
public sealed class P1D2Feint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 26_000, 31499u);
}

// Source enabled: D1, node 313.
[FeintNode("P3 你好，世界#1", "启用：读条 31573 后延迟 3 秒", "P3", Order = 1)]
public sealed class P3HelloWorldFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 3_000, 31573u);
}

// Source enabled: D1, node 316.
[FeintNode("P3 严重错误#1", "启用：读条 31588 后延迟 3 秒", "P3", Order = 2)]
public sealed class P3CriticalErrorFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 3_000, 31588u);
}

// Source disabled: node 320 and its parent P4 sequence are both disabled.
[FeintNode("P4 波动炮分摊#1", "时间轴默认关闭：读条 31617 后延迟 2 秒", "P4", Order = 1, EnabledByDefault = false)]
public sealed class P4D1DisabledFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 2_000, 31617u);
}

// Source enabled: D1, node 324.
[FeintNode("P5 一运·德尔塔#1", "启用：读条 31624 后延迟 1 秒", "P5", Order = 1)]
public sealed class P5DeltaFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 1_000, 31624u);
}

// Source enabled: D1, node 327.
[FeintNode("P5 三运·欧米茄#1", "启用：读条 32789 后延迟 3 秒", "P5", Order = 2)]
public sealed class P5OmegaFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 3_000, 32789u);
}

// Source enabled: D2, node 355.
[FeintNode("P4 速射炮#1", "启用：读条 31567 后延迟 5 秒", "P4", Order = 2)]
public sealed class P4RapidFireFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 5_000, 31567u);
}

// Source enabled: D2, node 359.
[FeintNode("P5 二运·西格玛#1", "启用：读条 32788 后延迟 1 秒", "P5", Order = 3)]
public sealed class P5SigmaFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 1_000, 32788u);
}

// Source enabled: D1, nodes 332 and 335.
[FeintNode("P6 宇宙龙炎·第一轮#1", "启用：读条 31654 后延迟 2 秒", "P6", Order = 1)]
[FeintNode("P6 宇宙龙炎·第二轮#1", "启用：读条 31657 后延迟 2 秒", "P6", Order = 2)]
public sealed class P6D1DragonfireFeints : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => TOPData.AfterCast(nodeId, condParams, 2_000, 31654u),
            2u => TOPData.AfterCast(nodeId, condParams, 2_000, 31657u),
            _ => false
        };
}

// Source disabled: node 341 sits under the disabled D1 P6 two-charge sequence.
[FeintNode("P6 宇宙龙炎·二挡备选#1", "时间轴默认关闭：两次读条 31657 间隔至少 12 秒，第二次后延迟 2 秒", "P6", Order = 3, EnabledByDefault = false)]
public sealed class P6D1SecondChargeOption : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterSecondCast(nodeId, condParams, 31657u, 12_000, 2_000);
}

// Source enabled: D2, node 369. Two 31657 casts, at least 12 seconds apart, then 4 seconds.
[FeintNode("P6 二挡陨石#1", "启用：两次读条 31657 间隔至少 12 秒，第二次后延迟 4 秒", "P6", Order = 4)]
public sealed class P6D2SecondChargeFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterSecondCast(nodeId, condParams, 31657u, 12_000, 4_000);
}

// Source disabled: node 363 sits under the disabled D2 P6 one-charge sequence.
[FeintNode("P6 一挡陨石备选#1", "时间轴默认关闭：读条 31657 后延迟 2 秒", "P6", Order = 5, EnabledByDefault = false)]
public sealed class P6D2OneChargeOption : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => TOPData.AfterCast(nodeId, condParams, 2_000, 31657u);
}

// Source enabled: D2, nodes 374 and 377. 31657 + 3 seconds, then 31664 + 8 seconds.
[FeintNode("P6 一挡陨石#1", "启用：读条 31657 后延迟 3 秒", "P6", Order = 3, SequenceName = "P6 D2 一挡陨石", SerialId = 1u)]
[FeintNode("P6 宇宙彗星#2", "启用：读条 31664 后延迟 8 秒", "P6", Order = 6, SequenceName = "P6 D2 一挡陨石", SerialId = 2u)]
public sealed class P6D2MeteorFeints : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => TOPData.AfterCast(nodeId, condParams, 3_000, 31657u),
            2u => TOPData.AfterCast(nodeId, condParams, 8_000, 31664u),
            _ => false
        };
}

// Source disabled: nodes 381 and 384 sit under the disabled D2 P6 dragonfire sequence.
[FeintNode("P6 二龙炎备选·第一轮#1", "时间轴默认关闭：读条 31654 后延迟 2 秒", "P6", Order = 7, EnabledByDefault = false)]
[FeintNode("P6 二龙炎备选·第二轮#2", "时间轴默认关闭：读条 31657 后延迟 2 秒", "P6", Order = 8, EnabledByDefault = false)]
public sealed class P6D2DragonfireOption : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => TOPData.AfterCast(nodeId, condParams, 2_000, 31654u),
            2u => TOPData.AfterCast(nodeId, condParams, 2_000, 31657u),
            _ => false
        };
}
