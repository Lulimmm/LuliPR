using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace ErosUI;

// ErosUI 框架门面：使用方的唯一入口。Configure 注入职业环境，
// Install/Uninstall 一站式装卸（窗口注册、热键面板、QT 注册、设置落盘全部内含），
// 必须严格成对地放在 OnEnterAcr/OnExitAcr 里。
public static class ErosUIFramework
{
    private static CombatControlWindow? control;
    private static ErosUISettingsWindow? settings;
    private static ErosUIQtPanelWindow? qtPanel;
    private static ErosUISplashWindow? splash;
    private static bool installRetryPending;

    /// <summary>注入本职业的全部环境。在 Rotation 构造函数最前面调用。</summary>
    public static void Configure(
        string jobTag,
        string jobName,
        IReadOnlyDictionary<string, bool> qtAll,
        Func<string, bool> qtIsMetaKey,
        Func<string, bool, bool> qtIsVisibleInMode,
        Func<string, bool> qtDefault,
        IReadOnlyDictionary<string, (string key, bool invert)[]> qtCascadeRules,
        string[] hotkeyNames,
        System.Action<ErosUIHotkeyBuilder>? buildHotkeys,
        string? author = null,
        string? defaultSettingsJson = null,
        System.Action? drawGeneralSettings = null,
        System.Action? drawDebugSettings = null)
        => ErosUIJobEnv.Configure(jobTag, jobName, qtAll, qtIsMetaKey, qtIsVisibleInMode,
            qtDefault, qtCascadeRules, hotkeyNames, buildHotkeys, author,
            defaultSettingsJson, drawGeneralSettings, drawDebugSettings);

    /// <summary>注册全部窗口并完成初始化：构建热键面板、注册 QT、压制宿主自带面板。幂等，重复调用会先卸载再注册。OnEnterAcr 调用。</summary>
    public static void Install()
    {
        Uninstall();
        // 当前 RPR ACR 只提供高难模块，不显示或切换到日随模式。
        var uiSettings = ErosUISettings.Instance;
        if (!uiSettings.IsHighEnd)
        {
            uiSettings.IsHighEnd = true;
            uiSettings.Save();
        }
        var ws = PromeRotation.Plugin.Instance?.WindowSystem;
        if (ws == null)
        {
            Svc.Log.Info($"[{ErosUIJobEnv.作者}] 无法访问宿主 WindowSystem，UI 未注册");
            installRetryPending = true;
            Svc.PluginInterface.UiBuilder.Draw += RetryPendingInstall;
            return;
        }

        control = new CombatControlWindow(
            $"{ErosUIJobEnv.作者}{ErosUIJobEnv.JobTag} 控制",
            ToggleSettings,
            () => 限制存档位置(ErosUISettings.Instance.控制条位置),
            pos =>
            {
                ErosUISettings.Instance.控制条位置 = pos;
                ErosUISettings.Instance.Save();
            },
            () => ErosUISettings.Instance.ControlPanelPositionLocked,
            locked =>
            {
                ErosUISettings.Instance.ControlPanelPositionLocked = locked;
                ErosUISettings.Instance.Save();
            });
        settings = new ErosUISettingsWindow();
        ws.AddWindow(control);
        ws.AddWindow(settings);
        qtPanel = new ErosUIQtPanelWindow();
        ws.AddWindow(qtPanel);
        qtPanel.IsOpen = uiSettings.QtPanelVisible;
        splash = new ErosUISplashWindow();
        ws.AddWindow(splash);

        HidePrPanels(force: true);   // 切职业瞬间宿主会重弹自带面板，强制压制一次
        ErosUIHotkeyUI.Rebuild();
        APIHelper.重建QT可见性();
    }

    private static void RetryPendingInstall()
    {
        if (!installRetryPending || PromeRotation.Plugin.Instance?.WindowSystem == null) return;
        // UiBuilder.Draw is the UI thread. Never register or remove windows
        // from the combat update callbacks while WindowSystem is drawing.
        // Install starts with Uninstall, which also removes this subscription.
        Install();
    }

