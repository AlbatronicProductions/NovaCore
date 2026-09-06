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
from analyze import analyze

ROOT = Path(__file__).resolve().parents[3]
DEPLOYED = ROOT / 'samples/NovaCore.Triangle/bin/Release/net10.0'
SOURCE = ROOT / 'native/NovaCore.Native/shaders'
OUTPUT = ROOT / 'build/near-surface-performance'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def variant(probe, destination):
    name = 'production_spherical_billboard.tese'
    source = (SOURCE / name).read_text()
    if probe == 'no-address':
        original = 'uint face;dvec2 faceUv;ProductionDirectionAddressD(direction,face,faceUv);'
        assert source.count(original) == 1
        source = source.replace(original, 'uint face=0u;dvec2 faceUv=dvec2(0.5);')
    elif probe == 'no-normal':
        begin = source.index('  dvec3 east=PhysicalEastD(direction)')
        end = source.index('  double localDisplacement=', begin)
        source = source[:begin] + '  vec3 surfaceNormal=baseNormal;\n' + source[end:]
    elif probe == 'no-near':
        original = 'NaturalTerrainFieldSampleD nearValue=EvaluateNaturalCandidateNearD(direction);'
        assert source.count(original) == 1
        source = source.replace(original, 'NaturalTerrainFieldSampleD nearValue;nearValue.height=0.0;nearValue.bodyGradient=dvec3(0.0);')
    elif probe == 'minimal-tcs':
        name = 'production_spherical_billboard.tesc'
        source = (SOURCE/name).read_text()
        begin = source.index('float edgeFactor(uint a,uint b){')
        end = source.index('\nvoid main()', begin)
        source = source[:begin]+'float edgeFactor(uint a,uint b){return 1.0;}\n'+source[end:]
    elif probe in ('normal', 'owners', 'force-one', 'counters-off'):
        return None
    else:
        raise ValueError(probe)
    path = destination/name
    path.write_text(source)
    compiler = Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'
    subprocess.run([str(compiler), '-I', str(SOURCE), str(path), '-o', str(destination/'shaders'/f'{name}.spv')], check=True)
    return dict(stage=name, sourceSha256=digest(path), spirvSha256=digest(destination/'shaders'/f'{name}.spv'))


def run(pose, probe, label, owners=False, geometry=False):
    OUTPUT.mkdir(exist_ok=True)
    original = {str(p.relative_to(DEPLOYED)): digest(p) for p in DEPLOYED.glob('shaders/*.spv')}
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
        if probe=='owners' or owners:
            env['NOVACORE_SURFACE_DIAGNOSTIC']='owners'
        if probe=='force-one':
            env['NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC']='force-tes1'
        if probe=='counters-off':
            env['NOVACORE_PERFORMANCE_COUNTERS_OFF']='1'
        if geometry:
            env['NOVACORE_PERFORMANCE_GEOMETRY']='1'
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000',
              '--physical-surface=m12d-natural-candidate','--p2s5c3-traversal',
              '--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='C':
            args.append('--surface-site=florida-launch')
        process=subprocess.run([str(runtime/'NovaCore.Triangle.exe'),*args], cwd=runtime,
                               env=env,capture_output=True,text=True,timeout=120)
        text=process.stdout+'\nSTDERR\n'+process.stderr
        # Retain only bounded numerical evidence after successful parsing.
        if process.returncode or 'directional visibility PASS:' not in text:
            (OUTPUT/f'{label}-failed.log').write_text(text)
            raise RuntimeError(f'{label}: runtime failed {process.returncode}')
        result=analyze(text,label)
        result.update(pose=pose,probe=probe,owners=owners,arguments=args,
                      environment={k:v for k,v in env.items() if k.startswith('NOVACORE_')},
                      nativeSha256=digest(runtime/'NovaCore.Native.dll'),
                      managedSha256=digest(runtime/'NovaCore.Triangle.dll'),
                      shaderIdentity=probeIdentity, deployedShaderHashes=original)
        target=OUTPUT/f'{label}.json'
        target.write_text(json.dumps(result,indent=2)+'\n')
        print(json.dumps({k:result['metrics'][k] for k in ('gpuTotalMs','gpuDetailedDrawMs','gpuCullCompactMs','tcsPatches','refinedVertices','fragmentInvocations','fenceWait','submit')},indent=2))
    assert all(digest(DEPLOYED/name)==value for name,value in original.items()), 'Deployed shaders changed'


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('pose',choices=list('ABCDE'))
    parser.add_argument('probe',choices=['normal','owners','no-address','no-normal','no-near','minimal-tcs','force-one','counters-off'])
    parser.add_argument('label')
    parser.add_argument('--owners',action='store_true')
    parser.add_argument('--geometry',action='store_true')
    args=parser.parse_args()
    run(args.pose,args.probe,args.label,args.owners,args.geometry)
