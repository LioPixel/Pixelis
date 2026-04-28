using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Transformations;
using Sparkle.CSharp.Graphics;
using Veldrid;

namespace Pixelis.CSharp.GUIs;

internal static class ModalGuiRenderer
{
    internal static readonly Vector2 DefaultBaseSize = new Vector2(550, 310);

    internal static RectangleF GetCenteredScaledRect(float scale, Vector2 baseSize)
    {
        Vector2 scaledSize = baseSize * scale;
        float screenWidth = MathF.Floor(GlobalGraphicsAssets.Window.GetWidth() / scale) * scale;
        float screenHeight = MathF.Floor(GlobalGraphicsAssets.Window.GetHeight() / scale) * scale;

        Vector2 pos = new Vector2(
            MathF.Floor((screenWidth / 2.0F - scaledSize.X / 2.0F) / scale) * scale,
            MathF.Floor((screenHeight / 2.0F - scaledSize.Y / 2.0F) / scale) * scale
        );

        return new RectangleF(pos.X, pos.Y, scaledSize.X, scaledSize.Y);
    }

    internal static void DrawModalBackground(GraphicsContext context, Framebuffer framebuffer, float scale, Vector2 baseSize)
    {
        RectangleF panelRect = GetCenteredScaledRect(scale, baseSize);

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight()),
            color: new Color(128, 128, 128, 128));
        context.PrimitiveBatch.DrawFilledRectangle(panelRect, color: new Color(128, 128, 128, 128));
        context.PrimitiveBatch.DrawEmptyRectangle(panelRect, 4 * scale, color: new Color(64, 64, 64, 128));
        context.PrimitiveBatch.End();
    }
}
