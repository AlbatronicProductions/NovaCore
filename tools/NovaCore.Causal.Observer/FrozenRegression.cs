using System.Security.Cryptography;
using System.Text.Json;

internal static class FrozenRegression
{
    internal static int Run(string fixture,string output)
    {
        if(Directory.Exists(output))throw new IOException("Use a fresh regression output directory.");
        if(FrozenSnapshot.Inspect(fixture)!=0)throw new InvalidDataException("Regression fixture is not valid");
        Directory.CreateDirectory(Path.Combine(output,"frozen"));
        foreach(string name in new[]{"identity.json","breadcrumbs.bin","resource-lifetimes.json"})File.Copy(Path.Combine(fixture,name),Path.Combine(output,name));
        foreach(string file in Directory.GetFiles(Path.Combine(fixture,"frozen"),"snapshot-*.bin"))File.Copy(file,Path.Combine(output,"frozen",Path.GetFileName(file)));
        string target=Directory.GetFiles(Path.Combine(output,"frozen"),"snapshot-*.bin")[0];byte[] original=File.ReadAllBytes(target);
        if(original.Length>1024*1024)throw new InvalidDataException("Use the small CPU fixture for mutation tests");
        ulong W(byte[] b,int i)=>BitConverter.ToUInt64(b,i*8);
        void Word(byte[] b,int i,ulong value)=>BitConverter.GetBytes(value).CopyTo(b,i*8);
        int Offset(byte[] b,ulong type){for(int i=0;i<(int)W(b,49);i++)if(W(b,64+4*i)==type)return (int)W(b,65+4*i);throw new InvalidDataException("Missing test section");}
        void Rehash(byte[] b){b.AsSpan(448,32).Clear();using var sha=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);sha.AppendData(b.AsSpan(0,4096));
            for(int i=0;i<(int)W(b,49);i++)if(W(b,65+i*4)>=4096)sha.AppendData(b.AsSpan((int)W(b,65+i*4),(int)W(b,66+i*4)));
            sha.GetHashAndReset().CopyTo(b,448);}
        var results=new List<object>();
        void Reject(string name,Action<byte[]> mutate,bool rehash=false,bool join=false){var bytes=(byte[])original.Clone();mutate(bytes);if(rehash)Rehash(bytes);File.WriteAllBytes(target,bytes);bool rejected=false;string reason="";
            try{if(join)rejected=FrozenSnapshot.Inspect(output)!=0;else rejected=FrozenSnapshot.Read(target).Errors.Count!=0;}catch(Exception error){rejected=true;reason=error.Message;}
            File.WriteAllBytes(target,original);results.Add(new{name,rejected,reason});if(!rejected)throw new InvalidOperationException("Accepted bad frozen evidence: "+name);}
        Reject("changed physical bytes",b=>b[Offset(b,1)]^=1);
        Reject("uncommitted header",b=>Word(b,63,0));
        Reject("wrong completion",b=>Word(b,6,W(b,4)+1),true);
        Reject("wrong prepared owner",b=>Word(b,15,W(b,15)+1),true);
        Reject("forged allocation bounds",b=>Word(b,18,ulong.MaxValue),true);
        Reject("compacted membership differs",b=>BitConverter.GetBytes(1u).CopyTo(b,Offset(b,4)),true);
        Reject("visibility membership differs",b=>BitConverter.GetBytes(0u).CopyTo(b,Offset(b,3)),true);
        Reject("camera differs despite valid hash",b=>b[Offset(b,100)]^=1,true,true);
        Reject("pupil basis differs despite valid hash",b=>b[Offset(b,103)+160]^=1,true,true);
        Reject("oversize draw",b=>BitConverter.GetBytes(uint.MaxValue).CopyTo(b,Offset(b,6)),true);
        Reject("unknown transport",b=>Word(b,45,3),true);
        if(W(original,45)>0){
            Reject("missing index readback provenance",b=>Word(b,54,0),true);
            Reject("future index provenance",b=>Word(b,54,W(b,4)+1),true);
            Reject("wrong host prefix length",b=>Word(b,53,W(b,53)+4),true);
            Reject("wrong GPU copy accounting",b=>Word(b,52,W(b,52)+4),true);
            Reject("forged cache owner despite valid hash",b=>Word(b,54,W(b,4)>1?W(b,4)-1:2),true);
        }
        foreach(ulong transport in new ulong[]{0,1,2}){
            var b=(byte[])original.Clone();Word(b,45,transport);Word(b,54,W(b,4));
            Word(b,53,transport==1?(ulong)BitConverter.ToUInt32(b,Offset(b,6))*4:0);
            Word(b,52,64*W(b,18)+(transport==1?16ul:28ul)*W(b,19)+188);Rehash(b);File.WriteAllBytes(target,b);
            if(FrozenSnapshot.Read(target).Errors.Count!=0)throw new InvalidOperationException("Historical transport reader failed");
            File.WriteAllBytes(target,original);results.Add(new{name="transport compatibility "+transport,passed=true});
        }
        byte[] journal=File.ReadAllBytes(Path.Combine(output,"breadcrumbs.bin"));int changed=128;
        while(BitConverter.ToUInt64(journal,changed)==0)changed+=512;
        journal[changed+120]^=1;File.WriteAllBytes(Path.Combine(output,"breadcrumbs.bin"),journal);
        bool corruptRejected=false;try{FrozenSnapshot.Inspect(output);}catch(InvalidDataException){corruptRejected=true;}
        if(!corruptRejected)throw new InvalidOperationException("Accepted corrupt frame journal");
        results.Add(new{name="corrupt journal",rejected=true});
        journal[changed+120]^=1;File.WriteAllBytes(Path.Combine(output,"breadcrumbs.bin"),journal);
        var header=(byte[])original.Clone();string identityPath=Path.Combine(output,"identity.json"),savedIdentity=File.ReadAllText(identityPath);
        File.WriteAllText(identityPath,"{\"gpuExecution\":true}");bool syntheticRejected=false;
        try{FrozenSnapshot.Inspect(output);}catch(InvalidDataException){syntheticRejected=true;}
        File.WriteAllText(identityPath,savedIdentity);if(!syntheticRejected)throw new InvalidOperationException("Synthetic capture accepted as GPU evidence");
        results.Add(new{name="synthetic evidence labelled live",rejected=true});
        ResourceLedger.SelfTest();results.Add(new{name="resource handle reuse retains separate births",rejected=false,passed=true});
        if(FrozenSnapshot.Inspect(output)!=0)throw new InvalidOperationException("Restored fixture failed");
        var ids=Directory.GetFiles(Path.Combine(output,"frozen"),"snapshot-*.bin").Select(p=>FrozenSnapshot.Read(p).Identity).Order().ToArray();
        var live=new LiveFrozenAudit(output,false);foreach(var id in ids)live.Finish((long)id);
        if(live.Failure!=null||live.ValidatedIdentity!=(long)ids[^1])throw new InvalidOperationException("Live frozen audit failed good evidence");
        results.Add(new{name="background live membership audit",passed=true});
        var damaged=(byte[])original.Clone();damaged[Offset(damaged,1)]^=1;File.WriteAllBytes(target,damaged);
        var badLive=new LiveFrozenAudit(output,false);badLive.Finish((long)W(original,3));File.WriteAllBytes(target,original);
        if(badLive.Failure==null)throw new InvalidOperationException("Live audit accepted corrupt capture");
        results.Add(new{name="background live corruption fails closed",rejected=true});
        File.WriteAllText(Path.Combine(output,"regression-results.json"),JsonSerializer.Serialize(new{gpuExecution=false,passed=true,checks=results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Frozen evidence regression: {results.Count} checks PASS; no GPU execution");return 0;
    }
}
