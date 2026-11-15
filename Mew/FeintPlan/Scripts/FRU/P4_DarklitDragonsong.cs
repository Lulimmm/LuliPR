using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("光与暗的龙诗#1", "光与暗的龙诗（40239/40301），目标 BaseId 17833", "P4", 17833u, Order = 2, DisplayOrder = 1)]
[FeintNode("光与暗的龙诗#2", "等待 100 秒后死亡轮回（40302/40247），目标 BaseId 17833", "P4", 17833u, Order = 3, DisplayOrder = 3)]
public sealed class P4DarklitDragonsongFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => FRUData.AfterCast(nodeId, condParams, 0,
                FRUData.读条.DarklitDragonsong, FRUData.读条.DarklitDragonsongAlternate),
            2u => FRUData.AfterDelayedCast(nodeId, "P4-D2-Second", condParams, 0,
                FRUData.读条.AkhMorn, FRUData.读条.AkhMornAlternate),
            _ => false
        };
}
