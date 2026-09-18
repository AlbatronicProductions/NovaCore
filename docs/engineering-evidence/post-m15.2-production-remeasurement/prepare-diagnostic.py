"""Generate disposable measurement-only copies. Never edit canonical source.
Run from repository root. CMake/build instructions are in reproduce.md.
"""
from pathlib import Path
import shutil

root = Path(__file__).resolve().parents[3]
out = root / 'build/post-m15.2-production-remeasurement'
native = out / 'diagnostic-native-source'
host = out / 'diagnostic-host'
if native.exists() or host.exists():
    raise SystemExit('Refuse to overwrite existing diagnostic copies')
shutil.copytree(root / 'native/NovaCore.Native', native)
host.mkdir()
for p in (root / 'samples/NovaCore.Triangle').glob('*'):
    if p.is_file() and p.suffix in ('.cs', '.csproj'):
        shutil.copy2(p, host / p.name)

def replace_once(s, old, new):
    if s.count(old) != 1:
        raise ValueError('Source anchor changed: ' + old[:100])
    return s.replace(old, new)

p = native / 'NovaCoreNative.cpp'
s = p.read_text(encoding='utf-8')
s = replace_once(s, 'uint64_t frames = 0;', '''uint64_t frames = 0;
    // Diagnostic-only fixed storage; no in-frame formatting or I/O.
    static std::array<std::array<double,14>,8192> auditRows{};''')
s = replace_once(s, 'auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;', '''auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;
        const std::array<double,8> auditBefore{a.cpuUpdateMs,a.cpuFenceWaitMs,a.cpuInspectionMs,a.cpuHostCallbackMs,a.cpuUploadMs,a.cpuRecordMs,a.cpuSubmitMs,a.cpuPresentMs};''')
s = replace_once(s, '        frames++;', '''        if(frames<auditRows.size()) auditRows[frames]={double(frames),frameMs,a.cpuUpdateMs-auditBefore[0],a.cpuFenceWaitMs-auditBefore[1],a.cpuInspectionMs-auditBefore[2],a.cpuHostCallbackMs-auditBefore[3],a.cpuUploadMs-auditBefore[4],a.cpuRecordMs-auditBefore[5],a.cpuSubmitMs-auditBefore[6],a.cpuPresentMs-auditBefore[7],frames?a.lastGpuTimingMs[0]:-1,frames?a.lastGpuTimingMs[6]:-1,double(a.submission->objectCount),double(a.submission->batchCount)};
        frames++;''')
s = replace_once(s, '    Destroy(a);\n    return NC_SUCCESS;', '''    for(size_t i=0;i<std::min<size_t>(frames,auditRows.size());i++) { std::printf("AUDIT_NATIVE"); for(double x:auditRows[i])std::printf(",%.9f",x); std::printf("\\n"); }
    std::fflush(stdout);
    Destroy(a);
    return NC_SUCCESS;''')
p.write_text(s, encoding='utf-8')

p = host / 'NovaCore.Triangle.csproj'
s = p.read_text(encoding='utf-8').replace('..\\..\\', str(root) + '\\')
s = s.replace('$(MSBuildThisFileDirectory)' + str(root), str(root))
p.write_text(s, encoding='utf-8')

p = host / 'Program.cs'
s = p.read_text(encoding='utf-8')
s = replace_once(s, 's.Contact?.AdvanceLive(e->Input.PauseToggle!=0);', '''var auditFrame=AuditCapture.Count;
    var auditStart=Stopwatch.GetTimestamp();var auditBytes=GC.GetAllocatedBytesForCurrentThread();
    var auditGc0=GC.CollectionCount(0);var auditGc1=GC.CollectionCount(1);var auditGc2=GC.CollectionCount(2);
    s.Contact?.AdvanceLive(e->Input.PauseToggle!=0);''')
s = replace_once(s, 's.Assembly?.AdvanceLive(e->Input.PauseToggle!=0||s.AssemblyAutoStart);s.AssemblyAutoStart=false;', 's.Assembly?.AdvanceLive(e->Input.PauseToggle!=0||auditFrame==60);s.AssemblyAutoStart=false;')
s = replace_once(s, 's.PoweredContact?.AdvanceLive(e->Input.PauseToggle!=0);', '''s.PoweredContact?.AdvanceLive(e->Input.PauseToggle!=0||auditFrame==60||auditFrame==90||auditFrame==120);
    var auditServiceEnd=Stopwatch.GetTimestamp();
    if(AuditCapture.Camera&&s.Assembly is not null){var angle=auditFrame*.01;s.Camera.Position=new(new(1),new(8*Math.Sin(angle),3,8*Math.Cos(angle)));s.Camera.Orientation=DoubleQuaternion.FromAxisAngle(Double3.UnitY,angle)*DoubleQuaternion.FromAxisAngle(Double3.UnitX,-Math.Atan2(3,8));}''')
s = replace_once(s, '    if(!orbitCamera&&s.Log.IsEnabled', '''    AuditCapture.Add(s.Assembly,s.PoweredContact,auditStart,auditServiceEnd,auditBytes,auditGc0,auditGc1,auditGc2,s.Submission.ObjectCount);
    if(!orbitCamera&&s.Log.IsEnabled''')
s = replace_once(s, 'finally{state.Assembly?.Dispose();', 'finally{AuditCapture.Report();state.Assembly?.Dispose();')
s += '''
// Measurement adapter only: timestamps and copied observations, never physics input.
file static class AuditCapture
{
    internal static readonly bool Camera=Environment.GetEnvironmentVariable("NOVACORE_AUDIT_CAMERA")=="1";
    private static readonly double[,] Rows=new double[8192,14];
    internal static int Count;
    internal static void Add(StockAssemblyDevelopmentScene? a,PoweredContactDevelopmentScene? c,long start,long serviceEnd,long bytes,int g0,int g1,int g2,int objects)
    {
        var end=Stopwatch.GetTimestamp();var allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;var i=Count++;
        if(i>=8192)throw new InvalidOperationException("audit capacity");
        Rows[i,0]=i;Rows[i,1]=(serviceEnd-start)*1000d/Stopwatch.Frequency;Rows[i,2]=(end-serviceEnd)*1000d/Stopwatch.Frequency;
        Rows[i,3]=allocated;Rows[i,4]=GC.CollectionCount(0)-g0;Rows[i,5]=GC.CollectionCount(1)-g1;Rows[i,6]=GC.CollectionCount(2)-g2;
        if(a is not null){var o=a.Observation;Rows[i,7]=o.State.Frontier;Rows[i,8]=o.Clock.Time.Ticks;Rows[i,9]=o.State.Actual.MainOn?1:0;Rows[i,10]=o.State.Actual.Jets;Rows[i,11]=a.Completed?2:a.ActiveExhaust?1:0;Rows[i,12]=o.Clock.Debt.Ticks;}
        if(c is not null){var o=c.Observation;Rows[i,7]=o.Actuator.Frontier;Rows[i,8]=o.Clock.Time.Ticks;Rows[i,11]=3;}
        Rows[i,13]=objects;
    }
    internal static void Report(){for(var i=0;i<Count;i++){Console.Write("AUDIT_MANAGED");for(var j=0;j<14;j++){Console.Write(',');Console.Write(Rows[i,j].ToString("R",System.Globalization.CultureInfo.InvariantCulture));}Console.WriteLine();}}
}
'''
p.write_text(s, encoding='utf-8')
print('Generated diagnostic-only native/host copies:', native, host)
