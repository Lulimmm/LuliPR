using System.Collections.Generic;

namespace Ninja.PR.Helper;

// Describes observed state only. No method here selects, submits or resets actions.
internal sealed record NinjaBattleDebugSnapshot
{
    public bool HasPlayer { get; init; }
    public bool Dead { get; init; }
    public bool AcrPaused { get; init; }
    public bool HostAllowsAcr { get; init; }
    public bool Occupied { get; init; }
    public bool Casting { get; init; }
    public float Countdown { get; init; }
    public float AnimationLock { get; init; }
    public float GcdRemain { get; init; }
    public bool? HostOpenerActive { get; init; }
    public bool HasActiveCommand { get; init; }
    public bool HasQueuedActions { get; init; }
    public bool GcdLocked { get; init; }
    public bool TargetPresent { get; init; }
    public bool TargetAttackable { get; init; }
    public float Distance { get; init; }
    public float MeleeRange { get; init; }
    public bool HoldsNormalDecisions { get; init; }
    public bool TcjActive { get; init; }
    public bool NormalMudraActive { get; init; }
    public bool ShadowWalker { get; init; }
    public float MudraCharges { get; init; }
    public bool PhantomUrgent { get; init; }
    public bool TenriEligible { get; init; }
    public bool PhantomBurst { get; init; }
    public string GcdCandidate { get; init; } = "无";
    public bool CanUsePostNinjutsuOffGcd { get; init; }
    public bool CanUseGcdOffGcd { get; init; }
    public bool CanUseCurrentGcdAbility { get; init; }
    public int PostNinjutsuUsed { get; init; }
    public int PostNinjutsuLimit { get; init; }
    public int GcdOffGcdUsed { get; init; }
    public int GcdOffGcdLimit { get; init; }
    public bool CommandReadable { get; init; }
    public string ActiveAction { get; init; } = "无";
    public bool ActiveTargetsEnemy { get; init; }
    public bool ActiveIsGroup { get; init; }
    public bool? ActiveReady { get; init; }
    public bool AwaitingVerification { get; init; }
    public string AwaitedAction { get; init; } = "所尝试技能";
    public long AttemptAgeMs { get; init; }
    public int RetryCount { get; init; }
    public bool SequenceActive { get; init; }

    public string DescribeHost()
    {
        if (!HasPlayer) return "自身角色不可用，无法执行技能。";
        if (Dead) return "自身已死亡，等待复活。";
        if (AcrPaused) return "ACR 已关闭或处于停手状态，普通技能不执行。";
        if (Countdown > 0) return $"倒计时剩余 {Countdown:F1} 秒；战前技能交给倒计时队列。";
        if (Occupied) return "角色处于交互、过场或其他占用状态，宿主暂停普通决策。";
        if (!HostAllowsAcr) return "宿主执行条件未满足；已有队列或倒计时任务仍可能继续处理。";
        if (Casting) return "自身正在读条，等待读条结束。";
        if (AnimationLock > 0) return $"动作锁剩余 {AnimationLock:F2} 秒，等待动作锁结束。";
        if (HostOpenerActive == true) return "宿主仍在执行固定起手队列，尚未交回普通循环。";
        if (HasActiveCommand) return "调度器已有活动命令；新的普通 GCD 等待该命令结束。";
        if (GcdLocked) return "宿主仍锁定上一条 GCD，尚不能选择新的普通 GCD。";
        if (HasQueuedActions) return "队列中仍有技能；是否可继续选择取决于对应队列是否为空。";
        if (GcdRemain > .3f) return $"GCD 剩余 {GcdRemain:F2} 秒，尚未进入普通 GCD 的 0.30 秒选择窗口。";
        return "未观察到上述宿主等待条件；实际提交结果见活动命令与原始调度器信息。";
    }

