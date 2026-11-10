using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;

namespace MewUI;

/// <summary>
/// The site's classic three-second pixel splash, rendered as a transient
/// Dalamud window when the ACR UI is installed. It intentionally has no input
/// handling so it cannot interfere with the game or the existing UI bounds.
/// </summary>
internal sealed class MewUISplashWindow : Window
{
    private const int LoaderSegments = 240;
    private const float StartDuration = 1f;
    private const float TotalDuration = 3.5f;
    private const float FadeDuration = 0.5f;
    private static readonly Vector2[] LoaderDirections = BuildLoaderDirections();
    private static readonly string[] CuteFrames =
    {
        "loader.cute_01.webp", "loader.cute_02.webp",
        "loader.cute_03.webp", "loader.cute_04.webp",
    };
    private double startedAt = -1d;
    private double waitingForImagesAt = -1d;

    public MewUISplashWindow()
        : base("##LuliMewUISplash",
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
        IsOpen = MewUICommonSettings.Instance.EnableLoadingAnimation;
        if (IsOpen) PreloadImages();
    }

    public void Replay()
    {
        if (!MewUICommonSettings.Instance.EnableLoadingAnimation)
        {
            IsOpen = false;
            return;
        }
        startedAt = -1d;
        waitingForImagesAt = -1d;
        PreloadImages();
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
        using var measurement = MewUIPerformance.Measure(MewUIRenderSection.Splash);
        if (startedAt < 0d)
        {
            var now = ImGui.GetTime();
            if (waitingForImagesAt < 0d) waitingForImagesAt = now;
            // The website preloads these images. Avoid a purple placeholder
            // flash while Dalamud uploads them, but never wait indefinitely.
            if (!ImagesReady() && now - waitingForImagesAt < 3d) return;
            startedAt = now;
        }

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
            : 1f - CssBezier((elapsed - (TotalDuration - FadeDuration)) / FadeDuration,
                0.25f, 0.1f, 0.25f, 1f);
        var drawList = ImGui.GetWindowDrawList();

        // The classic site's steps(1) animations swap the two backgrounds at 1s.
        if (elapsed < StartDuration)
            DrawImageCover(drawList, "start.webp", min, max, fade);
        else
            DrawImageCover(drawList, "loading.webp", min, max, fade);

        if (elapsed >= StartDuration)
        {
            var barProgress = Math.Clamp((elapsed - StartDuration) / 2f, 0f, 1f);
            DrawBars(drawList, min, max, barProgress,
                fade * BarOpacity(barProgress));
        }
        DrawLoader(drawList, min + windowSize * 0.5f, elapsed, fade);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        base.PostDraw();
    }

    private static void DrawImageCover(ImDrawListPtr drawList, string name, Vector2 min, Vector2 max, float opacity)
    {
        var texture = MewUILayer.GetResourceTextureWrap(name);
        if (texture == null)
        {
            drawList.AddRectFilled(min, max, MewUI.SimplePalette.ToU32(new Vector4(0.16f, 0.10f, 0.29f, opacity)));
            return;
        }

        var (uv0, uv1) = MewUILayer.CoverUv(texture, max - min);
        drawList.AddImage(texture.Handle, min, max, uv0, uv1,
            MewUI.SimplePalette.ToU32(new Vector4(1f, 1f, 1f, opacity)));
    }

    private static void PreloadImages()
    {
        MewUILayer.GetResourceTextureWrap("start.webp");
        MewUILayer.GetResourceTextureWrap("loading.webp");
        MewUILayer.GetResourceTextureWrap("loader.percent_atlas.webp");
        MewUILayer.GetResourceTextureWrap("loader.ring_glow.webp");
        foreach (var frame in CuteFrames)
            MewUILayer.GetResourceTextureWrap(frame);
    }

    private static bool ImagesReady()
    {
        if (MewUILayer.GetResourceTextureWrap("start.webp") == null
            || MewUILayer.GetResourceTextureWrap("loading.webp") == null
            || MewUILayer.GetResourceTextureWrap("loader.percent_atlas.webp") == null
            || MewUILayer.GetResourceTextureWrap("loader.ring_glow.webp") == null)
            return false;
        foreach (var frame in CuteFrames)
            if (MewUILayer.GetResourceTextureWrap(frame) == null)
                return false;
        return true;
    }

    private static void DrawBars(ImDrawListPtr drawList, Vector2 min, Vector2 max, float progress, float opacity)
    {
        var width = max.X - min.X;
        var centerY = min.Y + (max.Y - min.Y) * 0.5f;
        var barWidth = width * 0.32f;
        var left = min.X - width * 0.40f + width * 0.40f * progress;
        var right = max.X + width * 0.40f * (1f - progress);
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
        drawList.AddRectFilled(new Vector2(left + 2f, top + 2f),
            new Vector2(right + 2f, top + height + 2f),
            SimplePalette.ToU32(new Vector4(0f, 0f, 0f, opacity * 0.20f)));
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
    }

