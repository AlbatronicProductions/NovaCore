"""Verify this bounded revision and regenerate concise evidence. No source/Git edits.

Run after verify.py and qualify.ps1 from the preserved original revision tree.
This hashes observed results; it cannot retroactively prove a test was run.
"""
from pathlib import Path
import datetime
import hashlib
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
BUILD = ROOT / 'build/modular-player-revision'
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
CHANGED = set('''NovaCore.sln
native/NovaCore.Native/NovaCoreNative.cpp
native/NovaCore.Native/NovaCoreNative.h
native/NovaCore.Native/shaders/triangle.frag
native/NovaCore.Native/shaders/triangle.vert
samples/NovaCore.Triangle/ConstructionFlightMeasurements.cs
samples/NovaCore.Triangle/ConstructionFlightScene.cs
samples/NovaCore.Triangle/Program.cs
src/NovaCore.Graphics/ReusablePartVisuals.cs
src/NovaCore.Interop/NativeRuntime.cs
tests/NovaCore.Graphics.Tests/ModularViewportTests.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Launch.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.LaunchQualification.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.PersistenceQualification.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Qualification.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.cs
tools/NovaCore.ConstructionEditor/NovaCore.ConstructionEditor.csproj
tools/NovaCore.ConstructionEditor/Program.cs'''.splitlines())
ADDED = set('''samples/NovaCore.Triangle/ApplicationAssemblyInfo.cs
samples/NovaCore.Triangle/IApplicationPresentation.cs
tools/NovaCore.App/NovaCore.App.csproj
tools/NovaCore.App/Program.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Application.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.ApplicationQualification.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Direct.cs
tools/NovaCore.ConstructionEditor/DesktopEditorForm.Saves.cs
tools/NovaCore.ConstructionEditor/PartThumbnail.cs
tools/NovaCore.ConstructionEditor/PlayerApplication.cs'''.splitlines())

def require(ok, reason):
    if not ok:
        raise RuntimeError(reason)

def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT).decode('utf-8')

def record(path, base=ROOT):
    return dict(path=path.relative_to(base).as_posix(), bytes=path.stat().st_size, sha256=sha(path))

def aggregate(rows):
    data=''.join(f"{r['path']}\0{r['bytes']}\0{r['sha256']}\n" for r in sorted(rows,key=lambda r:r['path']))
    return hashlib.sha256(data.encode()).hexdigest()

def stats(path):
    files=[p for p in path.rglob('*') if p.is_file()]
    return dict(path=str(path), files=len(files), bytes=sum(p.stat().st_size for p in files))

entry=read(OUT/'entry.json')
old={r['path']:r for r in entry['files']}
missing=[name for name in old if not (ROOT/name).is_file()]
require(not missing, f'Missing entry files: {missing}')
changed=[dict(record(ROOT/name),entrySha256=row['sha256']) for name,row in old.items() if sha(ROOT/name)!=row['sha256'].lower()]
require({r['path'] for r in changed}==CHANGED, 'Unexpected entry delta; inspect without restoring any file')
names=set(git('ls-files','--cached','--others','--exclude-standard','-z').rstrip('\0').split('\0'))
prefix=OUT.relative_to(ROOT).as_posix()+'/'
new=names-set(old)
require({n for n in new if not n.startswith(prefix)}==ADDED, 'Unexpected added source outside evidence package')
head=git('rev-parse','HEAD').strip()
index=Path(git('rev-parse','--git-path','index').strip())
if not index.is_absolute(): index=ROOT/index
refs=git('show-ref').splitlines()
public=lambda rows: sorted(r for r in rows if ' refs/codex/' not in r)
worktrees=git('worktree','list','--porcelain').splitlines()
require(head==entry['head'], 'HEAD changed')
require(sha(index)==entry['indexSha256'].lower(), 'Git index changed')
require(public(refs)==public(entry['refs']), 'Public refs changed')
require(worktrees==entry['worktrees'], 'Worktree identity changed')
require(sha(ROOT/entry['backup']['path'])==entry['backup']['sha256'].lower(), 'Recovery ZIP changed')
diff=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True)
require(diff.returncode==0, 'git diff --check failed')

