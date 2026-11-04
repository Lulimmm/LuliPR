using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using LuliPR.Common;

namespace ErosUI;

internal static class ErosUIUsagePage
{
    private const double CredentialThresholdSeconds = 24d * 60d * 60d;
    private const string CredentialCommand = "/e 海豹助手高级凭证：ADV-Y5JRjBH433W9vKxyOXIbKk2a";
    private static readonly int[,] DigitSegments =
    {
        { 1, 1, 1, 1, 1, 1, 0 },
        { 0, 1, 1, 0, 0, 0, 0 },
        { 1, 1, 0, 1, 1, 0, 1 },
        { 1, 1, 1, 1, 0, 0, 1 },
        { 0, 1, 1, 0, 0, 1, 1 },
        { 1, 0, 1, 1, 0, 1, 1 },
        { 1, 0, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 0, 0, 0, 0 },
        { 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 0, 1, 1 }
    };

    internal static void Draw()
    {
        var totalSeconds = UsageStatistics.GetSnapshot().TotalSeconds;
        DrawDigitalDisplay(totalSeconds);
    }

    private static void DrawDigitalDisplay(double totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        var hours = Math.Max(0, (long)span.TotalHours);
        var text = $"{hours:00}:{span.Minutes:00}:{span.Seconds:00}";

        const float digitWidth = 30f;
        const float digitHeight = 58f;
        const float digitGap = 8f;
        const float colonWidth = 12f;
        const float colonGap = 9f;
        var displayWidth = 0f;
        foreach (var ch in text)
            displayWidth += ch == ':' ? colonWidth + colonGap : digitWidth + digitGap;
        displayWidth -= digitGap;

        var available = ImGui.GetContentRegionAvail();
        var regionHeight = MathF.Max(280f, available.Y);
        var center = ImGui.GetCursorScreenPos() + new Vector2(available.X * 0.5f, regionHeight * 0.5f);
        var drawList = ImGui.GetWindowDrawList();
        var clockRadius = MathF.Max(displayWidth, digitHeight) * 0.5f + 42f;
        DrawPixelClockMarks(drawList, center, clockRadius);

        var cursor = new Vector2(center.X - displayWidth * 0.5f, center.Y - digitHeight * 0.5f);
        foreach (var ch in text)
        {
            if (ch == ':')
            {
                DrawColon(drawList, cursor + new Vector2(colonWidth * 0.5f, digitHeight * 0.30f));
                cursor.X += colonWidth + colonGap;
                continue;
            }

            DrawDigit(drawList, ch - '0', cursor, digitWidth, digitHeight);
            cursor.X += digitWidth + digitGap;
        }

        ImGui.Dummy(new Vector2(available.X, regionHeight));
        if (totalSeconds >= CredentialThresholdSeconds)
            DrawCredentialButton(center, clockRadius);
    }

    private static void DrawCredentialButton(Vector2 center, float clockRadius)
    {
        const float width = 150f;
        ImGui.SetCursorScreenPos(new Vector2(center.X - width * 0.5f, center.Y + clockRadius + 18f));
        ImGui.PushStyleColor(ImGuiCol.Button, SimplePalette.WithAlpha(SimplePalette.Accent, 0.72f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.PrimaryHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.PrimaryActive);
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        var clicked = ImGui.Button("获取凭证", new Vector2(width, 34f));
        ImGui.PopStyleColor(4);
        if (clicked)
            Svc.Commands.ProcessCommand(CredentialCommand);
        ImGui.Dummy(new Vector2(width, 34f));
    }

    private static void DrawPixelClockMarks(ImDrawListPtr drawList, Vector2 center, float radius)
    {
        var color = SimplePalette.ToU32(SimplePalette.WithAlpha(SimplePalette.PrimaryHover, 0.82f));
        for (var i = 0; i < 12; i++)
        {
            var angle = -MathF.PI * 0.5f + i * MathF.PI / 6f;
            var radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var tangent = new Vector2(-radial.Y, radial.X);
            var inner = center + radial * (radius - 7f);
            var outer = center + radial * (radius + 7f);
            const float halfWidth = 3f;
            drawList.AddQuadFilled(
                inner - tangent * halfWidth,
                inner + tangent * halfWidth,
                outer + tangent * halfWidth,
                outer - tangent * halfWidth,
                color);
        }
    }

    private static void DrawDigit(ImDrawListPtr drawList, int digit, Vector2 origin, float width, float height)
    {
        const float thickness = 5f;
        const float inset = 2f;
        var horizontal = new Vector2(width - inset * 2f, thickness);
        var vertical = new Vector2(thickness, height * 0.5f - thickness * 1.5f);
        var positions = new[]
        {
            new Vector2(inset, 0f),
            new Vector2(width - thickness - inset, inset),
            new Vector2(width - thickness - inset, height * 0.5f + thickness * 0.5f),
            new Vector2(inset, height - thickness),
            new Vector2(0f, height * 0.5f + thickness * 0.5f),
            new Vector2(0f, inset),
            new Vector2(inset, height * 0.5f - thickness * 0.5f)
        };

        for (var i = 0; i < 7; i++)
        {
            var size = i is 0 or 3 or 6 ? horizontal : vertical;
            var color = DigitSegments[digit, i] == 1
                ? SimplePalette.PrimaryHover
                : new Vector4(0.25f, 0.18f, 0.35f, 0.45f);
            drawList.AddRectFilled(origin + positions[i], origin + positions[i] + size,
                SimplePalette.ToU32(color), thickness * 0.5f);
        }
    }

    private static void DrawColon(ImDrawListPtr drawList, Vector2 center)
    {
        var color = SimplePalette.ToU32(SimplePalette.PrimaryHover);
        drawList.AddCircleFilled(center + new Vector2(0f, -12f), 4f, color, 12);
        drawList.AddCircleFilled(center + new Vector2(0f, 12f), 4f, color, 12);
    }
}
