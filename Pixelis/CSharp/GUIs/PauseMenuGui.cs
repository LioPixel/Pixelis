using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class PauseMenuGui : Gui
{
    private bool _opendRightNow;
    
    public PauseMenuGui() : base("PauseMenu") { }

    protected override void Init()
    {
        base.Init();
        this._opendRightNow = true;
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.pause.title"), 18);
        this.AddElement("Test-Label", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 100), new Vector2(5, 5)));
        
        // Button color.
        Color lightPurpleColor = new Color(147, 112, 219, 180);
        Color purpleColor = new Color(128, 0, 128, 180);
        Color darkPurpleColor = new Color(75, 0, 130, 180);
 
        // Menu button.
        TextureButtonData menuButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData menuButtonLabelData = GuiText.ButtonLabel(Localization.T("common.menu"), 230);
        
        this.AddElement("Exit-Button", new TextureButtonElement(menuButtonData, menuButtonLabelData, Anchor.Center, new Vector2(0, 0), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            NetworkManager.Cleanup();
            AsyncOperation operation = SceneManager.LoadSceneAsync(null, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.loading")));

            operation.Completed += success =>
            {
                GuiManager.SetGui(new MenuGui());
            };

            return true;
        }));
        
        // Options button.
        TextureButtonData optionsButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData optionsButtonLabelData = GuiText.ButtonLabel(Localization.T("common.options"), 230);
        
        this.AddElement("Options-Button", new TextureButtonElement(optionsButtonData, optionsButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            GuiManager.SetGui(new OptionsGui());
            return true;
        }));
        
        // Reset button.
        TextureButtonData resetButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData resetButtonLabelData = GuiText.ButtonLabel(Localization.T("common.reset"), 230);
        
        this.AddElement("Reset-Button", new TextureButtonElement(resetButtonData, resetButtonLabelData, Anchor.Center, new Vector2(0, 120), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element ) => {
            if (SceneManager.ActiveScene is LevelScene level)
            {
                foreach (Entity entity in level.GetEntities())
                {
                    if (entity is Player player)
                    {
                        if (player.IsLocalPlayer)
                        {
                            player.LocalTransform.Translation = new Vector3(0, -16 * 2, 0);
                            player.GetComponent<RigidBody2D>()?.Awake = true;
                            SceneManager.ActiveCam2D?.Position = new Vector2(player.LocalTransform.Translation.X, player.LocalTransform.Translation.Y);   
                        }
                                                
                        if (NetworkManager.Client == null || !NetworkManager.Client.IsConnected)
                        {
                            player.LocalTransform.Translation = new Vector3(0, -16 * 2, 0);
                            player.GetComponent<RigidBody2D>()?.Awake = true;
                            SceneManager.ActiveCam2D?.Position = new Vector2(player.LocalTransform.Translation.X, player.LocalTransform.Translation.Y);   
                        }
                    }
                }
            }

            GuiManager.SetGui(null);
            return true;
        }));
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (!this._opendRightNow)
        {
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                if (SceneManager.ActiveScene == null)
                {
                    GuiManager.SetGui(new MenuGui());
                }
                else
                {
                    GuiManager.SetGui(null);
                }
            }
        }

        this._opendRightNow = false;
    }
    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight()), color: new Color(128, 128, 128, 128));
        context.PrimitiveBatch.End();
        
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
