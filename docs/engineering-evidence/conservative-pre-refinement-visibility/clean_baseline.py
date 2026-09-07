"""Measure banked runtime before any instrumentation, using existing fixed poses."""
import sys
sys.dont_write_bytecode=True
import json,hashlib,os,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'build/conservative-pre-refinement-visibility'
sys.path.insert(0,str(HERE.parent/'m13.2-terrain-shading'))
from validate import canonical
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import analyze

if __name__=='__main__':
    records=[]
    for pose in ('A','B','C','D','E'):
        with tempfile.TemporaryDirectory(prefix='layers-',dir=OUT) as tmp:
            env=canonical(Path(tmp));env.update(NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
            args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--benchmark-frames=350','--log=startup,validation,vulkan']
            if pose in ('A','B','E'):
                args.append('--p2s5c3-traversal')
                env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='A' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS='-0.001' if pose=='E' else '-0.035')
            elif pose=='D':
                args.append('--p2s5c3-traversal')
                env.update(NOVACORE_P2S5C3_HORIZON_ONLY='1',NOVACORE_P2S5E_HORIZON_FRAME='150',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1')
            else: args.append('--surface-site=florida-launch')
            p=subprocess.run([str(ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe'),*args],cwd=ROOT,env=env,capture_output=True,text=True,timeout=180)
            log=p.stdout+p.stderr
            (OUT/f'clean-{pose}.log').write_text(log,encoding='utf-8')
            assert p.returncode==0 and 'VUID-' not in log
            result=dict(pose=pose,args=args,env={k:v for k,v in env.items() if k.startswith('NOVACORE_')},sha256=hashlib.sha256(log.encode()).hexdigest(),bytes=len(log.encode()),summary=[x for x in log.splitlines() if any(s in x for s in ['timing:','timings:','timing averages:','GPU workload:','refinement averages:',' PASS:','specialization','Florida seating:'])])
            if pose in ('A','B','E'):result['aligned100']=analyze(log,pose)
            records.append(result)
            (HERE/'clean-runtime.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
            print(pose,'PASS',result.get('aligned100',{}).get('metrics',{}).get('gpuDetailedDrawMs'),flush=True)
