using ECommons.DalamudServices;
using PromeRotation.UI.HotKey;

namespace ErosUI;

// 热键面板管理：负责悬浮窗的构建、摘除与显隐。
// 面板条目完全由使用方经 ErosUIJobEnv.BuildHotkeys 注入，框架只负责
// 窗口外壳、布局参数、显隐与拖拽排序。
internal static class ErosUIHotkeyUI
{
    private static ErosUIHotkeyPanelWindow? window;

    /// <summary>（重新）构建热键面板。布局/显隐改动后调用；全部条目都被隐藏时不注册空面板。</summary>
    internal static void Rebuild()
    {
        Uninstall();

        var b = BuildHotkeys(includeHidden: false);
        if (b.Entries.Count == 0) return;

        var s = ErosUISettings.Instance;
        window = new ErosUIHotkeyPanelWindow(
            b.Entries,
            s.HotkeyColumns,
            45f * s.HotkeyScalePercent / 100f,
            s.HotkeySpacing);
        try { PromeRotation.Plugin.Instance?.WindowSystem.AddWindow(window); }
        catch { /* 宿主未就绪 */ }
    }

    internal static bool ExecuteHotkey(string name)
    {
        try
        {
            var builder = BuildHotkeys(includeHidden: true);
            foreach (var entry in builder.Entries)
            {
                if (!string.Equals(entry.Name, name, StringComparison.Ordinal)) continue;
                entry.Hotkey.OnClick();
                return true;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, $"ErosUI hotkey trigger failed: {name}");
        }
        return false;
    }

    internal static string[] GetHotkeyNames()
    {
        try
        {
            var entries = BuildHotkeys(includeHidden: true).Entries;
            var names = new string[entries.Count];
            for (var i = 0; i < entries.Count; i++) names[i] = entries[i].Name;
            return names;
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "ErosUI hotkey list build failed");
            return Array.Empty<string>();
        }
    }

    private static ErosUIHotkeyBuilder BuildHotkeys(bool includeHidden)
    {
        var builder = new ErosUIHotkeyBuilder(ErosUISettings.Instance, includeHidden);
        ErosUIJobEnv.BuildHotkeys?.Invoke(builder);
        return builder;
    }

    /// <summary>从宿主 WindowSystem 摘除热键面板。OnExitAcr 调用。</summary>
    internal static void Uninstall()
    {
        if (window == null) return;
        try { PromeRotation.Plugin.Instance?.WindowSystem.RemoveWindow(window); }
        catch (ArgumentException) { /* 窗口可能已被宿主移除 */ }
        window = null;
    }

    /// <summary>热键面板当前是否可见，未构建时为 false。</summary>
    internal static bool PanelVisible => window?.IsOpen ?? false;

    /// <summary>显示/隐藏热键面板。只切换显隐，不重建窗口。</summary>
    internal static void SetPanelVisible(bool visible)
    {
        if (window == null) return;
        window.IsOpen = visible;
    }

}
