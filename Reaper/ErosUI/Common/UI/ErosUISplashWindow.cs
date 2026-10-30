using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;

namespace ErosUI;

/// <summary>
/// The site's classic three-second pixel splash, rendered as a transient
/// Dalamud window when the ACR UI is installed. It intentionally has no input
/// handling so it cannot interfere with the game or the existing UI bounds.
/// </summary>
internal sealed class ErosUISplashWindow : Window
{
    private const int LoaderSegments = 72;
    private const float StartDuration = 1f;
    private const float TotalDuration = 3.5f;
    private const float FadeDuration = 0.5f;
    private static readonly Vector2[] LoaderDirections = BuildLoaderDirections();
    private static readonly string[] SealFrames =
    {
        "pet.seal_01.png", "pet.seal_02.png", "pet.seal_03.png",
        "pet.seal_04.png", "pet.seal_05.png", "pet.seal_06.png",
    };
    private double startedAt = -1d;
    private int cachedPercent = -1;
    private string cachedPercentText = string.Empty;

    public ErosUISplashWindow()
        : base("##LuliErosUISplash",
            ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoBackground)
    {
        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;
        IsOpen = ErosUICommonSettings.Instance.EnableLoadingAnimation;
    }

    public void Replay()
    {
        if (!ErosUICommonSettings.Instance.EnableLoadingAnimation)
        {
            IsOpen = false;
            return;
        }
        startedAt = -1d;
        IsOpen = true;
    }

    public override void PreDraw()
    {
        var viewport = ImGui.GetMainViewport();
        Position = viewport.Pos;
        Size = viewport.Size;
        PositionCondition = ImGuiCond.Always;
        SizeCondition = ImGuiCond.Always;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        base.PreDraw();
    }

