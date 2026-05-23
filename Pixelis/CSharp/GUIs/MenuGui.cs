using System.Numerics;
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

        GuiManager.Scale = MathF.Max(1.0F, MathF.Round(((PixelisGame)Game.Instance!).OptionsConfig.GetValue<float>("GuiScale")));

        LabelData fullscreen =
            new LabelData(ContentRegistry.Fontoe, "please go to full screen", 18, color: Color.White);
        this.AddElement("fullscreen",
            new LabelElement(fullscreen, Anchor.BottomRight, new Vector2(0, 0), new Vector2(1.5F, 1.5F)));


        ImageData logoData = new ImageData(ContentRegistry.Logo);
        this.AddElement("logo", new ImageElement(logoData, Anchor.TopCenter, new Vector2(0, 50), scale: new Vector2(2,2)));

        LabelData controlLabelData = new LabelData(ContentRegistry.Fontoe, this.BuildControlText(), 18, color: Color.White);
        this.AddElement("Control-Label", new LabelElement(controlLabelData, Anchor.TopLeft, new Vector2(10, 10)));

        string creditsText = "Credits:\nLio: Developer/Designer\nMrScautHD: Developer";
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
            new LabelData(ContentRegistry.Fontoe, "Host", 18),
            new LabelData(ContentRegistry.Fontoe, "Join", 18)
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
        LabelData textureButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Play", 18, hoverColor: Color.White);

        this.AddElement("Texture-Button", new TextureButtonElement(textureButtonData, textureButtonLabelData,
            Anchor.Center, Vector2.Zero, size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: (element) =>
            {
                Scene? selectedScene = LevelFactory.CreateByName(dropDownElement.SelectedOption?.Text ?? "Level 1");
                if (selectedScene != null)
                {
                    AsyncOperation operation = SceneManager.LoadSceneAsync(selectedScene, new ProgressBarLoadingGui("Loading"));
                    operation.Completed += OnCompletedLoading;
                }
                
                return true;
            }));
        
        // Options button.
        TextureButtonData optionsButtonData = new TextureButtonData(ContentRegistry.UiButton,
            hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData optionsButtonLabelData =
            new LabelData(ContentRegistry.Fontoe, "Options", 18, hoverColor: Color.White);

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
        LabelData exitButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Exit", 18, hoverColor: Color.White);

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
            new LabelData(ContentRegistry.Fontoe, "Multiplayer", 18, hoverColor: Color.White);

        this.AddElement("Multiplayer-Button", new TextureButtonElement(multiplayerButtonData,
            multiplayerButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1), clickFunc: (element) =>
            {
                switch (multiplayerdropDownElement.SelectedOption?.Text)
                {
                    case "Host":
                        //Logger.Error("test");
                        GuiManager.SetGui(new HostGui());
                        break;

                    case "Join":
                        GuiManager.SetGui(new JoinGui());
                        break;
                }

                return true;
            }));

        this.AddElement("Level-Editor-Button", new TextureButtonElement(optionsButtonData,
            new LabelData(ContentRegistry.Fontoe, "Level Editor", 18, hoverColor: Color.White),
            Anchor.Center, new Vector2(0, 120), size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                GuiManager.SetGui(new LevelEditorBrowserGui());
                return true;
            }));

        ((TextureButtonElement)this.GetElement("Options-Button")!).Offset = new Vector2(0, 180);
        ((TextureButtonElement)this.GetElement("Exit-Button")!).Offset = new Vector2(0, 240);
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
        return $"Controls:\n{KeyBindinds.GetMoveLeft()}: LEFT\n{KeyBindinds.GetMoveRight()}: RIGHT\n{KeyBindinds.GetJump()}: JUMP";
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
