using System;
using System.Collections.Generic;
using PromeRotation.Extensions;

using Ninja.PR.Data;

namespace Ninja.PR.Helper;

internal static class NinjaMudraDebug
{
    private const int HistoryLimit = 8;
    private static readonly List<uint> recentInputs = new();
    private static long lastRabbitAt;
    private static long lastInputAt;
    private static uint lastInputId;
    private static int rabbitCount;
    private static string lastRabbitReason = "None";

    public static int RabbitCount => rabbitCount;
    public static long LastRabbitAgeMs => lastRabbitAt == 0
        ? -1
        : Math.Max(0, Environment.TickCount64 - lastRabbitAt);
    public static string LastRabbitReason => lastRabbitReason;
    public static string RecentInputs => recentInputs.Count == 0
        ? "None"
        : string.Join(" -> ", recentInputs.ConvertAll(FormatAction));

    public static void Record(uint actionId)
    {
        if (actionId == NinjaActions.Rabbit)
        {
            var now = Environment.TickCount64;
            if (lastRabbitAt != 0 && now - lastRabbitAt <= 250) return;
            rabbitCount++;
            lastRabbitAt = now;
            lastRabbitReason = BuildRabbitReason();
            return;
        }

        if (!IsMudraInput(actionId)) return;
        var inputAt = Environment.TickCount64;
        if (lastInputId == actionId && inputAt - lastInputAt <= 250)
            return;

        recentInputs.Add(actionId);
        lastInputId = actionId;
        lastInputAt = inputAt;
        if (recentInputs.Count > HistoryLimit)
            recentInputs.RemoveAt(0);
    }

    public static void Reset()
    {
        recentInputs.Clear();
        lastRabbitAt = 0;
        lastInputAt = 0;
        lastInputId = 0;
        rabbitCount = 0;
        lastRabbitReason = "None";
    }

    private static bool IsMudraInput(uint actionId) => actionId is
        NinjaActions.Ten or NinjaActions.Chi or NinjaActions.Jin
        or NinjaActions.TenCombo or NinjaActions.ChiCombo or NinjaActions.JinCombo;

    private static string BuildRabbitReason()
    {
        var policy = NinjaSequencePolicy.IsActive
            ? $"terminal={FormatAction(NinjaSequencePolicy.TerminalAction)}"
            : "no active PR mudra sequence";
        var mudraStatus = PromeRotation.Core.Core.Me?.HasStatus(NinjaActions.MudraStatus) == true
            ? "496 active"
            : "496 missing";
        var expected = ExpectedRoute(NinjaSequencePolicy.TerminalAction);
        var duplicate = recentInputs.Count >= 2
            && recentInputs[^1] == recentInputs[^2];
        var cause = !NinjaSequencePolicy.IsActive
            ? "印输入发生在 PR 忍术序列之外，可能是手动输入或残留输入"
            : mudraStatus == "496 missing"
                ? "上一个印未成功或印状态已消失，后续输入被游戏判定为搓兔子"
                : duplicate
                    ? $"连续重复输入 {FormatAction(recentInputs[^1])}"
                    : $"最近印序列与当前目标 {expected} 不一致，可能存在调度器重复提交或手动插入";
        return $"{cause}; {policy}; {mudraStatus}; inputs={RecentInputs}";
    }

    private static string ExpectedRoute(uint terminalAction) => terminalAction switch
    {
        NinjaActions.Suiton => "Ten-Chi-Jin-Suiton",
        NinjaActions.Raiton => "Ten-Chi-Raiton",
        NinjaActions.HyoshoRanryu => "Ten-Jin-Hyosho",
        NinjaActions.TcjSuiton => "TCJ-Fuma-Raiton-Suiton",
        _ => "unknown"
    };

    private static string FormatAction(uint actionId) => actionId switch
    {
        NinjaActions.Ten or NinjaActions.TenCombo => "Ten",
        NinjaActions.Chi or NinjaActions.ChiCombo => "Chi",
        NinjaActions.Jin or NinjaActions.JinCombo => "Jin",
        NinjaActions.Suiton => "Suiton",
        NinjaActions.Raiton => "Raiton",
        NinjaActions.HyoshoRanryu => "Hyosho",
        NinjaActions.TcjSuiton => "TCJ Suiton",
        NinjaActions.Rabbit => "Rabbit(2272)",
        0 => "None",
        _ => actionId.ToString()
    };
}
