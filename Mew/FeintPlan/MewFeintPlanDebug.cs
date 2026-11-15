using System.Collections.Generic;
using ECommons.DalamudServices;
using LuliPR.Common;

namespace Mew.FeintPlan;

internal static class MewFeintPlanDebug
{
    private const int Capacity = 96;
    private static readonly Queue<string> Messages = new();

    public static IReadOnlyCollection<string> RecentMessages => Messages;

    public static void Print(string message) => Write(message, false);

    public static void Print(double timeInSec, string message)
        => Write($"[{timeInSec:F1}s] {message}", false);

    public static void Error(string message) => Write(message, true);

    private static void Write(string message, bool error)
    {
        if (error)
            Svc.Log.Error($"[Mew 牵制规划] {message}");

        if (!AcrSettingsStore.Current.MewFeintPlan.ShowDebugInfo)
            return;

        if (Messages.Count == Capacity)
            Messages.Dequeue();
        Messages.Enqueue(message);

        if (!error)
            Svc.Log.Debug($"[Mew 牵制规划] {message}");
    }
}
