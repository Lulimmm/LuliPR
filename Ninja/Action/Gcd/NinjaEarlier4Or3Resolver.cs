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

internal sealed class NinjaEarlier4Or3Resolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Raiton)
            || !PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            || !PromeSettings.Instance.GetQt(NinjaQts.Raiju)
            || !NinjaSettings.Instance.RaitonBackstab) return Blocked("该提前三段分支需要雷遁、小背刺、雷兽开关及背刺雷遁设置全部开启");
        if (me == null || NinjaCombatState.HasRaijuReadyForRotation) return Blocked("自身不可用或仍需保留雷兽状态");
        if (target?.CanUseAttackActionOn() != true || NinjaTarget.Distance > NinjaTarget.AttackRange) return Blocked("需要可攻击的近战目标");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人仍在进行中");
        if (!target.HasStatus(NinjaActions.KunaisBaneStatus)) return Blocked("目标缺少爆发状态（3906）");
        if (ActionHelper.GetLastComboID() != NinjaActions.GustSlash) return Blocked("上一段不是绝风，尚未到连击第三段");
        if (!NinjaCombatState.Recently(NinjaActions.TenChiJin, 8000)) return Blocked("最近 8 秒未观测到天地人");
        return NinjaCombatState.Recently(NinjaActions.FleetingRaiju, 2120) ? Ok : Blocked("最近 2.12 秒未观测到月影雷兽牙");
    }

    public override PAction GetAction()
    {
        var action = JobGaugeHelper.NIN.Kazematoi == 0
            ? NinjaActions.ArmorCrush
            : NinjaActions.AeolianEdge;
        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }
}
