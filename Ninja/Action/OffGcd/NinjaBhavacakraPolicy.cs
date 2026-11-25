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

internal static class NinjaBhavacakraPolicy
{
    public static bool IsEligible() => GetBlockReason() == null;

    public static string? GetBlockReason()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra)) return "六道轮回开关关闭";
        if (me == null || target?.CanUseAttackActionOn() != true) return "自身或可攻击目标不可用";
        if (NinjaTarget.Distance > NinjaTarget.AttackRange) return "目标超出近战范围";
        if (NinjaCombatState.IsTcjSequenceActive) return "天地人独占期间不使用六道轮回";
        if (JobGaugeHelper.NIN.Ninki < 50) return "忍气不足 50";
        if (NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960, 1060)) return "最近 1.06 秒已使用六道轮回，避免重复";
        if (NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)) return "刚使用夺取，处于 1.06 秒保护间隔";
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
            && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) && me.HasStatus(NinjaActions.ShadowWalkerStatus))
            return "小背刺将在 1.26 秒内就绪且有水遁状态，六道轮回暂时让位";
        return null;
    }

    public static bool ShouldForceSpend(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        if (!IsEligible()) return false;
        var me = Core.Me!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return true;

        if (ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 20f
            && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (me.HasStatus(NinjaActions.BunshinStatus)) return false;
            var comboFinisher = ActionHelper.GetLastComboID() == NinjaActions.GustSlash;
            return (comboIsFirst() && ((comboFinisher && ninki > 85) || ninki > 95))
                || (raijuIsFirst() && ninki > 95);
        }

        if (ninki == 100) return true;
        return PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
            && !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && ninki > 60;
    }
}
