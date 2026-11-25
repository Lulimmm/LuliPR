using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

using Ninja.PR.Action;
using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Action.Gcd;

internal sealed class NinjaRaitonResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        var delayBurst = NinjaBurstDelayPolicy.Enabled;
        if (!delayBurst && !PromeSettings.Instance.GetQt(NinjaQts.Raiton)) return Blocked("雷遁开关关闭");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不开始普通雷遁");
        if (NinjaSequencePolicy.IsNormalMudraActive) return Blocked("已有普通结印序列，等待当前忍术结束");
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1) return Blocked("结印可用次数不足 1");
        if (NinjaCombatState.Recently(NinjaActions.Ten, 2120)) return Blocked("最近 2.12 秒已输入天印，避免重复开始");
        if (me.HasStatus(NinjaActions.KassatsuStatus) || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return Blocked("生杀状态存在或最近 2.12 秒使用生杀，保留给冰晶");

        if (delayBurst)
            return NinjaBurstDelayPolicy.ShouldSpendMudra(ActionHelper.GetActionCharges(NinjaActions.Ten))
                ? new CheckResult(true, "延后爆发期间结印接近溢出，使用雷遁消耗一次结印")
                : Blocked($"延后爆发期间保留结印，达到 {NinjaBurstDelayPolicy.MudraOverflowThreshold:F1} 层才使用雷遁");

        if (NinjaSettings.Instance.ForceRaiton || PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        // Hold Raiton until the next
        // small-trick window is available, including while Shadow Walker is
        // still active. Suiton and Raiton are separate jutsu decisions.
        // This hold applies while Shadow Walker is absent. During the
        // Shadow Walker window Raiton is still allowed to satisfy the later
        // execution conditions (for example a forced or ranged cast).
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 19.5f
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)) return Blocked("小背刺将在 19.5 秒内转好且缺少水遁状态，保留结印给水遁");
        if (PromeSettings.Instance.GetQt(NinjaQts.RaitonWhenOutOfRange)
            && NinjaTarget.Distance > NinjaTarget.AttackRange) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (NinjaSettings.Instance.RaitonBackstab
            && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && target.HasStatus(NinjaActions.MugDebuff)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) > 1f)
        {
            if (JobGaugeHelper.NIN.Ninki == 100 && me.HasStatus(NinjaActions.PhantomReady)) return Ok;
            if (me.HasStatus(NinjaActions.PhantomReady)
                || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.5f
                || ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 2.5f) return Ok;
        }
        return Blocked("未命中强制雷遁、倾斜资源、远程雷遁、斩杀或爆发资源窗口；远离时还需开启远程雷遁");
    }

    public override PAction GetAction()
    {
        NinjaSettings.Instance.ForceRaiton = false;
        NinjaSequencePolicy.Begin(NinjaActions.Raiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Raiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
