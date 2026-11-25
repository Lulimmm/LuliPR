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

internal sealed class NinjaTenChiJinResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        if (!PromeSettings.Instance.GetQt(NinjaQts.TenChiJin)) return Blocked("天地人开关关闭");
        if (!Ready(NinjaActions.TenChiJin)) return Blocked("天地人尚未就绪或未解锁");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人已经在进行中，不重复开启");
        // 3851 means the previous TCJ sequence has granted 36961. Do
        // not enqueue another 7403 while Tenri Jindo is waiting.
        if (me.HasStatus(NinjaActions.TenriJindoReady)) return Blocked("已有天理人道可用状态（3851），先处理上一轮天地人的后续技能");
        if (PromeSettings.Instance.GetQt("鐢熸潃浜堝ず")
                && ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f) return Blocked("生杀优先条件启用，且生杀将在 2.12 秒内就绪，天地人暂时让位");
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)) return Blocked("自身仍有水遁赋予的状态（3848），暂不开启天地人");
        if (PromeSettings.Instance.GetQt("鐢熸潃浜堝ず")
                && me.HasStatus(NinjaActions.KassatsuStatus)) return Blocked("生杀优先条件启用，且生杀状态尚未消耗");
        if (PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
                && (ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 2.12f
                    || !target.HasStatus(3849u))) return Blocked("夺取开关开启：夺取将在 2.12 秒内就绪，或目标尚未获得夺取状态（3849）");
        if (ActionHelper.GetActionCooldown(NinjaActions.Ten) < 4f) return Blocked("结印冷却不足 4 秒，天地人暂时让位于普通忍术");
        return Ok;
    }

    public override PAction GetAction() =>
        BuildNormal(NinjaActions.TenChiJin, ActionType.OffGcd, ActionTargetType.Self);
}
