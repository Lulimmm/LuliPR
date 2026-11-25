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

internal sealed class NinjaComboResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        if (!HasMeleeTarget) return Blocked("基础连击需要可攻击的近战目标；目标状态与距离见上方诊断");
        if (me == null) return Blocked("自身角色不可用");
        if (NinjaCombatState.HasRaijuReadyForRotation) return Blocked("雷兽开关开启且有雷兽状态，暂不使用会消耗该状态的基础连击");
        return !NinjaCombatState.IsTcjSequenceActive ? Ok : Blocked("天地人独占期间不使用基础连击");
    }

    public override PAction GetAction()
    {
        var me = Core.Me!;
        var target = Core.Target!;
        var action = ResolveNextAction(me, target);
        return BuildNormal(action, ActionType.Gcd, ActionTargetType.Target);
    }

    // Keep the positional indicator and the actual basic-combo resolver on
    // exactly the same next-action calculation.
    internal static uint ResolveNextAction(
        Dalamud.Game.ClientState.Objects.Types.IBattleChara me,
        Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var last = ActionHelper.GetLastComboID();
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        var positional = TargetHelper.GetTargetPositional();
        uint action;

        if (last == NinjaActions.GustSlash && !TargetHelper.HasPositionalRequirement(target))
            action = kazematoi == 0 ? NinjaActions.ArmorCrush : NinjaActions.AeolianEdge;
        else if (last == NinjaActions.GustSlash && me.Level >= 26)
        {
            if (me.Level < 54) action = NinjaActions.AeolianEdge;
            else if (((PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
                        && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f)
                    || (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                        && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f))
                && kazematoi <= 1) action = NinjaActions.ArmorCrush;
            else if (target.HasStatus(NinjaActions.KunaisBaneStatus) && kazematoi != 0) action = NinjaActions.AeolianEdge;
            else if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge && me.Level >= 4 ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return action;
    }
}
