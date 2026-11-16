using Mew.FeintPlan.Events;

namespace Mew.FeintPlan.Scripts.TOP;

internal static class TOPData
{
    private sealed class RepeatedCastState
    {
        public long FirstCastAt;
        public bool HasFirstCast;
        public bool SecondCastAccepted;
    }

    private static readonly Dictionary<uint, RepeatedCastState> RepeatedCasts = new();

    public static void ResetRuntime() => RepeatedCasts.Clear();

    public static bool AfterCast(
        uint nodeId,
        IMewFeintEvent? context,
        int delayMs,
        params uint[] actionIds)
    {
        if (context is EnemyCastSpellCondParams cast && Contains(actionIds, cast.SpellId))
            FeintHelper.SetDelayOnce(nodeId, delayMs);

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    public static bool AfterSecondCast(
        uint nodeId,
        IMewFeintEvent? context,
        uint actionId,
        int minimumGapMs,
        int delayAfterSecondCastMs)
    {
        if (!RepeatedCasts.TryGetValue(nodeId, out var state))
            RepeatedCasts[nodeId] = state = new RepeatedCastState();

        if (!state.SecondCastAccepted
            && context is EnemyCastSpellCondParams cast
            && cast.SpellId == actionId)
        {
            var now = Environment.TickCount64;
            if (!state.HasFirstCast)
            {
                state.FirstCastAt = now;
                state.HasFirstCast = true;
            }
            else if (now - state.FirstCastAt >= minimumGapMs)
            {
                state.SecondCastAccepted = true;
                FeintHelper.SetDelayOnce(nodeId, delayAfterSecondCastMs);
            }
        }

        return FeintHelper.IsReady(nodeId, () => MewFeintPlanManager.IsFeintAvailable());
    }

    private static bool Contains(uint[] actionIds, uint actionId)
    {
        foreach (var id in actionIds)
            if (id == actionId) return true;
        return false;
    }
}
