using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using Ninja.PR.Data;

namespace Ninja.PR.Helper;

internal static class NinjaTarget
{
    public static float Distance
    {
        get
        {
            var me = Core.Me;
            var target = Core.Target;
            return me == null || target == null
                ? float.MaxValue
                : target.DistanceToMe();
        }
    }

    // Match Reaper's long-arm behavior: the ACR's own melee checks use the
    // same 3m base range plus the configured extra range as the native action
    // range hook.
    public static float AttackRange => PromeSettings.Instance.GetQt(NinjaQts.LongArm)
        ? 3f + Math.Clamp(NinjaSettings.Instance.ExtraRange, 0f, 3f)
        : 3f;
}
