"""Create-only diagnostic source; all replacements counted. No production edits."""
from pathlib import Path
import hashlib,json

here=Path(__file__).resolve().parent
old=here.parent/'twelve-sweep-qualification'
def write(name,text):
    p=here/name
    if p.exists(): raise RuntimeError(f'Will not overwrite {p}')
    p.write_text(text,encoding='utf-8',newline='\n')
def replace(s,a,b):
    assert s.count(a)==1,(a,s.count(a))
    return s.replace(a,b)

s=(old/'SelectedOperator.cs.txt').read_text()
s=replace(s,'        var transported=Vector(work);','        ProbeTrace.Take("S0",accepted.Cache);\n        ProbeTrace.Take("S1",Vector(work));\n        var transported=Vector(work);')
s=replace(s,'        var h=Scaled.From(duration.Numerical);','        ProbeTrace.Take("S2",Vector(work));\n        var h=Scaled.From(duration.Numerical);')
s=replace(s,'        var beforeD=Vector(work);','        var beforeD=Vector(work);\n        ProbeTrace.Take("S3",Vector(work));')
s=replace(s,'        if(ordinary)\n        {','        ProbeTrace.Equations(patch,source,free,duration,body,load,ordinary);\n        ProbeTrace.Take("S4",Vector(work));\n        if(ordinary)\n        {')
s=replace(s,'        for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],delta);','        if(ProbeTrace.UseD)for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],delta);\n        ProbeTrace.Take("S5",Vector(work));')
s=replace(s,'        for(int sweep=0;sweep<12;sweep++)','        ProbeTrace.Take("S6",Vector(work));\n        for(int sweep=0;sweep<ProbeTrace.Sweeps;sweep++)')
s=replace(s,'work[6]=twist;\n        }','work[6]=twist;\n            ProbeTrace.Take("sweep-"+(sweep+1),Vector(work));\n        }')
s=replace(s,'        proposal=new(this,accepted,target,dv,dw,proof);','        ProbeTrace.Take("S_FINAL",Vector(work));\n        proposal=new(this,accepted,target,dv,dw,proof);')
write('TraceOperator.cs.txt',s)
c=(old/'CoastDriver.cs').read_text().replace('private static Input InputFrom','internal static Input InputFrom').replace('private static CacheDuration Coast','internal static CacheDuration Coast')
write('InputDriver.cs.txt',c)
base=(old/'SelectedGate.csproj').read_text()
# Explicit compile list protects each executable from accidental default glob inclusion.
base=replace(base,'<OutputType>Exe</OutputType>','<EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>Exe</OutputType>')
base=replace(base,'    <Compile Remove="TwelveOperator.cs;GateQualification.cs" />','    <Compile Include="../twelve-sweep-qualification/FinalPolicy.cs" />\n    <Compile Include="../twelve-sweep-qualification/TwelveTransport.cs" />')
base=replace(base,'<Compile Include="SelectedOperator.cs.txt" />','<Compile Include="../twelve-sweep-qualification/SelectedOperator.cs.txt" />')
base=replace(base,'<Compile Include="SelectedQualification.cs.txt" />','<Compile Include="../twelve-sweep-qualification/SelectedQualification.cs.txt" />\n    <Compile Include="../twelve-sweep-qualification/CoastDriver.cs" />')
write('Reproduce.csproj',base.replace('<StartupObject>Qualification</StartupObject>','<StartupObject>BoundaryDriver</StartupObject>'))
trace=replace(base,'<Compile Include="../twelve-sweep-qualification/SelectedOperator.cs.txt" />','<Compile Include="TraceOperator.cs.txt" />\n    <Compile Include="PairedDriver.cs" />')
trace=replace(trace,'<Compile Include="../twelve-sweep-qualification/CoastDriver.cs" />','<Compile Include="InputDriver.cs.txt" />')
write('Trace.csproj',trace.replace('<StartupObject>Qualification</StartupObject>','<StartupObject>PairedDriver</StartupObject>'))
write('generation.json',json.dumps({'selectedSha256':hashlib.sha256((old/'SelectedOperator.cs.txt').read_bytes()).hexdigest(),'changes':['observation snapshots only','diagnostic UseD and Sweeps controls','two input helpers made internal in disposable driver'],'candidateModified':False},indent=2)+'\n')
