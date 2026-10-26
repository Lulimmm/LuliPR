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

internal static class NinjaCombatState
{
    private static readonly Dictionary<uint, long> ActionTimes = new();
    public static uint LastGcd { get; private set; }
    // The TCJ follow-up must use the most recently confirmed TCJ jutsu.
    // A time-window check is insufficient because all three jutsu can be
    // inside the same 2.12s window.
    public static uint LastTcjJutsu { get; private set; }
    public static uint PendingTcjJutsu { get; private set; }
    public static long PendingTcjJutsuAt { get; private set; }
    public static uint LastObservedActionId { get; private set; }
    public static long LastObservedActionAt { get; private set; }
    private static uint LastConfirmedActionId { get; set; }
    private static long LastConfirmedActionAt { get; set; }
    // AE's AfterSpell sets CurrGcdAbilityCount to 1 after the final
    // ninjutsu. Keep the same one-oGCD budget until the next GCD confirms.
    public static int PostNinjutsuOffGcdLimit { get; private set; }
    public static int PostNinjutsuOffGcdUsed { get; private set; }
    public static bool IsPostNinjutsuOffGcdLimited => PostNinjutsuOffGcdLimit > 0;
    public static bool CanUsePostNinjutsuOffGcd =>
        PostNinjutsuOffGcdLimit == 0 || PostNinjutsuOffGcdUsed < PostNinjutsuOffGcdLimit;
    public static int GcdOffGcdUsed { get; private set; }
    public static int MaxGcdOffGcd => Math.Clamp(PromeSettings.Instance.Hacks.MaxOgcdsPerGcd, 1, 5);
    public static bool CanUseGcdOffGcd => GcdOffGcdUsed < MaxGcdOffGcd;
    public static bool CanQueueMultipleOffGcd =>
        !IsPostNinjutsuOffGcdLimited && GcdOffGcdUsed + 2 <= MaxGcdOffGcd;
    private const int PendingTcjTimeoutMs = 2500;

    public static void Reset()
    {
        NinjaMudraDebug.Reset();
        ActionTimes.Clear();
        LastGcd = 0;
        LastTcjJutsu = 0;
        PendingTcjJutsu = 0;
        PendingTcjJutsuAt = 0;
        LastObservedActionId = 0;
        LastObservedActionAt = 0;
        LastConfirmedActionId = 0;
        LastConfirmedActionAt = 0;
        PostNinjutsuOffGcdLimit = 0;
        PostNinjutsuOffGcdUsed = 0;
        GcdOffGcdUsed = 0;
    }

    public static void Record(uint id, bool confirmTcjStep = true)
    {
        var now = Environment.TickCount64;
        LastObservedActionId = id;
        LastObservedActionAt = now;
        NinjaMudraDebug.Record(id);
        var canonical = CanonicalActionId(id);
        var tcjEvent = ResolveTcjEventId(id);
        if (tcjEvent != 0) canonical = tcjEvent;
        ActionTimes[id] = now;
        if (canonical != id) ActionTimes[canonical] = now;
        NinjaSequencePolicy.Record(canonical);
        if (canonical == NinjaActions.TenChiJin)
        {
            LastTcjJutsu = 0;
            PendingTcjJutsu = 0;
            PendingTcjJutsuAt = 0;
        }
        else if (confirmTcjStep
            && canonical is (NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton))
        {
            LastTcjJutsu = canonical;
            // The client can report the transformed action through a
            // different resolved alias. Any recognized TCJ jutsu therefore
            // confirms the pending submission, regardless of raw ID.
            PendingTcjJutsu = 0;
            PendingTcjJutsuAt = 0;
        }
        if ((confirmTcjStep
                && canonical is (NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton))
            || canonical is (NinjaActions.SpinningEdge or NinjaActions.GustSlash or NinjaActions.AeolianEdge or NinjaActions.ArmorCrush
                or NinjaActions.Suiton or NinjaActions.Raiton or NinjaActions.PhantomKamaitachi or NinjaActions.FleetingRaiju
                or NinjaActions.HyoshoRanryu))
            LastGcd = canonical;
    }

