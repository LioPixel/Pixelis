using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp;
using Sparkle.CSharp.Overlays;
using Veldrid;

namespace Pixelis.CSharp.Overlays;

public class ChatOverlay : Overlay
{
    private const int MaxMessages = 10;
    private const float VisibleDuration = 5.0f;
    private const float FadeDuration = 1.25f;
    private readonly List<string> _messages = new();
    private string _inputBuffer = string.Empty;
    private bool _isOpen = true;
    private bool _isInputActive;
    private float _cursorBlinkTimer;
    private bool _showCursor = true;
    private float _visibilityTimer = VisibleDuration;

    public ChatOverlay(string name, bool enabled = true) : base(name, enabled)
    {
        NetworkManager.ChatMessageReceived += this.OnChatMessageReceived;
        NetworkManager.ChatClearedReceived += this.OnChatClearedReceived;
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (!this._isInputActive)
        {
            this._visibilityTimer += (float)delta;
        }

        if (Input.IsKeyPressed(KeyboardKey.Enter))
        {
            if (!this._isOpen)
            {
                this._isOpen = true;
                this._isInputActive = true;
                this.ShowFully();
                NetworkManager.SetChatInputBlocked(true);
                Input.EnableTextInput();
            }
            else if (!this._isInputActive)
            {
                this._isInputActive = true;
                this.ShowFully();
                NetworkManager.SetChatInputBlocked(true);
                Input.EnableTextInput();
            }
            else
            {
                this.SubmitCurrentInput();
            }
        }

        if (!this._isOpen || !this._isInputActive)
        {
            return;
        }

        if (!Input.IsTextInputActive())
        {
            Input.EnableTextInput();
        }

        if (Input.GetTypedText(out string text) && !string.IsNullOrEmpty(text))
        {
            this._inputBuffer += text.Replace("\n", string.Empty).Replace("\r", string.Empty);
            this.ResetCursorBlink();
        }

        if (Input.IsKeyPressed(KeyboardKey.BackSpace, true) && this._inputBuffer.Length > 0)
        {
            this._inputBuffer = this._inputBuffer[..^1];
            this.ResetCursorBlink();
        }

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            this.CloseInput();
        }

        this._cursorBlinkTimer += (float)delta;
        if (this._cursorBlinkTimer >= 0.5f)
        {
            this._cursorBlinkTimer = 0.0f;
            this._showCursor = !this._showCursor;
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        if (!this._isOpen)
        {
            return;
        }

        float alphaFactor = this.GetAlphaFactor();
        if (alphaFactor <= 0.0f)
        {
            return;
        }

        float width = 430;
        float lineHeight = 18;
        float padding = 10;
        float borderThickness = 2;
        float textWidth = width - padding * 2 - 4;
        List<string> historyLines = this.GetHistoryLines(textWidth);
        int visibleLines = historyLines.Count;
        float historyHeight = Math.Max(visibleLines, 1) * lineHeight;
        List<string> inputLines = this.GetInputLines(textWidth);
        float inputBoxHeight = Math.Max(22, inputLines.Count * lineHeight + 6);
        float panelHeight = historyHeight + inputBoxHeight + 24;
        float windowHeight = GlobalGraphicsAssets.Window.GetHeight();
        Vector2 origin = new Vector2(16, Math.Max(16, windowHeight - panelHeight - 16));
        float inputBoxY = origin.Y + panelHeight - inputBoxHeight - padding;

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(origin.X, origin.Y, width, panelHeight), color: ApplyAlpha(new Color(12, 16, 24, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(origin, new Vector2(origin.X + width, origin.Y), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(new Vector2(origin.X, origin.Y + panelHeight), new Vector2(origin.X + width, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(origin, new Vector2(origin.X, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(new Vector2(origin.X + width, origin.Y), new Vector2(origin.X + width, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(origin.X + padding, inputBoxY, width - padding * 2, inputBoxHeight), color: ApplyAlpha(new Color(30, 36, 48, 220), alphaFactor));
        context.PrimitiveBatch.End();

        context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);

        for (int i = 0; i < visibleLines; i++)
        {
            context.SpriteBatch.DrawText(ContentRegistry.Fontoe, historyLines[i], new Vector2(origin.X + padding, origin.Y + padding + i * lineHeight), 16, color: ApplyAlpha(Color.LightGray, alphaFactor));
        }

        for (int i = 0; i < inputLines.Count; i++)
        {
            context.SpriteBatch.DrawText(
                ContentRegistry.Fontoe,
                inputLines[i],
                new Vector2(origin.X + padding + 2, inputBoxY + 3 + i * lineHeight),
                16,
                color: ApplyAlpha(this._isInputActive ? Color.White : Color.Gray, alphaFactor));
        }

        context.SpriteBatch.End();
    }

    private void SubmitCurrentInput()
    {
        string message = this._inputBuffer.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            this.CloseInput();
            return;
        }

        NetworkManager.SubmitChatInput(message);
        this._inputBuffer = string.Empty;
        this.CloseInput();
    }

    private void CloseInput()
    {
        this._isInputActive = false;
        this.ShowFully();
        NetworkManager.SetChatInputBlocked(false);
        Input.DisableTextInput();
    }

    private void OnChatMessageReceived(string message)
    {
        this.AddMessage(message);
    }

    private void OnChatClearedReceived()
    {
        this._messages.Clear();
        this.ShowFully();
    }

    private void AddMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        this._messages.Add(message);
        this.ShowFully();
        if (this._messages.Count > MaxMessages)
        {
            this._messages.RemoveAt(0);
        }
    }

    private void ResetCursorBlink()
    {
        this._cursorBlinkTimer = 0.0f;
        this._showCursor = true;
    }

    private void ShowFully()
    {
        this._visibilityTimer = 0.0f;
    }

    private List<string> GetHistoryLines(float maxWidth)
    {
        List<string> lines = new();

        foreach (string message in this._messages)
        {
            lines.AddRange(this.WrapText(message, maxWidth));
        }

        if (lines.Count > MaxMessages)
        {
            lines = lines[^MaxMessages..];
        }

        return lines;
    }

    private List<string> GetInputLines(float maxWidth)
    {
        string inputText = this._isInputActive
            ? $"> {this._inputBuffer}{(this._showCursor ? "_" : string.Empty)}"
            : "Press Enter to chat";

        return this.WrapText(inputText, maxWidth);
    }

    private List<string> WrapText(string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [string.Empty];
        }

        List<string> lines = new();
        string currentLine = string.Empty;

        foreach (char character in text)
        {
            string nextLine = currentLine + character;
            float nextLineWidth = ContentRegistry.Fontoe.MeasureText(nextLine, 16, Vector2.One).X;

            if (currentLine.Length > 0 && nextLineWidth > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = character.ToString();
            }
            else
            {
                currentLine = nextLine;
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private float GetAlphaFactor()
    {
        if (this._isInputActive)
        {
            return 1.0f;
        }

        if (this._visibilityTimer <= VisibleDuration)
        {
            return 1.0f;
        }

        float fadeProgress = (this._visibilityTimer - VisibleDuration) / FadeDuration;
        return Math.Clamp(1.0f - fadeProgress, 0.0f, 1.0f);
    }

    private static Color ApplyAlpha(Color color, float alphaFactor)
    {
        return new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)(color.A * alphaFactor), 0, 255));
    }
}
