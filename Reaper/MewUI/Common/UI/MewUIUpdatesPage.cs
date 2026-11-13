using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace MewUI;

internal static class MewUIUpdatesPage
{
    private static readonly HashSet<int> Collapsed = new();
    // Add one Notice per release. Each entry draws as its own card, newest first.
    private static readonly Notice[] Notices =
    [
        new(
            "1.4.3", "2026-11-14", "修复获取凭证按钮",
            "修复累计使用满 24 小时后，点击获取凭证未发送聊天消息的问题。改用游戏聊天命令入口，并在游戏主线程执行。",
            "#UI #USAGE #FIX"),
        new(
            "1.4.2", "2026-11-13", "Reaper 战斗逻辑回退与时间轴更新",
            "Reaper 战斗逻辑回退到 1.4.0，并补齐 PR 侧技能近期使用记录和周围敌人数检测接口。\n\n" +
            "更新伊甸近战减伤与伊甸镰刀时间轴。",
            "#RPR #LOGIC #TIMELINE"),
        new(
            "1.4.0", "2026-11-11", "时间轴直接云端更新",
            "直接比较 GitHub 与 PureTimelines 中的时间轴版本并更新，DLL 目录不再附带时间轴副本。\n\n" +
            "删除后可从云端恢复；自动清理旧版随包目录、更新备份和失效临时文件。",
            "#TIMELINE #CLEANUP"),
        new(
            "1.3.10", "2026-11-11", "修复时间轴版本检查误报",
            "时间轴内容比较忽略 UTF BOM 和换行格式差异，避免 Windows 发布包与 GitHub 原始文件仅因编码格式不同而误报为本地修改。",
            "#TIMELINE #FIX"),
        new(
            "1.3.9", "2026-11-11", "时间轴云端同步与UI",
            "内置伊甸近战减伤、伊甸镰刀、妖星镰刀和绝欧镰刀，时间轴初始版本统一为 1.0.1。\n\n" +
            "登录后首次启用 ACR 时检查 GitHub 时间轴版本，版本不一致时自动更新；覆盖本地修改前保留备份。\n\n" +
            "新增时间轴管理页，可查看版本、手动检查、删除和恢复时间轴。",
            "#TIMELINE #UI #UPDATE"),
        new(
            "1.3.8", "2026-11-10", "修复快速拖动像素小人时被裁切",
            "调整像素小人贴图的绘制裁剪区域，使其跟随窗口当前位置，修复快速拖动时角色部分显示或消失的问题。",
            "#UI #PET"),
        new(
            "1.3.7", "2026-11-09", "面板显隐记忆与 UI 性能优化",
            "QT 与 Hotkey 面板按职业记住显隐状态，重新上线、加载 ACR 或重建面板时自动恢复。\n\n" +
            "新增 UI 性能分析页，支持手动采样 CPU 耗时、托管分配和峰值。\n\n" +
            "优化 QT、Hotkey、侧栏和时长页面的绘制缓存，减少每帧内存分配和重复计算。",
            "#UI #QT #HOTKEY #PERF"),
        new(
            "1.3.6", "2026-11-07", "修复像素小人消失",
            "修复像素小人纹理加载异常后持续空白的问题，增加加载重试与有效画面兜底。\n\n" +
            "修复分辨率变化时窗口越界、宿主未就绪时窗口未注册的问题。",
            "#UI #PET #FIX"),
        new(
            "1.3.5", "2026-11-05", "使用时长与 Hook 设置优化",
            "新增 ACR 总使用时长数码时钟，累计达到 24 小时后可获取凭证。\n\n" +
            "优化 Reaper 设置排版：起手爆发药与突进并排显示，并增加 Hook 设置分节。",
            "#UI #USAGE #RPR"),
        new(
            "1.3.2", "2026-11-03", "更新载入动画",
            "按网页原版对齐载入动画的背景切换、圆环、横条和进度显示。\n\n" +
            "同步角色动画帧与播放节奏，并使用无损背景素材。",
            "#UI #LOADING"),
        new(
            "1.3.1", "2026-11-02", "字体崩溃修复与体积优化",
            "移除导致字体图集崩溃的自定义字体和鼠标样式。\n\n" +
            "压缩启动画面、背景和像素小人资源，大幅缩小 DLL 体积。\n\n" +
            "更新通告改为内置只读卡片，每次更新独立显示。",
            "#UI #FIX #SIZE"),
        new(
            "1.3.0", "2026-11-02", "QT 控制与时间轴修复",
            "Ninja：关闭技能 QT 后，其他技能中对应的等待和资源保留条件同步停用。\n\n" +
            "Ninja：关闭命水 QT 后，不再释放命水（16489）。\n\n" +
            "Reaper：修复 newQT 与 QT Trigger 只显示 JSON 文本、无法显示专用编辑控件的问题。",
            "#NIN #RPR #QT #TRIGGER"),
    ];
    private static readonly CardLayout[] Layouts = new CardLayout[Notices.Length];

    private readonly record struct CardLayout(bool Ready, ImFontPtr Font, float FontSize,
        float WrapWidth, float HeaderHeight, float DateHeight, float Height);

