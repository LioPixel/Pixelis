using Pixelis.CSharp;
using Sparkle.CSharp;
using Sparkle.CSharp.GUI.Loading;
using Veldrith;

GameSettings settings = new GameSettings()
{
    VSync = false,
    SampleCount = TextureSampleCount.Count8,
    IconPath = "content/logo.png",
    Title = "Pixelis - 2D [By LioPixel & MrScautHD]"
};

using PixelisGame game = new PixelisGame(settings);
game.Run(null, new LogoLoadingGui("Startup", "content/sparkle/images/logo.png"));
