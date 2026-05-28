using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Controls;
using Sparkle.CSharp;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class KeyBindsGui : Gui
{
    private BindingAction? _captureFor;

    private enum BindingAction
    {
        MoveLeft,
        MoveRight,
        Jump
    }

    public KeyBindsGui() : base("Key Binds")
    {
    }

    protected override void Init()
    {
        base.Init();

        LabelData titleData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.keybinds.title"), 18);
        this.AddElement("Title", new LabelElement(titleData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

        TextureButtonData buttonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        string backText = Localization.T("common.back");
        Vector2 backButtonSize = GuiText.ButtonSize(backText, 110);
        LabelData backLabelData = GuiText.ButtonLabel(backText, backButtonSize.X);
        this.AddElement("Back-Button", new TextureButtonElement(buttonData, backLabelData, Anchor.Center, new Vector2(-200, -120), size: backButtonSize, textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            GuiManager.SetGui(new ExtandedOptionsGui());
            return true;
        }));

        string resetText = Localization.T("common.reset");
        Vector2 resetButtonSize = GuiText.ButtonSize(resetText, 120, maxWidth: 175);
        LabelData resetLabelData = GuiText.ButtonLabel(resetText, resetButtonSize.X);
        this.AddElement("Reset-Button", new TextureButtonElement(buttonData, resetLabelData, Anchor.Center, new Vector2(170, -120), size: resetButtonSize, textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            this._captureFor = null;
            KeyBindinds.ResetToDefaults();
            RefreshButtonTexts();
            return true;
        }));

        this.AddElement("Move-Left-Button", MakeBindButton(buttonData, Localization.F(""), KeyBindinds.GetMoveLeft(), new Vector2(0, -50), BindingAction.MoveLeft));
        this.AddElement("Move-Right-Button", MakeBindButton(buttonData, "MoveRight", KeyBindinds.GetMoveRight(), new Vector2(0, 0), BindingAction.MoveRight));
        this.AddElement("Jump-Button", MakeBindButton(buttonData, "Jump", KeyBindinds.GetJump(), new Vector2(0, 50), BindingAction.Jump));
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (this._captureFor.HasValue)
        {
            KeyboardKey? pressed = TryGetPressedKey();
            if (pressed.HasValue)
            {
                switch (this._captureFor.Value)
                {
                    case BindingAction.MoveLeft:
                        KeyBindinds.SetMoveLeft(pressed.Value);
                        break;
                    case BindingAction.MoveRight:
                        KeyBindinds.SetMoveRight(pressed.Value);
                        break;
                    case BindingAction.Jump:
                        KeyBindinds.SetJump(pressed.Value);
                        break;
                }

                this._captureFor = null;
                RefreshButtonTexts();
            }
        }

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            GuiManager.SetGui(new ExtandedOptionsGui());
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        if (SceneManager.ActiveScene == null)
        {
            Texture2D backgroundTexture = ContentRegistry.Background2;
            Vector2 backgroundSize = new Vector2((float)GlobalGraphicsAssets.Window.GetWidth() / backgroundTexture.Width, (float)GlobalGraphicsAssets.Window.GetHeight() / backgroundTexture.Height);

            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.SpriteBatch.DrawTexture(backgroundTexture, Vector2.Zero, scale: backgroundSize);
            context.SpriteBatch.End();
        }

        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, ModalGuiRenderer.DefaultBaseSize);

        base.Draw(context, framebuffer);
    }

    private TextureButtonElement MakeBindButton(TextureButtonData buttonData, string labelKey, KeyboardKey currentKey, Vector2 offset, BindingAction bindingAction)
    {
        const float bindButtonWidth = 320F;
        LabelData bindLabelData = GuiText.ButtonLabel(BuildBindText(labelKey, currentKey), bindButtonWidth);
        return new TextureButtonElement(buttonData, bindLabelData, Anchor.Center, offset, size: new Vector2(bindButtonWidth, 40), textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            this._captureFor = bindingAction;
            bindLabelData.Text = $"{Localization.T(labelKey)}: ...";
            bindLabelData.Size = GuiText.ButtonLabel(bindLabelData.Text, bindButtonWidth).Size;
            return true;
        });
    }

    private void RefreshButtonTexts()
    {
        SetButtonText("Move-Left-Button", "MoveLeft", KeyBindinds.GetMoveLeft());
        SetButtonText("Move-Right-Button", "MoveRight", KeyBindinds.GetMoveRight());
        SetButtonText("Jump-Button", "Jump", KeyBindinds.GetJump());
    }

    private void SetButtonText(string elementName, string actionNameKey, KeyboardKey key)
    {
        if (this.TryGetElement(elementName, out GuiElement? element) && element is TextureButtonElement button)
        {
            button.LabelData.Text = BuildBindText(actionNameKey, key);
            button.LabelData.Size = GuiText.ButtonLabel(button.LabelData.Text, 320F).Size;
        }
    }

    private static string BuildBindText(string actionKey, KeyboardKey key)
    {
        return $"{Localization.T(actionKey)}: {key}";
    }

    private static KeyboardKey? TryGetPressedKey()
    {
        foreach (KeyboardKey key in Enum.GetValues<KeyboardKey>())
        {
            if (Input.IsKeyPressed(key))
            {
                return key;
            }
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
