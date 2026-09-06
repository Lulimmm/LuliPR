using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;

namespace Reaper.PR;

/// <summary>
/// PR-side implementation of the two native helpers used by the AE rotation.
/// The hooks are deliberately owned here instead of relying on AEAssist state.
/// </summary>
internal static class ReaperHooks
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate float ActionRangeDelegate(uint actionId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte MoveCastingDelegate(long a1, long a2, long a3, byte a4);

    private const string ActionRangeSignature =
        "48 89 5C 24 ?? 57 48 ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 8B ?? 0F 29 74 24 20";
    private const string MoveCastingSignature =
        "48 89 5C 24 ?? 48 89 74 24 ?? 4C 89 64 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70";
    private const string UpdatePositionInstanceSignature = "41 B8 ?? ?? ?? ?? F6 C2";
    private const string UpdatePositionHandlerSignature =
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B F9 41 8B D8";

    private static Hook<ActionRangeDelegate>? actionRangeHook;
    private static Hook<MoveCastingDelegate>? moveCastingHook;
    private static float cachedExtraRange;
    private static ushort updatePositionInstance;
    private static ushort updatePositionHandler;
    private static bool previousActionRange;
    private static bool previousMoveCasting;

    public static void Update()
    {
        var settings = PromeSettings.Instance;
        var rangeEnabled = settings.GetQt("长臂猿");
        var moveEnabled = settings.GetQt("移动读条");

        if (rangeEnabled != previousActionRange)
        {
            if (rangeEnabled)
            {
                cachedExtraRange = Math.Clamp(ReaperRotation.ExtraRange, 0f, 3f);
                EnsureActionRangeHook();
            }
            else
            {
                DisableActionRangeHook();
            }
            previousActionRange = rangeEnabled;
        }
        else if (rangeEnabled && actionRangeHook != null)
        {
            // Keep the same cached-value behavior as AE when the setting is
            // edited while the patch remains enabled.
            cachedExtraRange = Math.Clamp(ReaperRotation.ExtraRange, 0f, 3f);
        }

        if (moveEnabled != previousMoveCasting)
        {
            if (moveEnabled)
                EnsureMoveCastingHook();
            else
                DisableMoveCastingHook();
            previousMoveCasting = moveEnabled;
        }
        else if (!moveEnabled && moveCastingHook != null)
        {
            DisableMoveCastingHook();
        }
    }

    public static void Dispose()
    {
        DisableMoveCastingHook();
        DisableActionRangeHook();
    }

    private static void EnsureActionRangeHook()
    {
        if (actionRangeHook != null)
            return;

        try
        {
            var address = Svc.SigScanner.ScanText(ActionRangeSignature);
            if (address == IntPtr.Zero)
            {
                Svc.Log.Warning("Luli Reaper PR: ActionRange signature was not found");
                return;
            }

            actionRangeHook = Svc.Hook.HookFromAddress<ActionRangeDelegate>(address, ActionRangeDetour);
            actionRangeHook.Enable();
            Svc.Log.Information("Luli Reaper PR: ActionRange hook enabled");
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Reaper PR: failed to enable ActionRange hook");
            actionRangeHook = null;
        }
    }

    private static void EnsureMoveCastingHook()
    {
        if (moveCastingHook != null)
            return;

        try
        {
            InitializeMoveCastingOpcodes();
            var address = Svc.SigScanner.ScanText(MoveCastingSignature);
            if (address == IntPtr.Zero)
            {
                Svc.Log.Warning("Luli Reaper PR: MoveCasting signature was not found");
                return;
            }

            moveCastingHook = Svc.Hook.HookFromAddress<MoveCastingDelegate>(address, MoveCastingDetour);
            moveCastingHook.Enable();
            Svc.Log.Information("Luli Reaper PR: MoveCasting hook enabled");
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Reaper PR: failed to enable MoveCasting hook");
            moveCastingHook = null;
        }
    }

    private static void InitializeMoveCastingOpcodes()
    {
        var instanceAddress = Svc.SigScanner.ScanText(UpdatePositionInstanceSignature);
        var handlerAddress = Svc.SigScanner.ScanText(UpdatePositionHandlerSignature);
        if (instanceAddress == IntPtr.Zero || handlerAddress == IntPtr.Zero)
            throw new InvalidOperationException("MoveCasting update-position signatures were not found");

        updatePositionInstance = (ushort)Marshal.ReadInt16(instanceAddress + 2);
        updatePositionHandler = (ushort)Marshal.ReadInt16(handlerAddress + 81);
    }

    private static void DisableActionRangeHook()
    {
        if (actionRangeHook == null)
            return;

        try
        {
            actionRangeHook.Dispose();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Reaper PR: failed to disable ActionRange hook");
        }
        finally
        {
            actionRangeHook = null;
            cachedExtraRange = 0f;
            previousActionRange = false;
        }
    }

    private static void DisableMoveCastingHook()
    {
        if (moveCastingHook == null)
            return;

        try
        {
            moveCastingHook.Dispose();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Reaper PR: failed to disable MoveCasting hook");
        }
        finally
        {
            moveCastingHook = null;
            updatePositionInstance = 0;
            updatePositionHandler = 0;
            previousMoveCasting = false;
        }
    }

    private static float ActionRangeDetour(uint actionId)
    {
        var hook = actionRangeHook;
        if (hook == null)
            return 0f;

        try
        {
            return hook.Original(actionId) + cachedExtraRange;
        }
        catch
        {
            return hook.Original(actionId);
        }
    }

    private static byte MoveCastingDetour(long a1, long a2, long a3, byte a4)
    {
        var hook = moveCastingHook;
        if (hook == null)
            return a4;

        try
        {
            var me = Core.Me;
            if (me == null || !ReaperEventHandler.InBattle || !me.IsCasting)
                return hook.Original(a1, a2, a3, a4);

            var remaining = me.TotalCastTime - me.CurrentCastTime;
            if (remaining <= ReaperRotation.MoveCasting || a2 == 0)
                return hook.Original(a1, a2, a3, a4);

            var opcode = (ushort)Marshal.ReadInt16((nint)a2);
            if (opcode == updatePositionInstance || opcode == updatePositionHandler)
                return 1;

            return hook.Original(a1, a2, a3, a4);
        }
        catch
        {
            return hook.Original(a1, a2, a3, a4);
        }
    }
}
