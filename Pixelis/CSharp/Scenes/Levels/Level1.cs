using System.Numerics;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Pixelis.CSharp.Entities;
using Pixelis.CSharp.GUIs.Loading;
using Sparkle.CSharp.Scenes;
using Sparkle.CSharp.Utils.Async;
using Transform = Bliss.CSharp.Transformations.Transform;

namespace Pixelis.CSharp.Scenes.Levels;

public class Level1 : LevelScene
{

    public Level1() : base("Level 1") {}

    protected override void Init()
    {
        base.Init();
        this.Background = ContentRegistry.Background;

        this.CreatePlatform(0, 0, 4);
        this.CreatePlantSunFlower(2, -1);
        this.CreatePlatform(6, -1, 2);
        this.CreateRockWithGrass(7, -2);
        this.CreateStair(10, -1, 4, StairType.Up);
        this.CreateBushDead(11, -3);
        this.CreatePlatform(13, -4, 6);
        this.CreateOakLog(15, -5);
        this.CreateWinFlag(18, -5);
    }

    protected override void Update(double delta)
    {
        base.Update(delta);
        
    }

    protected override void OnLevelWon()
    {
        // Only transition locally if we're NOT in a network game
        // In network games, the server handles the transition
        if (NetworkManager.Client == null || !NetworkManager.Client.IsConnected)
        {
            AsyncOperation operation10 = SceneManager.LoadSceneAsync(new Level2(), new ProgressBarLoadingGui("Loading"));
            operation10.Completed += success =>
            {
                Player player = new Player(new Transform() { Translation = new Vector3(0, -16 * 2, 0) });
                SceneManager.ActiveScene?.AddEntity(player);
            };
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Only cleanup network if we're actually quitting, not during level transitions
            if (!NetworkManager.IsLevelTransition)
            {
                NetworkManager.Cleanup();
            }

            base.Dispose(disposing);
        }
    }
}