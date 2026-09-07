"""Private test copy: expose a compound assertion without relaxing it."""
import hashlib,json,pathlib,subprocess,sys,xml.etree.ElementTree as ET,shutil
sys.dont_write_bytecode=True
from prepare import ROOT,HERE,OUT,sha
from validate import canonical
def main():
    source=ROOT/'tests/NovaCore.Graphics.Tests';private=OUT/'anchor-test';private.mkdir(parents=True,exist_ok=True)
    text=(source/'Program.cs').read_text()
    assertion='    Check(maximumAuthorityError==0d&&terrainChecksum>=0d&&enu.IsValid&&allocated==0,"production SurfaceAnchor authority precision and allocation");'
    assert text.count(assertion)==1
    text=text.replace(assertion,'    Console.WriteLine($"Anchor predicate: maximumAuthorityError={maximumAuthorityError:R}; terrainChecksum={terrainChecksum:R}; enuValid={enu.IsValid}; allocated={allocated}");\n'+assertion)
    (private/'Program.cs').write_text(text)
    project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');properties=ET.SubElement(project,'PropertyGroup')
    for key,value in {'OutputType':'Exe','PlatformTarget':'x64','AllowUnsafeBlocks':'true','AssemblyName':'NovaCore.Graphics.Tests','EnableDefaultCompileItems':'false'}.items():ET.SubElement(properties,key).text=value
    items=ET.SubElement(project,'ItemGroup')
    for path in [*source.glob('*.cs'),*[(source/c.attrib['Include']).resolve() for c in ET.parse(source/'NovaCore.Graphics.Tests.csproj').findall('.//Compile')]]:
        ET.SubElement(items,'Compile',Include=str(private/'Program.cs' if path==source/'Program.cs' else path))
    deployment=source/'bin/Release/net10.0'
    for name in ['NovaCore.Core','NovaCore.Graphics','NovaCore.Interop','NovaCore.Simulation']:
        reference=ET.SubElement(items,'Reference',Include=name);ET.SubElement(reference,'HintPath').text=str(deployment/(name+'.dll'))
    ET.ElementTree(project).write(private/'probe.csproj',encoding='utf-8')
    subprocess.run(['dotnet','build',str(private/'probe.csproj'),'-c','Release','-v','quiet'],cwd=ROOT,check=True)
    folder=private/'bin/Release/net10.0';shutil.copy2(deployment/'NovaCore.Native.dll',folder/'NovaCore.Native.dll')
    result={'sourceHash':sha(source/'Program.cs'),'privateSourceHash':sha(private/'Program.cs'),'privateTestHash':sha(folder/'NovaCore.Graphics.Tests.dll'),'nativeHash':sha(folder/'NovaCore.Native.dll'),'environment':'canonical; default JIT settings','attempts':[]}
    env=canonical()
    for index in range(20):
        run=subprocess.run([str(folder/'NovaCore.Graphics.Tests.exe'),'--test=Canonical SurfaceAnchor physical terrain authority'],cwd=ROOT,env=env,text=True,capture_output=True)
        log=run.stdout+run.stderr
        record={'attempt':index,'exitCode':run.returncode,'logSha256':hashlib.sha256(log.encode()).hexdigest(),'lines':[x for x in log.splitlines() if any(t in x for t in ['Anchor predicate:','Canonical SurfaceAnchor terrain:','Exception:','PASS','FAIL'])]}
        result['attempts'].append(record);print(record,flush=True)
    (HERE/'anchor-predicate-probe.json').write_text(json.dumps(result,indent=2)+'\n')
if __name__=='__main__':main()
