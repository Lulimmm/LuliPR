using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace MewUI;

internal static class MewUIDebugPage
{
    internal const string TabLabel = "UI Debug";

    internal sealed record Snapshot(
        long DrawCount,
        int Frame,
        double UpdatedAt,
        bool AcrActive,
        bool PositionLocked,
        bool HitAreaHovered,
        string PetState,
        string Asset,
        Vector2 WindowPosition,
        Vector2 MousePosition,
        string ResourceStatus,
        string CurrentTextureStatus,
        int CurrentTextureWidth,
        int CurrentTextureHeight,
        string FallbackTextureStatus,
        int FallbackTextureWidth,
        int FallbackTextureHeight,
        bool UsedFallback,
        bool DrawSubmitted,
        string DrawResult);

    private static bool enabled;
    private static long drawCount;
    private static Snapshot? latest;
    private static Snapshot? detectedNoDraw;

    internal static bool Enabled => enabled;
    internal static Snapshot? Latest => latest;

    internal static void SetEnabled(bool value)
    {
        if (enabled == value) return;
        enabled = value;
        if (enabled)
        {
            latest = null;
            detectedNoDraw = null;
            drawCount = 0;
            MewUILayer.ClearResourceDiagnostics();
        }
    }

    internal static void BeginDraw(
        int frame, double now, bool acrActive, bool positionLocked, bool hovered,
        string petState, string asset, Vector2 windowPosition, Vector2 mousePosition)
    {
        if (!enabled) return;
        latest = new Snapshot(
            ++drawCount, frame, now, acrActive, positionLocked, hovered, petState, asset,
            windowPosition, mousePosition, "读取中", "未检查", 0, 0,
            "未检查", 0, 0, false, false, "已进入 Draw，正在解析纹理");
    }

    internal static void CompleteDraw(
        string resourceStatus,
        string currentTextureStatus, int currentWidth, int currentHeight,
        string fallbackTextureStatus, int fallbackWidth, int fallbackHeight,
        bool usedFallback, bool drawSubmitted, string drawResult)
    {
        if (!enabled || latest == null) return;
        var completed = latest with
        {
            ResourceStatus = resourceStatus,
            CurrentTextureStatus = currentTextureStatus,
            CurrentTextureWidth = currentWidth,
            CurrentTextureHeight = currentHeight,
            FallbackTextureStatus = fallbackTextureStatus,
            FallbackTextureWidth = fallbackWidth,
            FallbackTextureHeight = fallbackHeight,
            UsedFallback = usedFallback,
            DrawSubmitted = drawSubmitted,
            DrawResult = drawResult,
        };
        latest = completed;
        if (completed.HitAreaHovered && !completed.DrawSubmitted)
            detectedNoDraw = completed;
    }

    internal static void Draw()
    {
        SettingRow.SectionTitle("UI 绘制诊断");

        var enableDiagnostics = enabled;
        if (ImGui.Checkbox("启用诊断总开关", ref enableDiagnostics))
            SetEnabled(enableDiagnostics);

        ImGui.TextWrapped("关闭时不采集像素小人绘制和纹理加载状态。开启后，数据仅保留在本次运行内。");
        ImGui.Separator();

        var hasControl = MewUIFramework.ControlWindowExists;
        var controlOpen = MewUIFramework.ControlWindowOpen;
        var installPending = MewUIFramework.InstallRetryPending;
        ImGui.TextUnformatted($"控制窗口实例：{(hasControl ? "存在" : "不存在")}    窗口状态：{(controlOpen ? "打开" : "关闭")}");
        if (installPending)
            ImGui.TextWrapped("窗口注册正在等待宿主 WindowSystem 就绪。");

        if (latest == null)
        {
            ImGui.TextWrapped(enabled
                ? "尚无像素小人 Draw 记录；原因分析仅在检测到提示框命中且贴图命令未提交时显示。"
                : "诊断总开关关闭，当前未采集绘制状态。");
            return;
        }

        var snapshot = latest;
        var age = Math.Max(0d, ImGui.GetTime() - snapshot.UpdatedAt);
        if (age > 1.5d)
            ImGui.TextWrapped($"最近记录距今 {age:F1}s；以下贴图信息属于最后一次 Draw。" );
        else
            ImGui.TextUnformatted($"最近绘制：持续进入 Draw    帧 {snapshot.Frame}    诊断调用 {snapshot.DrawCount}");

        ImGui.TextUnformatted($"ACR：{(snapshot.AcrActive ? "开启" : "关闭")}    自动移动：{(snapshot.PositionLocked ? "锁定" : "解锁")}    命中区域悬停：{(snapshot.HitAreaHovered ? "是" : "否")}");
        ImGui.TextUnformatted($"动画状态：{snapshot.PetState}    当前资源：{snapshot.Asset}");
        ImGui.TextUnformatted($"窗口位置：({snapshot.WindowPosition.X:F1}, {snapshot.WindowPosition.Y:F1})    鼠标：({snapshot.MousePosition.X:F1}, {snapshot.MousePosition.Y:F1})");

        if (detectedNoDraw is { } issue)
        {
            ImGui.Separator();
            ImGui.TextUnformatted($"已检测到提示框命中但贴图未提交（帧 {issue.Frame}，资源 {issue.Asset}）");
            DrawNoDrawReasons(issue);
        }
        else
        {
            ImGui.Separator();
            ImGui.TextUnformatted("未检测到需要分析的情况。");
            ImGui.TextWrapped("检测条件：像素小人命中区域处于悬停状态，且该帧未提交贴图绘制命令。若命令已提交但最终画面仍缺图，插件无法直接读取 GPU 合成结果。");
        }

        if (!enabled)
            ImGui.TextUnformatted("诊断已关闭；以上为关闭前保留的最后一条记录。");

        if (enabled && SettingRow.Button("清空诊断记录"))
        {
            latest = null;
            detectedNoDraw = null;
            drawCount = 0;
            MewUILayer.ClearResourceDiagnostics();
        }
    }

    private static void DrawNoDrawReasons(Snapshot snapshot)
    {
        ImGui.TextUnformatted("检测到的不绘制原因");
        var reasonCount = 0;

        void AddReason(string reason)
        {
            reasonCount++;
            ImGui.TextWrapped($"原因 {reasonCount}：{reason}");
        }

        if (snapshot.CurrentTextureStatus != "正常")
            AddReason($"当前动画帧贴图不可用：{snapshot.CurrentTextureStatus}（{snapshot.CurrentTextureWidth} × {snapshot.CurrentTextureHeight}）。");
        if (snapshot.ResourceStatus != "TryGetWrap 成功")
            AddReason($"资源加载器状态：{snapshot.ResourceStatus}。");
        if (snapshot.FallbackTextureStatus != "正常")
            AddReason($"回退贴图不可用：{snapshot.FallbackTextureStatus}（{snapshot.FallbackTextureWidth} × {snapshot.FallbackTextureHeight}）。");
        AddReason(snapshot.DrawResult);
    }
}
