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

internal sealed class NinjaBunshinResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bunshin)) return Blocked("分身开关关闭");
        if (ActionHelper.GetGcdRemain() < .3f) return Blocked("GCD 剩余不足 0.30 秒，等待下一个插入窗口");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用分身");
        if (!Ready(NinjaActions.Bunshin)) return Blocked("分身尚未就绪或未解锁");
        if (JobGaugeHelper.NIN.Ninki < 50) return Blocked("忍气不足 50");
        return !PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
            || ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) >= 1.06f ? Ok : Blocked("夺取将在 1.06 秒内就绪，分身暂时让位");
    }

    public override PAction GetAction() => BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self);
}
