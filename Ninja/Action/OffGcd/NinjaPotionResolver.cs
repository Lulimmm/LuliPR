using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Action.OffGcd;

internal sealed class NinjaPotionResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(NinjaQts.Potion)) return Blocked("爆发药 QT 未开启");
        if (NinjaBurstDelayPolicy.Enabled) return Blocked("延后爆发开启，暂不使用爆发药");
        if (Core.Me == null || Core.Target?.CanUseAttackActionOn() != true)
            return Blocked("自身或可攻击目标不可用");
        if (NinjaCombatState.IsTcjSequenceActive) return Blocked("天地人独占期间不使用爆发药");
        if (NinjaSequencePolicy.IsActive || Core.Me.HasStatus(NinjaActions.MudraStatus))
            return Blocked("结印序列进行中，暂不使用爆发药");
        if (ActionHelper.GetGcdRemain() < .6f)
            return Blocked("GCD 剩余不足 0.60 秒，等待下一个插入窗口");

        var potion = GameData.GetBestPotionId();
        if (potion == 0) return Blocked("背包没有可用爆发药");
        if (ActionHelper.IsItemOnCooldown(potion)) return Blocked("爆发药仍在冷却");

        // Reaper's ordinary 0612 window, keyed to Ninja's Kunai's Bane.
        // Ninja has no triple-Enshroud branch or alternate potion schedule.
        var cooldown = ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent);
        return cooldown is > 2.5f and < 5f
            ? Ok
            : Blocked($"介毒之术冷却 {cooldown:F2} 秒，等待 2.5～5 秒的爆发药窗口（不含端点）");
    }

    public override PAction GetAction() =>
        Build(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self);
}
