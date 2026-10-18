using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ReNative3D36S;

public static class LanguageService
{
    private static Dictionary<string,string> data = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ReNative3D36S","language.txt");
    public static string Current { get; private set; } = "en";
    public static void Initialize()
    {
        string setting = "auto";
        try { if (File.Exists(ConfigFile)) setting = File.ReadAllText(ConfigFile).Trim(); } catch { }
        Set(setting, false);
    }
    public static void Set(string selection, bool save = true)
    {
        var os = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        var lang = selection == "auto" ? os : selection;
        Current = new[] { "pt", "en", "es", "ru" }.Contains(lang) ? lang : "en";
        var asm = typeof(LanguageService).Assembly;
        var resource = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith($"Locales.{Current}.json", StringComparison.OrdinalIgnoreCase));
        if (resource is not null)
        {
            using var stream = asm.GetManifestResourceStream(resource)!;
            using var doc = JsonDocument.Parse(stream);
            data = doc.RootElement.EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetString()??x.Name,StringComparer.OrdinalIgnoreCase);
        }
        if (save)
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!); File.WriteAllText(ConfigFile, selection); } catch { }
        }
    }
    public static string Text(string key) => data.TryGetValue(key,out var value) ? value : key;
    public static string Selection
    {
        get { try { return File.Exists(ConfigFile) ? File.ReadAllText(ConfigFile).Trim() : "auto"; } catch { return "auto"; } }
    }
}
