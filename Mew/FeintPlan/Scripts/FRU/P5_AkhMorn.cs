using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("死亡轮回#1", "死亡轮回循环 1/3（40310）", "P5", Order = 4, DisplayOrder = 2)]
[FeintNode("死亡轮回#2", "死亡轮回循环 2/3，间隔 90 秒", "P5", Order = 5, DisplayOrder = 4)]
[FeintNode("死亡轮回#3", "死亡轮回循环 3/3，间隔 90 秒", "P5", Order = 6, DisplayOrder = 6)]
public sealed class P5AkhMornFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterP5LoopCast(nodeId, serialId, "P5-D2-Next", condParams, FRUData.读条.AkhMornLoop);
}
