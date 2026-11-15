using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("星灵之剑#1", "Fulgent Blade 循环 1/3（40306）", "P5", Order = 1, DisplayOrder = 1)]
[FeintNode("星灵之剑#2", "Fulgent Blade 循环 2/3，间隔 90 秒", "P5", Order = 2, DisplayOrder = 3)]
[FeintNode("星灵之剑#3", "Fulgent Blade 循环 3/3，间隔 90 秒", "P5", Order = 3, DisplayOrder = 5)]
public sealed class P5FulgentBladeFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterP5LoopCast(nodeId, serialId, "P5-D1-Next", condParams, FRUData.读条.FulgentBlade);
}
