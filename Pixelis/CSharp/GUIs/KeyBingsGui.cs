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
using Veldrid;

namespace Pixelis.CSharp.GUIs;

public class KeyBingsGui : Gui
{
    private BindingAction? _captureFor;

    private enum BindingAction
    {
        MoveLeft,
        MoveRight,
        Jump
    }

    public KeyBingsGui() : base("Key Binds")
    {
    }

    protected override void Init()
    {
        base.Init();

        LabelData titleData = new LabelData(ContentRegistry.Fontoe, "Key Binds", 18);
        this.AddElement("Title", new LabelElement(titleData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

        TextureButtonData buttonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData backLabelData = new LabelData(ContentRegistry.Fontoe, "Back", 18, hoverColor: Color.White);
        this.AddElement("Back-Button", new TextureButtonElement(buttonData, backLabelData, Anchor.Center, new Vector2(-200, -120), size: new Vector2(100, 40), textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            GuiManager.SetGui(new OptionsGui());
            return true;
        }));

        this.AddElement("Move-Left-Button", MakeBindButton(buttonData, "Move Left", KeyBindings.GetMoveLeft(), new Vector2(0, -50), BindingAction.MoveLeft));
        this.AddElement("Move-Right-Button", MakeBindButton(buttonData, "Move Right", KeyBindings.GetMoveRight(), new Vector2(0, 0), BindingAction.MoveRight));
        this.AddElement("Jump-Button", MakeBindButton(buttonData, "Jump", KeyBindings.GetJump(), new Vector2(0, 50), BindingAction.Jump));
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
                        KeyBindings.SetMoveLeft(pressed.Value);
                        break;
                    case BindingAction.MoveRight:
                        KeyBindings.SetMoveRight(pressed.Value);
                        break;
                    case BindingAction.Jump:
                        KeyBindings.SetJump(pressed.Value);
                        break;
                }

                this._captureFor = null;
                RefreshButtonTexts();
            }
        }

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            GuiManager.SetGui(new OptionsGui());
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

    private TextureButtonElement MakeBindButton(TextureButtonData buttonData, string label, KeyboardKey currentKey, Vector2 offset, BindingAction bindingAction)
    {
        LabelData bindLabelData = new LabelData(ContentRegistry.Fontoe, BuildBindText(label, currentKey), 18, hoverColor: Color.White);
        return new TextureButtonElement(buttonData, bindLabelData, Anchor.Center, offset, size: new Vector2(280, 40), textOffset: new Vector2(0, 1), clickFunc: _ =>
        {
            this._captureFor = bindingAction;
            bindLabelData.Text = $"{label}: ...";
            return true;
        });
    }

    private void RefreshButtonTexts()
    {
        SetButtonText("Move-Left-Button", "Move Left", KeyBindings.GetMoveLeft());
        SetButtonText("Move-Right-Button", "Move Right", KeyBindings.GetMoveRight());
        SetButtonText("Jump-Button", "Jump", KeyBindings.GetJump());
    }

    private void SetButtonText(string elementName, string actionName, KeyboardKey key)
    {
        if (this.TryGetElement(elementName, out GuiElement? element) && element is TextureButtonElement button)
        {
            button.LabelData.Text = BuildBindText(actionName, key);
        }
    }

    private static string BuildBindText(string action, KeyboardKey key)
    {
        return $"{action}: {key}";
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
}
