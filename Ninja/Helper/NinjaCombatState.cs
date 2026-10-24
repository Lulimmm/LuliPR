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
    private const int PendingTcjTimeoutMs = 2500;

    public static void Reset()
    {
        ActionTimes.Clear();
        LastGcd = 0;
        LastTcjJutsu = 0;
        PendingTcjJutsu = 0;
        PendingTcjJutsuAt = 0;
        LastObservedActionId = 0;
        LastObservedActionAt = 0;
    }

    public static void Record(uint id)
    {
        var now = Environment.TickCount64;
        LastObservedActionId = id;
        LastObservedActionAt = now;
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
        else if (canonical is NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton)
        {
            LastTcjJutsu = canonical;
            // The client can report the transformed action through a
            // different resolved alias. Any recognized TCJ jutsu therefore
            // confirms the pending submission, regardless of raw ID.
            PendingTcjJutsu = 0;
            PendingTcjJutsuAt = 0;
        }
        if (canonical is NinjaActions.TcjFumaShuriken or NinjaActions.TcjRaiton or NinjaActions.TcjSuiton
            or NinjaActions.SpinningEdge or NinjaActions.GustSlash or NinjaActions.AeolianEdge or NinjaActions.ArmorCrush
            or NinjaActions.Suiton or NinjaActions.Raiton or NinjaActions.PhantomKamaitachi or NinjaActions.FleetingRaiju
            or NinjaActions.HyoshoRanryu)
            LastGcd = canonical;
    }

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

    // 1186 is the authoritative Ten Chi Jin window. The action timestamp is
    // retained for packet decoding only; it must not keep TCJ ownership alive
    // after the client removes the buff.
    public static bool IsTcjSequenceActive =>
        Core.Me?.HasStatus(NinjaActions.TcjStatus) == true;
}
