"""Isolated, bounded diagnostic runner. Never changes deployed production shaders.

Requires the companion temporary host instrumentation and a matching Release build.
The caller must close other GPU workloads or record their presence.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import sys
sys.dont_write_bytecode = True
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/"near-surface-performance"))
from analyze import analyze

ROOT = Path(__file__).resolve().parents[3]
DEPLOYED = ROOT / 'samples/NovaCore.Triangle/bin/Release/net10.0'
SOURCE = ROOT / 'native/NovaCore.Native/shaders'
OUTPUT = ROOT / 'build/m13-next-target'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def variant(probe, destination):
    name = 'production_spherical_billboard.tese'
    source = (SOURCE / name).read_text()
    if probe == 'copy-predicate':
        name = 'production_spherical_billboard_prepare.comp'
        source = (SOURCE/name).read_text()
        needle='  dvec3 direction=canonicalDirection(value,current);'
        code="""  // Diagnostic only: prove the existing same-direction copy from unchanged inputs.
  if(previous.metadata.x!=0u && all(equal(previous.identity.zw,current.identity.zw)) &&
     all(equal(previous.east.xyz,current.east.xyz)) && all(equal(previous.north.xyz,current.north.xyz)) &&
     all(equal(previous.up,current.up)) && all(equal(previous.transition,current.transition)) &&
     transitionWeight(value,current)==0.0){
    if(preparation.ranges[0].z!=0u)stagedPhysical.values[vertex]=physical.values[vertex];
    return;
  }
