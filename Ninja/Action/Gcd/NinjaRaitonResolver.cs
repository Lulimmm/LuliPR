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

internal sealed class NinjaRaitonResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.Raiton)
            || NinjaCombatState.IsTcjSequenceActive
            || ActionHelper.GetActionCharges(NinjaActions.Ten) < 1
            || NinjaCombatState.Recently(NinjaActions.Ten, 2120)
            || me.HasStatus(NinjaActions.KassatsuStatus)
            || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return No;

        if (NinjaSettings.Instance.ForceRaiton || PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        // Hold Raiton until the next
        // small-trick window is available, including while Shadow Walker is
        // still active. Suiton and Raiton are separate jutsu decisions.
        // This hold applies while Shadow Walker is absent. During the
        // Shadow Walker window Raiton is still allowed to satisfy the later
        // execution conditions (for example a forced or ranged cast).
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 19.5f
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)) return No;
        if (PromeSettings.Instance.GetQt(NinjaQts.RaitonWhenOutOfRange)
            && NinjaTarget.Distance > NinjaTarget.AttackRange) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (NinjaSettings.Instance.RaitonBackstab && target.HasStatus(NinjaActions.MugDebuff)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) > 1f)
        {
            if (JobGaugeHelper.NIN.Ninki == 100 && me.HasStatus(NinjaActions.PhantomReady)) return Ok;
            if (me.HasStatus(NinjaActions.PhantomReady)
                || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.5f
                || ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 2.5f) return Ok;
        }
        return No;
    }

    public override PAction GetAction()
    {
        NinjaSettings.Instance.ForceRaiton = false;
        NinjaSequencePolicy.Begin(NinjaActions.Raiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Raiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
