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

internal sealed class NinjaOgcdResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;
    private readonly Func<bool> raijuIsFirst;

    public NinjaOgcdResolver(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        this.comboIsFirst = comboIsFirst;
        this.raijuIsFirst = raijuIsFirst;
    }

    private CheckResult CheckBhavacakra()
    {
        var reason = NinjaBhavacakraPolicy.GetBlockReason();
        if (reason != null) return Blocked(reason);
        if (ActionHelper.GetGcdRemain() < .6f) return Blocked("GCD 剩余不足 0.60 秒，等待下一个插入窗口");

        var me = Core.Me!;
        var target = Core.Target!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        var lastCombo = ActionHelper.GetLastComboID();
        var comboFinisher = lastCombo == NinjaActions.GustSlash;
        var bunshinCooldown = ActionHelper.GetActionCooldown(NinjaActions.Bunshin);
        var bunshinActive = me.HasStatus(NinjaActions.BunshinStatus);
        if (bunshinCooldown < 20f && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (!bunshinActive)
                return (comboIsFirst() && comboFinisher && ninki > 85)
                    || (comboIsFirst() && ninki > 95)
                    || (raijuIsFirst() && ninki > 95) ? Ok : Blocked("分身将在 20 秒内就绪，保留忍气；仅连击/雷兽优先且即将溢出时消耗");
            return Blocked("分身冷却不足 20 秒且分身状态存在，暂时保留忍气");
        }
        if (bunshinActive && ((comboFinisher && ninki > 80) || (comboIsFirst() && ninki > 90))) return Ok;
        if ((comboFinisher && ninki > 85) || (comboIsFirst() && ninki > 95)) return Ok;
        if ((PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f)
            || (PromeSettings.Instance.GetQt("鐢熸潃浜堝ず")
                && ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f)) return Blocked("小背刺或生杀将在 2.12 秒内就绪，六道轮回暂时让位");
        return target.HasStatus(NinjaActions.KunaisBaneStatus) ? Ok : Blocked("忍气尚未触发溢出消耗，目标也没有爆发状态（3906），暂时保留资源");
    }

    public override CheckResult Check()
    {
        var me = Core.Me;
        if (me == null || Core.Target == null || ActionHelper.GetGcdRemain() < .6f
            || NinjaCombatState.IsTcjSequenceActive) return Blocked("常规六道轮回需要自身与目标可用、GCD 剩余至少 0.60 秒，且非天地人独占");
        return CheckBhavacakra();
    }

    public override PAction GetAction()
    {
        if (CheckBhavacakra().Success)
        {
            var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra);
            return BuildNormal(adjusted, ActionType.OffGcd, ActionTargetType.Target);
        }
        return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
    }
}