    private void DrawLoader(ImDrawListPtr drawList, Vector2 center, float elapsed, float opacity)
    {
        var pulsePosition = elapsed % 0.9f / 0.9f;
        var pulse = CssEaseInOut(pulsePosition <= 0.5f ? pulsePosition * 2f : (1f - pulsePosition) * 2f);
        var scale = 1f + 0.08f * pulse;
        var outerRadius = 80f * scale;
        var innerRadius = 46.8f * scale;
        var progress = elapsed % 3f / 3f;
        var glow = 4f + 12f * pulse;
        var glowAtlas = MewUILayer.GetResourceTextureWrap("loader.ring_glow.webp");
        if (glowAtlas != null)
        {
            var first = Math.Clamp((int)MathF.Floor(glow) - 4, 0, 12);
            var second = Math.Min(first + 1, 12);
            var fraction = glow - MathF.Floor(glow);
            var extent = new Vector2(128f * scale);
            var glowAlpha = opacity * (0.5f + 0.45f * pulse);
            for (var layer = 0; layer < 2; layer++)
            {
                var index = layer == 0 ? first : second;
                var weight = layer == 0 ? 1f - fraction : fraction;
                if (weight <= 0f) continue;
                drawList.AddImage(glowAtlas.Handle, center - extent, center + extent,
                    new Vector2(index / 13f, 0f), new Vector2((index + 1) / 13f, 1f),
                    SimplePalette.ToU32(new Vector4(1f, 182f / 255f, 232f / 255f, glowAlpha * weight)));
            }
        }
        for (var i = 0; i < LoaderSegments; i++)
        {
            var angle = (i + 0.5f) / LoaderSegments;
            Vector4 color;
            if (angle < progress)
            {
                var blend = angle / MathF.Max(progress, 0.0001f);
                color = Vector4.Lerp(new Vector4(1f, 214f / 255f, 242f / 255f, 0.95f),
                    new Vector4(1f, 182f / 255f, 232f / 255f, 0.92f), blend);
            }
            else
            {
                color = new Vector4(1f, 1f, 1f, 0.12f);
            }
            color.W *= opacity;
            drawList.AddQuadFilled(center + LoaderDirections[i] * outerRadius,
                center + LoaderDirections[i + 1] * outerRadius,
                center + LoaderDirections[i + 1] * innerRadius,
                center + LoaderDirections[i] * innerRadius,
                SimplePalette.ToU32(color));
        }

        // Pixelated 96px frames preserve the source WebP's four-frame timing.
        var frameTime = elapsed % 0.6f;
        var frameIndex = frameTime < 0.2f ? 0 : frameTime < 0.3f ? 1 : frameTime < 0.4f ? 2 : 3;
        var cute = MewUILayer.GetResourceTextureWrap(CuteFrames[frameIndex]);
        if (cute != null)
        {
            var topLeft = new Vector2(MathF.Round(center.X - 48f), MathF.Round(center.Y - 48f));
            drawList.AddImage(cute.Handle, topLeft, topLeft + new Vector2(96f),
                Vector2.Zero, Vector2.One, SimplePalette.ToU32(new Vector4(1f, 1f, 1f, opacity)));
        }

        // Pre-rendered Pixelcraft/monospace labels match the site's CSS without
        // registering a runtime font with Dalamud's font atlas.
        var percentValue = Math.Clamp((int)MathF.Floor(Math.Clamp(elapsed / 3f, 0f, 1f) * 100f), 0, 100);
        var percentAtlas = MewUILayer.GetResourceTextureWrap("loader.percent_atlas.webp");
        if (percentAtlas != null)
        {
            var column = percentValue % 10;
            var row = percentValue / 10;
            var uv0 = new Vector2(column / 10f, row / 11f);
            var uv1 = new Vector2((column + 1) / 10f, (row + 1) / 11f);
            var topLeft = center + new Vector2(-90f, -168f);
            drawList.AddImage(percentAtlas.Handle, topLeft, topLeft + new Vector2(180f, 90f),
                uv0, uv1, SimplePalette.ToU32(new Vector4(1f, 1f, 1f, opacity)));
        }
    }

    private static float CssEaseInOut(float progress) => CssBezier(progress, 0.42f, 0f, 0.58f, 1f);

    private static float CssBezier(float progress, float x1, float y1, float x2, float y2)
    {
        // Solve the CSS cubic-bezier x coordinate to get the matching y value.
        var t = Math.Clamp(progress, 0f, 1f);
        for (var i = 0; i < 6; i++)
        {
            var oneMinusT = 1f - t;
            var x = 3f * oneMinusT * oneMinusT * t * x1
                + 3f * oneMinusT * t * t * x2 + t * t * t;
            var derivative = 3f * oneMinusT * oneMinusT * x1
                + 6f * oneMinusT * t * (x2 - x1)
                + 3f * t * t * (1f - x2);
            t = Math.Clamp(t - (x - progress) / MathF.Max(derivative, 0.0001f), 0f, 1f);
        }
        var rest = 1f - t;
        return 3f * rest * rest * t * y1 + 3f * rest * t * t * y2 + t * t * t;
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
