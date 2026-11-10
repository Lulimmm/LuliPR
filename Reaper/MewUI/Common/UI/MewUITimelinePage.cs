using System.Numerics;
using Dalamud.Bindings.ImGui;
using LuliPR.Common;

namespace MewUI;

internal static class MewUITimelinePage
{
    private static string? pendingDeleteGuid;

    internal static void Draw()
    {
        var rows = BundledTimelineSync.GetRows();
        var checking = BundledTimelineSync.IsChecking;
        SettingRow.SectionTitle("时间轴");
        ImGui.TextColored(SimplePalette.TextSecondary, $"共 {rows.Length} 份 · 登录后首次启用 ACR 时自动检查版本");
        ImGui.Spacing();
        ImGui.BeginDisabled(checking || rows.Length == 0);
        if (Button(checking ? "正在检查…" : "检查全部版本", 132f))
            BundledTimelineSync.RequestCloudCheck();
        ImGui.EndDisabled();
        ImGui.Spacing();
        ImGui.TextDisabled("更新后重新加载时间轴生效。删除的时间轴可在本页恢复。");
        ImGui.Spacing();

        var width = ImGui.GetContentRegionAvail().X;
        var columns = width >= 620f ? 2 : 1;
        var openDeletePopup = false;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, 8f));
        if (ImGui.BeginTable("##mew_timeline_cards", columns, ImGuiTableFlags.SizingStretchSame))
        {
            foreach (var row in rows)
            {
                ImGui.TableNextColumn();
                ImGui.PushID(row.Guid);
                if (DrawCard(row, MathF.Max(1f, ImGui.GetContentRegionAvail().X), checking))
                {
                    pendingDeleteGuid = row.Guid;
                    openDeletePopup = true;
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        ImGui.PopStyleVar();

        // Open and draw the popup in the same ID scope, outside the card/table IDs.
        if (openDeletePopup) ImGui.OpenPopup("删除时间轴##mew_timeline");
        DrawDeleteConfirmation(rows);
    }

    private static bool DrawCard(BundledTimelineSync.TimelineRow row, float width, bool checking)
    {
        const float height = 204f;
        const float padding = 14f;
        var pos = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(pos + new Vector2(3f, 4f), pos + new Vector2(width, height),
            SimplePalette.ToU32(new Vector4(0.08f, 0.04f, 0.16f, 0.2f)), 8f);
        draw.AddRectFilled(pos, pos + new Vector2(width - 3f, height - 4f),
            SimplePalette.ToU32(SimplePalette.WithAlpha(SimplePalette.FrameBg, 0.66f)), 8f);
        draw.AddRectFilled(pos, pos + new Vector2(width - 3f, 36f),
            SimplePalette.ToU32(SimplePalette.WithAlpha(SimplePalette.Accent, 0.24f)),
            8f, ImDrawFlags.RoundCornersTop);
        draw.AddRect(pos, pos + new Vector2(width - 3f, height - 4f),
            SimplePalette.ToU32(SimplePalette.BorderStrong), 8f);

        ImGui.BeginGroup();
        var dot = pos + new Vector2(padding, 15f);
        draw.AddRectFilled(dot, dot + new Vector2(6f), SimplePalette.ToU32(SimplePalette.PrimaryHover));
        ImGui.SetCursorScreenPos(pos + new Vector2(padding + 14f, 10f));
        ImGui.TextColored(SimplePalette.TextPrimary, row.Name);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(row.Guid);

        var contentWidth = width - padding * 2f;
        DrawVersion(pos + new Vector2(padding, 50f), "本地版本", row.LocalVersion, row.IsDeleted);
        DrawVersion(pos + new Vector2(padding + contentWidth * 0.52f, 50f),
            "GitHub 版本", row.CloudVersion, false);

        ImGui.SetCursorScreenPos(pos + new Vector2(padding, 112f));
        ImGui.TextColored(StatusColor(row.Status), ShortStatus(row.Status));
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(row.Status);

        var requestDelete = false;
        ImGui.SetCursorScreenPos(pos + new Vector2(padding, 155f));
        ImGui.BeginDisabled(checking || row.IsWorking);
        if (Button("检查 / 更新", 110f))
            BundledTimelineSync.RequestCloudCheck(row.Guid);
        ImGui.SameLine(0f, 8f);
        if (row.IsDeleted)
        {
            if (Button("恢复", 62f, secondary: true))
                BundledTimelineSync.RequestRestore(row.Guid);
        }
        else
        {
            ImGui.BeginDisabled(row.LocalVersion == "未安装");
            requestDelete = Button("删除", 62f, secondary: true);
            ImGui.EndDisabled();
        }
        ImGui.EndDisabled();
        ImGui.EndGroup();
        ImGui.SetCursorScreenPos(pos);
        ImGui.Dummy(new Vector2(width, height));
        return requestDelete;
    }

    private static void DrawDeleteConfirmation(BundledTimelineSync.TimelineRow[] rows)
    {
        if (!ImGui.BeginPopupModal("删除时间轴##mew_timeline", ImGuiWindowFlags.AlwaysAutoResize))
            return;
        var index = Array.FindIndex(rows, item => item.Guid == pendingDeleteGuid);
        if (index < 0)
        {
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
            return;
        }
        var row = rows[index];
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 320f);
        ImGui.TextWrapped($"删除本地“{row.Name}”？");
        ImGui.Spacing();
        ImGui.TextWrapped("删除后不会自动重新安装，可以在时间轴页面恢复。");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (Button("确认删除", 100f))
        {
            BundledTimelineSync.RequestDelete(row.Guid);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (Button("取消", 82f, secondary: true))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private static void DrawVersion(Vector2 pos, string label, string version, bool disabled)
    {
        ImGui.SetCursorScreenPos(pos);
        ImGui.TextColored(SimplePalette.TextSecondary, label);
        ImGui.SetCursorScreenPos(pos + new Vector2(0f, 24f));
        ImGui.TextColored(disabled ? SimplePalette.TextDisabled : SimplePalette.PrimaryHover, version);
    }

    private static Vector4 StatusColor(string status)
        => status.Contains("失败", StringComparison.Ordinal) ? new Vector4(1f, 0.66f, 0.42f, 1f)
            : status.Contains("已更新", StringComparison.Ordinal) || status.Contains("最新", StringComparison.Ordinal)
                ? SimplePalette.PrimaryHover : SimplePalette.TextSecondary;

    private static string ShortStatus(string status)
    {
        if (status.Contains("失败", StringComparison.Ordinal)) return "检查失败，点击检查可重试";
        if (status.Contains("已是最新", StringComparison.Ordinal)) return "已是最新版本";
        if (status.Contains("已更新", StringComparison.Ordinal)) return "更新完成，请重新加载时间轴";
        if (status.StartsWith("正在", StringComparison.Ordinal)) return "正在检查…";
        if (status.Contains("已删除", StringComparison.Ordinal)) return "已删除 · 自动更新已暂停";
        if (status.Contains("内容不同", StringComparison.Ordinal)) return "版本相同 · 保留本地内容";
        return status;
    }

    private static bool Button(string label, float width, bool secondary = false)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, secondary ? SimplePalette.FrameBg
            : SimplePalette.WithAlpha(SimplePalette.Accent, 0.68f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.PrimaryHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.PrimaryActive);
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 7f);
        var clicked = ImGui.Button(label, new Vector2(width, 30f));
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
        return clicked;
    }
}
