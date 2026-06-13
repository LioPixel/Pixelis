using System.Numerics;
using Bliss.CSharp.Atlases;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Transformations;
using Box2D;
using MiniAudioEx.Core.StandardAPI;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.Controls;
using Pixelis.CSharp.Registries;
using Pixelis.CSharp.Scenes;
using Sparkle.CSharp;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.Graphics.Particles.Dim2;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Physics.Dim2;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Sparkle.CSharp.Scenes;
using Veldrith;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Entities;

public class Player : Entity
{
    public string UserName { get; private set; }
    public int IsPlayerOnGround;
    public PlayerPoseType PoseType;

    // Network sync properties
    public bool IsLocalPlayer;
    public Vector3 NetworkedPosition;
    public PlayerPoseType NetworkedPoseType;

    private Sprite _sprite;
    private float _timer;
    private int _frame;
    private AtlasAnimation _activeAnimation = null!;
    private PlayerPoseType _activeAnimationPoseType;
    private bool _isJumping;
    private AudioSource _audioSource;

    private readonly HashSet<ulong> _groundContacts = new();
    private readonly HashSet<ulong> _leftContacts = new();
    private readonly HashSet<ulong> _rightContacts = new();

    private Vector2 _previousPlatformVelocity;

    // Network update timer
    private float _networkUpdateTimer;
    private const float NetworkUpdateInterval = 0.016f;

    // Debug timer
    private float _networkDebugTimer;

    // Name display
    private Font _nameFont;
    private Vector2 _nameOffset = new Vector2(0, -20);

    // Respawn system
    private const float DEATH_Y = 100f;
    private Vector3 _spawnPoint;
    public Vector3 SpawnPoint => _spawnPoint;

    // Level completion
    private bool _hasCompletedLevel = false;
    private bool _hasReportedDeath;

    private const float BaseMaxSpeed  = 50f;
    private const float BaseGravity   = 15.5f;
    private const float BaseJumpForce = 90f;
    private static readonly Vector2 SpriteOffset = new(0F, -2F);

    public Player(Transform transform, bool isLocalPlayer = true, string userName = "") : base(transform, "player")
    {
        IsLocalPlayer = isLocalPlayer;
        UserName = userName;
        NetworkedPosition = transform.Translation;
        _spawnPoint = transform.Translation;
    }

