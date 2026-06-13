using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Registries;
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
    private bool _isEscapeMenuOpen;
    private bool _isUnsavedExitDialogOpen;

    public bool IsSaveDialogOpen => _saveNameTextBox?.Enabled ?? false;
    public bool IsAnyModalOpen => IsSaveDialogOpen || _isEscapeMenuOpen || _isUnsavedExitDialogOpen;

    public LevelEditorGui(CustomLevelScene scene) : base("LevelEditorOverlay", null)
    {
        _scene = scene;
    }

    protected override void Init()
    {
        base.Init();

        TextureButtonData buttonData = new TextureButtonData(
            AtlasRegistry.UiAtlasTexture,
            sourceRect: AtlasRegistry.ButtonRegion,
            hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice,
            borderInsets: new BorderInsets(12));

        TextureTextBoxData textBoxData = new TextureTextBoxData(
            AtlasRegistry.UiAtlasTexture,
            sourceRect: AtlasRegistry.ButtonRegion,            
            hoverColor: Color.LightGray,
            resizeMode: ResizeMode.NineSlice,
            borderInsets: new BorderInsets(12));

        TextureDropDownData dropDownData = new TextureDropDownData(
            AtlasRegistry.UiAtlasTexture,
            AtlasRegistry.UiAtlasTexture,
            AtlasRegistry.UiAtlasTexture,
            AtlasRegistry.UiAtlasTexture,
            AtlasRegistry.UiAtlasTexture,
            fieldSourceRect: AtlasRegistry.ButtonRegion,
            menuSourceRect: AtlasRegistry.MenuRegion,
            sliderBarSourceRect: new Rectangle(AtlasRegistry.ButtonRegion.X + 2, AtlasRegistry.ButtonRegion.Y, AtlasRegistry.ButtonRegion.Width - 2, AtlasRegistry.ButtonRegion.Height),
            sliderSourceRect: AtlasRegistry.SliderRegion,
            arrowSourceRect: AtlasRegistry.ArrowRegion,
            fieldResizeMode: ResizeMode.NineSlice,
            menuResizeMode: ResizeMode.NineSlice,
            sliderBarResizeMode: ResizeMode.NineSlice,
            fieldBorderInsets: new BorderInsets(12),
            menuBorderInsets: new BorderInsets(5),
            sliderBarBorderInsets: new BorderInsets(5));

        this.AddElement("Save-Button", new TextureButtonElement(
            buttonData,
            GuiText.ButtonLabel(Localization.T("common.save"), 150),
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
            GuiText.ButtonLabel(Localization.T("common.play"), 150),
            Anchor.TopLeft,
            new Vector2(340, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.StartEditorPlayMode();
                SetStatus(Localization.T("gui.level_editor.player_spawned"));
                return true;
            }));

        List<LabelData> placeableOptions = CustomLevelScene.GetAvailablePlaceables()
            .Select(placeable => new LabelData(FontRegistry.Fontoe, CustomLevelScene.GetPlaceableDisplayName(placeable), 18))
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
            menuContentInsets: (5, 5, 3, 3));

        _placeableDropDown.MenuToggled += isMenuOpen =>
        {
            _placeableDropDown.DropDownData.MenuSourceRect = isMenuOpen && _placeableDropDown.Options.Count > _placeableDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width - 2, (int)AtlasRegistry.MenuRegion.Height)
                : new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width, (int)AtlasRegistry.MenuRegion.Height);
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
            menuContentInsets: (5, 5, 3, 3));
        _movingBlockSpeedDropDown.MenuToggled += isMenuOpen =>
        {
            _movingBlockSpeedDropDown.DropDownData.MenuSourceRect = isMenuOpen && _movingBlockSpeedDropDown.Options.Count > _movingBlockSpeedDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width - 2, (int)AtlasRegistry.MenuRegion.Height)
                : new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width, (int)AtlasRegistry.MenuRegion.Height);
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
            menuContentInsets: (5, 5, 3, 3));
        _placeableDepthDropDown.MenuToggled += isMenuOpen =>
        {
            _placeableDepthDropDown.DropDownData.MenuSourceRect = isMenuOpen && _placeableDepthDropDown.Options.Count > _placeableDepthDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width - 2, (int)AtlasRegistry.MenuRegion.Height)
                : new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width, (int)AtlasRegistry.MenuRegion.Height);
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
            GuiText.ButtonLabel(Localization.T("common.menu"), 150),
            Anchor.TopLeft,
            new Vector2(180, 20),
            size: new Vector2(150, 40),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                RequestBackToBrowser();
                return true;
            }));

        this.AddElement("Place-Button", new TextureButtonElement(
            buttonData,
            GuiText.ButtonLabel("", 130),
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
            GuiText.ButtonLabel("", 130),
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

        ToggleData mouseOnlyToggleData = new(
            AtlasRegistry.UiAtlasTexture,
            AtlasRegistry.UiAtlasTexture,
            checkboxSourceRect: AtlasRegistry.CheckBoxRegion,
            checkmarkSourceRect: AtlasRegistry.CheckMarkRegion,
            checkboxHoverColor: Color.LightGray,
            checkmarkHoverColor: Color.LightGray);
        LabelData mouseOnlyToggleLabelData = new(
            FontRegistry.Fontoe,
            Localization.T("gui.level_editor.mouse_only_tools"),
            18,
            color: Color.White);

        this.AddElement("Mouse-Only-Tools-Toggle", new ToggleElement(
            mouseOnlyToggleData,
            mouseOnlyToggleLabelData,
            Anchor.BottomRight,
            new Vector2(-5, -5),
            5,
            toggleState: _scene.UseMouseOnlyTools,
            clickFunc: _ =>
            {
                _scene.SuppressPlacementUntilMouseRelease();
                _scene.SetUseMouseOnlyTools(!_scene.UseMouseOnlyTools);
                return true;
            }));

        this.AddElement("Hotbar-Label", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.hotbar"), 18, color: Color.White),
            Anchor.BottomCenter,
            new Vector2(0, -92),
            new Vector2(1.5F, 1.5F)));

        _statusLabel = new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.camera_hint"), 18, color: Color.White),
            Anchor.BottomLeft,
            new Vector2(20, -24));
        this.AddElement("Status-Label", _statusLabel);
        
        this.AddElement("Save-Confirm-Button", CreateModalButton(buttonData, Localization.T("common.save"), new Vector2(-85, 100), () =>
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
                SetStatus(Localization.F("gui.level_editor.saved", _saveNameTextBox.LabelData.Text.Trim()));
                SetSaveDialogVisible(false);
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
            }
        }));

        this.AddElement("Save-Cancel-Button", CreateModalButton(buttonData, Localization.T("common.cancel"), new Vector2(85, 100), () =>
        {
            SetSaveDialogVisible(false);
        }));


        _saveNameTextBox = new TextureTextBoxElement(
            textBoxData,
            new LabelData(FontRegistry.Fontoe, _scene.LevelName, 18, hoverColor: Color.White),
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.level_name_hint"), 18, color: Color.Gray),
            Anchor.Center,
            new Vector2(0, -25),
            30,
            TextAlignment.Center,
            new Vector2(0, 1),
            Vector2.One,
            1,
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
            menuContentInsets: (5, 5, 3, 3));
        _nextLevelDropDown.Enabled = false;
        _nextLevelDropDown.Interactable = false;
        _nextLevelDropDown.MenuToggled += isMenuOpen =>
        {
            _nextLevelDropDown.DropDownData.MenuSourceRect = isMenuOpen && _nextLevelDropDown.Options.Count > _nextLevelDropDown.MaxVisibleOptions
                ? new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width - 2, (int)AtlasRegistry.MenuRegion.Height)
                : new Rectangle(0, 0, (int)AtlasRegistry.MenuRegion.Width, (int)AtlasRegistry.MenuRegion.Height);
        };
        this.AddElement("Next-Level-Drop-Down", _nextLevelDropDown);
        
        this.AddElement("Save-Dialog-Title", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.save_level"), 18, color: Color.White),
            Anchor.Center,
            new Vector2(0, -88),
            new Vector2(2, 2)));

        this.AddElement("Save-Dialog-Hint", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.save_level_hint"), 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, -60)));

        LabelElement nextLevelLabel = new(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.next_level"), 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, 12));
        nextLevelLabel.Enabled = false;
        nextLevelLabel.Interactable = false;
        this.AddElement("Next-Level-Label", nextLevelLabel);

        this.AddElement("Escape-Menu-Title", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.escape_menu_title"), 18, color: Color.White),
            Anchor.Center,
            new Vector2(0, -78),
            new Vector2(2, 2)));

        this.AddElement("Escape-Menu-Hint", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.escape_menu_hint"), 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, -44)));

        this.AddElement("Escape-Menu-Button", CreateModalButton(buttonData, Localization.T("common.menu"), new Vector2(-170, 40), RequestBackToBrowser));
        this.AddElement("Escape-Options-Button", CreateModalButton(buttonData, Localization.T("common.options"), new Vector2(0, 40), () =>
        {
            SetEscapeMenuVisible(false);
            _scene.OpenOptions();
        }));
        this.AddElement("Escape-Cancel-Button", CreateModalButton(buttonData, Localization.T("common.cancel"), new Vector2(170, 40), () =>
        {
            SetEscapeMenuVisible(false);
        }));

        this.AddElement("Unsaved-Exit-Title", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.unsaved_exit_title"), 18, color: Color.White),
            Anchor.Center,
            new Vector2(0, -84),
            new Vector2(2, 2)));

        this.AddElement("Unsaved-Exit-Hint", new LabelElement(
            new LabelData(FontRegistry.Fontoe, Localization.T("gui.level_editor.unsaved_exit_hint"), 18, color: Color.LightGray),
            Anchor.Center,
            new Vector2(0, -46)));

        this.AddElement("Unsaved-Save-Menu-Button", CreateModalButton(buttonData, Localization.T("gui.level_editor.save_and_menu"), new Vector2(-180, 54), SaveAndBackToBrowser));
        this.AddElement("Unsaved-Discard-Menu-Button", CreateModalButton(buttonData, Localization.T("gui.level_editor.discard_and_menu"), new Vector2(0, 54), () =>
        {
            _scene.BackToBrowser();
        }));
        this.AddElement("Unsaved-Cancel-Button", CreateModalButton(buttonData, Localization.T("common.cancel"), new Vector2(180, 54), () =>
        {
            SetUnsavedExitDialogVisible(false);
        }));

        SetSaveDialogVisible(false);
        SetEscapeMenuVisible(false);
        SetUnsavedExitDialogVisible(false);
        RefreshEditorState();
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            if (IsSaveDialogOpen)
            {
                SetSaveDialogVisible(false);
                return;
            }

            if (_isUnsavedExitDialogOpen)
            {
                SetUnsavedExitDialogVisible(false);
                return;
            }

            if (_isEscapeMenuOpen)
            {
                SetEscapeMenuVisible(false);
                return;
            }

            _scene.HandleEditorEscape();
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
                    SetStatus(Localization.F("gui.level_editor.saved", _saveNameTextBox.LabelData.Text.Trim()));
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

        if (IsAnyModalOpen)
        {
            Vector2 modalSize = (_isEscapeMenuOpen || _isUnsavedExitDialogOpen
                ? new Vector2(560, 250)
                : new Vector2(420, 280)) * scale;
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
        SetButtonLabel("Place-Button", _scene.SelectedTool == EditorTool.Place ? $"[{Localization.T("gui.level_editor.tool.place")}]" : Localization.T("gui.level_editor.tool.place"));
        SetButtonLabel("Eraser-Button", _scene.SelectedTool == EditorTool.Eraser ? $"[{Localization.T("gui.level_editor.tool.eraser")}]" : Localization.T("gui.level_editor.tool.eraser"));
        ToggleElement("Place-Button", !_scene.UseMouseOnlyTools);
        ToggleElement("Eraser-Button", !_scene.UseMouseOnlyTools);
        ToggleElement("Moving-Block-Speed-Drop-Down", _scene.SelectedPlaceable == PlaceableType.MovingBlock);
        ToggleElement("Placeable-Depth-Drop-Down", true);
        SetStatus(_scene.UseMouseOnlyTools
            ? $"{_scene.GetEditorStatusMessage()} | {Localization.T("gui.level_editor.mouse_only_hint")}"
            : _scene.GetEditorStatusMessage());
    }

    public void OpenEscapeMenu()
    {
        _scene.SuppressPlacementUntilMouseRelease();
        SetSaveDialogVisible(false);
        SetUnsavedExitDialogVisible(false);
        SetEscapeMenuVisible(true);
    }

    private TextureButtonElement CreateModalButton(TextureButtonData buttonData, string text, Vector2 offset, Action onClick)
    {
        TextureButtonElement button = new(
            buttonData,
            GuiText.ButtonLabel(text, 150),
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
        if (visible)
        {
            SetEscapeMenuVisible(false);
            SetUnsavedExitDialogVisible(false);
        }

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

    private void SetEscapeMenuVisible(bool visible)
    {
        _isEscapeMenuOpen = visible;
        ToggleElement("Escape-Menu-Title", visible);
        ToggleElement("Escape-Menu-Hint", visible);
        ToggleElement("Escape-Menu-Button", visible);
        ToggleElement("Escape-Options-Button", visible);
        ToggleElement("Escape-Cancel-Button", visible);
    }

    private void SetUnsavedExitDialogVisible(bool visible)
    {
        _isUnsavedExitDialogOpen = visible;
        ToggleElement("Unsaved-Exit-Title", visible);
        ToggleElement("Unsaved-Exit-Hint", visible);
        ToggleElement("Unsaved-Save-Menu-Button", visible);
        ToggleElement("Unsaved-Discard-Menu-Button", visible);
        ToggleElement("Unsaved-Cancel-Button", visible);
    }

    private void RequestBackToBrowser()
    {
        _scene.SuppressPlacementUntilMouseRelease();
        SetEscapeMenuVisible(false);

        if (_scene.HasUnsavedChanges)
        {
            SetSaveDialogVisible(false);
            SetUnsavedExitDialogVisible(true);
            return;
        }

        _scene.BackToBrowser();
    }

    private void SaveAndBackToBrowser()
    {
        try
        {
            _scene.SaveLevel(_scene.LevelName);
            _scene.BackToBrowser();
        }
        catch (Exception exception)
        {
            SetUnsavedExitDialogVisible(false);
            SetStatus(exception.Message);
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
            float buttonWidth = name is "Place-Button" or "Eraser-Button" ? 130F : 150F;
            button.LabelData.Size = GuiText.ButtonLabel(text, buttonWidth).Size;
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
            .Select(speed => new LabelData(FontRegistry.Fontoe, $"{Localization.T("gui.level_editor.speed")} {speed:0.0}", 18))
            .ToList();
    }

    private static bool TryParseMovingBlockSpeed(string text, out float speed)
    {
        string value = text
            .Replace("Speed", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(Localization.T("gui.level_editor.speed"), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return float.TryParse(value, out speed);
    }

    private static List<LabelData> GetDepthOptions(float selectedDepth)
    {
        float[] values = [selectedDepth, 0.1F, 0.2F, 0.3F, 0.4F, 0.5F, 0.6F, 0.7F, 0.8F, 0.9F];
        return values
            .Distinct()
            .Select(depth => new LabelData(FontRegistry.Fontoe, $"{Localization.T("gui.level_editor.depth")} {depth:0.0}", 18))
            .ToList();
    }

    private static bool TryParseDepth(string text, out float depth)
    {
        string value = text
            .Replace("Depth", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(Localization.T("gui.level_editor.depth"), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
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
            .Select(levelName => new LabelData(FontRegistry.Fontoe, levelName, 18))
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
