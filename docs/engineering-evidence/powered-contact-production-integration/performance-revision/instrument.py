"""Disposable timestamp instrumentation; exact-byte restore, no physics edits."""
from pathlib import Path
import json, hashlib, re, sys
R = Path('E:/NovaCore')
S = R/'build/powered-contact-performance-revision'
S.mkdir(parents=True, exist_ok=True)
M = S/'originals.json'
probe = 'src/NovaCore.Simulation/PoweredContactCostProbe.cs'
harness = 'tests/NovaCore.Simulation.Tests/PoweredContactTests.CostProbe.cs'
if sys.argv[1] == 'restore':
    data = json.loads(M.read_text())
    for path, hexdata in data.items():
        (R/path).write_bytes(bytes.fromhex(hexdata))
    for path in [probe,harness]:
        (R/path).unlink()
    print('RESTORED',len(data),'original files; removed two temporary diagnostic source files')
    sys.exit()
if M.exists(): raise RuntimeError('Existing originals; do not overwrite')
originals = {}
pending = {}
def read(path):
    b=(R/path).read_bytes(); originals[path]=b.hex()
    return b.decode('utf-8-sig').replace('\r\n','\n')
def write(path,s): pending[path]=s
def method(s,signature,bucket):
    start=s.index(signature); brace=s.index('{',start)
    return s[:brace+1]+f'\n        using var diagnosticScope = NovaCore.Simulation.PoweredContactCostProbe.Enter({bucket});'+s[brace+1:]
def once(s,a,b):
    if s.count(a)!=1: raise RuntimeError((a,s.count(a)))
    return s.replace(a,b)
def patch(path,edits):
    s=read(path)
    for signature,bucket in edits: s=method(s,signature,bucket)
    write(path,s)
