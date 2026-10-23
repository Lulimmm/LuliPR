using System;
using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

namespace Ninja.PR;

internal static class NinjaActions
{
    public const uint SpinningEdge = 2240, GustSlash = 2242, AeolianEdge = 2255, ArmorCrush = 3563;
    public const uint FumaShuriken = 2265, Raiton = 2267, Suiton = 2271, Bunshin = 16493, Bhavacakra = 7402, Mug = 2248;
    public const uint TrickAttack = 2258, MugCurrent = 36957, TrickAttackCurrent = 36958;
    public const uint TenChiJin = 7403, Ten = 2259, Kassatsu = 2264;
    public const uint Shukuchi = 2262;
    public const uint TenriJindo = 36961, PhantomKamaitachi = 25774, FleetingRaiju = 25778;
    public const uint Meisui = 16489, Assassinate = 2246, AssassinateCooldown = 3566, TrueNorth = 7546;
    public const uint HyoshoRanryu = 16492, TenCombo = 18805, ChiCombo = 18806, JinCombo = 18807;
    public const uint TcjFumaShuriken = 18873, TcjRaiton = 18877, TcjSuiton = 18881;
    public const uint TrickDebuff = 3254, KunaisBaneStatus = 3906;
    public const uint RaijuReady = 2690, TcjStatus = 1186;
    public const uint ShadowWalkerStatus = 3848, KassatsuStatus = 497, TenriJindoReady = 3851;
    public const uint PhantomReady = 2723, BunshinStatus = 1954, MugDebuff = 3849;
}

internal static class NinjaQts
{
    public const string BigTrick = "百雷铳", SmallTrick = "介毒之术", TenChiJin = "天地人";
    public const string Bunshin = "分身", Meisui = "命水", Bhavacakra = "六道";
    public const string Suiton = "水遁", Raiton = "雷遁", Phantom = "镰鼬";
    public const string Raiju = "雷牙", TenriJindo = "天理人道";
    public const string AutoTrueNorth = "自动真北", PrepareSuiton = "预备水遁";
}

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
        NinjaAeSlotSequence.Record(canonical);
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
        if (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true
            || Recently(NinjaActions.TenChiJin, 2120)) return true;

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
        // confirmed LastTcjJutsu exactly like AE's LastGcd switch.
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

    // AE's TCJ GCD slot has exclusive ownership of the GCD until Suiton is
    // confirmed. Keep the same ownership in PR even during the short packet
    // gap where the 1186 status is not visible yet.
    public static bool IsTcjSequenceActive =>
        ActionTimes.ContainsKey(NinjaActions.TenChiJin)
        && TcjPacketWindowActive();
}

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

    // AE's GeneralSettings.AttackRange is the normal 3m Ninja action range.
    // Use the PR range helper so the same range correction/hack is applied.
    public static float AttackRange => GameData.GetCurrentAttackRange(3f);
}

internal abstract class NinjaResolver : IDecisionResolver
{
    protected static CheckResult No => new(false, "未满足使用条件");
    protected static CheckResult Ok => new(true, string.Empty);
    protected static bool Ready(uint id) => ActionHelper.IsReady(id);
    protected static bool InMeleeRange => Core.Target != null && Core.Me != null
        && NinjaTarget.Distance <= NinjaTarget.AttackRange;
    protected static bool HasMeleeTarget => Core.Target?.CanUseAttackActionOn() == true && InMeleeRange;
    protected static PAction Build(uint id, ActionType type, ActionTargetType target)
        => new(id, type, target) { RequiresVerification = true };
    public abstract CheckResult Check();
    public abstract PAction GetAction();
}

internal sealed class NinjaAlwaysResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(NinjaQts.PrepareSuiton)) return No;

        // A stale pre-pull flag must never take precedence over the TCJ GCD
        // sequence. AE's TCJ resolver owns every GCD while the 1186 aura is
        // active; otherwise the prep queue (2259/18806/18807/2271) can be
        // selected first and consume the window without submitting 18873.
        if (Core.Me?.HasStatus(NinjaActions.TcjStatus) == true
            || NinjaCombatState.Recently(NinjaActions.TenChiJin, 2120)) return No;

        return Ok;
    }

    public override PAction GetAction()
    {
        NinjaAeSlotSequence.Begin(NinjaActions.Suiton);
        if (ActionHelper.GetAdjustedActionId(2260u) == NinjaActions.Suiton)
            return Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}

