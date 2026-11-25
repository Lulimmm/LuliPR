$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../Ninja/UI/NinjaSuitonHotkey.cs') -Raw -Encoding UTF8
$actions = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../Ninja/Data/NinjaActions.cs') -Raw -Encoding UTF8
# Exercise the real hotkey against an isolated host. This checks click-time
# validation, queue contents and failure/retry boundaries without running the game.
$source = $source.Replace('namespace Ninja.PR.UI;', 'namespace Ninja.PR.UI {') + "`n}"
$actions = $actions.Replace('namespace Ninja.PR.Data;', 'namespace Ninja.PR.Data {') + "`n}"
# Each source has file-scoped imports; compile the action constants inside a
# namespace block so all imports remain before their own declarations.
$fixture = @'
namespace PromeRotation.Extensions { public static class Placeholder { } }
namespace PromeRotation.Resolvers { public static class Placeholder { } }
namespace PromeRotation.Core
{
    public sealed class TestCharacter
    {
        public bool IsDead;
        public bool Attackable = true;
        public readonly HashSet<uint> Statuses = new();
        public bool HasStatus(uint id) => Statuses.Contains(id);
        public bool CanUseAttackActionOn() => Attackable;
    }
    public static class Core
    {
        public static TestCharacter? Me = new();
        public static TestCharacter? Target = new();
    }
    public static class GameData { public static float Countdown; public static float GetCountdown() => Countdown; }
}
namespace PromeRotation.Data
{
    public enum ActionType { Gcd, OffGcd }
    public enum ActionTargetType { Self, Target }
    public sealed class PAction(uint id, ActionType type, ActionTargetType target)
    {
        public readonly uint ActionId = id;
        public readonly ActionType Type = type;
        public readonly ActionTargetType Target = target;
        public bool RequiresVerification { get; set; }
    }
}
namespace PromeRotation.Helpers
{
    public static class ActionHelper
    {
        public static float Charges = 2;
        public static readonly HashSet<uint> Locked = new();
        public static bool IsUnlocked(uint id) => !Locked.Contains(id);
        public static float GetActionCharges(uint id) => Charges;
    }
}
namespace PromeRotation.Managers
{
    public static class RotationManager
    {
#pragma warning disable CS0414
        private static bool _isOpenerInProgress;
#pragma warning restore CS0414
        public static void SetOpener(bool active) => _isOpenerInProgress = active;
    }
    public sealed class ActionGroupCommand { }
    public static class ActionQueueManager
    {
        public static bool HighPriority;
        public static int Calls;
        public static List<PAction>? LastActions;
        public static bool LastWasHighPriority;
        public static bool HasHighPriorityAction() => HighPriority;
        public static void Enqueue(List<PAction> actions, bool isHighPriority = false)
        {
            Calls++;
            LastActions = actions;
            LastWasHighPriority = isHighPriority;
            HighPriority = isHighPriority;
        }
    }
}
namespace PromeRotation.UI.HotKey
{
    public interface IHotkeyLogic { bool IsActive(); void OnClick(); }
}
namespace Ninja.PR
{
    public static class NinjaRotation
    {
        private static object? _activeCommand;
        public static readonly FieldInfo? ActiveCommandField = typeof(NinjaRotation)
            .GetField("_activeCommand", BindingFlags.Static | BindingFlags.NonPublic);
        public static void SetCommand(object? command) => _activeCommand = command;
    }
}
namespace Ninja.PR.Helper
{
    public static class NinjaTarget { public static float Distance = 3; public static float AttackRange => 3; }
    public static class NinjaCombatState
    {
        public static bool IsTcjSequenceActive;
        public static readonly HashSet<uint> Recent = new();
        public static bool Recently(uint id, int milliseconds) => Recent.Contains(id);
    }
    public static class NinjaSequencePolicy
    {
        public static bool IsActive;
        public static int Begins;
        public static uint Terminal;
        public static void Begin(uint id) { IsActive = true; Begins++; Terminal = id; }
    }
    public static class NinjaNoLossOpenerRuntime { public static bool ShouldHoldNormalDecisions; }
    public static class NinjaBattleDebug { public static void RecordManualSuitonRequest() { } }
}
namespace Ninja.PR.UI
{
    public static class SuitonHotkeyChecks
    {
        private static int count;
        private static readonly NinjaSuitonHotkey hotkey = new();
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            count++;
        }
        private static void Reset()
        {
            Core.Me = new(); Core.Target = new(); GameData.Countdown = 0;
            ActionHelper.Charges = 2; ActionHelper.Locked.Clear();
            RotationManager.SetOpener(false); NinjaRotation.SetCommand(null);
            NinjaCombatState.IsTcjSequenceActive = false; NinjaCombatState.Recent.Clear();
            NinjaSequencePolicy.IsActive = false; NinjaSequencePolicy.Begins = 0;
            NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions = false;
            ActionQueueManager.HighPriority = false; ActionQueueManager.Calls = 0;
            ActionQueueManager.LastActions = null; NinjaTarget.Distance = 3;
            NinjaSuitonHotkey.Reset();
        }
        private static void Rejected(System.Action change, string expected)
        {
            Reset(); change();
            Assert(!hotkey.IsActive(), expected + " should disable hotkey");
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 0 && NinjaSequencePolicy.Begins == 0, expected + " must not queue or begin a sequence");
            Assert(NinjaSuitonHotkey.GetBlockReason()?.Contains(expected) == true, "Expected block reason: " + expected);
        }
        public static int Run()
        {
            Rejected(() => Core.Me = null, "自身");
            Rejected(() => Core.Me!.IsDead = true, "死亡");
            Rejected(() => Core.Target = null, "没有可攻击目标");
            Rejected(() => Core.Target!.Attackable = false, "没有可攻击目标");
            Rejected(() => NinjaTarget.Distance = 20.01f, "距离");
            Rejected(() => GameData.Countdown = 15, "倒数");
            Rejected(() => GameData.Countdown = .1f, "倒数");
            Rejected(() => RotationManager.SetOpener(true), "固定起手");
            Rejected(() => NinjaNoLossOpenerRuntime.ShouldHoldNormalDecisions = true, "无损起手");
            Rejected(() => NinjaRotation.SetCommand(new ActionGroupCommand()), "连续技能组");
            Rejected(() => NinjaCombatState.IsTcjSequenceActive = true, "天地人");
            Rejected(() => NinjaSequencePolicy.IsActive = true, "忍术序列");
            Rejected(() => Core.Me!.Statuses.Add(NinjaActions.MudraStatus), "结印");
            Rejected(() => Core.Me!.Statuses.Add(NinjaActions.KassatsuStatus), "生杀");
            Rejected(() => Core.Me!.Statuses.Add(NinjaActions.ShadowWalkerStatus), "3848");
            Rejected(() => NinjaCombatState.Recent.Add(NinjaActions.Kassatsu), "生杀");
            Rejected(() => NinjaCombatState.Recent.Add(NinjaActions.Suiton), "刚使用水遁");
            Rejected(() => ActionHelper.Charges = 0, "次数不足");
            Rejected(() => ActionHelper.Charges = .999f, "次数不足");
            Rejected(() => ActionHelper.Locked.Add(NinjaActions.Ten), "未解锁");
            Rejected(() => ActionHelper.Locked.Add(NinjaActions.Suiton), "未解锁");
            Rejected(() => ActionQueueManager.HighPriority = true, "优先技能");

            Reset(); NinjaTarget.Distance = 20; ActionHelper.Charges = 1;
            // An ordinary active command/GCD must not prevent a manual request.
            NinjaRotation.SetCommand(new object());
            Assert(hotkey.IsActive(), "One charge and legal range should allow request behind an ordinary action");
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 1 && NinjaSequencePolicy.Begins == 1, "One click must queue one group");
            Assert(ActionQueueManager.LastWasHighPriority, "Manual group must have the same priority as Raiton hotkey");
            var actions = ActionQueueManager.LastActions!;
            Assert(actions.Count == 4, "Suiton group must contain exactly four actions");
            var expected = new uint[] { NinjaActions.Ten, NinjaActions.ChiCombo, NinjaActions.JinCombo, NinjaActions.Suiton };
            for (var i = 0; i < actions.Count; i++)
            {
                Assert(actions[i].ActionId == expected[i] && actions[i].Type == ActionType.Gcd && actions[i].RequiresVerification,
                    "Mudra sequence IDs, GCD types and verification must match");
                Assert(actions[i].Target == (i == 3 ? ActionTargetType.Target : ActionTargetType.Self), "Only Suiton targets the enemy");
            }
            for (var i = 0; i < 10; i++) hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 1, "Repeated clicks must not stack groups");
            NinjaSequencePolicy.IsActive = false;
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 1, "An early-ended sequence policy must not allow stacking behind the queued group");

            ActionQueueManager.HighPriority = false;
            Assert(NinjaSuitonHotkey.GetBlockReason()!.Contains("5 秒"), "Request debounce must survive sequence completion");
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 1, "Completion must not immediately permit another click");
            typeof(NinjaSuitonHotkey).GetField("lastQueuedAt", BindingFlags.Static | BindingFlags.NonPublic)!
                .SetValue(null, Environment.TickCount64 - 5100);
            Assert(hotkey.IsActive(), "A later deliberate click may submit a new request");
            ActionHelper.Charges = 0;
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 1, "Click must recheck resources after availability changed");
            Assert(!hotkey.IsActive(), "A failed request must not become an automatically repeated preparation");

            Reset();
            Assert(hotkey.IsActive(), "Initial manual request should be available");
            GameData.Countdown = 5;
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 0, "Countdown starting between display and click must prevent insertion");
            GameData.Countdown = 0;
            Assert(hotkey.IsActive(), "A blocked countdown click must not consume the five-second debounce");
            RotationManager.SetOpener(true);
            hotkey.OnClick();
            Assert(ActionQueueManager.Calls == 0, "Opener starting between display and click must prevent insertion");
            return count;
        }
    }
}
'@
# Keep the actual constants and hotkey in the same isolated compilation.
$actions = [regex]::Replace($actions, '(?m)^using [^;]+;\r?\n', '')
Add-Type -TypeDefinition ("#nullable enable`n" + $source + "`n" + $actions + "`n" + $fixture)
$count = [Ninja.PR.UI.SuitonHotkeyChecks]::Run()
Write-Output "Verified $count Suiton hotkey boundary and queue checks."
