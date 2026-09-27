"""Seal a prospective unbanked source export without touching the main Git index."""
import argparse,hashlib,json,pathlib,shutil,subprocess
p=argparse.ArgumentParser();p.add_argument('--destination',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
root=pathlib.Path.cwd().resolve();destination=a.destination.resolve()
if destination.exists():raise SystemExit('Destination must be absent (never overwrite a previous reproduction).')
if root not in destination.parents or 'build' not in destination.relative_to(root).parts:raise SystemExit('Use a fresh repository build subdirectory.')
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
tracked=set(git('ls-files','-z').decode().split('\0'))
allfiles=sorted(set(git('ls-files','-co','--exclude-standard','-z').decode().split('\0'))-{''})
accepted_evidence={'construction-connector-revision','earth-blackout-closure','earth-camera-blackout-20260922','modular-craft-first-playable','modular-player-revision','release-package-owner','vehicle-construction-architecture','post-blackout-storage-hygiene','final-blackout-build-retirement','stabilization-bank-gauntlet'}
selected=[];withheld=[]
for name in allfiles:
    f=root/name
    if not f.is_file():continue
    if name not in tracked and (name.startswith('docs/legal/') or (name.startswith('docs/engineering-evidence/') and name.split('/')[2] not in accepted_evidence)):
        withheld.append(name);continue
    selected.append(name)
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
manifest={name:{'bytes':(root/name).stat().st_size,'sha256':sha(root/name)} for name in selected}
# Final report manifests are sealed separately, avoiding a self-referential hash.
source={n:v for n,v in manifest.items() if not n.startswith('docs/engineering-evidence/')}
digest=lambda m:hashlib.sha256(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in sorted(m.items())).encode()).hexdigest()
destination.mkdir(parents=True)
for name,v in manifest.items():
    target=destination/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/name,target)
    if sha(target)!=v['sha256']:raise SystemExit('Export mismatch: '+name)
subprocess.run(['git','init','--quiet',str(destination)],check=True)
result={'baseHead':git('rev-parse','HEAD').decode().strip(),'sourceSha256':digest(source),'exportSha256':digest(manifest),'destination':str(destination),'files':manifest,'sourcePaths':list(source),'withheldUnbankedPaths':withheld,'deletedTrackedPaths':sorted(n for n in tracked if n and not (root/n).is_file()),'method':'Exact prospective unbanked file export with resolved LFS bytes and a fresh empty Git root marker. No commits or main index writes. Not a checkout of banked HEAD.'}
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ['files','sourcePaths','withheldUnbankedPaths']},indent=2));print('export files',len(manifest),'bytes',sum(v['bytes'] for v in manifest.values()),'withheld',len(withheld))
