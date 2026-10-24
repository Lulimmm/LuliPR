using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Action;

internal abstract class NinjaResolver : IDecisionResolver
{
    protected static CheckResult No => new(false, "未满足使用条件");
    protected static CheckResult Ok => new(true, string.Empty);
    protected static CheckResult Blocked(string message) => new(false, message);
    protected static bool Ready(uint id) => ActionHelper.IsReady(id);
    protected static bool InMeleeRange => Core.Target != null && Core.Me != null
        && NinjaTarget.Distance <= NinjaTarget.AttackRange;
    protected static bool HasMeleeTarget => Core.Target?.CanUseAttackActionOn() == true && InMeleeRange;
    protected static PAction Build(uint id, ActionType type, ActionTargetType target)
        => new(id, type, target) { RequiresVerification = true };
    // Normal combo actions are not opener actions and do not wait for
    // UAL/AEP confirmation before the next rotation decision.
    protected static PAction BuildNormal(uint id, ActionType type, ActionTargetType target)
        => new(id, type, target);
    public abstract CheckResult Check();
    public abstract PAction GetAction();
}
