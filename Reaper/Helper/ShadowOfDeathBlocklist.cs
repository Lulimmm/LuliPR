using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;

namespace Reaper.PR;

internal static class ShadowOfDeathBlocklist
{
    private static readonly HashSet<uint> BlockedDataIds = new();

    internal static bool IsBlocked(IBattleChara? target)
        => target != null && BlockedDataIds.Contains(target.BaseId);

    internal static void Set(uint dataId, bool enabled)
    {
        if (dataId == 0) return;
        if (enabled) BlockedDataIds.Add(dataId);
        else BlockedDataIds.Remove(dataId);
    }

    internal static void Reset() => BlockedDataIds.Clear();

    internal static uint GetShadowOfDeathOrComboActionId()
        => IsBlocked(R.Target) ? BaseGcd.GetComboActionId() : R.ShadowOfDeath;
}
