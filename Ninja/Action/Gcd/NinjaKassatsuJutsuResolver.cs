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

internal sealed class NinjaKassatsuJutsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.KassatsuStatus) != true) return Blocked("缺少生杀状态（497）");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不开始冰晶");
        if (NinjaSequencePolicy.IsNormalMudraActive) return Blocked("已有普通结印序列，等待当前忍术结束");
        if (target?.CanUseAttackActionOn() != true) return Blocked("没有可攻击目标，暂不开始冰晶");
        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        var trickWindowEnabled = PromeSettings.Instance.GetQt(NinjaQts.SmallTrick);
        var mugWindowEnabled = PromeSettings.Instance.GetQt(NinjaQts.BigTrick);
        if ((!trickWindowEnabled && !mugWindowEnabled)
            || (mugWindowEnabled && target.HasStatus(NinjaActions.KunaisBaneStatus))
            || (trickWindowEnabled && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 2120))
            || me.GetStatusLeftTime(NinjaActions.KassatsuStatus) < 3f) return Ok;
        return Blocked("等待夺取/小背刺爆发窗口，或生杀剩余不足 3 秒时提前使用");
    }

    public override PAction GetAction()
    {
        NinjaSequencePolicy.Begin(NinjaActions.HyoshoRanryu);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.TenCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.HyoshoRanryu, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}
