using System.Numerics;
using Bliss.CSharp.Transformations;
using Box2D;
using Pixelis.CSharp.Registries;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Entities;

public class MovingBlock : Entity {

    public int BlockPosX { get; private set; }
    public int BlockPosY { get; private set; }
    public int TargetBlockPosX;
    public int TargetBlockPosY;
    public float Speed;
    public float LayerDepth;
    
    private RigidBody2D _rigidBody;
    private double _lifeTime;
    
    public MovingBlock(int blockPosX, int blockPosY, int targetBlockPosX, int targetBlockPosY, float speed, float layerDepth = 0.5F) : base(new Transform() { Translation = new Vector3(blockPosX, blockPosY, 0)}, "Moving Block") {
        this.BlockPosX = blockPosX * 16;
        this.BlockPosY = blockPosY * 16;
        this.TargetBlockPosX = targetBlockPosX * 16;
        this.TargetBlockPosY = targetBlockPosY * 16;
        this.Speed = speed;
        this.LayerDepth = layerDepth;
    }

    /// <summary>
    /// Initializes the MovingPlatform instance by setting up its visual and physics components.
    /// </summary>
    protected override void Init() {
        base.Init();
        
        this.AddComponent(LevelScene.CreateAtlasSprite(AtlasRegistry.WorldAtlasTexture, AtlasRegistry.SpriteRegion, this.LayerDepth));

        this._rigidBody = new RigidBody2D(new BodyDefinition() { Type = BodyType.Kinematic },
            new PolygonShape2D(Polygon.MakeBox(8, 8), new ShapeDef() {
                EnableContactEvents = true,
                EnableSensorEvents = true,
                UserData = "MovingBlock"
            })
        );
        
        this.AddComponent(this._rigidBody);
    }

    /// <summary>
    /// Updates the state of the moving platform over time, including its position and velocity, based on a sinusoidal movement pattern between start and target positions.
    /// </summary>
    /// <param name="delta">The elapsed time since the last update, in seconds.</param>
    protected override void Update(double delta) {
        base.Update(delta);
        
        this._lifeTime += delta;

        // Override the fixed scaling value (if 16 is the scale factor).
        const float scale = 16.0f;

        // Start and target positions in world coordinates (pixels).
        float startX = this.BlockPosX / scale;
        float startY = this.BlockPosY / scale;
        float targetX = this.TargetBlockPosX / scale;
        float targetY = this.TargetBlockPosY / scale;

        // Sinusoidal movement.
        float time = (float) (this._lifeTime * this.Speed);
        float progress = (float) (Math.Sin(time) * 0.5F + 0.5F);

        // Calculate position based on progress.
        float posX = startX + (targetX - startX) * progress;
        float posY = startY + (targetY - startY) * progress;

        // Velocity based on derivative.
        float velocityX = (targetX - startX) * (float) (Math.Cos(time) * this.Speed * 0.5);
        float velocityY = (targetY - startY) * (float) (Math.Cos(time) * this.Speed * 0.5);

        // Apply physical movement.
        this._rigidBody.LinearVelocity = new Vector2(velocityX * scale, velocityY * scale);
        this.LocalTransform.Translation = new Vector3(posX * scale, posY * scale, 0.0F);
    }
}
