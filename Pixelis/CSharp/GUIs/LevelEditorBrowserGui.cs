using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Levels;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrid;

namespace Pixelis.CSharp.GUIs;

public class LevelEditorBrowserGui : Gui
{
    private TextureDropDownElement? _savedLevelsDropDown;
    private TextureButtonElement? _openButton;
    private TextureButtonElement? _deleteButton;
    private string _statusMessage = string.Empty;

    public LevelEditorBrowserGui() : base("LevelEditorBrowser", null)
    {
    }

    protected override void Init()
    {
        base.Init();

        this.AddElement("Title", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, "Level Editor", 18),
            Anchor.TopCenter,
            new Vector2(0, 70),
            new Vector2(4, 4)));

        TextureButtonData buttonData = new TextureButtonData(
            ContentRegistry.UiButton,
            hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice,
            borderInsets: new BorderInsets(12));

        TextureDropDownData dropDownData = new TextureDropDownData(
            ContentRegistry.UiButton,
            ContentRegistry.UiMenu,
            ContentRegistry.UiMenu,
            ContentRegistry.UiSlider,
            ContentRegistry.UiArrow,
            sliderBarSourceRect: new Rectangle(2, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height),
            fieldResizeMode: ResizeMode.NineSlice,
            menuResizeMode: ResizeMode.NineSlice,
            sliderBarResizeMode: ResizeMode.NineSlice,
            fieldBorderInsets: new BorderInsets(12),
            menuBorderInsets: new BorderInsets(5),
            sliderBarBorderInsets: new BorderInsets(5));

        // Only show user-saved levels in the editor browser. Shipped levels in content/levels are read-only.
        List<string> savedLevels = CustomLevelStorage.GetSavedLevelNames();
        List<LabelData> options = savedLevels.Count > 0
            ? savedLevels.Select(level => new LabelData(ContentRegistry.Fontoe, level, 18)).ToList()
            : [new LabelData(ContentRegistry.Fontoe, "No Levels Yet", 18, color: Color.Gray)];

        _savedLevelsDropDown = new TextureDropDownElement(
            dropDownData,
            options,
            5,
            Anchor.Center,
            new Vector2(0, -40),
            size: new Vector2(280, 40),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3));

        _savedLevelsDropDown.MenuToggled += isMenuOpen =>
        {
            _savedLevelsDropDown.DropDownData.MenuSourceRect = isMenuOpen && _savedLevelsDropDown.Options.Count > _savedLevelsDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height)
                : new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
        };

        _openButton = new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Open", 18, hoverColor: Color.White),
            Anchor.Center,
            new Vector2(0, 30),
            size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                if (_savedLevelsDropDown?.SelectedOption == null || savedLevels.Count == 0)
                {
                    return true;
                }

                CustomLevelData? existingLevel = CustomLevelStorage.LoadByName(_savedLevelsDropDown.SelectedOption.Text);
                if (existingLevel == null)
                {
                    return true;
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(new CustomLevelScene(existingLevel, true), new ProgressBarLoadingGui("Loading"));
                operation.Completed += _ => { };
                return true;
            });

        _openButton.Interactable = savedLevels.Count > 0;
        this.AddElement("Open-Button", _openButton);

        _deleteButton = new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Delete", 18, hoverColor: Color.White),
            Anchor.Center,
            new Vector2(0, 90),
            size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                if (_savedLevelsDropDown?.SelectedOption == null || savedLevels.Count == 0)
                {
                    return true;
                }

                if (CustomLevelStorage.DeleteByName(_savedLevelsDropDown.SelectedOption.Text))
                {
                    GuiManager.SetGui(new LevelEditorBrowserGui());
                }
                else
                {
                    _statusMessage = "Could not delete level.";
                    UpdateStatusLabel();
                }

                return true;
            });

        _deleteButton.Interactable = savedLevels.Count > 0;
        this.AddElement("Delete-Button", _deleteButton);

        this.AddElement("New-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "New", 18, hoverColor: Color.White),
            Anchor.Center,
            new Vector2(0, 150),
            size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(
                    new CustomLevelScene(CustomLevelStorage.CreateNew(), true),
                    new ProgressBarLoadingGui("Loading"));
                operation.Completed += _ => { };
                return true;
            }));

        this.AddElement("Back-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Back", 18, hoverColor: Color.White),
            Anchor.Center,
            new Vector2(0, 210),
            size: new Vector2(230, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                GuiManager.SetGui(new MenuGui());
                return true;
            }));

        this.AddElement("Status-Label", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, _statusMessage, 18, color: Color.Red),
            Anchor.Center,
            new Vector2(0, 260)));
        
        // SAVED LEVELS DROP DOWN MENU GET ADDED HERE BECAUSE OF SORTING ISSUES
        this.AddElement("Saved-Levels", _savedLevelsDropDown);
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            GuiManager.SetGui(new MenuGui());
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        IWindow window = GlobalGraphicsAssets.Window;
        Texture2D backgroundTexture = ContentRegistry.Background2;
        Vector2 backgroundSize = new(
            (float)window.GetWidth() / backgroundTexture.Width,
            (float)window.GetHeight() / backgroundTexture.Height);

        context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
        context.SpriteBatch.End();

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(0, 0, window.GetWidth(), window.GetHeight()),
            color: new Color(20, 20, 20, 120));
        context.PrimitiveBatch.End();

        base.Draw(context, framebuffer);
    }

    private void UpdateStatusLabel()
    {
        if (this.GetElement("Status-Label") is LabelElement labelElement)
        {
            labelElement.Data.Text = _statusMessage;
        }
    }
}
