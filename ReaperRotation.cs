using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.PureTimeline;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Updaters;
using PromeRotation.UI.HotKey;

namespace Reaper.PR;

[RotationMetadata(39u, "Luli Reaper PR", "菌子人", "1.0.0.0")]
public sealed partial class ReaperRotation : IRotation, IRotationLifecycle
{
    public string AuthorName => "菌子人";
    // AE exposes the unset/no-opener state as "待定".
    private const string FollowTimelineOpener = "待定";
    private const string LegacyFollowTimelineOpener = "跟随时间轴";
    private const string LegacyNoOpener = "无起手";
    private static readonly string[] OpenerOptions =
    {
        "切割2G团辅起手",
        "烙印3G团辅起手",
        "伊甸起手",
        "绝丝瓜起手0g起手",
        "特化1g暴食0g团辅",
        "提前1G团辅特化起手",
        FollowTimelineOpener
    };
    private static OpenerConfig openerConfig = LoadOpenerConfig();
    private static string selectedOpener = openerConfig.Selected ?? FollowTimelineOpener;
    internal static bool OpenerBurst => openerConfig.OpenerBurst;
    internal static bool OpenerRush => openerConfig.OpenerRush;
    internal static int PotionType => openerConfig.PotionType;
    internal static bool IsSelectedOpener(string name)
    {
        if (selectedOpener == FollowTimelineOpener) return false;
        return CanonicalOpenerName(selectedOpener) == CanonicalOpenerName(name);
    }