    protected override void Init()
    {
        base.Init();
        this.PoseType = PlayerPoseType.RightIdle;
        this.NetworkedPoseType = PlayerPoseType.RightIdle;
        this._activeAnimationPoseType = this.PoseType;
        this._activeAnimation = this.GetAnimationByPoseType(this.PoseType);
        Rectangle initialFrame = this._activeAnimation.GetFrameRegion(0);
        this._sprite = new Sprite(
            AtlasRegistry.WorldAtlasTexture,
            SpriteOffset,
            sourceRect: initialFrame,
            size: new Vector2(initialFrame.Width, initialFrame.Height),
            layerDepth: 1);
        this.AddComponent(this._sprite);
        
        RigidBody2D body = new RigidBody2D(new BodyDefinition()
        {
            Type = IsLocalPlayer ? BodyType.Dynamic : BodyType.Kinematic,
            FixedRotation = true,
            GravityScale = IsLocalPlayer ? BaseGravity : 0F
        }, new PolygonShape2D(Polygon.MakeBox(7, 8), new ShapeDef()
        {
            Density = 100,
            UserData = "Player",
            EnableContactEvents = IsLocalPlayer,
            EnableSensorEvents = IsLocalPlayer,
            Filter = new Filter()
            {
                CategoryBits = 0x0002,
                MaskBits = 0xFFFD
            }
        }));

        this.AddComponent(body);

        body.CreateShape(new ShapeDef()
        {
            IsSensor = true,
            UserData = "PlayerLeftSensor",
            EnableContactEvents = false,
            EnableSensorEvents = true,
            Filter = new Filter()
            {
                CategoryBits = 0x0002,
                MaskBits = 0xFFFD
            }
        }, Polygon.MakeOffsetBox(2, 7, new Vector2(-7, -1), Rotation.Identity));

        body.CreateShape(new ShapeDef()
        {
            IsSensor = true,
            UserData = "PlayerRightSensor",
            EnableContactEvents = false,
            EnableSensorEvents = true,
            Filter = new Filter()
            {
                CategoryBits = 0x0002,
                MaskBits = 0xFFFD
            }
        }, Polygon.MakeOffsetBox(2, 7, new Vector2(7, -1), Rotation.Identity));

        if (IsLocalPlayer)
        {
            ((Simulation2D)this.Scene.Simulation).ContactBeginTouch += this.ContactBeginTouch;
            ((Simulation2D)this.Scene.Simulation).ContactEndTouch += this.ContactEndTouch;
            ((Simulation2D)this.Scene.Simulation).SensorBeginTouch += this.ContactBeginSensorTouch;
            ((Simulation2D)this.Scene.Simulation).SensorEndTouch += this.ContactEndSensorTouch;
        }

        this._audioSource = new AudioSource();
        this._nameFont = FontRegistry.Fontoe;

        ParticleDefinition2D footDust = new ParticleDefinition2D(AtlasRegistry.WorldAtlasTexture, sourceRect: AtlasRegistry.SpriteRegion) {
            Looping = true,
            Duration = 9999.0F,
            EmissionRate = 0.0F,
            MaxParticles = 120,
            StartLifetime = 0.22F,
            LifetimeRandomness = 0.08F,
            StartSpeed = 2.2F * 16,
            SpeedRandomness = 0.9F,
            StartScale = new Vector2(0.10F, 0.10F),
            EndScale = new Vector2(0.03F, 0.03F),
            Acceleration = new Vector2(0, -1.5F) * 16,
            Gravity = new Vector2(0, 3.0F) * 16,
            Direction = new Vector2(1, -0.15F),
            Spread = 0.9F,
            SpawnBox = new Vector2(10.0F, 2.0F),
            Bounciness = 0.0F,
            CollisionDamping = 0.65F,
            CollisionSurfaceOffset = 0.02F,
            SimulateInWorldSpace = true
        };

        ParticleSystem2D footParticles = new ParticleSystem2D(footDust, new Vector3(0, 8, 0));
        this.AddComponent(footParticles);
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (!IsLocalPlayer)
        {
            UpdateNetworkedPlayer(delta);
            return;
        }

        bool isGround = IsPlayerOnGround > 0;
        bool isLeftWallCol = _leftContacts.Count > 0;
        bool isRightWallCol = _rightContacts.Count > 0;

        RigidBody2D body = this.GetComponent<RigidBody2D>()!;
        Vector2 velocity = body.LinearVelocity;

        if (_isJumping && isGround && velocity.Y >= 0F)
            this.LandFromJump();

        bool canMoveWithGui = this.Scene is CustomLevelScene customLevelScene && customLevelScene.IsPlayingFromEditor;

        if (GuiManager.ActiveGui == null || canMoveWithGui)
        {
            if (!NetworkManager.IsChatInputBlocked())
            {
                float groundAccel = 3f;
                float airAccel    = 0.5f;
                float maxSpeed    = BaseMaxSpeed;
                float jumpForce   = BaseJumpForce;
                KeyboardKey moveLeftKey = KeyBindinds.GetMoveLeft();
                KeyboardKey moveRightKey = KeyBindinds.GetMoveRight();
                KeyboardKey jumpKey = KeyBindinds.GetJump();

                bool emitParticles = false;

                float input = 0f;
                bool moveLeftDown = Input.IsKeyDown(moveLeftKey) || Input.IsKeyDown(KeyboardKey.Left);
                bool moveRightDown = Input.IsKeyDown(moveRightKey) || Input.IsKeyDown(KeyboardKey.Right);
                bool jumpPressed = Input.IsKeyPressed(jumpKey) || Input.IsKeyPressed(KeyboardKey.W) || Input.IsKeyPressed(KeyboardKey.Up);

                if (moveLeftDown && !isLeftWallCol)
                {
                    input -= 1f;
                    if (!_isJumping && isGround)
                    {
                        this.PoseType = PlayerPoseType.LeftWalk;
                        this.GetComponent<ParticleSystem2D>()?.Definition.Direction = new Vector2(1, 0.15F);
                        emitParticles = true;
                    }
                }

                if (moveRightDown && !isRightWallCol)
                {
                    input += 1f;
                    if (!_isJumping && isGround)
                    {
                        this.PoseType = PlayerPoseType.RightWalk;
                        this.GetComponent<ParticleSystem2D>()?.Definition.Direction = new Vector2(-1, 0.15F);
                        emitParticles = true;
                    }
                }

                if (emitParticles)
                {
                    this.GetComponent<ParticleSystem2D>()?.Definition.EmissionRate = 28;
                    this.GetComponent<ParticleSystem2D>()?.Definition.Looping = true;
                }
                else
                {
                    this.GetComponent<ParticleSystem2D>()?.Definition.EmissionRate = 0;
                    this.GetComponent<ParticleSystem2D>()?.Definition.Looping = false;
                }

                if (!moveRightDown && !moveLeftDown)
                {
                    if (!this._isJumping)
                    {
                        if (this.PoseType == PlayerPoseType.LeftWalk)
                        {
                            this.PoseType = PlayerPoseType.LeftIdle;
                        }

                        if (this.PoseType == PlayerPoseType.RightWalk)
                        {
                            this.PoseType = PlayerPoseType.RightIdle;
                        }
                    }
                }

                if (input != 0f)
                {
                    float accel = isGround ? groundAccel : airAccel;
                    velocity.X += input * accel;
                    velocity.X = Math.Clamp(velocity.X, -maxSpeed, maxSpeed);
                }
                else if (isGround)
                {
                    velocity.X *= 0.8f;
                }

                if (jumpPressed && isGround && !_isJumping)
                {
                    velocity.Y = -jumpForce;

                    if (this.PoseType == PlayerPoseType.LeftWalk || this.PoseType == PlayerPoseType.LeftIdle)
                    {
                        this.PoseType = PlayerPoseType.JumpLeft;
                    }

                    if (this.PoseType == PlayerPoseType.RightWalk || this.PoseType == PlayerPoseType.RightIdle)
                    {
                        this.PoseType = PlayerPoseType.JumpRight;
                    }

                    this._isJumping = true;

                    //if (((PixelisGame)Game.Instance!).OptionsConfig.GetValue<bool>("Sounds"))
                    //    this._audioSource.Play(ContentRegistry.Jump);
                }
            }
            else
            {
                this.GetComponent<ParticleSystem2D>()?.Definition.EmissionRate = 0;
                this.GetComponent<ParticleSystem2D>()?.Definition.Looping = false;
                if (isGround && !this._isJumping)
                {
                    velocity.X *= 0.8f;
                }
            }
        }

        body.LinearVelocity = velocity;

        _networkUpdateTimer += (float)delta;
        if (_networkUpdateTimer >= NetworkUpdateInterval)
        {
            _networkUpdateTimer = 0;
            NetworkManager.SendPlayerPosition(this.LocalTransform.Translation, this.PoseType);
        }

        this.GameOver();
        this.UpdateSpriteAnimation(delta);
    }

