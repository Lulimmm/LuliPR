using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;

using FfxivActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

using Ninja.PR.Data;

namespace Ninja.PR.Helper;

// During a normal mudra group, allow only the action currently owned by the
// rotation updater. Other player action requests are discarded until the
// group completes. Ten Chi Jin is excluded by NinjaSequencePolicy.
internal static class NinjaActionInputGuard
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate bool UseActionDelegate(
        ActionManager* self,
        FfxivActionType actionType,
        uint actionId,
        ulong targetId,
        uint extraParam,
        ActionManager.UseActionMode mode,
        uint comboRouteId,
        bool* outOptAreaTargeted);

    private static Hook<UseActionDelegate>? useActionHook;

    public static unsafe void Attach()
    {
        if (useActionHook != null) return;

        try
        {
            var address = ActionManager.Addresses.UseAction.Value;
            if (address == 0)
            {
                Svc.Log.Warning("Luli Ninja: UseAction address was not found");
                return;
            }

            useActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>(address, OnUseAction);
            useActionHook.Enable();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Ninja: failed to enable normal mudra input guard");
            useActionHook = null;
        }
    }

    public static void Dispose()
    {
        if (useActionHook == null) return;
        try
        {
            useActionHook.Dispose();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Ninja: failed to disable normal mudra input guard");
        }
        finally
        {
            useActionHook = null;
        }
    }

    private static unsafe bool OnUseAction(
        ActionManager* self,
        FfxivActionType actionType,
        uint actionId,
        ulong targetId,
        uint extraParam,
        ActionManager.UseActionMode mode,
        uint comboRouteId,
        bool* outOptAreaTargeted)
    {
        if (actionType == FfxivActionType.Action
            && NinjaSequencePolicy.IsNormalMudraActive
            && !IsRotationOwnedAction(actionId))
            return false;

        return useActionHook!.Original(self, actionType, actionId, targetId, extraParam,
            mode, comboRouteId, outOptAreaTargeted);
    }

    private static bool IsRotationOwnedAction(uint actionId)
    {
        var active = NinjaRotation.PeekLockedActionMethod?.Invoke(null, null) as PAction;
        if (active != null && ActionIdsMatch(actionId, active.ActionId)) return true;

        var attempted = NinjaRotation.LastAttemptedActionField?.GetValue(null) as PAction;
        return attempted != null && ActionIdsMatch(actionId, attempted.ActionId);
    }

    private static bool ActionIdsMatch(uint observed, uint expected)
    {
        if (observed == expected) return true;
        var observedAdjusted = ActionHelper.GetAdjustedActionId(observed);
        var expectedAdjusted = ActionHelper.GetAdjustedActionId(expected);
        return observedAdjusted != 0 && observedAdjusted == expectedAdjusted;
    }
}
