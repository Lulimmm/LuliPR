using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.PureTimeline;
using PromeRotation.Rotation;
using PromeRotation.Timeline;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Opener;

public sealed class NinjaNoLoss9GOpener : NinjaOpener
{
    public override string OpenerName => NoLoss9GOpenerName;

    public override void InitializeCountdown(CountDownHandler handler)
    {
        // Start the three mudras at 5 seconds. PR's normal Ninja opener
        // submits Suiton at 650ms so the action resolves on the 0-second
        // pull; an action scheduled at 0ms is skipped when combat starts.
        handler.AddAction(5000, Countdown(NinjaActions.Ten));
        handler.AddAction(4500, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(4000, Countdown(NinjaActions.JinCombo));
        handler.AddAction(650, Countdown(NinjaActions.Suiton, ActionTargetType.Target));
    }

    public override List<PAction> InCombatSequence
    {
        get
        {
            NinjaSequencePolicy.Begin(NinjaActions.TenChiJin);
            var actions = new List<PAction>
            {
                G(NinjaActions.SpinningEdge),
                G(NinjaActions.GustSlash),
                WeaveOSecondHalf(NinjaActions.MugCurrent),
                M(NinjaActions.Ten),
                M(NinjaActions.ChiCombo),
                G(NinjaActions.Raiton),
                SelfO(NinjaActions.Bunshin),
                G(NinjaActions.PhantomKamaitachi),
                WeaveOSecondHalf(NinjaActions.TrickAttackCurrent),
                G(NinjaActions.FleetingRaiju),
                SelfO(NinjaActions.Kassatsu),
                O(NinjaActions.Assassinate),
                M(NinjaActions.TenCombo),
                M(NinjaActions.JinCombo),
                G(NinjaActions.HyoshoRanryu),
                SelfOUnverified(NinjaActions.TenChiJin)
            };
            if (NinjaSettings.Instance.OpenerBurst && Potion() is { } potion)
                actions.Insert(1, potion);
            return actions;
        }
    }
}
