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

internal sealed class NinjaAssassinateResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        if (!PromeSettings.Instance.GetQt("梦幻三段")) return Blocked("梦幻三段开关关闭");
        if (Core.Target?.CanUseAttackActionOn() != true || !InMeleeRange) return Blocked("需要可攻击的近战目标");
        if (ActionHelper.GetGcdRemain() < .6f) return Blocked("GCD 剩余不足 0.60 秒，等待下一个插入窗口");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用梦幻三段");
        if (!ActionHelper.IsUnlocked(NinjaActions.Assassinate)) return Blocked("技能未解锁");
        if (ActionHelper.GetActionCooldown(NinjaActions.AssassinateCooldown) > 0f) return Blocked("梦幻三段仍在冷却");
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f) return Blocked("小背刺将在 2.12 秒内就绪，暂时让位");
        if (PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 2.12f) return Blocked("夺取将在 2.12 秒内就绪，暂时让位");
        return !NinjaCombatState.IsTcjSequenceActive ? Ok : Blocked("天地人独占期间不使用梦幻三段");
    }

    public override PAction GetAction()
    {
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Assassinate);
        return BuildNormal(adjusted == 0 ? NinjaActions.Assassinate : adjusted,
            ActionType.OffGcd, ActionTargetType.Target);
    }
}
