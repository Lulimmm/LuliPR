using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.PureTimeline;
using PromeRotation.Rotation;

using Ninja.PR.Data;
using Ninja.PR.Helper;

namespace Ninja.PR.Opener;

/// <summary>
/// AE ED opener migrated to the PR fixed opener contract.
/// </summary>
public sealed class NinjaEdenOpener : NinjaOpener
{
    public override string OpenerName => EdenOpenerName;

    public override void InitializeCountdown(CountDownHandler handler)
    {
        handler.AddAction(10500, Countdown(NinjaActions.Ten));
        handler.AddAction(10000, Countdown(NinjaActions.JinCombo));
        handler.AddAction(9500, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(4750, Countdown(2270));
        handler.AddAction(3500, Countdown(NinjaActions.Ten));
        handler.AddAction(3000, Countdown(NinjaActions.ChiCombo));
        handler.AddAction(2500, Countdown(NinjaActions.JinCombo));
        handler.AddAction(600, Countdown(NinjaActions.Suiton, ActionTargetType.Target));
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
                O(NinjaActions.MugCurrent),
                SelfO(NinjaActions.Bunshin),
                G(NinjaActions.ArmorCrush),
                SelfO(NinjaActions.Kassatsu),
                G(NinjaActions.SpinningEdge),
                WeaveO(NinjaActions.TrickAttackCurrent, 465),
                G(NinjaActions.PhantomKamaitachi),
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
