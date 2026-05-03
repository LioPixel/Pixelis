using System.Numerics;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Box2D;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs;
using Pixelis.CSharp.GUIs.Loading;
using Pixelis.CSharp.Levels;
using Sparkle.CSharp.Entities;
using Sparkle.CSharp.Entities.Components;
using Sparkle.CSharp.Graphics;
using Sparkle.CSharp.GUI;
using Sparkle.CSharp.Physics.Dim2.Def;
using Sparkle.CSharp.Physics.Dim2.Shapes;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Veldrid;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Scenes;

public class CustomLevelScene : LevelScene
{
    private const int BlockSize = 16;
    private const float DefaultPlaceableDepth = 0.5F;
    private readonly Dictionary<(int X, int Y), PlacedObject> _placedObjects = [];
    private readonly bool _editorMode;
    private readonly string? _originalLevelId;
    private readonly CustomLevelData _levelData;

    private EditorTool _selectedTool = EditorTool.Place;
    private PlaceableType _selectedPlaceable = PlaceableType.Block;
    private float _selectedMovingBlockSpeed = 1F;
    private float _selectedPlaceableDepth = DefaultPlaceableDepth;
    private (int X, int Y)? _pendingMovingBlockStart;
    private (int X, int Y)? _pendingPortalStart;
    private bool _isPlaying;
    private bool _suppressPlacementUntilMouseReleased;

    public CustomLevelScene(CustomLevelData levelData, bool editorMode) : base(levelData.Name)
    {
        _levelData = new CustomLevelData
        {
            Id = levelData.Id,
            Name = levelData.Name,
            NextLevelName = levelData.NextLevelName,
            Blocks = levelData.Blocks
                .Select(block => new CustomLevelBlockData
                {
                    Type = block.Type,
                    X = block.X,
                    Y = block.Y,
                    TargetX = block.TargetX,
                    TargetY = block.TargetY,
                    Speed = block.Speed,
                    Depth = block.Depth
                })
                .ToList()
        };

        _originalLevelId = levelData.Id;
        _editorMode = editorMode;
    }

    public string LevelName => _levelData.Name;
    public bool IsEditorMode => _editorMode;
    public EditorTool SelectedTool => _selectedTool;
    public PlaceableType SelectedPlaceable => _selectedPlaceable;
    public float SelectedMovingBlockSpeed => _selectedMovingBlockSpeed;
    public float SelectedPlaceableDepth => _selectedPlaceableDepth;
    public bool HasPendingMovingBlockStart => _pendingMovingBlockStart.HasValue;
    public bool HasWinFlag => _placedObjects.Values.Any(entry => entry.Type == PlaceableType.WinFlag);
    public string NextLevelName => _levelData.NextLevelName;
    public bool IsSaveDialogOpen => GuiManager.ActiveGui is LevelEditorGui editorGui && editorGui.IsSaveDialogOpen;
    public bool IsPlayingFromEditor => _isPlaying;

    protected override void Init()
    {
        base.Init();
        this.Background = ContentRegistry.Background2;

        foreach (CustomLevelBlockData block in _levelData.Blocks)
        {
            if (TryParsePlaceableType(block.Type, out PlaceableType placeable))
            {
                PlacePlaceable(new CustomLevelBlockData
                {
                    Type = placeable.ToString(),
                    X = block.X,
                    Y = block.Y,
                    TargetX = block.TargetX,
                    TargetY = block.TargetY,
                    Speed = block.Speed,
                    Depth = block.Depth
                }, persist: false);
            }
        }

        if (_editorMode)
        {
            GuiManager.SetGui(new LevelEditorGui(this));
        }
    }