internal sealed class NinjaJutsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true) return No;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Suiton) || !PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)) return No;
        if (me.HasStatus(NinjaActions.RaijuReady) || me.HasStatus(NinjaActions.TcjStatus)) return No;
        if (ActionHelper.GetActionCharges(NinjaActions.Ten) < 1 || NinjaCombatState.Recently(NinjaActions.Ten, 2120)) return No;
        if (me.HasStatus(NinjaActions.KassatsuStatus) || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return No;
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus) || NinjaCombatState.Recently(NinjaActions.Suiton, 15000)) return No;
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 19.5f) return No;
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) > 15f) return No;

        var distance = NinjaTarget.Distance;
        var meleeRange = NinjaTarget.AttackRange;
        if (distance > meleeRange + 17f) return No;
        return distance > meleeRange || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 15f
            || ActionHelper.GetActionCooldown(NinjaActions.Ten) <= 5f ? Ok : No;
    }

    public override PAction GetAction()
    {
        NinjaAeSlotSequence.Begin(NinjaActions.Suiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Suiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}

internal sealed class NinjaTcjResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        if (Core.Target?.CanUseAttackActionOn() != true
            || (Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
                && !NinjaCombatState.Recently(NinjaActions.TenChiJin, 2120)))
            return No;

        // Decide from the jutsu cast after Ten Chi Jin. Once Suiton has been
        // confirmed, the three-jutsu sequence is complete even if the status
        // removal packet arrives a frame later.
        return GetNextTcjAction() == 0 ? No : Ok;
    }
    public override PAction GetAction()
    {
        var id = GetNextTcjAction();
        if (id == 0) id = NinjaActions.TcjSuiton;
        NinjaCombatState.MarkTcjSubmitted(id);
        NinjaAeSlotSequence.Begin(NinjaActions.TcjSuiton);
        // AE's TCJAction submits the concrete follow-up spell directly. These
        // IDs are already the transformed actions, so resolving them again
        // through ActionManager can collapse the sequence back to Fuma.
        return Build(id, ActionType.Gcd, ActionTargetType.Target);
    }

    private static uint GetNextTcjAction()
    {
        return NinjaCombatState.GetTcjStepForDecision() switch
        {
            NinjaActions.TcjFumaShuriken => NinjaActions.TcjRaiton,
            NinjaActions.TcjRaiton => NinjaActions.TcjSuiton,
            NinjaActions.TcjSuiton => 0,
            _ => NinjaActions.TcjFumaShuriken
        };
    }
}

internal sealed class NinjaKassatsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt("生杀予夺")
            || ActionHelper.GetGcdRemain() < 0.6f
            || me.HasStatus(NinjaActions.TcjStatus)
            || !Ready(NinjaActions.Kassatsu)) return No;

        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        if (NinjaSettings.Instance.RaitonBackstab
            && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        return Ok;
    }

    public override PAction GetAction() =>
        Build(ActionHelper.GetAdjustedActionId(NinjaActions.Kassatsu), ActionType.OffGcd, ActionTargetType.Self);
}

internal sealed class NinjaTenChiJinResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.TenChiJin)
            || !Ready(NinjaActions.TenChiJin)
            || me.HasStatus(NinjaActions.KassatsuStatus)
            || me.HasStatus(NinjaActions.ShadowWalkerStatus)
            || ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 2.12f
            || ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f
            || !target.HasStatus(3849u)
            || ActionHelper.GetActionCooldown(NinjaActions.Ten) < 4f) return No;
        return Ok;
    }

    public override PAction GetAction() =>
        Build(NinjaActions.TenChiJin, ActionType.OffGcd, ActionTargetType.Self);
}

internal sealed class NinjaKassatsuJutsuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.KassatsuStatus) != true
            || NinjaCombatState.IsTcjSequenceActive
            || target?.CanUseAttackActionOn() != true) return No;
        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)
            || NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 2120)
            || me.GetStatusLeftTime(NinjaActions.KassatsuStatus) < 3f) return Ok;
        return No;
    }

    public override PAction GetAction()
    {
        NinjaAeSlotSequence.Begin(NinjaActions.HyoshoRanryu);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.TenCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.JinCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.HyoshoRanryu, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}

