using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Sprites;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Veldrid;

namespace Pixelis.CSharp.GUIs;

public class HostGui : Gui
{
    private string _errorMessage = "";
    private float _errorDisplayTime = 0f;

    public HostGui () : base("Host", null) { }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, "Host", 18);
        this.AddElement("Titel", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));
        
        TextureButtonData backButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData backButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Back", 18, hoverColor: Color.White);
        
        this.AddElement("Options-Button", new TextureButtonElement(backButtonData, backButtonLabelData, Anchor.Center, new Vector2(-200, -120), size: new Vector2(100, 40), textOffset: new Vector2(0, 1), clickFunc: (element) => {
            if (SceneManager.ActiveScene != null)
            {
                GuiManager.SetGui(new PauseMenuGui());
            }
            else
            {
                GuiManager.SetGui(new MenuGui());
            }
            return true;
        }));
        
        LabelData slotslabelData = new LabelData(ContentRegistry.Fontoe, "Slots", 18);
        this.AddElement("slots", new LabelElement(slotslabelData, Anchor.Center, new Vector2(0, -70), new Vector2(1.5F,1.5F)));
        
        LabelData twolabelData = new LabelData(ContentRegistry.Fontoe, "2", 18);
        this.AddElement("2", new LabelElement(twolabelData, Anchor.Center, new Vector2(-130, -30), new Vector2(1.5F,1.5F)));
        
        LabelData tenlabelData = new LabelData(ContentRegistry.Fontoe, "10", 18);
        this.AddElement("10", new LabelElement(tenlabelData, Anchor.Center, new Vector2(130, -30), new Vector2(1.5F,1.5F)));
        
        LabelData slotValueData = new LabelData(ContentRegistry.Fontoe, "", 18);
        this.AddElement("slot-value", new LabelElement(slotValueData, Anchor.Center, new Vector2(0, -30), new Vector2(1.5F,1.5F)));
        
        // Texture slider bar.
        TextureSlideBarData textureSlideBarData = new TextureSlideBarData(
            ContentRegistry.UiBar,
            null,
            ContentRegistry.UiSliderLowRes,
            barResizeMode: ResizeMode.NineSlice,
            filledBarResizeMode: ResizeMode.NineSlice,
            barBorderInsets: new BorderInsets(3),
            filledBarBorderInsets: new BorderInsets(3));
        
        this.AddElement("Texture-Slider-Bar", new TextureSlideBarElement(textureSlideBarData, Anchor.Center, new Vector2(0, 0), 2, 10, value: 2, wholeNumbers: true, size: new Vector2(140, 8), scale: new Vector2(2, 2), clickFunc: (element) => {
            return true;
        }));
        
        // Texture drop down.
        TextureDropDownData selectionDropDownData = new TextureDropDownData(
            ContentRegistry.UiButton,
            ContentRegistry.UiMenu,
            ContentRegistry.UiMenu,
            ContentRegistry.UiSlider,
            ContentRegistry.UiArrow,
            sliderBarSourceRect: new Rectangle(2, 0, (int) ContentRegistry.UiMenu.Width - 2, (int) ContentRegistry.UiMenu.Height),
            fieldResizeMode: ResizeMode.NineSlice,
            menuResizeMode: ResizeMode.NineSlice,
            sliderBarResizeMode: ResizeMode.NineSlice,
            fieldBorderInsets: new BorderInsets(12),
            menuBorderInsets: new BorderInsets(5),
            sliderBarBorderInsets: new BorderInsets(5)
        );
        
        List<LabelData> options = [
            new LabelData(ContentRegistry.Fontoe, "Level 1", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 2", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 3", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 4", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 5", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 6", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 7", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 8", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 9", 18),
            new LabelData(ContentRegistry.Fontoe, "Level 10", 18),
        ];
        
        TextureDropDownElement dropDownElement = new TextureDropDownElement(
            selectionDropDownData,
            options,
            4,
            Anchor.Center,
            new Vector2(200, 60),
            size: new Vector2(120, 40),
            scale: new Vector2(1, 1),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3)
        );
        
        dropDownElement.MenuToggled += (isMenuOpen) => {
            if (isMenuOpen) {
                if (dropDownElement.Options.Count > dropDownElement.MaxVisibleOptions) {
                    dropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0, (int) ContentRegistry.UiMenu.Width - 2, (int) ContentRegistry.UiMenu.Height);
                } 
            }
            else {
                dropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0, (int) ContentRegistry.UiMenu.Width, (int) ContentRegistry.UiMenu.Height);
            }
        };
        
        this.AddElement("Texture-Drop-Down", dropDownElement);
        
        // Texture text box.
        TextureTextBoxData nameTextBoxData = new TextureTextBoxData(ContentRegistry.UiMenu, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12), flip: SpriteFlip.None);
        LabelData nameTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, hoverColor: Color.White);
        LabelData nameHintTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, "Type your name...", 18, color: Color.Gray);
        
        this.AddElement("Name-Text-Box", new TextureTextBoxElement(nameTextBoxData, nameTextBoxLabelData, nameHintTextBoxLabelData, Anchor.Center, new Vector2(120, -120), 15, TextAlignment.Center, new Vector2(0, 1), (12, 12), new Vector2(230, 30), rotation: 0, clickFunc: (element) => {
            return true;
        }));

        LabelData errorLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, color: Color.Red);
        this.AddElement("Error-Label", new LabelElement(errorLabelData, Anchor.Center, new Vector2(0, 110), new Vector2(1, 1)));
        
        // Host button.
        TextureButtonData createButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData createButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Host", 18, hoverColor: Color.White);
        
        this.AddElement("Host-Button", new TextureButtonElement(createButtonData, createButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
        {
            GuiElement? slideBarElement = this.GetElement("Texture-Slider-Bar");
            
            if (slideBarElement is TextureSlideBarElement slideBar)
            {
                TextureTextBoxElement nameTextBox = (TextureTextBoxElement)this.GetElement("Name-Text-Box");
                string username = nameTextBox.LabelData.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = "Player";
                }

                if (NetworkManager.CreateServer((ushort) slideBar.Value, dropDownElement.SelectedOption?.Text ?? "Level 1", username, out string errorMessage))
                {
                    _errorMessage = "";
                    UpdateErrorLabel();
                    GuiManager.SetGui(null);
                    Logger.Info("SERVER STARTED!!!!");
                }
                else
                {
                    _errorMessage = errorMessage;
                    _errorDisplayTime = 5f;
                    UpdateErrorLabel();
                }
            }
            
            return true;
        }));
    }
    
    protected override void Update(double delta)
    {
        base.Update(delta);

        if (_errorDisplayTime > 0)
        {
            _errorDisplayTime -= (float)delta;
            if (_errorDisplayTime <= 0)
            {
                _errorMessage = "";
                UpdateErrorLabel();
            }
        }

        GuiElement? slotValueElement = this.GetElement("slot-value");
        GuiElement? slideBarElement = this.GetElement("Texture-Slider-Bar");
        
        if (slideBarElement is TextureSlideBarElement slideBar)
        {
            if (slotValueElement is LabelElement slotValue)
            {
                slotValue.Data.Text = slideBar.Value + "";
            }
        }
        
        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {

            if (SceneManager.ActiveScene == null)
            {
                GuiManager.SetGui(new MenuGui());
            }
            else
            {
                GuiManager.SetGui(new PauseMenuGui());
            }
        }
    }

    private void UpdateErrorLabel()
    {
        LabelElement errorLabel = (LabelElement)this.GetElement("Error-Label");
        if (errorLabel != null)
        {
            errorLabel.Data.Text = _errorMessage;
        }
    }
    
    
    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        if (SceneManager.ActiveScene == null)
        {
            IWindow window = GlobalGraphicsAssets.Window;
        
            // Background
            Texture2D backgroundTexture = ContentRegistry.Background2;
            Vector2 backgroundSize = new Vector2((float) window.GetWidth() / backgroundTexture.Width, (float) window.GetHeight() / backgroundTexture.Height);
        
            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
            context.SpriteBatch.End();
        }
        
        float scale = this.ScaleFactor;
            
        // Define base virtual size (e.g., half of 1280x720) and scale it
        Vector2 baseSize = new Vector2(550, 310);
        Vector2 scaledSize = baseSize * scale;

        // Snap window dimensions to scale grid to find the "scaled" center
        float screenWidth = MathF.Floor(GlobalGraphicsAssets.Window.GetWidth() / (float) scale) * scale;
        float screenHeight = MathF.Floor(GlobalGraphicsAssets.Window.GetHeight() / (float) scale) * scale;
        
        Vector2 pos = new Vector2(
            MathF.Floor((screenWidth / 2.0F - scaledSize.X / 2.0F) / scale) * scale,
            MathF.Floor((screenHeight / 2.0F - scaledSize.Y / 2.0F) / scale) * scale
        );
        
        // Draw gui rectangle
        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            
        // Overlay (Full screen)
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight()), color: new Color(128, 128, 128, 128));
            
        // Background Box
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(pos.X, pos.Y, scaledSize.X, scaledSize.Y), color: new Color(128, 128, 128, 128));
            
        // Border
        context.PrimitiveBatch.DrawEmptyRectangle(new RectangleF(pos.X, pos.Y, scaledSize.X, scaledSize.Y), 4 * scale, color: new Color(64, 64, 64, 128));
            
        context.PrimitiveBatch.End();
        
        
        
        base.Draw(context, framebuffer);
    }
}
