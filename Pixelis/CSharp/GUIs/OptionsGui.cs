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
    private int _guiScaleMarkerMax = -1;
    
    public OptionsGui() : base("Options", (550, 310))
    {
    }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, Localization.T("common.options"), 18);
        this.AddElement("Title", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

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
        
        // Toggle Vsync.
        ToggleData toggleDataVsync = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
        LabelData toggleLabelDataVsync = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.vsync"), 18);
        
        this.AddElement("Toggle-Vsync", new ToggleElement(toggleDataVsync, toggleLabelDataVsync, Anchor.Center, new Vector2(-14, -120), 5, toggleState: GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank, clickFunc: (element) => {
            GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank = !GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank;
            ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("Vsync", GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank);
            return true;
        }));

        if (Localization.CurrentLanguage == Localization.German)
        {
            // Toggle Debug mode.
            ToggleData debugModeToggleData = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
            LabelData debugModeToggleLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.debug_mode"), 18);

            this.AddElement("Toggle-DebugMode", new ToggleElement(debugModeToggleData, debugModeToggleLabelData, Anchor.Center, new Vector2(14, -70), 5, toggleState: ((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode"), clickFunc: (element) =>
            {
                bool condition = !((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode");
                //OverlayManager.GetOverlays().First(overlay => overlay.Name == "Debug").Enabled = condition;
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("DebugMode", condition);
                return true;
            }));
        }
        else if  (Localization.CurrentLanguage == Localization.English)
        {
            // Toggle Debug mode.
            ToggleData debugModeToggleData = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
            LabelData debugModeToggleLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.debug_mode"), 18);
            this.AddElement("Toggle-DebugMode", new ToggleElement(debugModeToggleData, debugModeToggleLabelData, Anchor.Center, new Vector2(10, -70), 5, toggleState: ((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode"), clickFunc: (element) =>
            {
                bool condition = !((PixelisGame) PixelisGame.Instance).OptionsConfig.GetValue<bool>("DebugMode");
                //OverlayManager.GetOverlays().First(overlay => overlay.Name == "Debug").Enabled = condition;
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("DebugMode", condition);
                return true;
            }));
        }
        else
        {
            return;
        }
        
        // Toggle Sound.
        ToggleData toggleDataSound = new ToggleData(ContentRegistry.ToggleBackground, ContentRegistry.ToggleCheckmark, checkboxHoverColor: Color.LightGray, checkmarkHoverColor: Color.LightGray);
        LabelData toggleLabelDataSound = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.sounds"), 18);
        
        this.AddElement("Toggle-Sounds", new ToggleElement(toggleDataSound, toggleLabelDataSound, Anchor.Center, new Vector2(-10, -20), 5, toggleState: ((PixelisGame) Game.Instance!).OptionsConfig.GetValue<bool>("Sounds"), clickFunc: (element) => {
            ((PixelisGame) Game.Instance).OptionsConfig.SetValue("Sounds", !((PixelisGame) Game.Instance!).OptionsConfig.GetValue<bool>("Sounds"));
            return true;
        }));
        
        LabelData masterVolumeLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.master_volume"), 18);
        this.AddElement("Master-Volume", new LabelElement(masterVolumeLabelData, Anchor.Center, new Vector2(0, 100)));

        LabelData guiScaleLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.options.gui_scale"), 18);
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

        string extendedOptionsText = Localization.T("gui.options.extended");
        Vector2 extendedOptionsButtonSize = GuiText.ButtonSize(extendedOptionsText, 150, maxWidth: 230);
        LabelData extendedOptionsLabelData = GuiText.ButtonLabel(extendedOptionsText, extendedOptionsButtonSize.X);
        this.AddElement("Extended-Options-Button", new TextureButtonElement(backButtonData, extendedOptionsLabelData, Anchor.Center, new Vector2(160, -120), size: extendedOptionsButtonSize, textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            GuiManager.SetGui(new ExtandedOptionsGui());
            return true;
        }));

        int maxGuiScale = GuiManager.MaxAllowedScaleFactor;
        int guiScale = GetConfiguredGuiScaleStep();
        ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", guiScale);
        GuiManager.SetScale(guiScale);

        this.AddElement("Gui-Scale-Slider-Bar", new TextureSlideBarElement(textureSlideBarData, Anchor.Center, new Vector2(0, 50), 0.0f, maxGuiScale, value: guiScale, wholeNumbers: true, size: new Vector2(140, 8), scale: new Vector2(2, 2), clickFunc: (element) => {
            if (element is TextureSlideBarElement slideBarElement)
            {
                int scaleStep = ClampGuiScaleStep((int)MathF.Round(slideBarElement.Value));
                slideBarElement.Value = scaleStep;
                GuiManager.SetScale(scaleStep);
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", scaleStep);
            }
            return true;
        }));

        this.RebuildGuiScaleMarkers(maxGuiScale);
    }
    protected override void Update(double delta)
    {
        base.Update(delta);
        this.SyncGuiScaleSliderToScaleSteps();

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
                int scaleStep = ClampGuiScaleStep((int)MathF.Round(guiScaleSlideBarElement.Value));
                ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", scaleStep);
                GuiManager.SetScale(scaleStep);
            }
        }
    }

    private static int GetConfiguredGuiScaleStep()
    {
        float configuredScale = ((PixelisGame)Game.Instance!).OptionsConfig.GetValue<float>("GuiScale");
        return ClampGuiScaleStep((int)MathF.Round(configuredScale));
    }

    private static int ClampGuiScaleStep(int scaleStep)
    {
        return Math.Clamp(scaleStep, 0, GuiManager.MaxAllowedScaleFactor);
    }

    private void SyncGuiScaleSliderToScaleSteps()
    {
        if (!this.TryGetElement("Gui-Scale-Slider-Bar", out GuiElement? element) || element is not TextureSlideBarElement slider)
        {
            return;
        }

        int maxGuiScale = GuiManager.MaxAllowedScaleFactor;
        slider.MaxValue = maxGuiScale;
        this.RebuildGuiScaleMarkers(maxGuiScale);

        int clampedScale = ClampGuiScaleStep((int)MathF.Round(slider.Value));

        if (MathF.Abs(slider.Value - clampedScale) > 0.001f)
        {
            slider.Value = clampedScale;
        }

        if (GuiManager.Scale != clampedScale)
        {
            GuiManager.SetScale(clampedScale);
            ((PixelisGame) Game.Instance!).OptionsConfig.SetValue("GuiScale", clampedScale);
        }
    }

    private void RebuildGuiScaleMarkers(int maxGuiScale)
    {
        maxGuiScale = Math.Max(0, maxGuiScale);
        if (this._guiScaleMarkerMax == maxGuiScale)
        {
            return;
        }

        for (int i = 0; i <= this._guiScaleMarkerMax; i++)
        {
            this.RemoveElement($"Gui-Scale-Marker-{i}");
        }

        this._guiScaleMarkerMax = maxGuiScale;

        if (maxGuiScale == 0)
        {
            LabelData singleLabelData = new LabelData(ContentRegistry.Fontoe, "Auto", 18);
            this.AddElement("Gui-Scale-Marker-0", new LabelElement(singleLabelData, Anchor.Center, new Vector2(0, 76), new Vector2(0.75f, 0.75f)));
            return;
        }

        const float sliderWidth = 140.0f;
        const float leftX = -sliderWidth / 2.0f;
        float step = sliderWidth / maxGuiScale;

        for (int i = 0; i <= maxGuiScale; i++)
        {
            float x = leftX + i * step;
            string markerText = i == 0 ? "Auto" : i.ToString();
            LabelData markerLabelData = new LabelData(ContentRegistry.Fontoe, markerText, 18);
            this.AddElement($"Gui-Scale-Marker-{i}", new LabelElement(markerLabelData, Anchor.Center, new Vector2(x, 76), new Vector2(0.75f, 0.75f)));
        }
    }
}