    protected override void Update(double delta)
    {
        base.Update(delta);

        if (!_editorMode || _isPlaying)
        {
            return;
        }

        if (_suppressPlacementUntilMouseReleased)
        {
            if (!Input.IsMouseButtonDown(MouseButton.Left))
            {
                _suppressPlacementUntilMouseReleased = false;
            }

            return;
        }

        UpdateEditorCamera(delta);

        if (IsSaveDialogOpen)
        {
            return;
        }

        if (Input.IsKeyPressed(KeyboardKey.Escape))
        {
            if (_pendingMovingBlockStart.HasValue || _pendingPortalStart.HasValue)
            {
                ClearPendingPlacement();
                return;
            }

            AsyncOperationBackToBrowser();
            return;
        }

        if (Input.IsMouseButtonDown(MouseButton.Left) && !IsMouseOverEditorUi())
        {
            Vector2 worldMousePosition = SceneManager.ActiveCam2D?.GetScreenToWorld(Input.GetMousePosition()) ?? Vector2.Zero;
            (int blockX, int blockY) = GetSnappedBlockCoordinate(worldMousePosition);

            if (_selectedTool == EditorTool.Eraser)
            {
                RemovePlaceable(blockX, blockY);
            }
            else if (_selectedPlaceable == PlaceableType.MovingBlock)
            {
                HandleMovingBlockPlacement(blockX, blockY);
            }
            else if (_selectedPlaceable == PlaceableType.Portal)
            {
                HandlePortalPlacement(blockX, blockY);
            }
            else
            {
                PlacePlaceable(CreatePlaceableData(_selectedPlaceable, blockX, blockY, depth: _selectedPlaceableDepth), persist: true);
            }
        }
    }

    protected override void Draw(GraphicsContext context, Framebuffer framebuffer)
    {
        base.Draw(context, framebuffer);

        if (!_editorMode)
        {
            return;
        }

        Camera2D? camera = SceneManager.ActiveCam2D;
        if (camera == null || IsMouseOverEditorUi())
        {
            return;
        }

        Vector2 worldMousePosition = camera.GetScreenToWorld(Input.GetMousePosition());
        (int blockX, int blockY) = GetSnappedBlockCoordinate(worldMousePosition);
        Vector2 blockPosition = new(blockX * BlockSize, blockY * BlockSize);
        Vector2 outlineTopLeft = blockPosition - new Vector2(BlockSize / 2F, BlockSize / 2F);

        context.PrimitiveBatch.Begin(context.CommandList, framebuffer.OutputDescription, view: camera.GetView());
        context.PrimitiveBatch.DrawEmptyRectangle(
            new RectangleF(outlineTopLeft.X, outlineTopLeft.Y, BlockSize, BlockSize),
            1.5F,
            color: _selectedTool == EditorTool.Eraser ? Color.Red : Color.White);

        if (_pendingMovingBlockStart.HasValue)
        {
            Vector2 startPosition = new(_pendingMovingBlockStart.Value.X * BlockSize, _pendingMovingBlockStart.Value.Y * BlockSize);
            Vector2 startOutlineTopLeft = startPosition - new Vector2(BlockSize / 2F, BlockSize / 2F);

            context.PrimitiveBatch.DrawEmptyRectangle(
                new RectangleF(startOutlineTopLeft.X, startOutlineTopLeft.Y, BlockSize, BlockSize),
                2F,
                color: Color.Yellow);
            context.PrimitiveBatch.DrawLine(
                startPosition,
                blockPosition,
                1.5F,
                color: Color.Yellow);
        }

        if (_pendingPortalStart.HasValue)
        {
            Vector2 startPosition = new(_pendingPortalStart.Value.X * BlockSize, _pendingPortalStart.Value.Y * BlockSize);
            Vector2 startOutlineTopLeft = startPosition - new Vector2(BlockSize / 2F, BlockSize / 2F);

            context.PrimitiveBatch.DrawEmptyRectangle(
                new RectangleF(startOutlineTopLeft.X, startOutlineTopLeft.Y, BlockSize, BlockSize),
                2F,
                color: Color.Cyan);
            context.PrimitiveBatch.DrawLine(
                startPosition,
                blockPosition,
                1.5F,
                color: Color.Cyan);
        }

        context.PrimitiveBatch.End();
    }

