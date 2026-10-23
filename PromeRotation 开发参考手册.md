# PromeRotation 开发参考手册

> 适用于 PromeRotation v1.5.1.8  
> 整合自脚本 API、本体 API 总结、Helper 速查及 ACR 开发文档


## 目录

- [一、概述](#一概述)
- [二、脚本系统（IRunningScript）](#二脚本系统irunningscript)
    - [2.1 快速开始](#21-快速开始)
    - [2.2 核心上下文 IScriptContext](#22-核心上下文-iscriptcontext)
    - [2.3 动作 API](#23-动作-api)
    - [2.4 条件 API](#24-条件-api)
    - [2.5 目标选择器](#25-目标选择器)
    - [2.6 移动系统](#26-移动系统)
- [三、本体核心 API（ACR 开发基础）](#三本体核心-apiacr-开发基础)
    - [3.1 Core 静态类](#31-core-静态类)
    - [3.2 GameData 静态类](#32-gamedata-静态类)
    - [3.3 PAction 与枚举](#33-paction-与枚举)
    - [3.4 PromeSettings 全局设置](#34-promesettings-全局设置)
    - [3.5 HackSettings 行为开关](#35-hacksettings-行为开关)
    - [3.6 接口契约](#36-接口契约)
    - [3.7 扩展方法](#37-扩展方法)
    - [3.8 帮助类（Helpers）总览](#38-帮助类helpers总览)
    - [3.9 管理器（Managers）](#39-管理器managers)
    - [3.10 Timeline 节点系统](#310-timeline-节点系统)
    - [3.11 其他系统](#311-其他系统)
- [四、Helper API 速查表](#四helper-api-速查表)
- [五、ACR 开发指南](#五acr-开发指南)
    - [5.1 加载规则与项目配置](#51-加载规则与项目配置)
    - [5.2 实现 IRotation 入口](#52-实现-irotation-入口)
    - [5.3 决策解析器 IDecisionResolver](#53-决策解析器-idecisionresolver)
    - [5.4 GCD / oGCD 调度机制](#54-gcd--ogcd-调度机制)
    - [5.5 QT 开关](#55-qt-开关)
    - [5.6 Hotkey](#56-hotkey)
    - [5.7 Timeline 节点提供者 IJobNodeProvider](#57-timeline-节点提供者-ijobnodeprovider)
    - [5.8 起手 IOpener](#58-起手-iopener)
    - [5.9 事件处理 IRotationEventHandler](#59-事件处理-irotationeventhandler)
    - [5.10 调试状态](#510-调试状态)
    - [5.11 工程实践建议](#511-工程实践建议)
    - [5.12 开发步骤与排错清单](#512-开发步骤与排错清单)
- [附录：常用枚举参考](#附录常用枚举参考)


## 一、概述

PromeRotation（简称 PR）是一个 FFXIV 战斗插件，提供两套脚本/自动化体系：

1. **脚本系统**（基于 `IRunningScript`）——适合编写一次性任务或简单的自动化脚本，使用 `IScriptContext` 上下文，提供动作、条件、目标选择等能力。
2. **ACR 系统**（基于 `IRotation`）——用于开发完整的职业战斗循环（ACR），支持 GCD/oGCD 决策、起手、QT、Timeline 节点、Hotkey 等高级功能。

本文档整合了这两套体系的 API 参考以及 ACR 开发的全流程指南，方便开发者快速上手。


## 二、脚本系统（IRunningScript）

脚本系统适用于轻量级自动化任务，脚本类实现 `IRunningScript` 接口，由 PR 框架加载并驱动。

### 2.1 快速开始

```csharp
    using System;
    using System.Numerics;
    using PromeRotation.Core;
    using PromeRotation.Helpers;
    using PromeRotation.Scripting;
    using PromeRotation.Timeline.Actions;
    using PromeRotation.Timeline.Conditions;

        public class MyScript : IRunningScript
    {
        private bool _finished;

       public void OnStart(IScriptContext ctx)
       {
        _finished = false;
            ctx.Log("[脚本] 开始执行");
        }

        public void OnUpdate(IScriptContext ctx, float deltaTime)
       {
            if (_finished) return;
        var me = ctx.GetMe();
        if (me == null) return;

        ctx.Log($"[脚本] 玩家: {me.Name}, HP: {me.CurrentHp}/{me.MaxHp}");

        // 执行你的逻辑，完成后设置 _finished = true
        _finished = true;
    }

    public bool IsFinished(IScriptContext ctx) => _finished;

    public void OnStop(IScriptContext ctx)
    {
        ctx.Log("[脚本] 执行结束");
    }
}
```

### 2.2 核心上下文 IScriptContext
IBattleChara? GetMe() —— 获取本地玩家对象。

void Log(string message) —— 输出普通日志。

void LogError(string message) —— 输出错误日志。

### 2.3 动作 API
所有动作类位于 PromeRotation.Timeline.Actions 命名空间，实现 IAction 接口。

技能执行：

EnqueueSkillAction(actionId, type, target, highPriority) —— 将技能加入队列。

ForceUseSkillAction(actionId, type, target) —— 强制立即使用技能。

EnqueueLocationAction(actionId, type, target, highPriority, x, y, z) —— 地面目标技能入队。

ForceUseLocationAction(actionId, type, target, x, y, z) —— 强制地面技能。

队列控制：

ClearAllQueuesAction() —— 清空所有队列。

目标控制：

SetTargetAction(targetType, targetId) —— 设置目标。

SetTargetSelectorModeAction(mode) —— 设置自动选目标模式。

角色控制：

HeadingControlAction(mode, angle) —— 面向控制。

ToggleAcrAction(mode) —— 切换 ACR 状态。

其他：

UsePotionAction(mode) —— 使用爆发药。

ExecuteCommandAction("/command") —— 执行游戏命令。

CustomLogAction("message") —— 自定义日志。

TriggerQtAction("QT名称", true) —— 触发 QT。

BatchTriggerQtAction(qtStateList) —— 批量触发 QT。

GreenMoveToPositionAction(x, y, z, wait, ignoreY) —— 移动到位置。

TeleportToPositionAction(x, y, z) —— 传送到位置。

组合动作：

```csharp
var actions = new List<IAction>
{
    new ClearAllQueuesAction(),
    new EnqueueSkillAction(100u, 0, 0, false),
    new CustomLogAction("组合完成")
};
```
### 2.4 条件 API
条件类位于 PromeRotation.Timeline.Conditions，实现 ICondition。

HasBuffFriendlyCondition(buffId, targetType, negate) —— 检查是否拥有 Buff。

BuffTimeFriendlyCondition(buffId, targetType, compare, threshold, immediate) —— 检查 Buff 剩余时间。

InCombatCondition() —— 是否战斗中。

CountdownCondition() —— 是否在倒计时中。

SkillCooldownCondition(actionId, compare, threshold, immediate) —— 技能冷却。

CastStartCondition(actionId, immediate) —— 读条开始。

ActionEffectCondition(actionId, immediate) —— 动作效果。

ChatLogCondition(pattern, immediate) —— 聊天日志匹配。

WeatherCondition(weatherId, negate) —— 天气。

PlayerPositionCondition(mode, compare, value, x, y, z, range, ignoreY) —— 位置检查。

TargetSelectableCondition(baseId, expectSelectable, immediate) —— 目标是否可选。

条件组合：

```csharp
var conditions = new List<ICondition>
{
    new InCombatCondition(),
    new HasBuffFriendlyCondition(775, targetType, false)
};
```
### 2.5 目标选择器
SelectorModeType 枚举（常用）：

Closest —— 最近目标

Manual —— 手动

其他：Farthest, LowestHp, HighestHp 等。

使用：

```csharp
var setMode = new SetTargetSelectorModeAction(SelectorModeType.Closest);
var setTarget = new SetTargetAction(default(TargetSelectionType), 0u);
```
### 2.6 移动系统
GreenMovePoint 用于多点路径：

```csharp
var point = new GreenMovePoint(
    position: new Vector3(100f, 0f, 100f),
    tolerance: 1f,
    speedMultiplier: 1f,
    ignoreY: false,
    maxCastWaitMs: 5000,
    waitForQueuedCast: true
);
```
## 三、本体核心 API（ACR 开发基础）
此部分描述开发完整 ACR 所需的核心类型与接口。

### 3.1 Core 静态类
static IBattleChara? Me —— 本地玩家，可能为 null。

static IBattleChara? Target —— 当前选中的目标。

static void SetTarget(IBattleChara? target) —— 设置目标。

### 3.2 GameData 静态类
static bool WillMove { get; set; } —— 移动标志（读）。

static bool TpMove { get; set; } —— 瞬移标志。

static bool IsInCombat() —— 是否战斗中。

static float GetCountdown() —— 倒计时剩余秒数。

static uint Weather —— 当前天气 ID。

static bool IsPlayerOccupied() —— 是否被占用（不可操作）。

static uint GetBestPotionId() —— 最佳爆发药 ID。

static float GetCurrentMeleeRange() —— 近战攻击距离（含 hack）。

static float GetCurrentAttackRange(float range) —— 校正后的技能距离。

static bool IsIn120() —— 是否处于 2 分钟爆发窗口（含药）。

static bool IsInPure120() —— 排除药，仅团队 buff。

### 3.3 PAction 与枚举
PAction(actionId, type, target) 是技能执行的最小单元。

ActionId / Type / Target —— 只读。

bool RequiresVerification —— 是否等待命中校验（起手用）。

uint NetworkTid —— 指定网络目标 ID。

bool IsLocationAction / Vector3 Position —— 地面技能专用。

枚举：

ActionType：Gcd, OffGcd, Item, LimitBreak

ActionTargetType：Self, Target, TargetOfTarget, FocusTarget, MouseOver, LowestHealthPartyMember, PartyMember2…PartyMember8

### 3.4 PromeSettings 全局设置
单例 PromeSettings.Instance：

HackSettings Hacks { get; } —— 行为开关。

AcrState EnableAcr { get; set; } —— ACR 总开关（Off / Hold / On）。

bool OpenerHasBeenExecuted { get; set; } —— 起手执行标志（脱战复位）。

SelectorModeType TargetSelectorMode { get; set; } —— 自动选目标模式。

Dictionary<string,bool> QuickToggles { get; } —— QT 字典。

void AddQt(string key, bool defaultValue) —— 注册 QT。

bool GetQt(string key) / void SetQt(string key, bool value) —— 读写 QT。

void ClearQts() —— 清空 QT（加载新职业时调用）。

### 3.5 HackSettings 行为开关
bool IncreaseAttackRange —— 扩大攻击距离。

bool ShowGroundCircle / GroundCircleRainbow —— 地面圈显示。

bool NoActionMoveEnabled —— 移动施法。

bool GcdWaitForNetworkPacket / OgcdWaitForNetworkPacket —— 等待网络回包。

bool EnableUalLogs / EnableAepLogs —— 调试日志。

bool DevMode —— 开发者模式。

bool ShowGreenMoveDebug / GreenMoveUseGameQueue —— 移动调试。

SerializableVector2 FloatingIconPosition —— 悬浮图标位置。

### 3.6 接口契约
IRotation —— ACR 主入口
```csharp
public interface IRotation
{
    string RotationName { get; }
    uint JobId { get; }
    PAction? NextGcd();
    PAction? NextOffGcd();
    void UpdateDebugStatus();
    IOpener? GetOpener();
    IRotationEventHandler GetEventHandler();
    void DrawSettings();
    void DrawQTs();
}
```
需配合 [RotationMetadata(jobId, rotationName)] 特性。

IDecisionResolver —— 决策单元
```csharp
public interface IDecisionResolver
{
    CheckResult Check();      // 判断条件是否满足
    PAction GetAction();      // 返回技能
}
CheckResult(bool Success, string Message) —— 失败原因写在 Message 中。
```
IOpener —— 起手
```csharp
public interface IOpener
{
    string OpenerName { get; }
    List<PAction> InCombatSequence { get; }          // 进战后固定序列
    void InitializeCountdown(CountDownHandler handler); // 倒计时预读
}
CountDownHandler 提供 AddAction(int ms, PAction) 和 AddAction(int ms, Func<PAction>)。
```
IRotationEventHandler —— 事件回调
```csharp
public interface IRotationEventHandler
{
    void OnUpdate();                 // 每帧
    void OnOutOfBattleUpdate();      // 脱战每帧
    void OnBattleStarted();          // 进战瞬间
    void OnBattleUpdate();           // 战斗中每帧
    void OnBattleEnded();            // 脱战/团灭/通关
    void OnTerritoryChanged(ushort territoryId); // 切区
}
```
元信息（静态属性）
ACR 类可暴露以下静态属性供框架反射读取：

static IReadOnlyDictionary<string, bool> QtList —— QT 列表（名→默认值）。

static IReadOnlyDictionary<string, Type> Openers —— 起手模板（名→类型）。

static IJobNodeProvider? NodeProvider —— Timeline 职业节点提供者（可选）。

### 3.7 扩展方法
BattleCharaExtensions（this IBattleChara）
bool HasStatus(uint statusId)

int GetStatusStackCount(uint statusId)

float GetStatusLeftTime(uint statusId)

float DistanceToMe()

Vector2 PositionXY()

bool IsPlayer()

bool IsEnemy()

bool CanUseAttackActionOn()

SkillExtensions（this uint actionId）
float GetActionCooldown()

uint GetAdjustedActionId()

float GetActionCharges()

float GetActionRecastTime()

float GetActionRecastTimeElapsed()

bool IsActionHighlighted()

### 3.8 帮助类（Helpers）总览
所有 Helper 位于 PromeRotation.Helpers。

StatusHelper
HasStatus(IBattleChara c, uint id)

GetStatusStack(IBattleChara c, uint id)

GetStatusLeftTime(IBattleChara c, uint id)

TargetHelper
Positional GetTargetPositional() —— 身位。

bool HasPositionalRequirement(IBattleChara c)

Vector3 GetStancePoint(IBattleChara t, Positional s)

uint EnemyIn5m() / EnemyInRange(float range)

uint EnemyInRangeTarget(IBattleChara? t, float range)

IBattleChara? FindNpcByBaseId(uint baseId)

List<IBattleChara> FindNpcsByBaseId(uint baseId)

bool IsAllBossUntargetable()

PosHelper
bool IsWithinRange(Vector3 cur, Vector3 tgt, float range, bool ignoreY = true)

Vector3 ScreenToWorld()

PartyHelper
List<IBattleChara> GetParty()

List<IBattleChara> GetUIParty()

MathHelper
double θ(float x1, float y1, float x2, float y2) —— 方位角。

double θVector2(Vector2 a, Vector2 b)

double NormalizeAngle(double angle)

Vector3 Round(this Vector3 v, int dot)

ActionHelper
GCD：GetGcdTotal(), GetGcdElapsed(), GetGcdRemain(), GetGcdIsActive()

动画锁：GetAnimationLock(), SetAnimetionLock()

连击：GetComboLeftTime(), GetLastComboID()

咏唱：GetCastTimeTotal(), GetCastTimeElapsed(), GetCastTimeRemain()

技能查询：GetActionCooldown(uint id), GetActionCharges(uint id), GetActionRecastTime(uint id), GetActionRecastTimeElapsed(uint id), GetMaxCharges(uint id), GetAdjustedActionId(uint id), IsActionHighlighted(uint id)

地面技能：UseActionLocation(uint id, ulong targetId, Vector3 pos)

鼠标位置：GetCursorPosition()

CastHelper
int GetActionCast100Ms(uint id) —— 读条时长（100ms 单位）。

double GetAdjustedRemainingCastMs()

bool ShouldWaitForCast(DateTime startUtc, int allowedDelayMs)

LimitBreakHelper
uint GetLimitBreakCharge() —— 充能段数（0=Lv1, 1=Lv2, 2=Lv3）。

uint GetLimitBreakActionId()

ItemHelper
uint GetItemCountInInventory(uint itemId, bool isHq)

IconHelper
IDalamudTextureWrap? GetGameIcon(uint iconId)

IDalamudTextureWrap? GetActionIcon(uint actionId)

IDalamudTextureWrap? GetStatusIcon(uint statusId)

IDalamudTextureWrap? GetJobIcon(uint jobId)

IDalamudTextureWrap? GetDamageTypeIcon(DamageIconType type)

IDalamudTextureWrap? GetIconFromPath(string path)

HintHelper
void ShowToast2(string text, float sec, HintType type) —— 屏幕提示。

JobGaugeHelper
每个职业一个静态嵌套类，例如 JobGaugeHelper.WAR.BeastGauge，JobGaugeHelper.BRD.SoulVoiceGauge 等。具体字段依职业不同。

HackHelper
void TeleportNormal(Vector3 position)

void TeleportToMouse()

Task TeleportWithReturn(Vector3 posA, Vector3 posB, int delayMs)

### 3.9 管理器（Managers）
RotationManager（静态）
List<SolverStatus> GcdSolverStatus / OffGcdSolverStatus —— 调试状态。

IRotation? GetCurrentRotation()

void LoadRotationForJob(uint jobId)

IReadOnlyDictionary<string,bool>? GetCurrentQtList()

IReadOnlyDictionary<string,Type>? GetOpenersByJob(int jobId)

内部驱动：Tick(IFramework)，Initialize() / Dispose()。

ActionQueueManager（静态）
void Enqueue(PAction action, bool isHighPriority=false)

void Enqueue(List<PAction> actions, bool isHighPriority=false)

void EnqueueOffGcdList(List<PAction> actions, bool isHighPriority=false)

void ClearNormalQueues() / ClearAllQueues()

bool HasActionsInQueue() / HasHighPriorityAction()

bool HasActionsInGcdQueue() / HasActionsInOffGcdQueue()

CombatEventManager（静态，事件订阅）
事件：OnEvent, OnStartCast, OnActionEffect, OnActionEffectEntry, OnCreateObject, OnNpcYell, OnActorControl, OnActorControlAddStatus, OnActorControlRemoveStatus, OnActorControlTargetIcon, OnActorControlTether, OnActorControlCancelCast 等。

典型用法：

```csharp
CombatEventManager.OnActionEffect += e => {
    if (e.SourceId != Core.Me?.EntityId) return;
    // 处理自己施放的技能
};
```

### 3.10 Timeline 节点系统
PromeRotation.Timeline 提供条件-动作节点编辑器。

注册自定义节点：ActionFactory.Register(key, creator) / ConditionFactory.Register(key, creator)。

内置动作节点：TriggerQtAction, BatchTriggerQtAction, EnqueueSkillAction, ForceUseSkillAction, EnqueueLocationAction, ForceUseLocationAction, SetTargetAction, SetTargetSelectorModeAction, HeadingControlAction, GreenMoveToPositionAction, TeleportToPositionAction, ExecuteCommandAction, ClearAllQueuesAction, CustomLogAction。

内置条件节点：SkillCooldownCondition, HasBuffFriendlyCondition, BuffTimeFriendlyCondition, CountdownCondition, InCombatCondition, PlayerPositionCondition, CastStartCondition, ActionEffectCondition, TargetSelectableCondition, ChatLogCondition, WeatherCondition。

### 3.11 其他系统
Network：Opcode 映射、封包 hook（PromeRotation.Network）。

NetworkVerifier：服务器授权与完整性校验。

GreenMoveSystem：移动与寻路（IGreenMoveSystem, GreenMoveManager, GreenMovePoint 等）。

TargetSelector：自动选目标服务（TargetSelectorService）。

Service：GcdWatchdogService, SpeedMultiplierService 等。

## 四、Helper API 速查表
```
用途	推荐 API
检查 Buff	StatusHelper.HasStatus() / HasStatus() 扩展
Buff 层数	StatusHelper.GetStatusStack() / GetStatusStackCount()
Buff 剩余时间	StatusHelper.GetStatusLeftTime() / GetStatusLeftTime()
判断身位	TargetHelper.GetTargetPositional()
范围内敌人数量	TargetHelper.EnemyInRange()
获取小队列表	PartyHelper.GetParty()
GCD 信息	ActionHelper.GetGcdTotal() / GetGcdRemain()
技能冷却	ActionHelper.GetActionCooldown() / GetActionCooldown() 扩展
技能充能	ActionHelper.GetActionCharges() / GetActionCharges() 扩展
咏唱剩余	ActionHelper.GetCastTimeRemain()
使用位置技能	ActionHelper.UseActionLocation()
LB 充能	LimitBreakHelper.GetLimitBreakCharge()
最佳爆发药	GameData.GetBestPotionId()
当前天气	GameData.Weather
战斗状态	GameData.IsInCombat()
本地玩家	Core.Me
当前目标	Core.Target
设置目标	Core.SetTarget()
职业图标	IconHelper.GetJobIcon()
屏幕提示	HintHelper.ShowToast2()
```
## 五、ACR 开发指南
本节详细说明如何从零开发一个外部 ACR。

### 5.1 加载规则与项目配置
包目录结构
PR 扫描 pluginConfigs/PromeRotation/ACR/ 下一层子目录作为 ACR 包。主 DLL 优先命名为 {包目录名}.dll，若目录下只有一个 DLL 也可使用。推荐：

```text
ACR/
└── Bard/
    ├── Bard.dll
    └── (其他依赖)
```    
项目配置（.csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <OutputPath>...\pluginConfigs\PromeRotation\ACR\Bard</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Dalamud" HintPath="...\Dalamud.dll" Private="false" />
    <Reference Include="PromeRotation" HintPath="...\PromeRotation.dll" Private="false" />
    <!-- 其他必要引用（ECommons, Lumina 等） -->
  </ItemGroup>
</Project>
```
Private="false" 避免把宿主已有程序集复制进包。

输出目录直接指向 PR 的 ACR 包目录。

### 5.2 实现 IRotation 入口
```csharp
[RotationMetadata((uint)Job.BRD, "D120诗人")]
public class BardRotation : IRotation
{
    public string RotationName => "D120诗人";
    public uint JobId => (uint)Job.BRD;

    private readonly IRotationEventHandler _eventHandler = new BardEventHandler();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();

    // 静态元信息（暴露给框架）
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>();
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>();
    public static IJobNodeProvider? NodeProvider { get; } = null;

    public BardRotation()
    {
        // 注册 resolver（按优先级从高到低）
        _gcdResolvers.Add(new HeavyShotResolver());
        // ...
        // 注册 QT
        foreach (var kv in QtList)
            PromeSettings.Instance.AddQt(kv.Key, kv.Value);
    }

    public PAction? NextGcd()
    {
        foreach (var r in _gcdResolvers)
        {
            var res = r.Check();
            if (res.Success) return r.GetAction();
        }
        return null;
    }

    public PAction? NextOffGcd() { /* 类似 */ }

    public void UpdateDebugStatus()
    {
        RotationManager.GcdSolverStatus.Clear();
        foreach (var r in _gcdResolvers)
        {
            var res = r.Check();
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name = r.GetType().Name,
                Success = res.Success,
                Message = res.Message
            });
        }
        // 同样处理 OffGcd
    }

    public IOpener? GetOpener() { /* 从 Openers 选择 */ }
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    public void DrawSettings() { /* 配置 UI */ }
    public void DrawQTs() { /* QT UI（可选） */ }
}
```
### 5.3 决策解析器 IDecisionResolver
每个 resolver 负责一个技能或一组判断：

```csharp
public class HeavyShotResolver : IDecisionResolver
{
    public CheckResult Check()
    {
        var player = Core.Me;
        if (player == null) return new CheckResult(false, "玩家为空");
        var target = Core.Target;
        if (target == null) return new CheckResult(false, "无目标");
        if (!target.CanUseAttackActionOn()) return new CheckResult(false, "目标不可攻击");
        if (target.DistanceToMe() > GameData.GetCurrentAttackRange(25))
            return new CheckResult(false, "超出射程");
        // 其他条件...
        return new CheckResult(true, "可用");
    }

    public PAction GetAction()
    {
        var adjustedId = BardSkill.强力射击.GetAdjustedActionId();
        return new PAction(adjustedId, ActionType.Gcd, ActionTargetType.Target);
    }
}
```
### 5.4 GCD / oGCD 调度机制
NextGcd() 仅在 GCD 队列为空、GCD 剩余 ≤0.3s、无活动命令、动画锁为 0 时被调用。

NextOffGcd() 仅在 oGCD 队列为空、GCD 剩余 >0.6s、无活动命令、动画锁为 0 时被调用。

起手队列执行期间常规循环暂停。

两次决策间隔至少 100ms（内部限流）。

### 5.5 QT 开关
定义：

```csharp
public static class BardQT
{
    public const string 启用起手 = "启用起手";
}

public static IReadOnlyDictionary<string, bool> QtList { get; } = new()
{
    { BardQT.启用起手, true }
};
```
读取/写入：

```csharp
if (PromeSettings.Instance.GetQt(BardQT.启用起手)) { /* ... */ }
PromeSettings.Instance.SetQt(BardQT.启用起手, false);
```
### 5.6 Hotkey
在构造函数中注册：

```csharp
HotkeyUI.AddHotkey(new ActionHotkey(
    new PAction(SkillId, ActionType.OffGcd, ActionTargetType.Target)));

HotkeyUI.AddHotkey(new DelegateHotkey("退避ST",
    new ExecuteLogic(() =>
    {
        ActionQueueManager.Enqueue(
            new PAction(SkillId, ActionType.OffGcd, ActionTargetType.PartyMember2),
            true);
    })));
```
### 5.7 Timeline 节点提供者 IJobNodeProvider
可选能力，用于暴露职业专属 Timeline 节点。

```csharp
public sealed class BardNodeProvider : IJobNodeProvider
{
    public void RegisterNodes()
    {
        // 注册自定义条件和动作类型（通常通过静态构造函数触发）
        RuntimeHelpers.RunClassConstructor(typeof(MyCondition).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(MyAction).TypeHandle);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => new[] { ("条件显示名", "描述", () => new MyCondition()) };

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => new[] { ("动作显示名", "描述", () => new MyAction()) };
}
```
然后在 ACR 类上暴露 public static IJobNodeProvider? NodeProvider => new BardNodeProvider();

### 5.8 起手 IOpener
```csharp
public class SimpleOpener : IOpener
{
    public string OpenerName => "标准起手";
    public List<PAction> InCombatSequence => new()
    {
        new PAction(BardSkill.猛者, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(BardSkill.战斗之声, ActionType.OffGcd, ActionTargetType.Self),
        // ...
    };

    public void InitializeCountdown(CountDownHandler handler)
    {
        handler.AddAction(3000, new PAction(BardSkill.爆发药, ActionType.Item, ActionTargetType.Self));
        handler.AddAction(2000, () => BuildPrePullAction());
    }

    private PAction BuildPrePullAction() { /* 实时决策 */ }
}
```
在 ACR 的静态 Openers 字典中注册。

关键标志：PromeSettings.Instance.OpenerHasBeenExecuted 在战斗开始时设为 true，在 OnBattleEnded / OnTerritoryChanged 中重置为 false。

### 5.9 事件处理 IRotationEventHandler
```csharp
public class BardEventHandler : IRotationEventHandler
{
    public void OnUpdate() { }
    public void OnOutOfBattleUpdate() { }
    public void OnBattleStarted() { /* 初始化战斗数据 */ }
    public void OnBattleUpdate() { /* 更新缓存 */ }
    public void OnBattleEnded()
    {
        BardBattleData.Instance.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        BardBattleData.Instance.Reset();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }
}
```
### 5.10 调试状态
在 UpdateDebugStatus() 中填充 RotationManager.GcdSolverStatus 和 OffGcdSolverStatus，UI 会显示每个 resolver 的成败及原因。

### 5.11 工程实践建议
拆分 Resolver：每个技能或判断点独立，注册顺序即优先级。

日常与高难分离：将拉怪、减伤、盾姿等能力放在 Daily/ 目录，用设置开关控制。

集中管理减伤：建立减伤调度器，避免多个 resolver 冲突。

使用设置文件：复杂职业额外保存 JSON 配置，DrawSettings() 绘制完整设置页。

一次性 QT：实现“强制一次”模式，执行后自动关闭。

目标选择辅助：集中处理敌人搜索、冷却和目标绑定。

调试页：在 DrawSettings() 中增加调试分页，显示实时状态和轻量测试入口。

### 5.12 开发步骤与排错清单
开发步骤：

新建类库项目，配置输出路径和引用。

定义技能 ID、Buff ID、QT 常量。

实现 IRotation 主类，添加 [RotationMetadata]。

按优先级实现 GCD 和 oGCD resolver。

实现起手（可选）并注册到 Openers。

实现事件处理器。

注册 QT 并实现 UI（DrawQTs）。

添加 Hotkey（可选）。

如需 Timeline 节点，实现 IJobNodeProvider。

编译，将 DLL 放入 ACR 包目录，刷新 PR 加载。

排错清单：

加载失败：检查目录结构、DLL 命名、类型是否 public、是否有无参构造函数、RotationMetadata 是否正确、静态属性类型是否匹配。

QT 不显示：检查静态属性名是否为 QtList，构造函数中是否调用了 AddQt。

起手不生效：检查 Openers 字典，OpenerHasBeenExecuted 状态，战斗结束时是否重置。

技能不执行：确认 ACR 状态为 On，检查 GameData.IsPlayerOccupied()，起手是否完成，Check() 是否返回成功，技能 ID 是否正确。

Hotkey 不响应：确认在构造函数中注册，职业切换后重新注册。

## 附录：常用枚举参考
ActionType
Gcd, OffGcd, Item, LimitBreak

ActionTargetType
Self, Target, TargetOfTarget, FocusTarget, MouseOver, LowestHealthPartyMember, PartyMember2…PartyMember8

SelectorModeType
None, Closest, Farthest, LowestHp, HighestHp, LowestHpIn3R, HighestHpIn3R, LowestHpIn6R, HighestHpIn6R

AcrState
Off, Hold, On

Positional
None, Front, Flank, Rear

HintType
Info, Warning

DamageIconType
PhysicalDamage (60011), MagicDamage (60012), RealDamage (60013)

# 更新时间：2026-06-19