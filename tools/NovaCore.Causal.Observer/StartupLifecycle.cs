using NovaCore.Interop;

// Clock-independent production policy. The UI allowance is not a GPU deadline.
// A milestone cannot reset a clock: observed timestamps must remain immutable.
internal sealed class StartupLifecycle(long launched, long frequency)
{
    readonly long[] retained = new long[32];
    readonly List<object> transitions = new(12);
    static readonly string[] Names = ["ui-handoff", "loading-handoff", "native-initialization", "first-render-submission", "first-gpu-completion", "steady-rendering"];
    static readonly double[] Limits = [60, 30, 30, 5, 1, 1];
    public string Phase { get; private set; } = "ui-handoff";
    public long SteadyQpc => retained[DiagnosticStartup.Steady];
    public bool Started => SteadyQpc != 0;
    public object Evidence => new { contract=1, launched, frequency, phase=Phase, milestones=retained, transitions,
        deadlines=new { uiHandoffSeconds=60, loadingHandoffSeconds=30, nativeInitializationSeconds=30,
            firstSubmissionSeconds=5, firstCompletionSeconds=1, steadySeconds=1, postClickOverallSeconds=60,
            processOverallSeconds=120, shutdownSeconds=2 } };
    public string? Update(long[] snapshot, long now, bool exited)
    {
        for (int i=6;i<=19;i++) {
            if (retained[i]!=0 && retained[i]!=snapshot[i]) return "startup milestone changed or disappeared";
            if (snapshot[i]!=0 && retained[i]==0) {
                retained[i]=snapshot[i];
                if(i<=15||i==19)transitions.Add(new { slot=i, qpc=snapshot[i], observedQpc=now });
            }
        }
        long shutdown=retained[DiagnosticStartup.Shutdown];
        long horizon=shutdown!=0?Math.Min(now,shutdown):now;
        long prior=launched;
        for(int slot=6;slot<=12;slot++) {
            long stamp=retained[slot];
            if(stamp==0) { if(retained.Skip(slot+1).Take(12-slot).Any(t=>t!=0))return "startup milestone order invalid";break; }
            if(stamp<prior||stamp>now)return "startup timestamp order invalid";
            prior=stamp;
        }
        // UI-ready announces a rendered form, but the UI deadline still starts at process launch.
        for(int p=0;p<6;p++) {
            long begin=p==0?launched:retained[p+6];
            if(begin==0||begin>horizon)break;
            long end=retained[p+7];
            if((Math.Min(end==0?horizon:end,horizon)-begin)/(double)frequency>Limits[p])return Names[p]+" exceeded deadline";
        }
        long startupEnd=retained[12]!=0?Math.Min(horizon,retained[12]):horizon;
        if((startupEnd-launched)/(double)frequency>120)return "overall process startup exceeded 120s";
        if(retained[7]!=0&&(startupEnd-retained[7])/(double)frequency>60)return "overall post-click startup exceeded 60s";
        if(retained[10]!=0&&retained[16]<=0)return "first submission has no rendered frame identity";
        if(retained[11]!=0&&(retained[17]<=0||retained[17]<retained[16]))return "first completion frame identity invalid";
        if(retained[12]!=0&&retained[18]<=retained[17])return "steady rendering lacks subsequent GPU completion";
        if(shutdown!=0) {
            Phase=exited?"exited":"orderly-shutdown";
            if(!exited&&(now-shutdown)/(double)frequency>2)return "orderly shutdown exceeded 2s";
        } else if(exited) return "child exited without orderly lifecycle shutdown";
        else { int phase=0;for(int s=7;s<=12;s++)if(retained[s]!=0)phase++;Phase=phase==6?"steady-rendering":Names[phase]; }
        if(retained[19]!=0&&shutdown==0)return "startup admission revoked";
        return null;
    }
}
