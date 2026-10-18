using System.IO;
using System.Text.Json;

namespace ReNative3D36S;

// Only contains local paths and selected engine. No ROM, AES keys, or credentials.
internal sealed class EnginePreferences
{
    public string Selected { get; set; } = "pocket";
    public string LocalForkDirectory { get; set; } = "";
    public string RecompilerExecutable { get; set; } = "";
    private static string PathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReNative3D36S", "engine-preferences.json");
    internal static EnginePreferences Load()
    {
        try { return JsonSerializer.Deserialize<EnginePreferences>(File.ReadAllText(PathName)) ?? new(); }
        catch (IOException) { return new(); }
        catch (JsonException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
    }
    internal void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            var temp = PathName + ".new";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, PathName, overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal bool LocalForkValid() => Directory.Exists(LocalForkDirectory)
        && File.Exists(Path.Combine(LocalForkDirectory, "Cargo.toml"))
        && File.Exists(Path.Combine(LocalForkDirectory, "scripts", "container", "inside.sh"))
        && File.Exists(Path.Combine(LocalForkDirectory, "scripts", "container", "Containerfile"))
        && File.Exists(Path.Combine(LocalForkDirectory, "tools", "title_id.py"));
    internal bool RecompilerValid() => File.Exists(RecompilerExecutable)
        && (string.Equals(Path.GetFileName(RecompilerExecutable), "3dsrecomp.exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileName(RecompilerExecutable), "recomp3ds.exe", StringComparison.OrdinalIgnoreCase));
}
