// Optional, explicitly authorized native-test policy. It changes diagnostic
// admission only; production faults and deadlines never enter this exception.
internal sealed class AdaptiveCapturePolicy(bool enabled,long frequency)
{
    internal bool Enabled=>enabled;
    internal long RequestSerial{get;private set;}
    internal long RequestQpc{get;private set;}
    internal long AckQpc{get;private set;}
    internal long DrainQpc{get;private set;}
    internal long Cutoff{get;private set;}
    internal bool Requested=>RequestSerial!=0;
    internal bool Drained=>DrainQpc!=0;
    internal static bool IsCost(string fault)=>fault.StartsWith("repeated capture fence cost",StringComparison.Ordinal)||fault.StartsWith("repeated capture completion CPU cost",StringComparison.Ordinal)||fault.StartsWith("repeated topology witness",StringComparison.Ordinal);
    internal object Evidence=>new{enabled,mode=Drained?"black-box":Requested?"draining":"packed-capture",RequestSerial,RequestQpc,AckQpc,DrainQpc,Cutoff};
    internal bool Request(string fault,long serial,long now){
        bool cost=IsCost(fault);
        if(!enabled||!cost||Requested)return false;
        RequestSerial=serial;RequestQpc=now;return true;
    }
    internal void Observe(byte[] record){
        long W(int i)=>BitConverter.ToInt64(record,i*8);
        if(W(5)==27&&W(6)==2&&W(9)==2&&AckQpc!=0)throw new InvalidDataException("Capture admitted after disable acknowledgement");
        if(W(5)==29&&W(6)==2&&W(9) is 1 or 2&&AckQpc!=0)throw new InvalidDataException("Topology witness admitted after disable acknowledgement");
        if(W(5)!=30)return;
        if(!Requested||W(6)!=2||W(7)!=0||W(10)!=RequestSerial)throw new InvalidDataException("Invalid capture-control acknowledgement");
        if(W(9)==1){if(AckQpc!=0||W(11)<W(12))throw new InvalidDataException("Capture admission cutoff invalid");AckQpc=W(1);Cutoff=W(11);}
        else if(W(9)==2){if(AckQpc==0||Drained||W(11)!=Cutoff||W(12)!=Cutoff||W(13)!=0||W(14)!=0)throw new InvalidDataException("Capture drain ownership invalid");DrainQpc=W(1);}
        else throw new InvalidDataException("Unknown capture-control operation");
    }
    internal string? Check(long now,long durable,long audited){
        if(!Requested)return null;
        if(AckQpc==0&&(now-RequestQpc)>frequency)return "capture admission disable not acknowledged within existing 1s progress bound";
        if((!Drained||durable<Cutoff||audited<Cutoff)&&(now-RequestQpc)>3*frequency)return "pending capture drain exceeded existing 3s publication bound";
        return null;
    }
    internal string? CompletionFailure(long durable,long audited)=>Requested&&
        (AckQpc==0||!Drained||durable!=Cutoff||audited!=Cutoff)
        ?"capture fallback ended without acknowledged, drained and audited cutoff":null;
}
