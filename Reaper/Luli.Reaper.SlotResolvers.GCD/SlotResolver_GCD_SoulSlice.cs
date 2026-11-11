using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;

namespace Reaper.PR;

public interface IAePriorityResolver
{
    int AePriority { get; }
}

public sealed class SoulSliceGcd : Gcd, IAePriorityResolver
{
    public int AePriority { get; private set; } = int.MinValue;

    public override CheckResult Check()
    {
        AePriority = int.MinValue;
        var me = Core.Me;
        var target = Core.Target;
        var charges = R.Charges(R.SoulSlice);
        var remainGcdMs = (long)(ActionHelper.GetGcdRemain() * 1000f);

        if (!R.Qt("灵魂切割")) return Reject(-1, "灵魂切割 QT 未开启");
        if (me == null || target == null || !R.Near()) return Reject(-2, "目标超出攻击范围");
        if (!R.HasUsableCharge(R.SoulSlice, target)) return Reject(-3, "灵魂切割尚不可施放");
        if (charges < 1f) return Reject(-4, "灵魂切割没有可用充能");
        if (R.Has(2587u) || R.Has(3858u)) return Reject(-5, "已有绞决或缢杀待消费");
        if (JobGaugeHelper.RPR.灵魂值 > 50) return Reject(-6, "当前灵魂值大于 50");
        if (R.Qt("死亡之影") && !R.TargetHas(2586u, 2f))
            return Reject(-7, "目标死亡烙印不足两秒");

        if (me.Level is >= 60 and <= 77)
        {
            if (!R.HasUsableCharge(R.SoulSlice, target)) return Reject(-8, "灵魂切割尚不可施放");
            return Accept(0, "60～77 级基础条件满足");
        }

        if (me.Level > 77)
        {
            // AE's threshold is 15 ms, while PR exposes status time in seconds.
            if (ReaperRotation.IsSelectedOpener("烙印3G团辅起手")
                && EngageManager.GetBattleTime() < 5f
                && !R.TargetHas(2586u, 0.015f))
                return Reject(-9, "烙印 3G 起手保护");

            var comboTimeLeft = ActionHelper.GetComboLeftTime();
            if (comboTimeLeft > 0f && comboTimeLeft < 2f && !R.Has(2972u))
                return Reject(-10, "连击剩余时间不足两秒");

            var nextChargeReady = R.Is1ChargesNextMs(R.SoulSlice, remainGcdMs);
            if (charges == 1f && nextChargeReady) return Accept(1, "一层充能即将恢复");
            if (charges == 2f) return Accept(2, "充能已满");
            if (charges == 1f) return Accept(0, "拥有一层充能");

            var circleCooldown = R.Cd(R.ArcaneCircle);
            if (R.Qt("附体") && R.Qt("双附体") && !R.Qt("三附体") && circleCooldown < 17.5f
                && JobGaugeHelper.RPR.灵魂值 <= 50 && charges >= 1f)
                return Accept(1, "双附体窗口前补充灵魂");

            if (R.Qt("附体") && R.Qt("三附体") && circleCooldown < 42.5f && circleCooldown > 17f
                && JobGaugeHelper.RPR.魂衣值 < 100 && charges >= 1f)
                return Accept(1, "三附体规划阶段补充灵魂");

            if (R.Qt("只打大丰收附体") && JobGaugeHelper.RPR.灵魂值 <= 50 && charges >= 1f)
                return Accept(1, "大丰收附体模式补充灵魂");

            if (R.Has(2972u) && R.Cd(R.Gluttony) >= 1f && JobGaugeHelper.RPR.魂衣值 < 50
                && JobGaugeHelper.RPR.灵魂值 <= 50 && !R.Qt("只打大丰收附体"))
                return Accept(1, "虚无收割期间补充资源");
        }

        return Accept(0, "灵魂切割基础条件满足");
    }

    public override PAction GetAction() => R.A(R.SoulSlice);

    private CheckResult Reject(int aeCode, string reason)
    {
        AePriority = aeCode;
        return new CheckResult(false, $"检查结果 {aeCode}：{reason}");
    }

    private CheckResult Accept(int aeCode, string reason)
    {
        AePriority = aeCode;
        return new CheckResult(true, $"检查结果 {aeCode}：{reason}");
    }
}
