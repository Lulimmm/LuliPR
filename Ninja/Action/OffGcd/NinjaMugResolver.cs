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

internal sealed class NinjaMugResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        if (!PromeSettings.Instance.GetQt(NinjaQts.BigTrick)) return Blocked("夺取开关关闭");
        if (Core.Target?.CanUseAttackActionOn() != true) return Blocked("没有可攻击目标");
        if (!InMeleeRange) return Blocked("夺取需要近战目标，当前距离超出范围");
        // Dokumori is safe to weave with 400 ms left in the GCD. The host
        // scheduler is scoped by NinjaWeavePolicy while this rotation is
        // active so this resolver is not blocked by the default 600 ms gate.
        if (ActionHelper.GetGcdRemain() < .4f) return Blocked("GCD 剩余不足 0.40 秒，等待下一个插入窗口");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用夺取");
        return Ready(NinjaActions.MugCurrent) ? Ok : Blocked("夺取尚未就绪或未解锁");
    }

    public override PAction GetAction()
    {
        if (PromeSettings.Instance.GetQt(NinjaQts.Bunshin)
            && NinjaSettings.Instance.P5PerfectBunshin
            && JobGaugeHelper.NIN.Ninki >= 50)
        {
            // AE's CurrGcdAbilityCount is 1 after a completed ninjutsu. Do
            // not enqueue Bunshin + Mug as a pair in that single weave slot;
            // submit the first action and let the next GCD reopen the queue.
            if (!NinjaCombatState.CanQueueMultipleOffGcd)
                return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin),
                    ActionType.OffGcd, ActionTargetType.Self);
            ActionQueueManager.EnqueueOffGcdList(new List<PAction>
            {
                BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self),
                BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target)
            });
            return null!;
        }
        return BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}
