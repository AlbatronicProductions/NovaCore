// Native-route qualification only. The accepted Ultra/max sustained floor is
// 60 FPS (NOVACORE_CURRENT_STATE.md, accepted-plan.md). CPU wall/present cadence
// includes fence waits; never add GPU duration to it. Preferred targets remain
// reporting goals, not this blackout-route stop boundary.
internal sealed class ProductionFrameWatch(long frequency)
{
    internal const int SustainedMinimumFps=60;
    internal const double FrameBudgetMilliseconds=1000d/SustainedMinimumFps;
    internal sealed record Sample(long Frame,long PreviousFrame,double Milliseconds,long BeginQpc,long EndQpc);
    readonly Queue<Sample> ordinary=new(4);
    readonly HashSet<long> heavy=[];
    long previousFrame,previousQpc;
    internal long Samples{get;private set;}
    internal long DiagnosticIntervalsExcluded{get;private set;}
    internal long DiscontinuitiesExcluded{get;private set;}
    internal Sample? Trigger{get;private set;}
    internal Sample[] TriggerSamples{get;private set;}=[];
    internal object Evidence=>new{minimumFps=SustainedMinimumFps,FrameBudgetMilliseconds,
        recurrence="more than budget in 3 of last 4 ordinary present intervals",
        Samples,DiagnosticIntervalsExcluded,DiscontinuitiesExcluded,Trigger,TriggerSamples};
    internal string? Observe(byte[] record){
        long W(int i)=>BitConverter.ToInt64(record,i*8);
        if(W(5)==27&&W(6)==2&&W(9)==2||W(5)==29&&W(6)==2&&W(9)==2){
            if(heavy.Count>=64)throw new InvalidDataException("Production frame diagnostic ownership capacity");
            heavy.Add(W(2));return null;
        }
        if(W(5) is 3 or 4&&W(6)==0){previousQpc=0;ordinary.Clear();heavy.Clear();return null;}
        if(W(5)!=11||W(6)!=1)return null;
        long frame=W(2),qpc=W(1),prior=previousFrame,began=previousQpc;
        previousFrame=frame;previousQpc=qpc;
        if(W(7)<0){previousQpc=0;ordinary.Clear();return null;}
        if(began!=0&&qpc<=began)throw new InvalidDataException("Production frame clock did not advance");
        bool diagnostic=heavy.Contains(prior)||heavy.Contains(frame);
        heavy.RemoveWhere(id=>id<frame);
        if(began==0||frame!=prior+1){DiscontinuitiesExcluded++;ordinary.Clear();return null;}
        if(diagnostic){DiagnosticIntervalsExcluded++;return null;}
        var sample=new Sample(frame,prior,(qpc-began)*1000d/frequency,began,qpc);Samples++;
        if(ordinary.Count==4)ordinary.Dequeue();ordinary.Enqueue(sample);
        // Integer cross-product preserves the exact 1/60-second boundary.
        if(Trigger==null&&ordinary.Count(s=>(UInt128)(s.EndQpc-s.BeginQpc)*SustainedMinimumFps>(ulong)frequency)>=3){
            Trigger=sample;TriggerSamples=ordinary.ToArray();
            return "ordinary frame cadence below accepted sustained 60 FPS floor (3 of last 4)";
        }
        return null;
    }
}