    public override void Draw()
    {
        if (startedAt < 0d)
            startedAt = ImGui.GetTime();

        var elapsed = (float)(ImGui.GetTime() - startedAt);
        if (elapsed >= TotalDuration)
        {
            IsOpen = false;
            return;
        }

        var windowSize = ImGui.GetWindowSize();
        var min = ImGui.GetWindowPos();
        var max = min + windowSize;
        var fade = elapsed <= TotalDuration - FadeDuration
            ? 1f
            : 1f - (elapsed - (TotalDuration - FadeDuration)) / FadeDuration;
        var drawList = ImGui.GetWindowDrawList();

        // The site keeps the start frame for the first second, then fades the
        // loading frame in over the next two seconds. Keep both layers separate
        // so the transition matches the CSS steps/opacity timing.
        if (elapsed < StartDuration)
            DrawImageCover(drawList, "start.png", min, max, fade);
        else
            DrawImageCover(drawList, "loading.png", min, max,
                fade * Math.Clamp((elapsed - StartDuration) / 2f, 0f, 1f));

        if (elapsed >= StartDuration)
        {
            var barProgress = Math.Clamp((elapsed - StartDuration) / 2f, 0f, 1f);
            var percentProgress = Math.Clamp(elapsed / 3f, 0f, 1f);
            var ringProgress = (elapsed % 3f) / 3f;
            DrawBars(drawList, min, max, barProgress,
                fade * BarOpacity(barProgress));
            DrawLoader(drawList, min + windowSize * 0.5f, percentProgress, ringProgress, fade);
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        base.PostDraw();
    }

    private static void DrawImageCover(ImDrawListPtr drawList, string name, Vector2 min, Vector2 max, float opacity)
    {
        var texture = ErosUILayer.GetResourceTextureWrap(name);
        if (texture == null)
        {
            drawList.AddRectFilled(min, max, ErosUI.SimplePalette.ToU32(new Vector4(0.16f, 0.10f, 0.29f, opacity)));
            return;
        }

        var (uv0, uv1) = ErosUILayer.CoverUv(texture, max - min);
        drawList.AddImage(texture.Handle, min, max, uv0, uv1,
            ErosUI.SimplePalette.ToU32(new Vector4(1f, 1f, 1f, opacity)));
    }

    private static void DrawBars(ImDrawListPtr drawList, Vector2 min, Vector2 max, float progress, float opacity)
    {
        var width = max.X - min.X;
        var centerY = min.Y + (max.Y - min.Y) * 0.5f;
        var barWidth = width * 0.32f;
        var left = min.X - width * 0.40f + width * 0.40f * progress;
        var right = max.X + width * 0.08f - width * 0.40f * progress;
        DrawPixelBar(drawList, left, left + barWidth, centerY - 6f, opacity);
        DrawPixelBar(drawList, right - barWidth, right, centerY - 6f, opacity);
    }

    private static float BarOpacity(float progress) => progress <= 0f
        ? 0f
        : progress < 0.10f
            ? progress / 0.10f
            : 1f - Math.Clamp((progress - 0.10f) / 0.90f, 0f, 1f);

    private static void DrawPixelBar(ImDrawListPtr drawList, float left, float right, float top, float opacity)
    {
        const float height = 12f;
        for (var x = left; x < right; x += 12f)
        {
            var segmentEnd = MathF.Min(x + 8f, right);
            drawList.AddRectFilled(new Vector2(x, top), new Vector2(segmentEnd, top + height),
                SimplePalette.ToU32(new Vector4(1f, 0.84f, 0.95f, opacity)));
            if (segmentEnd < right)
                drawList.AddRectFilled(new Vector2(segmentEnd, top), new Vector2(MathF.Min(x + 12f, right), top + height),
                    SimplePalette.ToU32(new Vector4(1f, 0.62f, 0.85f, opacity)));
        }

        drawList.AddRect(new Vector2(left, top), new Vector2(right, top + height),
            SimplePalette.ToU32(new Vector4(0.424f, 0.388f, 1f, opacity)), 0f,
            ImDrawFlags.None, 2f);
        drawList.AddRect(new Vector2(left + 2f, top + 2f), new Vector2(right + 2f, top + height + 2f),
            SimplePalette.ToU32(new Vector4(0f, 0f, 0f, opacity * 0.20f)), 0f,
            ImDrawFlags.None, 2f);
    }

    private void DrawLoader(ImDrawListPtr drawList, Vector2 center, float percentProgress, float ringProgress, float opacity)
    {
        const float radius = 80f;
        var pulse = 1f + 0.08f * MathF.Sin((float)(ImGui.GetTime() * Math.PI * 2d / 0.9d));
        var ringRadius = radius * pulse;
        var visible = Math.Clamp((int)MathF.Round(LoaderSegments * ringProgress), 1, LoaderSegments);
        for (var i = 0; i < LoaderSegments; i++)
        {
            var p0 = center + LoaderDirections[i] * ringRadius;
            var p1 = center + LoaderDirections[i + 1] * ringRadius;
            var alpha = i < visible ? opacity * 0.95f : opacity * 0.12f;
            drawList.AddLine(p0, p1, SimplePalette.ToU32(new Vector4(1f, 0.84f, 0.95f, alpha)), 3f);
        }

        var cuteSize = new Vector2(96f) * pulse;
        // Use the same six decoded frames as the site's animated cute.webp. A
        // native PNG frame avoids backend-dependent animated-WebP uploads.
        var sealFrame = SealFrames[(int)(ImGui.GetTime() * 6.67d) % SealFrames.Length];
        var cute = ErosUILayer.GetResourceTextureWrap(sealFrame);
        if (cute != null)
            drawList.AddImage(cute.Handle, center - cuteSize * 0.5f, center + cuteSize * 0.5f,
                Vector2.Zero, Vector2.One, SimplePalette.ToU32(new Vector4(1f, 1f, 1f, opacity)));

        var percentValue = (int)MathF.Round(percentProgress * 100f);
        if (percentValue != cachedPercent)
        {
            cachedPercent = percentValue;
            cachedPercentText = $"{percentValue}%";
        }
        var percent = cachedPercentText;
        var fontSize = ImGui.GetFontSize() * 3f;
        var textSize = ImGui.CalcTextSize(percent) * 3f;
        var textPos = center + new Vector2(-textSize.X * 0.5f, -100f);
        drawList.AddRect(textPos - new Vector2(10f, 4f), textPos + textSize + new Vector2(10f, 4f),
            SimplePalette.ToU32(new Vector4(0.424f, 0.388f, 1f, opacity)), 0f, ImDrawFlags.None, 2f);
        drawList.AddText(ImGui.GetFont(), fontSize, textPos + new Vector2(2f, 2f),
            SimplePalette.ToU32(new Vector4(0.35f, 0.20f, 0.55f, opacity * 0.7f)), percent);
        drawList.AddText(ImGui.GetFont(), fontSize, textPos,
            SimplePalette.ToU32(new Vector4(1f, 0.96f, 1f, opacity)), percent);
    }

    private static Vector2[] BuildLoaderDirections()
    {
        var directions = new Vector2[LoaderSegments + 1];
        for (var i = 0; i <= LoaderSegments; i++)
        {
            var angle = -MathF.PI * 0.5f + i / (float)LoaderSegments * MathF.Tau;
            directions[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
        return directions;
    }
}
