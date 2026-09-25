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
        // Check() is called every decision frame. Never carry a successful
        // priority over from an earlier frame whose conditions no longer hold.
        AePriority = int.MinValue;

        var me = Core.Me;
        var target = Core.Target;

        var remainGcd = ActionHelper.GetGcdRemain();
        var currentCharges = R.Charges(R.SoulSlice);

        // -1：检查是否开启“灵魂切割”
        if (!R.Qt("灵魂切割"))
        {
            return Reject(-1, "灵魂切割 QT 未开启");
        }

        // -2：检查当前目标和攻击距离
        //
        // AE：
        // Core.Me.Distance(currTarget) > attackRange
        //
        // PR：
        // R.Near() 会使用当前攻击距离，并兼容长臂猿的额外范围设置。
        if (me == null || target == null || !R.Near())
        {
            return Reject(-2, "没有有效目标或目标超出攻击范围");
        }

        // -3：技能是否已解锁。Soul Slice 是充能技能，PR 的
        // ActionHelper.IsReady() 要求整组冷却为 0；当已有一层、第二层
        // 仍在恢复时会错误返回 false。因此可用性由已解锁 + charges >= 1
        // 共同判断，不能使用 R.Ready()。
        if (!ActionHelper.IsUnlocked(R.SoulSlice))
        {
            return Reject(-3, "灵魂切割尚未就绪");
        }

        // -4：必须至少拥有一层充能
        if (currentCharges < 1f)
        {
            return Reject(-4, "灵魂切割没有可用充能");
        }

        // -5：魂衣阶段或已有妖异之镰 GCD 待消费时不使用。
        //
        // 2587 / 3858 是绞决、缢杀等妖异之镰技能的待消费状态；
        // 2593 与职业量谱则共同表示夜游魂衣阶段。只检查 2587 / 3858
        // 会漏掉正常的附体窗口，以及 Buff 刚切换但量谱已经更新的帧。
        if (R.Has(2587u)
            || R.Has(3858u)
            || R.InShroud
            || R.InEnshroud)
        {
            return Reject(-5, "当前处于附体/夜游魂衣阶段或有妖异之镰待消费");
        }

        // -6：灵魂值大于 50 时不使用，避免灵魂资源溢出
        if (JobGaugeHelper.RPR.灵魂值 > 50)
        {
            return Reject(-6, "当前灵魂值大于 50");
        }

        // -7：开启死亡之影时，如果自己的死亡烙印不足两秒，
        // 优先留出 GCD 补死亡烙印
        if (R.Qt("死亡之影") && !R.TargetHas(2586u, 2f))
        {
            return Reject(-7, "目标死亡烙印不足两秒");
        }

        // 60～77 级逻辑
        //
        // AE 原始代码的实际等级范围是 60～77，
        // 虽然注释中写的是 70～77，这里按代码条件转换。
        if (me.Level is >= 60 and <= 77)
        {
            // AE 原逻辑在这里又检查了一次技能是否就绪。
            // 为保持条件完整，PR 版本也保留这次检查。
            if (!ActionHelper.IsUnlocked(R.SoulSlice))
            {
                return Reject(-8, "60～77 级时灵魂切割尚未就绪");
            }

            return Accept(0, "60～77 级基础使用条件满足");
        }

        // 78 级以上的主要输出循环
        if (me.Level > 77)
        {
            // -9：烙印 3G 团辅起手保护
            //
            // AE：
            // CurrBattleTimeInMs < 5000
            //
            // PR：
            // EngageManager.GetBattleTime() 的单位为秒。
            if (ReaperRotation.IsSelectedOpener("烙印3G团辅起手")
                && EngageManager.GetBattleTime() < 5f
                && !R.TargetHas(2586u, 15f))
            {
                return Reject(-9, "烙印3G团辅起手阶段尚未建立死亡烙印");
            }

            // -10：连击即将断裂时不插入灵魂切割
            //
            // 2972：虚无收割相关状态。
            var comboTimeLeft = ActionHelper.GetComboLeftTime();
            if (comboTimeLeft > 0f
                && comboTimeLeft < 2f
                && !R.Has(2972u))
            {
                return Reject(-10, "基础连击剩余时间不足两秒");
            }

            // 以当前剩余 GCD 作为预判时长，
            // 判断打完本次 GCD 后是否即将恢复下一层充能。
            var isNextChargeSoon = R.Is1ChargesNextMs(
                R.SoulSlice,
                (long)(remainGcd * 1000f));

            // AE 返回 1：
            // 只有一层，但打完当前 GCD 后即将恢复下一层，
            // 因此允许使用，避免充能恢复时间浪费。
            if (currentCharges >= 1f && currentCharges < 2f && isNextChargeSoon)
            {
                return Accept(1, "一层充能且下一层即将恢复");
            }

            // AE 返回 2：
            // 两层充能已满，优先使用防止溢出。
            if (currentCharges >= 2f)
            {
                return Accept(2, "灵魂切割充能已满");
            }

            // AE 返回 0：
            // 有一层充能，下一层不会立即恢复，正常使用。
            if (currentCharges >= 1f && currentCharges < 2f)
            {
                return Accept(0, "拥有一层可用充能");
            }

            // 双附体逻辑：
            //
            // AE 检查的 24405 是神秘环，不是死亡之影。
            // PR 中对应 R.ArcaneCircle。
            if (R.Qt("附体")
                && R.Qt("双附体")
                && !R.Qt("三附体")
                && R.Cd(R.ArcaneCircle) < 17.5f)
            {
                if (JobGaugeHelper.RPR.灵魂值 <= 50
                    && currentCharges >= 1f)
                {
                    return Accept(1, "双附体窗口前补充灵魂资源");
                }
            }

            // 三附体逻辑：
            //
            // 神秘环冷却位于 17～42.5 秒时，
            // 如果魂衣值尚未到 100，则允许使用灵魂切割。
            if (R.Qt("附体")
                && R.Qt("三附体")
                && R.Cd(R.ArcaneCircle) < 42.5f
                && R.Cd(R.ArcaneCircle) > 17f)
            {
                if (JobGaugeHelper.RPR.魂衣值 < 100
                    && currentCharges >= 1f)
                {
                    return Accept(1, "三附体规划阶段补充灵魂资源");
                }
            }

            // 只打大丰收附体模式
            if (R.Qt("只打大丰收附体")
                && JobGaugeHelper.RPR.灵魂值 <= 50
                && currentCharges >= 1f)
            {
                return Accept(1, "只打大丰收附体模式下补充灵魂资源");
            }

            // 虚无收割状态下的资源补充
            //
            // 2972：虚无收割相关状态
            // 24393：暴食
            //
            // AE 使用 Cooldown.TotalMilliseconds >= 1000，
            // PR 的 R.Cd() 单位为秒，因此对应 >= 1f。
            if (R.Has(2972u)
                && R.Cd(R.Gluttony) >= 1f
                && JobGaugeHelper.RPR.魂衣值 < 50
                && JobGaugeHelper.RPR.灵魂值 <= 50
                && !R.Qt("只打大丰收附体"))
            {
                return Accept(1, "虚无收割状态下灵魂和魂衣资源不足");
            }
        }

        // 对应 AE 最后的 return 0。
        return Accept(0, "灵魂切割基础条件满足");
    }

    public override PAction GetAction()
    {
        return R.A(R.SoulSlice);
    }

    private CheckResult Reject(int aeCode, string reason)
    {
        AePriority = aeCode;
        return new CheckResult(
            false,
            $"AE检查结果 {aeCode}：{reason}");
    }

    private CheckResult Accept(int aePriority, string reason)
    {
        AePriority = aePriority;
        return new CheckResult(
            true,
            $"AE优先级 {aePriority}：{reason}");
    }
}