internal sealed class NinjaPhantomResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me?.HasStatus(NinjaActions.PhantomReady) != true || NinjaCombatState.IsTcjSequenceActive
            || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.Phantom) || JobGaugeHelper.NIN.Ninki > 90) return No;

        var distance = NinjaTarget.Distance;
        if (distance > NinjaTarget.AttackRange) return Ok;
        if (NinjaSettings.Instance.PhantomOnlyForRange) return No;
        if (me.HasStatus(NinjaActions.BunshinStatus) && target.HasStatus(NinjaActions.KunaisBaneStatus)) return No;
        if (PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        if (NinjaSettings.Instance.RaitonBackstab)
            return target.HasStatus(NinjaActions.MugDebuff) && !target.HasStatus(NinjaActions.KunaisBaneStatus)
                && NinjaCombatState.Recently(NinjaActions.Raiton, 2120) ? Ok : No;
        if (target.HasStatus(NinjaActions.MugDebuff) && !target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus) && ActionHelper.GetActionCharges(NinjaActions.Ten) < .9f) return Ok;
        if (NinjaCombatState.Recently(NinjaActions.Bunshin, 20000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 18000)
            && !NinjaCombatState.Recently(NinjaActions.MugCurrent, 20000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1.7f) return Ok;
        return NinjaCombatState.Recently(NinjaActions.Bunshin, 22000)
            && NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 22000)
            && NinjaCombatState.Recently(NinjaActions.MugCurrent, 24000)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) < 1f ? Ok : No;
    }

    public override PAction GetAction() => Build(NinjaActions.PhantomKamaitachi, ActionType.Gcd, ActionTargetType.Target);
}

internal sealed class NinjaRaijuResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Raiju) || me == null
            || target?.CanUseAttackActionOn() != true || NinjaTarget.Distance > NinjaTarget.AttackRange
            || me.HasStatus(NinjaActions.TcjStatus)) return No;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.Ten) < 2.5f) return No;
        return me.HasStatus(NinjaActions.RaijuReady) ? Ok : No;
    }

    public override PAction GetAction() => Build(NinjaActions.FleetingRaiju, ActionType.Gcd, ActionTargetType.Target);
}

internal sealed class NinjaRaitonResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target?.CanUseAttackActionOn() != true
            || !PromeSettings.Instance.GetQt(NinjaQts.Raiton)
            || me.HasStatus(NinjaActions.TcjStatus)
            || ActionHelper.GetActionCharges(NinjaActions.Ten) < 1
            || NinjaCombatState.Recently(NinjaActions.Ten, 2120)
            || me.HasStatus(NinjaActions.KassatsuStatus)
            || NinjaCombatState.Recently(NinjaActions.Kassatsu, 2120)) return No;

        if (NinjaSettings.Instance.ForceRaiton || PromeSettings.Instance.GetQt("倾斜资源")) return Ok;
        // Match the AE Raiton gate: Raiton is held until the next
        // small-trick window is available, including while Shadow Walker is
        // still active. Suiton and Raiton are separate jutsu decisions.
        // AE only applies this hold while Shadow Walker is absent. During the
        // Shadow Walker window Raiton is still allowed to satisfy the later
        // execution conditions (for example a forced or ranged cast).
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 19.5f
            && !me.HasStatus(NinjaActions.ShadowWalkerStatus)) return No;
        if (NinjaSettings.Instance.RaitonWhenOutOfRange && NinjaTarget.Distance > NinjaTarget.AttackRange) return Ok;
        if (PromeSettings.Instance.GetQt("斩杀") && target.MaxHp > 0
            && target.CurrentHp * 100f / target.MaxHp < NinjaSettings.Instance.TTK) return Ok;
        if (target.HasStatus(NinjaActions.KunaisBaneStatus)) return Ok;
        if (NinjaSettings.Instance.RaitonBackstab && target.HasStatus(NinjaActions.MugDebuff)
            && ActionHelper.GetActionCharges(NinjaActions.Ten) > 1f)
        {
            if (JobGaugeHelper.NIN.Ninki == 100 && me.HasStatus(NinjaActions.PhantomReady)) return Ok;
            if (me.HasStatus(NinjaActions.PhantomReady)
                || ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.5f
                || ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 2.5f) return Ok;
        }
        return No;
    }

    public override PAction GetAction()
    {
        NinjaSettings.Instance.ForceRaiton = false;
        NinjaSettings.Instance.RaitonWhenOutOfRange = false;
        NinjaAeSlotSequence.Begin(NinjaActions.Raiton);
        ActionQueueManager.Enqueue(new List<PAction>
        {
            Build(NinjaActions.Ten, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.ChiCombo, ActionType.Gcd, ActionTargetType.Self),
            Build(NinjaActions.Raiton, ActionType.Gcd, ActionTargetType.Target)
        });
        return null!;
    }
}

