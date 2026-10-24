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
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt("生杀予夺")
            || ActionHelper.GetGcdRemain() < 0.6f
            || me.HasStatus(NinjaActions.TcjStatus)
            || !Ready(NinjaActions.Kassatsu)) return No;

        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        if (NinjaSettings.Instance.RaitonBackstab
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        return Ok;
    }

    public override PAction GetAction() =>
        Build(ActionHelper.GetAdjustedActionId(NinjaActions.Kassatsu), ActionType.OffGcd, ActionTargetType.Self);
}
