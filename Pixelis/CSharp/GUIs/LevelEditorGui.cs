using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Veldrith;
using static Pixelis.CSharp.Scenes.CustomLevelScene;

namespace Pixelis.CSharp.GUIs;

public class LevelEditorGui : Gui
{
    private readonly CustomLevelScene _scene;
    private TextureTextBoxElement? _saveNameTextBox;
    private LabelElement? _statusLabel;
    private TextureDropDownElement? _placeableDropDown;
    private TextureDropDownElement? _movingBlockSpeedDropDown;
    private TextureDropDownElement? _placeableDepthDropDown;
    private TextureDropDownElement? _nextLevelDropDown;

    public bool IsSaveDialogOpen => _saveNameTextBox?.Enabled ?? false;

    public LevelEditorGui(CustomLevelScene scene) : base("LevelEditorOverlay", null)
    {
        _scene = scene;
    }

    protected override void Init()
    {
        base.Init();

        TextureButtonData buttonData = new TextureButtonData(
            ContentRegistry.UiButton,
            hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice,
            borderInsets: new BorderInsets(12));

        TextureTextBoxData textBoxData = new TextureTextBoxData(
            ContentRegistry.UiMenu,
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

        this.AddElement("Save-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Save", 18, hoverColor: Color.White),
            Anchor.TopLeft,
            new Vector2(20, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                SetSaveDialogVisible(true);
                return true;
            }));