    private void Respawn()
    {
        this.LocalTransform.Translation = _spawnPoint;
        RigidBody2D body = this.GetComponent<RigidBody2D>()!;
        body.LinearVelocity = Vector2.Zero;
    }

    public void SetSpawnPoint(Vector3 spawnPoint)
    {
        _spawnPoint = spawnPoint;
    }

    private void UpdateNetworkedPlayer(double delta)
    {
        this.PoseType = NetworkedPoseType;
        this.LocalTransform.Translation = NetworkedPosition;

        _networkDebugTimer += (float)delta;
        if (_networkDebugTimer >= 1.0f)
            _networkDebugTimer = 0;

        this.UpdateSpriteAnimation(delta);
    }

    protected override void FixedUpdate(double fixedStep)
    {
        base.FixedUpdate(fixedStep);

        if (!IsLocalPlayer) return;

        RigidBody2D body = this.GetComponent<RigidBody2D>()!;
        Vector2 platformVelocity = Vector2.Zero;

        foreach (ContactData contact in body.Contacts)
        {
            if (contact.ShapeA.UserData?.ToString() == "MovingBlock")
                platformVelocity = contact.ShapeA.Body.LinearVelocity;
            else if (contact.ShapeB.UserData?.ToString() == "MovingBlock")
                platformVelocity = contact.ShapeB.Body.LinearVelocity;
        }
        
        if (platformVelocity != Vector2.Zero)
        {
            Vector3 current = this.LocalTransform.Translation;
            this.LocalTransform.Translation = new Vector3(
                current.X + platformVelocity.X * (float)fixedStep,
                current.Y,
                current.Z
            );
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        base.Draw(context, framebuffer);

        if (this.UserName != string.Empty)
        {
            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription, view: SceneManager.ActiveCam2D?.GetView());
            Vector2 scale = new Vector2(0.25F, 0.25F);
            Vector2 textSize = FontRegistry.Fontoe.MeasureText(this.UserName, 18, scale);
            Vector2 namePos = new Vector2(this.LocalTransform.Translation.X - textSize.X / 2.0F, this.LocalTransform.Translation.Y - 22);
            context.SpriteBatch.DrawText(FontRegistry.Fontoe, this.UserName, namePos, 18, scale: scale, color: Color.Gray, layerDepth:1);
            context.SpriteBatch.End();
        }
    }

