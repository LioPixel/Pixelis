using System.Numerics;
using Bliss.CSharp.Colors;
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

public class GameOverGui : Gui
{
    public GameOverGui() : base("GameOver", null) { }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.game_over.title"), 18);
        this.AddElement("Test-Label", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 100), new Vector2(5, 5)));
 
        // Menu button.
        TextureButtonData menuButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData menuButtonLabelData = GuiText.ButtonLabel(Localization.T("common.menu"), 230);
        
        this.AddElement("Menu-Button", new TextureButtonElement(menuButtonData, menuButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            AsyncOperation operation = SceneManager.LoadSceneAsync(null, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.loading")));

            operation.Completed += success =>
            {
                GuiManager.SetGui(new MenuGui());
            };
            return true;
        }));
        
        // Reset button.
        TextureButtonData resetButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData resetButtonLabelData = GuiText.ButtonLabel(Localization.T("common.reset"), 230);
        
        this.AddElement("Reset-Button", new TextureButtonElement(resetButtonData, resetButtonLabelData, Anchor.Center, new Vector2(0, 0), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            if (SceneManager.ActiveScene is CustomLevelScene customLevelScene && customLevelScene.IsPlayingFromEditor)
            {
                customLevelScene.ReturnToEditorMode();
                return true;
            }

            if (SceneManager.ActiveScene is LevelScene level)
            {
                foreach (Entity entity in level.GetEntities())
                {
                    if (entity is Player player)
                    {
                        if (player.IsLocalPlayer)
                        {
                            player.LocalTransform.Translation = player.SpawnPoint;
                            if (player.GetComponent<RigidBody2D>() is RigidBody2D rigidBody)
                            {
                                rigidBody.LinearVelocity = Vector2.Zero;
                                rigidBody.Awake = true;
                            }
                            SceneManager.ActiveCam2D?.Position = new Vector2(player.LocalTransform.Translation.X, player.LocalTransform.Translation.Y);   
                        }
                        
                        if (NetworkManager.Client == null || !NetworkManager.Client.IsConnected)
                        {
                            player.LocalTransform.Translation = player.SpawnPoint;
                            if (player.GetComponent<RigidBody2D>() is RigidBody2D rigidBody)
                            {
                                rigidBody.LinearVelocity = Vector2.Zero;
                                rigidBody.Awake = true;
                            }
                            SceneManager.ActiveCam2D?.Position = new Vector2(player.LocalTransform.Translation.X, player.LocalTransform.Translation.Y);   
                        }
                    }
                }
            }
            
            GuiManager.SetGui(null);
            return true;
        }));
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
