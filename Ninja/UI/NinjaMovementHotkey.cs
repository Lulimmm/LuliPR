using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PromeRotation.Helpers;
using PromeRotation.UI.HotKey;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.UI;

// Use Shukuchi at a point 20 yalms in the camera direction instead of
// targeting the current enemy.
internal sealed class NinjaMovementHotkey : IHotkeyLogic
{
    private static unsafe RaptureAtkModule* GetRaptureAtkModule()
    {
        var framework = Framework.Instance();
        if (framework == null) return null;

        var uiModule = framework->GetUIModule();
        if (uiModule == null) return null;

        return uiModule->GetRaptureAtkModule();
    }

    // Match AE's IsUnlockWithCDCheck: reject the short post-cast duplicate
    // window, accept a ready charge, and allow the 100 ms ability queue window
    // when the last charge is about to become available.
    private static bool HasCharge()
    {
        if (!ActionHelper.IsUnlocked(NinjaActions.Shukuchi)
            || NinjaCombatState.Recently(NinjaActions.Shukuchi, 500)) return false;

        if (ActionHelper.GetActionCharges(NinjaActions.Shukuchi) >= 1f)
            return true;

        return ActionHelper.TryGetActionCooldown(NinjaActions.Shukuchi, out var cooldown)
            && cooldown <= 0.1f;
    }

    public bool IsActive() => HasCharge();

    public void OnClick() => TryUse();

    public static void TryUse()
    {
        if (!HasCharge()) return;
        var me = PromeRotation.Core.Core.Me;
        if (me == null) return;

        ActionHelper.UseActionLocation(
            NinjaActions.Shukuchi,
            0,
            GetForwardPosition(me.Position, GetCameraRotation(), 20f));
    }

    private static Vector3 GetForwardPosition(Vector3 position, float facingRadians, float distance)
    {
        var dx = MathF.Sin(facingRadians) * distance;
        var dz = MathF.Cos(facingRadians) * distance;
        return new Vector3(position.X + dx, position.Y + 5f, position.Z + dz);
    }

    private static unsafe float GetCameraRotation()
    {
        var raptureAtkModule = GetRaptureAtkModule();
        if (raptureAtkModule == null) return 0f;

        var atkModule = (AtkModule*)raptureAtkModule;
        var numberArray = atkModule->AtkArrayDataHolder.NumberArrays[24];
        if (numberArray == null) return 0f;

        var degrees = numberArray->IntArray[3];
        var sign = Math.Sign(degrees) == -1 ? -1 : 1;
        return (float)((Math.Abs(degrees * (Math.PI / 180.0)) - Math.PI) * sign);
    }
}
