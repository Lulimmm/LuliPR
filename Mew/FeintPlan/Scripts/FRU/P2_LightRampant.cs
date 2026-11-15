using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("光之失控#1", "Light Rampant（40212）", "P2", Order = 4, DisplayOrder = 3)]
public sealed class P2LightRampantFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.LightRampant);
}