    public void GameOver()
    {
        if (!IsLocalPlayer) return;

        if (this.LocalTransform.Translation.Y < DEATH_Y)
        {
            _hasReportedDeath = false;
            return;
        }

        if (!_hasReportedDeath)
        {
            _hasReportedDeath = true;
            NetworkManager.NotifyPlayerDied();
        }

        GuiManager.SetGui(new GameOverGui());
    }

    private void ContactBeginSensorTouch(SensorBeginTouchEvent e)
    {
        RigidBody2D myBody = this.GetComponent<RigidBody2D>()!;

        if (e.SensorShape.UserData?.ToString() == "PlayerLeftSensor" && e.SensorShape.Body == myBody.Body)
            _leftContacts.Add(ContactKey(e.SensorShape, e.VisitorShape));
        else if (e.VisitorShape.UserData?.ToString() == "PlayerLeftSensor" && e.VisitorShape.Body == myBody.Body)
            _leftContacts.Add(ContactKey(e.SensorShape, e.VisitorShape));

        if (e.SensorShape.UserData?.ToString() == "PlayerRightSensor" && e.SensorShape.Body == myBody.Body)
            _rightContacts.Add(ContactKey(e.SensorShape, e.VisitorShape));
        else if (e.VisitorShape.UserData?.ToString() == "PlayerRightSensor" && e.VisitorShape.Body == myBody.Body)
            _rightContacts.Add(ContactKey(e.SensorShape, e.VisitorShape));
    }

    private void ContactEndSensorTouch(SensorEndTouchEvent e)
    {
        RigidBody2D myBody = this.GetComponent<RigidBody2D>()!;

        bool isMyLeftSensor = (e.SensorShape.UserData?.ToString() == "PlayerLeftSensor" && e.SensorShape.Body == myBody.Body) ||
                              (e.VisitorShape.UserData?.ToString() == "PlayerLeftSensor" && e.VisitorShape.Body == myBody.Body);

        bool isMyRightSensor = (e.SensorShape.UserData?.ToString() == "PlayerRightSensor" && e.SensorShape.Body == myBody.Body) ||
                               (e.VisitorShape.UserData?.ToString() == "PlayerRightSensor" && e.VisitorShape.Body == myBody.Body);

        if (isMyLeftSensor || isMyRightSensor)
        {
            ulong key = ContactKey(e.SensorShape, e.VisitorShape);
            _leftContacts.Remove(key);
            _rightContacts.Remove(key);
        }
    }

    private void ContactBeginTouch(ContactBeginTouchEvent e)
    {
        if (IsGroundContact(e))
        {
            ulong key = ContactKey(e.ShapeA, e.ShapeB);
            if (_groundContacts.Add(key))
                IsPlayerOnGround = _groundContacts.Count;
        }

        if ((e.ShapeA.UserData?.ToString() == "flag") ||
             e.ShapeB.UserData?.ToString() == "flag")
        {
            if (this.Scene is CustomLevelScene customLevelScene && customLevelScene.IsPlayingFromEditor)
            {
                customLevelScene.ResetEditorPlayerToOrigin();
                return;
            }

            ((LevelScene)this.Scene).WonLevel = true;

            if (IsLocalPlayer && !_hasCompletedLevel)
            {
                _hasCompletedLevel = true;
                OnLevelComplete();
            }
        }

        if (e.ShapeA.UserData is Portal entity1)
        {
            this.LocalTransform.Translation = new Vector3(entity1.TeleportPos, 0);
            SceneManager.ActiveCam2D!.Position = entity1.TeleportPos;
        }

        if (e.ShapeB.UserData is Portal entity2)
        {
            this.LocalTransform.Translation = new Vector3(entity2.TeleportPos, 0);
            SceneManager.ActiveCam2D!.Position = entity2.TeleportPos;
        }
    }

    private void OnLevelComplete()
    {
        string nextLevel = DetermineNextLevel();
        Bliss.CSharp.Logging.Logger.Info($"[PLAYER] Level completed! Moving all players to: {nextLevel}");
        NetworkManager.NotifyLevelComplete(nextLevel);
    }

    private string DetermineNextLevel()
    {
        if (this.Scene is CustomLevelScene customLevelScene && !string.IsNullOrWhiteSpace(customLevelScene.NextLevelName))
        {
            return customLevelScene.NextLevelName;
        }
        
        return "Level 1";
    }

