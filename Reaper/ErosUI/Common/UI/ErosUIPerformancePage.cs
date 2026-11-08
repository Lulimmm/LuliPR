using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace ErosUI;

internal static class ErosUIPerformancePage
{
    internal const string TabLabel = "性能分析";
    private static readonly string[] WindowNames = ["像素小人", "设置窗口", "QT 面板", "Hotkey 面板", "载入动画"];
    private static readonly string[,] Rows = new string[WindowNames.Length, 4];
    private static double nextRefresh;
    private static string frameSummary = string.Empty, allocationSummary = string.Empty;

    internal static void Draw()
    {
        if (!ErosUIPerformance.CanView) return;
        SettingRow.SectionTitle("UI 性能分析");
        ImGui.TextColored(ErosUIPerformance.IsEnabled ? SimplePalette.StateRunning : SimplePalette.TextSecondary,
            ErosUIPerformance.IsEnabled ? "正在采样" : "采样已关闭");
        if (SettingRow.Button(ErosUIPerformance.IsEnabled ? "停止分析" : "开始分析"))
        {
            if (ErosUIPerformance.IsEnabled) ErosUIPerformance.Stop();
            else ErosUIPerformance.Start();
            nextRefresh = 0d;
        }
        ImGui.SameLine();
        if (SettingRow.Button("清空记录"))
        {
            ErosUIPerformance.Clear();
            nextRefresh = 0d;
        }
        ImGui.TextWrapped("默认关闭，每次加载 ACR 后需手动开始。关闭设置窗口后仍可采样其他可见面板。");
        ImGui.TextWrapped("统计本 ACR 的 UI Draw() CPU 耗时及当前线程托管分配；不代表 GPU 或游戏总占用。设置窗口数据包含本页。\n表格每 0.5 秒刷新，均值与峰值自本次开始或清空后累计。");

        var now = ImGui.GetTime();
        if (now >= nextRefresh)
        {
            RefreshText();
            nextRefresh = now + 0.5d;
        }
        ImGui.Spacing();
        ImGui.TextUnformatted(frameSummary);
        ImGui.TextUnformatted(allocationSummary);

        if (ImGui.BeginTable("##ui_performance_windows", 5,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("窗口");
            ImGui.TableSetupColumn("平均 ms/次");
            ImGui.TableSetupColumn("峰值 ms/次");
            ImGui.TableSetupColumn("分配 B/次");
            ImGui.TableSetupColumn("采样次数");
            ImGui.TableHeadersRow();
            for (var i = 0; i < WindowNames.Length; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(WindowNames[i]);
                for (var column = 0; column < 4; column++)
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Rows[i, column]);
                }
            }
            ImGui.EndTable();
        }
        ImGui.Spacing();
        ImGui.TextUnformatted("最近 120 个采样帧：UI 合计 CPU 耗时");
        DrawHistory();
    }

    private static void RefreshText()
    {
        frameSummary = $"采样帧数：{ErosUIPerformance.FrameCount}    UI 合计平均：{ErosUIPerformance.AverageFrameMilliseconds:F3} ms/帧";
        allocationSummary = $"UI 合计峰值：{ErosUIPerformance.PeakFrameMilliseconds:F3} ms/帧    平均分配：{ErosUIPerformance.AverageFrameBytes:F0} B/帧";
        for (var i = 0; i < WindowNames.Length; i++)
        {
            var sample = ErosUIPerformance.GetSnapshot((ErosUIRenderSection)i);
            Rows[i, 0] = sample.AverageMilliseconds.ToString("F3");
            Rows[i, 1] = sample.PeakMilliseconds.ToString("F3");
            Rows[i, 2] = sample.AverageBytes.ToString("F0");
            Rows[i, 3] = sample.Calls.ToString();
        }
    }

    private static void DrawHistory()
    {
        var size = new Vector2(MathF.Max(1f, ImGui.GetContentRegionAvail().X), 110f);
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, SimplePalette.ToU32(SimplePalette.FrameBg), 6f);
        var count = ErosUIPerformance.HistoryCount;
        var peak = 0.1f;
        for (var i = 0; i < count; i++) peak = MathF.Max(peak, ErosUIPerformance.HistoryAt(i));
        var previous = Vector2.Zero;
        var color = SimplePalette.ToU32(SimplePalette.PrimaryHover);
        for (var i = 0; i < count; i++)
        {
            var point = new Vector2(min.X + 4f + (size.X - 8f) * i / 119f,
                max.Y - 4f - (size.Y - 8f) * ErosUIPerformance.HistoryAt(i) / peak);
            if (i > 0) drawList.AddLine(previous, point, color, 1.5f);
            previous = point;
        }
        ImGui.Dummy(size);
    }
}
