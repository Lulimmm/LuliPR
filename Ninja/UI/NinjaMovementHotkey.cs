using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using PromeRotation.Helpers;
using PromeRotation.UI.HotKey;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.UI;

// Use Shukuchi at a point 20 yalms in the camera direction instead of
// targeting the current enemy.
internal sealed class NinjaMovementHotkey : IHotkeyLogic
{
    private readonly bool oppositeCamera;

    public NinjaMovementHotkey(bool oppositeCamera = false)
    {
        this.oppositeCamera = oppositeCamera;
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

    public void OnClick() => TryUse(oppositeCamera);

    public static void TryUse()
        => TryUse(false);

    public static void TryUseOppositeCamera()
        => TryUse(true);

    private static void TryUse(bool oppositeCamera)
    {
        if (!HasCharge()) return;
        var me = PromeRotation.Core.Core.Me;
        if (me == null) return;

        if (!TryGetCameraRotation(out var cameraRotation)) return;

        if (oppositeCamera)
            cameraRotation += MathF.PI;

        ActionHelper.UseActionLocation(
            NinjaActions.Shukuchi,
            0,
            GetForwardPosition(me.Position, cameraRotation, 20f));
    }

    private static Vector3 GetForwardPosition(Vector3 position, float facingRadians, float distance)
    {
        var dx = MathF.Sin(facingRadians) * distance;
        var dz = MathF.Cos(facingRadians) * distance;
        return new Vector3(position.X + dx, position.Y + 5f, position.Z + dz);
    }

    // Read the active camera directly. The old AE port used
    // RaptureAtkModule.NumberArrays[24].IntArray[3], which is a UI angle
    // snapshot and can lag or differ from the camera used for world input.
    private static unsafe bool TryGetCameraRotation(out float rotation)
    {
        rotation = 0f;
        var cameraManager = CameraManager.Instance();
        if (cameraManager == null) return false;

        var camera = cameraManager->GetActiveCamera();
        if (camera == null) return false;

        var dirH = ((CameraEx*)camera)->DirH;
        var sign = Math.Sign(dirH) == -1 ? -1 : 1;
        rotation = (float)((Math.Abs(dirH) - Math.PI) * sign);
        return true;
    }

}

// The horizontal camera direction lives at this offset in the current client
// Camera object. Keep this local layout small and independent of UI arrays.
[StructLayout(LayoutKind.Explicit, Size = 688)]
internal struct CameraEx
{
    [FieldOffset(320)]
    public float DirH;
}
