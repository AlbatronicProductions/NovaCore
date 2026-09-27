// Qualification thresholds, not renderer/recovery timeouts. All storage belongs
// to the observer process. Never adapts upward to a slower scene.
internal sealed class CaptureCostWatch(long frequency,bool retainedReplay=false,TopologyWitnessWatch? topology=null,bool qualifyOrdinaryFence=true)
{
    internal const double RepeatingFenceMilliseconds=6,RepeatingCompletionMilliseconds=1;
    internal sealed record Sample(long Frame,long Submission,long Completed,long Capture,long Generation,long Pupil,double Milliseconds,byte[] Begin,byte[] End)
    {
        public string Scope {get;init;}="fence";
        public byte[]? Authority {get;init;}
        public byte[]? Copy {get;init;}
    }
    internal sealed record Capture(long Identity,long Frame,long Generation,long Pupil,long PayloadBytes);
    sealed class Pending(Capture owner){
        internal readonly Capture Owner=owner;
        internal readonly byte[] Header=new byte[4096];
        internal int Fragments;
        internal long Submission;
        internal byte[]? Copy,Completion,Packing;
        internal long H(int i)=>BitConverter.ToInt64(Header,i*8);
    }
    internal Capture? LatestScheduled {get;private set;}
    readonly Dictionary<long,Pending> scheduled=[];
    readonly Queue<Sample> captures=new(4),ordinary=new(4),completions=new(4);
    byte[]? begin;
    long submissionSequence,firstScheduledFrame;
    internal long ExcludedLeadingRecords {get;private set;}
    internal IReadOnlyCollection<Sample> TriggerSamples {get;private set;}=[];
    internal Sample? Trigger {get;private set;}
    internal Sample? OrdinaryTrigger {get;private set;}
    internal IReadOnlyCollection<Sample> OrdinaryTriggerSamples {get;private set;}=[];
    static void Require(bool ok,string why){if(!ok)throw new InvalidDataException("Capture cost ownership: "+why);}
    Pending? Find(long frame){
        if(scheduled.TryGetValue(frame,out var p))return p;
        if(retainedReplay&&(firstScheduledFrame==0||frame<firstScheduledFrame)){ExcludedLeadingRecords++;return null;}
        throw new InvalidDataException("Capture cost ownership: missing scheduled frame "+frame);
    }
    string? Add(Queue<Sample> window,Sample sample,double threshold,string reason){
        Require(double.IsFinite(sample.Milliseconds)&&sample.Milliseconds>=0,"invalid timing");
        if(window.Count==4)window.Dequeue();window.Enqueue(sample);
        if(ReferenceEquals(window,ordinary)){
            if(!qualifyOrdinaryFence)return null; // Native route uses complete frame cadence, not a diagnostic fence budget.
            if(OrdinaryTrigger==null&&window.Count(s=>s.Milliseconds>threshold)>=3){OrdinaryTrigger=sample;OrdinaryTriggerSamples=window.ToArray();return reason;}
        }else if(Trigger==null&&window.Count(s=>s.Milliseconds>threshold)>=3){Trigger=sample;TriggerSamples=window.ToArray();return reason;}
        return null;
    }
    internal string? Observe(byte[] record)
    {
        long W(int i)=>BitConverter.ToInt64(record,i*8);
        if(W(5)==27&&W(6)==2&&W(9)==2){
            var c=new Capture(W(10),W(11),W(13),W(14),W(12));
            Require(c.Identity>0&&c.Frame>0&&c.PayloadBytes>0&&scheduled.Count<64,"invalid/overflow schedule");
            Require(LatestScheduled==null||(c.Identity>LatestScheduled.Identity&&c.Frame>LatestScheduled.Frame),"reused capture identity/frame");
            Require(scheduled.TryAdd(c.Frame,new(c)),"duplicate scheduled frame");
            if(firstScheduledFrame==0)firstScheduledFrame=c.Frame;LatestScheduled=c;
        }
        if(W(5)==26&&W(6)==2&&scheduled.TryGetValue(W(9),out var a)){
            long part=W(10),bytes=W(13);
            Require(part==a.Fragments&&W(11)==11&&W(12)==4096&&bytes==Math.Min(376,4096-part*376),"authority fragment");
            record.AsSpan(112,(int)bytes).CopyTo(a.Header.AsSpan((int)part*376));a.Fragments++;
            if(a.Fragments==11)Require(a.H(3)==a.Owner.Identity&&a.H(4)==a.Owner.Frame&&a.H(11)==a.Owner.Generation&&a.H(15)==a.Owner.Pupil&&a.H(16)==a.H(15)&&a.H(17)==a.H(15)&&a.H(48)==a.Owner.PayloadBytes&&a.H(45) is >=0 and <=4,"authority scalar disagreement");
            if(a.Fragments==11)topology?.ValidateCapture(a.Header);
        }
        if(W(5)==10&&W(6)==1&&W(7)==0){
            submissionSequence=W(11);
            if(scheduled.TryGetValue(W(3),out var p)){
                Require(p.Submission==0&&p.Fragments==11&&submissionSequence>0&&W(10)!=0,"submission/authority missing or duplicate");p.Submission=submissionSequence;
            }
        }
        if(W(5)==27&&W(6)==2&&W(9) is 3 or 6 or 8 or 10){
            var p=Find(W(11));if(p==null)return null;
            Require(W(10)==p.Owner.Identity&&p.Submission>0&&p.Fragments==11,"completion identity/submission");
            if(W(9)==10){
                Require(p.H(45)==4&&p.Packing==null&&p.Copy==null&&p.Completion==null&&W(7)==0&&W(4)==p.Owner.Frame&&W(12) is >=0 and <2&&W(13)!=0,"packing completion ordering/owner");
                Require(W(14)==0x344b5046&&W(15)==0&&W(16)==p.H(18)&&W(17)==p.H(19)&&W(18)>=0&&W(18)<=p.H(19)*3&&W(18)%3==0&&
                    (W(19)|(W(20)<<32))==p.Owner.Frame&&(W(21)|(W(22)<<32))==p.Owner.Identity&&
                    W(23)==48*p.H(18)+16*Math.Min(p.H(18),4)+4*p.H(19)+4*W(18)+252&&W(24)==64*p.H(18)+4*p.H(19)+4*W(18)+188&&
                    W(25)==(p.H(18)-Math.Min(p.H(18),4))*4&&W(26)==0&&W(27)==0&&W(28)==0&&W(29)==0,"packing GPU authority");p.Packing=(byte[])record.Clone();
            }else if(W(9)==8){
                Require(p.Copy==null&&p.Completion==null&&p.H(45)>0,"copy telemetry ordering");
                Require((p.H(45)!=4||p.Packing!=null)&&W(12)==(p.H(45)==4?BitConverter.ToInt64(p.Packing!,23*8):p.H(52))&&W(14)==p.H(54)&&W(15)==p.H(35)&&W(16)==p.H(33)&&W(17) is >=0 and <2,"copy resource/slot disagreement");
                if(p.Packing!=null)Require(W(17)==BitConverter.ToInt64(p.Packing,12*8),"packing/copy slot disagreement");
                Require(W(13)>=0&&(p.H(45)<2||W(13)==0),"host copy disagreement");p.Copy=(byte[])record.Clone();
            }else if(W(9)==3){
                Require(p.Completion==null&&W(12)==p.Submission&&W(13)==p.Owner.Generation&&W(14)==p.Owner.Pupil&&W(4)==p.Owner.Frame,"completed authority disagreement");
                Require(p.H(45)==0||p.Copy!=null,"missing copy completion");p.Completion=(byte[])record.Clone();
            }else{
                Require(p.Completion!=null&&W(12)==p.Owner.PayloadBytes&&W(4)==p.Owner.Frame,"missing completion or cost payload disagreement");
                double gpu=BitConverter.ToDouble(record,13*8),cpu=BitConverter.ToDouble(record,14*8);
                Require(double.IsFinite(gpu)&&gpu>=0,"invalid GPU cost");
                var sample=new Sample(p.Owner.Frame,p.Submission,W(4),p.Owner.Identity,p.Owner.Generation,p.Owner.Pupil,cpu,p.Completion!,(byte[])record.Clone()){
                    Scope="completion",Authority=p.Header,Copy=p.Copy};
                var fault=Add(completions,sample,RepeatingCompletionMilliseconds,"repeated capture completion CPU cost exceeds 1ms (3 of last 4)");
                scheduled.Remove(p.Owner.Frame);return fault;
            }
        }
        if(W(5)!=24)return null;
        if(W(6)==0){Require(begin==null,"nested fence begin");begin=(byte[])record.Clone();return null;}
        if(W(6)!=1||begin==null)return null;
        double ms=(W(1)-BitConverter.ToInt64(begin,8))*1000d/frequency;
        long frame=W(3);Require(BitConverter.ToInt64(begin,24)==frame,"fence frame disagreement");scheduled.TryGetValue(frame,out var owner);
        if(retainedReplay&&(firstScheduledFrame==0||frame<firstScheduledFrame)){ExcludedLeadingRecords++;begin=null;return null;}
        var wait=new Sample(frame,owner?.Submission??submissionSequence,W(4),owner?.Owner.Identity??0,owner?.Owner.Generation??0,owner?.Owner.Pupil??0,ms,begin,(byte[])record.Clone());begin=null;
        return Add(owner==null?ordinary:captures,wait,RepeatingFenceMilliseconds,$"repeated {(owner==null?"noncapture":"capture")} fence cost exceeds 6ms (3 of last 4)");
    }
}