    /// <summary>卸载全部窗口与热键面板，并把两份设置落盘。OnExitAcr 调用，与 Install 严格成对。</summary>
    public static void Uninstall()
    {
        ErosUIPerformance.Stop();
        if (installRetryPending)
        {
            installRetryPending = false;
            Svc.PluginInterface.UiBuilder.Draw -= RetryPendingInstall;
        }
        ErosUIHotkeyUI.Uninstall();
        var ws = PromeRotation.Plugin.Instance?.WindowSystem;
        if (ws == null) { control?.Dispose(); control = null; settings = null; qtPanel = null; splash = null; SaveSettings(); return; }

        if (control != null)
        {
            try { ws.RemoveWindow(control); } catch (ArgumentException) { }
            control.Dispose();
            control = null;
        }
        if (settings != null)
        {
            try { ws.RemoveWindow(settings); } catch (ArgumentException) { }
            settings = null;
        }
        if (qtPanel != null)
        {
            try { ws.RemoveWindow(qtPanel); } catch (ArgumentException) { }
            qtPanel = null;
        }
        if (splash != null)
        {
            try { ws.RemoveWindow(splash); } catch (ArgumentException) { }
            splash = null;
        }
        SaveSettings();
    }

    /// <summary>把通用设置与当前职业设置立即写入磁盘。</summary>
    public static void SaveSettings()
    {
        ErosUISettings.Instance.Save();
        ErosUICommonSettings.Instance.Save();
    }

    /// <summary>写入 QT 开关状态，并按联动表把关联的键一并写入。</summary>
    public static void 设置QT(string qtKey, bool 值) => APIHelper.设置QT(qtKey, 值);

    /// <summary>清空后按当前职业与当前模式重新注册 QT，并同步显隐配置到宿主。切换模式后调用。</summary>
    public static void 重建QT可见性() => APIHelper.重建QT可见性();

    /// <summary>宿主清空 QT 注册表后恢复当前职业的 QT 及用户开关状态。</summary>
    public static void RestoreQtIfHostCleared() => APIHelper.RestoreQtIfHostCleared();

    /// <summary>切换 QT 悬浮面板的显示/隐藏。</summary>
    public static void ToggleQtPanel()
    {
        SetQtPanelVisible(!QtPanelVisible);
    }

    /// <summary>QT 悬浮面板的显隐状态；未构建时返回保存的偏好。</summary>
    public static bool QtPanelVisible => qtPanel?.IsOpen ?? ErosUISettings.Instance.QtPanelVisible;

    /// <summary>显示/隐藏 QT 悬浮面板并保存偏好，不重建窗口。</summary>
    public static void SetQtPanelVisible(bool visible) => SetPanelVisibility(qtVisible: visible);

    /// <summary>热键悬浮面板当前是否可见。</summary>
    public static bool HotkeyPanelVisible => ErosUIHotkeyUI.PanelVisible;

    /// <summary>Execute one of the current job's registered hotkeys by name.</summary>
    public static bool ExecuteHotkey(string name) => ErosUIHotkeyUI.ExecuteHotkey(name);

    /// <summary>List the current job's registered hotkeys, including hidden entries.</summary>
    public static string[] GetHotkeyNames() => ErosUIHotkeyUI.GetHotkeyNames();

    /// <summary>显示/隐藏热键悬浮面板并保存偏好，不重建窗口。</summary>
    public static void SetHotkeyPanelVisible(bool visible) => SetPanelVisibility(hotkeyVisible: visible);

    /// <summary>同步切换 QT 与 Hotkey 悬浮面板的显隐状态。</summary>
    public static void ToggleAuxiliaryPanels()
    {
        SetPanelVisibility(!QtPanelVisible, !HotkeyPanelVisible);
    }

