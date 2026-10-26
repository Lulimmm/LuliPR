using PromeRotation.Helpers;

namespace Ninja.PR.Helper;

internal enum NinjaGcdPhase
{
    Inactive,
    FirstHalf,
    SecondHalf
}

/// <summary>
/// Reads the live weaponskill recast and exposes its two timing halves.
/// The value is calculated on every call so skill-speed changes and temporary
/// recast modifiers are reflected by opener decisions.
/// </summary>
internal static class NinjaGcdTiming
{
    private const float MinimumGcdSeconds = 0.01f;

    public static float TotalSeconds => MathF.Max(0f, ActionHelper.GetGcdTotal());
    public static float ElapsedSeconds => MathF.Max(0f, ActionHelper.GetGcdElapsed());
    public static float HalfSeconds => TotalSeconds * 0.5f;

    public static bool IsActive => ActionHelper.GetGcdRemain() > 0f
        && TotalSeconds >= MinimumGcdSeconds;

    public static NinjaGcdPhase CurrentPhase
    {
        get
        {
            if (!IsActive) return NinjaGcdPhase.Inactive;
            return ElapsedSeconds < HalfSeconds
                ? NinjaGcdPhase.FirstHalf
                : NinjaGcdPhase.SecondHalf;
        }
    }

    public static bool IsFirstHalf => CurrentPhase == NinjaGcdPhase.FirstHalf;
    public static bool IsSecondHalf => CurrentPhase == NinjaGcdPhase.SecondHalf;

    // PAction.MinGcdElapsedForWeaveMs uses elapsed GCD milliseconds.
    public static int HalfElapsedMilliseconds =>
        Math.Max(0, (int)MathF.Ceiling(HalfSeconds * 1000f));
}