    protected override void OnLevelWon()
    {
        if (NetworkManager.Client != null && NetworkManager.Client.IsConnected)
        {
            return;
        }

        Scene? nextScene = LevelFactory.CreateByName(_levelData.NextLevelName);
        if (nextScene == null)
        {
            GuiManager.SetGui(new MenuGui());
            return;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene, new ProgressBarLoadingGui("Loading"));
        operation.Completed += _ =>
        {
            if (SceneManager.ActiveScene is CustomLevelScene customLevelScene)
            {
                customLevelScene.SpawnGameplayPlayerAtOrigin();
                return;
            }

            Player player = new(new Transform { Translation = new Vector3(0, -16 * 2, 0) });
            SceneManager.ActiveScene?.AddEntity(player);
        };
    }

    public void SetTool(EditorTool tool)
    {
        _selectedTool = tool;
        if (tool != EditorTool.Place)
        {
            ClearPendingPlacement();
        }

        RefreshEditorGui();
    }

    public void SetPlaceable(PlaceableType placeable)
    {
        if (_selectedPlaceable != placeable)
        {
            ClearPendingPlacement();
        }

        _selectedPlaceable = placeable;
        _selectedTool = EditorTool.Place;
        RefreshEditorGui();
    }

    public void SetMovingBlockSpeed(float speed)
    {
        _selectedMovingBlockSpeed = speed;
        RefreshEditorGui();
    }

    public void SetPlaceableDepth(float depth)
    {
        _selectedPlaceableDepth = depth;
        RefreshEditorGui();
    }

    public void SetNextLevelName(string nextLevelName)
    {
        _levelData.NextLevelName = nextLevelName.Trim();
    }

    public string GetEditorStatusMessage()
    {
        if (_selectedPlaceable == PlaceableType.MovingBlock)
        {
            return _pendingMovingBlockStart.HasValue
                ? $"Moving Block: choose target, speed {_selectedMovingBlockSpeed:0.0}, depth {_selectedPlaceableDepth:0.0}"
                : $"Moving Block: choose start, speed {_selectedMovingBlockSpeed:0.0}, depth {_selectedPlaceableDepth:0.0}";
        }

        if (_selectedPlaceable == PlaceableType.Portal)
        {
            return _pendingPortalStart.HasValue
                ? $"Portal: choose target, depth {_selectedPlaceableDepth:0.0}"
                : $"Portal: choose start, depth {_selectedPlaceableDepth:0.0}";
        }

        return $"WASD / arrow keys move the camera, depth {_selectedPlaceableDepth:0.0}";
    }

    public void StartEditorPlayMode()
    {
        _isPlaying = true;
        SuppressPlacementUntilMouseRelease();
        SpawnOrResetLocalPlayer(Vector3.Zero);

        SceneManager.ActiveCam2D!.Position = Vector2.Zero;
        SceneManager.ActiveCam2D.Target = Vector2.Zero;
        GuiManager.SetGui(new LevelEditorPlayGui(this));
    }

    public void SpawnGameplayPlayerAtOrigin()
    {
        _isPlaying = false;
        SpawnOrResetLocalPlayer(Vector3.Zero);
        SceneManager.ActiveCam2D!.Position = Vector2.Zero;
        SceneManager.ActiveCam2D.Target = Vector2.Zero;
    }

    public void ResetEditorPlayerToOrigin()
    {
        SpawnOrResetLocalPlayer(Vector3.Zero);
        SceneManager.ActiveCam2D!.Position = Vector2.Zero;
        SceneManager.ActiveCam2D.Target = Vector2.Zero;
    }

    public void ReturnToEditorMode()
    {
        _isPlaying = false;
        SuppressPlacementUntilMouseRelease();

        foreach (Player player in this.GetEntities().OfType<Player>().Where(entity => entity.IsLocalPlayer).ToList())
        {
            this.RemoveEntity(player);
            player.Dispose();
        }

        GuiManager.SetGui(new LevelEditorGui(this));
    }

    public void SuppressPlacementUntilMouseRelease()
    {
        _suppressPlacementUntilMouseReleased = true;
    }

    public void SaveLevel(string requestedName)
    {
        _levelData.Name = requestedName.Trim();
        if (!HasWinFlag)
        {
            _levelData.NextLevelName = string.Empty;
        }

        SyncLevelData();

        CustomLevelStorage.Save(_levelData, _originalLevelId);
    }