tx='src/NovaCore.Simulation/Transactions/'
contact='src/NovaCore.Simulation/Spacecraft/Contact/Staging/'
path=tx+'SimulationTransactionEngine.PoweredFreeFlight.cs'
s=read(path)
for sig,bucket in [('private PoweredFlightStatus CheckPoweredSource(',11),('private PoweredFlightResult AdmitPoweredTime(',0),('private PoweredFlightStatus PreparePoweredFlightInOwnedPhase(',6),('private PoweredFlightResult PublishPoweredFlightInOwnedPhase(',6),('private PoweredFlightResult ServicePoweredDebt(',0)]: s=method(s,sig,bucket)
s=once(s,'        _state.InstallAppliedEndpoint(slot, endpoint, record.StateRevision);','        using (NovaCore.Simulation.PoweredContactCostProbe.Enter(7))\n        {\n        _state.InstallAppliedEndpoint(slot, endpoint, record.StateRevision);')
s=once(s,'        p.Active = false; p.ResourceLease = default; p.Prepared = default;','        p.Active = false; p.ResourceLease = default; p.Prepared = default;\n        }')
s=once(s,'            p.ExpectedPhysical = successorPhysical; p.ExpectedResource = successorResource;','            using var diagnosticAcknowledgement = NovaCore.Simulation.PoweredContactCostProbe.Enter(8);\n            p.ExpectedPhysical = successorPhysical; p.ExpectedResource = successorResource;')
write(path,s)
patch(tx+'SimulationTransactionEngine.EnginePreparation.cs', [('private EnginePreparationStatus PrepareSingleEngineActuationInOwnedPhase(',1),('private EnginePreparationStatus ReadSingleEngineActuationInOwnedPhase(',1)])
patch(tx+'SimulationTransactionEngine.Propellant.cs', [('private bool PropellantMassMatches(',9),('private PropellantPreparationStatus PrepareFinitePropellantInOwnedPhase(',1),('private PropellantPreparationStatus ReadFinitePropellantInOwnedPhase(',1)])
patch(tx+'SimulationTransactionEngine.SpacecraftCommands.cs',[('private SpacecraftCommandCommit CommitNextSpacecraftCommandInOwnedPhase(',1),('private SpacecraftCommandStatus CloseSpacecraftCommandBoundaryInOwnedPhase(',1)])
patch(contact+'LocalContactWorld.Powered.cs',[('private PoweredFlightStatus ComparePoweredAuthority(',3),('internal PoweredFlightStatus PreparePoweredInput(',3),('internal void InstallPoweredInput(',8),('internal PoweredFlightStatus ReadPoweredEndpoint(',5),('internal double PoweredPenetration()',5),('internal void Commit()',8)])
path=contact+'LocalContactWorld.cs';s=read(path)
s=method(s,'internal LocalContactStatus Step(',3)
s=once(s,'try { simulation.Timestep(dt); }','try { using var diagnosticNative = NovaCore.Simulation.PoweredContactCostProbe.Enter(4); simulation.Timestep(dt); }')
s=once(s,'        var state = simulation.Bodies.GetBodyReference(body);\n        var p = FromFloat','        using var diagnosticExport = NovaCore.Simulation.PoweredContactCostProbe.Enter(5);\n        var state = simulation.Bodies.GetBodyReference(body);\n        var p = FromFloat')
write(path,s)
patch('src/NovaCore.Simulation/Spacecraft/Actuation/OrdinaryContactInputProjection.cs',[('internal static bool TryMap(',2)])
patch('src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs',[('internal static bool TryRatio(',10)])
path='tests/NovaCore.Simulation.Tests/Program.cs';s=read(path)
s=once(s,'if (args.Contains("--powered-contact-cost",','if (args.Contains("--powered-contact-cost-probe", StringComparer.Ordinal)) { PoweredContactTests.CostProbe(); return; }\nif (args.Contains("--powered-contact-cost",')
write(path,s)
write(probe,'''using System.Diagnostics;
namespace NovaCore.Simulation;
internal static class PoweredContactCostProbe
{
    internal const int Count = 12;
    internal static readonly long[] Ticks = new long[Count];
    internal static readonly long[] Calls = new long[Count];
    internal static readonly long[] Parents = new long[Count * Count];
    internal static bool Enabled;
    private static int current;
    private static long previous, start;
    internal static void Begin() { Array.Clear(Ticks); Array.Clear(Calls); Array.Clear(Parents); current=0; Enabled=true; previous=start=Stopwatch.GetTimestamp(); }
    internal static long End() { var now=Stopwatch.GetTimestamp(); Ticks[current]+=now-previous; Enabled=false; return now-start; }
    internal static Scope Enter(int next)
    {
        if (!Enabled) return new(-1,0,0);
        var now=Stopwatch.GetTimestamp(); Ticks[current]+=now-previous; previous=now;
        var parent=current; current=next; Calls[next]++; return new(parent,next,now);
    }
    internal readonly struct Scope(int parent, int phase, long entered) : IDisposable
    {
        public void Dispose()
        {
            if(parent<0) return;
            var now=Stopwatch.GetTimestamp(); Ticks[current]+=now-previous; Parents[parent*Count+phase]+=now-entered;
            current=parent; previous=now;
        }
    }
}
''')
write(harness,'''using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
internal static partial class PoweredContactTests
{
    internal static void CostProbe()
    {
        using var prime=new Contact(); for(var i=0;i<128;i++) Complete(prime);
        using var c=new Contact(PoweredContactFixture.CenteredBaseline);
        for(var i=0;i<128;i++) Complete(c);
        var names=new[]{"A_HostResidual","B_EngineResource","C_MapperExclusive","D_ContactValidation","E_BEPU","F_ExportSeal","G_TransactionPrepareValidate","H_CanonicalWrites","I_AckBodyUpdate","J_MassReconstruction","K_Ratio","L_PoweredSource"};
        var total=new double[1024]; var phases=new double[1024][]; var parents=new long[144]; var calls=new long[12]; var allocations=new long[1024];
        for(var i=0;i<1024;i++) phases[i]=new double[12];
        // Initialize diagnostic class outside first measured window.
        PoweredContactCostProbe.Begin(); PoweredContactCostProbe.End();
        var thread=Environment.CurrentManagedThreadId; var gc=Collections();
        for(var i=0;i<1024;i++)
        {
            var alloc=GC.GetAllocatedBytesForCurrentThread();
            PoweredContactCostProbe.Begin(); Complete(c); var elapsed=PoweredContactCostProbe.End();
            allocations[i]=GC.GetAllocatedBytesForCurrentThread()-alloc;
            total[i]=elapsed*1000d/Stopwatch.Frequency;
            long sum=0;
            for(var j=0;j<12;j++){ sum+=PoweredContactCostProbe.Ticks[j]; phases[i][j]=PoweredContactCostProbe.Ticks[j]*1000d/Stopwatch.Frequency; calls[j]+=PoweredContactCostProbe.Calls[j]; }
            for(var j=0;j<144;j++) parents[j]+=PoweredContactCostProbe.Parents[j];
            if(sum!=elapsed || Environment.CurrentManagedThreadId!=thread) throw new InvalidOperationException("Probe partition/thread changed");
        }
        var after=Collections();
        // Diagnostic-enabled and disabled runs share exact production arithmetic/order.
        using var reference=new Contact(PoweredContactFixture.CenteredBaseline);
        for(var i=0;i<1152;i++) Complete(reference);
        for(var i=0;i<1152;i++)
        {
            Check(c.Engine.TryGetPoweredFlightRecord(i,out var a) && reference.Engine.TryGetPoweredFlightRecord(i,out var b) && a==b && a.Endpoint.SameBits(b.Endpoint),"probe history/physical/resource bits unchanged");
        }
        Check(NativeBits(c).SequenceEqual(NativeBits(reference)),"probe native bits unchanged");
        static object Stats(double[] values) { var a=(double[])values.Clone(); Array.Sort(a);return new{median=a[512],p95=a[972],p99=a[1013],max=a[1023],mean=a.Average()}; }
        var report=new { population="UNPOWERED STILL-FUELLED RETAINED-CONTACT CONTROL",warm=128,samples=1024,thread,
            gc=after.Zip(gc,(a,b)=>a-b).ToArray(),allocationMax=allocations.Max(),allocationTotal=allocations.Sum(),equivalence="1152 exact history/endpoint/native matches",
            complete=Stats(total),phases=names.Select((name,j)=>new{name,calls=calls[j],statistics=Stats(phases.Select(a=>a[j]).ToArray())}).ToArray(),
            nested=names.SelectMany((name,j)=>names.Select((child,k)=>new{parent=name,child,total_ms=parents[j*12+k]*1000d/Stopwatch.Frequency})).Where(x=>x.total_ms!=0).ToArray(),
            slow=Enumerable.Range(0,1024).OrderByDescending(i=>total[i]).Take(16).Select(i=>new{index=i,total_ms=total[i],phase_ms=phases[i],allocation=allocations[i]}).ToArray()};
        Console.WriteLine(JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    }
}
''')
M.write_text(json.dumps(originals),encoding='utf-8')
for path,s in pending.items(): (R/path).write_text(s,encoding='utf-8',newline='\n')
print('INSTRUMENTED',len(originals),'original files plus two diagnostic files')
