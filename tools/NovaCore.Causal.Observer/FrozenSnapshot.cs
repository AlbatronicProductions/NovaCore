using System.Security.Cryptography;
using System.Text.Json;

// Offline reader. It has no process-launch or GPU entry point.
internal static class FrozenSnapshot
{
    const ulong Magic=0x314e455a4f52464e; const int HeaderSize=4096; const long SlotBytes=88L*1024*1024;
    sealed record Section(ulong Type,ulong Offset,ulong Bytes,ulong Stride);
    internal sealed record Result(string Path,ulong Identity,ulong Frame,ulong Submission,ulong Generation,ulong Pupil,
        ulong Vertices,ulong Triangles,ulong Visible,ulong Clipping,ulong Tcs,ulong Tes,ulong Swap,
        string PayloadSha256,string AuthoritySha256,bool Synthetic,bool DirectPreparedRaster,Dictionary<ulong,string> SectionSha256,List<string> Errors,ulong[] TopologyAuthority);
    static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
    static ulong W(byte[] h,int i)=>BitConverter.ToUInt64(h,i*8);
    static string AuthorityHash(byte[] original){
        var h=(byte[])original.Clone();
        foreach(int i in new[]{5,6,8,9,40,41,42,43,44,53,56,57,58,59,63})h.AsSpan(i*8,8).Clear();
        if(W(h,45)==4){h.AsSpan(52*8,8).Clear();h.AsSpan(4032,64).Clear();BitConverter.GetBytes(W(h,51)&~4ul).CopyTo(h,51*8);}
        for(int i=0;i<(int)W(h,49);i++)if(W(h,64+i*4)==4)BitConverter.GetBytes(W(h,19)*12).CopyTo(h,(66+i*4)*8);
        return Convert.ToHexStringLower(SHA256.HashData(h));
    }
    internal static Result Read(string path)
    {
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
        Require(file.Length>=HeaderSize&&file.Length<=HeaderSize+SlotBytes,"Frozen file size");
        var h=new byte[HeaderSize];file.ReadExactly(h);
        Require(W(h,0)==Magic&&W(h,1)==1&&W(h,2)==HeaderSize,"Frozen schema");
        Require(W(h,3)!=0&&W(h,63)==W(h,3),"Uncommitted frozen file");
        Require(W(h,4)==W(h,6)&&W(h,5)!=0&&W(h,5)==W(h,44),"Frozen submission/completion mismatch");
        Require(W(h,15)==W(h,16)&&W(h,16)==W(h,17),"Frozen pupil ownership mismatch");
        Require(W(h,18)>0&&W(h,18)<=(ulong)SlotBytes/64&&W(h,19)>0&&W(h,19)<=(ulong)SlotBytes/28&&W(h,48)<=(ulong)SlotBytes&&W(h,49)==(W(h,45)>=3?16ul:15ul),"Frozen capacity/sections");
        var sections=new Dictionary<ulong,Section>();var ranges=new List<(ulong Start,ulong End)>();
        for(int i=0;i<(int)W(h,49);i++){
            var s=new Section(W(h,64+i*4),W(h,65+i*4),W(h,66+i*4),W(h,67+i*4));
            Require(sections.TryAdd(s.Type,s)&&s.Offset<=ulong.MaxValue-s.Bytes,"Frozen duplicate/overflow section");
            bool bulk=s.Type<100;
            Require(s.Stride==(bulk?(s.Type==1?64ul:4ul):1ul),"Frozen section stride");
            Require(bulk?s.Type is >=1 and <=6&&s.Offset>=HeaderSize&&s.Offset+s.Bytes<=(ulong)file.Length
                :s.Type is >=100 and <=109&&s.Offset>=1024&&s.Offset+s.Bytes<=HeaderSize,"Frozen section range");
            Require(ranges.All(r=>s.Offset+s.Bytes<=r.Start||s.Offset>=r.End),"Frozen overlapping sections");
            ranges.Add((s.Offset,s.Offset+s.Bytes));
        }
        var expected=new Dictionary<ulong,ulong>{{1,W(h,18)*64},{2,W(h,19)*12},{3,W(h,19)*4},{5,168},{6,20},
            {100,96},{101,96},{102,192},{103,480},{104,512},{105,160},{106,240},{107,128},{108,576}};
        foreach(var (type,size) in expected)Require(sections.TryGetValue(type,out var s)&&s.Bytes==size,"Frozen authority layout "+type);
        Require(sections.ContainsKey(4)&&sections[4].Bytes<=W(h,19)*12,"Frozen compacted capacity");
        var digest=h.AsSpan(448,32).ToArray();h.AsSpan(448,32).Clear();
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(h);
        var payload=new Dictionary<ulong,byte[]>();var hashes=new Dictionary<ulong,string>();
        foreach(var s in sections.Values){
            byte[] data;
            if(s.Type>=100)data=h.AsSpan((int)s.Offset,(int)s.Bytes).ToArray();
            else {Require(s.Bytes<=int.MaxValue,"Frozen section size");data=new byte[(int)s.Bytes];file.Position=(long)s.Offset;file.ReadExactly(data);hash.AppendData(data);}
            payload.Add(s.Type,data);hashes.Add(s.Type,Convert.ToHexStringLower(SHA256.HashData(data)));
        }
        Require(hash.GetHashAndReset().AsSpan().SequenceEqual(digest),"Frozen SHA256 mismatch");
        uint U(ulong type,int index)=>BitConverter.ToUInt32(payload[type],index*4);
        uint count=U(6,0);Require(count%3==0&&count<=W(h,19)*3&&payload[4].Length==(long)count*4,"Frozen draw index range");
        Require(W(h,45)<=4,"Frozen capture transport version");
        if(W(h,45)>0){
            Require(W(h,54)>0&&W(h,54)<=W(h,4)&&W(h,53)==(W(h,45)==1?(ulong)count*4:0),"Frozen cached-index/prefix provenance");
            ulong copied=W(h,18)*64+W(h,19)*(W(h,45)==1?4ul:16ul)+188+(W(h,45)<3&&W(h,54)==W(h,4)?W(h,19)*12:0);
            if(W(h,45)==4){
                var m=Enumerable.Range(0,16).Select(i=>BitConverter.ToUInt32(h,4032+i*4)).ToArray();
                copied=48*W(h,18)+16*Math.Min(W(h,18),4)+4*W(h,19)+(ulong)count*4+188+64;
                Require((W(h,51)&4)!=0&&m[0]==0x344b5046&&m[1]==0&&m[2]==W(h,18)&&m[3]==W(h,19)&&m[4]==count&&
                    ((ulong)m[5]|((ulong)m[6]<<32))==W(h,4)&&((ulong)m[7]|((ulong)m[8]<<32))==W(h,3)&&m[9]==copied&&
                    m[10]==64*W(h,18)+4*W(h,19)+(ulong)count*4+188&&m[11]==(W(h,18)-Math.Min(W(h,18),4))*4&&m.Skip(12).All(v=>v==0),"Frozen lossless GPU packing proof");
                Require(sections.Values.Where(s=>s.Type>=100).All(s=>s.Offset+s.Bytes<=4032),"Packing proof overlaps inputs");
                for(int v=4;v<(int)W(h,18);v++)Require(payload[1].AsSpan(v*64+48,16).IndexOfAnyExcept((byte)0)<0,"GPU-attested zero padding changed");
            }
            Require(W(h,52)==copied,"Frozen GPU copy accounting");
        }
        ulong[] topology=[];
        if(W(h,45)>=3){
            Require(sections.TryGetValue(109,out var ts)&&ts.Bytes==176,"Frozen topology provenance layout");
            topology=Enumerable.Range(0,22).Select(i=>BitConverter.ToUInt64(payload[109],i*8)).ToArray();var t=topology;
            Require(t[0]>0&&t[1]==W(h,33)&&t[2]>0&&t[2]==W(h,55)&&t[3]==W(h,13)&&t[4]==W(h,14)&&t[5]==W(h,18)&&t[6]==W(h,19),"Frozen topology resource identity");
            Require(t[7]==2&&t[8]==0&&t[9]==W(h,19)*12&&t[10]==4&&t[11]!=0&&t[12]>0,"Frozen topology source/destination layout");
            Require(t[13]>0&&t[13]==W(h,54)&&t[13]<W(h,4)&&t[14]!=0&&t[15]>0&&t[16]==t[13]&&t[17]==t[15]&&t[18]>t[2]&&t[18]>t[12]&&t[19]>t[18],"Frozen topology submitted/completed provenance");
            foreach(int i in new[]{20,21})Require(double.IsFinite(BitConverter.Int64BitsToDouble((long)t[i]))&&BitConverter.Int64BitsToDouble((long)t[i])>=0,"Frozen topology cost");
        }
        Require(U(6,1)==1&&U(6,2)==0&&U(6,3)==0&&U(6,4)==0,"Frozen indirect contract");
        var errors=new List<string>();void Check(bool ok,string error){if(!ok)errors.Add(error);}
        var visible=new List<UInt128>(checked((int)W(h,19)));var actual=new UInt128[count/3];
        UInt128 Triangle(ulong type,int i){uint a=U(type,3*i),b=U(type,3*i+1),c=U(type,3*i+2);
            Require(a<W(h,18)&&b<W(h,18)&&c<W(h,18),"Frozen out-of-range vertex index");
            return ((UInt128)a<<64)|((UInt128)b<<32)|c;}
        for(int i=0;i<(int)W(h,19);i++){
            var triangle=Triangle(2,i);uint flag=U(3,i);Require(flag<=1,"Frozen visibility flag");if(flag==1)visible.Add(triangle);
        }
        for(int i=0;i<actual.Length;i++)actual[i]=Triangle(4,i);
        var expectedTriangles=visible.ToArray();Array.Sort(expectedTriangles);Array.Sort(actual);
        Check(expectedTriangles.AsSpan().SequenceEqual(actual),"Compacted membership differs from source visibility");
        Check((ulong)visible.Count==U(5,0)&&U(5,4)==count,"Visibility/compaction counters disagree");
        Check(U(5,5)==W(h,19)&&W(h,19)==(ulong)U(5,0)+U(5,1)+U(5,3)+U(5,8),"Cull input accounting mismatch");
        Check(U(5,2)==0&&U(5,3)==0,"GPU reports overflow or invalid physical triangles");
        float outer=BitConverter.ToSingle(payload[5],23*4),inner=BitConverter.ToSingle(payload[5],24*4);
        if((W(h,51)&8)!=0){
            float target=BitConverter.ToSingle(payload[101],88);
            Check(W(h,14)==1&&W(h,25)==4&&W(h,50)==2&&(W(h,31)&0xfc00)==0&&float.IsFinite(target)&&target>=0,"Direct prepared raster eligibility mismatch");
            Check(W(h,42)==0&&W(h,43)==0&&outer==0&&inner==0,"Direct prepared raster has tessellation work");
        }else{
            Check(W(h,42)==(ulong)visible.Count,"TCS query differs from retained population");
            Check(float.IsFinite(outer)&&outer is >=1 and <=64,"Tessellation factor outside accepted domain");
        }
        Check(unchecked((long)W(h,9)) is 0 or 1000001003,"Present failed or unavailable");
        // Pupil raw identity[0] starts at byte 128 of the 160-byte current pupil.
        Check(BitConverter.ToUInt32(payload[103],160+128)==W(h,15),"Captured current pupil bytes differ from prepared identity");
        return new(path,W(h,3),W(h,4),W(h,5),W(h,11),W(h,15),W(h,18),W(h,19),(ulong)visible.Count,
            W(h,40),W(h,42),W(h,43),W(h,10),Convert.ToHexStringLower(digest),AuthorityHash(h),(W(h,51)&2)!=0,(W(h,51)&8)!=0,hashes,errors,topology);
    }
    internal static int Inspect(string directory,string? authorityDirectory=null)
    {
        var files=Directory.GetFiles(Path.Combine(directory,"frozen"),"snapshot-*.bin");
        Require(files.Length is >=1 and <=2,"No complete bounded frozen capture");
        var results=files.Select(Read).OrderBy(r=>r.Identity).ToArray();
        using var identity=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"identity.json")));
        bool gpu=identity.RootElement.GetProperty("gpuExecution").GetBoolean();
        Require(results.All(r=>r.Synthetic!=gpu),"Synthetic/live evidence identity mismatch");
        Require(!gpu||results.Length==2,"Live stage requires two complete frozen captures");
        var journal=ReadAuthority(authorityDirectory??directory,results);
        bool clean=results.All(r=>r.Errors.Count==0)&&journal.Errors.Count==0;
        File.WriteAllText(Path.Combine(directory,"frozen-analysis.json"),JsonSerializer.Serialize(new{
            schema=1,clean,scope="Exact captured prepared geometry and GPU membership; no synthesized historical pupil",captures=results,journal,
            limits="Two sampled frames, cadence 1000ms; intervening frame inputs/progress are journaled, not full geometry captures"
        },new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(new{frozenCaptures=results.Length,clean,journal.Errors}));return clean?0:3;
    }
    sealed record JournalResult(int CompleteAuthorityFrames,int PartialAuthorityFrames,ulong LastSubmitted,ulong LastCompleted,List<string> Errors);
    internal static bool InspectPinned(string directory,string? authorityDirectory=null,int maximum=3)
    {
        string alarm=Path.Combine(directory,"first-alarm");bool clean=false;string? failure=null;Result[] results=[];JournalResult? journal=null;
        try{
            using var pins=JsonDocument.Parse(File.ReadAllText(Path.Combine(alarm,"capture-pins.json")));
            Require(pins.RootElement.GetProperty("failure").ValueKind==JsonValueKind.Null,"First alarm has an incomplete/unpublished trigger capture");
            var paths=Directory.GetFiles(alarm,"snapshot-*.bin");Require(paths.Length<=maximum,"Alarm pin capacity");results=paths.Select(Read).OrderBy(r=>r.Identity).ToArray();
            using var identity=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"identity.json")));bool gpu=identity.RootElement.GetProperty("gpuExecution").GetBoolean();
            Require(results.All(r=>r.Synthetic!=gpu),"Alarm synthetic/live provenance mismatch");
            if(results.Length!=0)journal=ReadAuthority(authorityDirectory??directory,results);
            clean=results.All(r=>r.Errors.Count==0)&&(journal==null||journal.Errors.Count==0);
        }catch(Exception error){failure=error.Message;}
        File.WriteAllText(Path.Combine(alarm,"frozen-validation.json"),JsonSerializer.Serialize(new{clean,failure,captures=results,journal},new JsonSerializerOptions{WriteIndented=true}));return clean;
    }
    static JournalResult ReadAuthority(string directory,Result[] captures)
    {
        using var file=new FileStream(Path.Combine(directory,"breadcrumbs.bin"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite);var header=new byte[128];file.ReadExactly(header);
        Require(W(header,0)==0x314c41535541434e&&W(header,1)==2&&W(header,2)==512&&W(header,15)<=131072,"Journal schema");
        Require(file.Length==128+(long)W(header,15)*512,"Journal capacity");
        var groups=new Dictionary<ulong,Dictionary<int,byte[]>>();var authoritySerials=new Dictionary<ulong,ulong>();var submitted=new Dictionary<ulong,ulong>();
        var completed=new Dictionary<ulong,ulong>();var packing=new Dictionary<ulong,byte[]>();var errors=new List<string>();var serials=new List<ulong>();ulong lastSubmitted=0,lastCompleted=0;
        foreach(var e in CausalJournal.Records(directory)){
            if(W(e,0)==0)continue;
            ulong checksum=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){checksum^=e[i];checksum=unchecked(checksum*1099511628211);}
            Require(W(e,0)==W(e,63)&&checksum==W(e,61),"Corrupt journal in frozen join");
            serials.Add(W(e,0));
            lastSubmitted=Math.Max(lastSubmitted,W(e,3));lastCompleted=Math.Max(lastCompleted,W(e,4));
            if(W(e,5)==10&&W(e,6)==1&&W(e,7)==0)submitted[W(e,3)]=W(e,11);
            if(W(e,5)==27&&W(e,6)==2&&W(e,9)==3)completed[W(e,11)]=W(e,12);
            if(W(e,5)==27&&W(e,6)==2&&W(e,9)==10){Require(packing.TryAdd(W(e,11),e),"Duplicate packing completion");}
            if(W(e,5)!=26||W(e,6)!=2)continue;
            ulong frame=W(e,9);int part=checked((int)W(e,10));
            Require(W(e,11)==11&&W(e,12)==4096&&part is >=0 and <11&&W(e,13)==(ulong)Math.Min(376,4096-part*376),"Authority fragment layout");
            if(!groups.TryGetValue(frame,out var chunks)){chunks=new();groups.Add(frame,chunks);}
            if(!authoritySerials.TryGetValue(frame,out var serial)||W(e,0)<serial)authoritySerials[frame]=W(e,0);
            Require(chunks.TryAdd(part,e.AsSpan(112,(int)W(e,13)).ToArray()),"Duplicate authority fragment");
        }
        var assembled=new Dictionary<ulong,byte[]>();
        serials.Sort();Require(serials.Count>0,"Empty journal");
        for(int i=1;i<serials.Count;i++)Require(serials[i]==serials[i-1]+1,"Journal gap in frozen join");
        foreach(var (frame,chunks) in groups)if(chunks.Count==11){var h=new byte[4096];for(int i=0;i<11;i++)chunks[i].CopyTo(h,i*376);Require(W(h,0)==Magic&&W(h,4)==frame,"Authority identity");assembled.Add(frame,h);}
        using var lifetimes=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"resource-lifetimes.json")));
        foreach(var capture in captures){
            if(!assembled.TryGetValue(capture.Frame,out var h)){errors.Add("Capture authority missing from retained journal: "+capture.Frame);continue;}
            if(W(h,3)!=capture.Identity||W(h,11)!=capture.Generation||W(h,15)!=capture.Pupil)errors.Add("Capture authority identity mismatch: "+capture.Frame);
            if(AuthorityHash(h)!=capture.AuthoritySha256)errors.Add("Capture inputs differ from recorded frame: "+capture.Frame);
            if(!submitted.TryGetValue(capture.Frame,out var sequence)||sequence!=capture.Submission)errors.Add("Capture submission not joined: "+capture.Frame);
            if(!completed.TryGetValue(capture.Frame,out sequence)||sequence!=capture.Submission)errors.Add("Capture completion not joined: "+capture.Frame);
            if(W(h,45)==4){
                if(!packing.TryGetValue(capture.Frame,out var p)||W(p,7)!=0||W(p,4)!=capture.Frame||W(p,10)!=capture.Identity||W(p,12)>=2)errors.Add("Missing exact packing completion: "+capture.Frame);
                else {
                    using var f=new FileStream(capture.Path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);f.Position=4032;var raw=new byte[64];f.ReadExactly(raw);
                    for(int i=0;i<16;i++)if(W(p,14+i)!=BitConverter.ToUInt32(raw,i*4))errors.Add("Packing GPU metadata changed: "+capture.Frame+"/"+i);
                    int matches=lifetimes.RootElement.GetProperty("history").EnumerateArray().Count(l=>l.GetProperty("Kind").GetUInt64()==3&&l.GetProperty("Handle").GetUInt64()==W(p,13)&&l.GetProperty("BirthSerial").GetUInt64()<authoritySerials[capture.Frame]&&
                        (l.GetProperty("DeathSerial").ValueKind==JsonValueKind.Null||l.GetProperty("DeathSerial").GetUInt64()>W(p,0)));
                    if(matches!=1)errors.Add("Packing metadata owner unresolved: "+capture.Frame);
                }
            }
            if(capture.TopologyAuthority.Length!=0){
                using var proof=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"topology-witnesses.json")));
                var matches=proof.RootElement.GetProperty("history").EnumerateArray().Where(x=>x.GetProperty("Identity").GetUInt64()==capture.TopologyAuthority[0]).ToArray();
                if(matches.Length!=1||matches[0].GetProperty("Complete").ValueKind==JsonValueKind.Null||!matches[0].GetProperty("Authority").EnumerateArray().Select(x=>x.GetUInt64()).SequenceEqual(capture.TopologyAuthority))errors.Add("Missing retained topology GPU completion proof: "+capture.Frame);
                else {
                    try{TopologyWitnessWatch.ValidateProof(matches[0],capture.TopologyAuthority,lifetimes.RootElement.GetProperty("history"));}
                    catch(Exception error){errors.Add("Invalid topology proof: "+capture.Frame+"/"+error.Message);}
                }
            }
            if(!capture.Synthetic)for(int handleWord=32;handleWord<=37;handleWord++){
                ulong handle=W(h,handleWord),serial=authoritySerials[capture.Frame];
                int owners=lifetimes.RootElement.GetProperty("history").EnumerateArray().Count(l=>l.GetProperty("Kind").GetUInt64()==3&&l.GetProperty("Handle").GetUInt64()==handle&&l.GetProperty("BirthSerial").GetUInt64()<serial&&
                    (l.GetProperty("DeathSerial").ValueKind==JsonValueKind.Null||l.GetProperty("DeathSerial").GetUInt64()>serial));
                if(owners!=1)errors.Add("Capture buffer incarnation not resolved: "+capture.Frame+"/"+handleWord);
            }
        }
        return new(assembled.Count,groups.Count-assembled.Count,lastSubmitted,lastCompleted,errors);
    }
}