    public void BackToBrowser()
    {
        AsyncOperationBackToBrowser();
    }

    private void AsyncOperationBackToBrowser()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(null, new GUIs.Loading.ProgressBarLoadingGui("Loading"));
        operation.Completed += _ => GuiManager.SetGui(new LevelEditorBrowserGui());
    }

    private void UpdateEditorCamera(double delta)
    {
        Camera2D? camera = SceneManager.ActiveCam2D;
        if (camera == null)
        {
            return;
        }

        Vector2 movement = Vector2.Zero;

        if (Input.IsKeyDown(KeyboardKey.A) || Input.IsKeyDown(KeyboardKey.Left))
        {
            movement.X -= 1;
        }

        if (Input.IsKeyDown(KeyboardKey.D) || Input.IsKeyDown(KeyboardKey.Right))
        {
            movement.X += 1;
        }

        if (Input.IsKeyDown(KeyboardKey.W) || Input.IsKeyDown(KeyboardKey.Up))
        {
            movement.Y -= 1;
        }

        if (Input.IsKeyDown(KeyboardKey.S) || Input.IsKeyDown(KeyboardKey.Down))
        {
            movement.Y += 1;
        }

        if (movement == Vector2.Zero)
        {
            return;
        }

        movement = Vector2.Normalize(movement) * 160F * (float)delta;
        camera.Position += movement;
        camera.Target = camera.Position;
    }

    private bool IsMouseOverEditorUi()
    {
        if (IsSaveDialogOpen)
        {
            return true;
        }

        Vector2 mousePosition = Input.GetMousePosition();

        if (GuiManager.ActiveGui is LevelEditorGui editorGui && editorGui.IsPointOverExtendedUi(mousePosition))
        {
            return true;
        }

        float windowWidth = GlobalGraphicsAssets.Window.GetWidth();
        float windowHeight = GlobalGraphicsAssets.Window.GetHeight();
        float uiScale = GuiManager.ActiveGui?.ScaleFactor ?? 1;

        RectangleF topBarArea = new RectangleF(0, 0, windowWidth, 84 * uiScale);
        RectangleF bottomBarArea = new RectangleF(0, windowHeight - (130 * uiScale), windowWidth, 130 * uiScale);

        return Contains(topBarArea, mousePosition)
               || Contains(bottomBarArea, mousePosition);
    }

    private static bool Contains(RectangleF rectangle, Vector2 point)
    {
        return point.X >= rectangle.X
               && point.X <= rectangle.X + rectangle.Width
               && point.Y >= rectangle.Y
               && point.Y <= rectangle.Y + rectangle.Height;
    }

    private static (int X, int Y) GetSnappedBlockCoordinate(Vector2 worldPosition)
    {
        float halfBlock = BlockSize / 2F;

        return (
            (int)MathF.Floor((worldPosition.X + halfBlock) / BlockSize),
            (int)MathF.Floor((worldPosition.Y + halfBlock) / BlockSize)
        );
    }

    public static IReadOnlyList<PlaceableType> GetAvailablePlaceables()
    {
        return
        [
            PlaceableType.Block,
            PlaceableType.MovingBlock,
            PlaceableType.Portal,
            PlaceableType.WinFlag,
            PlaceableType.Tree,
            PlaceableType.TreeDead,
            PlaceableType.FlowerOrange,
            PlaceableType.Bush,
            PlaceableType.PlantSunFlower,
            PlaceableType.BushDead,
            PlaceableType.RockWithGrass,
            PlaceableType.PlantFlowerRed,
            PlaceableType.OakLog,
            PlaceableType.Grass
        ];
    }

    public static string GetPlaceableDisplayName(PlaceableType placeable)
    {
        return placeable switch
        {
            PlaceableType.Block => "Block",
            PlaceableType.MovingBlock => "Moving Block",
            PlaceableType.Portal => "Portal",
            PlaceableType.WinFlag => "Win Flag",
            PlaceableType.Tree => "Tree",
            PlaceableType.TreeDead => "Dead Tree",
            PlaceableType.FlowerOrange => "Orange Flower",
            PlaceableType.Bush => "Bush",
            PlaceableType.PlantSunFlower => "Sunflower",
            PlaceableType.BushDead => "Dead Bush",
            PlaceableType.RockWithGrass => "Rock Grass",
            PlaceableType.PlantFlowerRed => "Red Flower",
            PlaceableType.OakLog => "Oak Log",
            PlaceableType.Grass => "Grass",
            _ => placeable.ToString()
        };
    }

    public static bool TryParsePlaceableType(string? value, out PlaceableType placeable)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            placeable = PlaceableType.Block;
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out placeable);
    }

    public static IReadOnlyList<string> GetLevelTransitionOptions()
    {
        return LevelFactory.GetMenuLevelNames();
    }

    private void HandleMovingBlockPlacement(int blockX, int blockY)
    {
        if (!_pendingMovingBlockStart.HasValue)
        {
            _pendingMovingBlockStart = (blockX, blockY);
            SuppressPlacementUntilMouseRelease();
            RefreshEditorGui();
            return;
        }

        (int X, int Y) start = _pendingMovingBlockStart.Value;
        PlacePlaceable(CreatePlaceableData(
            PlaceableType.MovingBlock,
            start.X,
            start.Y,
            blockX,
            blockY,
            _selectedMovingBlockSpeed,
            _selectedPlaceableDepth), persist: true);
        _pendingMovingBlockStart = null;
        SuppressPlacementUntilMouseRelease();
        RefreshEditorGui();
    }

    private void HandlePortalPlacement(int blockX, int blockY)
    {
        if (!_pendingPortalStart.HasValue)
        {
            _pendingPortalStart = (blockX, blockY);
            SuppressPlacementUntilMouseRelease();
            RefreshEditorGui();
            return;
        }

        (int X, int Y) start = _pendingPortalStart.Value;
        PlacePlaceable(CreatePlaceableData(
            PlaceableType.Portal,
            start.X,
            start.Y,
            blockX,
            blockY,
            depth: _selectedPlaceableDepth), persist: true);
        _pendingPortalStart = null;
        SuppressPlacementUntilMouseRelease();
        RefreshEditorGui();
    }

    private void PlacePlaceable(CustomLevelBlockData blockData, bool persist)
    {
        if (!TryParsePlaceableType(blockData.Type, out PlaceableType placeable))
        {
            return;
        }

        if (placeable == PlaceableType.WinFlag)
        {
            RemoveExistingWinFlagsExcept(blockData.X, blockData.Y);
        }

        if (_placedObjects.TryGetValue((blockData.X, blockData.Y), out PlacedObject? existingObject))
        {
            if (existingObject.Matches(blockData))
            {
                return;
            }

            this.RemoveEntity(existingObject.Entity);
            _placedObjects.Remove((blockData.X, blockData.Y));
        }

        Entity entity = CreatePlaceableEntity(blockData, placeable);
        _placedObjects[(blockData.X, blockData.Y)] = new PlacedObject(blockData, placeable, entity);
        this.AddEntity(entity);

        if (persist)
        {
            SyncLevelData();
        }
    }

    private void RemoveExistingWinFlagsExcept(int blockX, int blockY)
    {
        List<(int X, int Y)> winFlagPositions = _placedObjects
            .Where(entry => entry.Value.Type == PlaceableType.WinFlag
                            && (entry.Key.X != blockX || entry.Key.Y != blockY))
            .Select(entry => entry.Key)
            .ToList();

        foreach ((int X, int Y) winFlagPosition in winFlagPositions)
        {
            if (_placedObjects.TryGetValue(winFlagPosition, out PlacedObject? placedObject))
            {
                _placedObjects.Remove(winFlagPosition);
                this.RemoveEntity(placedObject.Entity);
            }
        }
    }

    private void RemovePlaceable(int blockX, int blockY)
    {
        ClearPendingPlacement();

        if (!_placedObjects.TryGetValue((blockX, blockY), out PlacedObject? placedObject))
        {
            return;
        }

        _placedObjects.Remove((blockX, blockY));
        this.RemoveEntity(placedObject.Entity);
        SyncLevelData();
    }

    private void SyncLevelData()
    {
        _levelData.Blocks = _placedObjects
            .Select(entry => entry.Value.CloneData())
            .OrderBy(block => block.Y)
            .ThenBy(block => block.X)
            .ToList();
    }

    private void RefreshEditorGui()
    {
        if (GuiManager.ActiveGui is LevelEditorGui editorGui)
        {
            editorGui.RefreshEditorState();
        }
    }

    private void ClearPendingMovingBlock()
    {
        if (!_pendingMovingBlockStart.HasValue)
        {
            return;
        }

        _pendingMovingBlockStart = null;
        RefreshEditorGui();
    }

    private void ClearPendingPortal()
    {
        if (!_pendingPortalStart.HasValue)
        {
            return;
        }

        _pendingPortalStart = null;
        RefreshEditorGui();
    }

    private void ClearPendingPlacement()
    {
        bool hadPending = _pendingMovingBlockStart.HasValue || _pendingPortalStart.HasValue;
        _pendingMovingBlockStart = null;
        _pendingPortalStart = null;

        if (hadPending)
        {
            RefreshEditorGui();
        }
    }

    private void SpawnOrResetLocalPlayer(Vector3 spawnPoint)
    {
        Player? player = this.GetEntities().OfType<Player>().FirstOrDefault(entity => entity.IsLocalPlayer);

        if (player == null)
        {
            player = new Player(new Transform
            {
                Translation = spawnPoint
            });
            this.AddEntity(player);
        }
        else
        {
            player.LocalTransform.Translation = spawnPoint;
        }

        player.SetSpawnPoint(spawnPoint);

        if (player.GetComponent<RigidBody2D>() is RigidBody2D body)
        {
            body.LinearVelocity = Vector2.Zero;
            body.Awake = true;
        }
    }

    private static CustomLevelBlockData CreatePlaceableData(
        PlaceableType placeable,
        int blockX,
        int blockY,
        int? targetX = null,
        int? targetY = null,
        float? speed = null,
        float? depth = null)
    {
        return new CustomLevelBlockData
        {
            Type = placeable.ToString(),
            X = blockX,
            Y = blockY,
            TargetX = targetX,
            TargetY = targetY,
            Speed = speed,
            Depth = depth ?? DefaultPlaceableDepth
        };
    }

    private static Entity CreatePlaceableEntity(CustomLevelBlockData blockData, PlaceableType placeable)
    {
        float layerDepth = blockData.Depth ?? DefaultPlaceableDepth;

        return placeable switch
        {
            PlaceableType.Block => CreateBlockEntity(blockData.X, blockData.Y, layerDepth),
            PlaceableType.MovingBlock => new MovingBlock(
                blockData.X,
                blockData.Y,
                blockData.TargetX ?? blockData.X,
                blockData.TargetY ?? blockData.Y,
                blockData.Speed ?? 1F,
                layerDepth),
            PlaceableType.Portal => new Portal(
                new Transform { Translation = new Vector3(blockData.X * BlockSize, blockData.Y * BlockSize, 0) },
                new Vector2((blockData.TargetX ?? blockData.X) * BlockSize, (blockData.TargetY ?? blockData.Y) * BlockSize),
                layerDepth: layerDepth),
            PlaceableType.WinFlag => new WinFlag(new Transform { Translation = new Vector3(blockData.X * BlockSize + 7, blockData.Y * BlockSize - 16, 0) }, layerDepth),
            PlaceableType.Tree => CreateDecorativeEntity(ContentRegistry.TreeBig, blockData.X, blockData.Y, -20.5F, layerDepth),
            PlaceableType.TreeDead => CreateDecorativeEntity(ContentRegistry.TreeBigDead, blockData.X, blockData.Y, -11F, layerDepth),
            PlaceableType.FlowerOrange => CreateDecorativeEntity(ContentRegistry.PlantFlower, blockData.X, blockData.Y, -8F, layerDepth),
            PlaceableType.Bush => CreateDecorativeEntity(ContentRegistry.Bush, blockData.X, blockData.Y, 1F, layerDepth),
            PlaceableType.PlantSunFlower => CreateDecorativeEntity(ContentRegistry.PlantSunFlower, blockData.X, blockData.Y, -8F, layerDepth),
            PlaceableType.BushDead => CreateDecorativeEntity(ContentRegistry.BushDead, blockData.X, blockData.Y, 0F, layerDepth),
            PlaceableType.RockWithGrass => CreateDecorativeEntity(ContentRegistry.RockGrass, blockData.X, blockData.Y, 0F, layerDepth),
            PlaceableType.PlantFlowerRed => CreateDecorativeEntity(ContentRegistry.PlantFlowerRed, blockData.X, blockData.Y, 0F, layerDepth),
            PlaceableType.OakLog => CreateDecorativeEntity(ContentRegistry.OakLog, blockData.X, blockData.Y, 0F, layerDepth),
            PlaceableType.Grass => CreateDecorativeEntity(ContentRegistry.Grass, blockData.X, blockData.Y, 0F, layerDepth),
            _ => CreateBlockEntity(blockData.X, blockData.Y, layerDepth)
        };
    }

    private static Entity CreateDecorativeEntity(Texture2D texture, int blockX, int blockY, float offsetY, float layerDepth)
    {
        Entity entity = new(new Transform
        {
            Translation = new Vector3(blockX * BlockSize, blockY * BlockSize + offsetY, 0)
        });
        entity.AddComponent(new Sprite(texture, Vector2.Zero, layerDepth: layerDepth));
        return entity;
    }

    private static Entity CreateBlockEntity(int blockX, int blockY, float layerDepth)
    {
        Entity entity = new(new Transform
        {
            Translation = new Vector3(blockX * BlockSize, blockY * BlockSize, 0)
        });

        entity.AddComponent(new Sprite(ContentRegistry.Sprite, Vector2.Zero, layerDepth: layerDepth));
        entity.AddComponent(new RigidBody2D(
            new BodyDefinition
            {
                Type = BodyType.Static
            },
            new PolygonShape2D(
                Polygon.MakeBox(8, 8),
                new ShapeDef
                {
                    EnableContactEvents = true,
                    EnableSensorEvents = true
                })));

        return entity;
    }

    public enum EditorTool
    {
        Place,
        Eraser
    }

    public enum PlaceableType
    {
        Block,
        MovingBlock,
        Portal,
        WinFlag,
        Tree,
        TreeDead,
        FlowerOrange,
        Bush,
        PlantSunFlower,
        BushDead,
        RockWithGrass,
        PlantFlowerRed,
        OakLog,
        Grass
    }

    private sealed class PlacedObject
    {
        public PlacedObject(CustomLevelBlockData data, PlaceableType type, Entity entity)
        {
            Data = new CustomLevelBlockData
            {
                Type = data.Type,
                X = data.X,
                Y = data.Y,
                TargetX = data.TargetX,
                TargetY = data.TargetY,
                Speed = data.Speed,
                Depth = data.Depth
            };
            Type = type;
            Entity = entity;
        }

        public CustomLevelBlockData Data { get; }
        public PlaceableType Type { get; }
        public Entity Entity { get; }

        public bool Matches(CustomLevelBlockData other)
        {
            return string.Equals(Data.Type, other.Type, StringComparison.OrdinalIgnoreCase)
                   && Data.X == other.X
                   && Data.Y == other.Y
                   && Data.TargetX == other.TargetX
                   && Data.TargetY == other.TargetY
                   && Math.Abs((Data.Speed ?? 0F) - (other.Speed ?? 0F)) < 0.001F
                   && Math.Abs((Data.Depth ?? DefaultPlaceableDepth) - (other.Depth ?? DefaultPlaceableDepth)) < 0.001F;
        }

        public CustomLevelBlockData CloneData()
        {
            return new CustomLevelBlockData
            {
                Type = Data.Type,
                X = Data.X,
                Y = Data.Y,
                TargetX = Data.TargetX,
                TargetY = Data.TargetY,
                Speed = Data.Speed,
                Depth = Data.Depth
            };
        }
    }
}
