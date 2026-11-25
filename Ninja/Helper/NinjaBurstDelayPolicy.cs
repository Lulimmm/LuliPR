using PromeRotation.Data;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

// Fixed openers and manual hotkeys own their actions; only normal decisions consult this policy.
internal static class NinjaBurstDelayPolicy
{
    public const float MudraOverflowThreshold = 1.8f;
    public const string BlockReason = "延后爆发开启，暂不使用该爆发技能";
    public static bool Enabled => PromeSettings.Instance.GetQt(NinjaQts.DelayBurst);
    public static bool ShouldSpendMudra(float charges) => charges >= MudraOverflowThreshold;
}
