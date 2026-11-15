using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("钻石星尘#1", "Diamond Dust（40197）", "P2", Order = 1, DisplayOrder = 1)]
public sealed class P2DiamondDustFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.DiamondDust);
}
