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
        if (!PromeSettings.Instance.GetQt(NinjaQts.TenriJindo)) return Blocked("天理人道：QT 未开启");
        if (me == null || target?.CanUseAttackActionOn() != true)
            return Blocked("天理人道：没有有效目标");
        if (ActionHelper.GetGcdRemain() < 0.6f)
            return Blocked("天理人道：GCD 剩余不足 600 毫秒");
        if (NinjaCombatState.IsTcjSequenceActive)
            return Blocked("天理人道：天地人进行中");
        if (!me.HasStatus(NinjaActions.TenriJindoReady))
            return Blocked("天理人道：缺少 3851 状态");
        if (!ActionHelper.IsUnlocked(NinjaActions.TenriJindo))
            return Blocked("天理人道：技能未解锁");
        if (!Ready(NinjaActions.TenriJindo))
            return Blocked("天理人道：技能未准备好");
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