internal sealed class NinjaEarlier4Or3Resolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        return NinjaSettings.Instance.RaitonBackstab
            && target?.CanUseAttackActionOn() == true
            && NinjaTarget.Distance <= NinjaTarget.AttackRange
            && me?.HasStatus(NinjaActions.TcjStatus) != true
            && target.HasStatus(NinjaActions.KunaisBaneStatus)
            && ActionHelper.GetLastComboID() == NinjaActions.GustSlash
            && NinjaCombatState.Recently(NinjaActions.TenChiJin, 8000)
            && NinjaCombatState.Recently(NinjaActions.FleetingRaiju, 2120) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var action = JobGaugeHelper.NIN.Kazematoi == 0
            ? NinjaActions.ArmorCrush
            : NinjaActions.AeolianEdge;
        return Build(ActionHelper.GetAdjustedActionId(action), ActionType.Gcd, ActionTargetType.Target);
    }
}

internal sealed class NinjaKBaseResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!NinjaSettings.Instance.RaitonBackstab
            || me == null || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || me.HasStatus(NinjaActions.TcjStatus)) return No;

        return (target.HasStatus(NinjaActions.KunaisBaneStatus)
                || NinjaCombatState.Recently(NinjaActions.TrickAttackCurrent, 1060))
            && ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f
            && !target.HasStatus(NinjaActions.MugDebuff) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var last = ActionHelper.GetLastComboID();
        var me = Core.Me!;
        var target = Core.Target!;
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        var positional = TargetHelper.GetTargetPositional();
        uint action;

        if (last == NinjaActions.GustSlash && !TargetHelper.HasPositionalRequirement(target))
            action = kazematoi == 0 ? NinjaActions.ArmorCrush : NinjaActions.AeolianEdge;
        else if (last == NinjaActions.GustSlash)
        {
            if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return Build(ActionHelper.GetAdjustedActionId(action), ActionType.Gcd, ActionTargetType.Target);
    }
}

internal sealed class NinjaTenriJindoResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        if (!PromeSettings.Instance.GetQt(NinjaQts.TenriJindo)) return No;
        // Ten Chi Jin owns the action window. Do not allow a previously
        // available 36961 to be selected while the local 1186 buff is up,
        // including the short packet gap before IsTcjSequenceActive updates.
        if (me == null || me.HasStatus(NinjaActions.TcjStatus)
            || NinjaCombatState.IsTcjSequenceActive) return No;
        if (ActionHelper.GetGcdRemain() < 0.6f) return No;
        if (!ActionHelper.IsUnlocked(NinjaActions.TenriJindo)
            || !Ready(NinjaActions.TenriJindo)) return No;
        if (NinjaCombatState.Recently(NinjaActions.TenChiJin, 2120)) return No;
        return me.HasStatus(NinjaActions.TenriJindoReady) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.TenriJindo);
        return Build(adjusted == 0 ? NinjaActions.TenriJindo : adjusted,
            ActionType.OffGcd, ActionTargetType.Target);
    }
}

