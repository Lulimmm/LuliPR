$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
# Compile actual resolver and budget sources against an isolated host. No game
# input is sent; queue entries are captured to verify selected action sequences.
$files = @(
    'Ninja/Data/NinjaActions.cs', 'Ninja/Data/NinjaQts.cs',
    'Ninja/Helper/NinjaBurstDelayPolicy.cs', 'Ninja/Helper/NinjaCombatState.cs',
    'Ninja/Action/NinjaResolver.cs',
    'Ninja/Action/Gcd/NinjaJutsuResolver.cs', 'Ninja/Action/Gcd/NinjaRaitonResolver.cs',
    'Ninja/Action/Gcd/NinjaRaijuResolver.cs', 'Ninja/Action/Gcd/NinjaComboResolver.cs',
    'Ninja/Action/OffGcd/NinjaMugResolver.cs', 'Ninja/Action/OffGcd/NinjaTrickAttackResolver.cs',
    'Ninja/Action/OffGcd/NinjaPotionResolver.cs',
    'Ninja/Action/OffGcd/NinjaTenChiJinResolver.cs', 'Ninja/Action/OffGcd/NinjaAssassinateResolver.cs',
    'Ninja/Action/OffGcd/NinjaBunshinResolver.cs', 'Ninja/Action/OffGcd/NinjaKassatsuResolver.cs'
)
$sources = foreach ($file in $files) {
    $text = Get-Content -LiteralPath (Join-Path $root $file) -Raw -Encoding UTF8
    $text = [regex]::Replace($text, '(?m)^namespace ([\w.]+);', 'namespace $1 {')
    "namespace IsolatedCompilation {`n$text`n}}"
}
$fixture = @'
namespace IsolatedCompilation
{
    using System;
    using System.Collections.Generic;
    using Dalamud.Game.ClientState.Objects.Types;
    using PromeRotation.Core;
    using PromeRotation.Data;
    using PromeRotation.Helpers;
    using PromeRotation.Managers;
    using PromeRotation.Resolvers;
    using Ninja.PR.Action.Gcd;
    using Ninja.PR.Action.OffGcd;
    using Ninja.PR.Data;
    using Ninja.PR.Helper;

    namespace Dalamud.Game.ClientState.Objects.Types
    {
        public sealed class IBattleChara
        {
            public byte Level = 100;
            public bool Attackable = true;
            public uint CurrentHp = 100, MaxHp = 100;
            public readonly HashSet<uint> Statuses = new();
            public bool HasStatus(uint id) => Statuses.Contains(id);
            public float GetStatusLeftTime(uint id) => 10;
            public bool CanUseAttackActionOn() => Attackable;
        }
    }
    namespace PromeRotation.Extensions { public static class Placeholder { } }
    namespace PromeRotation.Core
    {
        public static class Core { public static IBattleChara? Me = new(); public static IBattleChara? Target = new(); }
        public static class GameData { public static uint PotionId = 145995; public static uint GetBestPotionId() => PotionId; }
    }
    namespace PromeRotation.Data
    {
        public enum ActionType { Gcd, OffGcd, Item }
        public enum ActionTargetType { Self, Target }
        public sealed class PAction(uint id, ActionType type, ActionTargetType target)
        {
            public uint ActionId = id;
            public ActionType Type = type;
            public ActionTargetType Target = target;
            public bool RequiresVerification { get; set; }
        }
        public sealed class Hacks { public int MaxOgcdsPerGcd = 2; }
        public sealed class PromeSettings
        {
            public static readonly PromeSettings Instance = new();
            public readonly Dictionary<string, bool> Qts = new();
            public readonly Hacks Hacks = new();
            public bool GetQt(string key) => Qts.TryGetValue(key, out var enabled) && enabled;
        }
    }
    namespace PromeRotation.Helpers
    {
        public enum Positional { Front, Rear, Flank }
        public static class ActionHelper
        {
            public static float Charges = 2, GcdRemain = 1.5f;
            public static bool ItemOnCooldown;
            public static readonly Dictionary<uint, float> Cooldowns = new();
            public static readonly HashSet<uint> Locked = new();
            public static uint GetAdjustedActionId(uint id) => id;
            public static bool IsReady(uint id) => IsUnlocked(id) && GetActionCooldown(id) <= 0;
            public static bool IsUnlocked(uint id) => !Locked.Contains(id);
            public static float GetActionCooldown(uint id) => Cooldowns.TryGetValue(id, out var value) ? value : 0;
            public static bool IsItemOnCooldown(uint id) => ItemOnCooldown;
            public static float GetActionCharges(uint id) => Charges;
            public static float GetGcdRemain() => GcdRemain;
            public static float GetGcdTotal() => 2.13f;
            public static uint GetLastComboID() => 0;
        }
        public sealed class Gauge { public int Ninki = 100; public byte Kazematoi = 3; }
        public static class JobGaugeHelper { public static readonly Gauge NIN = new(); }
        public static class TargetHelper
        {
            public static Positional GetTargetPositional() => Positional.Flank;
            public static bool HasPositionalRequirement(IBattleChara target) => true;
        }
    }
    namespace PromeRotation.Resolvers
    {
        public record CheckResult(bool Success, string Message);
        public interface IDecisionResolver { CheckResult Check(); PAction GetAction(); }
    }
    namespace PromeRotation.Managers
    {
        public static class ActionQueueManager
        {
            public static readonly List<List<PAction>> Groups = new();
            public static void Enqueue(List<PAction> actions, bool isHighPriority = false) => Groups.Add(actions);
            public static void EnqueueOffGcdList(List<PAction> actions, bool isHighPriority = false) => Groups.Add(actions);
        }
    }
    namespace PromeRotation.UI.HotKey { public interface IHotkeyLogic { bool IsActive(); void OnClick(); } }
    namespace Ninja.PR
    {
        public static class NinjaEventHandler { public static bool CanQueueMultipleOffGcd => true; }
    }
    namespace Ninja.PR.Helper
    {
        public static class NinjaTarget { public static float Distance = 3; public static float AttackRange => 3; }
        public static class NinjaMudraDebug { public static void Reset() { } public static void Record(uint id) { } }
        public static class NinjaAttemptDebug { public static void Reset() { } }
        public static class NinjaSequencePolicy
        {
            public static bool IsActive;
            public static bool IsNormalMudraActive => IsActive;
            public static void Begin(uint terminal) => IsActive = true;
            public static void Record(uint id) { }
        }
    }
    namespace Ninja.PR.Data
    {
        public sealed class NinjaSettings
        {
            public static readonly NinjaSettings Instance = new();
            public bool RaitonBackstab = true, ForceRaiton, P5PerfectBunshin;
            public bool TrickAttackDelayUntilNextMug, LockTrickAttackIntoMug;
            public int TTK = 20;
        }
    }