    public string DescribeGcd()
    {
        // Keep this order aligned with NinjaRotation.NextGcd, including existing holds.
        if (HoldsNormalDecisions)
            return TcjActive
                ? "无损起手等待标志拦截全部 GCD；天地人后续忍术也尚未进入选择。"
                : "无损起手等待标志拦截普通 GCD，等待原起手/天地人队列交接。";
        if (!TargetPresent) return "没有选中目标，普通 GCD 返回空；恢复可攻击目标后才重新判断。";
        if (!TargetAttackable) return "目标存在但当前不可攻击，普通 GCD 返回空；可能处于机制退场或换场。";
        if (TcjActive) return "天地人状态独占 GCD，只评估天地人后续忍术，普通连击暂停。";
        if (NormalMudraActive) return "普通结印进行中，等待已入队的印与终结忍术；不另选普通 GCD。";
        if (PhantomUrgent) return "镰鼬状态即将赶不上下一次背刺，优先选择镰鼬。";
        if (TenriEligible) return "天理人道条件满足，当前 GCD 选择让位；能力技是否可用还要检查额度与队列。";
        if (PhantomBurst) return "爆发状态内镰鼬条件满足，优先于普通连击。";
        if (GcdCandidate != "无") return $"按优先级首个条件满足：{GcdCandidate}；仍需宿主允许选择和提交。";
        return Distance > MeleeRange
            ? $"未找到满足条件的 GCD；距离 {Distance:F1} 米超过近战范围 {MeleeRange:F1} 米，远程技能也未命中条件。"
            : "未找到满足条件的 GCD，请展开技能条件查看各分支原因。";
    }

    public string DescribeOffGcd()
    {
        if (TcjActive) return "天地人独占期间，普通能力技与优先能力技（Always）均返回空。";
        if (HoldsNormalDecisions) return "无损起手等待期间，普通能力技与优先能力技均返回空。";
        if (!CanUsePostNinjutsuOffGcd)
            return $"忍术后能力技额度已用完（{PostNinjutsuUsed}/{PostNinjutsuLimit}），等待下一次 GCD 确认。";
        if (!CanUseGcdOffGcd)
            return $"本 GCD 能力技额度已用完（{GcdOffGcdUsed}/{GcdOffGcdLimit}），等待下一次 GCD 确认。";
        if (!CanUseCurrentGcdAbility) return "当前 GCD 的单能力技限制已用完，等待下一次 GCD 确认。";
        return "Ninja 入口额度允许评估能力技；各技能仍需满足目标、距离、冷却、时机和优先级条件。";
    }

    public string DescribeCommand()
    {
        if (!HasActiveCommand) return "当前没有活动命令。";
        if (!CommandReadable) return "存在活动命令，但当前宿主未提供可读取的技能详情；请查看原始调度器。";
        var target = ActiveTargetsEnemy && !TargetAttackable
            ? "该技能需要敌方目标，但当前没有可攻击目标。"
            : "";
        var verification = AwaitingVerification
            ? $"等待{AwaitedAction}的技能确认，已观察 {AttemptAgeMs / 1000f:F1} 秒，宿主重试 {RetryCount} 次。"
            : "尚未观察到等待确认记录，可能仍在等待提交时机。";
        var availability = ActiveReady == false ? "SDK 报告技能未就绪，需结合冷却与状态检查。" : "";
        return $"{ActiveAction}；{(ActiveIsGroup ? "连续技能组" : "单条命令")}。{target}{verification}{availability}";
    }

    public IEnumerable<string> Observations()
    {
        if (!TargetAttackable && (HasActiveCommand || HasQueuedActions))
            yield return "目标暂不可攻击，但命令或队列仍存在；无目标不代表旧序列已取消。";
        if (HoldsNormalDecisions && TcjActive)
            yield return "无损起手等待与天地人状态同时存在：等待标志在天地人后续忍术选择之前返回空。";
        if (!SequenceActive && ActiveIsGroup)
            yield return "连续技能组仍在处理；序列策略未激活不等于技能组已清空。";
        if (AwaitingVerification && AttemptAgeMs >= 3000)
            yield return "同一技能的等待确认已持续至少 3 秒；需区分旧命令未结束与同一技能重复入队。";
        if (TenriEligible && (!CanUsePostNinjutsuOffGcd || !CanUseGcdOffGcd || !CanUseCurrentGcdAbility))
            yield return "天理人道自身条件满足，但能力技入口额度已用完；满足技能条件不代表已经提交。";
    }
}
