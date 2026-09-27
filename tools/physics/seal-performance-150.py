"""Identity/preservation seal; no production or retained-forensic writes."""
import argparse,hashlib,json,pathlib,subprocess
p=argparse.ArgumentParser();p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--compare',type=pathlib.Path);a=p.parse_args()
r=pathlib.Path(__file__).resolve().parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=r).decode().strip()
e=json.loads((r/'build/performance-150fps/entry.json').read_text())
files={n:sha(r/n) for n in sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').split('\0'))-{''}) if (r/n).is_file()}
runtime={n:v for n,v in files.items() if n.startswith(('src/','native/','samples/','assets/','external/','tools/NovaCore.')) or n in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','NovaCore.sln','global.json','NuGet.Config')}
package=r/'tools/NovaCore.App/bin/Release/net10.0-windows'
parts={p.relative_to(package).as_posix():{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(package.rglob('*')) if p.is_file()}
protected={n:v for n,v in e['protected'].items() if pathlib.Path(n).name!='retention-status.json'}
result={'head':git('rev-parse','HEAD'),'headUnchanged':git('rev-parse','HEAD')==e['head'],'refsUnchanged':git('show-ref','--heads','--tags')==e['refs'].strip(),
 'sourceSha256':hashlib.sha256(json.dumps(runtime,sort_keys=True,separators=(',',':')).encode()).hexdigest(),
 'packageSha256':hashlib.sha256(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in sorted(parts.items())).encode()).hexdigest(),
 'protectedCount':len(protected),'protectedFailures':[n for n,v in protected.items() if not pathlib.Path(n).is_file() or sha(pathlib.Path(n))!=v],
 'changedEntryFiles':{n:{'before':v,'after':files.get(n)} for n,v in e['files'].items() if files.get(n)!=v},
 'addedFiles':{n:v for n,v in files.items() if n not in e['files']},'runtimeFiles':runtime,'packageFiles':parts}
if a.compare:
 prior=json.loads(a.compare.read_text());result['frozenComparison']={k:prior[k]==result[k] for k in ('head','sourceSha256','packageSha256')}
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('changedEntryFiles','addedFiles','runtimeFiles','packageFiles')},indent=2))
assert result['headUnchanged'] and result['refsUnchanged'] and not result['protectedFailures'] and all(result.get('frozenComparison',{}).values())
