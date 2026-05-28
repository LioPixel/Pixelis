using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Sprites;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Riptide;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.GUI.Elements;
using Sparkle.CSharp.GUI.Elements.Data;
using Sparkle.CSharp.Scenes;
using Veldrith;

namespace Pixelis.CSharp.GUIs;

public class JoinGui : Gui
{
    private const string OfficialServer1Address = "92.204.41.78:7777";
    private bool _isConnecting = false;
    private string _errorMessage = "";
    private float _errorDisplayTime = 0f;
    private const float OfficialServerPanelLeft = 20F;
    private readonly Vector2 _officialServerPanelSize = new Vector2(210, 250);
    private readonly string[] _officialServerStatuses =
    [
        Localization.T("gui.join.server_status.off"),
        Localization.T("gui.join.server_status.off"),
        Localization.T("gui.join.server_status.off"),
        Localization.T("gui.join.server_status.off"),
        Localization.T("gui.join.server_status.off")
    ];
    private Task? _serverStatusProbeTask;
    private float _serverStatusProbeCooldown;
    
    public JoinGui () : base("Join", null) { }

    protected override void Init()
    {
        base.Init();
        
        LabelData labelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.join"), 18);
        this.AddElement("Titel", new LabelElement(labelData, Anchor.TopCenter, new Vector2(0, 50), new Vector2(5, 5)));

        LabelData officialServerListTitleData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.join.official_servers"), 18, color: Color.White);
        this.AddElement("Official-Server-List-Title", new LabelElement(officialServerListTitleData, Anchor.CenterLeft, new Vector2(27, -107), new Vector2(1.2F, 1.2F)));
        
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
        
        // IP address text box.
        TextureTextBoxData ipadressTextBoxData = new TextureTextBoxData(ContentRegistry.UiMenu, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12), flip: SpriteFlip.None);
        LabelData ipadressTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, hoverColor: Color.White);
        LabelData ipadressHintTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.join.ip_hint"), 18, color: Color.Gray);
        
        this.AddElement("IP-Adress-Text-Box", new TextureTextBoxElement(ipadressTextBoxData, ipadressTextBoxLabelData, ipadressHintTextBoxLabelData, Anchor.Center, new Vector2(0, -10), 40, TextAlignment.Center, new Vector2(0, 1), Vector2.One, (12, 12), new Vector2(260, 30), rotation: 0, clickFunc: (element) => {
            return true;
        }));
        
        // Join button.
        TextureButtonData createButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));
        LabelData createButtonLabelData = GuiText.ButtonLabel(Localization.T("gui.multiplayer.join"), 230);
        
        this.AddElement("Join-Button", new TextureButtonElement(createButtonData, createButtonLabelData, Anchor.Center, new Vector2(0, 60), size: new Vector2(230, 40), textOffset: new Vector2(0, 1), clickFunc: (element) =>
        {
            if (!_isConnecting)
            {
                TryJoinServer();
            }
            return true;
        }));
        
        // Error message label (initially empty)
        LabelData errorLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, color: Color.Red);
        this.AddElement("Error-Label", new LabelElement(errorLabelData, Anchor.Center, new Vector2(0, 110), new Vector2(1, 1)));
        
        // Username text box.
        TextureTextBoxData nameTextBoxData = new TextureTextBoxData(ContentRegistry.UiMenu, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12), flip: SpriteFlip.None);
        LabelData nameTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, "", 18, hoverColor: Color.White);
        LabelData nameHintTextBoxLabelData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.multiplayer.name_hint"), 18, color: Color.Gray);
        
        this.AddElement("Name-Text-Box", new TextureTextBoxElement(nameTextBoxData, nameTextBoxLabelData, nameHintTextBoxLabelData, Anchor.Center, new Vector2(120, -120), 15, TextAlignment.Center, new Vector2(0, 1), Vector2.One, (12, 12), new Vector2(230, 30), rotation: 0, clickFunc: (element) => {
            return true;
        }));

        TextureButtonData serverButtonData = new TextureButtonData(ContentRegistry.UiButton, hoverColor: Color.LightGray, resizeMode: ResizeMode.NineSlice, borderInsets: new BorderInsets(12));

        this.AddElement("Official-Server-1-Button", new TextureButtonElement(
            serverButtonData,
            GuiText.ButtonLabel("Pixelis 1", 170),
            Anchor.CenterLeft,
            new Vector2(40, -67),
            size: new Vector2(170, 26),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                if (!_isConnecting)
                {
                    TryJoinServerWithAddress(OfficialServer1Address);
                }
                return true;
            }));

        this.AddElement("Official-Server-2-Button", new TextureButtonElement(
            serverButtonData,
            GuiText.ButtonLabel("Pixelis 2", 170),
            Anchor.CenterLeft,
            new Vector2(40, -35),
            size: new Vector2(170, 26),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                ShowComingSoonMessage();
                return true;
            }));

        this.AddElement("Official-Server-3-Button", new TextureButtonElement(
            serverButtonData,
            GuiText.ButtonLabel("Pixelis 3", 170),
            Anchor.CenterLeft,
            new Vector2(40, -3),
            size: new Vector2(170, 26),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                ShowComingSoonMessage();
                return true;
            }));

        this.AddElement("Official-Server-4-Button", new TextureButtonElement(
            serverButtonData,
            GuiText.ButtonLabel("Pixelis 4", 170),
            Anchor.CenterLeft,
            new Vector2(40, 29),
            size: new Vector2(170, 26),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                ShowComingSoonMessage();
                return true;
            }));

        this.AddElement("Official-Server-5-Button", new TextureButtonElement(
            serverButtonData,
            GuiText.ButtonLabel("Pixelis 5", 170),
            Anchor.CenterLeft,
            new Vector2(40, 61),
            size: new Vector2(170, 26),
            textOffset: new Vector2(0, 1),
            clickFunc: _ =>
            {
                ShowComingSoonMessage();
                return true;
            }));

        LabelData officialServerHintData = new LabelData(ContentRegistry.Fontoe, Localization.T("gui.join.coming_soon"), 18, color: Color.LightGray);
        this.AddElement("Official-Server-Hint", new LabelElement(officialServerHintData, Anchor.CenterLeft, new Vector2(50, 95), new Vector2(1, 1)));
        RefreshOfficialServerLabels();
        QueueServerStatusProbe();
    }
    
    private void TryJoinServer()
    {
        // Get IP from text box
        TextureTextBoxElement ipTextBox = (TextureTextBoxElement)this.GetElement("IP-Adress-Text-Box");
        string ipAddress = ipTextBox.LabelData.Text.Trim();
        TryJoinServerWithAddress(string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1:7777" : ipAddress);
    }

    private void TryJoinServerWithAddress(string ipAddress)
    {
        TextureTextBoxElement nameTextBox = (TextureTextBoxElement)this.GetElement("Name-Text-Box");
        string username = nameTextBox.LabelData.Text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            username = Localization.T("network.player.default_name");
        }

        _isConnecting = true;
        _errorMessage = Localization.T("gui.join.connecting");
        UpdateErrorLabel();

        NetworkManager.SetConnectionCallbacks(OnConnectionSuccess, OnConnectionFailed);
        NetworkManager.JoinServer(ipAddress, username);
    }

    private void ShowComingSoonMessage()
    {
        _errorMessage = Localization.T("gui.join.official_server_coming_soon");
        _errorDisplayTime = 3f;
        UpdateErrorLabel();
    }
    
    private void OnConnectionSuccess()
    {
        _isConnecting = false;
        _errorMessage = "";
        
        // Close the GUI on successful connection
        GuiManager.SetGui(null);
    }
    
    private void OnConnectionFailed(string reason)
    {
        _isConnecting = false;
        _errorMessage = Localization.F("gui.join.connection_failed", reason);
        _errorDisplayTime = 5f; // Display error for 5 seconds
        UpdateErrorLabel();
    }
    
    private void UpdateErrorLabel()
    {
        LabelElement errorLabel = (LabelElement)this.GetElement("Error-Label");
        if (errorLabel != null)
        {
            errorLabel.Data.Text = _errorMessage;
        }
    }

    private void QueueServerStatusProbe()
    {
        _serverStatusProbeCooldown = 5f;
        _serverStatusProbeTask = ProbeOfficialServerStatusesAsync();
    }

    private async Task ProbeOfficialServerStatusesAsync()
    {
        _officialServerStatuses[0] = await ProbeServerAsync(OfficialServer1Address) ? Localization.T("gui.join.server_status.on") : Localization.T("gui.join.server_status.off");
        _officialServerStatuses[1] = Localization.T("gui.join.server_status.off");
        _officialServerStatuses[2] = Localization.T("gui.join.server_status.off");
        _officialServerStatuses[3] = Localization.T("gui.join.server_status.off");
        _officialServerStatuses[4] = Localization.T("gui.join.server_status.off");
        RefreshOfficialServerLabels();
    }

    private async Task<bool> ProbeServerAsync(string endpoint)
    {
        Client? probeClient = null;

        try
        {
            probeClient = new Client();
            TaskCompletionSource<bool> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            probeClient.Connected += (_, _) => completionSource.TrySetResult(true);
            probeClient.ConnectionFailed += (_, _) => completionSource.TrySetResult(false);
            probeClient.Disconnected += (_, _) => completionSource.TrySetResult(false);
            probeClient.Connect(endpoint);

            DateTime deadline = DateTime.UtcNow.AddSeconds(1.5);
            while (!completionSource.Task.IsCompleted && DateTime.UtcNow < deadline)
            {
                probeClient.Update();
                await Task.Delay(50);
            }

            if (!completionSource.Task.IsCompleted)
            {
                return false;
            }

            bool isOnline = await completionSource.Task;

            if (probeClient.IsConnected)
            {
                probeClient.Disconnect();
            }

            return isOnline;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (probeClient != null && probeClient.IsConnected)
            {
                probeClient.Disconnect();
            }
        }
    }

    private void RefreshOfficialServerLabels()
    {
        SetOfficialServerButtonLabel("Official-Server-1-Button", $"Pixelis 1 ({_officialServerStatuses[0]})");
        SetOfficialServerButtonLabel("Official-Server-2-Button", $"Pixelis 2 ({_officialServerStatuses[1]})");
        SetOfficialServerButtonLabel("Official-Server-3-Button", $"Pixelis 3 ({_officialServerStatuses[2]})");
        SetOfficialServerButtonLabel("Official-Server-4-Button", $"Pixelis 4 ({_officialServerStatuses[3]})");
        SetOfficialServerButtonLabel("Official-Server-5-Button", $"Pixelis 5 ({_officialServerStatuses[4]})");
    }

    private void SetOfficialServerButtonLabel(string elementName, string text)
    {
        if (this.GetElement(elementName) is TextureButtonElement button)
        {
            button.LabelData.Text = text;
            button.LabelData.Size = GuiText.ButtonLabel(text, 170).Size;
        }
    }
    
    protected override void Update(double delta)
    {
        base.Update(delta);

        // Countdown error display time
        if (_errorDisplayTime > 0)
        {
            _errorDisplayTime -= (float)delta;
            if (_errorDisplayTime <= 0)
            {
                _errorMessage = "";
                UpdateErrorLabel();
            }
        }

        _serverStatusProbeCooldown -= (float)delta;
        if (_serverStatusProbeCooldown <= 0 && (_serverStatusProbeTask == null || _serverStatusProbeTask.IsCompleted))
        {
            QueueServerStatusProbe();
        }

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
        
        ModalGuiRenderer.DrawModalBackground(context, framebuffer, this.ScaleFactor, ModalGuiRenderer.DefaultBaseSize);

        float scale = this.ScaleFactor;
        Vector2 panelSize = _officialServerPanelSize * scale;
        Vector2 panelPosition = new Vector2(
            MathF.Floor(OfficialServerPanelLeft / scale) * scale,
            MathF.Floor(((GlobalGraphicsAssets.Window.GetHeight() / 2F) - (panelSize.Y / 2F)) / scale) * scale);

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription);
        context.PrimitiveBatch.DrawFilledRectangle(
            new RectangleF(panelPosition.X, panelPosition.Y, panelSize.X, panelSize.Y),
            color: new Color(35, 35, 35, 235));
        context.PrimitiveBatch.DrawEmptyRectangle(
            new RectangleF(panelPosition.X, panelPosition.Y, panelSize.X, panelSize.Y),
            3 * scale,
            color: Color.White);
        context.PrimitiveBatch.End();
        
        base.Draw(context, framebuffer);
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}
