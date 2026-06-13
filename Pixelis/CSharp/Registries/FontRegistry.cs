using Bliss.CSharp.Fonts;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Content.Types;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp.Registries;

public class FontRegistry : Registry
{
    public static Font Fontoe { get; private set; }
    
    protected override void Load(ContentManager content)
    {
        base.Load(content);
        
        Fontoe = content.Load(new FontContent("content/fontoe.ttf"));
    }

    protected override void Dispose(bool disposing) {}
}