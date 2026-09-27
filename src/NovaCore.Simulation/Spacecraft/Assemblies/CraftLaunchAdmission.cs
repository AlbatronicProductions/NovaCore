using System.Security.Cryptography;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold process handoff validation; owns neither design nor live state.</summary>
internal static class CraftLaunchAdmission
{
    internal static byte[] Read(Stream stream)
    {
        using var bytes=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=stream.Read(buffer,0,Math.Min(buffer.Length,CompiledConstructionDesign.MaximumDocumentBytes+1-checked((int)bytes.Length))))>0)
        {
            if(bytes.Length+count>CompiledConstructionDesign.MaximumDocumentBytes)throw new InvalidDataException("Craft launch input exceeds the document bound.");
            bytes.Write(buffer,0,count);
        }
        return bytes.ToArray();
    }
    internal static string SourceHash(ReadOnlySpan<byte> bytes)=>Convert.ToHexStringLower(SHA256.HashData(bytes));
    internal static CompiledCraft Prepare(AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes,string assetRoot,
        string sourceHash,string documentDigest,string compiledDigest,string catalogDigest)
    {
        if(SourceHash(bytes)!=sourceHash||catalog.Digest!=catalogDigest)throw new InvalidDataException("Craft launch source/catalog identity changed.");
        var design=CompiledConstructionDesign.LoadCraft(catalog,bytes);
        if(design.Digest!=documentDigest)throw new InvalidDataException("Craft launch document identity changed.");
        var craft=CraftCompiler.Compile(catalog,design.Data,assetRoot);
        if(craft.Digest!=compiledDigest)throw new InvalidDataException("Craft launch compiled dependency identity changed.");
        if(!craft.Function||!craft.AdmissionDiagnostics.IsEmpty)throw new InvalidDataException(RefusalReason(craft));
        return craft;
    }
    // Describe the actual failed predicates. This is not a second admission policy.
    internal static string RefusalReason(CompiledCraft craft)
    {
        var problems=craft.Diagnostics.Concat(craft.AdmissionDiagnostics).ToArray();
        var reasons=problems.GroupBy(d=>d.Code,StringComparer.Ordinal).Select(group=>group.Key switch {
            "FUEL_PATH"=>"Propellant is unavailable to one or more engines or attitude jets. Fill and enable connected tanks; check fuel connections if the problem remains.",
            "POWER_PATH"=>"Electrical power is unavailable to one or more required systems. Charge connected batteries and enable electrical systems.",
            _=>string.Join(" ",group.Select(d=>d.Message).Distinct(StringComparer.Ordinal))
        }).Where(reason=>reason is not null);
        return "Launch refused. "+string.Join("\n\n",reasons);
    }
}
