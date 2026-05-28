using System.Numerics;
using System.Diagnostics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.Controls;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class MenuGui : Gui
{
    public MenuGui() : base("Menu", null)
    {
    }

    public string _nameInput = "Player";
    private TextureTextBoxElement? _nameInputBox;


    protected override void Init()
    {
        base.Init();

        GuiManager.SetScale(((PixelisGame)Game.Instance!).OptionsConfig.GetValue<int>("GuiScale"));

        LabelData fullscreen =
            new LabelData(ContentRegistry.Fontoe, Localization.T("gui.main_menu.fullscreen_hint"), 18, color: Color.White);
        this.AddElement("Fullscreen",
            new LabelElement(fullscreen, Anchor.BottomRight, new Vector2(0, 0), new Vector2(1.5F, 1.5F)));
        
        LabelData haveBug = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.main_menu.havebug"), 18, color: Color.White);
        this.AddElement("HaveBug", new LabelElement(haveBug, Anchor.BottomRight, new Vector2(0, -670), new Vector2(1.2F, 1.2F)));

        // Options button.
        TextureButtonData mailButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData mailButtonLabelData = GuiText.ButtonLabel(Localization.T("gui.main_menu.mail"), 230);

        this.AddElement("E-Mail-Button", new TextureButtonElement(mailButtonData, mailButtonLabelData, Anchor.BottomRight, new Vector2(-5, -630), size: new Vector2(160, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "mailto:kontakt@pixelis.net",
                    UseShellExecute = true
                });
                return true;
            }));

        ImageData logoData = new ImageData(ContentRegistry.Logo);
        this.AddElement("Logo", new ImageElement(logoData, Anchor.TopCenter, new Vector2(0, 50), scale: new Vector2(2,2)));

        LabelData controlLabelData = new LabelData(ContentRegistry.Fontoe, this.BuildControlText(), 18, color: Color.White);
        this.AddElement("Control-Label", new LabelElement(controlLabelData, Anchor.TopLeft, new Vector2(10, 10)));

        string creditsText = Localization.T("gui.main_menu.credits");
        LabelData creditsLabelData = new LabelData(ContentRegistry.Fontoe, creditsText, 18, color: Color.White);
        this.AddElement("Credits-Label", new LabelElement(creditsLabelData, Anchor.BottomLeft, new Vector2(10, -10)));

        // Button color.
        Color lightPurpleColor = new Color(147, 112, 219, 180);
        Color purpleColor = new Color(128, 0, 128, 180);
        Color darkPurpleColor = new Color(75, 0, 130, 180);

        // Texture drop down.
        TextureDropDownData multiplayerDropDownData = new TextureDropDownData(
            ContentRegistry.UiButton,
            ContentRegistry.UiMenu,
            ContentRegistry.UiMenu,
            ContentRegistry.UiSlider,
            ContentRegistry.UiArrow,
            sliderBarSourceRect: new Rectangle(2, 0, (int)ContentRegistry.UiMenu.Width - 2,
                (int)ContentRegistry.UiMenu.Height),
            fieldResizeMode: ResizeMode.NineSlice,
            menuResizeMode: ResizeMode.NineSlice,
            sliderBarResizeMode: ResizeMode.NineSlice,
            fieldBorderInsets: new BorderInsets(12),
            menuBorderInsets: new BorderInsets(5),
            sliderBarBorderInsets: new BorderInsets(5)
        );

        List<LabelData> multiplayerOptions =
        [
            new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.host"), 18),
            new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.join"), 18)
        ];

        TextureDropDownElement multiplayerdropDownElement = new TextureDropDownElement(
            multiplayerDropDownData,
            multiplayerOptions,
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

        multiplayerdropDownElement.MenuToggled += (isMenuOpen) =>
        {
            if (isMenuOpen)
            {
                if (multiplayerdropDownElement.Options.Count > multiplayerdropDownElement.MaxVisibleOptions)
                {
                    multiplayerdropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0,
                        (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height);
                }
            }
            else
            {
                multiplayerdropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0,
                    (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
            }
        };

        this.AddElement("Multiplayer-Drop-Down", multiplayerdropDownElement);

        // Texture drop down.
        TextureDropDownData selectionDropDownData = new TextureDropDownData(
            ContentRegistry.UiButton,
            ContentRegistry.UiMenu,
            ContentRegistry.UiMenu,
            ContentRegistry.UiSlider,
            ContentRegistry.UiArrow,
            sliderBarSourceRect: new Rectangle(2, 0, (int)ContentRegistry.UiMenu.Width - 2,
                (int)ContentRegistry.UiMenu.Height),
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
            new Vector2(200, 0),
            size: new Vector2(120, 40),
            scale: new Vector2(1, 1),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3)
        );

        dropDownElement.MenuToggled += (isMenuOpen) =>
        {
            if (isMenuOpen)
            {
                if (dropDownElement.Options.Count > dropDownElement.MaxVisibleOptions)
                {
                    dropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0,
                        (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height);
                }
            }
            else
            {
                dropDownElement.DropDownData.MenuSourceRect = new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width,
                    (int)ContentRegistry.UiMenu.Height);
            }
        };

        this.AddElement("Texture-Drop-Down", dropDownElement);

        // Texture button.
        TextureButtonData textureButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData textureButtonLabelData = GuiText.ButtonLabel(Localization.T("common.play"), 230);

        this.AddElement("Texture-Button", new TextureButtonElement(textureButtonData, textureButtonLabelData,
            Anchor.Center, Vector2.Zero, size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: (element) =>
            {
                Scene? selectedScene = LevelFactory.CreateByName(dropDownElement.SelectedOption?.Text ?? "Level 1");
                if (selectedScene != null)
                {
                    AsyncOperation operation = SceneManager.LoadSceneAsync(selectedScene, new ProgressBarLoadingGui("Loading", Localization.T("gui.loading.loading")));
                    operation.Completed += OnCompletedLoading;
                }
                
                return true;
            }));
        
        // Options button.
        TextureButtonData optionsButtonData = new TextureButtonData(ContentRegistry.UiButton,
            hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData optionsButtonLabelData =
            GuiText.ButtonLabel(Localization.T("common.options"), 230);

        this.AddElement("Options-Button", new TextureButtonElement(optionsButtonData, optionsButtonLabelData,
            Anchor.Center, new Vector2(0, 120), size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: (element) =>
            {
                GuiManager.SetGui(new OptionsGui());
                return true;
            }));

        // Exit button.
        TextureButtonData exitButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData exitButtonLabelData = GuiText.ButtonLabel(Localization.T("common.exit"), 230);

        this.AddElement("Exit-Button", new TextureButtonElement(exitButtonData, exitButtonLabelData, Anchor.Center,
            new Vector2(0, 180), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
            {
                GuiManager.SetGui(new ExitGui());
                return true;
            }));

        // Multiplayer button.
        TextureButtonData multiplayerButtonData = new TextureButtonData(ContentRegistry.UiButton,
            hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData multiplayerButtonLabelData =
            GuiText.ButtonLabel(Localization.T("gui.main_menu.multiplayer"), 230);

        this.AddElement("Multiplayer-Button", new TextureButtonElement(multiplayerButtonData,
            multiplayerButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1), clickFunc: (element) =>
            {
                switch (multiplayerdropDownElement.SelectedOption?.Text)
                {
                    case var text when text == Localization.T("gui.multiplayer.host"):
                        //Logger.Error("test");
                        GuiManager.SetGui(new HostGui());
                        break;

                    case var text when text == Localization.T("gui.multiplayer.join"):
                        GuiManager.SetGui(new JoinGui());
                        break;
                }

                return true;
            }));

        this.AddElement("Level-Editor-Button", new TextureButtonElement(optionsButtonData,
            GuiText.ButtonLabel(Localization.T("gui.level_editor.title"), 230),
            Anchor.Center, new Vector2(0, 120), size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                GuiManager.SetGui(new LevelEditorBrowserGui());
                return true;
            }));

        ((TextureButtonElement)this.GetElement("Options-Button")!).Offset = new Vector2(0, 180);
        ((TextureButtonElement)this.GetElement("Exit-Button")!).Offset = new Vector2(0, 240);
        
        // Discord button.
        TextureButtonData discordButtonData = new TextureButtonData(ContentRegistry.UiButton,hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData discordButtonLabelData = GuiText.ButtonLabel(Localization.T("common.discord"), 230);
        this.AddElement("Discord-Button", new TextureButtonElement(discordButtonData, discordButtonLabelData, Anchor.Center, new Vector2(520, 300), size: new Vector2(200, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/rK8jCuYpqn",
                UseShellExecute = true,
            });
                return true;
            }));
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (this.TryGetElement("Control-Label", out GuiElement? element) && element is LabelElement labelElement)
        {
            labelElement.Data.Text = this.BuildControlText();
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        IWindow window = GlobalGraphicsAssets.Window;

        // Background
        Texture2D backgroundTexture = ContentRegistry.Background2;
        Vector2 backgroundSize = new Vector2((float)window.GetWidth() / backgroundTexture.Width,
            (float)window.GetHeight() / backgroundTexture.Height);

        context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
        context.SpriteBatch.End();
        
        base.Draw(context, framebuffer);

    }

    private void OnCompletedLoading(bool success)
    {
        GuiManager.SetGui(null);
        if (SceneManager.ActiveScene is CustomLevelScene customLevelScene)
        {
            customLevelScene.SpawnGameplayPlayerAtOrigin();
            return;
        }

        Player player = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) });
        SceneManager.ActiveScene?.AddEntity(player);
    }

    private string BuildControlText()
    {
        return $"{Localization.T("gui.main_menu.controls")}:\n{KeyBindinds.GetMoveLeft()}: {Localization.T("input.left")}\n{KeyBindinds.GetMoveRight()}: {Localization.T("input.right")}\n{KeyBindinds.GetJump()}: {Localization.T("input.jump").ToUpperInvariant()}";
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
