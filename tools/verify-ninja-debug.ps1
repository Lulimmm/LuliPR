$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../Ninja/Helper/NinjaBattleDebugSnapshot.cs') -Raw -Encoding UTF8
# Compile the actual diagnostic formatter without loading the game or PR SDK.
# Cover interrupted sequences and the priority order responsible for misleading UI.
$checks = @'

public static class NinjaDebugChecks
{
    public static int Run()
    {
        var count = 0;
        var normal = new NinjaBattleDebugSnapshot
        {
            HasPlayer = true, HostAllowsAcr = true, TargetPresent = true,
            TargetAttackable = true, MeleeRange = 3, MudraCharges = 2,
            CanUsePostNinjutsuOffGcd = true, CanUseGcdOffGcd = true,
            CanUseCurrentGcdAbility = true
        };
        void Expect(string text, string expected)
        {
            if (!text.Contains(expected, System.StringComparison.Ordinal))
                throw new System.Exception($"Expected '{expected}', got '{text}'");
            count++;
        }
        string Observations(NinjaBattleDebugSnapshot state) => string.Join("\n", state.Observations());
        Expect((normal with { TargetPresent = false, TargetAttackable = false }).DescribeGcd(), "没有选中目标");
        Expect((normal with { TargetAttackable = false }).DescribeGcd(), "目标存在但当前不可攻击");
        Expect(normal.DescribeHost(), "未观察到");
        Expect((normal with { AcrPaused = true, HasActiveCommand = true }).DescribeHost(), "停手状态");
        Expect((normal with { HasPlayer = false }).DescribeHost(), "自身角色不可用");
        Expect((normal with { Dead = true }).DescribeHost(), "等待复活");
        Expect((normal with { HostAllowsAcr = false }).DescribeHost(), "执行条件未满足");
        Expect((normal with { Countdown = 5 }).DescribeHost(), "倒计时剩余");
        Expect((normal with { Occupied = true }).DescribeHost(), "占用状态");
        Expect((normal with { GcdRemain = .31f }).DescribeHost(), "0.30 秒选择窗口");
        Expect((normal with { GcdRemain = .30f }).DescribeHost(), "未观察到");
        Expect((normal with { HasActiveCommand = true }).DescribeHost(), "活动命令");
        Expect((normal with { HostOpenerActive = true }).DescribeHost(), "固定起手队列");
        Expect((normal with { TcjActive = true, HoldsNormalDecisions = true }).DescribeGcd(), "后续忍术也尚未进入选择");
        Expect((normal with { TcjActive = true }).DescribeGcd(), "天地人状态独占");
        Expect((normal with { NormalMudraActive = true }).DescribeGcd(), "普通结印进行中");
        Expect((normal with { Distance = 4 }).DescribeGcd(), "超过近战范围");
        Expect((normal with { GcdCandidate = "基础连击" }).DescribeGcd(), "仍需宿主允许");
        Expect((normal with { TenriEligible = true }).DescribeGcd(), "能力技是否可用还要检查");
        Expect((normal with { TcjActive = true, CanUseGcdOffGcd = false }).DescribeOffGcd(), "天地人独占");
        Expect((normal with { CanUsePostNinjutsuOffGcd = false }).DescribeOffGcd(), "忍术后能力技额度");
        Expect((normal with { CanUseGcdOffGcd = false }).DescribeOffGcd(), "本 GCD 能力技额度");
        Expect((normal with { CanUseCurrentGcdAbility = false }).DescribeOffGcd(), "单能力技限制");
        Expect(normal.DescribeOffGcd(), "允许评估");
        Expect((normal with { ShadowWalker = true, MudraCharges = 0, GcdCandidate = "基础连击" }).DescribeGcd(), "基础连击");
        Expect(Observations(normal with { TargetAttackable = false, HasQueuedActions = true }), "不代表旧序列已取消");
        Expect(Observations(normal with { ActiveIsGroup = true }), "不等于技能组已清空");
        Expect(Observations(normal with { AwaitingVerification = true, AttemptAgeMs = 3000 }), "至少 3 秒");
        Expect((normal with { HasActiveCommand = true, CommandReadable = false }).DescribeCommand(), "未提供可读取");
        Expect((normal with { HasActiveCommand = true, CommandReadable = true, ActiveTargetsEnemy = true,
            TargetAttackable = false, AwaitingVerification = true, AttemptAgeMs = 5000,
            RetryCount = 3, ActiveReady = false, ActiveAction = "水遁（2271）" }).DescribeCommand(), "水遁（2271）");
        Expect((normal with { HasActiveCommand = true, CommandReadable = true, ActiveTargetsEnemy = true,
            TargetAttackable = false }).DescribeCommand(), "没有可攻击目标");
        Expect(Observations(normal with { TenriEligible = true, CanUseGcdOffGcd = false }), "不代表已经提交");
        return count;
    }
}
'@
Add-Type -TypeDefinition ($source + $checks)
$count = [Ninja.PR.Helper.NinjaDebugChecks]::Run()
Write-Output "Verified $count Ninja battle diagnostic scenarios."
