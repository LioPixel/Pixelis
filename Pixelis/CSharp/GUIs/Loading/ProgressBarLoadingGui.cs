using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.GUI.Loading;
using Veldrith;

namespace Pixelis.CSharp.GUIs.Loading;

public class ProgressBarLoadingGui : LoadingGui
{

    private string _loadingText;

    public ProgressBarLoadingGui(string name, string? loadingText = null, float minTime = 2.5F, (int, int)? size = null) : base(name, minTime, size)
    {
        this._loadingText = loadingText ?? Localization.T("gui.loading.loading");
    }
    
    protected override void Init() {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, _loadingText, 18);
        this.AddElement("loading_label", new LabelElement(labelData, Anchor.Center, Vector2.Zero, new Vector2(5, 5)));
        
        TextureSlideBarData textureSlideBarData = new TextureSlideBarData(
            ContentRegistry.UiBar,
            ContentRegistry.UiFilledBar,
            null,
            barResizeMode: ResizeMode.NineSlice,
            filledBarResizeMode: ResizeMode.NineSlice,
            barBorderInsets: new BorderInsets(3),
            filledBarBorderInsets: new BorderInsets(3),
            disabledBarColor: Color.White,
            disabledFilledBarColor: Color.White);
        
        TextureSlideBarElement slideBarElement = new TextureSlideBarElement(textureSlideBarData, Anchor.Center, new Vector2(0, 80), 0, 1, wholeNumbers: false, size: new Vector2(140, 8), scale: new Vector2(5, 5)) {
            Interactable = false
        };
        this.AddElement("progress_bar", slideBarElement);
    }
    
    protected override void Update(double delta) {
        base.Update(delta);
        
        if (this.TryGetElement("progress_bar", out GuiElement? element)) {
            if (element is TextureSlideBarElement barElement) {
                barElement.Value = float.Lerp(barElement.Value, this.Progress, (float) delta * 5.0F);
            }
        }
    }
    
    protected override void Draw(GraphicsContext context, Framebuffer framebuffer) {
        //context.CommandList.ClearColorTarget(0, Color.Black.ToRgbaFloat());
        
        IWindow window = GlobalGraphicsAssets.Window;

        // Background
        Texture2D backgroundTexture = ContentRegistry.Background2;
        Vector2 backgroundSize = new Vector2((float)window.GetWidth() / backgroundTexture.Width,
            (float)window.GetHeight() / backgroundTexture.Height);

        context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
        context.SpriteBatch.End();
        
        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight()), color: new Color(128, 128, 128, 128));
        context.PrimitiveBatch.End();
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
