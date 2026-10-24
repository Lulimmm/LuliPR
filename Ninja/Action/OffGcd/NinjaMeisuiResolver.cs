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
        if (me == null) return Blocked("Meisui: player unavailable");

        // Keep the success path in the same order as the action requirements.
        if (ActionHelper.GetGcdRemain() < .6f)
            return Blocked("Meisui: GCD < 600ms");
        if (JobGaugeHelper.NIN.Ninki >= 50)
            return Blocked($"Meisui: Ninki {JobGaugeHelper.NIN.Ninki} >= 50");

        var cooldown = ActionHelper.GetActionCooldown(NinjaActions.Meisui);
        if (cooldown > 0f)
            return Blocked($"Meisui: cooldown {cooldown:F2}s");
        if (!ActionHelper.IsUnlocked(NinjaActions.Meisui))
            return Blocked("Meisui: locked");
        if (!me.HasStatus(NinjaActions.ShadowWalkerStatus))
            return Blocked("Meisui: missing 3848");
        if (me.HasStatus(NinjaActions.TcjStatus))
            return Blocked("Meisui: TCJ status active");
        if (!NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000))
            return Blocked("Meisui: no TCJ Suiton in 15s");

        var trickCooldown = ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent);
        return trickCooldown >= 20f
            ? Ok
            : Blocked($"Meisui: Trick Attack cooldown {trickCooldown:F2}s < 20s");
    }

    // Meisui is a normal ability slot; verification is reserved for
    // fixed opener actions in PR and must not stall the regular oGCD queue.
    public override PAction GetAction() => BuildNormal(NinjaActions.Meisui, ActionType.OffGcd, ActionTargetType.Self);
}