    private void ContactEndTouch(ContactEndTouchEvent e)
    {
        ulong key = ContactKey(e.ShapeA, e.ShapeB);
        if (_groundContacts.Remove(key))
            IsPlayerOnGround = _groundContacts.Count;
    }

    private void LandFromJump()
    {
        this._isJumping = false;

        if (this.PoseType == PlayerPoseType.JumpLeft)
            this.PoseType = PlayerPoseType.LeftIdle;
        else if (this.PoseType == PlayerPoseType.JumpRight)
            this.PoseType = PlayerPoseType.RightIdle;
    }

    private void UpdateSpriteAnimation(double delta)
    {
        AtlasAnimation animation = this.GetAnimationByPoseType(this.PoseType);
        if (!ReferenceEquals(animation, this._activeAnimation) || this.PoseType != this._activeAnimationPoseType)
        {
            this._activeAnimation = animation;
            this._activeAnimationPoseType = this.PoseType;
            this._timer = 0F;
            this._frame = 0;
        }

        bool shouldLoop = !this.IsJumpPose(this.PoseType);
        this._timer += (float)delta;

        if (this._activeAnimation.FrameCount <= 0)
            return;

        while (true)
        {
            float frameDelay = Math.Max(1, this._activeAnimation.GetFrameDelay(this._frame)) / 1000F;
            if (this._timer < frameDelay)
                break;

            if (!shouldLoop && this._frame >= this._activeAnimation.FrameCount - 1)
            {
                this._timer = 0F;
                break;
            }

            this._timer -= frameDelay;
            this._frame++;

            if (this._frame >= this._activeAnimation.FrameCount)
                this._frame = 0;
        }

        Rectangle frameRegion = this._activeAnimation.GetFrameRegion(this._frame);
        this._sprite.SourceRect = frameRegion;
        this._sprite.Size = new Vector2(frameRegion.Width, frameRegion.Height);
    }

    private AtlasAnimation GetAnimationByPoseType(PlayerPoseType poseType)
    {
        switch (poseType)
        {
            case PlayerPoseType.LeftIdle:
                return AtlasRegistry.PlayerIdleLeftRegion;
            case PlayerPoseType.RightIdle:
                return AtlasRegistry.PlayerIdleRightRegion;
            case PlayerPoseType.LeftWalk:
                return AtlasRegistry.PlayerWalkLeftRegion;
            case PlayerPoseType.RightWalk:
                return AtlasRegistry.PlayerWalkRightRegion;
            case PlayerPoseType.JumpLeft:
                return AtlasRegistry.PlayerJumpLeftRegion;
            case PlayerPoseType.JumpRight:
                return AtlasRegistry.PlayerJumpRightRegion;
            default:
                return AtlasRegistry.PlayerIdleRightRegion;
        }
    }

    private bool IsJumpPose(PlayerPoseType poseType)
    {
        return poseType == PlayerPoseType.JumpLeft || poseType == PlayerPoseType.JumpRight;
    }

    private ulong ContactKey(Shape a, Shape b)
    {
        unchecked
        {
            ulong ha = (ulong)a.GetHashCode();
            ulong hb = (ulong)b.GetHashCode();
            return ha < hb ? (ha << 32) ^ hb : (hb << 32) ^ ha;
        }
    }

    private bool IsGroundContact(ContactBeginTouchEvent e)
    {
        bool footIsA = e.ShapeA.UserData?.ToString() == "Player";
        bool footIsB = e.ShapeB.UserData?.ToString() == "Player";
        if (!footIsA && !footIsB) return false;

        var n = e.Manifold.Normal;
        if (footIsA) n = -n;

        var sim = (Simulation2D)this.Scene.Simulation;
        var g = Vector2.Normalize(sim.World.Gravity);
        if (g.LengthSquared() < 1e-6f) g = new Vector2(0, 1);

        return Vector2.Dot(n, -g) > 0.6f;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && IsLocalPlayer)
        {
            ((Simulation2D)this.Scene.Simulation).ContactBeginTouch -= this.ContactBeginTouch;
            ((Simulation2D)this.Scene.Simulation).ContactEndTouch -= this.ContactEndTouch;
            ((Simulation2D)this.Scene.Simulation).SensorBeginTouch -= this.ContactBeginSensorTouch;
            ((Simulation2D)this.Scene.Simulation).SensorEndTouch -= this.ContactEndSensorTouch;
        }
    }
}
