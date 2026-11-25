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

internal sealed class NinjaKBaseResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Raiton)
            || !PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            || !NinjaSettings.Instance.RaitonBackstab) return Blocked("该连击优先分支需要雷遁、小背刺开关及背刺雷遁设置开启");
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        if (NinjaTarget.Distance > NinjaTarget.AttackRange) return Blocked("目标超出近战范围");
        if (NinjaCombatState.HasRaijuReadyForRotation) return Blocked("仍需保留雷兽状态，暂不优先基础连击");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用基础连击");
        if (!target.HasStatus(NinjaActions.KunaisBaneStatus)
            && !NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 1060)) return Blocked("目标不在爆发状态内，且最近 1.06 秒未使用背刺");
        if (ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) >= 2.12f) return Blocked("生杀冷却仍有至少 2.12 秒，不需要此连击优先分支");
        return !target.HasStatus(NinjaActions.MugDebuff) ? Ok : Blocked("目标已有夺取状态，不需要此连击优先分支");
    }

    public override PAction GetAction()
    {
        var last = ActionHelper.GetLastComboID();
        var me = Core.Me!;
        var target = Core.Target!;
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        var positional = TargetHelper.GetTargetPositional();
        uint action;

        if (last == NinjaActions.GustSlash && !TargetHelper.HasPositionalRequirement(target))
            action = kazematoi == 0 ? NinjaActions.ArmorCrush : NinjaActions.AeolianEdge;
        else if (last == NinjaActions.GustSlash)
        {
            if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }
}
