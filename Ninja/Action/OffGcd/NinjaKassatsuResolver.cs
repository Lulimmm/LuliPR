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

internal sealed class NinjaKassatsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (NinjaBurstDelayPolicy.Enabled) return Blocked(NinjaBurstDelayPolicy.BlockReason);
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return Blocked("自身或可攻击目标不可用");
        if (!PromeSettings.Instance.GetQt("生杀予夺")) return Blocked("生杀予夺开关关闭");
        if (ActionHelper.GetGcdRemain() < 0.6f) return Blocked("GCD 剩余不足 0.60 秒，等待下一个插入窗口");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用生杀");
        if (!Ready(NinjaActions.Kassatsu)) return Blocked("生杀尚未就绪或未解锁");

        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return Blocked("小背刺开启，但自身没有水遁状态、目标也没有爆发状态，暂时保留生杀");
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && PromeSettings.Instance.GetQt(NinjaQts.Raiton)
            && NinjaSettings.Instance.RaitonBackstab
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return Blocked("背刺雷遁设置开启，等待目标爆发状态（3906）再使用生杀");
        return Ok;
    }

    public override PAction GetAction() =>
        BuildNormal(ActionHelper.GetAdjustedActionId(NinjaActions.Kassatsu), ActionType.OffGcd, ActionTargetType.Self);
}
