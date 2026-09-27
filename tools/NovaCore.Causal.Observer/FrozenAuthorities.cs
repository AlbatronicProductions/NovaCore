using System.Security.Cryptography;
using System.Text.Json;

internal static class FrozenAuthorities
{
    internal static Dictionary<string,string> Verify(JsonElement gate)
    {
        var identities=gate.GetProperty("identities").EnumerateArray().ToArray();
        if(identities.Length is <4 or >10000)throw new InvalidDataException("Authority manifest capacity");
        var sealedFiles=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var file in identities){
            string path=Path.GetFullPath(file.GetProperty("path").GetString()!);
            string expected=file.GetProperty("sha256").GetString()!;
            using var content=File.OpenRead(path);string actual=Convert.ToHexStringLower(SHA256.HashData(content));
            if(actual!=expected||!sealedFiles.TryAdd(path,expected))throw new InvalidDataException("Authority changed or duplicated: "+path);
        }
        string application=Path.GetFullPath(gate.GetProperty("application").GetString()!);
        string runtime=Path.GetDirectoryName(application)!;
        var runtimeFiles=Directory.EnumerateFiles(runtime).Where(p=>Path.GetExtension(p) is ".dll" or ".exe" or ".json")
            .Concat(Directory.EnumerateFiles(Path.Combine(runtime,"shaders"),"*",SearchOption.AllDirectories)).ToArray();
        foreach(string required in new[]{application,Path.Combine(runtime,"NovaCore.Native.dll"),Path.Combine(runtime,"NovaCore.Graphics.dll"),Path.Combine(runtime,"NovaCore.Triangle.dll")}.Concat(runtimeFiles))
            if(!sealedFiles.ContainsKey(Path.GetFullPath(required)))throw new InvalidDataException("Unsealed runtime authority: "+required);
        if(!runtimeFiles.Any(p=>Path.GetExtension(p)==".spv"))throw new InvalidDataException("Compiled shader authority is absent");
        var roles=new Dictionary<string,string>();
        foreach(string role in new[]{"production","regional","oracle"}){
            var asset=gate.GetProperty("authorityAssets").GetProperty(role);string path=Path.GetFullPath(asset.GetProperty("path").GetString()!);
            if(!sealedFiles.TryGetValue(path,out var sha)||sha!=asset.GetProperty("sha256").GetString())throw new InvalidDataException("Unsealed physical authority: "+role);
            roles.Add(role,path);
        }
        return roles;
    }
    internal static int Inspect(string path){using var file=JsonDocument.Parse(File.ReadAllText(path));var roles=Verify(file.RootElement);
        Console.WriteLine(JsonSerializer.Serialize(new{authoritiesVerified=true,roles=roles.Keys,launchPerformed=false}));return 0;}
}
