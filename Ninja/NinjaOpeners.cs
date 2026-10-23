using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace Ninja.PR;

public abstract class NinjaOpener : IOpener
{
    protected static PAction G(uint id) => new(id, ActionType.Gcd, ActionTargetType.Target) { RequiresVerification = true };
    protected static PAction S(uint id) => new(id, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true };
    // AE marks every mudra GCD with DontUseGcdOpt. NinjaAeSlotSequence
    // supplies the equivalent strict submit policy for this opener.
    protected static PAction M(uint id) => new(id, ActionType.Gcd, ActionTargetType.Self) { RequiresVerification = true };
    // Mudra and Suiton are regular GCD actions in AE. Keep that type in the
    // countdown queue so the PR scheduler applies the same GCD readiness and
    // target handling before the pull.
    protected static PAction Countdown(uint id, ActionTargetType target = ActionTargetType.Self) =>
        new(id, ActionType.Gcd, target) { RequiresVerification = true };
    protected static PAction O(uint id) => new(id, ActionType.OffGcd, ActionTargetType.Target) { RequiresVerification = true };
    protected static PAction SelfO(uint id) => new(id, ActionType.OffGcd, ActionTargetType.Self) { RequiresVerification = true };
    protected static PAction WeaveO(uint id, int elapsedMs) => O(id).WithWeaveDelay(elapsedMs);
    protected static PAction? Potion()
    {
        var id = GameData.GetBestPotionId();
        return id == 0 ? null : new PAction(id, ActionType.Item, ActionTargetType.Self) { RequiresVerification = true };
    }
    protected static PAction Ground(uint id)
    {
        var position = Core.Target?.Position ?? Core.Me?.Position ?? default;
        var adjustedId = ActionHelper.GetAdjustedActionId(id);
        return new PAction(adjustedId == 0 ? id : adjustedId, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true,
            IsLocationAction = true,
            Position = position
        };
    }

    protected static void AddStandardCountdown(CountDownHandler handler)
    {
        handler.AddAction(5000, Countdown(NinjaActions.Ten));
        handler.AddAction(4500, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(4000, Countdown(NinjaActions.JinCombo));
        handler.AddAction(650, Countdown(NinjaActions.Suiton, ActionTargetType.Target));
    }

    public abstract string OpenerName { get; }
    public abstract List<PAction> InCombatSequence { get; }
    public virtual void InitializeCountdown(CountDownHandler handler) => AddStandardCountdown(handler);
}

public sealed class NinjaStandardOpener : NinjaOpener
{
    public override string OpenerName => "常规斩击背刺起手";
    public override List<PAction> InCombatSequence
    {
        get
        {
            NinjaAeSlotSequence.Begin(NinjaActions.TenChiJin);
            var actions = new List<PAction>
            {
                SelfO(NinjaActions.Kassatsu), G(NinjaActions.SpinningEdge), G(NinjaActions.GustSlash),
                O(NinjaActions.MugCurrent), G(NinjaActions.ArmorCrush), SelfO(NinjaActions.Bunshin),
                G(NinjaActions.PhantomKamaitachi), WeaveO(NinjaActions.TrickAttackCurrent, 365), M(NinjaActions.TenCombo),
                M(NinjaActions.JinCombo), G(NinjaActions.HyoshoRanryu), SelfO(NinjaActions.TenChiJin)
            };
            if (NinjaSettings.Instance.OpenerBurst && Potion() is { } potion) actions.Insert(2, potion);
            return actions;
        }
    }
}

public sealed class NinjaEdOpener : NinjaOpener
{
    public override string OpenerName => "ED起手";
    public override List<PAction> InCombatSequence
    {
        get
        {
            NinjaAeSlotSequence.Begin(NinjaActions.TenChiJin);
            var actions = new List<PAction>
            {
                G(NinjaActions.SpinningEdge), G(NinjaActions.GustSlash), O(NinjaActions.MugCurrent), SelfO(NinjaActions.Bunshin),
                G(NinjaActions.ArmorCrush), SelfO(NinjaActions.Kassatsu), G(NinjaActions.SpinningEdge), WeaveO(NinjaActions.TrickAttackCurrent, 465),
                G(NinjaActions.PhantomKamaitachi), M(NinjaActions.TenCombo), M(NinjaActions.JinCombo),
                G(NinjaActions.HyoshoRanryu), SelfO(NinjaActions.TenChiJin)
            };
            if (NinjaSettings.Instance.OpenerBurst && Potion() is { } potion) actions.Insert(1, potion);
            return actions;
        }
    }

    public override void InitializeCountdown(CountDownHandler handler)
    {
        handler.AddAction(10500, Countdown(NinjaActions.Ten));
        handler.AddAction(10000, Countdown(NinjaActions.JinCombo));
        handler.AddAction(9500, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(4750, () => Ground(2270));
        handler.AddAction(3500, Countdown(NinjaActions.Ten));
        handler.AddAction(3000, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(2500, Countdown(NinjaActions.JinCombo));
        handler.AddAction(600, Countdown(NinjaActions.Suiton, ActionTargetType.Target));
    }
}