    // This is called only for confirmed action-effect events. Start-cast
    // events are intentionally excluded so an unsuccessful input cannot
    // consume the AE-equivalent oGCD budget.
    public static void RecordConfirmed(uint id)
    {
        var now = Environment.TickCount64;
        var canonical = CanonicalActionId(id);
        // ActionEffect and ActionEffectEntry can describe the same packet.
        // Count it once so duplicate entries cannot consume two oGCD slots or
        // repeatedly reopen/reset the per-GCD budget.
        if (canonical == LastConfirmedActionId
            && now - LastConfirmedActionAt <= 250) return;
        LastConfirmedActionId = canonical;
        LastConfirmedActionAt = now;
        if (IsGcdAction(canonical))
        {
            // A confirmed GCD starts a new PR oGCD budget. The final
            // ninjutsu gets the AE-specific one-oGCD exception below.
            GcdOffGcdUsed = 0;
            PostNinjutsuOffGcdLimit = 0;
            PostNinjutsuOffGcdUsed = 0;
            if (IsFinalNinjutsu(canonical))
                PostNinjutsuOffGcdLimit = 1;
            return;
        }

        // Some clients report an adjusted action ID for an oGCD. Count both
        // the raw packet and its canonical action so the AE-equivalent slot
        // is consumed regardless of which alias the event bridge exposes.
        if (IsOffGcdAction(id) || IsOffGcdAction(canonical))
        {
            GcdOffGcdUsed = Math.Min(GcdOffGcdUsed + 1, MaxGcdOffGcd);
            PostNinjutsuOffGcdUsed = Math.Min(PostNinjutsuOffGcdUsed + 1, PostNinjutsuOffGcdLimit);
        }
    }

    private static bool IsFinalNinjutsu(uint id) => id is NinjaActions.Suiton
        or NinjaActions.Raiton or NinjaActions.TcjSuiton or NinjaActions.HyoshoRanryu;

    private static bool IsGcdAction(uint id) => id is NinjaActions.SpinningEdge
        or NinjaActions.GustSlash or NinjaActions.AeolianEdge or NinjaActions.ArmorCrush
        or NinjaActions.FumaShuriken or NinjaActions.Raiton or NinjaActions.Suiton
        or NinjaActions.Ten or 2260u or 2261u or 2263u
        or 2266u or 2268u or 2269u or 2270u
        or 16491u
        or NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton
        or 18876u or 18878u or 18880u
        or NinjaActions.TenCombo or NinjaActions.ChiCombo or NinjaActions.JinCombo
        or NinjaActions.PhantomKamaitachi or NinjaActions.FleetingRaiju
        or NinjaActions.HyoshoRanryu;

    private static bool IsOffGcdAction(uint id) => id is NinjaActions.Bunshin
        or NinjaActions.Bhavacakra or 36960u or NinjaActions.Mug or NinjaActions.MugCurrent
        or NinjaActions.TrickAttack or NinjaActions.TrickAttackCurrent
        or NinjaActions.TenChiJin or NinjaActions.Kassatsu or NinjaActions.Shukuchi
        or NinjaActions.TenriJindo or NinjaActions.Meisui or NinjaActions.Assassinate
        or NinjaActions.AssassinateCooldown or NinjaActions.TrueNorth;

    private static uint CanonicalActionId(uint id)
    {
        // Some clients report the final TCJ packet as the ordinary Suiton
        // action (2271). Keep that packet distinct from a normal Suiton cast
        // so the AE-equivalent 18881 RecentlyUsed condition can pass.
        if (id == NinjaActions.Suiton
            && (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true
                || PendingTcjJutsu == NinjaActions.TcjSuiton))
            return NinjaActions.TcjSuiton;

        // Base mudra/ninjutsu actions are separate GCD inputs. They must not
        // be canonicalized to a TCJ follow-up through ActionManager's
        // adjusted-action aliases.
        if (id is NinjaActions.FumaShuriken or NinjaActions.Ten or 2260u or 2261u or 2263u
            or NinjaActions.Raiton or NinjaActions.Suiton)
            return id;

        foreach (var action in new[]
        {
            NinjaActions.TenChiJin, NinjaActions.TcjFumaShuriken,
            NinjaActions.TcjRaiton, NinjaActions.TcjSuiton
        })
        {
            if (id == action) return action;
            var adjusted = ActionHelper.GetAdjustedActionId(action);
            if (adjusted != 0 && id == adjusted) return action;
        }
        return id;
    }

