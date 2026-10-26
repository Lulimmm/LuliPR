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

internal sealed class NinjaPhantomResolver : NinjaResolver
{
    public bool IsExpiringSoon()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.PhantomReady) != true
            || NinjaCombatState.IsTcjSequenceActive
            || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.Phantom)) return false;

        var remaining = me.GetStatusLeftTime(NinjaActions.PhantomReady);
        if (remaining <= 0f) return false;

        // Hold 2723 for the Kunai's Bane window when it will survive until
        // Trick Attack is ready. If its remaining time is shorter than that
        // cooldown, consume Kamaitachi before the proc expires.
        var trickCooldown = Math.Max(0f,
            ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent));
        return remaining <= trickCooldown;
    }

    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null) return Blocked("镰鼬：自身不可用");
        if (!me.HasStatus(NinjaActions.PhantomReady)) return Blocked("镰鼬：缺少 2723 状态");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("镰鼬：天地人进行中");
        if (target?.CanUseAttackActionOn() != true) return Blocked("镰鼬：目标不可用");
        if (!PromeSettings.Instance.GetQt(NinjaQts.Phantom)) return Blocked("镰鼬：QT 未开启");
        // The proc is otherwise lost permanently. Consume it before applying
        // normal Ninki and range-only preferences.
        if (IsExpiringSoon()) return Ok;
        if (JobGaugeHelper.NIN.Ninki > 90) return Blocked("镰鼬：忍气超过 90");

        var distance = NinjaTarget.Distance;
        if (distance > NinjaTarget.AttackRange) return Ok;
        if (NinjaSettings.Instance.PhantomOnlyForRange) return Blocked("镰鼬：仅远程使用设置");
        if (me.HasStatus(NinjaActions.BunshinStatus) && target.HasStatus(NinjaActions.KunaisBaneStatus))
            return Blocked("镰鼬：3906 期间保留分身");
        if (PromeSettings.Instance.GetQt("鍊炬枩璧勬簮")) return Ok;

        // A failed backstab check must continue to the 3906 window, matching
        // AE's fall-through behavior.
        if (NinjaSettings.Instance.RaitonBackstab
            && target.HasStatus(NinjaActions.MugDebuff)
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)
            && NinjaCombatState.Recently(NinjaActions.Raiton, 2120)) return Ok;
        if (target.HasStatus(NinjaActions.MugDebuff) && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;

        // Kunai's Bane (3906) is the burst window. Keep Phantom ahead of the
        // normal combo while the proc is ready.
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (NinjaCombatState.Recently(NinjaActions.Bunshin, 20000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 18000)
            && !NinjaCombatState.Recently(NinjaActions.MugCurrent, 20000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1.7f) return Ok;
        return NinjaCombatState.Recently(NinjaActions.Bunshin, 22000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 22000)
            && NinjaCombatState.Recently(NinjaActions.MugCurrent, 24000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1f
            ? Ok
            : Blocked("镰鼬：当前没有优先级窗口");
    }

    public override PAction GetAction() => BuildNormal(NinjaActions.PhantomKamaitachi, ActionType.Gcd, ActionTargetType.Target);
}
