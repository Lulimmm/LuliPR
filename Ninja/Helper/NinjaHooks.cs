using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

// Native action range extension used by the Ninja long-arm QT. NinjaTarget
// mirrors the same value so ACR decisions and client validation agree.
internal static class NinjaHooks
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate float ActionRangeDelegate(uint actionId);

    private const string ActionRangeSignature =
        "48 89 5C 24 ?? 57 48 ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 8B ?? 0F 29 74 24 20";

    private static Hook<ActionRangeDelegate>? actionRangeHook;
    private static float cachedExtraRange;
    private static bool previousEnabled;

    public static void Update()
    {
        var enabled = PromeSettings.Instance.GetQt(NinjaQts.LongArm);
        if (enabled != previousEnabled)
        {
            if (enabled)
            {
                cachedExtraRange = Math.Clamp(NinjaSettings.Instance.ExtraRange, 0f, 3f);
                EnsureActionRangeHook();
            }
            else
            {
                DisableActionRangeHook();
            }
            previousEnabled = enabled;
        }
        else if (enabled && actionRangeHook != null)
        {
            cachedExtraRange = Math.Clamp(NinjaSettings.Instance.ExtraRange, 0f, 3f);
        }
    }

    public static void Dispose() => DisableActionRangeHook();

    private static void EnsureActionRangeHook()
    {
        if (actionRangeHook != null) return;
        try
        {
            var address = Svc.SigScanner.ScanText(ActionRangeSignature);
            if (address == IntPtr.Zero)
            {
                Svc.Log.Warning("Luli Ninja PR: ActionRange signature was not found");
                return;
            }

            actionRangeHook = Svc.Hook.HookFromAddress<ActionRangeDelegate>(address, ActionRangeDetour);
            actionRangeHook.Enable();
            Svc.Log.Information("Luli Ninja PR: ActionRange hook enabled");
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Luli Ninja PR: failed to enable ActionRange hook");
            actionRangeHook = null;
        }
    }

    private static void DisableActionRangeHook()
    {
        if (actionRangeHook == null)
        {
            cachedExtraRange = 0f;
            previousEnabled = false;
            return;
        }

        try { actionRangeHook.Dispose(); }
        catch (Exception ex) { Svc.Log.Error(ex, "Luli Ninja PR: failed to disable ActionRange hook"); }
        finally
        {
            actionRangeHook = null;
            cachedExtraRange = 0f;
            previousEnabled = false;
        }
    }

    private static float ActionRangeDetour(uint actionId)
    {
        var hook = actionRangeHook;
        if (hook == null) return 0f;
        try { return hook.Original(actionId) + cachedExtraRange; }
        catch { return hook.Original(actionId); }
    }
}
