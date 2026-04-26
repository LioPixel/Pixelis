using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Scenes.Levels;
using Sparkle.CSharp;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.GUI.Loading;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrid;

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

        string controlText = "Controls:\nA/Left: LEFT\nD/Right: RIGHT\nSpace/Up: JUMP";
        LabelData controlLabelData = new LabelData(ContentRegistry.Fontoe, controlText, 18, color: Color.White);
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

        List<LabelData> options =
        [
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
        TextureButtonData textureButtonData = new TextureButtonData(ContentRegistry.UiButton,
            hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData textureButtonLabelData = new LabelData(ContentRegistry.Fontoe, "Play", 18, hoverColor: Color.White);

        this.AddElement("Texture-Button", new TextureButtonElement(textureButtonData, textureButtonLabelData,
            Anchor.Center, Vector2.Zero, size: new Vector2(230, 40), textOffset: new Vector2(0, 1),
            clickFunc: (element) =>
            {
                switch (dropDownElement.SelectedOption?.Text)
                {
                    case "Level 1":
                        AsyncOperation operation = SceneManager.LoadSceneAsync(new Level1(), new ProgressBarLoadingGui("Loading"));
                        operation.Completed += OnCompletedLoading;
                        break;

                    case "Level 2":
                        AsyncOperation operation2 = SceneManager.LoadSceneAsync(new Level2(), new ProgressBarLoadingGui("Loading"));
                        operation2.Completed += OnCompletedLoading;
                        break;

                    case "Level 3":
                        AsyncOperation operation3 = SceneManager.LoadSceneAsync(new Level3(), new ProgressBarLoadingGui("Loading"));
                        operation3.Completed += OnCompletedLoading;
                        break;

                    case "Level 4":
                        AsyncOperation operation4 = SceneManager.LoadSceneAsync(new Level4(), new ProgressBarLoadingGui("Loading"));
                        operation4.Completed += OnCompletedLoading;
                        break;

                    case "Level 5":
                        AsyncOperation operation5 = SceneManager.LoadSceneAsync(new Level5(), new ProgressBarLoadingGui("Loading"));
                        operation5.Completed += OnCompletedLoading;
                        break;

                    case "Level 6":
                        AsyncOperation operation6 = SceneManager.LoadSceneAsync(new Level6(), new ProgressBarLoadingGui("Loading"));
                        operation6.Completed += OnCompletedLoading;
                        break;

                    case "Level 7":
                        AsyncOperation operation7 = SceneManager.LoadSceneAsync(new Level7(), new ProgressBarLoadingGui("Loading"));
                        operation7.Completed += OnCompletedLoading;
                        break;

                    case "Level 8":
                        AsyncOperation operation8 = SceneManager.LoadSceneAsync(new Level8(), new ProgressBarLoadingGui("Loading"));
                        operation8.Completed += OnCompletedLoading;
                        break;

                    case "Level 9":
                        AsyncOperation operation9 = SceneManager.LoadSceneAsync(new Level9(), new ProgressBarLoadingGui("Loading"));
                        operation9.Completed += OnCompletedLoading;
                        break;

                    case "Level 10":
                        AsyncOperation operation10 = SceneManager.LoadSceneAsync(new Level10(), new ProgressBarLoadingGui("Loading"));
                        operation10.Completed += OnCompletedLoading;
                        break;
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
        Player player = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) });
        SceneManager.ActiveScene?.AddEntity(player);
    }
}
