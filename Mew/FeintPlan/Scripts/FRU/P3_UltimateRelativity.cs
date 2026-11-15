using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("时间压缩·绝", "Ultimate Relativity（40266）", "P3", Order = 1, DisplayOrder = 1, SequenceName = "D1宇宙重力/冲击波星云", SerialId = 1u)]
[FeintNode("脉冲星震波", "等待 70 秒、Shockwave Pulsar（40282）后延迟 3 秒", "P3", Order = 2, DisplayOrder = 3, SequenceName = "D1宇宙重力/冲击波星云", SerialId = 2u)]
public sealed class P3UltimateRelativityFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.UltimateRelativity),
            2u => FRUData.AfterDelayedCast(nodeId, "P3-D1-Second", condParams, 3_000,
                FRUData.读条.ShockwavePulsar),
            _ => false
        };
}