internal static class NinjaBhavacakraPolicy
{
    public static bool IsEligible()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra) || me == null
            || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || me.HasStatus(NinjaActions.TcjStatus) || JobGaugeHelper.NIN.Ninki < 50
            || NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960, 1060)
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)
            || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
                && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) && me.HasStatus(NinjaActions.ShadowWalkerStatus)))
            return false;

        return true;
    }

    public static bool ShouldForceSpend(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        if (!IsEligible()) return false;
        var me = Core.Me!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return true;

        if (ActionHelper.GetActionCooldown(NinjaActions.Bunshin) < 20f
            && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (me.HasStatus(NinjaActions.BunshinStatus)) return false;
            var comboFinisher = ActionHelper.GetLastComboID() == NinjaActions.GustSlash;
            return (comboIsFirst() && ((comboFinisher && ninki > 85) || ninki > 95))
                || (raijuIsFirst() && ninki > 95);
        }

        if (ninki == 100) return true;
        return !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && ninki > 60;
    }
}

internal sealed class NinjaPriorityBhavacakraResolver : NinjaResolver
{
    // AE's first oGCD resolver is _100B. It is a strict overflow/spend
    // guard and is intentionally separate from the later Bhavacakra resolver.
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.Bhavacakra)
            || me == null || target?.CanUseAttackActionOn() != true
            || NinjaTarget.Distance > NinjaTarget.AttackRange
            || NinjaCombatState.Recently(NinjaActions.Bhavacakra, 1060)
            || NinjaCombatState.Recently(36960u, 1060)
            || JobGaugeHelper.NIN.Ninki < 50
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1060)
            || NinjaCombatState.IsTcjSequenceActive
            || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 1.26f
                && PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && me.HasStatus(NinjaActions.ShadowWalkerStatus))) return No;

        if (me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) > 2.12f
            && PromeSettings.Instance.GetQt(NinjaQts.Meisui)
            && ActionHelper.GetActionCooldown(NinjaActions.Meisui) < 2.12f) return Ok;
        if (JobGaugeHelper.NIN.Ninki == 100) return Ok;
        return !NinjaSettings.Instance.P5PerfectBunshin
            && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 1.06f
            && JobGaugeHelper.NIN.Ninki > 60 ? Ok : No;
    }

    public override PAction GetAction() =>
        Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
}

internal sealed class NinjaOgcdResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;
    private readonly Func<bool> raijuIsFirst;

    public NinjaOgcdResolver(Func<bool> comboIsFirst, Func<bool> raijuIsFirst)
    {
        this.comboIsFirst = comboIsFirst;
        this.raijuIsFirst = raijuIsFirst;
    }

    private bool CanUseBhavacakra()
    {
        if (!NinjaBhavacakraPolicy.IsEligible() || ActionHelper.GetGcdRemain() < .6f)
            return false;

        var me = Core.Me!;
        var target = Core.Target!;
        var ninki = JobGaugeHelper.NIN.Ninki;
        var lastCombo = ActionHelper.GetLastComboID();
        var comboFinisher = lastCombo == NinjaActions.GustSlash;
        var bunshinCooldown = ActionHelper.GetActionCooldown(NinjaActions.Bunshin);
        var bunshinActive = me.HasStatus(NinjaActions.BunshinStatus);
        if (bunshinCooldown < 20f && PromeSettings.Instance.GetQt(NinjaQts.Bunshin))
        {
            if (!bunshinActive)
                return (comboIsFirst() && comboFinisher && ninki > 85)
                    || (comboIsFirst() && ninki > 95)
                    || (raijuIsFirst() && ninki > 95);
            return false;
        }
        if (bunshinActive && ((comboFinisher && ninki > 80) || (comboIsFirst() && ninki > 90))) return true;
        if ((comboFinisher && ninki > 85) || (comboIsFirst() && ninki > 95)) return true;
        if (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f
            || ActionHelper.GetActionCooldown(NinjaActions.Kassatsu) < 2.12f) return false;
        return target.HasStatus(NinjaActions.KunaisBaneStatus);
    }

    public override CheckResult Check()
    {
        var me = Core.Me;
        if (me == null || Core.Target == null || ActionHelper.GetGcdRemain() < .6f
            || me.HasStatus(NinjaActions.TcjStatus)) return No;
        return CanUseBhavacakra() ? Ok : No;
    }

    public override PAction GetAction()
    {
        if (CanUseBhavacakra())
        {
            var adjusted = ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra);
            return Build(adjusted, ActionType.OffGcd, ActionTargetType.Target);
        }
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bhavacakra), ActionType.OffGcd, ActionTargetType.Target);
    }
}

