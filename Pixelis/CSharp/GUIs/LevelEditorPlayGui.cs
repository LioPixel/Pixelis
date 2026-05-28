using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class LevelEditorPlayGui : Gui
{
    private readonly CustomLevelScene _scene;

    public LevelEditorPlayGui(CustomLevelScene scene) : base("LevelEditorPlayOverlay", null)
    {
        _scene = scene;
    }

    protected override void Init()
    {
        base.Init();

        TextureButtonData buttonData = new TextureButtonData(
            ContentRegistry.UiButton,
            hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice,
            borderInsets: new BorderInsets(12));

        this.AddElement("Editor-Button", new TextureButtonElement(
            buttonData,
            GuiText.ButtonLabel(Localization.T("gui.level_editor.editor"), 150),
            Anchor.TopRight,
            new Vector2(-20, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.ReturnToEditorMode();
                return true;
            }));
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        float scale = this.ScaleFactor;
        Vector2 snappedWindowSize = GetSnappedWindowSize(scale);
        Vector2 panelSize = new Vector2(200, 84) * scale;

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(snappedWindowSize.X - panelSize.X, 0, panelSize.X, panelSize.Y),
            color: new Color(15, 15, 15, 160));
        context.PrimitiveBatch.End();

        base.Draw(context, framebuffer);
    }

    private static Vector2 GetSnappedWindowSize(float scale)
    {
        float width = MathF.Floor(GlobalGraphicsAssets.Window.GetWidth() / scale) * scale;
        float height = MathF.Floor(GlobalGraphicsAssets.Window.GetHeight() / scale) * scale;
        return new Vector2(width, height);
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
