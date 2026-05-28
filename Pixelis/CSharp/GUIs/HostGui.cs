using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Sprites;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class HostGui : Gui
{
    private string _errorMessage = "";
    private float _errorDisplayTime = 0f;
    private bool _isTutorialOpen = false;

    public HostGui () : base("Host", null) { }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.host"), 18);
        this.AddElement("Titel", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

        TextureButtonData infoButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData infoButtonLabelData = new LabelData(ContentRegistry.Fontoe, "!", 18, hoverColor: Color.White);
        this.AddElement("Tutorial-Info-Button", new TextureButtonElement(infoButtonData, infoButtonLabelData, Anchor.TopRight, new Vector2(-20, 20), size: new Vector2(40, 40), textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            SetTutorialVisible(true);
            return true;
        }));
        
        TextureButtonData backButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        string backText = Localization.T("common.back");
        Vector2 backButtonSize = GuiText.ButtonSize(backText, 110);
        LabelData backButtonLabelData = GuiText.ButtonLabel(backText, backButtonSize.X);
        
        this.AddElement("Options-Button", new TextureButtonElement(backButtonData, backButtonLabelData, Anchor.Center, new Vector2(-200, -120), size: backButtonSize, textOffset: new Vector2(0, 1), clickFunc: (element) => {
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
        
        LabelData slotslabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.host.slots"), 18);
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
        
        List<LabelData> options = LevelFactory.GetMenuLevelNames()
            .Select(levelName => new LabelData(ContentRegistry.Fontoe, levelName, 18))
            .ToList();
        
        TextureDropDownElement dropDownElement = new TextureDropDownElement(
            selectionDropDownData,
            options,
            4,
            Anchor.Center,
            new Vector2(190, 60),
            size: new Vector2(140, 40),
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
        LabelData nameHintTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.name_hint"), 18, color: Color.Gray);
        
        this.AddElement("Name-Text-Box", new TextureTextBoxElement(nameTextBoxData, nameTextBoxLabelData, nameHintTextBoxLabelData, Anchor.Center, new Vector2(120, -120), 15, TextAlignment.Center, new Vector2(0, 1), Vector2.One, (12, 12), new Vector2(230, 30), rotation: 0, clickFunc: (element) => {
            return true;
        }));

        LabelData errorLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, color: Color.Red);
        this.AddElement("Error-Label", new LabelElement(errorLabelData, Anchor.Center, new Vector2(0, 110), new Vector2(1, 1)));

        this.AddElement("Tutorial-Title", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, Localization.T("gui.host.tailscale.title"), 18, color: Color.White),
            Anchor.Center,
            new Vector2(0, -108),
            new Vector2(1.8F, 1.8F)));

        string tailscaleTutorialText = Localization.T("gui.host.tailscale.body");
        this.AddElement("Tutorial-Body", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, tailscaleTutorialText, 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, -8)));

        TextureButtonData tutorialCloseButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        string closeText = Localization.T("common.close");
        Vector2 closeButtonSize = GuiText.ButtonSize(closeText, 150);
        LabelData tutorialCloseButtonLabelData = GuiText.ButtonLabel(closeText, closeButtonSize.X);
        this.AddElement("Tutorial-Close-Button", new TextureButtonElement(tutorialCloseButtonData, tutorialCloseButtonLabelData, Anchor.Center, new Vector2(0, 112), size: closeButtonSize, textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            SetTutorialVisible(false);
            return true;
        }));

        SetTutorialVisible(false);
        
        // Host button.
        TextureButtonData createButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData createButtonLabelData = GuiText.ButtonLabel(Localization.T("gui.multiplayer.host"), 230);
        
        this.AddElement("Host-Button", new TextureButtonElement(createButtonData, createButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
        {
            GuiElement? slideBarElement = this.GetElement("Texture-Slider-Bar");
            
            if (slideBarElement is TextureSlideBarElement slideBar)
            {
                TextureTextBoxElement nameTextBox = (TextureTextBoxElement)this.GetElement("Name-Text-Box");
                string username = nameTextBox.LabelData.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = Localization.T("network.player.default_name");
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
        
        if (_isTutorialOpen && Input.IsKeyPressed(KeyboardKey.Escape))
        {
            SetTutorialVisible(false);
            return;
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

    private void SetTutorialVisible(bool visible)
    {
        _isTutorialOpen = visible;
        ToggleHostElements(!visible);
        ToggleElement("Tutorial-Title", visible);
        ToggleElement("Tutorial-Body", visible);
        ToggleElement("Tutorial-Close-Button", visible);
        ToggleElement("Tutorial-Info-Button", !visible);
    }

    private void ToggleHostElements(bool visible)
    {
        ToggleElement("Titel", visible);
        ToggleElement("Options-Button", visible);
        ToggleElement("slots", visible);
        ToggleElement("2", visible);
        ToggleElement("10", visible);
        ToggleElement("slot-value", visible);
        ToggleElement("Texture-Slider-Bar", visible);
        ToggleElement("Texture-Drop-Down", visible);
        ToggleElement("Name-Text-Box", visible);
        ToggleElement("Error-Label", visible);
        ToggleElement("Host-Button", visible);
    }

    private void ToggleElement(string name, bool visible)
    {
        GuiElement? element = this.GetElement(name);
        if (element == null)
        {
            return;
        }

        element.Enabled = visible;
        element.Interactable = visible;
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
        
        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, ModalGuiRenderer.DefaultBaseSize);

        if (_isTutorialOpen)
        {
            float scale = this.ScaleFactor;
            Vector2 windowSize = new Vector2(GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight());
            Vector2 modalSize = new Vector2(560, 360) * scale;
            Vector2 modalPosition = new Vector2(
                MathF.Floor((windowSize.X - modalSize.X) / (2F * scale)) * scale,
                MathF.Floor((windowSize.Y - modalSize.Y) / (2F * scale)) * scale);

            context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(0, 0, windowSize.X, windowSize.Y),
                color: new Color(0, 0, 0, 150));
            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(modalPosition.X, modalPosition.Y, modalSize.X, modalSize.Y),
                color: new Color(35, 35, 35, 235));
            context.PrimitiveBatch.DrawEmptyRectangle(
                new RectangleF(modalPosition.X, modalPosition.Y, modalSize.X, modalSize.Y),
                3 * scale,
                color: Color.White);
            context.PrimitiveBatch.End();
        }

        
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
    }
}
