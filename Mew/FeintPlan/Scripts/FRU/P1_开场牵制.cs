using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

[FeintNode("暴风破", "开场延迟 3 秒", "P1", Order = 1, DisplayOrder = 1, SequenceName = "D1开场牵制", SerialId = 1u)]
[FeintNode("光焰圆光#2", "Sinblaze/Sinsmite 后延迟 5 秒", "P1", Order = 2, DisplayOrder = 3, SequenceName = "D1开场牵制", SerialId = 2u)]
public sealed class P1D1OpeningFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => serialId switch
        {
            1u => FRUData.AfterCombatDelay(nodeId, 3_000),
            2u => FRUData.AfterDamageEffect(nodeId, condParams, 5_000,
                FRUData.伤害判定.Sinblaze, FRUData.伤害判定.Sinsmite),
            _ => false
        };
}

[FeintNode("光焰圆光#1", "光焰圆光（40170）", "P1", Order = 3, DisplayOrder = 2)]
public sealed class P1D2BurnishedGloryFeint : IFeintNode
{
    public bool ShouldTrigger(uint nodeId, uint serialId, int currTimeInMs, IMewFeintEvent? condParams)
        => FRUData.AfterCast(nodeId, condParams, 0, FRUData.读条.BurnishedGlory);
}
