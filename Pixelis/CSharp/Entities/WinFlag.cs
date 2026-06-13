using System.Numerics;
using Box2D;
using Pixelis.CSharp.Registries;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Entities;

public class WinFlag : Entity
{
    private readonly float _layerDepth;

    public WinFlag(Transform transform, float layerDepth = 0.5F) : base(transform, "flag")
    {
        _layerDepth = layerDepth;
    }

    protected override void Init()
    {
        base.Init();
        
        this.AddComponent(LevelScene.CreateAtlasSprite(AtlasRegistry.WorldAtlasTexture, AtlasRegistry.WinFlagRegion, _layerDepth));
        
        RigidBody2D body = new RigidBody2D(new BodyDefinition()
        {
            Type = BodyType.Static
        }, new PolygonShape2D(Polygon.MakeBox(12, 48 / 2.0F), new ShapeDef()
        {
            Density = 100,
            UserData = "flag",
            EnableContactEvents = true
        }));
        
        this.AddComponent(body);
    }
}
