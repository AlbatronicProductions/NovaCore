using NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftFlightOptions(string Assets,string Catalog,string SourceHash,string DocumentDigest,string CompiledDigest,string CatalogDigest)
{
    internal static bool TryParse(string[] args,out CraftFlightOptions? options,out string? error)
    {
        options=null;error=null;var supplied=args.Where(a=>a.StartsWith("--craft-",StringComparison.Ordinal)).ToArray();
        if(supplied.Length==0)return true;
        var expected=new[]{"--craft-assets","--craft-catalog","--craft-source-sha","--craft-document","--craft-compiled","--craft-catalog-sha"};
        var values=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var arg in supplied)
        {
            var split=arg.IndexOf('=');var key=split<0?arg:arg[..split];var value=split<0?"":arg[(split+1)..];
            if((key!="--craft-stdin"&&!expected.Contains(key,StringComparer.Ordinal))||!values.TryAdd(key,value))
            {error="Unknown or repeated craft launch option.";return false;}
        }
        if(!values.TryGetValue("--craft-stdin",out var stdin)||stdin!=""||expected.Any(k=>!values.TryGetValue(k,out var v)||string.IsNullOrWhiteSpace(v))||
            expected.Skip(2).Any(k=>values[k].Length!=64||!values[k].All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {error="Craft launch requires a complete pinned editor snapshot.";return false;}
        options=new(values[expected[0]],values[expected[1]],values[expected[2]],values[expected[3]],values[expected[4]],values[expected[5]]);return true;
    }
    internal CompiledCraft Load(Stream source)
    {
        var bytes=CraftLaunchAdmission.Read(source);
        var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Catalog));
        return CraftLaunchAdmission.Prepare(catalog,bytes,Assets,SourceHash,DocumentDigest,CompiledDigest,CatalogDigest);
    }
}
