using Bliss.CSharp.Fonts;
using Bliss.CSharp.Textures;
using MiniAudioEx.Core.StandardAPI;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Content.Types;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp;

public class ContentRegistry : Registry {
    
    public static Texture2D Sprite { get; private set; }
    public static AudioClip Jump { get; private set; }
    public static Texture2D ToggleBackground { get; private set; }
    public static Texture2D ToggleCheckmark { get; private set; }
    public static Texture2D Button { get; private set; }
    public static Texture2D PlayerIdleLeft { get; private set; }
    public static Texture2D PlayerIdleRight { get; private set; }
    public static Texture2D PlayerJumpLeft { get; private set; }
    public static Texture2D PlayerJumpRight { get; private set; }
    public static Texture2D PlayerWalkLeft { get; private set; }
    public static Texture2D PlayerWalkRight { get; private set; }
    public static Texture2D Background { get; private set; }
    public static Texture2D Background2 { get; private set; }
    public static Texture2D Background3 { get; private set; }
    public static Texture2D Background4 { get; private set; }
    public static Texture2D Background5 { get; private set; }
    public static Texture2D Background6 { get; private set; }
    public static Texture2D Background7 { get; private set; }
    public static Texture2D Background8 { get; private set; }
    public static Texture2D Background9 { get; private set; }
    public static Texture2D Background10 { get; private set; }
    public static Texture2D Background11 { get; private set; }
    public static Texture2D Background12 { get; private set; }
    public static Texture2D Portal { get; private set; }
    public static Texture2D OakLog { get; private set; }
    public static Texture2D PlantFlowerRed { get; private set; }
    public static Texture2D RockGrass { get; private set; }
    public static Texture2D BushDead { get; private set; }
    public static Texture2D PlantSunFlower { get; private set; }
    public static Texture2D Bush { get; private set; }
    public static Texture2D PlantFlower { get; private set; }
    public static Texture2D TreeBigDead { get; private set; }
    public static Texture2D TreeBig { get; private set; }
    public static Texture2D WinFlag { get; private set; }
    public static Texture2D MenuBackground { get; private set; }
    public static Texture2D UiArrow { get; private set; }
    public static Texture2D UiMenu { get; private set; }
    public static Texture2D UiButton { get; private set; }
    public static Texture2D UiSlider { get; private set; }
    public static Texture2D UiBar { get; private set; }
    public static Texture2D UiFilledBar { get; private set; }
    public static Texture2D UiSliderLowRes { get; private set; }
    public static Texture2D Logo { get; private set; }
    public static Texture2D Grass { get; private set; }

    public static Font Fontoe { get; private set; }

    protected override void Load(ContentManager content)
    {
        base.Load(content);
        
        Sprite = content.Load(new TextureContent("content/sprite.png"));
        Jump = content.Load(new AudioClipContent("content/jump.mp3"));
        ToggleBackground = content.Load(new TextureContent("content/ui_checkbox.png"));
        ToggleCheckmark = content.Load(new TextureContent("content/ui_checkmark.png"));
        Button = content.Load(new TextureContent("content/button.png"));
        Background = content.Load(new TextureContent("content/background.png"));
        PlayerIdleLeft = content.Load(new TextureContent("content/player/idle_left.png"));
        PlayerIdleRight = content.Load(new TextureContent("content/player/idle_right.png"));
        PlayerJumpLeft = content.Load(new TextureContent("content/player/jump_left.png"));
        PlayerJumpRight = content.Load(new TextureContent("content/player/jump_right.png"));
        PlayerWalkLeft = content.Load(new TextureContent("content/player/walk_left.png"));
        PlayerWalkRight = content.Load(new TextureContent("content/player/walk_right.png"));
        Background2 = content.Load(new TextureContent("content/background2.png"));
        Background3 = content.Load(new TextureContent("content/background3.png"));
        Background4 = content.Load(new TextureContent("content/background4.png"));
        Background5 = content.Load(new TextureContent("content/background5.png"));
        Background6 = content.Load(new TextureContent("content/background6.png"));
        Background7 = content.Load(new TextureContent("content/background7.png"));
        Background8 = content.Load(new TextureContent("content/background8.png"));
        Background9 = content.Load(new TextureContent("content/background9.png"));
        Background10 = content.Load(new TextureContent("content/background10.png"));
        Background11 = content.Load(new TextureContent("content/background11.png"));
        Background12 = content.Load(new TextureContent("content/background12.png"));
        Portal = content.Load(new TextureContent("content/portal.png"));
        OakLog = content.Load(new TextureContent("content/oak_log.png"));
        PlantFlowerRed = content.Load(new TextureContent("content/plant_flower_red.png"));
        RockGrass = content.Load(new TextureContent("content/rock_grass.png"));
        BushDead = content.Load(new TextureContent("content/bush_dead.png"));
        PlantSunFlower = content.Load(new TextureContent("content/plant_sun_flower.png"));
        Bush = content.Load(new TextureContent("content/bush.png"));
        PlantFlower = content.Load(new TextureContent("content/plant_flower.png"));
        TreeBig = content.Load(new TextureContent("content/tree_big.png"));
        TreeBigDead = content.Load(new TextureContent("content/tree_big_dead.png"));
        WinFlag = content.Load(new TextureContent("content/win_flag.png"));
        MenuBackground = content.Load(new TextureContent("content/menu_background.png"));
        UiArrow = content.Load(new TextureContent("content/ui_arrow.png"));
        UiMenu = content.Load(new TextureContent("content/ui_menu.png"));
        UiButton = content.Load(new TextureContent("content/ui_button.png"));
        UiSlider = content.Load(new TextureContent("content/ui_slider.png"));
        UiBar = content.Load(new TextureContent("content/ui_bar.png"));
        UiFilledBar = content.Load(new TextureContent("content/ui_filled_bar.png"));
        UiSliderLowRes = content.Load(new TextureContent("content/ui_slider_low_res.png"));
        Logo = content.Load(new TextureContent("content/logo.png"));
        Grass = content.Load(new TextureContent("content/grass.png"));
        Fontoe = content.Load(new FontContent("content/fontoe.ttf"));
    }

    protected override void Dispose(bool disposing)
    {
        
    }
}