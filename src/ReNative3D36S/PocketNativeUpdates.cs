using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReNative3D36S;

internal sealed record NativeRelease(string Version, string DownloadUrl, DateTimeOffset PublishedAt);

internal sealed class PocketNativeUpdates
{
    private const string Api = "https://api.github.com/repos/whovian64/pocket-native/releases/latest";
    private readonly string kit;
    private readonly Action<string> log;
    private readonly HttpClient http;
    private NativeRelease? remembered;
    private DateTimeOffset lastCheck = DateTimeOffset.MinValue;

    public PocketNativeUpdates(string kit, Action<string> log)
    {
        this.kit = kit;
        this.log = log;
        http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ReNative3D36S/0.2.0");
    }

    public string InstalledVersion
    {
        get
        {
            try
            {
                string marker = Path.Combine(kit,".renative-version");
                if (File.Exists(marker)) return File.ReadAllText(marker).Trim();
                var cargo = Path.Combine(kit,"Cargo.toml");
                if (!File.Exists(cargo)) return "not installed";
                var m=Regex.Match(File.ReadAllText(cargo), @"(?m)^version\s*=\s*""(?<v>[0-9]+\.[0-9]+\.[0-9]+)""");
                return m.Success ? m.Groups["v"].Value : "unknown";
            }
            catch { return "unknown"; }
        }
    }

    public async Task<NativeRelease?> GetLatestAsync(CancellationToken token, bool refresh = false)
    {
        if (!refresh && remembered is not null && DateTimeOffset.UtcNow-lastCheck < TimeSpan.FromMinutes(10)) return remembered;
        try
        {
            using var response=await http.GetAsync(Api,token);
            response.EnsureSuccessStatusCode();
            await using var stream=await response.Content.ReadAsStreamAsync(token);
            using var json=await JsonDocument.ParseAsync(stream,cancellationToken:token);
            var root=json.RootElement;
            var tag=root.GetProperty("tag_name").GetString() ?? throw new InvalidDataException("GitHub release tag missing.");
            if (!Regex.IsMatch(tag,@"^v?\d+\.\d+\.\d+$")) throw new InvalidDataException("Unrecognized upstream release tag.");
            var url=root.GetProperty("zipball_url").GetString() ?? throw new InvalidDataException("Release ZIP missing.");
            if (!Uri.TryCreate(url,UriKind.Absolute,out var link) || (link.Host!="api.github.com" && link.Host!="github.com"))
                throw new InvalidDataException("Unexpected release download location.");
            remembered = new NativeRelease(tag.TrimStart('v'),url,root.TryGetProperty("published_at",out var pub)?pub.GetDateTimeOffset():DateTimeOffset.MinValue);
            lastCheck=DateTimeOffset.UtcNow;
            return remembered;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) { log("Release check unavailable: "+ex.Message); return null; }
    }
    public bool NeedsUpdate(NativeRelease release)
    {
        if (!Version.TryParse(InstalledVersion, out var installed)) return true;
        return Version.TryParse(release.Version, out var latest) && latest > installed;
    }
    public async Task InstallAsync(NativeRelease release, CancellationToken token)
    {
        var root=Path.GetDirectoryName(kit)!;
        Directory.CreateDirectory(root);
        var temp=Path.Combine(root,"pn-download-"+Guid.NewGuid().ToString("N"));
        var zip=Path.Combine(root,"pn-download-"+Guid.NewGuid().ToString("N")+".zip");
        var previous=kit+".previous";
        try
        {
            using var response=await http.GetAsync(release.DownloadUrl,HttpCompletionOption.ResponseHeadersRead,token);
            response.EnsureSuccessStatusCode();
            await using(var input=await response.Content.ReadAsStreamAsync(token))
            await using(var output=File.Create(zip)) await input.CopyToAsync(output,token);
            ZipFile.ExtractToDirectory(zip,temp);
            var dirs=Directory.GetDirectories(temp);
            if(dirs.Length!=1) throw new InvalidDataException("Release archive folder structure unexpected.");
            var extracted=dirs[0];
            if(!File.Exists(Path.Combine(extracted,"scripts","container","inside.sh")) ||
               !File.Exists(Path.Combine(extracted,"scripts","container","Containerfile")) ||
               !File.Exists(Path.Combine(extracted,"Cargo.toml")))
               throw new InvalidDataException("Release does not contain expected Pocket Native files.");
            await File.WriteAllTextAsync(Path.Combine(extracted,".renative-version"),release.Version,token);
            // Keep last known good source. Transactional directory rename, restore if swap fails.
            if(Directory.Exists(previous)) Directory.Delete(previous,true);
            if(Directory.Exists(kit)) Directory.Move(kit,previous);
            try { Directory.Move(extracted,kit); }
            catch { if(!Directory.Exists(kit) && Directory.Exists(previous)) Directory.Move(previous,kit); throw; }
            log("Pocket Native source switched to stable release v"+release.Version+" (previous source backed up).");
        }
        finally
        {
            try { if(File.Exists(zip)) File.Delete(zip); } catch { }
            try { if(Directory.Exists(temp)) Directory.Delete(temp,true); } catch { }
        }
    }
    public async Task UpdateProfilesAsync(NativeRelease release,CancellationToken token)
    {
        if(!Directory.Exists(kit)) throw new DirectoryNotFoundException("Install Pocket Native engine first.");
        var root=Path.GetDirectoryName(kit)!;
        var zip=Path.Combine(root,"pn-profiles-"+Guid.NewGuid().ToString("N")+".zip");
        var tmp=Path.Combine(root,"pn-profiles-"+Guid.NewGuid().ToString("N"));
        var target=Path.Combine(kit,"titles");
        var backup=Path.Combine(kit,"titles.previous");
        try
        {
            using var res=await http.GetAsync(release.DownloadUrl,HttpCompletionOption.ResponseHeadersRead,token);
            res.EnsureSuccessStatusCode();
            await using(var input=await res.Content.ReadAsStreamAsync(token))
            await using(var output=File.Create(zip)) await input.CopyToAsync(output,token);
            ZipFile.ExtractToDirectory(zip,tmp);
            var rootSource=Directory.GetDirectories(tmp).Single();
            var source=Path.Combine(rootSource,"titles");
            if(!Directory.Exists(source) || !Directory.EnumerateDirectories(source).Any()) throw new InvalidDataException("Release has no title profiles.");
            if(Directory.Exists(backup)) Directory.Delete(backup,true);
            if(Directory.Exists(target)) Directory.Move(target,backup);
            try { Directory.Move(source,target); }
            catch { if(!Directory.Exists(target) && Directory.Exists(backup)) Directory.Move(backup,target); throw; }
            await File.WriteAllTextAsync(Path.Combine(kit,".renative-profiles-version"),release.Version,token);
            log("Title profiles updated to v"+release.Version+". Existing binaries unchanged; overrides may require game rebuild.");
        }
        finally
        {
            try { if(File.Exists(zip)) File.Delete(zip); } catch { }
            try { if(Directory.Exists(tmp)) Directory.Delete(tmp,true); } catch { }
        }
    }
}
