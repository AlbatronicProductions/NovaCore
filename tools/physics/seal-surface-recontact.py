"""Read-only source/package/preservation seal for the unbanked contact candidate."""
import argparse, hashlib, json, pathlib, subprocess

p=argparse.ArgumentParser()
p.add_argument('--output',type=pathlib.Path,required=True)
p.add_argument('--compare',type=pathlib.Path)
a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[2]
def git(*args): return subprocess.check_output(['git',*args],cwd=root).decode().strip()
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def digest(files): return hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(files.items())).encode()).hexdigest()
paths=sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').split('\0'))-{''})
entry=json.loads((root/'build/surface-recontact/entry.json').read_text(encoding='utf-8-sig'))
current={k:{'bytes':(root/k).stat().st_size,'sha256':sha(root/k)} for k in paths if (root/k).is_file()}
missing=sorted(k for k in entry['files'] if k not in current)
changed={k:v for k,v in current.items() if k in entry['files'] and v!=entry['files'][k]}
added={k:v for k,v in current.items() if k not in entry['files']}
# Documentation/test/tooling reports are excluded to avoid self-referential
# seals. Everything compiled or packaged for the player remains included.
runtime={k:v for k,v in current.items() if k.startswith(('src/','native/','samples/','assets/','external/','tools/NovaCore.')) or k in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','NovaCore.sln','global.json','NuGet.Config')}
package=root/'tools/NovaCore.App/bin/Release/net10.0-windows'
files={f.relative_to(package).as_posix():{'bytes':f.stat().st_size,'sha256':sha(f)} for f in sorted(package.rglob('*')) if f.is_file()}
index=root/pathlib.Path(git('rev-parse','--git-path','index'))
settings=pathlib.Path.home()/'AppData/Local/NovaCore/Launcher/settings.json'
result={'head':git('rev-parse','HEAD'),'indexSha256':sha(index),'refsSha256':hashlib.sha256(subprocess.check_output(['git','show-ref','--head'],cwd=root)).hexdigest(),
    'sourceSha256':digest(runtime),'sourceFileCount':len(runtime),'packageSha256':digest(files),'packageFileCount':len(files),
    'settingsSha256':sha(settings),'entryFiles':len(entry['files']),'missing':missing,'changed':changed,'added':added,
    'entryHeadUnchanged':git('rev-parse','HEAD')==entry.get('head'),'ksaSha256':sha(pathlib.Path('E:/Kitten Space Agency/KSA.dll'))}
result['entryProtectedUnchanged']=result['indexSha256']==entry['index']['sha256'] and result['refsSha256']==entry['refs'] and result['settingsSha256']==entry['settings']['sha256']
if a.compare:
    before=json.loads(a.compare.read_text(encoding='utf-8-sig'))
    result['comparison']={k:result[k]==before[k] for k in ('head','indexSha256','refsSha256','sourceSha256','packageSha256','settingsSha256','ksaSha256')}
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('changed','added')},indent=2))
if missing or not result['entryHeadUnchanged'] or not result['entryProtectedUnchanged'] or not all(result.get('comparison',{}).values()): raise SystemExit(1)
