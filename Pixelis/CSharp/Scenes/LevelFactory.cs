using Pixelis.CSharp.Levels;
using Sparkle.CSharp.Scenes;

namespace Pixelis.CSharp.Scenes;

public static class LevelFactory
{

    public static List<string> BuiltInLevelNames
    {
        get
        {
            List<string> names = new List<string>();
            try
            {
                string dir = CustomLevelStorage.ContentDirectoryPath;
                if (Directory.Exists(dir))
                {
                    foreach (string filePath in Directory.GetFiles(dir, "*.json"))
                    {
                        try
                        {
                            string json = File.ReadAllText(filePath);
                            CustomLevelData? data = System.Text.Json.JsonSerializer.Deserialize<CustomLevelData>(json);
                            string levelName = data?.Name ?? Path.GetFileNameWithoutExtension(filePath);
                            if (!string.IsNullOrWhiteSpace(levelName) && !names.Contains(levelName, StringComparer.OrdinalIgnoreCase))
                            {
                                names.Add(levelName.Trim());
                            }
                        }
                        catch
                        {
                            // ignore malformed files
                        }
                    }
                }
            }
            catch
            {
                // ignore IO errors
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }

    public static bool IsBuiltInLevelName(string name)
    {
        return BuiltInLevelNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public static List<string> GetMenuLevelNames()
    {
        List<string> names = new List<string>(BuiltInLevelNames);

        foreach (string custom in CustomLevelStorage.GetCustomLevelNames())
        {
            if (!names.Contains(custom, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(custom);
            }
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public static Scene? CreateByName(string levelName)
    {
        return CreateCustomLevel(levelName);
    }

    private static Scene? CreateCustomLevel(string levelName)
    {
        CustomLevelData? data = CustomLevelStorage.LoadByName(levelName);
        return data == null ? null : new CustomLevelScene(data, false);
    }
}
