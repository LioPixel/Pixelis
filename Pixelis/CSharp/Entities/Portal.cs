using System.Numerics;
using Box2D;
using Pixelis.CSharp.Registries;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Color = Bliss.CSharp.Colors.Color;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Entities;

public class Portal : Entity
{
    public Vector2 TeleportPos { get; private set; }
    
    private Sprite _sprite;
    private Color? _color;
    private readonly float _layerDepth;
    private float _timer;
    private int _frame;
    private Rectangle _portalRegion;
    
    
    private const float FrameTime = 0.15f;
    private const int TotalFrames = 6;
    
    public Portal(Transform transform, Vector2 teleportPos, Color? color = null, float layerDepth = 0.5F) : base(transform, "portal")
    {
        this.TeleportPos = teleportPos;
        this._color = color;
        this._layerDepth = layerDepth;
    }
    
    protected override void Init()
    {
        base.Init();

        this._portalRegion = AtlasRegistry.PortalRegion;
        this._sprite = LevelScene.CreateAtlasSprite(AtlasRegistry.WorldAtlasTexture, this.GetFrameRegion(), this._layerDepth, new Vector2(80, -8), this._color);
        this.AddComponent(this._sprite);
        
        RigidBody2D body = new RigidBody2D(new BodyDefinition()
        {
            Type = BodyType.Static
        }, new PolygonShape2D(Polygon.MakeOffsetBox(9, 16, new Vector2(0, -8), Rotation.Identity), new ShapeDef()
        {
            Density = 100,
            UserData = this,
            EnableContactEvents = true
        }));
        
        this.AddComponent(body);
    }

    protected override void Update(double delta)
    {
        base.Update(delta);
        
        // increment timer
        _timer += (float)delta;

        // check if it's time to switch frame
        if (_timer >= FrameTime)
        {
            _timer = 0f;
            _frame++;

            if (_frame >= TotalFrames)
                _frame = 0;

            _sprite.SourceRect = this.GetFrameRegion();
        }
    }

    private Rectangle GetFrameRegion()
    {
        return new Rectangle(this._portalRegion.X + this._frame * 32, this._portalRegion.Y, 32, 32);
    }
}
