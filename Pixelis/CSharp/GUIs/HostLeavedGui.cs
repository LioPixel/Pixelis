using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.GUIs.Loading;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrid;

namespace Pixelis.CSharp.GUIs;

public class HostLeavedGui : Gui
{

    public HostLeavedGui() : base("HostLeaved")
    {
        
    }

    protected override void Init()
    {
        base.Init();

        LabelData hostleavedData = new LabelData(ContentRegistry.Fontoe, "The host has leaved the game", 18);
        this.AddElement("host-leaved", new LabelElement(hostleavedData, Anchor.TopCenter, new Vector2(0, 200), new Vector2(3, 3)));
        
        // Menu button.
        TextureButtonData menuButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData menuButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Back to Main Menu", 18, hoverColor: Color.White);
        
        this.AddElement("Menu-Button", new TextureButtonElement(menuButtonData, menuButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            AsyncOperation operation = SceneManager.LoadSceneAsync(null, new ProgressBarLoadingGui("Loading"));

            operation.Completed += success =>
            {
                GuiManager.SetGui(new MenuGui());
            };
            return true;
        }));
    }

    protected override void Update(double delta)
    {
        base.Update(delta);
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        if (SceneManager.ActiveScene == null)
        {
            IWindow window = GlobalGraphicsAssets.Window;

            // Background
            Texture2D backgroundTexture = ContentRegistry.Background2;
            Vector2 backgroundSize = new Vector2((float)window.GetWidth() / backgroundTexture.Width, (float)window.GetHeight() / backgroundTexture.Height);

            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
            context.SpriteBatch.End();
        }

        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, ModalGuiRenderer.DefaultBaseSize);

        base.Draw(context, framebuffer);
    }
}
