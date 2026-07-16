using MiniAudioEx.Core.StandardAPI;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.Overlays;
using Pixelis.CSharp.Registries;
using Sparkle.CSharp;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.IO.Json;
using Sparkle.CSharp.Overlays;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp;

public class PixelisGame : Game
{
    public JsonConfig OptionsConfig;

    public PixelisGame(GameSettings settings) : base(settings)
    {
        this.OptionsConfig = new JsonConfig("options", "configs", new Dictionary<string, object>()
        {
            { "Vsync", false },
            { "DebugMode", false },
            { "Sounds", true },
            { "MasterVolume", 0.5 },
            { "GuiScale", 0 },
            { "Language", Localization.English },
            { "Toggle-Fps", true },
            { "KeybindMoveLeft", "A" },
            { "KeybindMoveRight", "D" },
            { "KeybindJump", "Space" },
        });
    }

    protected override void OnRun()
    {
        base.OnRun();
        RegistryManager.AddRegistry(new TextureRegistry());
        RegistryManager.AddRegistry(new AudioRegistry());
        RegistryManager.AddRegistry(new FontRegistry());
        RegistryManager.AddRegistry(new AtlasRegistry());
        
        // Apply Config settings:
        GlobalGraphicsAssets.GraphicsDevice.SyncToVerticalBlank = this.OptionsConfig.GetValue<bool>("Vsync");
        int guiScale = Math.Clamp((int)MathF.Round(this.OptionsConfig.GetValue<float>("GuiScale")), 0, GuiManager.MaxAllowedScaleFactor);
        this.OptionsConfig.SetValue("GuiScale", guiScale);
        GuiManager.SetScale(guiScale);
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
