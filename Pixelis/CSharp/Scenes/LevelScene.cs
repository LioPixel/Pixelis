﻿using System.Numerics;
using Bliss.CSharp.Camera.Dim2;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Box2D;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Physics.Dim2;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Sparkle.CSharp.Scenes;
using Veldrid;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Scenes;

public abstract class LevelScene : Scene
{
    
    public Texture2D? Background;
    public bool WonLevel;
    
    private Font _nameFont;
    
    // Track if we're in a network game
    private bool _isNetworkGame = false;
    
    protected LevelScene(string name) : base(name, SceneType.Scene2D, null, () => new Simulation2D(new PhysicsSettings2D()
    {
        WorldDef = new WorldDef() 
        {
            Gravity = new Vector2(0, 9.81F),
        }
    })) { }
    

    protected override void Init()
    {
        base.Init();
        
        // Check if we're in a network game
        _isNetworkGame = NetworkManager.Client != null && NetworkManager.Client.IsConnected;
        
        // Load font for player name
        _nameFont = ContentRegistry.Fontoe;
        
        // CAMERA
        Rectangle size = new Rectangle(0, 0, GlobalGraphicsAssets.Window.GetWidth(), GlobalGraphicsAssets.Window.GetHeight());
        Camera2D camera2D = new Camera2D(Vector2.Zero, Vector2.Zero, size, CameraFollowMode.FollowTargetSmooth, zoom: 7);
        this.AddEntity(camera2D);

        // PLAYER
        //Player player = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) });
        //this.AddEntity(player);
    }
    
    protected override void Update(double delta)
    {
        base.Update(delta);
        
        if (SceneManager.ActiveScene != null)
        {
            if (GuiManager.ActiveGui == null)
            {
                if (Input.IsKeyPressed(KeyboardKey.Escape))
                {
                    GuiManager.SetGui(new PauseMenuGui());
                }
            }
        }
        
        
        Camera2D? cam2D = SceneManager.ActiveCam2D;

        if (cam2D == null)
        {
            return;
        }

        foreach (Entity entity in this.GetEntities())
        {
            if (entity is Player player)
            {
                if (player.IsLocalPlayer)
                {
                    Vector3 playerPos = player.LocalTransform.Translation;
                    cam2D.Target = new Vector2(playerPos.X, playerPos.Y);   
                }
            }
        }
        
        // Level won - BUT only handle it if we're NOT in a network game
        // In network games, the server will handle the transition
        if (this.WonLevel && !_isNetworkGame)
        {
            this.OnLevelWon();
        }
    }

    protected override void AfterUpdate(double delta)
    {
        base.AfterUpdate(delta);
    }
    
    

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        // Background
        if (this.Background != null)
        {
            IWindow window = GlobalGraphicsAssets.Window;
            Vector2 backgroundSize = new Vector2((float) window.GetWidth() / this.Background.Width, (float) window.GetHeight() / this.Background.Height);
        
            context.SpriteBatch.Begin(context.CommandList, framebuffer.OutputDescription);
            context.SpriteBatch.DrawTexture(this.Background, Vector2.Zero, scale: backgroundSize);
            context.SpriteBatch.End();
        }
        
        base.Draw(context, framebuffer);
        
        // Draw player names
    }
    
    protected abstract void OnLevelWon();
    
    protected void CreatePlatform(int blockPosX, int blockPosY, int length, float layerDepth = 0.5F)
    {
        for (int i = 0; i < length; i++)
        {
            Entity element = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16 + (16 * i), blockPosY * 16, 0) });
            element.AddComponent(new Sprite(ContentRegistry.Sprite, Vector2.Zero, layerDepth: layerDepth));
            element.AddComponent(new RigidBody2D(new BodyDefinition() { Type = BodyType.Static }, new PolygonShape2D(Polygon.MakeBox(8, 8), new ShapeDef()
            {
                EnableContactEvents = true,
                EnableSensorEvents = true
            })));
            this.AddEntity(element);
        }
    }

    protected void CreateMovingPlatform(int blockPosX, int blockPosY, int length, int targetBlockPosX, int targetBlockPosY, float speed, float layerDepth = 0.5F) {
        for (int i = 0; i < length; i++) {
            MovingBlock movingBlock = new MovingBlock(blockPosX + i, blockPosY, targetBlockPosX + i, targetBlockPosY, speed, layerDepth);
            this.AddEntity(movingBlock);
        }
    }

    protected void CreateStair(int blockPosX, int blockPosY, int length, StairType stairType, float layerDepth = 0.5F)
    {
        for (int i = 0; i < length; i++)
        {
            
            Vector2 position;

            if (stairType == StairType.Up)
            {
                position = new Vector2(blockPosX * 16 + (16 * i), blockPosY * 16 - (16 * i));
            }
            else
            {
                position = new Vector2(blockPosX * 16 + (16 * i), blockPosY * 16 + (16 * i));
            }
            
            Entity element = new Entity(new Transform() { Translation = new Vector3(position, 0) });
            element.AddComponent(new Sprite(ContentRegistry.Sprite, Vector2.Zero, layerDepth: layerDepth));
            element.AddComponent(new RigidBody2D(new BodyDefinition() { Type = BodyType.Static }, new PolygonShape2D(Polygon.MakeBox(8, 8), new ShapeDef()
            {
                EnableContactEvents = true,
                EnableSensorEvents = true
            })));
            this.AddEntity(element);
        }
    }

    protected void CreateWinFlag(int blockPosX, int blockPosY, float layerDepth = 0.5F)
    {
        WinFlag winFlag = new WinFlag(new Transform() { Translation = new Vector3(blockPosX * 16 + 7, (blockPosY * 16) - 16, 0) }, layerDepth);
        this.AddEntity(winFlag);
    }

    protected void CreateTree(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 - 20.5F, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.TreeBig, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }

    protected void CreateTreeDead(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 - 11, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.TreeBigDead, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateFlowerOrange(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 - 8, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.PlantFlower, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateBush(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 + 1, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.Bush, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateGrass(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 + 1, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.Bush, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreatePlantSunFlower(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16 - 8, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.PlantSunFlower, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateBushDead(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.BushDead, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateRockWithGrass(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.RockGrass, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreatePlantFlowerRed(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.PlantFlowerRed, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }
    
    protected void CreateOakLog(int blockPosX, int blockPosY, float layerDepth = 0.4F)
    {
        Entity entity = new Entity(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16, 0) });
        entity.AddComponent(new Sprite(ContentRegistry.OakLog, Vector2.Zero, layerDepth: layerDepth));
        this.AddEntity(entity);
    }

    protected void CreatePortal(int blockPosX, int blockPosY, int teleportPosX, int teleportPosY, Color? color = null, float layerDepth = 0.5F)
    {
        Portal portal = new Portal(new Transform() { Translation = new Vector3(blockPosX * 16, blockPosY * 16, 0) }, new Vector2(teleportPosX * 16, teleportPosY * 16), color, layerDepth);
        this.AddEntity(portal);
    }
    
    protected enum StairType
    {
        Up,
        Down
    }
}