    // Depending on the packet source, a transformed TCJ spell can be
        // reported as the underlying hotbar action (2259/2265/2267/2271). Infer the
    // current TCJ step only while the TCJ window is active; outside that
    // window these IDs must remain ordinary mudra/ninjutsu actions.
    private static bool TcjPacketWindowActive()
    {
        if (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true) return true;

        if (PendingTcjJutsu is NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton)
            return PendingTcjAgeMs <= PendingTcjTimeoutMs;

        return LastTcjJutsu is NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton
            && Recently(LastTcjJutsu, 2120);
    }

    private static uint ResolveTcjEventId(uint id)
    {
        if (!ActionTimes.ContainsKey(NinjaActions.TenChiJin)
            || !TcjPacketWindowActive()) return 0;

        // A pending submission identifies the action whose cast/effect packet
        // is expected. Once it is confirmed, infer the next action from the
        // confirmed LastTcjJutsu using the same step progression as the
        // original action sequence.
        var expected = PendingTcjJutsu != 0
            ? PendingTcjJutsu
            : LastTcjJutsu switch
            {
                NinjaActions.TcjFumaShuriken => NinjaActions.TcjRaiton,
                NinjaActions.TcjRaiton => NinjaActions.TcjSuiton,
                _ => NinjaActions.TcjFumaShuriken
            };
        if (id == expected) return expected;

        // Action-effect packets expose the resolved ninjutsu action rather
        // than the transformed TCJ action in some client builds. Keep these
        // aliases explicit; relying only on ActionManager's adjusted ID can
        // return the ordinary mudra action while the 1186 status is active.
        var resolvedBase = expected switch
        {
            NinjaActions.TcjFumaShuriken => NinjaActions.FumaShuriken,
            NinjaActions.TcjRaiton => NinjaActions.Raiton,
            NinjaActions.TcjSuiton => NinjaActions.Suiton,
            _ => 0u
        };
        if (id == resolvedBase
            || (expected == NinjaActions.TcjFumaShuriken && id == NinjaActions.Ten)) return expected;
        if (expected == NinjaActions.TcjFumaShuriken && id is 18874u or 18875u) return expected;

        var adjusted = ActionHelper.GetAdjustedActionId(expected);
        return adjusted != 0 && adjusted != expected && id == adjusted ? expected : 0;
    }

    public static bool IsExpectedTcjActionAlias(uint id) => ResolveTcjEventId(id) != 0;

    public static bool Recently(uint id, int ms) => ActionTimes.TryGetValue(id, out var tick)
        && Environment.TickCount64 - tick <= ms;

    public static uint GetLastTcjStep() => LastTcjJutsu;

    public static void MarkTcjSubmitted(uint id)
    {
        if (id is not (NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton))
            return;
        PendingTcjJutsu = id;
        PendingTcjJutsuAt = Environment.TickCount64;

        // The client does not expose a consistent cast/effect packet for the
        // final TCJ action: depending on the packet path it can be reported
        // as 18881, ordinary Suiton (2271), or only as the transformed action
        // submission. Keep the recency anchor as soon as that slot is
        // submitted. A later combat event still refreshes the timestamp.
        if (id == NinjaActions.TcjSuiton)
            ActionTimes[NinjaActions.TcjSuiton] = PendingTcjJutsuAt;
    }

    public static uint GetTcjStepForDecision()
    {
        if (PendingTcjJutsu != 0)
        {
            if (Environment.TickCount64 - PendingTcjJutsuAt <= PendingTcjTimeoutMs)
                return PendingTcjJutsu;
            PendingTcjJutsu = 0;
            PendingTcjJutsuAt = 0;
        }
        return LastTcjJutsu;
    }

    public static int PendingTcjAgeMs => PendingTcjJutsu == 0
        ? 0
        : (int)Math.Clamp(Environment.TickCount64 - PendingTcjJutsuAt, 0, int.MaxValue);

    // 1186 is the authoritative Ten Chi Jin window. The 7403 action is only
    // the activation event; its timestamp must not keep the TCJ resolver
    // active after the status has been removed by the client. Otherwise an
    // unknown/stale step falls back to 18873 and is retried outside TCJ.
    public static bool IsTcjSequenceActive =>
        Core.Me?.HasStatus(NinjaActions.TcjStatus) == true;
}
