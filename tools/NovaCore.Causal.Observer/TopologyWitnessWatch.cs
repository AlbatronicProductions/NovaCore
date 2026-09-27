using System.Text.Json;

// Observer-owned bounded proof ledger. Copy origins must survive journal expiry.
internal sealed class TopologyWitnessWatch(long frequency,ResourceLedger resources)
{
    internal sealed class Witness {
        public ulong Identity {get;init;}
        public byte[] Create {get;init;}=[];
        public byte[]? Record {get;set;}
        public byte[]? Submit {get;set;}
        public byte[]? SubmitBegin {get;set;}
        public byte[]? Complete {get;set;}
        public byte[]? Progress {get;set;}
        public byte[]? FenceBegin {get;set;}
        public byte[]? FenceEnd {get;set;}
        public byte[]? FrameTiming {get;set;}
        public byte[]? Retire {get;set;}
        public ulong[] Authority {get;set;}=[];
        public double RecordMilliseconds {get;set;}
    }
    readonly List<Witness> history=new(256);
    readonly Dictionary<ulong,Witness> active=[];
    readonly Dictionary<long,Witness> pending=[];
    readonly Dictionary<ulong,Witness> origins=[];
    internal sealed record Sample(double Milliseconds,ulong Identity,ulong Frame,byte[] Begin,byte[] End);
    readonly Queue<Sample> fenceCosts=new(4),completionCosts=new(4);
    byte[]? fenceBegin;
    byte[]? progress;
    internal object Evidence=>new{history,trigger};
    object? trigger;
    static ulong W(byte[] b,int i)=>BitConverter.ToUInt64(b,i*8);
    static void Require(bool ok,string reason){if(!ok)throw new InvalidDataException("Topology witness: "+reason);}
    static ulong[] Authority(byte[] b)=>Enumerable.Range(10,22).Select(i=>W(b,i)).ToArray();
    string? Cost(Queue<Sample> window,double ms,string scope,Witness owner,byte[] record,byte[]? begin=null){
        Require(double.IsFinite(ms)&&ms>=0,"nonfinite cost");if(window.Count==4)window.Dequeue();window.Enqueue(new(ms,owner.Identity,owner.Authority[13],begin??owner.Submit!,(byte[])record.Clone()));
        double limit=scope=="fence"?CaptureCostWatch.RepeatingFenceMilliseconds:CaptureCostWatch.RepeatingCompletionMilliseconds;
        if(trigger==null&&window.Count(v=>v.Milliseconds>limit)>=3){trigger=new{scope,milliseconds=ms,owner.Identity,owner.Authority,record=(byte[])record.Clone(),window=window.ToArray()};return $"repeated topology witness {scope} cost exceeds {limit}ms (3 of last 4)";}return null;
    }
    internal string? Observe(byte[] r){
        if((W(r,5)==19&&W(r,6)==2)||(W(r,5)==22&&W(r,6)==1&&W(r,7)==0))progress=(byte[])r.Clone();
        if(W(r,5)==14&&W(r,6)==2&&W(r,9)==7&&origins.TryGetValue(W(r,10),out var timed))timed.FrameTiming=(byte[])r.Clone();
        if(W(r,5)==29&&W(r,6)==2){
            ulong operation=W(r,9),id=W(r,10);
            if(operation==1){
                var a=Authority(r);Require(history.Count<256&&active.Count<4&&!history.Any(x=>x.Identity==id)&&id!=0,"bounded cache identity");
                Require(a[2]>0&&a[3]>0&&a[5]>0&&a[6]>0&&a[7]==2&&a[8]==0&&a[9]==a[6]*12&&a[9]<=88ul*1024*1024&&a[10]==4,"source topology layout");
                Require(resources.Buffer(a[1],a[2],a[9])&&resources.Buffer(a[11],a[12],a[9]),"resource incarnation");
                Require(a.Skip(13).All(x=>x==0),"premature copy authority");
                var witness=new Witness{Identity=id,Create=(byte[])r.Clone(),Authority=a};history.Add(witness);active.Add(id,witness);return null;
            }
            Require(active.TryGetValue(id,out var t),"unknown active owner");var owner=t!;
            if(operation==5){Require(owner.Record!=null&&W(r,11)==owner.Authority[13],"record cost identity");owner.RecordMilliseconds=BitConverter.ToDouble(r,12*8);Require(double.IsFinite(owner.RecordMilliseconds)&&owner.RecordMilliseconds>=0,"record cost");return null;}
            var authority=Authority(r);Require(authority.Take(13).SequenceEqual(owner.Authority.Take(13)),"immutable authority changed");
            if(operation==2){
                Require(owner.Record==null&&authority[13]>0&&authority[13]==W(r,2)&&authority[14]!=0&&authority[18]==W(r,0)&&authority[15]==0&&authority[16]==0,"record identity/order");
                Require(resources.Buffer(authority[1],authority[2],authority[9])&&resources.Buffer(authority[11],authority[12],authority[9]),"source retired before copy");
                Require(pending.TryAdd((long)authority[13],owner)&&origins.TryAdd(authority[13],owner),"multiple topology activations in a frame");owner.Record=(byte[])r.Clone();owner.Authority=authority;
            }else if(operation==3){
                Require(owner.Record!=null&&owner.Submit!=null&&owner.Complete==null&&authority[13]==authority[16]&&authority[16]==W(r,4)&&authority[15]==authority[17]&&authority[15]==W(owner.Submit!,11)&&authority[19]==W(r,0),"completion without exact submitted fence");
                Require(authority[13]==owner.Authority[13]&&authority[14]==owner.Authority[14]&&authority[18]==owner.Authority[18],"completion origin changed");
                Require(double.IsFinite(BitConverter.Int64BitsToDouble((long)authority[20]))&&BitConverter.Int64BitsToDouble((long)authority[20])>=0,"GPU cost");
                Require(progress!=null&&W(progress!,4)==authority[16],"completion lacks GPU progress boundary");
                owner.Progress=progress;owner.Complete=(byte[])r.Clone();owner.Authority=authority;pending.Remove((long)authority[13]);
                return Cost(completionCosts,BitConverter.Int64BitsToDouble((long)authority[21]),"completion",owner,r);
            }else if(operation==4){owner.Retire=(byte[])r.Clone();active.Remove(id);pending.Remove((long)owner.Authority[13]);}
            else throw new InvalidDataException("Topology witness: unknown operation");return null;
        }
        if(W(r,5)==10&&W(r,6)==0&&pending.TryGetValue((long)W(r,2),out var recorded)){
            Require(recorded.SubmitBegin==null&&W(r,12)==recorded.Authority[14]&&W(r,11)!=0&&W(r,13)>0,"submitted command differs from recorded copy");recorded.SubmitBegin=(byte[])r.Clone();
        }
        if(W(r,5)==10&&W(r,6)==1&&W(r,7)==0&&pending.TryGetValue((long)W(r,3),out var submitted)){
            Require(submitted.Submit==null&&submitted.SubmitBegin!=null&&W(r,10)==W(submitted.SubmitBegin!,11)&&W(r,9)==W(submitted.SubmitBegin!,9)&&W(r,11)==W(submitted.SubmitBegin!,13),"submission identity");submitted.Submit=(byte[])r.Clone();
        }
        if(W(r,5)==24){
            if(W(r,6)==0){Require(fenceBegin==null,"nested fence");fenceBegin=(byte[])r.Clone();}
            else if(W(r,6)==1&&fenceBegin!=null){var begin=fenceBegin;fenceBegin=null;
                if(pending.TryGetValue((long)W(r,3),out var owner)){Require(W(begin,3)==W(r,3)&&owner.Submit!=null&&W(begin,10)==W(owner.Submit!,10),"fence origin");owner.FenceBegin=begin;owner.FenceEnd=(byte[])r.Clone();return Cost(fenceCosts,((long)W(r,1)-(long)W(begin,1))*1000d/frequency,"fence",owner,r,begin);}
            }
        }
        return null;
    }
    internal void ValidateCapture(byte[] h){
        if(W(h,45)<3)return;
        Require(W(h,49)==16&&W(h,124)==109&&W(h,126)==176,"capture provenance section");
        int offset=checked((int)W(h,125));Require(offset>=1024&&offset<=4096-176,"capture provenance bounds");
        var authority=Enumerable.Range(0,22).Select(i=>BitConverter.ToUInt64(h,offset+i*8)).ToArray();
        Require(active.TryGetValue(authority[0],out var t)&&t.Complete!=null&&t.Authority.SequenceEqual(authority),"capture lacks retained GPU completion proof");
        Require(authority[1]==W(h,33)&&authority[2]==W(h,55)&&authority[3]==W(h,13)&&authority[4]==W(h,14)&&authority[5]==W(h,18)&&authority[6]==W(h,19)&&authority[13]==W(h,54)&&authority[16]<W(h,4),"capture topology owner");
    }
    internal void Save(string directory){
        var temporary=Path.Combine(directory,"topology-witnesses.tmp");
        using(var file=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.Read)){JsonSerializer.Serialize(file,new{capacity=256,history,trigger});file.Flush(true);}
        File.Move(temporary,Path.Combine(directory,"topology-witnesses.json"),true);
    }
    internal static void ValidateProof(JsonElement witness,ulong[] t,JsonElement lifetimes){
        byte[] Record(string name){
            var r=Convert.FromBase64String(witness.GetProperty(name).GetString()!);Require(r.Length==512,"proof record size");
            ulong hash=14695981039346656037;for(int i=0;i<504;i++)if(i<488||i>=496){hash^=r[i];hash=unchecked(hash*1099511628211);}
            Require(W(r,0)>0&&W(r,0)==W(r,63)&&hash==W(r,61),"proof checksum/commit "+name);return r;
        }
        var create=Record("Create");var record=Record("Record");var begin=Record("SubmitBegin");var submit=Record("Submit");var complete=Record("Complete");var progress=Record("Progress");
        Require(W(create,5)==29&&W(create,9)==1&&W(record,5)==29&&W(record,9)==2&&W(complete,5)==29&&W(complete,9)==3,"proof phases");
        Require(Authority(create).Take(13).SequenceEqual(t.Take(13))&&Authority(record).Take(13).SequenceEqual(t.Take(13))&&Authority(complete).SequenceEqual(t),"proof immutable authority");
        Require(W(record,0)==t[18]&&W(record,23)==t[13]&&W(record,24)==t[14]&&W(complete,0)==t[19]&&W(complete,4)==t[16],"proof origin");
        Require(W(begin,5)==10&&W(begin,6)==0&&W(begin,2)==t[13]&&W(begin,12)==t[14]&&W(begin,11)!=0&&W(begin,13)==t[15],"proof submitted command");
        Require(W(submit,5)==10&&W(submit,6)==1&&W(submit,7)==0&&W(submit,3)==t[13]&&W(submit,11)==t[15]&&W(submit,10)==W(begin,11)&&W(submit,9)==W(begin,9),"proof submit result");
        Require(((W(progress,5)==19&&W(progress,6)==2)||(W(progress,5)==22&&W(progress,6)==1&&W(progress,7)==0))&&W(progress,4)==t[16],"proof completion progress");
        Require(W(create,0)<W(record,0)&&W(record,0)<W(begin,0)&&W(begin,0)<W(submit,0)&&W(submit,0)<W(progress,0)&&W(progress,0)<W(complete,0),"proof event ordering");
        foreach(var pair in new[]{(Handle:t[1],Birth:t[2]),(Handle:t[11],Birth:t[12])}){
            var matches=lifetimes.EnumerateArray().Where(l=>l.GetProperty("Kind").GetUInt64()==3&&l.GetProperty("Handle").GetUInt64()==pair.Handle&&l.GetProperty("BirthSerial").GetUInt64()==pair.Birth).ToArray();
            Require(matches.Length==1&&pair.Birth<W(create,0)&&matches[0].GetProperty("Facts")[0].GetUInt64()>=t[9]&&(matches[0].GetProperty("DeathSerial").ValueKind==JsonValueKind.Null||matches[0].GetProperty("DeathSerial").GetUInt64()>W(complete,0)),"proof resource lifetime");
        }
    }
}
