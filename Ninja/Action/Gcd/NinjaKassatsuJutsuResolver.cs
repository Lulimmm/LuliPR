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

internal sealed class NinjaKassatsuJutsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.KassatsuStatus) != true
            || NinjaCombatState.IsTcjSequenceActive
            || target?.CanUseAttackActionOn() != true) return No;
        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)
            || NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 2120)
            || me.GetStatusLeftTime(NinjaActions.KassatsuStatus) < 3f) return Ok;
        return No;
    }

    public override PAction GetAction()
    {
        NinjaSequencePolicy.Begin(NinjaActions.HyoshoRanryu);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.TenCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.HyoshoRanryu, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