        this.AddElement("Play-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Play", 18, hoverColor: Color.White),
            Anchor.TopLeft,
            new Vector2(340, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.StartEditorPlayMode();
                SetStatus("Player spawned at 0, 0");
                return true;
            }));

        List<LabelData> placeableOptions = CustomLevelScene.GetAvailablePlaceables()
            .Select(placeable => new LabelData(ContentRegistry.Fontoe, CustomLevelScene.GetPlaceableDisplayName(placeable), 18))
            .ToList();

        _placeableDropDown = new TextureDropDownElement(
            dropDownData,
            placeableOptions,
            6,
            Anchor.TopRight,
            new Vector2(-20, 20),
            size: new Vector2(220, 40),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3));

        _placeableDropDown.MenuToggled += isMenuOpen =>
        {
            _placeableDropDown.DropDownData.MenuSourceRect = isMenuOpen && _placeableDropDown.Options.Count > _placeableDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height)
                : new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
        };

        _placeableDropDown.OptionChanged += option =>
        {
            _scene.SuppressPlacementUntilMouseRelease();
            if (TryGetPlaceableByLabel(option.Text, out PlaceableType selectedPlaceable))
            {
                _scene.SetPlaceable(selectedPlaceable);
            }
        };

        this.AddElement("Placeable-Drop-Down", _placeableDropDown);

        List<LabelData> movingSpeedOptions = GetMovingSpeedOptions(_scene.SelectedMovingBlockSpeed);
        _movingBlockSpeedDropDown = new TextureDropDownElement(
            dropDownData,
            movingSpeedOptions,
            5,
            Anchor.TopRight,
            new Vector2(-400, 20),
            size: new Vector2(130, 40),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3));
        _movingBlockSpeedDropDown.MenuToggled += isMenuOpen =>
        {
            _movingBlockSpeedDropDown.DropDownData.MenuSourceRect = isMenuOpen && _movingBlockSpeedDropDown.Options.Count > _movingBlockSpeedDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height)
                : new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
        };
        _movingBlockSpeedDropDown.OptionChanged += option =>
        {
            _scene.SuppressPlacementUntilMouseRelease();
            if (TryParseMovingBlockSpeed(option.Text, out float speed))
            {
                _scene.SetMovingBlockSpeed(speed);
            }
        };
        this.AddElement("Moving-Block-Speed-Drop-Down", _movingBlockSpeedDropDown);

        List<LabelData> depthOptions = GetDepthOptions(_scene.SelectedPlaceableDepth);
        _placeableDepthDropDown = new TextureDropDownElement(
            dropDownData,
            depthOptions,
            5,
            Anchor.TopRight,
            new Vector2(-260, 20),
            size: new Vector2(130, 40),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3));
        _placeableDepthDropDown.MenuToggled += isMenuOpen =>
        {
            _placeableDepthDropDown.DropDownData.MenuSourceRect = isMenuOpen && _placeableDepthDropDown.Options.Count > _placeableDepthDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height)
                : new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
        };
        _placeableDepthDropDown.OptionChanged += option =>
        {
            _scene.SuppressPlacementUntilMouseRelease();
            if (TryParseDepth(option.Text, out float depth))
            {
                _scene.SetPlaceableDepth(depth);
            }
        };
        this.AddElement("Placeable-Depth-Drop-Down", _placeableDepthDropDown);

        this.AddElement("Back-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "Menu", 18, hoverColor: Color.White),
            Anchor.TopLeft,
            new Vector2(180, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.BackToBrowser();
                return true;
            }));

        this.AddElement("Place-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "", 18, hoverColor: Color.White),
            Anchor.BottomCenter,
            new Vector2(-80, -30),
            size: new Vector2(130, 50),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.SetTool(EditorTool.Place);
                return true;
            }));

        this.AddElement("Eraser-Button", new TextureButtonElement(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, "", 18, hoverColor: Color.White),
            Anchor.BottomCenter,
            new Vector2(65, -30),
            size: new Vector2(130, 50),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.SetTool(EditorTool.Eraser);
                return true;
            }));

        this.AddElement("Hotbar-Label", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, "Hotbar", 18, color: Color.White),
            Anchor.BottomCenter,
            new Vector2(0, -92),
            new Vector2(1.5F, 1.5F)));

        _statusLabel = new LabelElement(
            new LabelData(ContentRegistry.Fontoe, "WASD / arrow keys move the camera", 18, color: Color.White),
            Anchor.BottomLeft,
            new Vector2(20, -24));
        this.AddElement("Status-Label", _statusLabel);
        
        this.AddElement("Save-Confirm-Button", CreateModalButton(buttonData, "Save", new Vector2(-85, 100), () =>
        {
            if (_saveNameTextBox == null)
            {
                return;
            }

            try
            {
                if (_scene.HasWinFlag && _nextLevelDropDown?.SelectedOption != null)
                {
                    _scene.SetNextLevelName(_nextLevelDropDown.SelectedOption.Text);
                }

                _scene.SaveLevel(_saveNameTextBox.LabelData.Text);
                SetStatus($"Saved: {_saveNameTextBox.LabelData.Text.Trim()}");
                SetSaveDialogVisible(false);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
            }
        }));

        this.AddElement("Save-Cancel-Button", CreateModalButton(buttonData, "Cancel", new Vector2(85, 100), () =>
        {
            SetSaveDialogVisible(false);
        }));


        _saveNameTextBox = new TextureTextBoxElement(
            textBoxData,
            new LabelData(ContentRegistry.Fontoe, _scene.LevelName, 18, hoverColor: Color.White),
            new LabelData(ContentRegistry.Fontoe, "Level name...", 18, color: Color.Gray),
            Anchor.Center,
            new Vector2(0, -25),
            30,
            TextAlignment.Center,
            new Vector2(0, 1),
            Vector2.One,
            (12, 12),
            new Vector2(320, 34),
            clickFunc: _ => true);
        _saveNameTextBox.Enabled = false;
        this.AddElement("Save-Name-TextBox", _saveNameTextBox);

        List<LabelData> nextLevelOptions = GetNextLevelOptions(_scene.NextLevelName);
        _nextLevelDropDown = new TextureDropDownElement(
            dropDownData,
            nextLevelOptions,
            6,
            Anchor.Center,
            new Vector2(0, 42),
            size: new Vector2(320, 40),
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            scrollMaskInsets: (3, 3));
        _nextLevelDropDown.Enabled = false;
        _nextLevelDropDown.Interactable = false;
        _nextLevelDropDown.MenuToggled += isMenuOpen =>
        {
            _nextLevelDropDown.DropDownData.MenuSourceRect = isMenuOpen && _nextLevelDropDown.Options.Count > _nextLevelDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width - 2, (int)ContentRegistry.UiMenu.Height)
                : new Rectangle(0, 0, (int)ContentRegistry.UiMenu.Width, (int)ContentRegistry.UiMenu.Height);
        };
        this.AddElement("Next-Level-Drop-Down", _nextLevelDropDown);
        
        this.AddElement("Save-Dialog-Title", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, "Save Level", 18, color: Color.White),
            Anchor.Center,
            new Vector2(0, -88),
            new Vector2(2, 2)));

        this.AddElement("Save-Dialog-Hint", new LabelElement(
            new LabelData(ContentRegistry.Fontoe, "Choose the name for this level", 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, -60)));

        LabelElement nextLevelLabel = new(
            new LabelData(ContentRegistry.Fontoe, "Win Flag -> Next Level", 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, 12));
        nextLevelLabel.Enabled = false;
        nextLevelLabel.Interactable = false;
        this.AddElement("Next-Level-Label", nextLevelLabel);

        SetSaveDialogVisible(false);
        RefreshEditorState();
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (IsSaveDialogOpen && Input.IsKeyPressed(KeyboardKey.Escape))
        {
            SetSaveDialogVisible(false);
            return;
        }

        if (IsSaveDialogOpen && Input.IsKeyPressed(KeyboardKey.Enter))
        {
            GuiElement? confirmButton = this.GetElement("Save-Confirm-Button");
            if (confirmButton is TextureButtonElement && _saveNameTextBox != null)
            {
                try
                {
                    if (_scene.HasWinFlag && _nextLevelDropDown?.SelectedOption != null)
                    {
                        _scene.SetNextLevelName(_nextLevelDropDown.SelectedOption.Text);
                    }

                    _scene.SaveLevel(_saveNameTextBox.LabelData.Text);
                    SetStatus($"Saved: {_saveNameTextBox.LabelData.Text.Trim()}");
                    SetSaveDialogVisible(false);
                }
                catch (Exception exception)
                {
                    SetStatus(exception.Message);
                }
            }
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        float scale = this.ScaleFactor;
        Vector2 snappedWindowSize = GetSnappedWindowSize(scale);
        float topBarHeight = 84 * scale;
        float bottomBarHeight = 130 * scale;

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(0, 0, snappedWindowSize.X, topBarHeight),
            color: new Color(15, 15, 15, 160));
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(0, snappedWindowSize.Y - bottomBarHeight, snappedWindowSize.X, bottomBarHeight),
            color: new Color(15, 15, 15, 160));

        if (IsSaveDialogOpen)
        {
            Vector2 modalSize = new Vector2(420, 280) * scale;
            Vector2 modalPosition = GetCenteredRectanglePosition(snappedWindowSize, modalSize, scale);

            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight()),
                color: new Color(0, 0, 0, 150));
            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(modalPosition.X, modalPosition.Y, modalSize.X, modalSize.Y),
                color: new Color(35, 35, 35, 235));
            context.PrimitiveBatch.DrawEmptyRectangle(
                new RectangleF(modalPosition.X, modalPosition.Y, modalSize.X, modalSize.Y),
                3 * scale,
                color: Color.White);
        }

        context.PrimitiveBatch.End();

        base.Draw(context, framebuffer);
    }

    private static Vector2 GetSnappedWindowSize(float scale)
    {
        float width = MathF.Floor(GlobalGraphicsAssets.Window.GetWidth() / scale) * scale;
        float height = MathF.Floor(GlobalGraphicsAssets.Window.GetHeight() / scale) * scale;
        return new Vector2(width, height);
    }

    private static Vector2 GetCenteredRectanglePosition(Vector2 windowSize, Vector2 rectangleSize, float scale)
    {
        return new Vector2(
            MathF.Floor((windowSize.X / 2.0F - rectangleSize.X / 2.0F) / scale) * scale,
            MathF.Floor((windowSize.Y / 2.0F - rectangleSize.Y / 2.0F) / scale) * scale
        );
    }

    public bool IsPointOverExtendedUi(Vector2 point)
    {
        return this.GetElements()
            .OfType<TextureDropDownElement>()
            .Where(dropDown => dropDown.Enabled)
            .Any(dropDown => IsPointInsideDropDown(dropDown, point));
    }

    public void RefreshEditorState()
    {
        SetButtonLabel("Place-Button", _scene.SelectedTool == EditorTool.Place ? "[Place]" : "Place");
        SetButtonLabel("Eraser-Button", _scene.SelectedTool == EditorTool.Eraser ? "[Eraser]" : "Eraser");
        ToggleElement("Moving-Block-Speed-Drop-Down", _scene.SelectedPlaceable == PlaceableType.MovingBlock);
        ToggleElement("Placeable-Depth-Drop-Down", true);
        SetStatus(_scene.GetEditorStatusMessage());
    }

    private TextureButtonElement CreateModalButton(TextureButtonData buttonData, string text, Vector2 offset, Action onClick)
    {
        TextureButtonElement button = new(
            buttonData,
            new LabelData(ContentRegistry.Fontoe, text, 18, hoverColor: Color.White),
            Anchor.Center,
            offset,
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                onClick();
                return true;
            });

        button.Enabled = false;
        return button;
    }

    private void SetSaveDialogVisible(bool visible)
    {
        ToggleElement("Save-Name-TextBox", visible);
        ToggleElement("Save-Confirm-Button", visible);
        ToggleElement("Save-Cancel-Button", visible);
        ToggleElement("Save-Dialog-Title", visible);
        ToggleElement("Save-Dialog-Hint", visible);
        ToggleElement("Next-Level-Label", visible && _scene.HasWinFlag);
        ToggleElement("Next-Level-Drop-Down", visible && _scene.HasWinFlag);

        if (visible && _saveNameTextBox != null)
        {
            _saveNameTextBox.LabelData.Text = _scene.LevelName;
        }

        if (visible && _nextLevelDropDown != null)
        {
            RebuildDropDownOptions(_nextLevelDropDown, GetNextLevelOptions(_scene.NextLevelName));
        }
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

    private void SetButtonLabel(string name, string text)
    {
        if (this.GetElement(name) is TextureButtonElement button)
        {
            button.LabelData.Text = text;
        }
    }

    private static bool TryGetPlaceableByLabel(string label, out PlaceableType placeable)
    {
        foreach (PlaceableType availablePlaceable in CustomLevelScene.GetAvailablePlaceables())
        {
            if (string.Equals(CustomLevelScene.GetPlaceableDisplayName(availablePlaceable), label, StringComparison.OrdinalIgnoreCase))
            {
                placeable = availablePlaceable;
                return true;
            }
        }

        placeable = PlaceableType.Block;
        return false;
    }

    private static List<LabelData> GetMovingSpeedOptions(float selectedSpeed)
    {
        float[] values = [selectedSpeed, 0.5F, 1F, 1.5F, 2F, 3F];
        return values
            .Distinct()
            .Select(speed => new LabelData(ContentRegistry.Fontoe, $"Speed {speed:0.0}", 18))
            .ToList();
    }

    private static bool TryParseMovingBlockSpeed(string text, out float speed)
    {
        string value = text.Replace("Speed", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return float.TryParse(value, out speed);
    }

    private static List<LabelData> GetDepthOptions(float selectedDepth)
    {
        float[] values = [selectedDepth, 0.1F, 0.2F, 0.3F, 0.4F, 0.5F, 0.6F, 0.7F, 0.8F, 0.9F];
        return values
            .Distinct()
            .Select(depth => new LabelData(ContentRegistry.Fontoe, $"Depth {depth:0.0}", 18))
            .ToList();
    }

    private static bool TryParseDepth(string text, out float depth)
    {
        string value = text.Replace("Depth", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return float.TryParse(value, out depth);
    }

    private static List<LabelData> GetNextLevelOptions(string selectedLevelName)
    {
        List<string> options = [];

        if (!string.IsNullOrWhiteSpace(selectedLevelName))
        {
            options.Add(selectedLevelName.Trim());
        }

        foreach (string levelName in CustomLevelScene.GetLevelTransitionOptions())
        {
            if (!options.Contains(levelName, StringComparer.OrdinalIgnoreCase))
            {
                options.Add(levelName);
            }
        }

        if (options.Count == 0)
        {
            options.Add("Level 1");
        }

        return options
            .Select(levelName => new LabelData(ContentRegistry.Fontoe, levelName, 18))
            .ToList();
    }

    private static void RebuildDropDownOptions(TextureDropDownElement dropDown, List<LabelData> options)
    {
        dropDown.Options.Clear();
        foreach (LabelData option in options)
        {
            dropDown.Options.Add(option);
        }
    }

    private static bool IsPointInsideDropDown(TextureDropDownElement dropDown, Vector2 point)
    {
        RectangleF fieldRect = new RectangleF(
            dropDown.Position.X,
            dropDown.Position.Y,
            dropDown.ScaledSize.X,
            dropDown.ScaledSize.Y);

        if (Contains(fieldRect, point))
        {
            return true;
        }

        if (!dropDown.IsMenuOpen)
        {
            return false;
        }

        float menuHeight = dropDown.ScaledSize.Y * Math.Min(dropDown.Options.Count, dropDown.MaxVisibleOptions);
        RectangleF menuRect = new RectangleF(
            dropDown.Position.X,
            dropDown.Position.Y + dropDown.ScaledSize.Y,
            dropDown.ScaledSize.X,
            menuHeight);

        return Contains(menuRect, point);
    }

    private static bool Contains(RectangleF rectangle, Vector2 point)
    {
        return point.X >= rectangle.X
               && point.X <= rectangle.X + rectangle.Width
               && point.Y >= rectangle.Y
               && point.Y <= rectangle.Y + rectangle.Height;
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Data.Text = message;
        }
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
