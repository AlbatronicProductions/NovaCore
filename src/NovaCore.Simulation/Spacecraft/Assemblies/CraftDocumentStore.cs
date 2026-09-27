using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftRecoveryData(string Schema,byte[]? Draft,string? SavedDigest);

/// <summary>Cold bounded document IO; no editor or simulation publication authority.</summary>
internal static class CraftDocumentStore
{
    internal const string RecoverySchema="novacore.craft-recovery/1";
    // JSON base64 expansion of the maximum draft plus bounded schema/key/hash overhead.
    internal const int MaximumRecoveryBytes=4*((CompiledConstructionDesign.MaximumDocumentBytes+2)/3)+256;
    internal static byte[] Read(string path)=>ReadBounded(path,CompiledConstructionDesign.MaximumDocumentBytes);
    private static byte[] ReadBounded(string path,int maximum)
    {
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        Require(file.Length<=maximum,"Bounded file size exceeded.");
        using var data=new MemoryStream();var buffer=new byte[8192];int read;
        while((read=file.Read(buffer,0,Math.Min(buffer.Length,maximum+1-checked((int)data.Length))))>0){
            Require(data.Length+read<=maximum,"Bounded file size exceeded during read.");data.Write(buffer,0,read);
        }
        return data.ToArray();
    }
    internal static void Save(string path,AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var canonical=CompiledConstructionDesign.LoadCraft(catalog,bytes).Save();
        WriteAtomic(path,canonical,staged=>{_=CompiledConstructionDesign.LoadCraft(catalog,Read(staged));});
    }
    internal static (ConstructionEditorDocument? Document,string? SavedDigest) ReadRecovery(string path,AssemblyDefinitionCatalog catalog)
        =>ValidateRecovery(catalog,AssemblyJson.Read<CraftRecoveryData>(ReadBounded(path,MaximumRecoveryBytes),MaximumRecoveryBytes));
    private static (ConstructionEditorDocument? Document,string? SavedDigest) ValidateRecovery(AssemblyDefinitionCatalog catalog,CraftRecoveryData recovery)
    {
        Require(recovery is not null&&recovery.Schema==RecoverySchema&&(recovery.SavedDigest is null||Hash(recovery.SavedDigest)),"Invalid recovery schema/saved identity.");
        Require(catalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema,"Recovery requires Part Standard catalog.");
        var document=recovery.Draft is null?null:CompiledConstructionDesign.LoadCraft(catalog,recovery.Draft);
        return(document is null?null:ConstructionEditorDocument.Compile(catalog,document.Data),recovery.SavedDigest);
    }
    internal static void SaveRecovery(string path,AssemblyDefinitionCatalog catalog,byte[]? draft,string? savedDigest)
    {
        var envelope=new CraftRecoveryData(RecoverySchema,draft,savedDigest);_=ValidateRecovery(catalog,envelope);
        var bytes=AssemblyJson.Write(envelope);Require(bytes.Length<=MaximumRecoveryBytes,"Bounded recovery size exceeded.");
        WriteAtomic(path,bytes,staged=>{_=ReadRecovery(staged,catalog);});
    }
    private static void WriteAtomic(string path,byte[] bytes,Action<string> validate)
    {
        var destination=Path.GetFullPath(path);var directory=Path.GetDirectoryName(destination)!;
        Require(Directory.Exists(directory),"Choose an existing save directory.");
        var temporary=Path.Combine(directory,"."+Path.GetFileName(destination)+"."+Guid.NewGuid().ToString("N")+".tmp");
        var created=false;
        try{
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                created=true;stream.Write(bytes);stream.Flush(flushToDisk:true);
            }
            validate(temporary);
            // Same-directory rename publishes all bytes at once. This does not promise
            // durability of directory metadata under sudden power loss.
            File.Move(temporary,destination,overwrite:true);created=false;
        }
        finally{if(created)File.Delete(temporary);}
    }
}