    public static class BurstDelayChecks
    {
        private static int count;
        private static void Assert(bool value, string reason)
        {
            if (!value) throw new Exception(reason);
            count++;
        }
        private static void Reset(bool delay)
        {
            Core.Me = new(); Core.Target = new();
            NinjaSequencePolicy.IsActive = false; NinjaCombatState.Reset();
            ActionHelper.Charges = 2; ActionHelper.GcdRemain = 1.5f;
            GameData.PotionId = 145995; ActionHelper.ItemOnCooldown = false;
            ActionHelper.Cooldowns.Clear(); ActionHelper.Locked.Clear();
            NinjaTarget.Distance = 3; ActionQueueManager.Groups.Clear();
            PromeSettings.Instance.Qts.Clear();
            foreach (var key in new[] { NinjaQts.Suiton, NinjaQts.Raiton, NinjaQts.Raiju, NinjaQts.BigTrick,
                NinjaQts.SmallTrick, NinjaQts.Bunshin, NinjaQts.TenChiJin, NinjaQts.Assassinate, "生杀予夺" })
                PromeSettings.Instance.Qts[key] = true;
            PromeSettings.Instance.Qts[NinjaQts.DelayBurst] = delay;
            NinjaSettings.Instance.ForceRaiton = false;
            NinjaSettings.Instance.P5PerfectBunshin = false;
        }
        public static int Run()
        {
            Reset(true);
            foreach (var resolver in new IDecisionResolver[] { new NinjaJutsuResolver(), new NinjaMugResolver(),
                new NinjaTrickAttackResolver(), new NinjaTenChiJinResolver(), new NinjaAssassinateResolver(),
                new NinjaBunshinResolver(), new NinjaKassatsuResolver() })
            {
                var result = resolver.Check();
                Assert(!result.Success && result.Message.Contains("延后爆发"), resolver.GetType().Name + " must hold burst");
            }
            Assert(ActionQueueManager.Groups.Count == 0, "Checking delayed burst must not enqueue actions");

            var raiton = new NinjaRaitonResolver();
            var raiju = new NinjaRaijuResolver();
            foreach (var charges in new[] { 0f, .99f, 1f, 1.5f, 1.799f, 1.8f, 1.9f, 2f })
            {
                Reset(true); ActionHelper.Charges = charges;
                NinjaSettings.Instance.ForceRaiton = true;
                PromeSettings.Instance.Qts["倾斜资源"] = true;
                PromeSettings.Instance.Qts[NinjaQts.Raiton] = false;
                Core.Target!.Statuses.Add(NinjaActions.KunaisBaneStatus);
                Assert(raiton.Check().Success == (charges >= 1.8f), "Delay overflow boundary: " + charges);
            }
            Reset(true); ActionHelper.Charges = 1.8f;
            Assert(raiton.Check().Success, "Overflow Raiton must bypass imminent Trick/Suiton hold");
            raiton.GetAction();
            var group = ActionQueueManager.Groups[0];
            Assert(group.Count == 3 && group[0].ActionId == NinjaActions.Ten
                && group[1].ActionId == NinjaActions.ChiCombo && group[2].ActionId == NinjaActions.Raiton,
                "Overflow must queue Ten-Chi-Raiton only");
            Assert(NinjaCombatState.RecordConfirmed(NinjaActions.Raiton), "Confirm overflow ninjutsu");
            Assert(NinjaCombatState.PostNinjutsuOffGcdLimit == 1, "Overflow Raiton retains the existing single-weave budget");

            Reset(true); Core.Me!.Statuses.Add(NinjaActions.RaijuReady);
            Core.Target!.Statuses.Add(NinjaActions.KunaisBaneStatus);
            PromeSettings.Instance.Qts[NinjaQts.Raiju] = false;
            Assert(NinjaCombatState.HasRaijuReadyForRotation, "Delay mode must preserve the proc even when its normal QT is off");
            Assert(raiju.Check().Success, "Proc must bypass burst-window mudra hold");
            Assert(raiju.GetAction().ActionId == NinjaActions.FleetingRaiju, "Use the existing Raiju action");
            Assert(!new NinjaComboResolver().Check().Success, "Basic combo must not consume a pending Raiju proc");
            Core.Me.Statuses.Remove(NinjaActions.RaijuReady);
            Assert(new NinjaComboResolver().Check().Success, "No proc: basic combo must continue while burst is delayed");

            Reset(true); Core.Target = null;
            Assert(!raiton.Check().Success && !raiju.Check().Success, "No target must still block attacks");
            Reset(true); Core.Me!.Statuses.Add(NinjaActions.MudraStatus); NinjaSequencePolicy.IsActive = true;
            Assert(!raiton.Check().Success, "Do not start another sequence while mudra is active");
            Reset(true); Core.Me!.Statuses.Add(NinjaActions.KassatsuStatus);
            Assert(!raiton.Check().Success, "Do not turn an existing Kassatsu into overflow Raiton");
            Reset(true); Core.Me!.Statuses.Add(NinjaActions.TcjStatus);
            Assert(!raiton.Check().Success && !raiju.Check().Success, "Respect an already active TCJ sequence");
            Reset(true); Core.Me!.Statuses.Add(NinjaActions.RaijuReady); NinjaTarget.Distance = 4;
            Assert(!raiju.Check().Success, "Raiju must retain melee range checks");

            Reset(false); Core.Me!.Statuses.Add(NinjaActions.RaijuReady);
            Core.Target!.Statuses.Add(NinjaActions.KunaisBaneStatus);
            Assert(!raiju.Check().Success, "QT off: original Raiju burst-window hold remains");
            Core.Target.Statuses.Clear();
            Assert(raiju.Check().Success, "QT off: ordinary Raiju remains available");
            PromeSettings.Instance.Qts[NinjaQts.Raiju] = false;
            Assert(!NinjaCombatState.HasRaijuReadyForRotation && !raiju.Check().Success, "QT off: normal Raiju QT regains control");
            Reset(false); ActionHelper.Charges = 1.5f; NinjaSettings.Instance.ForceRaiton = true;
            Assert(raiton.Check().Success, "QT off: forced Raiton regains original behavior below overflow threshold");
            Reset(false);
            Assert(new NinjaJutsuResolver().Check().Success, "QT off: ordinary Suiton is available again");
            Assert(new NinjaMugResolver().Check().Success, "QT off: Dokumori resumes");
            ActionHelper.Cooldowns[NinjaActions.MugCurrent] = 30;
            Assert(new NinjaBunshinResolver().Check().Success, "QT off: Bunshin resumes outside the immediate Dokumori hold");
            ActionHelper.Cooldowns[NinjaActions.TrickAttackCurrent] = 30;
            ActionHelper.Cooldowns[NinjaActions.MugCurrent] = 30;
            Assert(new NinjaAssassinateResolver().Check().Success, "QT off: Dream Within a Dream resumes");
            Core.Me!.Statuses.Add(NinjaActions.ShadowWalkerStatus);
            ActionHelper.Cooldowns[NinjaActions.TrickAttackCurrent] = 0;
            Assert(new NinjaTrickAttackResolver().Check().Success, "QT off: Trick Attack resumes");
            Core.Target!.Statuses.Add(NinjaActions.KunaisBaneStatus);
            Assert(new NinjaKassatsuResolver().Check().Success, "QT off: Kassatsu resumes");
            Core.Me.Statuses.Clear();
            Core.Target.Statuses.Add(NinjaActions.MugDebuff);
            ActionHelper.Cooldowns[NinjaActions.Ten] = 5;
            Assert(new NinjaTenChiJinResolver().Check().Success, "QT off: Ten Chi Jin resumes");

            var potion = new NinjaPotionResolver();
            Reset(false);
            ActionHelper.Cooldowns[NinjaActions.TrickAttackCurrent] = 3.5f;
            Assert(!potion.Check().Success, "Automatic potion must require its own QT");
            PromeSettings.Instance.Qts[NinjaQts.Potion] = true;
            ActionHelper.Cooldowns[NinjaActions.MugCurrent] = 99f;
            foreach (var cooldown in new[] { -1f, 0f, 2.499f, 2.5f, 2.501f, 3.5f, 4.999f, 5f, 5.001f, 60f })
            {
                ActionHelper.Cooldowns[NinjaActions.TrickAttackCurrent] = cooldown;
                Assert(potion.Check().Success == (cooldown > 2.5f && cooldown < 5f),
                    "0612 must follow Kunai's Bane cooldown, excluding endpoints: " + cooldown);
            }
            ActionHelper.Cooldowns[NinjaActions.TrickAttackCurrent] = 3.5f;
            foreach (var remain in new[] { 0f, .4f, .599f, .6f, 1.5f })
            {
                ActionHelper.GcdRemain = remain;
                Assert(potion.Check().Success == (remain >= .6f), "Potion weave boundary: " + remain);
            }
            foreach (var id in new uint[] { 45995, 1045995 })
            {
                GameData.PotionId = id;
                var action = potion.GetAction();
                Assert(action.ActionId == id && action.Type == ActionType.Item
                    && action.Target == ActionTargetType.Self && action.RequiresVerification,
                    "Potion must preserve the current NQ/HQ item ID, self target and verification");
            }
            GameData.PotionId = 0;
            Assert(!potion.Check().Success, "Missing potion must block automatic use");
            GameData.PotionId = 1045995; ActionHelper.ItemOnCooldown = true;
            Assert(!potion.Check().Success, "Shared item cooldown must block automatic use");
            ActionHelper.ItemOnCooldown = false;
            Core.Target = null;
            Assert(!potion.Check().Success, "Target disappearing must block automatic use");
            Core.Target = new() { Attackable = false };
            Assert(!potion.Check().Success, "Untargetable boss must block automatic use");
            Core.Target.Attackable = true; Core.Me = null;
            Assert(!potion.Check().Success, "Missing player must block automatic use");
            Core.Me = new();
            Core.Me.Statuses.Add(NinjaActions.MudraStatus);
            Assert(!potion.Check().Success, "Manual mudra status must block automatic use");
            Core.Me.Statuses.Clear(); NinjaSequencePolicy.IsActive = true;
            Assert(!potion.Check().Success, "Queued mudra sequence must block automatic use");
            NinjaSequencePolicy.IsActive = false; Core.Me.Statuses.Add(NinjaActions.TcjStatus);
            Assert(!potion.Check().Success, "TCJ sequence must block automatic use");
            Core.Me.Statuses.Clear(); PromeSettings.Instance.Qts[NinjaQts.DelayBurst] = true;
            Assert(!potion.Check().Success, "Delay burst must also hold automatic potions");
            PromeSettings.Instance.Qts[NinjaQts.DelayBurst] = false;
            Assert(potion.Check().Success, "Potion must resume after all temporary blockers clear");
            return count;
        }
    }
}
'@
Add-Type -TypeDefinition ("#nullable enable`nglobal using System;`n" + ($sources -join "`n") + "`n" + $fixture)
$count = [IsolatedCompilation.BurstDelayChecks]::Run()
Write-Output "Verified $count Ninja delay-burst, potion and queue scenarios."
