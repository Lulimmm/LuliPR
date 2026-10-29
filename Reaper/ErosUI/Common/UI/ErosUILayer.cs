using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;

namespace ErosUI;

// 自绘背景窗口共用的绘制占位辅助。
// 背景必须画进窗口自己的绘制列表，窗口叠加顺序才是对的：画进 viewport 背景层
// 会让背景沉到所有窗口之下，两个窗口重叠时内容会互相穿透。
// 但窗口开启背景模糊时，Dalamud 的 PrependBlurBehind 会替换掉窗口绘制列表的
// 第一条命令。所以自绘背景前先垫一条零面积矩形占住第一位，真正的绘制从第二条开始。
internal static class ErosUILayer
{
    private static readonly Dictionary<string, ISharedImmediateTexture> ResourceTextures = new(StringComparer.Ordinal);
    private static readonly List<Meteor> Meteors = new();
    private static readonly Random MeteorRandom = new();
    private static double nextMeteorAt = -1d;

    private sealed class Meteor
    {
        public double BornAt;
        public double Duration;
        public Vector2 Start;
        public Vector2 Travel;
        public float Length;
        public (float Offset, float Drift, double Delay, double Duration, float Size)[] Sparkles = [];
    }
    // 零面积全透明矩形：只占住绘制列表第一位的命令槽，不产生任何视觉
    public static void 垫牺牲帧(ImDrawListPtr drawList) =>
        drawList.AddRectFilled(Vector2.Zero, Vector2.Zero, 0u);

    /// <summary>Draw the website's embedded pixel-glass surface.</summary>
    public static void DrawWebsiteSurface(
        ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 tint,
        Vector4 border, float rounding, float imageAlpha = 0.46f)
    {
        垫牺牲帧(drawList);
        drawList.PushClipRect(min, max, false);
        drawList.AddRectFilled(min + new Vector2(3f, 4f), max + new Vector2(3f, 4f),
            SimplePalette.ToU32(new Vector4(0.18f, 0.09f, 0.32f, 0.28f)), rounding);

        // 先铺现有的半透明玻璃色，再叠加网页背景。这样窗口仍保留原来的
        // 透出游戏画面的比例，但网页纹理不会被紫色遮罩压到几乎不可见。
        drawList.AddRectFilled(min, max, SimplePalette.ToU32(tint), rounding, ImDrawFlags.RoundCornersAll);

        var texture = GetResourceTexture("bg2.jpg");
        if (texture != null)
        {
            // Get a frame-safe wrap. Dalamud may dispose the previous frame's
            // UnknownTextureWrap during resource/device changes, so never cache
            // the returned wrap across draw calls.
            var wrap = texture.GetWrapOrEmpty();
            if (wrap != null)
            {
                var (uv0, uv1) = CoverUv(wrap, max - min);
                drawList.AddImageRounded(wrap.Handle, min, max, uv0, uv1,
                    SimplePalette.ToU32(new Vector4(1f, 1f, 1f, Math.Clamp(imageAlpha, 0f, 1f))),
                    rounding, ImDrawFlags.RoundCornersAll);
            }
        }

        drawList.AddRect(min + new Vector2(1f), max - new Vector2(1f),
            SimplePalette.ToU32(border), MathF.Max(0f, rounding - 1f), ImDrawFlags.RoundCornersAll, 2f);
        drawList.PopClipRect();
    }

    /// <summary>
    /// Draws the website's animated shooting-star layer over the embedded background.
    /// The layer is clipped to the window and only uses translucent draw calls, so it
    /// does not change the existing glass/background opacity.
    /// </summary>
    public static void DrawShootingStars(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        if (max.X <= min.X || max.Y <= min.Y)
            return;

        var now = ImGui.GetTime();
        if (nextMeteorAt < 0d)
            nextMeteorAt = now + 0.6d;

        if (now >= nextMeteorAt)
        {
            SpawnMeteor(now, min, max);
            nextMeteorAt = now + 1.4d + MeteorRandom.NextDouble() * 3.2d;
        }

        for (var i = Meteors.Count - 1; i >= 0; i--)
        {
            if (now - Meteors[i].BornAt > Meteors[i].Duration + 0.25d)
                Meteors.RemoveAt(i);
        }

        drawList.PushClipRect(min, max, false);
        foreach (var meteor in Meteors)
            DrawMeteor(drawList, meteor, now);
        drawList.PopClipRect();
    }