internal sealed class NinjaMugResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt(NinjaQts.BigTrick)
        && Core.Target?.CanUseAttackActionOn() == true && InMeleeRange
        && ActionHelper.GetGcdRemain() >= .6f && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
        && Ready(NinjaActions.MugCurrent) ? Ok : No;

    public override PAction GetAction()
    {
        if (NinjaSettings.Instance.P5PerfectBunshin && JobGaugeHelper.NIN.Ninki >= 50)
        {
            ActionQueueManager.EnqueueOffGcdList(new List<PAction>
            {
                Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self),
                Build(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target)
            });
            return null!;
        }
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.MugCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}

internal sealed class NinjaTrickAttackResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) || me?.HasStatus(NinjaActions.ShadowWalkerStatus) != true
            || target?.CanUseAttackActionOn() != true || !InMeleeRange || !Ready(NinjaActions.TrickAttackCurrent)
            || (ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) <= 0f && PromeSettings.Instance.GetQt(NinjaQts.BigTrick))
            || NinjaCombatState.Recently(NinjaActions.MugCurrent, 1360)) return No;

        var elapsed = ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain();
        if (NinjaSettings.Instance.TrickAttackDelayUntilNextMug && !target.HasStatus(NinjaActions.MugDebuff)) return No;
        // AE's parameterless RecentlyUsed() uses a 1 second window.
        if (NinjaCombatState.Recently(NinjaActions.TcjSuiton, 1000) && elapsed < .7f) return No;
        if (NinjaCombatState.Recently(NinjaActions.Raiton, 1500) && elapsed < .7f) return No;
        if (ActionHelper.GetActionCooldown(NinjaActions.TenChiJin) < 10f && elapsed < 1.45f) return No;
        return Ok;
    }

    public override PAction GetAction()
    {
        NinjaSettings.Instance.TrickAttackDelayUntilNextMug = false;
        return Build(ActionHelper.GetAdjustedActionId(NinjaActions.TrickAttackCurrent), ActionType.OffGcd, ActionTargetType.Target);
    }
}

internal sealed class NinjaBunshinResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt(NinjaQts.Bunshin)
        && ActionHelper.GetGcdRemain() >= .3f && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
        && Ready(NinjaActions.Bunshin) && JobGaugeHelper.NIN.Ninki >= 50
        && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) >= 1.06f ? Ok : No;

    public override PAction GetAction() => Build(ActionHelper.GetAdjustedActionId(NinjaActions.Bunshin), ActionType.OffGcd, ActionTargetType.Self);
}

internal sealed class NinjaMeisuiResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        // AE's direct Meisui resolver does not gate on the Meisui QT. The QT
        // is only part of the _100B overflow-priority branch above, so keep
        // that distinction when migrating the 16489 resolver.
        if (me == null || !Ready(NinjaActions.Meisui)
            || !ActionHelper.IsUnlocked(NinjaActions.Meisui)
            || ActionHelper.GetGcdRemain() < .6f
            || me.HasStatus(NinjaActions.TcjStatus)) return No;

        if (JobGaugeHelper.NIN.Ninki < 50
            && me.HasStatus(NinjaActions.ShadowWalkerStatus)
            && NinjaCombatState.Recently(NinjaActions.TcjSuiton, 15000)
            && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) >= 20f) return Ok;

        return No;
    }

    public override PAction GetAction() => Build(NinjaActions.Meisui, ActionType.OffGcd, ActionTargetType.Self);
}

internal sealed class NinjaAssassinateResolver : NinjaResolver
{
    public override CheckResult Check() => PromeSettings.Instance.GetQt("梦幻三段")
        && Core.Target?.CanUseAttackActionOn() == true && InMeleeRange
        && ActionHelper.GetGcdRemain() >= .6f && Core.Me?.HasStatus(NinjaActions.TcjStatus) != true
        && Ready(NinjaActions.AssassinateCooldown)
        && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) >= 2.12f
        && ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) >= 2.12f
        && !NinjaCombatState.Recently(NinjaActions.TenChiJin, 2120) ? Ok : No;

    public override PAction GetAction() => Build(ActionHelper.GetAdjustedActionId(NinjaActions.Assassinate), ActionType.OffGcd, ActionTargetType.Target);
}

