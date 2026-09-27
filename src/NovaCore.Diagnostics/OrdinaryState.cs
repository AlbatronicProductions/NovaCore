namespace NovaCore.Diagnostics;

/// <summary>Checkpoint is the exact reduced state at its sequence, not speculative worker state.</summary>
public sealed class OrdinaryState
{
    public ulong Sequence {get;set;}
    public long Faults {get;set;}
    public ulong LastProducedObserved {get;set;}
    public long Dropped {get;set;}
    public bool Clean {get;set;}
    public Dictionary<string,OrdinaryEvent> Open {get;set;}=[];
    public Dictionary<ulong,ResourceHistory> Resources {get;set;}=[];
    public Dictionary<ulong,OrdinaryEvent> Context {get;set;}=[];
    public Dictionary<ulong,ResourceHistory> SubmittedResources {get;set;}=[];
    public Dictionary<ulong,OrdinaryEvent> SubmittedContext {get;set;}=[];
    public Dictionary<ulong,OrdinaryEvent> Pending {get;set;}=[];
    public Dictionary<ulong,OrdinaryEvent> Recordings {get;set;}=[];
    public Dictionary<ulong,OrdinaryEvent> LastPhase {get;set;}=[];
    public Dictionary<ulong,Dictionary<ulong,OrdinaryEvent>> PendingContexts {get;set;}=[];
    public OrdinaryEvent? LastSubmit {get;set;}
    public OrdinaryEvent? LastCompletion {get;set;}
    public OrdinaryEvent? LastPresent {get;set;}
    public OrdinaryEvent? LastPublication {get;set;}
    public const int MaximumResources=8192,MaximumOperations=1024;
    public void Apply(OrdinaryEvent e)
    {
        if(e.Sequence!=Sequence+1)throw new InvalidDataException("Noncontiguous minimum record sequence.");
        Sequence=e.Sequence;Faults|=unchecked((long)e.Words[29]);Clean=false;
        if(e.Words[4]>=1&&e.Words[4]<=21)LastPhase[e.Words[4]]=e;else Faults|=(long)OrdinaryFault.Protocol;
        string key=$"{e.Words[4]}:{e.Words[7]}";
        if(e.Edge==OrdinaryEdge.Enter){if(Open.Count>=MaximumOperations||!Open.TryAdd(key,e))Faults|=(long)OrdinaryFault.Protocol;}
        else if(e.Edge is OrdinaryEdge.Return or OrdinaryEdge.Exception){if(!Open.Remove(key))Faults|=(long)OrdinaryFault.Protocol;}
        switch(e.Phase){
            case OrdinaryPhase.Resource:
                if(e.Edge!=OrdinaryEdge.Info)break;
                // data0:1 birth,2 retire,3 bind,4 map,5 unmap. Full payload retained.
                if(e.Words[17]==1){if(e.Words[14]==0||Resources.Count>=MaximumResources||!Resources.TryAdd(e.Words[14],new(e,null,null)))Faults|=(long)OrdinaryFault.IdentityCapacity;}
                else if(e.Words[17]==2){if(Resources.TryGetValue(e.Words[14],out var retiring)&&retiring.Birth.Words[13]==e.Words[13]&&retiring.Birth.Words[18]==e.Words[18]){Resources.Remove(e.Words[14]);if(SubmittedResources.TryGetValue(e.Words[14],out var retained))SubmittedResources[e.Words[14]]=retained with{Retirement=e};Recordings.Remove(e.Words[14]);}else Faults|=(long)OrdinaryFault.Protocol;}
                else if(Resources.TryGetValue(e.Words[14],out var history)&&history.Birth.Words[13]==e.Words[13])Resources[e.Words[14]]=e.Words[17]==3?history with{Binding=e}:history with{Mapping=e};
                else Faults|=(long)OrdinaryFault.Protocol;
                break;
            case OrdinaryPhase.Context:
                if(e.Words[17]==0)Context.Clear();
                else if(Context.Count<128||Context.ContainsKey(e.Words[17]))Context[e.Words[17]]=e;
                else Faults|=(long)OrdinaryFault.IdentityCapacity;
                break;
            case OrdinaryPhase.Submit:
                if(e.Edge==OrdinaryEdge.Enter){
                    SubmittedContext=new(Context);if(Pending.Count==0)SubmittedResources=new(Resources);
                    else foreach(var resource in Resources)Keep(resource.Key,resource.Value);
                    foreach(var c in Context.Values)Retain(c.Words[14]);
                    Retain(e.Words[14]);Retain(e.Words[16]);
                }
                if(e.Edge==OrdinaryEdge.Return&&e.Result==0){LastSubmit=e;if(Pending.Count>=64||!Pending.TryAdd(e.Words[8],e))Faults|=(long)OrdinaryFault.Protocol;else PendingContexts[e.Words[8]]=new(SubmittedContext);}
                break;
            case OrdinaryPhase.Completed:
                if(e.Words[8]==0||!Pending.TryGetValue(e.Words[8],out var submit)||e.Words[13]!=submit.Words[13]||e.Words[14]!=submit.Words[14])Faults|=(long)OrdinaryFault.Protocol;
                else {LastCompletion=e;Pending.Remove(e.Words[8]);PendingContexts.Remove(e.Words[8]);}
                break;
            case OrdinaryPhase.Record:
                if(e.Words[14]!=0){if(Recordings.Count<MaximumOperations||Recordings.ContainsKey(e.Words[14]))Recordings[e.Words[14]]=e;else Faults|=(long)OrdinaryFault.IdentityCapacity;}break;
            case OrdinaryPhase.Present:LastPresent=e;break;
            case OrdinaryPhase.Publication:LastPublication=e;break;
            case OrdinaryPhase.Shutdown:if(e.Edge==OrdinaryEdge.Return&&e.Result==0&&Open.Count==0&&Pending.Count==0&&Faults==0)Clean=true;break;
        }
    }
    void Retain(ulong birth){if(birth==0)return;if(Resources.TryGetValue(birth,out var r)){Keep(birth,r);if(r.Binding is not null&&Resources.TryGetValue(r.Binding.Words[16],out var m))Keep(r.Binding.Words[16],m);}else Faults|=(long)OrdinaryFault.Protocol;}
    void Keep(ulong birth,ResourceHistory r){if(SubmittedResources.Count<MaximumResources||SubmittedResources.ContainsKey(birth))SubmittedResources[birth]=r;else Faults|=(long)OrdinaryFault.IdentityCapacity;}
}
public sealed record ResourceHistory(OrdinaryEvent Birth,OrdinaryEvent? Binding,OrdinaryEvent? Mapping,OrdinaryEvent? Retirement=null);