    private static void SpawnMeteor(double now, Vector2 min, Vector2 max)
    {
        var width = MathF.Max(1f, max.X - min.X);
        var angle = (115f + (float)MeteorRandom.NextDouble() * 15f) * (MathF.PI / 180f);
        var travelDistance = width * (0.45f + (float)MeteorRandom.NextDouble() * 0.35f);

        // The website starts meteors above/right of the viewport and sends them
        // diagonally down and left.
        var travel = new Vector2(-MathF.Abs(MathF.Cos(angle) * travelDistance),
            MathF.Abs(MathF.Sin(angle) * travelDistance));
        var start = new Vector2(
            min.X + width * (0.50f + (float)MeteorRandom.NextDouble() * 0.50f),
            min.Y - (10f + (float)MeteorRandom.NextDouble() * 40f));

        var sparkleCount = 3 + MeteorRandom.Next(4);
        var sparkles = new (float, float, double, double, float)[sparkleCount];
        for (var i = 0; i < sparkles.Length; i++)
        {
            sparkles[i] = (
                18f + (float)MeteorRandom.NextDouble() * (220f * 0.6f),
                ((float)MeteorRandom.NextDouble() - 0.5f) * 22f,
                0.08d + MeteorRandom.NextDouble() * 0.35d,
                0.4d + MeteorRandom.NextDouble() * 0.5d,
                2f + (float)MeteorRandom.NextDouble() * 2.5f);
        }

        Meteors.Add(new Meteor
        {
            BornAt = now,
            Duration = 1.2d + MeteorRandom.NextDouble(),
            Start = start,
            Travel = travel,
            Length = 220f + (float)MeteorRandom.NextDouble() * 200f,
            Sparkles = sparkles,
        });
    }

    private static void DrawMeteor(ImDrawListPtr drawList, Meteor meteor, double now)
    {
        var elapsed = now - meteor.BornAt;
        var t = Math.Clamp((float)(elapsed / meteor.Duration), 0f, 1f);
        var opacity = t switch
        {
            < 0.06f => t / 0.06f * 0.6f,
            < 0.14f => 0.6f + (t - 0.06f) / 0.08f * 0.4f,
            < 0.75f => 0.85f,
            _ => (1f - t) / 0.25f * 0.85f,
        };
        if (opacity <= 0f)
            return;

        var head = meteor.Start + meteor.Travel * t;
        var travelDirection = Vector2.Normalize(meteor.Travel);
        var tail = head - travelDirection * meteor.Length;

        // Broad translucent bloom and glow, followed by the four-colour tail.
        drawList.AddCircleFilled(head, 22f, SimplePalette.ToU32(new Vector4(0.72f, 0.56f, 1f, opacity * 0.07f)), 24);
        drawList.AddCircleFilled(head, 11f, SimplePalette.ToU32(new Vector4(0.80f, 0.70f, 1f, opacity * 0.14f)), 20);
        drawList.AddLine(tail, head, SimplePalette.ToU32(new Vector4(0.68f, 0.52f, 1f, opacity * 0.10f)), 12f);
        drawList.AddLine(tail, head, SimplePalette.ToU32(new Vector4(0.62f, 0.70f, 1f, opacity * 0.18f)), 6f);

        const int segments = 8;
        for (var i = 0; i < segments; i++)
        {
            var a = i / (float)segments;
            var b = (i + 1) / (float)segments;
            var p0 = Vector2.Lerp(tail, head, a);
            var p1 = Vector2.Lerp(tail, head, b);
            var colour = MeteorTailColour(a, opacity);
            drawList.AddLine(p0, p1, SimplePalette.ToU32(colour), 2.2f - a * 0.6f);
        }

        var perpendicular = new Vector2(-travelDirection.Y, travelDirection.X);
        var cross = perpendicular * 9f;
        drawList.AddLine(head - cross, head + cross,
            SimplePalette.ToU32(new Vector4(0.92f, 0.88f, 1f, opacity * 0.50f)), 1.3f);
        drawList.AddLine(head - travelDirection * 9f, head + travelDirection * 9f,
            SimplePalette.ToU32(new Vector4(0.98f, 0.85f, 1f, opacity * 0.42f)), 1.2f);
        drawList.AddCircleFilled(head, 3.6f,
            SimplePalette.ToU32(new Vector4(0.96f, 0.98f, 1f, opacity)), 16);

        foreach (var sparkle in meteor.Sparkles)
        {
            var sparkleT = elapsed - sparkle.Delay;
            if (sparkleT <= 0d || sparkleT >= sparkle.Duration)
                continue;
            var sparkleProgress = (float)(sparkleT / sparkle.Duration);
            var sparklePosition = head - travelDirection * sparkle.Offset
                + perpendicular * sparkle.Drift * sparkleProgress;
            var sparkleAlpha = opacity * (1f - sparkleProgress) * 0.75f;
            drawList.AddCircleFilled(sparklePosition, sparkle.Size,
                SimplePalette.ToU32(new Vector4(0.86f, 0.78f, 1f, sparkleAlpha)), 10);
        }
    }

