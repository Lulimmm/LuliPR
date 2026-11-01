using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

using Ninja.PR.Action.OffGcd;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

internal static class NinjaTrickAttackDebug
{
    private const int HistoryLimit = 12;
    private static readonly List<string> history = new();
    private static string current = "\u65E0";
    private static string lastRecordedReason = string.Empty;

    public static string Current => current;
    public static string History => history.Count == 0 ? "\u65E0" : string.Join(" -> ", history);

    public static void Reset()
    {
        history.Clear();
        current = "\u65E0";
        lastRecordedReason = string.Empty;
    }

    public static void Update()
    {
        if (!ActionHelper.IsReady(NinjaActions.TrickAttackCurrent))
        {
            current = "36958 \u672A\u8F6C\u597D";
            lastRecordedReason = string.Empty;
            return;
        }

        var reason = GetBlockReason();
        if (reason == null)
        {
            current = "36958 \u5DF2\u8F6C\u597D\uFF0C\u80CC\u523A\u89E3\u6790\u5668\u5141\u8BB8\u4F7F\u7528";
            lastRecordedReason = string.Empty;
            return;
        }

        current = reason;
        if (reason == lastRecordedReason) return;

        lastRecordedReason = reason;
        history.Add(reason);
        if (history.Count > HistoryLimit)
            history.RemoveAt(0);
    }

    private static string? GetBlockReason()
    {
        if (NinjaCombatState.IsTcjSequenceActive)
            return "\u5929\u5730\u4EBA\u72B6\u6001\u62E6\u622A";
        if (!NinjaCombatState.CanUsePostNinjutsuOffGcd)
            return "\u5FCD\u672F\u540E\u80FD\u529B\u6280\u989D\u5EA6\u62E6\u622A";
        if (!NinjaCombatState.CanUseGcdOffGcd)
            return "\u5F53\u524D GCD \u80FD\u529B\u6280\u989D\u5EA6\u62E6\u622A";
        if (ActionUpdaterRouter.HasActiveCommand())
            return "\u8C03\u5EA6\u5668\u5DF2\u6709\u6D3B\u52A8\u547D\u4EE4";
        if (ActionUpdaterRouter.HasLockedGcdAction())
            return "\u5B58\u5728\u9501\u5B9A GCD \u6280\u80FD";
        if (ActionQueueManager.HasActionsInQueue())
            return "\u52A8\u4F5C\u961F\u5217\u5DF2\u6709\u5F85\u63D0\u4EA4\u6280\u80FD";

        var kassatsu = new NinjaKassatsuResolver().Check();
        if (kassatsu.Success)
            return "\u88AB\u751F\u6740\u4E88\u593A\u4F18\u5148\u7EA7\u62E6\u622A";

        return NinjaTrickAttackResolver.GetBlockReason();
    }

}