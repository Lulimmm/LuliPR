using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;

using Ninja.PR.Data;

namespace Ninja.PR.Helper;

internal static class NinjaAttemptDebug
{
    private const int HistoryLimit = 8;
    private static readonly List<string> history = new();
    private static uint currentId;
    private static ActionType currentType;
    private static long currentSince;
    private static int consecutiveObservations;
    private static int lastRetryCount;

    public static uint CurrentId => currentId;
    public static ActionType CurrentType => currentType;
    public static long CurrentAgeMs => currentSince == 0
        ? -1
        : Math.Max(0, Environment.TickCount64 - currentSince);
    public static int ConsecutiveObservations => consecutiveObservations;
    public static int RetryCount => lastRetryCount;
    public static string History => history.Count == 0 ? "无" : string.Join(" -> ", history);

    public static void Update()
    {
        var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
        if (attempted == null)
        {
            currentId = 0;
            currentSince = 0;
            consecutiveObservations = 0;
            lastRetryCount = 0;
            return;
        }

        var retryCount = ReadRetryCount();
        if (attempted.ActionId != currentId || attempted.Type != currentType)
        {
            currentId = attempted.ActionId;
            currentType = attempted.Type;
            currentSince = Environment.TickCount64;
            consecutiveObservations = 1;
            lastRetryCount = retryCount;
            AddHistory($"{FormatAction(currentId)} [{FormatType()}]");
            return;
        }

        consecutiveObservations++;
        lastRetryCount = retryCount;
    }

    public static void Reset()
    {
        history.Clear();
        currentId = 0;
        currentType = default;
        currentSince = 0;
        consecutiveObservations = 0;
        lastRetryCount = 0;
    }

    public static string FormatCurrent() => currentId == 0
        ? "无"
        : $"{FormatAction(currentId)} ({currentId})";

    public static string FormatType() => currentId == 0 ? "无" : currentType switch
    {
        ActionType.Gcd => "GCD",
        ActionType.OffGcd => "能力技",
        ActionType.Item => "爆发药",
        _ => currentType.ToString()
    };

    public static string FormatAction(uint id) => id switch
    {
        NinjaActions.SpinningEdge => "旋风刃",
        NinjaActions.GustSlash => "绝风",
        NinjaActions.AeolianEdge => "风蚀箭",
        NinjaActions.ArmorCrush => "攻其不备",
        NinjaActions.FumaShuriken => "风魔手里剑",
        NinjaActions.Raiton => "雷遁",
        NinjaActions.Suiton => "水遁",
        NinjaActions.Rabbit => "搓兔子",
        NinjaActions.Bunshin => "分身之术",
        NinjaActions.MugCurrent or NinjaActions.Mug => "夺取",
        NinjaActions.TrickAttackCurrent or NinjaActions.TrickAttack => "背刺",
        NinjaActions.TenChiJin => "天地人",
        NinjaActions.Ten => "天印",
        NinjaActions.Chi => "地印",
        NinjaActions.Jin => "人印",
        NinjaActions.Kassatsu => "生杀夺予",
        NinjaActions.Shukuchi => "缩地",
        NinjaActions.TenriJindo => "天理人道",
        NinjaActions.PhantomKamaitachi => "镰鼬",
        NinjaActions.FleetingRaiju => "月影雷兽牙",
        NinjaActions.Meisui => "命水",
        NinjaActions.Assassinate or NinjaActions.AssassinateCooldown => "梦幻三段",
        NinjaActions.HyoshoRanryu => "冰晶乱流之术",
        NinjaActions.TenCombo => "天印（连段）",
        NinjaActions.ChiCombo => "地印（连段）",
        NinjaActions.JinCombo => "人印（连段）",
        NinjaActions.TcjFumaShuriken => "天地人风魔手里剑",
        NinjaActions.TcjRaiton => "天地人雷遁",
        NinjaActions.TcjSuiton => "天地人水遁",
        36960u => "六道轮回（强化）",
        0 => "无",
        _ => $"技能 {id}"
    };

    private static int ReadRetryCount()
    {
        try
        {
            return NinjaRotation.RetryCountField?.GetValue(null) is int count ? count : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static void AddHistory(string value)
    {
        history.Add(value);
        if (history.Count > HistoryLimit)
            history.RemoveAt(0);
    }
}
