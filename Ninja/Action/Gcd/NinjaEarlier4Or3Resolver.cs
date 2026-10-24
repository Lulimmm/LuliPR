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
        return NinjaSettings.Instance.RaitonBackstab
            && target?.CanUseAttackActionOn() == true
            && NinjaTarget.Distance <= NinjaTarget.AttackRange
            && !NinjaCombatState.IsTcjSequenceActive
            && target.HasStatus(NinjaActions.KunaisBaneStatus)
            && ActionHelper.GetLastComboID() == NinjaActions.GustSlash
            && NinjaCombatState.Recently(NinjaActions.TenChiJin, 8000)
            && NinjaCombatState.Recently(NinjaActions.FleetingRaiju, 2120) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var action = JobGaugeHelper.NIN.Kazematoi == 0
            ? NinjaActions.ArmorCrush
            : NinjaActions.AeolianEdge;
        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }
}
