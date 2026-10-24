using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

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

    // Ninja's normal melee range is 3m. Use the PR range helper so range
    // correction remains centralized.
    public static float AttackRange => GameData.GetCurrentAttackRange(3f);
}
