using MiniAudioEx.Core.StandardAPI;
using Sparkle.CSharp.Content;
using Sparkle.CSharp.Registries;

namespace Pixelis.CSharp.Registries;

public class AudioRegistry : Registry
{
    public static AudioClip Jump { get; private set; }
    
    protected override void Load(ContentManager content)
    {
        base.Load(content);
        
        
    }

    protected override void Dispose(bool disposing) {}
}