"""Bounded baseline/candidate performance and final-raster comparison runner.

Requires temporary capture_host.py and the earlier pose/telemetry patch for reproduction.
Never modifies deployed shaders. Raw captures live only in each run's temporary runtime.
"""
import os,sys,json,hashlib,subprocess,tempfile,shutil
from pathlib import Path
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'near-surface-performance'))
from analyze import analyze
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'build/tes-address-validation'
DEPLOY=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
SHADERS=ROOT/'native/NovaCore.Native/shaders'
BASELINE='219ec3c3f4f1db0496e7ef6204d510a1bd337aeb'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

SAMPLE_DECL='''
layout(set=0,binding=41,std430) readonly buffer ParityIndices { uint indices[]; } parityIndices;
layout(set=0,binding=59,std430) buffer ParitySamples { uint count;uint enabled;uint unused0;uint unused1;uvec4 words[]; } parity;
'''
SAMPLE_BODY='''
  if(parity.enabled!=0u){
    uvec3 ids=uvec3(parityIndices.indices[gl_PrimitiveID*3],parityIndices.indices[gl_PrimitiveID*3+1],parityIndices.indices[gl_PrimitiveID*3+2]);
    if((ids.x%128u)==0u){
      uint sampleIndex=atomicAdd(parity.count,1u);
      if(sampleIndex<262144u){uint offset=sampleIndex*8u;
        parity.words[offset]=uvec4(ids,floatBitsToUint(gl_TessCoord.x));
        parity.words[offset+1]=uvec4(floatBitsToUint(gl_TessCoord.yz),0,0);
        parity.words[offset+2]=floatBitsToUint(vec4(relativeBody,terrainHeight));
        parity.words[offset+3]=floatBitsToUint(vec4(bodyDirection,float(localWeight)));
        parity.words[offset+4]=floatBitsToUint(gl_Position);
        parity.words[offset+5]=floatBitsToUint(vec4(normal,float(FacilitySupport(direction).weight)));
        parity.words[offset+6]=uvec4(unpackDouble2x32(baseHeight),unpackDouble2x32(nearHeight));
        parity.words[offset+7]=uvec4(unpackDouble2x32(localDisplacement),unpackDouble2x32(height));
      }
    }
  }
'''

