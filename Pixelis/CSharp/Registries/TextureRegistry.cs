using Bliss.CSharp.Textures;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Content.Types;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp.Registries;

public class TextureRegistry : Registry
{
    // Logo.
    public static Texture2D Logo { get; private set; }
    
    // Gui backgrounds.
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
    
    protected override void Load(ContentManager content) {
        base.Load(content);
        
        // Logo.
        Logo = content.Load(new TextureContent("content/logo.png"));
        
        // Gui backgrounds.
        Background = content.Load(new TextureContent("content/background.png"));
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
        
    }
    
    protected override void Dispose(bool disposing) {}
}