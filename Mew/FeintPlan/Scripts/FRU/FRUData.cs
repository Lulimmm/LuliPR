using System;
using System.Collections.Generic;
using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.FRU;

public static class FRUData
{
    private static readonly Dictionary<string, long> ReadyAt = new(StringComparer.Ordinal);

    public static class 读条
    {
        public const uint BurnishedGlory = 40170u;
        public const uint DiamondDust = 40197u;
        public const uint HallowedRay = 40210u;
        public const uint LightRampant = 40212u;
        public const uint AbsoluteZero = 40224u;
        public const uint UltimateRelativity = 40266u;
        public const uint ShockwavePulsar = 40282u;
        public const uint ShellCrusher = 40286u;
        public const uint CrystallizeTime = 40298u;
        public const uint CrystallizeTimeAlternate = 40240u;
        public const uint DarklitDragonsong = 40301u;
        public const uint DarklitDragonsongAlternate = 40239u;
        public const uint AkhMorn = 40302u;
        public const uint AkhMornAlternate = 40247u;
        public const uint FulgentBlade = 40306u;
        public const uint AkhMornLoop = 40310u;
    }

    public static class 伤害判定
    {
        public const uint Sinblaze = 40156u;
        public const uint Sinsmite = 40142u;
    }

    public static string? DetectPhaseEntry(IMewFeintEvent? context)
    {
        if (context is WeatherChangedCondParams weather)
        {
            return weather.NewWeatherId switch
            {
                35u => "P2",
                105u => "P3",
                108u => "P5",
                _ => null
            };
        }

        if (context is EnemyCastSpellCondParams cast
            && IsSpell(cast.SpellId,
                40298u, 40240u, 40301u, 40239u))
            return "P4";

        return null;
    }

    public static void ResetRuntime() => ReadyAt.Clear();

    public static void MarkFeintQueued(string nodeName)
    {
        var now = Environment.TickCount64;
        if (nodeName == "时间压缩·绝")
            ReadyAt["P3-D1-Second"] = now + 70_000;
        else if (nodeName == "光与暗的龙诗#1")
            ReadyAt["P4-D2-Second"] = now + 100_000;
        else if (nodeName.StartsWith("星灵之剑#", StringComparison.Ordinal))
            ReadyAt["P5-D1-Next"] = now + 90_000;
        else if (nodeName.StartsWith("死亡轮回#", StringComparison.Ordinal))
            ReadyAt["P5-D2-Next"] = now + 90_000;
    }

    public static bool AfterCombatDelay(uint nodeId, int delayMs)
    {
        FeintHelper.SetDelay(nodeId, delayMs);
        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    public static bool AfterCast(uint nodeId, IMewFeintEvent? context, int delayMs, params uint[] spellIds)
    {
        if (context is EnemyCastSpellCondParams cast && IsSpell(cast.SpellId, spellIds))
            FeintHelper.SetDelay(nodeId, delayMs);

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    public static bool AfterDamageEffect(uint nodeId, IMewFeintEvent? context, int delayMs, params uint[] actionIds)
    {
        if (context is ReceviceAbilityEffectCondParams effect && IsSpell(effect.ActionId, actionIds))
            FeintHelper.SetDelay(nodeId, delayMs);

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    public static bool AfterDelayedCast(
        uint nodeId,
        string timerKey,
        IMewFeintEvent? context,
        int delayAfterCastMs,
        params uint[] spellIds)
    {
        if (ReadyAt.TryGetValue(timerKey, out var readyAt)
            && Environment.TickCount64 >= readyAt
            && context is EnemyCastSpellCondParams cast
            && IsSpell(cast.SpellId, spellIds))
            FeintHelper.SetDelay(nodeId, delayAfterCastMs);

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    public static bool AfterP5LoopCast(
        uint nodeId,
        uint serialId,
        string timerKey,
        IMewFeintEvent? context,
        uint spellId)
    {
        var waitElapsed = serialId == 1
            || (ReadyAt.TryGetValue(timerKey, out var readyAt) && Environment.TickCount64 >= readyAt);
        if (waitElapsed && context is EnemyCastSpellCondParams cast && cast.SpellId == spellId)
            FeintHelper.SetDelay(nodeId, 0);

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    private static bool IsSpell(uint spellId, params uint[] spellIds)
    {
        foreach (var id in spellIds)
            if (spellId == id) return true;
        return false;
    }
}
