"""Run the isolated unified application routes sequentially; no source edits."""
import argparse,pathlib,os,subprocess,json,time,hashlib
p=argparse.ArgumentParser();p.add_argument('--cache',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--catalog96',type=pathlib.Path,required=True);p.add_argument('--routes',nargs='+',default=['short','long','rcs64','rcs96']);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[3];out=a.output.resolve();out.mkdir(parents=True,exist_ok=True);env=os.environ.copy();env['NOVACORE_ASSET_CACHE']=str(a.cache.resolve());layer=out/'empty-implicit-layers';layer.mkdir(exist_ok=True);env['VK_IMPLICIT_LAYER_PATH']=str(layer)
exe=root/'tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe';runs=[]
def package_identity():
 files={f.relative_to(exe.parent).as_posix():(f.stat().st_size,hashlib.sha256(f.read_bytes()).hexdigest()) for f in exe.parent.rglob('*') if f.is_file()}
 return hashlib.sha256(''.join(f'{k}\0{v[0]}\0{v[1]}\n' for k,v in sorted(files.items())).encode()).hexdigest()
candidate=package_identity()
for name in a.routes:
 result=out/name/'result.json';result.parent.mkdir(parents=True,exist_ok=True);log=result.parent/'runtime.log';tank={'short':'nc.tank.short-2','long':'nc.tank.long-2'}.get(name,name)
 cmd=[str(exe),'--qualify-editor',str(result),'--qualification-tank',tank]
 if name=='rcs96':cmd+=['--catalog',str(a.catalog96.resolve())]
 start=time.monotonic()
 with log.open('w',encoding='utf-8') as f:r=subprocess.run(cmd,cwd=root,env=env,stdout=f,stderr=subprocess.STDOUT,timeout=600)
 if package_identity()!=candidate:raise SystemExit('Candidate package changed during native qualification')
 row={'route':name,'command':cmd,'packageSha256':candidate,'exit':r.returncode,'seconds':time.monotonic()-start,'logSha256':hashlib.sha256(log.read_bytes()).hexdigest(),'result':json.loads(result.read_text(encoding='utf-8')) if result.exists() else None}
 windows=[]
 for line in log.read_text(encoding='utf-8',errors='replace').splitlines():
  if line.startswith('MODULAR_INTEGRATED_WINDOW '):windows.append(json.loads(line.split(' ',1)[1]))
 row['windows']=windows;runs.append(row);(out/'results.json').write_text(json.dumps(runs,indent=2),encoding='utf-8');print(name,row['exit'],row['result'],flush=True)
 if r.returncode or not row['result'] or row['result'].get('judgment')!='APPLICATION_INTEGRATION_PASS':raise SystemExit(1)
 if name.startswith('rcs') and sum(x['rcsFrames'] for x in windows)<768:raise SystemExit('Missing active RCS performance windows')
