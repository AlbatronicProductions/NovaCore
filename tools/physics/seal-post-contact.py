"""Read-only identity/preservation verification for this bounded correction."""
import argparse, hashlib, json, pathlib, subprocess
p=argparse.ArgumentParser();p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--compare',type=pathlib.Path);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[2]
def git(*args):return subprocess.check_output(['git',*args],cwd=root).decode().strip()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
entry=json.loads((root/'build/post-contact-performance/entry.json').read_text(encoding='utf-8-sig'))
paths=sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').split('\0'))-{''})
files={k:sha(root/k) for k in paths if (root/k).is_file()}
runtime={k:v for k,v in files.items() if k.startswith(('src/','native/','samples/','assets/','external/','tools/NovaCore.')) or k in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','NovaCore.sln','global.json','NuGet.Config')}
package=root/'tools/NovaCore.App/bin/Release/net10.0-windows'
package_files={f.relative_to(package).as_posix():{'bytes':f.stat().st_size,'sha256':sha(f)} for f in sorted(package.rglob('*')) if f.is_file()}
package_sha=hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(package_files.items())).encode()).hexdigest()
protected={k:v for k,v in entry['protected'].items() if not pathlib.Path(k).is_relative_to(package)}
failed=[k for k,v in protected.items() if not pathlib.Path(k).is_file() or sha(pathlib.Path(k))!=v]
changed={k:{'before':v,'after':files.get(k)} for k,v in entry['files'].items() if files.get(k)!=v}
added={k:v for k,v in files.items() if k not in entry['files']}
refs=git('show-ref','--heads','--tags')
old_refs='\n'.join(v for v in entry['refs'].splitlines() if ' refs/heads/' in v or ' refs/tags/' in v)
result={'head':git('rev-parse','HEAD'),'headUnchanged':git('rev-parse','HEAD')==entry['head'],'userRefsUnchanged':refs==old_refs,
        'protectedCount':len(protected),'protectedFailures':failed,'entryFiles':len(entry['files']),
        'changedEntryFiles':changed,'addedFiles':added,'sourceSha256':digest(runtime),'runtimeFiles':runtime,
        'packageSha256':package_sha,'packageFiles':package_files,
        'ksaSha256':sha(pathlib.Path('E:/Kitten Space Agency/KSA.dll'))}
if a.compare:
    before=json.loads(a.compare.read_text(encoding='utf-8-sig'))
    result['frozenComparison']={k:before[k]==result[k] for k in ('head','sourceSha256','packageSha256','ksaSha256')}
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('changedEntryFiles','addedFiles','runtimeFiles','packageFiles')},indent=2))
if failed or not result['headUnchanged'] or not result['userRefsUnchanged'] or not all(result.get('frozenComparison',{}).values()):raise SystemExit(1)
