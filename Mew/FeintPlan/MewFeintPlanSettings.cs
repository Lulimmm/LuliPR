using System.Collections.Generic;

namespace Mew.FeintPlan;

public sealed class MewFeintPlanSettings
{
    public Dictionary<uint, ZoneFeintPlan> ZonePlans { get; set; } = new();
    public bool GlobalEnabled { get; set; } = true;
    public bool ShowDebugInfo { get; set; }

    public ZoneFeintPlan GetOrCreateZonePlan(uint zoneId)
    {
        if (ZonePlans.TryGetValue(zoneId, out var plan))
            return plan;

        plan = new ZoneFeintPlan { ZoneId = zoneId };
        ZonePlans[zoneId] = plan;
        return plan;
    }
}
