using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class TopologyRegression
{
    static void Word(byte[] b,int i,ulong v)=>BitConverter.GetBytes(v).CopyTo(b,i*8);
    static ulong W(byte[] b,int i)=>BitConverter.ToUInt64(b,i*8);
    static void Commit(byte[] b){ulong hash=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){hash^=b[i];hash=unchecked(hash*1099511628211);}Word(b,61,hash);Word(b,63,W(b,0));}
    sealed class Fixture {
        internal readonly ResourceLedger Resources=new();
        internal readonly TopologyWitnessWatch Watch;
        internal ulong Serial,Clock,Frame=10;
        internal ulong[] Authority=[];
        internal Fixture(){Watch=new(1000000,Resources);}
        internal byte[] Event(ulong phase,ulong kind,params ulong[] data){
            var b=new byte[512];Word(b,0,++Serial);Word(b,1,Clock);Word(b,2,Frame);Word(b,3,Frame);Word(b,4,Frame-1);Word(b,5,phase);Word(b,6,kind);
            for(int i=0;i<data.Length;i++)Word(b,9+i,data[i]);Commit(b);return b;
        }
        internal byte[] Topology(ulong operation){var b=Event(29,2,[operation,..Authority]);return b;}
        internal void Start(ulong id=1,int broken=-1){
            var src=Event(15,2,3,100+id,336,1);Resources.Observe(src);var dst=Event(15,2,3,200+id,336,1);Resources.Observe(dst);
            Authority=[id,100+id,W(src,0),42,1,30,28,2,0,336,4,200+id,W(dst,0),0,0,0,0,0,0,0,0,0];
            if(broken==0)Authority[2]++;Watch.Observe(Topology(1));
            Authority[13]=Frame;Authority[14]=72;Authority[18]=Serial+1;Watch.Observe(Topology(2));
            if(broken==1)Watch.Observe(Topology(2));
            if(broken!=2){Watch.Observe(Event(10,0,1,1,99,broken==3?73ul:72ul,Frame+63));
                Watch.Observe(Event(10,1,1,broken==4?98ul:99ul,Frame+63));}
        }
        internal string? Fence(double milliseconds){Watch.Observe(Event(24,0,1,99,1,ulong.MaxValue));Clock+=(ulong)(milliseconds*1000);return Watch.Observe(Event(24,1));}
        internal string? Complete(double cpu=.02,int broken=-1){
            var progress=Event(19,2);Word(progress,4,Frame);Commit(progress);if(broken!=5)Watch.Observe(progress);
            Authority[15]=Authority[17]=Frame+63;Authority[16]=Frame;Authority[19]=Serial+1;Authority[20]=(ulong)BitConverter.DoubleToInt64Bits(.2);Authority[21]=(ulong)BitConverter.DoubleToInt64Bits(cpu);
            if(broken==6)Authority[15]++;if(broken==7)Authority[13]++;if(broken==8)Authority[14]++;
            var r=Topology(3);Word(r,4,Frame);Commit(r);return Watch.Observe(r);
        }
    }
    internal static int Run(string output,string fixture){
        Directory.CreateDirectory(output);int checks=0;void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);checks++;}
        var f=new Fixture();f.Start();Check(f.Fence(3)==null&&f.Complete()==null,"normal topology proof rejected");
        f.Watch.Save(output);f.Resources.Save(output);
        using(var proof=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"topology-witnesses.json"))))
        using(var resources=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"resource-lifetimes.json")))){
            TopologyWitnessWatch.ValidateProof(proof.RootElement.GetProperty("history")[0],f.Authority,resources.RootElement.GetProperty("history"));checks++;
        }
        for(int broken=0;broken<=8;broken++){
            var b=new Fixture();bool rejected=false;try{b.Start(broken:broken);b.Complete(broken:broken);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"bad ownership accepted "+broken);
        }
        foreach(string field in new[]{"Create","Record","SubmitBegin","Submit","Complete","Progress"}){
            var node=JsonNode.Parse(File.ReadAllText(Path.Combine(output,"topology-witnesses.json")))!;
            var bytes=Convert.FromBase64String(node["history"]![0]![field]!.GetValue<string>());bytes[120]^=1;node["history"]![0]![field]=Convert.ToBase64String(bytes);
            using var proof=JsonDocument.Parse(node.ToJsonString());using var resources=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"resource-lifetimes.json")));bool rejected=false;
            try{TopologyWitnessWatch.ValidateProof(proof.RootElement.GetProperty("history")[0],f.Authority,resources.RootElement.GetProperty("history"));}catch(InvalidDataException){rejected=true;}
            Check(rejected,"corrupt proof capsule accepted "+field);
        }
        // A validly checksummed submit for the wrong command still cannot prove a copy.
        foreach(int mutation in new[]{0,1,2,3}){
            var node=JsonNode.Parse(File.ReadAllText(Path.Combine(output,"topology-witnesses.json")))!;var lives=JsonNode.Parse(File.ReadAllText(Path.Combine(output,"resource-lifetimes.json")))!;
            if(mutation<2){string name=mutation==0?"SubmitBegin":"Submit";var bytes=Convert.FromBase64String(node["history"]![0]![name]!.GetValue<string>());Word(bytes,mutation==0?12:11,999);Commit(bytes);node["history"]![0]![name]=Convert.ToBase64String(bytes);}
            else if(mutation==2)lives["history"]![0]!["BirthSerial"]=999;else lives["history"]![1]!["DeathSerial"]=3;
            using var proof=JsonDocument.Parse(node.ToJsonString());using var resources=JsonDocument.Parse(lives.ToJsonString());bool rejected=false;
            try{TopologyWitnessWatch.ValidateProof(proof.RootElement.GetProperty("history")[0],f.Authority,resources.RootElement.GetProperty("history"));}catch(InvalidDataException){rejected=true;}Check(rejected,"forged lifetime/submit chain accepted");
        }
        var sparse=new Fixture();string? alarm=null;for(ulong i=1;i<=4;i++){
            sparse.Frame=i*1000;sparse.Start(i);alarm=sparse.Fence(i==2?3:7);sparse.Complete();sparse.Watch.Observe(sparse.Topology(4));
            // Hundreds of cheap ordinary fences do not dilute witness history.
            for(int j=0;j<100;j++){sparse.Frame++;sparse.Watch.Observe(sparse.Event(24,0,1,99,1,ulong.MaxValue));sparse.Clock+=100;sparse.Watch.Observe(sparse.Event(24,1));}
            Check((alarm!=null)==(i==4),"sparse witness recurrence boundary");
        }
        string alarmDir=Path.Combine(output,"first-alarm");Directory.CreateDirectory(alarmDir);sparse.Watch.Save(alarmDir);sparse.Resources.Save(alarmDir);
        using(var saved=JsonDocument.Parse(File.ReadAllText(Path.Combine(alarmDir,"topology-witnesses.json")))){
            var window=saved.RootElement.GetProperty("trigger").GetProperty("window");Check(window.GetArrayLength()==4&&window[0].GetProperty("Frame").GetUInt64()==1000,"first sparse alarm identity aged out");
            foreach(var sample in window.EnumerateArray()){Check(Convert.FromBase64String(sample.GetProperty("Begin").GetString()!).Length==512&&Convert.FromBase64String(sample.GetProperty("End").GetString()!).Length==512,"raw qualifying fence missing");}
        }
        var completion=new Fixture();for(ulong i=1;i<=3;i++){completion.Frame=i*100;completion.Start(i);completion.Fence(.2);Check((completion.Complete(2)!=null)==(i==3),"topology completion recurrence");completion.Watch.Observe(completion.Topology(4));}
        var reused=new Fixture();byte[] reusable=new byte[512],expected=[];
        for(ulong i=1;i<=3;i++){reused.Frame=i*100;reused.Start(i);reused.Watch.Observe(reused.Event(24,0,1,99,1,ulong.MaxValue));reused.Clock+=7000;
            reused.Event(24,1).CopyTo(reusable,0);if(i==3)expected=(byte[])reusable.Clone();reused.Watch.Observe(reusable);Array.Fill(reusable,(byte)0x5a);reused.Complete();reused.Watch.Observe(reused.Topology(4));}
        string reuseDir=Path.Combine(output,"reused-buffer");Directory.CreateDirectory(reuseDir);reused.Watch.Save(reuseDir);
        using(var saved=JsonDocument.Parse(File.ReadAllText(Path.Combine(reuseDir,"topology-witnesses.json"))))Check(Convert.FromBase64String(saved.RootElement.GetProperty("trigger").GetProperty("record").GetString()!).SequenceEqual(expected),"first topology trigger aliased reusable observer buffer");
        // Convert a small CPU fixture to the new self-contained wire format.
        // Its indices remain the fixture's synthetic bytes, never called GPU data.
        var source=Directory.GetFiles(Path.Combine(fixture,"frozen"),"snapshot-*.bin").OrderBy(p=>FrozenSnapshot.Read(p).Frame).Last();var data=File.ReadAllBytes(source);
        Word(data,45,3);Word(data,33,f.Authority[1]);Word(data,55,f.Authority[2]);Word(data,13,42);Word(data,54,f.Authority[13]);Word(data,52,64*W(data,18)+16*W(data,19)+188);Word(data,49,16);
        ulong end=1024;for(int i=0;i<15;i++)if(W(data,65+i*4)<4096)end=Math.Max(end,W(data,65+i*4)+W(data,66+i*4));end=(end+31)&~31ul;
        Word(data,124,109);Word(data,125,end);Word(data,126,176);Word(data,127,1);for(int i=0;i<22;i++)Word(data,(int)end/8+i,f.Authority[i]);
        void Hash(){data.AsSpan(448,32).Clear();using var sha=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);sha.AppendData(data.AsSpan(0,4096));for(int i=0;i<16;i++)if(W(data,65+i*4)>=4096)sha.AppendData(data.AsSpan((int)W(data,65+i*4),(int)W(data,66+i*4)));sha.GetHashAndReset().CopyTo(data,448);}
        Hash();string target=Path.Combine(output,"transport-v3.bin");File.WriteAllBytes(target,data);Check(FrozenSnapshot.Read(target).Errors.Count==0,"v3 membership reader failed");f.Watch.ValidateCapture(data);checks++;
        var original=(byte[])data.Clone();foreach(int field in new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21}){
            data=(byte[])original.Clone();Word(data,(int)end/8+field,ulong.MaxValue);Hash();File.WriteAllBytes(target,data);bool rejected=false;
            try{FrozenSnapshot.Read(target);f.Watch.ValidateCapture(data);}catch(Exception){rejected=true;}Check(rejected,"forged v3 provenance accepted "+field);
        }
        File.WriteAllBytes(target,original);
        // V4 is a lossless transport: the final physical section is still all
        // original 64-byte records, with a GPU-completed zero-padding witness.
        data=(byte[])original.Clone();Word(data,45,4);Word(data,51,W(data,51)|4);
        int indirectOffset=0;for(int i=0;i<16;i++)if(W(data,64+i*4)==6)indirectOffset=(int)W(data,65+i*4);
        uint count=BitConverter.ToUInt32(data,indirectOffset),vertices=(uint)W(data,18),triangles=(uint)W(data,19);
        ulong actual=48ul*vertices+16ul*Math.Min(vertices,4u)+4ul*triangles+4ul*count+252;
        Word(data,52,actual);uint[] metadata=[0x344b5046,0,vertices,triangles,count,(uint)W(data,4),(uint)(W(data,4)>>32),(uint)W(data,3),(uint)(W(data,3)>>32),(uint)actual,64*vertices+4*triangles+4*count+188,(vertices-Math.Min(vertices,4u))*4,0,0,0,0];
        for(int i=0;i<16;i++)BitConverter.GetBytes(metadata[i]).CopyTo(data,4032+i*4);Hash();string packedTarget=Path.Combine(output,"transport-v4.bin");File.WriteAllBytes(packedTarget,data);
        Check(FrozenSnapshot.Read(packedTarget).Errors.Count==0,"v4 lossless membership reader failed");f.Watch.ValidateCapture(data);checks++;
        var packedOriginal=(byte[])data.Clone();for(int field=0;field<16;field++){
            data=(byte[])packedOriginal.Clone();data[4032+field*4]^=1;Hash();File.WriteAllBytes(packedTarget,data);bool rejected=false;
            try{FrozenSnapshot.Read(packedTarget);}catch(Exception){rejected=true;}Check(rejected,"forged lossless packing metadata accepted "+field);
        }
        File.WriteAllBytes(packedTarget,packedOriginal);
        File.WriteAllText(Path.Combine(output,"topology-regression.json"),JsonSerializer.Serialize(new{passed=true,checks,gpuExecution=false,firstAlarm=alarm,scope="exact wire/ownership/proof and sparse first-alarm costs; CPU fixtures"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS topology observer checks={checks}; GPU execution=false");return 0;
    }
}

