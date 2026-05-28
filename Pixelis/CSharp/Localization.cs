using System.Text.Json;
using Sparkle.CSharp;

namespace Pixelis.CSharp;

public static class Localization
{
    public const string English = "en";
    public const string German = "de";

    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new();

    public static string CurrentLanguage
    {
        get
        {
            if (Game.Instance is PixelisGame pixelisGame)
            {
                try
                {
                    string language = pixelisGame.OptionsConfig.GetValue<string>("Language");
                    return language == German ? German : English;
                }
                catch
                {
                    return English;
                }
            }

            return English;
        }
    }

    public static string CurrentLanguageName => T(CurrentLanguage == German ? "language.german" : "language.english");

    public static void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == German ? English : German);
    }

    public static void SetLanguage(string language)
    {
        if (Game.Instance is PixelisGame pixelisGame)
        {
            try
            {
                pixelisGame.OptionsConfig.SetValue("Language", language == German ? German : English);
            }
            catch
            {
                // The config builder should create the key; if an old config cannot be updated, keep the fallback language.
            }
        }
    }

    public static string T(string key)
    {
        Dictionary<string, string> currentLanguageTexts = GetLanguage(CurrentLanguage);
        if (currentLanguageTexts.TryGetValue(key, out string? text))
        {
            return text;
        }

        Dictionary<string, string> englishTexts = GetLanguage(English);
        return englishTexts.TryGetValue(key, out string? fallbackText) ? fallbackText : key;
    }

    public static string F(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    private static Dictionary<string, string> GetLanguage(string language)
    {
        language = language == German ? German : English;

        if (Languages.TryGetValue(language, out Dictionary<string, string>? texts))
        {
            return texts;
        }

        texts = LoadLanguage(language);
        Languages[language] = texts;
        return texts;
    }

    private static Dictionary<string, string> LoadLanguage(string language)
    {
        string path = FindLanguagePath(language);

        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string FindLanguagePath(string language)
    {
        string fileName = $"{language}.json";
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "content", "localization", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "content", "localization", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Pixelis", "content", "localization", fileName)
        ];

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
