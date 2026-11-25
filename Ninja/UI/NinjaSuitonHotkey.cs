using System;
using System.Collections.Generic;
using System.Reflection;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;
using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.UI;

// One click queues one verified sequence. No QT or normal resolver reissues it.
internal sealed class NinjaSuitonHotkey : IHotkeyLogic
{
    internal const string Name = "预备水遁";
    private const long RequestCooldownMs = 5000;
    private static readonly FieldInfo? HostOpenerField = typeof(RotationManager)
        .GetField("_isOpenerInProgress", BindingFlags.Static | BindingFlags.NonPublic);
    private static long lastQueuedAt = long.MinValue;

    public bool IsActive() => GetBlockReason() == null;

    internal static string? GetBlockReason()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || me.IsDead) return "自身角色不可用或已死亡";
        if (GameData.GetCountdown() > 0) return "战斗倒数进行中，水遁交给 Opener 的倒数队列";
        try
        {
            if (HostOpenerField?.GetValue(null) is not bool openerActive)
                return "无法读取宿主起手状态，暂不插入手动水遁";
            if (openerActive || NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions)
                return "固定起手或无损起手交接中，暂不插入手动水遁";
            if (NinjaRotation.ActiveCommandField == null)
                return "无法读取当前命令，暂不插入手动水遁";
            if (NinjaRotation.ActiveCommandField.GetValue(null)?.GetType().Name == "ActionGroupCommand")
                return "已有连续技能组正在执行，等待该组结束";
        }
        catch
        {
            return "宿主队列状态读取失败，暂不插入手动水遁";
        }
        if (target?.CanUseAttackActionOn() != true) return "没有可攻击目标";
        if (NinjaTarget.Distance > NinjaTarget.AttackRange + 17f) return "目标超出水遁允许距离";
        if (NinjaCombatState.IsTcjSequenceActive) return "天地人进行中";
        if (NinjaSequencePolicy.IsActive || me.HasStatus(NinjaActions.MudraStatus)) return "已有结印或忍术序列，等待完成";
        // As with manual Raiton, an ordinary selected/queued GCD need not be
        // ready first. Do not stack behind another manually prioritized group.
        if (ActionQueueManager.HasHighPriorityAction()) return "已有优先技能，等待队列处理";
        var now = Environment.TickCount64;
        if (lastQueuedAt != long.MinValue && now - lastQueuedAt < RequestCooldownMs) return "距离上次手动水遁请求不足 5 秒，避免重复入队";
        if (!ActionHelper.IsUnlocked(NinjaActions.Ten) || !ActionHelper.IsUnlocked(NinjaActions.Suiton)) return "天印或水遁尚未解锁";
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1) return "结印可用次数不足 1";
        if (me.HasStatus(NinjaActions.KassatsuStatus) || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return "生杀状态存在或刚使用生杀，保留给冰晶";
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)) return "已有水遁赋予的状态（3848），无需重复水遁";
        if (NinjaCombatState.Recently(NinjaActions.Suiton, 2120)) return "刚使用水遁，避免重复入队";
        return null;
    }

    internal static void Reset() => lastQueuedAt = long.MinValue;

    public void OnClick()
    {
        // Recheck at the click boundary; panel availability may be a frame old.
        if (!IsActive()) return;
        // Match Raiton: the host waits for the next legal GCD boundary.
        // Do not add a GCD-remain gate or a persistent preparation flag.
        NinjaSequencePolicy.Begin(NinjaActions.Suiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            new(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true },
            new(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true },
            new(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true },
            new(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true }
        }, isHighPriority: true);
        lastQueuedAt = Environment.TickCount64;
        NinjaBattleDebug.RecordManualSuitonRequest();
    }
}
