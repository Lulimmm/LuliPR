
namespace MewUI;

// MewUI 设置窗口（SettingsWindowBase 子类，多职业共用同一窗口类）。
// 标题按职业环境注入（"{作者} {JobName}设置"），页签结构各职业一致：
// 基础设置 / Hotkey / QT面板，末位「主题」页由基类提供（夜间/日间）。
// 「主题」页为多职业通用内容（MewUICommonSettings），其余页读当前职业数据。
public sealed class MewUISettingsWindow : SettingsWindowBase
{

    public MewUISettingsWindow() : base($"{MewUIJobEnv.作者} {MewUIJobEnv.JobName}设置")
    {
        // 三项 Allow 全关 → Dalamud 不再往标题栏注入「窗口设置」齿轮，
        // 右上角只剩关闭键（ImGui 自绘，不受影响）
        AllowPinning = false;
        AllowClickthrough = false;
        AllowBackgroundBlur = false;
    }

    // 页签固定, 末位「主题」页由基类追加
    private static readonly string[] tabs = ["基础设置", "Hotkey", "QT面板", "更新通告", "使用时长", "时间轴"];

    private static readonly string[] tabsWithDebug = [..tabs, "Debug"];
    private static readonly string[] tabsWithPerformance = [..tabs, MewUIPerformancePage.TabLabel];
    private static readonly string[] tabsWithDebugAndPerformance = [..tabsWithDebug, MewUIPerformancePage.TabLabel];

    protected override string[] Tabs => MewUIJobEnv.DrawDebugSettings == null
        ? MewUIPerformance.CanView ? tabsWithPerformance : tabs
        : MewUIPerformance.CanView ? tabsWithDebugAndPerformance : tabsWithDebug;

    protected override void DrawTabContent(int tabIndex)
    {
        var currentTabs = Tabs;
        if (tabIndex >= 0 && tabIndex < currentTabs.Length && currentTabs[tabIndex] == MewUIPerformancePage.TabLabel)
        {
            MewUIPerformancePage.Draw();
            return;
        }
        if (tabIndex >= 0 && tabIndex < currentTabs.Length && currentTabs[tabIndex] == "时间轴")
        {
            MewUITimelinePage.Draw();
            return;
        }
        if (tabIndex >= 0 && tabIndex < currentTabs.Length && currentTabs[tabIndex] == "Debug")
        {
            MewUIJobEnv.DrawDebugSettings?.Invoke();
            return;
        }
        switch (tabIndex)
        {
            case 0: MewUISettingsUI.DrawGeneral(); break;
            case 1: MewUISettingsUI.DrawHotkey(); break;
            case 2: MewUISettingsUI.DrawQtPanel(); break;
            case 3: MewUIUpdatesPage.Draw(); break;
            case 4: MewUIUsagePage.Draw(); break;
        }
    }
}
