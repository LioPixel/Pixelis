using Bliss.CSharp.Atlases;
using Bliss.CSharp.Images;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp.Registries;

public class AtlasRegistry : Registry
{
    public static TextureAtlas UiTextureAtlas { get; private set; }
    public static Texture2D UiAtlasTexture => UiTextureAtlas.Texture;
    
    public static TextureAtlas WorldTextureAtlas { get; private set; }
    public static Texture2D WorldAtlasTexture => WorldTextureAtlas.Texture;
    
    // UI.
    public static Rectangle ArrowRegion => UiTextureAtlas.GetRegion("arrow");
    public static Rectangle MenuRegion => UiTextureAtlas.GetRegion("menu");
    public static Rectangle ButtonRegion => UiTextureAtlas.GetRegion("button");
    public static Rectangle SliderRegion => UiTextureAtlas.GetRegion("slider");
    public static Rectangle BarRegion => UiTextureAtlas.GetRegion("bar");
    public static Rectangle FilledBarRegion => UiTextureAtlas.GetRegion("filled_bar");
    public static Rectangle SliderLowResRegion => UiTextureAtlas.GetRegion("slider_low_res");
    public static Rectangle CheckBoxRegion => UiTextureAtlas.GetRegion("checkbox");
    public static Rectangle CheckMarkRegion => UiTextureAtlas.GetRegion("checkmark");

    
    // World.
    public static Rectangle SpriteRegion => WorldTextureAtlas.GetRegion("sprite");
    public static Rectangle PortalRegion => WorldTextureAtlas.GetRegion("portal");
    public static Rectangle OakLogRegion => WorldTextureAtlas.GetRegion("oak_log");
    public static Rectangle PlantFlowerRedRegion => WorldTextureAtlas.GetRegion("plant_flower_red");
    public static Rectangle RockGrassRegion => WorldTextureAtlas.GetRegion("rock_grass");
    public static Rectangle BushDeadRegion => WorldTextureAtlas.GetRegion("bush_dead");
    public static Rectangle PlantSunFlowerRegion => WorldTextureAtlas.GetRegion("plant_sun_flower");
    public static Rectangle BushRegion => WorldTextureAtlas.GetRegion("bush");
    public static Rectangle PlantFlowerRegion => WorldTextureAtlas.GetRegion("plant_flower");
    public static Rectangle TreeBigRegion => WorldTextureAtlas.GetRegion("tree_big");
    public static Rectangle TreeBigDeadRegion => WorldTextureAtlas.GetRegion("tree_big_dead");
    public static Rectangle WinFlagRegion => WorldTextureAtlas.GetRegion("win_flag");
    public static Rectangle GrassRegion => WorldTextureAtlas.GetRegion("grass");
    
    // Player.
    public static AtlasAnimation PlayerIdleLeftRegion => WorldTextureAtlas.GetAnimation("player_idle_left");
    public static AtlasAnimation PlayerIdleRightRegion => WorldTextureAtlas.GetAnimation("player_idle_right");
    public static AtlasAnimation PlayerJumpLeftRegion => WorldTextureAtlas.GetAnimation("player_jump_left");
    public static AtlasAnimation PlayerJumpRightRegion => WorldTextureAtlas.GetAnimation("player_jump_right");
    public static AtlasAnimation PlayerWalkLeftRegion => WorldTextureAtlas.GetAnimation("player_walk_left");
    public static AtlasAnimation PlayerWalkRightRegion => WorldTextureAtlas.GetAnimation("player_walk_right");
    
    protected override void Load(ContentManager content) {
        base.Load(content);
        
        // Create Ui Atlas.
        TextureAtlasBuilder uiAtlasBuilder = new TextureAtlasBuilder();
        
        // UI.
        uiAtlasBuilder.Add("arrow", "content/ui_arrow.png");
        uiAtlasBuilder.Add("menu", "content/ui_menu.png");
        uiAtlasBuilder.Add("button", "content/ui_button.png");
        uiAtlasBuilder.Add("slider", "content/ui_slider.png");
        uiAtlasBuilder.Add("bar", "content/ui_bar.png");
        uiAtlasBuilder.Add("filled_bar", "content/ui_filled_bar.png");
        uiAtlasBuilder.Add("slider_low_res", "content/ui_slider_low_res.png");
        uiAtlasBuilder.Add("checkbox", "content/ui_checkbox.png");
        uiAtlasBuilder.Add("checkmark", "content/ui_checkmark.png");
        
        UiTextureAtlas = uiAtlasBuilder.Build(content.GraphicsDevice);
        
        // Create World Atlas.
        TextureAtlasBuilder worldAtlasBuilder = new TextureAtlasBuilder();
        
        // World.
        worldAtlasBuilder.Add("sprite", "content/sprite.png");
        worldAtlasBuilder.Add("portal", "content/portal.png");
        worldAtlasBuilder.Add("oak_log", "content/oak_log.png");
        worldAtlasBuilder.Add("plant_flower_red", "content/plant_flower_red.png");
        worldAtlasBuilder.Add("rock_grass", "content/rock_grass.png");
        worldAtlasBuilder.Add("bush_dead", "content/bush_dead.png");
        worldAtlasBuilder.Add("plant_sun_flower", "content/plant_sun_flower.png");
        worldAtlasBuilder.Add("bush", "content/bush.png");
        worldAtlasBuilder.Add("plant_flower", "content/plant_flower.png");
        worldAtlasBuilder.Add("tree_big", "content/tree_big.png");
        worldAtlasBuilder.Add("tree_big_dead", "content/tree_big_dead.png");
        worldAtlasBuilder.Add("win_flag", "content/win_flag.png");
        worldAtlasBuilder.Add("grass", "content/grass.png");
        
        // Player.
        worldAtlasBuilder.Add("player_idle_left", new AnimatedImage("content/player/idle_left.gif"));
        worldAtlasBuilder.Add("player_idle_right", new AnimatedImage("content/player/idle_right.gif"));
        worldAtlasBuilder.Add("player_jump_left", new AnimatedImage("content/player/jump_left.gif"));
        worldAtlasBuilder.Add("player_jump_right", new AnimatedImage("content/player/jump_right.gif"));
        worldAtlasBuilder.Add("player_walk_left", new AnimatedImage("content/player/walk_left.gif"));
        worldAtlasBuilder.Add("player_walk_right", new AnimatedImage("content/player/walk_right.gif"));
        
        WorldTextureAtlas = worldAtlasBuilder.Build(content.GraphicsDevice);
    }
    
    protected override void Dispose(bool disposing)
    {
        
    }
}