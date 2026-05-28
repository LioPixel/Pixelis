using System.Numerics;
using Bliss.CSharp.Colors;
using Sparkle.CSharp.GUI.Elements.Data;

namespace Pixelis.CSharp.GUIs;

public static class GuiText
{
    public static LabelData ButtonLabel(string text, float buttonWidth, float baseSize = 18F, float minSize = 14F)
    {
        return new LabelData(ContentRegistry.Fontoe, text, FitSize(text, buttonWidth - 24F, baseSize, minSize), hoverColor: Color.White);
    }

    public static LabelData Label(string text, float maxWidth, float baseSize = 18F, float minSize = 14F, Color? color = null)
    {
        return new LabelData(ContentRegistry.Fontoe, text, FitSize(text, maxWidth, baseSize, minSize), color: color);
    }

    public static Vector2 ButtonSize(string text, float minWidth, float height = 40F, float baseSize = 18F, float maxWidth = 300F)
    {
        float measuredWidth = ContentRegistry.Fontoe.MeasureText(text, (int)baseSize, Vector2.One).X;
        float width = Math.Clamp(MathF.Ceiling(measuredWidth + 34F), minWidth, maxWidth);
        return new Vector2(width, height);
    }

    private static float FitSize(string text, float maxWidth, float baseSize, float minSize)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return baseSize;
        }

        for (float size = baseSize; size >= minSize; size -= 1F)
        {
            if (ContentRegistry.Fontoe.MeasureText(text, (int)size, Vector2.One).X <= maxWidth)
            {
                return size;
            }
        }

        return minSize;
    }
}
