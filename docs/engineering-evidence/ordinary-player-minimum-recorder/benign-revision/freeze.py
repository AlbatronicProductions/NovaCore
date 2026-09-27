"""Freeze/check this bounded candidate without rewriting the previous run's identity."""
import pathlib,json,hashlib,subprocess,os,datetime
root=pathlib.Path(__file__).resolve().parents[4];docs=pathlib.Path(__file__).parent;out=root/'build/ordinary-recorder-benign-revision'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def ident(p):return {'bytes':p.stat().st_size,'sha256':sha(p)} if p.is_file() else None
def git(*a):return subprocess.check_output(['git','--no-optional-locks',*a],cwd=root)
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
def digest(d):return hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(d.items())).encode()).hexdigest()
allowed=set('''docs/engineering-evidence/ordinary-player-minimum-recorder/README.md
native/NovaCore.Native/NovaCoreNative.cpp
native/NovaCore.Native/OrdinaryNative.inl
native/NovaCore.Native/OrdinaryRecorder.h
native/NovaCore.Native/OrdinaryRecorderMock.cpp
src/NovaCore.Diagnostics/OrdinaryJournal.cs
src/NovaCore.Diagnostics/OrdinaryObserver.cs
src/NovaCore.Diagnostics/OrdinaryProtocol.cs
src/NovaCore.Diagnostics/OrdinarySession.cs
src/NovaCore.Interop/MinimumRecorder.cs
tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj
tests/NovaCore.Graphics.Tests/Program.cs
tests/NovaCore.MinimumRecorder.Tests/Program.cs
tests/NovaCore.MinimumRecorder.Tests/native-fixture.py
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Application.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.ApplicationQualification.cs
tools/NovaCore.ConstructionEditor/NovaCore.ConstructionEditor.csproj
tools/NovaCore.ConstructionEditor/Program.cs'''.splitlines())
newAllowed=set('''native/NovaCore.Native/OrdinaryMeasurement.h
src/NovaCore.Diagnostics/BenignRecorderRoute.cs
tests/NovaCore.Graphics.Tests/BenignRecorderRouteTests.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.RecorderQualification.cs'''.splitlines())
entry=json.loads((out/'entry.json').read_text());names=sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').decode().split('\0'))-{''})
changed=[p for p,v in entry['files'].items() if ident(root/p)!=v];assert set(changed)<=allowed,changed
new=[p for p in names if p not in entry['files']];assert all(p in newAllowed or p.startswith('docs/engineering-evidence/ordinary-player-minimum-recorder/benign-revision/') for p in new),new
assert git('rev-parse','HEAD').decode().strip()==entry['head']
assert hashlib.sha256(git('show-ref','--head')).hexdigest()==entry['refs']
assert ident(root/git('rev-parse','--git-path','index').decode().strip())==entry['index']
source={p:ident(root/p) for p in names if (p.startswith(('src/','native/','tools/','tests/','samples/','assets/','external/')) or p in ('NovaCore.sln','Directory.Build.props','global.json')) and (root/p).is_file()}
package=root/'tools/NovaCore.App/bin/Release/net10.0-windows';files={p.relative_to(package).as_posix():ident(p) for p in package.rglob('*') if p.is_file()}
identity={'head':entry['head'],'sourceSha256':digest(source),'sourceFiles':len(source),'packageSha256':digest(files),'packageFiles':len(files),'settingsSha256':sha(pathlib.Path(os.environ['LOCALAPPDATA'])/'NovaCore/Launcher/settings.json'),'indexSha256':entry['index']['sha256'],'refsSha256':entry['refs'],'executableSha256':sha(package/'NovaCore.exe'),'nativeSha256':sha(package/'NovaCore.Native.dll')}
if (docs/'identity.json').exists():
 assert identity==json.loads((docs/'identity.json').read_text()),'Frozen identity changed'
 assert source==json.loads((docs/'source-files.json').read_text()) and files==json.loads((docs/'package-files.json').read_text())
 write(out/'postflight-identity.json',identity)
else:
 verify=json.loads((out/'package.json').read_text());assert verify['judgment']=='PASS' and verify['files']==files
 write(docs/'identity.json',identity);write(docs/'source-files.json',source);write(docs/'package-files.json',files)
write(docs/'preservation.json',{'judgment':'PASS','entryFiles':len(entry['files']),'changed':changed,'newProductionAndTests':sorted(set(new)&newAllowed),'unrelatedEntryFilesPreserved':True,'headRefsIndexUnchanged':True})
print(json.dumps(identity,indent=2))