"""
        assert source.count(needle)==1
        source=source.replace(needle,code+needle)
    elif probe == 'no-normal':
        begin = source.index('  dvec3 east=PhysicalEastD(direction)')
        end = source.index('  double localDisplacement=', begin)
        source = source[:begin] + '  vec3 surfaceNormal=baseNormal;\n' + source[end:]
    elif probe == 'no-near':
        original = 'NaturalTerrainFieldSampleD nearValue=EvaluateNaturalCandidateNearD(direction);'
        assert source.count(original) == 1
        source = source.replace(original, 'NaturalTerrainFieldSampleD nearValue;nearValue.height=0.0;nearValue.bodyGradient=dvec3(0.0);')
    elif probe == 'cheap-fragment':
        name = 'planetary_production.frag'
        source = (SOURCE/name).read_text()
        source = source[:source.index('void main()')] + 'void main(){vec3 v=color.rgb+normal+lightDirection+response.xyz+viewDirection+bodyDirection+vec3(terrainHeight)+bodyCameraHigh+bodyCameraLow+localDetail.xyz+vec3(productionUv,productionTransition.x)+vec3(topologyCoordinate,productionTransition.y)+vec3(material,productionLayer)+vec3(productionAddress.xyz)+vec3(productionAddress.w);outColor=vec4(fract(abs(v)*.001),color.a+response.w+localDetail.w);}'
    elif probe == 'no-fragment':
        name = 'planetary_production.frag'
        source = (SOURCE/name).read_text()
        source = source[:source.index('void main()')]+ 'void main(){outColor=vec4(.3,.3,.3,1.0);}'
    elif probe == 'minimal-tcs':
        name = 'production_spherical_billboard.tesc'
        source = (SOURCE/name).read_text()
        begin = source.index('float edgeFactor(uint a,uint b){')
        end = source.index('\nvoid main()', begin)
        source = source[:begin]+'float edgeFactor(uint a,uint b){return 1.0;}\n'+source[end:]
    elif probe == 'normal':
        return None
    else:
        raise ValueError(probe)
    path = destination/name
    path.write_text(source)
    compiler = Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'
    subprocess.run([str(compiler), '-I', str(SOURCE), str(path), '-o', str(destination/'shaders'/f'{name}.spv')], check=True)
    return dict(stage=name, sourceSha256=digest(path), spirvSha256=digest(destination/'shaders'/f'{name}.spv'))


def run(pose, probe, label, owners=False, geometry=False, motion=None, level=None, delta=False, florida=False, components=False, speed=None, capture=False):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    originalHashes = {str(p.relative_to(DEPLOYED)): digest(p) for p in DEPLOYED.glob('shaders/*.spv')}
    with tempfile.TemporaryDirectory(prefix='isolated-', dir=OUTPUT) as temporary:
        runtime = Path(temporary).resolve()
        assert runtime.is_relative_to(OUTPUT.resolve())
        for path in DEPLOYED.iterdir():
            if path.is_file():
                shutil.copy2(path, runtime/path.name)
        shutil.copytree(DEPLOYED/'shaders', runtime/'shaders')
        # Read-only input sharing avoids duplicating the 64 MiB elevation oracle.
        (runtime/'earth-data').mkdir()
        for path in (DEPLOYED/'earth-data').iterdir():
            if path.is_file():
                os.link(path, runtime/'earth-data'/path.name)
        probeIdentity = variant(probe, runtime)
        if components:
            for filename,target in [('production_spherical_billboard_prepare.comp','result.reserved'),('production_spherical_billboard_incoming_prepare.comp','physical.values[vertex].reserved')]:
                source=(SOURCE/filename).read_text()
                original=target+'=vec4(0.0);'
                assert source.count(original)==1
                source=source.replace(original,target+'=uintBitsToFloat(uvec4(unpackDouble2x32(RegionalPhysicalResidual(direction)),unpackDouble2x32(height-CandidateNaturalBaseHeightD(direction))));')
                path=runtime/filename;path.write_text(source)
                subprocess.run([str(Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SOURCE),str(path),'-o',str(runtime/'shaders'/f'{filename}.spv')],check=True)

        layers = runtime/'layers'
        (layers/'implicit').mkdir(parents=True)
        (layers/'explicit').mkdir()
        manifest = Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json'
        data = json.loads(manifest.read_text())
        data['layer']['library_path'] = str((manifest.parent/data['layer']['library_path']).resolve())
        (layers/'explicit/validation.json').write_text(json.dumps(data))
        env = {k:v for k,v in os.environ.items() if not k.startswith(('VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS','NOVACORE_'))}
        env.update(VK_LAYER_PATH=str(layers/'explicit'), VK_IMPLICIT_LAYER_PATH=str(layers/'implicit'),
                   VK_LAYER_SETTINGS_PATH=str(layers), VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',
                   NOVACORE_WINDOW_CLIENT_WIDTH='3440', NOVACORE_WINDOW_CLIENT_HEIGHT='1440',
                   NOVACORE_WINDOW_BORDERLESS='1', NOVACORE_PERFORMANCE_FRAME_LOG='1',
                   NOVACORE_P2S5F_DIRECTIONAL_ONLY='1', NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',
                   NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='A' else '17',
                   NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',
                   NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'D':'-1.0','E':'-0.001'}.get(pose,'-0.035'))
        if pose in ('D','E'):
            env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
        if pose=='C':
            env['NOVACORE_PERFORMANCE_FLORIDA']='1'
        if owners:
            env['NOVACORE_SURFACE_DIAGNOSTIC']='owners'
        if motion: env['NOVACORE_PREPARATION_MOTION']=motion
        if level is not None: env['NOVACORE_P2S5F_DIRECTIONAL_LEVEL']=str(level)
        if delta: env['NOVACORE_PREPARATION_DELTA']='1'
        if florida: env['NOVACORE_PREPARATION_FLORIDA']='1'
        if components: env['NOVACORE_PREPARATION_COMPONENTS']='1'
        if speed is not None: env['NOVACORE_PREPARATION_SPEED_MPS']=str(speed)
        if capture: env['NOVACORE_TES_PARITY']=str(runtime)
        if geometry:
            env['NOVACORE_PERFORMANCE_GEOMETRY']='1'
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000',
              '--physical-surface=m12d-natural-candidate','--p2s5c3-traversal',
              '--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='C' or florida:
            args.append('--surface-site=florida-launch')
        try:
            process=subprocess.run([str(runtime/'NovaCore.Triangle.exe'),*args], cwd=runtime,
                                   env=env,capture_output=True,text=True,timeout=180)
        except subprocess.TimeoutExpired as error:
            log=(error.stdout or b'')+(error.stderr or b'')
            if isinstance(log,bytes):log=log.decode('utf-8',errors='replace')
            (OUTPUT/f'{label}-failed.log').write_text(log,encoding='utf-8')
            raise

        text=process.stdout+'\nSTDERR\n'+process.stderr
        # Retain only bounded numerical evidence after successful parsing.
        if process.returncode or 'directional visibility PASS:' not in text:
            (OUTPUT/f'{label}-failed.log').write_text(text)
            raise RuntimeError(f'{label}: runtime failed {process.returncode}')
        if 'VUID-' in text: raise RuntimeError('Strict Vulkan failed')
        (OUTPUT/f'{label}.log').write_text(text,encoding='utf-8')
        result=analyze(text,label)
        result.update(pose=pose,probe=probe,owners=owners,arguments=args,
                      environment={k:v for k,v in env.items() if k.startswith('NOVACORE_')},
                      nativeSha256=digest(runtime/'NovaCore.Native.dll'),
                      managedSha256=digest(runtime/'NovaCore.Triangle.dll'),
                      shaderIdentity=probeIdentity, deployedShaderHashes=originalHashes)
        if capture:
            raw={key:(runtime/(key+'.bin')).read_bytes() for key in ('pixels','prepared')}
            result['capture']={'bytes':sum(map(len,raw.values())), 'sha256':{key:hashlib.sha256(value).hexdigest() for key,value in raw.items()},'identity':[x for x in text.splitlines() if 'TES parity capture:' in x]}
        target=OUTPUT/f'{label}.json'
        target.write_text(json.dumps(result,indent=2)+'\n')
        print(label,result['metrics']['gpuDetailedDrawMs'],flush=True)
        if capture: result['_raw']=raw
        assert all(digest(DEPLOYED/name)==value for name,value in originalHashes.items()), 'Deployed shaders changed'
        return result


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('pose',choices=list('ABCDE'))
    parser.add_argument('probe',choices=['normal','no-normal','no-near','no-fragment','cheap-fragment','minimal-tcs','copy-predicate'])
    parser.add_argument('label')
    parser.add_argument('--owners',action='store_true')
    parser.add_argument('--geometry',action='store_true')
    parser.add_argument('--motion',choices=['stationary','subthreshold','one','slow','normal','fast','transition'])
    parser.add_argument('--level',type=int)
    parser.add_argument('--delta',action='store_true')
    parser.add_argument('--florida',action='store_true')
    parser.add_argument('--components',action='store_true')
    parser.add_argument('--speed',type=float,help='Scripted metres/second at a deterministic 60 Hz; not wall-clock velocity')
    parser.add_argument('--capture',action='store_true')
    args=parser.parse_args()
    run(**vars(args))
