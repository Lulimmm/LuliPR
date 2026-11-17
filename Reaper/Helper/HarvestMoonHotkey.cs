using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.TargetSelector;
using PromeRotation.UI.HotKey;
using PromeRotation.Updaters;

namespace Reaper.PR;

internal sealed class HarvestMoonHotkey : IHotkeyLogic
{
    private const uint HarvestMoonActionId = 24388u;
    private const uint HarvestMoonReadyStatusId = 2594u;

    public bool IsActive() => false;

    public void OnClick()
    {
        var me = Core.Me;
        if (me?.HasStatus(HarvestMoonReadyStatusId) != true
            || !ActionHelper.TryGetActionRange(HarvestMoonActionId, out var actionRange))
            return;

        IBattleChara? nearestTarget = null;
        var nearestDistance = float.MaxValue;
        foreach (var obj in Svc.Objects)
        {
            if (obj is not IBattleChara candidate || !TargetSelectorHelper.IsValidHostileTarget(candidate))
                continue;

            var distance = TargetSelectorHelper.GetTargetDistance(me, candidate);
            if (distance > actionRange || distance >= nearestDistance)
                continue;

            nearestTarget = candidate;
            nearestDistance = distance;
        }

        if (nearestTarget == null) return;

        Core.SetTarget(nearestTarget);
        ActionUpdaterRouter.TryEnqueueForcedAction(new PAction(HarvestMoonActionId, ActionType.Gcd, ActionTargetType.Target)
        {
            NetworkTid = nearestTarget.EntityId
        });
    }
}
