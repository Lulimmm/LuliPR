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

internal sealed class NinjaTenriJindoResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.TenriJindo)) return Blocked("Tenri Jindo: QT disabled");
        if (me == null || target?.CanUseAttackActionOn() != true)
            return Blocked("Tenri Jindo: no valid target");
        if (ActionHelper.GetGcdRemain() < 0.6f)
            return Blocked("Tenri Jindo: GCD < 600ms");
        if (me.HasStatus(NinjaActions.TcjStatus)
            || NinjaCombatState.IsTcjSequenceActive)
            return Blocked("Tenri Jindo: TCJ sequence active");
        if (!me.HasStatus(NinjaActions.TenriJindoReady))
            return Blocked("Tenri Jindo: missing 3851");
        if (!ActionHelper.IsUnlocked(NinjaActions.TenriJindo))
            return Blocked("Tenri Jindo: action locked");
        if (!Ready(NinjaActions.TenriJindo))
            return Blocked("Tenri Jindo: action not ready");
        return Ok;
    }

    public override PAction GetAction()
    {
        // Submit the currently adjusted action so the proc is accepted by
        // behavior here so the ready 36961 action is accepted by the client.
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.TenriJindo);
        return BuildNormal(adjusted == 0 ? NinjaActions.TenriJindo : adjusted,
            ActionType.OffGcd, ActionTargetType.Target);
    }
}