    private static Vector4 MeteorTailColour(float t, float opacity)
    {
        var pink = new Vector4(1f, 0.63f, 0.91f, opacity * 0.86f);
        var purple = new Vector4(0.74f, 0.46f, 1f, opacity * 0.80f);
        var blue = new Vector4(0.42f, 0.70f, 1f, opacity * 0.76f);
        var white = new Vector4(0.93f, 0.96f, 1f, opacity * 0.95f);
        return t switch
        {
            < 0.34f => Vector4.Lerp(pink, purple, t / 0.34f),
            < 0.72f => Vector4.Lerp(purple, blue, (t - 0.34f) / 0.38f),
            _ => Vector4.Lerp(blue, white, (t - 0.72f) / 0.28f),
        };
    }

    public static IDalamudTextureWrap? GetResourceTextureWrap(string name)
        => GetResourceTexture(name)?.GetWrapOrEmpty();

    private static ISharedImmediateTexture? GetResourceTexture(string name)
    {
        if (ResourceTextures.TryGetValue(name, out var cached))
            return cached;
        try
        {
            var texture = Svc.Texture.GetFromManifestResource(
                typeof(ErosUILayer).Assembly, $"ErosUI.Resources.{name}");
            ResourceTextures[name] = texture;
            return texture;
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[ErosUI] embedded texture unavailable ({name}): {ex.Message}");
            return null;
        }
    }

    public static (Vector2 Uv0, Vector2 Uv1) CoverUv(IDalamudTextureWrap texture, Vector2 destination)
    {
        if (texture.Width <= 0 || texture.Height <= 0 || destination.X <= 0f || destination.Y <= 0f)
            return (Vector2.Zero, Vector2.One);
        var sourceRatio = texture.Width / (float)texture.Height;
        var destinationRatio = destination.X / destination.Y;
        if (destinationRatio > sourceRatio)
        {
            var crop = 1f - sourceRatio / destinationRatio;
            return (new Vector2(0f, crop * 0.5f), new Vector2(1f, 1f - crop * 0.5f));
        }
        var horizontalCrop = 1f - destinationRatio / sourceRatio;
        return (new Vector2(horizontalCrop * 0.5f, 0f), new Vector2(1f - horizontalCrop * 0.5f, 1f));
    }
}