def preservation(root, inventory, excluded=()):
    rows=read(inventory); known={r['path'].replace('\\','/'):r for r in rows}
    missing=[n for n in known if not (root/n).is_file()]
    changed=[n for n,r in known.items() if (root/n).is_file() and sha(root/n)!=r['sha256'].lower()]
    added=sorted(p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p.relative_to(root).parts[0].lower() not in excluded and p.relative_to(root).as_posix() not in known)
    require(not missing and not changed, f'Preservation failed for {root}: missing={missing}, changed={changed}')
    return dict(root=str(root),entryFiles=len(rows),changed=changed,missing=missing,added=added,excludedTopLevel=list(excluded))

ksa=preservation(Path('E:/Kitten Space Agency'),BUILD/'ksa-install-entry.json')
require(not ksa['added'],'Unexpected KSA installation additions')
ksa_user=preservation(Path.home()/'Documents/My Games/Kitten Space Agency',BUILD/'ksa-user-entry.json',('logs','crashes','profiler'))
require(ksa_user['added']==['vehicles/NovaCore_UX_Convergence_20260921/meta.toml','vehicles/NovaCore_UX_Convergence_20260921/vehicle.xml'],'Unexpected KSA non-log additions')
settings=Path.home()/'AppData/Local/NovaCore/Launcher/settings.json'
require(settings.read_bytes()==(BUILD/'launcher-settings-before-live.json').read_bytes(),'Launcher preferences changed; preserve and inspect')
write('preservation.json',dict(checkedUtc=NOW,entry=record(OUT/'entry.json'),head=head,indexSha256=sha(index),publicRefsUnchanged=True,worktreesUnchanged=True,entryFiles=len(old),unchangedEntryFiles=len(old)-len(changed),changedEntryFiles=[r['path'] for r in changed],missingEntryFiles=[],addedImplementationFiles=sorted(ADDED),protectedEntryStatusCount=len(entry['status']),diffCheckExit=diff.returncode,appManagedRefChanges=dict(added=sorted(set(refs)-set(entry['refs'])),removed=sorted(set(entry['refs'])-set(refs))),recovery=entry['backup'],ksaInstallation=ksa,ksaUserContent=ksa_user,launcherSettingsSha256=sha(settings),noCleanup=True))

reports=[]
for folder in ('regressions','regressions-final'):
    result=read(BUILD/folder/'results.json')
    for row in result['results']:
        require(row['exitCode']==0,'Recorded regression failure')
        require(sha(ROOT/row['log'])==row['sha256'],'Regression log changed')
    reports.append(dict(report=record(BUILD/folder/'results.json'),checkedUtc=result['checkedUtc'],commands=len(result['results']),results=result['results']))
write('regression-results.json',dict(schema='novacore.modular-player-regressions/1',playerPass=False,reports=reports))

