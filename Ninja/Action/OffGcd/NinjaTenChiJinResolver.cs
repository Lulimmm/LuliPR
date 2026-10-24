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

namespace Ninja.PR.Action.OffGcd;

internal sealed class NinjaTenChiJinResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.TenChiJin)
            || !Ready(NinjaActions.TenChiJin)
            || NinjaCombatState.IsTcjSequenceActive
            // 3851 means the previous TCJ sequence has granted 36961. Do
            // not enqueue another 7403 while Tenri Jindo is waiting.
            || me.HasStatus(NinjaActions.TenriJindoReady)
            || me.HasStatus(NinjaActions.KassatsuStatus)
            || me.HasStatus(NinjaActions.ShadowWalkerStatus)
            || ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 2.12f
            || ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f
            || !target.HasStatus(3849u)
            || ActionHelper.GetActionCooldown(NinjaActions.Ten) < 4f) return No;
        return Ok;
    }

    public override PAction GetAction() =>
        BuildNormal(NinjaActions.TenChiJin, ActionType.OffGcd, ActionTargetType.Self);
}
