using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace MewUI;

// Cache only the label, never cooldown/charge state. Font and culture changes
// invalidate its measurement so scale and alignment continue to follow ImGui.
internal struct HotkeyNumberLabel
{
    private bool initialized;
    private int value;
    private CultureInfo? culture;
    private ImFontPtr font;
    private float fontSize;
    private string? text;
    private Vector2 size;

    internal (string Text, Vector2 Size) Get(int number, ImFontPtr currentFont, float currentFontSize)
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var textChanged = !initialized || value != number || !ReferenceEquals(culture, currentCulture);
        if (textChanged)
        {
            text = number.ToString(currentCulture);
            value = number;
            culture = currentCulture;
        }
        if (textChanged || !font.Equals(currentFont) || fontSize != currentFontSize)
        {
            size = ImGui.CalcTextSize(text!);
            font = currentFont;
            fontSize = currentFontSize;
        }
        initialized = true;
        return (text!, size);
    }
}