routes=[]; candidates=[]
for config in ('Debug','Release'):
    runtime=ROOT/f'tools/NovaCore.App/bin/{config}/net10.0-windows'
    native=BUILD/f'native-{config.lower()}'
    native_sha=sha(runtime/'NovaCore.Native.dll')
    require(native_sha==sha(native/'NovaCore.Native.dll'),'Native candidate copy mismatch')
    for p in (runtime/'shaders').glob('*.spv'):
        require(sha(p)==sha(native/'shaders'/p.name),'Shader candidate copy mismatch')
    for p in (runtime/'assets/vehicles/modular-starter').rglob('*'):
        if p.is_file(): require(sha(p)==sha(ROOT/'assets/vehicles/modular-starter'/p.name),'Craft content copy mismatch')
    files=[record(p,runtime) for p in sorted(runtime.rglob('*')) if p.is_file()]
    candidates.append(dict(configuration=config,path=str(runtime),files=len(files),bytes=sum(r['bytes'] for r in files),contentSetSha256=aggregate(files),manifest=files))
    for tank in ('short','long'):
        stem=BUILD/f'app-{tank}-{config.lower()}-final'
        row=read(str(stem)+'.json'); process=read(str(stem)+'.process.json')
        log=Path(str(stem)+'.log'); text=log.read_text(encoding='cp1252')
        require(row['judgment']=='APPLICATION_INTEGRATION_PASS' and row['playerPass'] is False and process['exitCode']==0,'Actual application route did not pass')
        require(row['native']==native_sha,'Tested native no longer matches candidate')
        require(process['executableSha256']==sha(runtime/'NovaCore.exe'),'App host changed')
        fingerprints=[]
        for line in text.splitlines():
            if line.startswith('Runtime '):
                match=re.search(r'path=(.*?); bytes=(\d+); sha256=([0-9a-f]{64})',line)
                if match:
                    p=Path(match[1]); require(p.stat().st_size==int(match[2]) and sha(p)==match[3],'Runtime-tested file changed')
                    fingerprints.append(line)
        windows=[json.loads(line.split(' ',1)[1]) for line in text.splitlines() if line.startswith('MODULAR_INTEGRATED_WINDOW ')]
        errors=[line for line in text.splitlines() if 'Vulkan validation [error]' in line]
        warnings=[line for line in text.splitlines() if 'Vulkan validation [warning]' in line]
        require(len(windows)==9 and all(w['complete'] and w['samples']==256 for w in windows),'Incomplete integrated measurements')
        require(not errors,'Vulkan validation error')
        require(all(w['allocatedBytes']['max']==0 for w in row['warmWindows']),'Warm editor allocation changed')
        row.update(configuration=config.lower(),tankSize=tank,flightWindows=windows,processMeasurement=process,logBytes=log.stat().st_size,logSha256=sha(log),validationErrors=errors,validationWarningCount=len(warnings),validationWarningKinds=sorted(set(re.findall(r'\[warning\]\[([^]]+)\]',text))),runtimeFileFingerprints=fingerprints)
        routes.append(row)
write('application-results.json',dict(schema='novacore.modular-player-application/1',playerPass=False,routes=routes))

source=[record(ROOT/n) for n in sorted(names) if not n.startswith(prefix)]
write('source-identity.json',dict(schema='novacore.modular-player-identity/1',checkedUtc=NOW,head=head,entry=record(OUT/'entry.json'),sourceSet=dict(files=len(source),sha256=aggregate(source),algorithm='SHA256 of sorted UTF-8 path + NUL + decimal bytes + NUL + SHA256 + LF; git cached/other nonignored paths excluding this evidence directory'),entryChanges=changed,addedImplementation=[record(ROOT/n) for n in sorted(ADDED)],candidateProvenance='Local native and solution builds; final four actual application routes; no deployment/publish/copy to a release installation',candidates=candidates))
craft=read(BUILD/'visual-review-craft.json')
require(sha(Path(craft['destination']))==craft['sha256'],'Visual review copy changed')
build_groups=[stats(p) if p.is_dir() else record(p) for p in sorted(BUILD.iterdir())]
external=[]
for name in ksa_user['added']:
    p=Path(ksa_user['root'])/name; external.append(dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p),role='Own named KSA reference save; retain'))
external.append(dict(**craft,bytes=Path(craft['destination']).stat().st_size))
package_files=[p for p in OUT.iterdir() if p.is_file() and p.name!='inventory.json']
write('inventory.json',dict(checkedUtc=NOW,units='Logical file bytes; not allocated clusters',evidenceExcludingThisInventory=dict(files=len(package_files),bytes=sum(p.stat().st_size for p in package_files)),build=stats(BUILD),buildGroups=build_groups,external=external,cleanupAuthorized=False,retention='Retain all; entry/recovery and unique evidence protected. Native/managed outputs and historical logs are reproducible candidates, not deletion authorization. Historical one-off refactor scripts are non-idempotent and must not be replayed.'))
print(f'SEAL PASS: {len(old)-len(changed)} preserved entry files; {len(changed)} bounded edits; {len(ADDED)} implementation additions; {len(routes)} application routes; no Player PASS.')
