using Bliss.CSharp.Interact.Keyboards;
using Sparkle.CSharp;

namespace Pixelis.CSharp.Controls;

public static class KeyBindings
{
    private const string MoveLeftKey = "KeybindMoveLeft";
    private const string MoveRightKey = "KeybindMoveRight";
    private const string JumpKey = "KeybindJump";

    public static KeyboardKey GetMoveLeft()
    {
        return GetOrDefault(MoveLeftKey, KeyboardKey.A);
    }

    public static KeyboardKey GetMoveRight()
    {
        return GetOrDefault(MoveRightKey, KeyboardKey.D);
    }

    public static KeyboardKey GetJump()
    {
        return GetOrDefault(JumpKey, KeyboardKey.Space);
    }

    public static void SetMoveLeft(KeyboardKey key)
    {
        SetKey(MoveLeftKey, key);
    }

    public static void SetMoveRight(KeyboardKey key)
    {
        SetKey(MoveRightKey, key);
    }

    public static void SetJump(KeyboardKey key)
    {
        SetKey(JumpKey, key);
    }

    private static KeyboardKey GetOrDefault(string configKey, KeyboardKey fallback)
    {
        PixelisGame game = (PixelisGame)Game.Instance!;
        string? raw = game.OptionsConfig.GetValue<string>(configKey);

        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw, out KeyboardKey parsed))
        {
            return parsed;
        }

        game.OptionsConfig.SetValue(configKey, fallback.ToString());
        return fallback;
    }

    private static void SetKey(string configKey, KeyboardKey key)
    {
        PixelisGame game = (PixelisGame)Game.Instance!;
        game.OptionsConfig.SetValue(configKey, key.ToString());
    }
}
