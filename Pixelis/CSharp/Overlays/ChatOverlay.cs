using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Overlays;
using Veldrid;

namespace Pixelis.CSharp.Overlays;

public class ChatOverlay : Overlay
{
    private const string InputPrefix = "> ";
    private const int MaxMessages = 10;
    private const float BaseFontSize = 16.0f;
    private const float VisibleDuration = 5.0f;
    private const float FadeDuration = 1.25f;
    private readonly List<string> _messages = new();
    private string _inputBuffer = string.Empty;
    private bool _isOpen = true;
    private bool _isInputActive;
    private int _caretIndex;
    private (int Start, int End) _highlightRange;
    private float _cursorBlinkTimer;
    private bool _showCursor = true;
    private float _visibilityTimer = VisibleDuration;

    private readonly record struct WrappedLine(string Text, int StartIndex);

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
            this.InsertText(text.Replace("\n", string.Empty).Replace("\r", string.Empty));
        }

        if (Input.IsKeyPressed(KeyboardKey.BackSpace, true))
        {
            this.HandleBackspace();
        }

        if (Input.IsKeyPressed(KeyboardKey.Delete, true))
        {
            this.HandleDelete();
        }

        bool shiftDown = Input.IsKeyDown(KeyboardKey.ShiftLeft) || Input.IsKeyDown(KeyboardKey.ShiftRight);
        bool controlDown = Input.IsKeyDown(KeyboardKey.ControlLeft) || Input.IsKeyDown(KeyboardKey.ControlRight);

        if (Input.IsKeyPressed(KeyboardKey.Left, true))
        {
            this.MoveCaret(-1, shiftDown);
        }

        if (Input.IsKeyPressed(KeyboardKey.Right, true))
        {
            this.MoveCaret(1, shiftDown);
        }

        if (Input.IsKeyPressed(KeyboardKey.Home, true))
        {
            this.MoveCaretTo(0, shiftDown);
        }

        if (Input.IsKeyPressed(KeyboardKey.End, true))
        {
            this.MoveCaretTo(this._inputBuffer.Length, shiftDown);
        }

        if (controlDown && Input.IsKeyPressed(KeyboardKey.A))
        {
            this._highlightRange = (0, this._inputBuffer.Length);
            this._caretIndex = this._inputBuffer.Length;
            this.ResetCursorBlink();
        }

        if (controlDown && Input.IsKeyPressed(KeyboardKey.C))
        {
            this.CopySelection();
        }

        if (controlDown && Input.IsKeyPressed(KeyboardKey.X))
        {
            this.CutSelection();
        }

        if (controlDown && Input.IsKeyPressed(KeyboardKey.V))
        {
            this.PasteClipboard();
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

        float uiScale = MathF.Max(1.0f, GuiManager.Scale);
        float fontSize = BaseFontSize * uiScale;
        float width = 430 * uiScale;
        float lineHeight = 18 * uiScale;
        float padding = 10 * uiScale;
        float borderThickness = 2 * uiScale;
        float textWidth = width - padding * 2 - 4;
        List<string> historyLines = this.GetHistoryLines(textWidth, fontSize);
        int visibleLines = historyLines.Count;
        float historyHeight = Math.Max(visibleLines, 1) * lineHeight;
        List<WrappedLine> inputLines = this.GetInputLines(textWidth, fontSize);
        float inputBoxHeight = Math.Max(22 * uiScale, inputLines.Count * lineHeight + 6 * uiScale);
        float panelHeight = historyHeight + inputBoxHeight + 24 * uiScale;
        float windowHeight = GlobalGraphicsAssets.Window.GetHeight();
        Vector2 origin = new Vector2(16 * uiScale, Math.Max(16 * uiScale, windowHeight - panelHeight - 16 * uiScale));
        float inputBoxY = origin.Y + panelHeight - inputBoxHeight - padding;
        Vector2 inputTextOrigin = new Vector2(origin.X + padding + 2 * uiScale, inputBoxY + 3 * uiScale);

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(origin.X, origin.Y, width, panelHeight), color: ApplyAlpha(new Color(12, 16, 24, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(origin, new Vector2(origin.X + width, origin.Y), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(new Vector2(origin.X, origin.Y + panelHeight), new Vector2(origin.X + width, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(origin, new Vector2(origin.X, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawLine(new Vector2(origin.X + width, origin.Y), new Vector2(origin.X + width, origin.Y + panelHeight), borderThickness, 0.5f, ApplyAlpha(new Color(220, 220, 220, 180), alphaFactor));
        context.PrimitiveBatch.DrawFilledRectangle(new RectangleF(origin.X + padding, inputBoxY, width - padding * 2, inputBoxHeight), color: ApplyAlpha(new Color(30, 36, 48, 220), alphaFactor));

        if (this._isInputActive)
        {
            this.DrawInputSelection(context, inputLines, inputTextOrigin, lineHeight, alphaFactor, fontSize);
            this.DrawInputCaret(context, inputLines, inputTextOrigin, lineHeight, alphaFactor, fontSize, uiScale);
        }

        context.PrimitiveBatch.End();

        context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);

        for (int i = 0; i < visibleLines; i++)
        {
            context.SpriteBatch.DrawText(ContentRegistry.Fontoe, historyLines[i], new Vector2(origin.X + padding, origin.Y + padding + i * lineHeight), (int)fontSize, color: ApplyAlpha(Color.LightGray, alphaFactor));
        }

        for (int i = 0; i < inputLines.Count; i++)
        {
            context.SpriteBatch.DrawText(
                ContentRegistry.Fontoe,
                inputLines[i].Text,
                new Vector2(inputTextOrigin.X, inputTextOrigin.Y + i * lineHeight),
                (int)fontSize,
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
        this._caretIndex = 0;
        this._highlightRange = (0, 0);
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

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int start = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int end = Math.Max(this._highlightRange.Start, this._highlightRange.End);

        if (start != end)
        {
            this._inputBuffer = this._inputBuffer.Remove(start, end - start).Insert(start, text);
            this._caretIndex = start + text.Length;
            this._highlightRange = (0, 0);
        }
        else
        {
            this._inputBuffer = this._inputBuffer.Insert(this._caretIndex, text);
            this._caretIndex += text.Length;
        }

        this.ResetCursorBlink();
    }

    private void HandleBackspace()
    {
        int start = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int end = Math.Max(this._highlightRange.Start, this._highlightRange.End);

        if (start != end)
        {
            this._inputBuffer = this._inputBuffer.Remove(start, end - start);
            this._caretIndex = start;
            this._highlightRange = (0, 0);
            this.ResetCursorBlink();
            return;
        }

        if (this._caretIndex <= 0)
        {
            return;
        }

        this._inputBuffer = this._inputBuffer.Remove(this._caretIndex - 1, 1);
        this._caretIndex--;
        this.ResetCursorBlink();
    }

    private void HandleDelete()
    {
        int start = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int end = Math.Max(this._highlightRange.Start, this._highlightRange.End);

        if (start != end)
        {
            this._inputBuffer = this._inputBuffer.Remove(start, end - start);
            this._caretIndex = start;
            this._highlightRange = (0, 0);
            this.ResetCursorBlink();
            return;
        }

        if (this._caretIndex >= this._inputBuffer.Length)
        {
            return;
        }

        this._inputBuffer = this._inputBuffer.Remove(this._caretIndex, 1);
        this.ResetCursorBlink();
    }

    private void MoveCaret(int delta, bool extendSelection)
    {
        if (!extendSelection && this._highlightRange.Start != this._highlightRange.End)
        {
            this._caretIndex = delta < 0
                ? Math.Min(this._highlightRange.Start, this._highlightRange.End)
                : Math.Max(this._highlightRange.Start, this._highlightRange.End);
            this._highlightRange = (0, 0);
            this.ResetCursorBlink();
            return;
        }

        int newCaretIndex = Math.Clamp(this._caretIndex + delta, 0, this._inputBuffer.Length);
        this.MoveCaretTo(newCaretIndex, extendSelection);
    }

    private void MoveCaretTo(int newCaretIndex, bool extendSelection)
    {
        newCaretIndex = Math.Clamp(newCaretIndex, 0, this._inputBuffer.Length);

        if (extendSelection)
        {
            if (this._highlightRange.Start == this._highlightRange.End)
            {
                this._highlightRange = (this._caretIndex, newCaretIndex);
            }
            else
            {
                this._highlightRange = (this._highlightRange.Start, newCaretIndex);
            }
        }
        else
        {
            this._highlightRange = (0, 0);
        }

        this._caretIndex = newCaretIndex;
        this.ResetCursorBlink();
    }

    private void CopySelection()
    {
        int start = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int end = Math.Max(this._highlightRange.Start, this._highlightRange.End);
        if (start == end)
        {
            return;
        }

        Input.SetClipboardText(this._inputBuffer.Substring(start, end - start));
    }

    private void CutSelection()
    {
        int start = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int end = Math.Max(this._highlightRange.Start, this._highlightRange.End);
        if (start == end)
        {
            return;
        }

        this.CopySelection();
        this._inputBuffer = this._inputBuffer.Remove(start, end - start);
        this._caretIndex = start;
        this._highlightRange = (0, 0);
        this.ResetCursorBlink();
    }

    private void PasteClipboard()
    {
        string clipboardText = Input.GetClipboardText().Replace("\n", string.Empty).Replace("\r", string.Empty);
        this.InsertText(clipboardText);
    }

    private List<string> GetHistoryLines(float maxWidth, float fontSize)
    {
        List<string> lines = new();

        foreach (string message in this._messages)
        {
            lines.AddRange(this.WrapText(message, maxWidth, fontSize));
        }

        if (lines.Count > MaxMessages)
        {
            lines = lines[^MaxMessages..];
        }

        return lines;
    }

    private List<WrappedLine> GetInputLines(float maxWidth, float fontSize)
    {
        string inputText = this._isInputActive
            ? $"{InputPrefix}{this._inputBuffer}"
            : "Press Enter to chat";

        return this.WrapTextWithIndices(inputText, maxWidth, fontSize);
    }

    private List<string> WrapText(string text, float maxWidth, float fontSize)
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
            float nextLineWidth = ContentRegistry.Fontoe.MeasureText(nextLine, (int)fontSize, Vector2.One).X;

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

    private List<WrappedLine> WrapTextWithIndices(string text, float maxWidth, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [new WrappedLine(string.Empty, 0)];
        }

        List<WrappedLine> lines = new();
        string currentLine = string.Empty;
        int currentLineStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            string nextLine = currentLine + text[i];
            float nextLineWidth = ContentRegistry.Fontoe.MeasureText(nextLine, (int)fontSize, Vector2.One).X;

            if (currentLine.Length > 0 && nextLineWidth > maxWidth)
            {
                lines.Add(new WrappedLine(currentLine, currentLineStart));
                currentLine = text[i].ToString();
                currentLineStart = i;
            }
            else
            {
                currentLine = nextLine;
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(new WrappedLine(currentLine, currentLineStart));
        }

        if (lines.Count == 0)
        {
            lines.Add(new WrappedLine(string.Empty, 0));
        }

        return lines;
    }

    private void DrawInputSelection(GraphicsContext context, List<WrappedLine> inputLines, Vector2 textOrigin, float lineHeight, float alphaFactor, float fontSize)
    {
        int selectionStart = Math.Min(this._highlightRange.Start, this._highlightRange.End);
        int selectionEnd = Math.Max(this._highlightRange.Start, this._highlightRange.End);
        if (selectionStart == selectionEnd)
        {
            return;
        }

        int displaySelectionStart = InputPrefix.Length + selectionStart;
        int displaySelectionEnd = InputPrefix.Length + selectionEnd;

        for (int i = 0; i < inputLines.Count; i++)
        {
            WrappedLine line = inputLines[i];
            int lineStart = line.StartIndex;
            int lineEnd = line.StartIndex + line.Text.Length;
            int visibleStart = Math.Max(displaySelectionStart, lineStart);
            int visibleEnd = Math.Min(displaySelectionEnd, lineEnd);
            if (visibleStart >= visibleEnd)
            {
                continue;
            }

            int startInLine = visibleStart - lineStart;
            int endInLine = visibleEnd - lineStart;
            float startX = this.MeasureTextWidth(line.Text[..startInLine], fontSize);
            float selectionWidth = this.MeasureTextWidth(line.Text[startInLine..endInLine], fontSize);

            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(textOrigin.X + startX, textOrigin.Y + i * lineHeight, selectionWidth, lineHeight),
                color: ApplyAlpha(new Color(75, 110, 175, 180), alphaFactor));
        }
    }

    private void DrawInputCaret(GraphicsContext context, List<WrappedLine> inputLines, Vector2 textOrigin, float lineHeight, float alphaFactor, float fontSize, float uiScale)
    {
        if (!this._showCursor)
        {
            return;
        }

        int displayCaretIndex = InputPrefix.Length + this._caretIndex;

        for (int i = 0; i < inputLines.Count; i++)
        {
            WrappedLine line = inputLines[i];
            int lineStart = line.StartIndex;
            int lineEnd = line.StartIndex + line.Text.Length;
            bool isLastLine = i == inputLines.Count - 1;

            if (displayCaretIndex < lineStart || (!isLastLine && displayCaretIndex >= lineEnd))
            {
                continue;
            }

            int caretInLine = Math.Clamp(displayCaretIndex - lineStart, 0, line.Text.Length);
            float caretX = this.MeasureTextWidth(line.Text[..caretInLine], fontSize);
            context.PrimitiveBatch.DrawFilledRectangle(
                new RectangleF(textOrigin.X + caretX, textOrigin.Y + i * lineHeight, 2 * uiScale, lineHeight),
                color: ApplyAlpha(Color.White, alphaFactor));
            return;
        }
    }

    private float MeasureTextWidth(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0.0f;
        }

        return ContentRegistry.Fontoe.MeasureText(text, (int)fontSize, Vector2.One).X;
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
