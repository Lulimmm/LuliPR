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

internal sealed class NinjaRaijuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        var delayBurst = NinjaBurstDelayPolicy.Enabled;
        if (!delayBurst && !PromeSettings.Instance.GetQt(NinjaQts.Raiju)) return Blocked("月影雷兽牙开关关闭");
        if (me == null) return Blocked("自身角色不可用");
        if (target?.CanUseAttackActionOn() != true) return Blocked("没有可攻击目标");
        if (NinjaTarget.Distance > NinjaTarget.AttackRange) return Blocked("目标超过近战范围，等待靠近");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用雷兽");
        if (!delayBurst && target.HasStatus(NinjaActions.KunaisBaneStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.Ten) < 2.5f) return Blocked("爆发状态内结印冷却不足 2.5 秒，保留雷兽让位于忍术");
        return me.HasStatus(NinjaActions.RaijuReady) ? Ok : Blocked("缺少雷兽可用状态（2690）");
    }

    // Raiju is a normal GCD action; only mudra/jutsu sequences need the
    // strict verification path used by Build().
    public override PAction GetAction() => BuildNormal(NinjaActions.FleetingRaiju, ActionType.Gcd, ActionTargetType.Target);
}
