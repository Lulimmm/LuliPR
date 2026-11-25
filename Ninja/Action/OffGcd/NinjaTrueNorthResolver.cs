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

internal sealed class NinjaTrueNorthResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;

    public NinjaTrueNorthResolver(Func<bool> comboIsFirst) => this.comboIsFirst = comboIsFirst;

    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.AutoTrueNorth) || me == null) return Blocked("自动真北开关关闭或自身不可用");
        if (target?.CanUseAttackActionOn() != true) return Blocked("没有可攻击目标");
        if (NinjaCombatState.Recently(NinjaActions.TrueNorth, 2120)) return Blocked("最近 2.12 秒已使用真北");
        if (me.HasStatus(1250u)) return Blocked("真北状态已经存在，无需重复");
        if (ActionHelper.GetActionCharges(NinjaActions.TrueNorth) < 1) return Blocked("真北可用次数不足 1");
        if (ActionHelper.GetGcdRemain() < .6f) return Blocked("GCD 剩余不足 0.60 秒，等待下一个插入窗口");
        if (ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain() <= ActionHelper.GetGcdTotal() * .625f)
            return Blocked("尚未进入本 GCD 已经过 62.5% 的真北插入时机");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用真北");
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f) return Blocked("小背刺将在 2.12 秒内就绪，真北暂时让位");
        if (!TargetHelper.HasPositionalRequirement(target)) return Blocked("目标没有身位要求，无需真北");
        if (!comboIsFirst()) return Blocked("下一优先 GCD 不是基础连击，无需为连击提前真北");

        var last = ActionHelper.GetLastComboID();
        if (last != NinjaActions.GustSlash) return Blocked("上一段不是绝风，尚未到有身位要求的第三段");
        var positional = TargetHelper.GetTargetPositional();
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        if (kazematoi == 0 && positional != Positional.Flank) return Ok;
        if (kazematoi > 3 && positional != Positional.Rear) return Ok;
        if (positional != Positional.Flank && positional != Positional.Rear) return Ok;
        if (((PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
                    && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f)
                || (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                    && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f))
            && kazematoi <= 1
            && positional != Positional.Flank) return Ok;
        return Blocked("当前身位满足连击与风缠策略，不需要真北");
    }

    public override PAction GetAction() => BuildNormal(NinjaActions.TrueNorth, ActionType.OffGcd, ActionTargetType.Self);
}
