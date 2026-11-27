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
            "1.5.7", "2026-11-28", "Ninja 爆发药与身位颜色刷新",
            "新增 Ninja 自动爆发药 QT，默认关闭。沿用 Reaper 的 0612 普通判断，以介毒之术冷却大于 2.5 秒且小于 5 秒作为喝药窗口，并检查药品冷却与 GCD 插入窗口；结印、天地人和延后爆发期间暂停自动用药。\n\n" +
            "Ninja 身位固定扇区参考 Reaper 持续刷新红绿颜色，与进度扇区同步随走位变色；仅调整颜色刷新。",
            "#NIN #POTION #POSITIONAL #UI"),
        new(
            "1.5.6", "2026-11-26", "Ninja 战斗诊断与爆发控制",
            "完善 Ninja 战斗 Debug，显示目标、队列、忍术与能力技额度及各技能等待原因，便于排查目标消失、转场和爆发衔接中断。\n\n" +
            "预备水遁改为手动 Hotkey，每次点击提交一组结印，移除原自动预备水遁 QT；起手倒数流程保持独立。\n\n" +
            "新增“延后爆发”QT：暂停水遁、百雷铳、背刺、天地人、三段、分身和生杀；忍术达到 1.8 层时使用雷遁防止溢出，并消耗其雷爪。固定起手和手动 Hotkey 保持各自行为。\n\n" +
            "移除水遁、冰晶乱流专用的独立重复回包去重，保留忍术后正常单插限制。",
            "#NIN #DEBUG #HOTKEY #QT"),
        new(
            "1.5.5", "2026-11-23", "像素小人兼容性修复与资源压缩",
            "像素小人与加载动画统一使用内嵌 PNG，避免 WebP 解码组件异常导致只显示提示框、角色空白。静态兜底帧同步切换为 PNG。\n\n" +
            "三张不透明背景保持原始分辨率，改用高质量 JPEG 压缩；所有图片仍内嵌 DLL，像素小人的透明 PNG 帧保持原样。",
            "#UI #PET #FIX"),
        new(
            "1.5.4", "2026-11-23", "像素小人绘制兜底与纹理诊断",
            "当当前动画帧和上一有效帧都无法获取时，像素小人会尝试使用内置静态帧绘制，减少单帧资源异常导致角色空白。\n\n" +
            "UI Debug 保留最近一次纹理失效原因和重试倒计时，便于区分资源加载、纹理句柄失效及兜底帧不可用。",
            "#UI #PET #FIX #DEBUG"),
        new(
            "1.5.3", "2026-11-22", "目标禁用与像素小人诊断",
            "设置页新增 UI Debug 总开关；仅检测到提示框命中且该帧未提交贴图绘制命令时，保留并列出当前帧、回退贴图和资源加载原因。\n\n" +
            "Reaper 时间轴新增按目标 DataID 禁用或恢复死亡之影的动作节点，并将规则应用于轮转与起手。\n\n" +
            "修正大丰收的状态剩余时间判断，允许状态在当前 GCD 结束前到期时继续释放。",
            "#RPR #UI #DEBUG #TIMELINE"),
        new(
            "1.5.2", "2026-11-21", "\u65F6\u95F4\u8F74\u53EF\u7528\u6027\u6807\u8BB0",
            "\u65F6\u95F4\u8F74\u9875\u9762\u65B0\u589E\u53EF\u7528\u3001\u4E0D\u53EF\u7528\u548C\u672A\u6D4B\u8BD5\u6807\u8BB0\uFF0C\u5E76\u4E3A\u4F0A\u7538\u9545\u5200\u548C\u5996\u661F\u9545\u5200\u8BBE\u7F6E\u5BF9\u5E94\u72B6\u6001\u3002",
            "#TIMELINE #UI"),
        new(
            "1.5.1", "2026-11-17", "\u9644\u4F53\u671F\u95F4\u6B7B\u4EA1\u70D9\u5370\u5237\u65B0\u4F18\u5148\u7EA7",
            "\u795E\u79D8\u73AF\u51B7\u5374 4 \u79D2\u5185\uFF0C\u82E5\u76EE\u6807\u6B7B\u4EA1\u70D9\u5370\u5269\u4F59\u4E0D\u8DB3 22 \u79D2\uFF0C\u9644\u4F53 GCD \u4F18\u5148\u4F7F\u7528 24378 \u5237\u65B0\u70D9\u5370\uFF0C\u518D\u4F7F\u7528\u9547\u9B42\u65A9\u7B49\u9644\u4F53\u6280\u80FD\u3002",
            "#RPR #ENSHROUD #FIX"),
        new(
            "1.5.0", "2026-11-17", "\u64AD\u9B42\u79CD\uFF08\u6536\u83B7\u6708\uFF09Hotkey \u4FEE\u590D",
            "\u4FEE\u590D\u70B9\u51FB\u64AD\u9B42\u79CD Hotkey \u540E\u6536\u83B7\u6708\u4E0D\u4F1A\u91CA\u653E\u7684\u95EE\u9898\u3002\u4EC5\u5728\u81EA\u8EAB\u62E5\u6709 2594 \u72B6\u6001\u65F6\uFF0C\u5F3A\u5236\u63D0\u4EA4 24388u \u5E76\u81EA\u52A8\u9009\u62E9\u8303\u56F4\u5185\u6700\u8FD1\u7684\u6709\u6548\u654C\u4EBA\u3002\u540C\u6B65\u66F4\u65B0\u4F0A\u7538\u9561\u5200\u65F6\u95F4\u8F74\u89E6\u53D1\u3002",
            "#RPR #HOTKEY #TIMELINE"),
        new(
            "1.4.10", "2026-11-17", "移除 M12S 近战减伤时间轴",
            "从时间轴管理与云端同步清单中移除 M12S 近战减伤时间轴。",
            "#TIMELINE #M12S"),
        new(
            "1.4.9", "2026-11-17", "Reaper 爆发窗口与时间轴调整",
            "调整 Enshroud GCD 的团辅窗口死亡烙印阈值至 22 秒。BloodStalk 的暴食两 GCD 冷却判断改用 PR SDK 原生冷却查询，并保持 AE 的固定 5 秒条件。更新伊甸镰刀时间轴，并移除伊甸近战减伤时间轴登记。",
            "#RPR #BLOODSTALK #ENSHROUD #TIMELINE"),
        new(
            "1.4.8", "2026-11-16", "TOP 绝境战牵制规划",
            "新增 TOP 分阶段牵制规划，按 D1/D2 预设启用对应节点。时间轴中未启用的节点不加入预设，并标记为“用户自选”，可由玩家自行勾选。",
            "#TOP #FEINT #UI"),
        new(
            "1.4.7", "2026-11-16", "牵制规划修复与调试完善",
            "修复 FRU 后续阶段牵制不触发的问题，完善序列防重、延迟和重开清零，保留 D1/D2 自动切换。\n\n" +
            "新增读条与技能效果计数、节点等待原因和实际释放确认，过滤友方读条。\n\n" +
            "调整 Reaper 神秘环、夜游魂衣的释放条件与双附体窗口判断。",
            "#FRU #FEINT #DEBUG #RPR"),
        new(
            "1.4.6", "2026-11-15", "牵制规划副本分组与排版优化",
            "牵制规划地图按零式副本与绝境战分组，地图选择按钮改为紧凑布局，并简化页面标题。",
            "#UI #FEINT #LAYOUT"),
        new(
            "1.4.5", "2026-11-15", "FRU 牵制规划与职能联动",
            "新增 FRU 分阶段牵制规划，进入对应副本后按时间轴 D1/D2 职能自动启用减伤轴；未选择职能时默认使用 D1。调整各阶段节点顺序。",
            "#FRU #FEINT #TIMELINE"),
        new(
            "1.4.4", "2026-11-15", "Reaper 战斗逻辑与联动修复",
            "修正 BloodStalk 对暴食的两 GCD 冷却联动，按 AE 的固定 2500ms GCD 窗口和可释放状态判断。同步调整 Reaper 技能就绪、目标状态与起手身位确认逻辑，并更新伊甸近战减伤时间轴。",
            "#RPR #LOGIC #TIMELINE"),
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
