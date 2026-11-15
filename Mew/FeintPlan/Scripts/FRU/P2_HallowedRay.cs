using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("神圣光线#1", "Hallowed Ray（40210）", "P2", Order = 2, DisplayOrder = 2)]
public sealed class P2HallowedRayFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.HallowedRay);
}

[FeintNode("绝对零度#1", "Absolute Zero（40224）", "P2", Order = 3, DisplayOrder = 4)]
public sealed class P2AbsoluteZeroFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.AbsoluteZero);
}
