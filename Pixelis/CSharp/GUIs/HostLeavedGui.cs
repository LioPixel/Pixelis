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
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class HostLeavedGui : Gui
{
    private static readonly Vector2 BaseWindowSize = ModalGuiRenderer.DefaultBaseSize;

    public HostLeavedGui() : base("HostLeaved")
    {
        
    }

    protected override void Init()
    {
        base.Init();

        LabelData hostleavedData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.host_left.message"), 18, color: Color.White);
        this.AddElement("host-leaved", new LabelElement(hostleavedData, Anchor.Center, new Vector2(0, -35), new Vector2(1.6F, 1.6F)));
        
        // Menu button.
        TextureButtonData menuButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        string menuText = Localization.T("gui.host_left.back_to_main_menu");
        Vector2 menuButtonSize = GuiText.ButtonSize(menuText, 230, maxWidth: 320);
        LabelData menuButtonLabelData = GuiText.ButtonLabel(menuText, menuButtonSize.X);
        
        this.AddElement("Menu-Button", new TextureButtonElement(menuButtonData, menuButtonLabelData, Anchor.Center, new Vector2(0, 68), size: menuButtonSize, textOffset: new Vector2(0, 1), clickFunc: (element) => {
            AsyncOperation operation = SceneManager.LoadSceneAsync(null, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.loading")));

            operation.Completed += success =>
            {
                NetworkManager.Cleanup();
                GuiManager.SetGui(new MenuGui());
            };
            return true;
        }));
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            NetworkManager.Cleanup();
            GuiManager.SetGui(new MenuGui());
        }
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

        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, BaseWindowSize);

        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