internal sealed class NinjaTrueNorthResolver : NinjaResolver
{
    private readonly Func<bool> comboIsFirst;

    public NinjaTrueNorthResolver(Func<bool> comboIsFirst) => this.comboIsFirst = comboIsFirst;

    public override CheckResult Check()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (!PromeSettings.Instance.GetQt(NinjaQts.AutoTrueNorth) || me == null
            || target?.CanUseAttackActionOn() != true
            || NinjaCombatState.Recently(NinjaActions.TrueNorth, 2120)
            || me.HasStatus(1250u)
            || ActionHelper.GetActionCharges(NinjaActions.TrueNorth) < 1
            || ActionHelper.GetGcdRemain() < .6f
            || ActionHelper.GetGcdTotal() - ActionHelper.GetGcdRemain() <= ActionHelper.GetGcdTotal() * .625f
            || me.HasStatus(NinjaActions.TcjStatus)
            || (PromeSettings.Instance.GetQt(NinjaQts.SmallTrick)
                && ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 2.12f)
            || !TargetHelper.HasPositionalRequirement(target)
            || !comboIsFirst()) return No;

        var last = ActionHelper.GetLastComboID();
        if (last != NinjaActions.GustSlash) return No;
        var positional = TargetHelper.GetTargetPositional();
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        if (kazematoi == 0 && positional != Positional.Flank) return Ok;
        if (kazematoi > 3 && positional != Positional.Rear) return Ok;
        if (positional != Positional.Flank && positional != Positional.Rear) return Ok;
        if (((ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f && kazematoi <= 1)
                || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f && kazematoi <= 1))
            && positional != Positional.Flank) return Ok;
        return No;
    }

    public override PAction GetAction() => Build(NinjaActions.TrueNorth, ActionType.OffGcd, ActionTargetType.Self);
}

internal sealed class NinjaComboResolver : NinjaResolver
{
    public override CheckResult Check()
    {
        var me = Core.Me;
        return HasMeleeTarget && me != null && !me.HasStatus(NinjaActions.RaijuReady)
            && !me.HasStatus(NinjaActions.TcjStatus) ? Ok : No;
    }

    public override PAction GetAction()
    {
        var last = ActionHelper.GetLastComboID();
        var me = Core.Me!;
        var target = Core.Target!;
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        var positional = TargetHelper.GetTargetPositional();
        uint action;

        if (last == NinjaActions.GustSlash && !TargetHelper.HasPositionalRequirement(target))
            action = kazematoi == 0 ? NinjaActions.ArmorCrush : NinjaActions.AeolianEdge;
        else if (last == NinjaActions.GustSlash && me.Level >= 26)
        {
            if (me.Level < 54) action = NinjaActions.AeolianEdge;
            else if ((PromeSettings.Instance.GetQt(NinjaQts.SmallTrick) || PromeSettings.Instance.GetQt(NinjaQts.BigTrick))
                && ((ActionHelper.GetActionCooldown(NinjaActions.MugCurrent) < 8f && kazematoi <= 1)
                    || (ActionHelper.GetActionCooldown(NinjaActions.TrickAttackCurrent) < 8f && kazematoi <= 1))) action = NinjaActions.ArmorCrush;
            else if (target.HasStatus(NinjaActions.KunaisBaneStatus) && kazematoi != 0) action = NinjaActions.AeolianEdge;
            else if (kazematoi == 0 || (kazematoi <= 3 && positional == Positional.Flank)) action = NinjaActions.ArmorCrush;
            else action = NinjaActions.AeolianEdge;
        }
        else action = last == NinjaActions.SpinningEdge && me.Level >= 4 ? NinjaActions.GustSlash : NinjaActions.SpinningEdge;

        return Build(ActionHelper.GetAdjustedActionId(action), ActionType.Gcd, ActionTargetType.Target);
    }
}