    private static string CanonicalOpenerName(string? name) => name switch
    {
        LegacyFollowTimelineOpener => FollowTimelineOpener,
        LegacyNoOpener => FollowTimelineOpener,
        "0G团辅起手" => "绝丝瓜起手0g起手",
        "-1G起手" => "提前1G团辅特化起手",
        "2G团辅起手" => "切割2G团辅起手",
        "3G团辅起手" => "烙印3G团辅起手",
        "ED起手" => "伊甸起手",
        "暴食1G起手" => "特化1g暴食0g团辅",
        _ => name ?? string.Empty
    };

    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        ["切割2G团辅起手"] = typeof(TwoGOpener),
        ["烙印3G团辅起手"] = typeof(ThreeGOpener),
        ["伊甸起手"] = typeof(EdOpener),
        ["绝丝瓜起手0g起手"] = typeof(ZeroGOpener),
        ["特化1g暴食0g团辅"] = typeof(GluttonyOneGOpener),
        ["提前1G团辅特化起手"] = typeof(MinusOneGOpener)
    };
    // Resolver order mirrors AE's ReaperRotationEntry exactly. WhorlOfDeath
    // exists in the reference tree but is intentionally not registered there.
    private readonly List<IDecisionResolver> gcd = new()
    {
        new PerfectioGcd(),
        new EnshroudGcd(),
        new PlentifulHarvestGcd(),
        new GibbetGcd(),
        new SoulSliceGcd(),
        new ShadowOfDeathGcd(),
        new HarvestMoonGcd(),
        new BaseGcd(),
        new HarpeGcd()
    };
    private readonly List<IDecisionResolver> ogcd = new()
    {
        new PotionOgcd(),
        new ArcaneCircleOgcd(),
        new SacrificiumOgcd(),
        new EnshroudOgcd(),
        new TrueNorthOgcd(),
        new LemureOgcd(),
        new GluttonyOgcd(),
        new BloodStalkOgcd()
    };
    private readonly IRotationEventHandler events = new ReaperEventHandler();
    private readonly HotkeyPanel hotkeyPanel;
    private readonly HashSet<string> qtIds = new();
    private readonly HashSet<string> hiddenQtIds = new();
    internal static float ExtraRange => openerConfig.ExtraRange;
    internal static float MoveCasting => openerConfig.MoveCasting;
    public ReaperRotation()
    {
        foreach (var q in new[]
        {
            ("基础连", true), ("灵魂切割", true), ("死亡之影", true), ("收获月", false),
            ("死亡之涡", true), ("勾刃", false), ("大丰收", true), ("完人", true),
            ("附体", true), ("单附体", false), ("双附体", true), ("三附体", false),
            ("牲祭", true), ("暴食", true), ("暴食前置", false), ("隐匿挥割", true),
            ("倾泄隐匿挥割", false), ("自动真北", true), ("神秘环", true), ("爆发药", false),
            ("移动读条", false), ("提高完人优先级", false), ("只打大丰收附体", false),
            ("脱战播魂种", true), ("长臂猿", false), ("团契", true)
        })
        {
            PromeSettings.Instance.AddQt(q.Item1, q.Item2);
            qtIds.Add(q.Item1);
        }

        hotkeyPanel = new HotkeyPanel(7, 45f, 5f, "Luli Reaper Hotkeys", "LuliReaperPR");
        HotkeyManager.Instance.AddHotkeyPanel(hotkeyPanel);
        hotkeyPanel.AddHotkey("爆发药", new DynamicActionLogic(() => GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self), R.Potion);
        hotkeyPanel.AddHotkey("疾跑", R.A(3u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("极限技", new DynamicActionLogic(LimitBreakHelper.GetLimitBreakActionId, ActionType.LimitBreak, ActionTargetType.Target));
        hotkeyPanel.AddHotkey("镜头方向后撤", R.A(24402u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("镜头方向突进", R.A(R.Ingress, ActionType.OffGcd, ActionTargetType.Target));
        hotkeyPanel.AddHotkey("牵制", R.A(7549u, ActionType.OffGcd));
        hotkeyPanel.AddHotkey("播魂种", R.A(R.Soulsow, ActionType.Gcd));
        hotkeyPanel.AddHotkey("浴血", R.A(7542u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("内丹", R.A(7541u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("神秘纹", R.A(24404u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("亲疏自行", R.A(7548u, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("苦难之心", R.A(16535u, ActionType.OffGcd, ActionTargetType.Self));
        EnforceHiddenPanels();
    }

    public PAction? NextAlways()
    {
        EnforceHiddenPanels();
        if (R.Qt("脱战播魂种") && !GameData.IsInCombat() && R.Ready(R.Soulsow) && !R.Has(2594u))
            return R.A(R.Soulsow, ActionType.Gcd, ActionTargetType.Self);
        return null;
    }
    public PAction? NextGcd() { EnforceHiddenPanels(); return Next(gcd); }
    public PAction? NextOffGcd() { EnforceHiddenPanels(); return Next(ogcd); }
    private static PAction? Next(IEnumerable<IDecisionResolver> rs) { foreach (var r in rs) if (r.Check().Success) return r.GetAction(); return null; }
    public void UpdateDebugStatus()
    {
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();
        RotationManager.AlwaysSolverStatus.Add(new SolverStatus { Name = "SoulsowOutOfCombat", Success = R.Qt("脱战播魂种") && !GameData.IsInCombat(), Message = string.Empty });
        AddStatuses(gcd, RotationManager.GcdSolverStatus);
        AddStatuses(ogcd, RotationManager.OffGcdSolverStatus);
    }
    private static void AddStatuses(IEnumerable<IDecisionResolver> resolvers, IList<SolverStatus> output)
    {
        foreach (var resolver in resolvers)
        {
            var result = resolver.Check();
            output.Add(new SolverStatus { Name = resolver.GetType().Name, Success = result.Success, Message = result.Message });
        }
    }
    public IOpener? GetOpener()
    {
        if (selectedOpener == FollowTimelineOpener || selectedOpener == LegacyNoOpener) return null;
        var name = CanonicalOpenerName(selectedOpener);
        if (string.IsNullOrWhiteSpace(name) || !Openers.TryGetValue(name, out var type)) return null;
        return Activator.CreateInstance(type) as IOpener;
    }
    public IRotationEventHandler GetEventHandler() => events;
    public void OnEnterAcr() => ReaperHooks.Update();
    public void OnExitAcr() => ReaperHooks.Dispose();
    private sealed class OpenerConfig
    {
        public string? Selected { get; set; }
        public bool OpenerBurst { get; set; }
        public bool OpenerRush { get; set; }
        public int PotionType { get; set; }
        public bool HideQtPage { get; set; }
        public bool HideHotkeyPage { get; set; }
        public float ExtraRange { get; set; } = 3f;
        public float MoveCasting { get; set; } = 0.4f;
    }

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncherCN", "pluginConfigs", "PromeRotation", "ACR", "菌子人", "opener.json");

    private static OpenerConfig LoadOpenerConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var config = JsonSerializer.Deserialize<OpenerConfig>(File.ReadAllText(ConfigPath));
                if (config?.Selected != null)
                {
                    var normalized = CanonicalOpenerName(config.Selected);
                    if (Array.IndexOf(OpenerOptions, normalized) >= 0)
                    {
                        config.Selected = normalized;
                        return config;
                    }
                }
            }
        }
        catch
        {
            // A damaged optional settings file should not prevent the ACR from loading.
        }
        return new OpenerConfig { Selected = FollowTimelineOpener };
    }

    private static void SaveOpenerConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(openerConfig));
        }
        catch
        {
            // The in-memory selection remains active if the optional persistence file is unavailable.
        }
    }
}
