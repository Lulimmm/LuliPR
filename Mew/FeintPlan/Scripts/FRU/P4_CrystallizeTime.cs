using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("时间结晶", "Crystallize Time（40298/40240），目标 BaseId 17835", "P4", 17835u, Order = 1, DisplayOrder = 2)]
public sealed class P4CrystallizeTimeFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0,
            FRUData.读条.CrystallizeTime, FRUData.读条.CrystallizeTimeAlternate);
}