    /// <summary>关闭两个辅助面板并保存偏好。</summary>
    public static void CloseAuxiliaryPanels()
    {
        SetPanelVisibility(false, false);
    }

    private static void SetPanelVisibility(bool? qtVisible = null, bool? hotkeyVisible = null)
    {
        var uiSettings = ErosUISettings.Instance;
        var changed = false;
        if (qtVisible is { } qt)
        {
            if (qtPanel != null) qtPanel.IsOpen = qt;
            changed |= uiSettings.QtPanelVisible != qt;
            uiSettings.QtPanelVisible = qt;
        }
        if (hotkeyVisible is { } hotkey)
        {
            ErosUIHotkeyUI.SetPanelVisible(hotkey);
            changed |= uiSettings.HotkeyPanelVisible != hotkey;
            uiSettings.HotkeyPanelVisible = hotkey;
        }
        // A double click changes both panels with one write. Window teardown
        // and rebuild never change the saved preference.
        if (changed) uiSettings.Save();
    }

    /// <summary>打开设置窗口。</summary>
    public static void OpenSettings()
    {
        if (settings != null) settings.IsOpen = true;
    }

    /// <summary>切换设置窗口的打开/关闭（战斗控制条「设置」按钮使用）。</summary>
    public static void ToggleSettings()
    {
        if (settings == null) return;
        settings.IsOpen ^= true;
    }

    /// <summary>Replay the embedded website-style loading splash.</summary>
    public static void ReplaySplash() => splash?.Replay();

    /// <summary>
    /// Persist the loading-animation preference and apply it to the current splash.
    /// Enabling the option affects the next install/replay; changing it does not
    /// unexpectedly cover the settings window with a new splash.
    /// </summary>
    public static void SetLoadingAnimationEnabled(bool enabled)
    {
        var commonSettings = ErosUICommonSettings.Instance;
        commonSettings.EnableLoadingAnimation = enabled;
        commonSettings.Save();

        if (!enabled && splash != null)
            splash.IsOpen = false;
    }

    /// <summary>给宿主 DrawSettings 回调用的入口：绘制一个打开设置窗口的按钮。</summary>
    public static void DrawSettingsEntry()
    {
        if (ImGui.Button($"打开 {ErosUIJobEnv.作者} 设置窗口"))
            OpenSettings();
    }

    // ============================================================
    // === 宿主本体面板压制 ===
    // ============================================================
    private static long _上次HidePrPanels;

    /// <summary>关闭宿主自带的 QT 面板窗口，防止它与本框架的面板同时出现。</summary>
    /// <remarks>
    /// 只能在 UI 线程调用（由控制条每帧触发，内部 1 秒节流）。
    /// 不要挂到 Framework.Update：那会在渲染线程之外改窗口状态，与渲染竞争后
    /// 会导致整个游戏的 ImGui 交互假死。force 参数绕过节流立即执行，供切职业时
    /// 抑制宿主重新弹出的面板。
    /// </remarks>
    public static void HidePrPanels(bool force = false)
    {
        var now = Environment.TickCount64;
        if (!force && now - _上次HidePrPanels < 1000) return;
        _上次HidePrPanels = now;
        try { PromeRotation.Plugin.Instance?.CloseQtWindow(); } catch { /* 宿主未就绪 */ }
    }

    // 存档位置钳制在屏幕工作区内，防止读取越界存档或切换分辨率后窗口跑出屏幕。
    private static Vector2 限制存档位置(Vector2? saved)
    {
        if (saved is not Vector2 v) return new Vector2(32f, 320f);
        var vp = ImGui.GetMainViewport();
        var min = vp.WorkPos + new Vector2(4f);
        var max = vp.WorkPos + vp.WorkSize - new Vector2(124f, 124f);
        if (max.X < min.X) max.X = min.X;
        if (max.Y < min.Y) max.Y = min.Y;
        return new Vector2(Math.Clamp(v.X, min.X, max.X), Math.Clamp(v.Y, min.Y, max.Y));
    }
}
