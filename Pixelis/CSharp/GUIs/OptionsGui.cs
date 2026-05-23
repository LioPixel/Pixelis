using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using MiniAudioEx.Core.StandardAPI;
using Sparkle.CSharp;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Overlays;
using Sparkle.CSharp.Scenes;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class OptionsGui : Gui
{
    private static readonly Vector2 BaseWindowSize = new Vector2(550, 310);
    private int _guiScaleMarkerMax;
    
    public OptionsGui() : base("Options")
    {
    }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, "Options", 18);
        this.AddElement("Title", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

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
        
        // Toggle Vsync.
        ToggleData toggleDataVsync = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
        LabelData toggleLabelDataVsync = new LabelData(ContentRegistry.Fontoe, "V-Sync", 18);
        
        this.AddElement("Toggle-Vsync", new ToggleElement(toggleDataVsync, toggleLabelDataVsync, Anchor.Center, new Vector2(-5, -120), 5, toggleState: GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank, clickFunc: (element) => {
            GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank = !GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank;
            ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("Vsync", GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank);
            return true;
        }));
        
        // Toggle Debug mode.
        ToggleData debugModeToggleData = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
        LabelData debugModeToggleLabelData = new LabelData(ContentRegistry.Fontoe, "Debug Mode", 18);
        
        this.AddElement("Toggle-DebugMode", new ToggleElement(debugModeToggleData, debugModeToggleLabelData, Anchor.Center, new Vector2(19, -70), 5, toggleState: ((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode"), clickFunc: (element) =>
        {
            bool condition = !((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode");
            //OverlayManager.GetOverlays().First(overlay => overlay.Name == "Debug").Enabled = condition;
            ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("DebugMode", condition);
            return true;
        }));
        
        // Toggle Sound.
        ToggleData toggleDataSound = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
        LabelData toggleLabelDataSound = new LabelData(ContentRegistry.Fontoe, "Sounds", 18);
        
        this.AddElement("Toggle-Sounds", new ToggleElement(toggleDataSound, toggleLabelDataSound, Anchor.Center, new Vector2(0, -20), 5, toggleState: ((PixelisGame) Game.Instance!).OptionsConfig.GetValue<bool>("Sounds"), clickFunc: (element) => {
            ((PixelisGame) Game.Instance).OptionsConfig.SetValue("Sounds", !((PixelisGame) Game.Instance!).OptionsConfig.GetValue<bool>("Sounds"));
            return true;
        }));
        
        LabelData masterVolumeLabelData = new LabelData(ContentRegistry.Fontoe, "Master Volume", 18);
        this.AddElement("Master-Volume", new LabelElement(masterVolumeLabelData, Anchor.Center, new Vector2(0, 100)));

        LabelData guiScaleLabelData = new LabelData(ContentRegistry.Fontoe, "GUI Scale", 18);
        this.AddElement("Gui-Scale", new LabelElement(guiScaleLabelData, Anchor.Center, new Vector2(0, 20)));
        
        // Texture slider bar.
        TextureSlideBarData textureSlideBarData = new TextureSlideBarData(
            ContentRegistry.UiBar,
            null,
            ContentRegistry.UiSliderLowRes,
            barResizeMode: ResizeMode.NineSlice,
            filledBarResizeMode: ResizeMode.NineSlice,
            barBorderInsets: new BorderInsets(3),
            filledBarBorderInsets: new BorderInsets(3));
        
        this.AddElement("Texture-Slider-Bar", new TextureSlideBarElement(textureSlideBarData, Anchor.Center, new Vector2(0, 130), 0, 1, value: ((PixelisGame) Game.Instance!).OptionsConfig.GetValue<float>("MasterVolume"), wholeNumbers: false, size: new Vector2(140, 8), scale: new Vector2(2, 2), clickFunc: (element) => {
            return true;
        }));

        LabelData keyBingsLabelData = new LabelData(ContentRegistry.Fontoe, "Keybinds", 18, hoverColor: Color.White);
        this.AddElement("Key-Binds-Button", new TextureButtonElement(backButtonData, keyBingsLabelData, Anchor.Center, new Vector2(180, -120), size: new Vector2(130, 40), textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            GuiManager.SetGui(new KeyBindsGui());
            return true;
        }));

        float maxGuiScale = this.GetMaxGuiScale();
        float guiScale = Math.Clamp(MathF.Round(((PixelisGame) Game.Instance!).OptionsConfig.GetValue<float>("GuiScale")), 1.0F, maxGuiScale);
        ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", guiScale);
        GuiManager.Scale = guiScale;

        this.AddElement("Gui-Scale-Slider-Bar", new TextureSlideBarElement(textureSlideBarData, Anchor.Center, new Vector2(0, 50), 1.0f, maxGuiScale, value: guiScale, wholeNumbers: true, size: new Vector2(140, 8), scale: new Vector2(2, 2), clickFunc: (element) => {
            if (element is TextureSlideBarElement slideBarElement)
            {
                float roundedScale = Math.Clamp(MathF.Round(slideBarElement.Value), 1.0F, slideBarElement.MaxValue);
                slideBarElement.Value = roundedScale;
                GuiManager.Scale = roundedScale;
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", roundedScale);
            }
            return true;
        }));

        this.RebuildGuiScaleMarkers((int)maxGuiScale);
    }
    protected override void Update(double delta)
    {
        base.Update(delta);
        this.SyncGuiScaleSliderToWindowSize();

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
        
        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, BaseWindowSize);
        
        
        
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (this.TryGetElement("Texture-Slider-Bar", out GuiElement? element))
            {
                TextureSlideBarElement slideBarElement = (TextureSlideBarElement) element!;
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("MasterVolume", slideBarElement.Value);
                AudioContext.MasterVolume = slideBarElement.Value;
            }

            if (this.TryGetElement("Gui-Scale-Slider-Bar", out GuiElement? guiScaleElement))
            {
                TextureSlideBarElement guiScaleSlideBarElement = (TextureSlideBarElement) guiScaleElement!;
                float roundedScale = MathF.Round(guiScaleSlideBarElement.Value);
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", roundedScale);
                GuiManager.Scale = roundedScale;
            }
        }
    }

    private float GetMaxGuiScale()
    {
        IWindow window = GlobalGraphicsAssets.Window;
        float widthScale = MathF.Floor(window.GetWidth() / BaseWindowSize.X);
        float heightScale = MathF.Floor(window.GetHeight() / BaseWindowSize.Y);
        return MathF.Max(1.0F, MathF.Min(widthScale, heightScale));
    }

    private void SyncGuiScaleSliderToWindowSize()
    {
        if (!this.TryGetElement("Gui-Scale-Slider-Bar", out GuiElement? element) || element is not TextureSlideBarElement slider)
        {
            return;
        }

        float maxGuiScale = this.GetMaxGuiScale();
        int maxGuiScaleInt = (int)maxGuiScale;
        slider.MaxValue = maxGuiScale;
        this.RebuildGuiScaleMarkers(maxGuiScaleInt);

        float clampedScale = Math.Clamp(MathF.Round(slider.Value), 1.0F, maxGuiScale);

        if (MathF.Abs(slider.Value - clampedScale) > 0.001f)
        {
            slider.Value = clampedScale;
        }

        if (MathF.Abs(GuiManager.Scale - clampedScale) > 0.001f)
        {
            GuiManager.Scale = clampedScale;
            ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", clampedScale);
        }
    }

    private void RebuildGuiScaleMarkers(int maxGuiScale)
    {
        maxGuiScale = Math.Max(1, maxGuiScale);
        if (this._guiScaleMarkerMax == maxGuiScale)
        {
            return;
        }

        for (int i = 1; i <= this._guiScaleMarkerMax; i++)
        {
            this.RemoveElement($"Gui-Scale-Marker-{i}");
        }

        this._guiScaleMarkerMax = maxGuiScale;

        if (maxGuiScale == 1)
        {
            LabelData singleLabelData = new LabelData(ContentRegistry.Fontoe, "x1", 18);
            this.AddElement("Gui-Scale-Marker-1", new LabelElement(singleLabelData, Anchor.Center, new Vector2(0, 76), new Vector2(0.75f, 0.75f)));
            return;
        }

        const float sliderWidth = 140.0f;
        const float leftX = -sliderWidth / 2.0f;
        float step = sliderWidth / (maxGuiScale - 1);

        for (int i = 1; i <= maxGuiScale; i++)
        {
            float x = leftX + (i - 1) * step;
            LabelData markerLabelData = new LabelData(ContentRegistry.Fontoe, $"x{i}", 18);
            this.AddElement($"Gui-Scale-Marker-{i}", new LabelElement(markerLabelData, Anchor.Center, new Vector2(x, 76), new Vector2(0.75f, 0.75f)));
        }
    }
}