    internal static void Draw()
    {
        ImGui.TextColored(SimplePalette.PrimaryHover, "LATEST POSTS / ACR 更新通告");
        ImGui.Spacing();

        var width = ImGui.GetContentRegionAvail().X;
        var columns = Math.Clamp((int)((width + 20f) / 260f), 1, 4);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(10f, 10f));
        if (ImGui.BeginTable("##update_cards", columns, ImGuiTableFlags.SizingStretchSame))
        {
            for (var i = 0; i < Notices.Length; i++)
            {
                ImGui.TableNextColumn();
                ImGui.PushID(i);
                DrawCard(Notices[i], i, MathF.Max(1f, ImGui.GetContentRegionAvail().X - 6f));
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    private static void DrawCard(Notice notice, int index, float width)
    {
        const float padding = 16f;
        var headerHeight = ImGui.GetFrameHeight() + 8f;
        var wrapWidth = MathF.Max(1f, width - padding * 2f);
        var collapsed = Collapsed.Contains(index);
        var height = collapsed
            ? headerHeight
            : GetCardHeight(notice, index, wrapWidth, headerHeight);

        var pos = ImGui.GetCursorScreenPos();
        var max = pos + new Vector2(width, height);
        var day = MewUICommonSettings.Instance.UIMode == SettingsUIMode.Day;
        var panel = day ? new Vector4(0.55f, 0.47f, 0.77f, 0.50f) : new Vector4(0.20f, 0.16f, 0.36f, 0.75f);
        var header = day ? new Vector4(0.44f, 0.36f, 0.68f, 0.85f) : new Vector4(0.16f, 0.12f, 0.29f, 0.90f);
        var border = day ? new Vector4(0.49f, 0.36f, 0.71f, 1f) : new Vector4(0.35f, 0.27f, 0.55f, 1f);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(pos + new Vector2(6f), max + new Vector2(6f), 0x7368263cu);
        dl.AddRectFilled(pos, max, SimplePalette.ToU32(panel));
        dl.AddRectFilled(pos, new Vector2(max.X, pos.Y + headerHeight), SimplePalette.ToU32(header));
        dl.AddRect(pos, max, SimplePalette.ToU32(border), 0f, ImDrawFlags.None, 2f);
        dl.AddLine(new Vector2(pos.X, pos.Y + headerHeight),
            new Vector2(max.X, pos.Y + headerHeight), SimplePalette.ToU32(border), 2f);

        ImGui.BeginGroup();
        ImGui.SetCursorScreenPos(pos + new Vector2(14f, (headerHeight - 9f) * 0.5f));
        dl.AddRectFilled(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(9f),
            SimplePalette.ToU32(SimplePalette.Primary));
        ImGui.SetCursorScreenPos(pos + new Vector2(34f, (headerHeight - ImGui.GetTextLineHeight()) * 0.5f));
        dl.PushClipRect(pos, new Vector2(max.X - 30f, pos.Y + headerHeight), true);
        ImGui.TextUnformatted(notice.Caption);
        dl.PopClipRect();
        ImGui.SetCursorScreenPos(new Vector2(max.X - 27f, pos.Y + 4f));
        if (ImGui.SmallButton(collapsed ? "+" : "-"))
        {
            if (!Collapsed.Add(index)) Collapsed.Remove(index);
        }

        if (!collapsed)
        {
            ImGui.SetCursorScreenPos(pos + new Vector2(padding, headerHeight + padding));
            ImGui.BeginGroup();
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
            ImGui.TextUnformatted(notice.Title);
            if (notice.Date.Length > 0) ImGui.TextDisabled(notice.Date);
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextSecondary);
            ImGui.TextUnformatted(notice.Body);
            ImGui.PopStyleColor();
            if (notice.TagText.Length > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(SimplePalette.PrimaryHover, notice.TagText);
            }
            ImGui.PopTextWrapPos();
            ImGui.EndGroup();
        }
        ImGui.SetCursorScreenPos(pos);
        ImGui.Dummy(new Vector2(width + 6f, height + 6f));
        ImGui.EndGroup();
    }

    private static float GetCardHeight(Notice notice, int index, float wrapWidth, float headerHeight)
    {
        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize();
        var dateHeight = notice.Date.Length == 0 ? 0f : ImGui.GetTextLineHeightWithSpacing();
        var previous = Layouts[index];
        if (previous.Ready && previous.Font.Equals(font) && previous.FontSize == fontSize
            && previous.WrapWidth == wrapWidth && previous.HeaderHeight == headerHeight
            && previous.DateHeight == dateHeight)
            return previous.Height;

        var titleHeight = ImGui.CalcTextSize(notice.Title, false, wrapWidth).Y;
        var bodyHeight = ImGui.CalcTextSize(notice.Body, false, wrapWidth).Y;
        var tagHeight = notice.TagText.Length == 0 ? 0f : ImGui.CalcTextSize(notice.TagText, false, wrapWidth).Y + 12f;
        var height = MathF.Max(210f, headerHeight + 32f + titleHeight + bodyHeight + tagHeight + dateHeight + 16f);
        Layouts[index] = new CardLayout(true, font, fontSize, wrapWidth, headerHeight, dateHeight, height);
        return height;
    }

    private sealed record Notice(string Version, string Date, string Title, string Body, string TagText)
    {
        public string Caption { get; } = "UPDATE " + Version;
    }
}
