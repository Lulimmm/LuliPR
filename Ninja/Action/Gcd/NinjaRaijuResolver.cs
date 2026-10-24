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
        if (!PromeSettings.Instance.GetQt(NinjaQts.Raiju) || me == null
            || target?.CanUseAttackActionOn() != true || NinjaTarget.Distance > NinjaTarget.AttackRange
            || me.HasStatus(NinjaActions.TcjStatus)) return No;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.Ten) < 2.5f) return No;
        return me.HasStatus(NinjaActions.RaijuReady) ? Ok : No;
    }

    // Raiju is a normal GCD action; only mudra/jutsu sequences need the
    // strict verification path used by Build().
    public override PAction GetAction() => BuildNormal(NinjaActions.FleetingRaiju, ActionType.Gcd, ActionTargetType.Target);
}
