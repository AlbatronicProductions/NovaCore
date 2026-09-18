"""Read-only identity/status/inventory checks; writes concise reports, never deletes or runs physics."""
from pathlib import Path
import json, subprocess, hashlib, zipfile
ROOT=Path('E:/NovaCore'); E=Path(__file__).parent; F=E.parent/'development-profile-feasibility'
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
before=json.loads((E/'preflight.json').read_text())
original=json.loads((E.parent/'identity.json').read_text())
expected=before['head']
refs={x:git('rev-parse',x) for x in ['HEAD','main','origin/main']}
assert all(x==expected for x in refs.values())
remote=git('ls-remote','--refs','origin','refs/heads/main').split()[0]; assert remote==expected
assert git('branch','--show-current')==before['branch']
tags=git('show-ref','--tags').splitlines(); assert tags==before['tags']
assert not git('diff','--cached','--name-only')
for check in [('diff','--check'),('diff','--cached','--check')]: subprocess.run(['git',*check],cwd=ROOT,capture_output=True,check=True)
for s in before['priorStage5Seals']: assert sha(ROOT/s['path'])==s['current'],s['path']
for s in original['stage4Preservation']+original['stage3Preservation']:
    if s['preservedAt']=='current': digest=sha(ROOT/s['path'])
    else:
        archive,name=s['preservedAt'].split('::')
        with zipfile.ZipFile(ROOT/archive) as z: digest=hashlib.sha256(z.read(name)).hexdigest().upper()
    assert digest==s['sha256'],s['path']
for s in before['binaryInputs']: assert sha(ROOT/s['path'])==s['sha256'],s['path']
for name in ['Probe.cs','Probe.csproj']:
    assert sha(E/'reproduction'/name)==sha(ROOT/'build/srv01-stage5-closure/probe'/name)
status=git('status','--short').splitlines()
assert sorted(x for x in status if not x.startswith('??'))==sorted(x for x in before['status'] if not x.startswith('??'))
ksa=Path('E:/Kitten Space Agency')
ksa_files={
 'KSA.dll':'A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8',
 'BepuPhysics.dll':'77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7',
 'BepuUtilities.dll':'E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68',
 'Content/Core/CoreFuelTankAGameData.xml':'4C032E3E89F941BF6C3AD38997E286B7A90568292C04B00216D12D518552F242',
 'Content/Core/CoreFuelTankAAssets.xml':'B3876903B0C6E793767EE3DA2E0F4B6AAFD204099EBD43C9E881FFEB6DDE3266',
 'Content/Core/CoreFuelTankBGameData.xml':'4DA7EA1E554201AC9E9D3F024DC611791CAC656B792F9894FB959465CC8FEC0F',
 'Content/Core/Volatiles.xml':'6602BF06CF6DCCF9FC9D7EC3D0FEBB7F73912F294A0BCB1DBFD6D602FE5002B5',
 'Content/Core/defaultvehicles/Rocket/vehicle.xml':'B94F783D3AE21056559CE49542BDFF64BA788178F2442811302BC311F648D2A3',
 'Content/Core/CorePropulsionAGameData.xml':'D6A7E3EE26ADD68B8CCC572DCCD94ED24B26764C2191C919EF60A86B4924D48B',
 'Content/Core/Reactions.xml':'FEC730270EBA5E1A6DEBA169EAE79F66150AA04409EA90AC5D4D83CB1E814F61'}
for p,digest in ksa_files.items(): assert sha(ksa/p)==digest,p
scratch=[]
for name in ['srv01-display-gap-population','srv01-startup-first-present','srv01-stage2-manual','srv01-stage3','srv01-stage4','srv01-stage5','srv01-stage5-closure']:
    p=(ROOT/'build'/name).resolve(); assert p.parent==(ROOT/'build').resolve()
    files=[f for f in p.rglob('*') if f.is_file()] if p.exists() else []
    scratch.append(dict(path=str(p),exists=p.exists(),files=len(files),bytes=sum(f.stat().st_size for f in files),
        disposition='RETAIN — preceding checkpoint/build dependency; no deletion in this ticket' if name!='srv01-stage5-closure' else 'DISPOSABLE — exact probe sources, seals, results and reproduction retained; not deleted'))
command='Remove-Item -LiteralPath '+','.join("'"+s['path']+"'" for s in scratch)+' -Recurse -Force -ErrorAction Stop'
verify=','.join("'"+s['path']+"'" for s in scratch)+" | ForEach-Object { [pscustomobject]@{ Path=$_; Exists=Test-Path -LiteralPath $_ } }"
cleanup='''# Exact disposable inventory and deferred manual cleanup

No deletion was attempted. The six predecessor output roots remain explicitly retained by Project Control; they include executable/checkpoint dependencies. The new causal-probe root contains rebuildable diagnostic output. Its exact source and results are preserved in this evidence package. The later mission-feasibility gate created no additional build/bin/obj tree.

All targets below were resolved and verified to be immediate children of E:\\NovaCore\\build. No production source, asset, legal review or evidence directory is a cleanup target.

| Exact path | Files | Bytes | Disposition |
|---|---:|---:|---|
'''
cleanup+='\n'.join(f"| `{s['path']}` | {s['files']} | {s['bytes']} | {s['disposition']} |" for s in scratch)
cleanup+=f"\n\nTotal: {sum(s['files'] for s in scratch)} files, {sum(s['bytes'] for s in scratch)} bytes. Removed: 0 files / 0 bytes.\n"
cleanup+='''
## Reviewed manual command — retained for later release, NOT for execution now

Execute only after Project Control releases the six preserved roots and their reproducibility dependencies have been retained/rebuilt elsewhere. Do not use this command to override their current retention requirement. It is printed now to preserve exact cleanup reporting; no deletion workaround or retry was performed.

```powershell
'''+command+'''
```

Non-destructive existence verification (safe now):

```powershell
'''+verify+'''
```

Expected now: all True. After a later authorized complete cleanup: all False. Re-inventory before any later deletion if these paths have changed. Do not retry deletion of absent paths.
'''
(E/'cleanup.md').write_text(cleanup)
evidence=[p for root in [E,F] for p in sorted(root.rglob('*')) if p.is_file() and p.name!='final-verification.json']
output=dict(refs=refs,remoteMain=remote,branch=before['branch'],tags=tags,historicalTagsUnchanged=True,
    stage5Seals=14,stage4Preserved=35,stage3Preserved=29,executableInputsUnchanged=True,
    currentKsaFiles=[dict(path=str(ksa/p),sha256=h) for p,h in ksa_files.items()],
    status=status,trackedChangedPaths=git('diff','--name-only').splitlines(),staged=[],diffCheck='PASS',cachedDiffCheck='PASS',
    productionEditsThisInvestigation=[],permanentTestEditsThisInvestigation=[],
    evidence=[dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=sha(p)) for p in evidence],
    disposable=scratch,disposableFiles=sum(s['files'] for s in scratch),disposableBytes=sum(s['bytes'] for s in scratch),
    removedFiles=0,removedBytes=0,manualCleanupCommand=command,nonDestructiveVerification=verify)
(E/'final-verification.json').write_text(json.dumps(output,indent=2)+'\n')
print(json.dumps({k:output[k] for k in ['refs','remoteMain','branch','stage5Seals','stage4Preserved','stage3Preserved','staged','diffCheck','disposable','disposableFiles','disposableBytes']},indent=2))
