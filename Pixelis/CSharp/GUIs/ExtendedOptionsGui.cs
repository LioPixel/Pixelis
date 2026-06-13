using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Pixelis.CSharp.Registries;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class ExtandedOptionsGui : Gui
{
    private static readonly Vector2 BaseWindowSize = new Vector2(550, 310);
    
    public ExtandedOptionsGui() : base("Options")
    {
    }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(FontRegistry.Fontoe, Localization.T("common.extended.options"), 18);
        this.AddElement("Title", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));
        
        TextureButtonData backButtonData = new TextureButtonData(AtlasRegistry.UiAtlasTexture, sourceRect: AtlasRegistry.ButtonRegion, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        string backText = Localization.T("common.back");
        Vector2 backButtonSize = GuiText.ButtonSize(backText, 110);
        LabelData backButtonLabelData = GuiText.ButtonLabel(backText, backButtonSize.X);
        
        this.AddElement("Options-Button", new TextureButtonElement(backButtonData, backButtonLabelData, Anchor.Center, new Vector2(-200, -120), size: backButtonSize, textOffset: new Vector2(0, 1), clickFunc: (element) => {
            GuiManager.SetGui(new OptionsGui());
            return true;
        }));
        
        string keybindsText = Localization.T("gui.options.keybinds");
        Vector2 keybindsButtonSize = GuiText.ButtonSize(keybindsText, 150, maxWidth: 210);
        LabelData keyBingsLabelData = GuiText.ButtonLabel(keybindsText, keybindsButtonSize.X);
        this.AddElement("Key-Binds-Button", new TextureButtonElement(backButtonData, keyBingsLabelData, Anchor.Center, new Vector2(-100, 0), size: keybindsButtonSize, textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            GuiManager.SetGui(new KeyBindsGui());
            return true;
        }));

        TextureDropDownData languageDropDownData = new TextureDropDownData(
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

        List<LabelData> languageOptions = GetLanguageOptions();
        TextureDropDownElement languageDropDownElement = new TextureDropDownElement(
            languageDropDownData,
            languageOptions,
            2,
            Anchor.Center,
            new Vector2(120, 0),
            size: new Vector2(180, 40),
            scale: Vector2.One,
            fieldTextOffset: new Vector2(10, 1),
            menuTextOffset: new Vector2(10, 1),
            sliderOffset: new Vector2(-1F, 0),
            menuContentInsets: (5, 5, 3, 3));

        languageDropDownElement.MenuToggled += isMenuOpen =>
        {
            languageDropDownElement.DropDownData.MenuSourceRect = isMenuOpen && languageDropDownElement.Options.Count > languageDropDownElement.MaxVisibleOptions
                ? new Rectangle(AtlasRegistry.ButtonRegion.X, AtlasRegistry.ButtonRegion.Y, (int)AtlasRegistry.MenuRegion.Width - 2, (int)AtlasRegistry.MenuRegion.Height)
                : new Rectangle(AtlasRegistry.ButtonRegion.X, AtlasRegistry.ButtonRegion.Y, (int)AtlasRegistry.MenuRegion.Width, (int)AtlasRegistry.MenuRegion.Height);
        };

        languageDropDownElement.OptionChanged += option =>
        {
            string selectedLanguage = option.Text == Localization.T("language.german")
                ? Localization.German
                : Localization.English;

            if (selectedLanguage != Localization.CurrentLanguage)
            {
                Localization.SetLanguage(selectedLanguage);
                GuiManager.SetGui(new ExtandedOptionsGui());
            }
        };

        this.AddElement("Language-Drop-Down", languageDropDownElement);
    }
    protected override void Update(double delta)
    {
        base.Update(delta);
        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {

            GuiManager.SetGui(new OptionsGui());
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        if (SceneManager.ActiveScene == null)
        {
            IWindow window = GlobalGraphicsAssets.Window;
        
            // Background
            Texture2D backgroundTexture = TextureRegistry.Background2;
            Vector2 backgroundSize = new Vector2((float) window.GetWidth() / backgroundTexture.Width, (float) window.GetHeight() / backgroundTexture.Height);
        
            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
            context.SpriteBatch.End();
        }
        
        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, BaseWindowSize);
        
        
        
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing) {}

    private float GetMaxGuiScale()
    {
        IWindow window = GlobalGraphicsAssets.Window;
        float widthScale = MathF.Floor(window.GetWidth() / BaseWindowSize.X);
        float heightScale = MathF.Floor(window.GetHeight() / BaseWindowSize.Y);
        return MathF.Max(1.0F, MathF.Min(widthScale, heightScale));
    }

    private static List<LabelData> GetLanguageOptions()
    {
        LabelData english = new LabelData(FontRegistry.Fontoe, Localization.T("language.english"), 18);
        LabelData german = new LabelData(FontRegistry.Fontoe, Localization.T("language.german"), 18);

        return Localization.CurrentLanguage == Localization.German
            ? [german, english]
            : [english, german];
    }
}
