from pathlib import Path
import subprocess,hashlib,json
r=Path(r'E:\NovaCore'); out=r/'build/srv01-stage3'
changes={}
def edit(rel, replacements):
 p=r/rel; b=p.read_bytes(); text=b.decode().replace('\r\n','\n'); changes[p]=b
 for a,z in replacements:
  a=a.replace('\r\n','\n'); z=z.replace('\r\n','\n')
  assert text.count(a)==1,(rel,a,text.count(a))
  text=text.replace(a,z)
 p.write_bytes(text.encode())
probe='LocalContactWorld.Stage3Alloc'
try:
 edit('src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs',[
 ('    private static long nextGeneration;', '    internal static readonly long[] Stage3Alloc = new long[16];\r\n    private static long nextGeneration;'),
 ('        try { simulation.Timestep(dt); }','        Stage3Alloc[4]=GC.GetAllocatedBytesForCurrentThread();\r\n        try { simulation.Timestep(dt); Stage3Alloc[5]=GC.GetAllocatedBytesForCurrentThread(); }'),
 ('        try\r\n        {\r\n            var d = configuration.BoxDimensions;', '''        var diagnosticStepper=(BepuPhysics.DefaultTimestepper)simulation.Timestepper;
        diagnosticStepper.Slept += static (dt,dispatcher)=>Stage3Alloc[10]=GC.GetAllocatedBytesForCurrentThread();
        diagnosticStepper.BeforeCollisionDetection += static (dt,dispatcher)=>Stage3Alloc[11]=GC.GetAllocatedBytesForCurrentThread();
        diagnosticStepper.CollisionsDetected += static (dt,dispatcher)=>Stage3Alloc[12]=GC.GetAllocatedBytesForCurrentThread();
        diagnosticStepper.ConstraintsSolved += static (dt,dispatcher)=>Stage3Alloc[13]=GC.GetAllocatedBytesForCurrentThread();
        try
        {
            var d = configuration.BoxDimensions;''')])
 edit('src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Assemblies.cs',[
 ('                status=PrepareAssemblyInOwnedPhase(authority,out var proposal,false);',f'                {probe}[2]=GC.GetAllocatedBytesForCurrentThread();\r\n                status=PrepareAssemblyInOwnedPhase(authority,out var proposal,false);'),
 ('                var published=PublishAssemblyInOwnedPhase(authority,proposal,false);',f'                {probe}[6]=GC.GetAllocatedBytesForCurrentThread();\r\n                var published=PublishAssemblyInOwnedPhase(authority,proposal,false);'),
 ('count+=published.PublishedCount;',f'count+=published.PublishedCount; {probe}[7]=GC.GetAllocatedBytesForCurrentThread();')])
 edit('tests/NovaCore.Simulation.Tests/AssemblyDepartureTests.cs',[
 ('        s.Engine.ObserveAssemblyFlight(s.Authority,out var before);',f'        {probe}[0]=GC.GetAllocatedBytesForCurrentThread();\r\n        s.Engine.ObserveAssemblyFlight(s.Authority,out var before);'),
 ('        var result=s.Engine.ServiceAssemblyDepartureDebt(s.Authority);\r\n        var observed=',f'        {probe}[1]=GC.GetAllocatedBytesForCurrentThread();\r\n        var result=s.Engine.ServiceAssemblyDepartureDebt(s.Authority);\r\n        var observed='),
 ('        return credit.Status==',f'        {probe}[8]=GC.GetAllocatedBytesForCurrentThread();\r\n        return credit.Status=='),
 ('        using(var measurement=new OrdinaryAllocationMeasurement("departure-contact-handoff"))','''        var native=Field(s.Engine.AssemblyContactWorldForTest(s.Authority)!,"simulation");
        var graphBefore=AllocationGraph(native);
        var diagnosticPool=((BepuPhysics.Simulation)native).BufferPool;
        Console.WriteLine("POOL_BEFORE "+diagnosticPool.GetCapacityForPower(11)+","+diagnosticPool.GetCapacityForPower(12)+","+diagnosticPool.GetCapacityForPower(14));
        using(var measurement=new OrdinaryAllocationMeasurement("departure-contact-handoff"))'''),
 ('OrdinaryAllocationMeasurement.RequireZero(bytes,"departure-contact-handoff");',f'''Console.WriteLine("ATTRIBUTION "+string.Join(",",{probe}));
        var graphAfter=AllocationGraph(native);
        Console.WriteLine("POOL_AFTER "+diagnosticPool.GetCapacityForPower(11)+","+diagnosticPool.GetCapacityForPower(12)+","+diagnosticPool.GetCapacityForPower(14));
        foreach(var item in graphAfter)if(!graphBefore.ContainsKey(item.Key))
        {{
            var a=(Array)item.Key;var element=a.GetType().GetElementType()!;
            GC.KeepAlive(Array.CreateInstance(element,a.Length));
            var start=GC.GetAllocatedBytesForCurrentThread();var clone=Array.CreateInstance(element,a.Length);var size=GC.GetAllocatedBytesForCurrentThread()-start;GC.KeepAlive(clone);
            Console.WriteLine("NEW "+item.Value+" TYPE="+a.GetType()+" LENGTH="+a.Length+" ALLOCATION_SIZE="+size);
            foreach(var old in graphBefore)if(old.Value==item.Value)Console.WriteLine("OLD LENGTH="+((Array)old.Key).Length);
        }}
        OrdinaryAllocationMeasurement.RequireZero(bytes,"departure-contact-handoff");'''),
 ('    internal static void DepartureAllocation()', '''    private static Dictionary<object,string> AllocationGraph(object root)
    {
        var found=new Dictionary<object,string>(ReferenceEqualityComparer.Instance);
        void Walk(object? value,string path,int depth)
        {
            if(value is null||depth>30)return;
            var type=value.GetType();
            if(type.IsPrimitive||type.IsEnum||value is string||value is Type||value is Delegate||type==typeof(System.Reflection.Pointer)||type==typeof(IntPtr)||type==typeof(UIntPtr))return;
            if(!type.IsValueType&&!found.TryAdd(value,path))return;
            if(value is Array a)
            {
                var element=type.GetElementType()!;
                if(element.IsPrimitive||element.IsPointer||element.IsEnum)return;
                for(var i=0;i<a.Length;i++)Walk(a.GetValue(i),path+"["+i+"]",depth+1);
                return;
            }
            for(var t=type;t is not null;t=t.BaseType)
            foreach(var field in t.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly))
                if(!field.FieldType.IsPointer)Walk(field.GetValue(value),path+"."+field.Name,depth+1);
        }
        Walk(root,"Simulation",0);return found;
    }

    internal static void DepartureAllocation()''')])
 with (out/'attribution-build.log').open('w') as f:
  p=subprocess.run(['dotnet','build','tests/NovaCore.Simulation.Tests','-c','Debug','--artifacts-path',str(out/'attribution'),'-p:ContinuousIntegrationBuild=true','--nologo','-v:q'],cwd=r,stdout=f,stderr=subprocess.STDOUT)
 assert p.returncode==0,'build failed'
finally:
 for p,b in changes.items():p.write_bytes(b)
 (out/'attribution-restoration.json').write_text(json.dumps({str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in changes},indent=2))
with (out/'attribution-debug.log').open('w') as f:
 p=subprocess.run(['dotnet',str(out/'attribution/bin/NovaCore.Simulation.Tests/debug/NovaCore.Simulation.Tests.dll'),'--assembly-departure-validation'],cwd=r,stdout=f,stderr=subprocess.STDOUT)
print((out/'attribution-debug.log').read_text())
