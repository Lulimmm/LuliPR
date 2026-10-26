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

internal sealed class NinjaMeisuiResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        // The direct Meisui resolver does not gate on the Meisui QT. The QT
        // is only part of the _100B overflow-priority branch above, so keep
        // that distinction when migrating the 16489 resolver.
        if (me == null) return Blocked("命水：自身不可用");

        // Keep the success path in the same order as the action requirements.
        if (ActionHelper.GetGcdRemain() < .6f)
            return Blocked("命水：GCD 剩余不足 600 毫秒");
        if (JobGaugeHelper.NIN.Ninki >= 50)
            return Blocked($"命水：忍气 {JobGaugeHelper.NIN.Ninki} >= 50");

        var cooldown = ActionHelper.GetActionCooldown(NinjaActions.Meisui);
        if (cooldown > 0f)
            return Blocked($"命水：冷却 {cooldown:F2} 秒");
        if (!ActionHelper.IsUnlocked(NinjaActions.Meisui))
            return Blocked("命水：技能未解锁");
        if (!me.HasStatus(NinjaActions.ShadowWalkerStatus))
            return Blocked("命水：缺少 3848 状态");
        if (NinjaCombatState.IsTcjSequenceActive)
            return Blocked("命水：天地人状态中");
        if (!NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000))
            return Blocked("命水：最近 15 秒没有天地人水遁");

        var trickCooldown = ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent);
        return trickCooldown >= 20f
            ? Ok
            : Blocked($"命水：背刺冷却 {trickCooldown:F2} 秒 < 20 秒");
    }

    // Meisui is a normal ability slot; verification is reserved for
    // fixed opener actions in PR and must not stall the regular oGCD queue.
    public override PAction GetAction() => BuildNormal(NinjaActions.Meisui, ActionType.OffGcd, ActionTargetType.Self);
}