def run(pose,kind,capture=False,diagnostic=None,negative=False):
    label=f'{pose}-{kind}'+('-'+diagnostic if diagnostic else '')+('-negative' if negative else '')+('-capture' if capture else '')
    original={p.name:sha(p) for p in (DEPLOY/'shaders').glob('*.spv')}
    with tempfile.TemporaryDirectory(prefix='run-',dir=OUT) as name:
        runtime=Path(name)
        for p in DEPLOY.iterdir():
            if p.is_file():shutil.copy2(p,runtime/p.name)
        shutil.copytree(DEPLOY/'shaders',runtime/'shaders')
        (runtime/'earth-data').mkdir()
        for p in (DEPLOY/'earth-data').iterdir():
            if p.is_file():os.link(p,runtime/'earth-data'/p.name)
        filename='production_spherical_billboard.tese'
        source=(subprocess.check_output(['git','show',BASELINE+':native/NovaCore.Native/shaders/'+filename],cwd=ROOT).decode() if kind=='baseline' else (SHADERS/filename).read_text())
        if negative:source=source.replace('layout(constant_id=0) const bool diagnosticGeographicAddress=false;','const bool diagnosticGeographicAddress=false;')
        if capture:
            source=source.replace('void main()',SAMPLE_DECL+'\nvoid main()')
            source=source[:source.rfind('}')]+SAMPLE_BODY+'\n}\n'
        shader=runtime/filename;shader.write_text(source,encoding='utf-8')
        subprocess.run([str(Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SHADERS),str(shader),'-o',str(runtime/'shaders'/f'{filename}.spv')],check=True)
        (runtime/'implicit').mkdir();(runtime/'explicit').mkdir()
        manifest=Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json';data=json.loads(manifest.read_text());data['layer']['library_path']=str((manifest.parent/data['layer']['library_path']).resolve());(runtime/'explicit/validation.json').write_text(json.dumps(data))
        env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
        env.update(VK_LAYER_PATH=str(runtime/'explicit'),VK_IMPLICIT_LAYER_PATH=str(runtime/'implicit'),VK_LAYER_SETTINGS_PATH=str(runtime),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='A' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'D':'-1.0','E':'-0.001'}.get(pose,'-0.035'))
        if pose in 'DE':env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
        if pose=='C':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
        if diagnostic:env['NOVACORE_SURFACE_DIAGNOSTIC']=diagnostic
        if capture:env['NOVACORE_TES_PARITY']=str(runtime)
        else:env['NOVACORE_PERFORMANCE_GEOMETRY']='1'
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='C':args.append('--surface-site=florida-launch')
        proc=subprocess.run([str(runtime/'NovaCore.Triangle.exe'),*args],cwd=runtime,env=env,capture_output=True,text=True,timeout=180)
        log=proc.stdout+proc.stderr
        if proc.returncode or 'VUID-' in log or 'directional visibility PASS:' not in log:
            (OUT/(label+'-failed.log')).write_text(log,encoding='utf-8');raise RuntimeError(f'{label}: exit {proc.returncode}; check retained failure log')
        result=analyze(log,label)
        result.update(pose=pose,kind=kind,capture=capture,diagnostic=diagnostic,negativeControl=negative,shaderSha256=sha(runtime/'shaders'/f'{filename}.spv'),nativeSha256=sha(runtime/'NovaCore.Native.dll'),managedSha256=sha(runtime/'NovaCore.Triangle.dll'),arguments=args,environment={k:v for k,v in env.items() if k.startswith('NOVACORE_') and k!='NOVACORE_TES_PARITY'},captureIdentity=[line for line in log.splitlines() if 'TES parity capture:' in line or 'NCSM1 TES geographic address:' in line])
        (OUT/(label+'.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
        raw={}
        if capture:
            raw={key:(runtime/(key+'.bin')).read_bytes() for key in ('pixels','tes','prepared')}
            result['rawBytes']=sum(map(len,raw.values()))
        print(label,result['metrics']['gpuDetailedDrawMs'],flush=True)
    assert original=={p.name:sha(p) for p in (DEPLOY/'shaders').glob('*.spv')}
    return result,raw

def compare(a,b):
    result={}
    n=3440*1440
    for key,offset,count,dtype in [('depth',0,n,'<f4'),('hdr',n*4,n*4,'<f2'),('image',n*12,n*4,'u1')]:
        x=np.frombuffer(a['pixels'],dtype=dtype,count=count,offset=offset);y=np.frombuffer(b['pixels'],dtype=dtype,count=count,offset=offset)
        unequal=x!=y;delta=np.abs(x.astype('f8')-y.astype('f8'))
        result[key]=dict(values=count,exact=int((~unequal).sum()),mismatches=int(unequal.sum()),maxDelta=float(delta.max()),rmsDelta=float(np.sqrt(np.mean(delta**2))),nonFinite=int((~np.isfinite(delta)).sum()))
        result[key].update(baselineMinimum=float(x.min()),baselineMaximum=float(x.max()),baselineNonzero=int(np.count_nonzero(x)),baselineSha256=hashlib.sha256(x.tobytes()).hexdigest(),candidateSha256=hashlib.sha256(y.tobytes()).hexdigest())
        mask=unequal.reshape(1440,3440,-1).any(axis=2);ys,xs=np.where(mask)
        result[key]['pixelBoundingBox']=None if not len(xs) else [int(xs.min()),int(ys.min()),int(xs.max()),int(ys.max())]
        result[key]['mismatchTiles8x8']=[[int(t.sum()) for t in np.array_split(row,8,axis=1)] for row in np.array_split(mask,8)]
    result['prepared']=dict(bytes=len(a['prepared']),byteIdentical=a['prepared']==b['prepared'],sha256Baseline=hashlib.sha256(a['prepared']).hexdigest(),sha256Candidate=hashlib.sha256(b['prepared']).hexdigest())
    def samples(raw):
        count=int(np.frombuffer(raw,'<u4',count=1)[0]);v=np.frombuffer(raw,'<u4',offset=16,count=count*32).reshape(count,32)
        return {tuple(row[:6]):row[8:].copy() for row in v}
    x=samples(a['tes']);y=samples(b['tes']);keys=x.keys()&y.keys();same=x.keys()==y.keys()
    xx=np.array([x[k] for k in sorted(keys)],dtype='<u4');yy=np.array([y[k] for k in sorted(keys)],dtype='<u4')
    xf=xx.view('<f4');yf=yy.view('<f4');position=xf[:,:3].astype('f8')-yf[:,:3].astype('f8')
    elevation=xx[:,20:24].copy().view('<f8')[:,1]-yy[:,20:24].copy().view('<f8')[:,1]
    result['tes']=dict(baselineSamples=len(x),candidateSamples=len(y),sameSampleKeys=same,compared=len(keys),bitIdentical=bool(np.array_equal(xx,yy)),maxPositionDelta=float(np.abs(position).max()),rmsPositionDelta=float(np.sqrt(np.mean(position**2))),maxElevationDelta=float(np.abs(elevation).max()),nearSamples=int((xf[:,7]>0).sum()),facilitySupportSamples=int((xf[:,15]>0).sum()))
    return result

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('mode',choices=['performance','parity']);parser.add_argument('poses');parser.add_argument('--diagnostic',choices=['owners','boundaries']);parser.add_argument('--negative',action='store_true');args=parser.parse_args()
    OUT.mkdir(exist_ok=True)
    for pose in args.poses:
        a,araw=run(pose,'baseline',args.mode=='parity',args.diagnostic)
        b,braw=run(pose,'candidate',args.mode=='parity',args.diagnostic,args.negative)
        for key in ('cameraBody','bodyOrientation','viewProjection','generation','compactedTriangles'):assert a['firstFrame'][key]==b['firstFrame'][key],key
        if args.mode=='parity':
            result=compare(araw,braw);result.update(pose=pose,diagnostic=args.diagnostic,negativeControl=args.negative,rawBytesCreated=len(araw['pixels'])+len(araw['tes'])+len(araw['prepared'])+len(braw['pixels'])+len(braw['tes'])+len(braw['prepared']))
            path=OUT/(pose+'-parity'+('-'+args.diagnostic if args.diagnostic else '')+('-negative' if args.negative else '')+'.json');path.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result,indent=2),flush=True)
