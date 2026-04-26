using MiniAudioEx.Core.StandardAPI;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.Overlays;
using Sparkle.CSharp;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.IO.Configs.Json;
using Sparkle.CSharp.Overlays;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp;

public class PixelisGame : Game
{
    public JsonConfig OptionsConfig;

    public PixelisGame(GameSettings settings) : base(settings)
    {
        JsonConfigBuilder jsonConfigBuilder = new JsonConfigBuilder("configs", "options");
        jsonConfigBuilder.Add("Vsync", false);
        jsonConfigBuilder.Add("DebugMode", false);
        jsonConfigBuilder.Add("Sounds", true);
        jsonConfigBuilder.Add("MasterVolume", 0.5F);
        jsonConfigBuilder.Add("GuiScale", 1.0F);
        jsonConfigBuilder.Add("Toggle-Fps", true);
        this.OptionsConfig = jsonConfigBuilder.Build();
    }

    protected override void OnRun()
    {
        base.OnRun();
        RegistryManager.AddRegistry(new ContentRegistry());
        
        // Apply Config settings:
        GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank = this.OptionsConfig.GetValue<bool>("Vsync");
        float guiScale = MathF.Max(1.0F, MathF.Round(this.OptionsConfig.GetValue<float>("GuiScale")));
        this.OptionsConfig.SetValue("GuiScale", guiScale);
        GuiManager.Scale = guiScale;
    }
    
    protected override void Init()
    {
        base.Init();
        GuiManager.SetGui(new MenuGui());
        OverlayManager.AddOverlay(new DebugOverlay("Debug", true));
        OverlayManager.AddOverlay(new ChatOverlay("Chat", true));
        AudioContext.MasterVolume = ((PixelisGame) Game.Instance!).OptionsConfig.GetValue<float>("MasterVolume");
    }

    protected override void Load(ContentManager content)
    {
        base.Load(content);
    }

    protected override void Update(double delta)
    {
        base.Update(delta);
        
        NetworkManager.Update();
    }

    protected override void FixedUpdate(double fixedStep)
    {
        base.FixedUpdate(fixedStep);
    }
}
