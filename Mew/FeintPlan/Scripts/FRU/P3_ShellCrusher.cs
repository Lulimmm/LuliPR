using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("破盾一击", "Shell Crusher（40286）", "P3", Order = 3, DisplayOrder = 2)]
public sealed class P3ShellCrusherFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.ShellCrusher);
}
