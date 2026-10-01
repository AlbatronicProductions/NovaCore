"""Read-only canonical development package verification; no historical manifests rewritten."""
import argparse, hashlib, json, pathlib, re

p = argparse.ArgumentParser()
p.add_argument('--repository', type=pathlib.Path, default=pathlib.Path('.'))
p.add_argument('--package', type=pathlib.Path)
p.add_argument('--configuration', choices=['Debug', 'Release'], default='Release')
p.add_argument('--native', type=pathlib.Path)
p.add_argument('--output', type=pathlib.Path, required=True)
a = p.parse_args()
root = a.repository.resolve()
package = a.package.resolve() if a.package else root/f'tools/NovaCore.App/bin/{a.configuration}/net10.0-windows'
native = a.native.resolve() if a.native else root/('build/native-ninja-release' if a.configuration == 'Release' else 'build/native-ninja')
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
files = {f.relative_to(package).as_posix(): {'bytes': f.stat().st_size, 'sha256': sha(f)} for f in sorted(package.rglob('*')) if f.is_file()}
errors = []
def require(condition, message):
    if not condition: errors.append(message)
def same(relative, source):
    require(relative in files and source.is_file() and files[relative]['sha256'] == sha(source), 'Missing or mismatched: '+relative)
cmake = (root/'native/NovaCore.Native/CMakeLists.txt').read_text()
targets = set()
for body in re.findall(r'add_dependencies\(NovaCore.Native\s+([^)]*)\)', cmake): targets.update(body.split())
shaders = set()
for target in targets:
    body = re.search(r'(?:add_custom_target|nc_shader_target)\('+re.escape(target)+r'\s+DEPENDS\s+([^)]*)\)', cmake)
    require(body is not None, 'Unresolved shader target: '+target)
    if body: shaders.update(re.findall(r'\$\{SPIRV_DIR\}/([\w.-]+\.spv)', body[1]))
# CMake can produce several shaders in one command. A requested output owns
# all its sibling outputs (including the nested-scale cull shaders).
groups = [set(re.findall(r'\$\{SPIRV_DIR\}/([\w.-]+\.spv)', body)) for body in re.findall(r'(?:add_custom_command|nc_shader_command)\(OUTPUT\s+(.*?)\s+COMMAND',cmake,re.S)]
while True:
    closure = shaders | set().union(*(g for g in groups if g & shaders))
    if closure == shaders: break
    shaders = closure
require(bool(shaders), 'No source-derived shaders')
# Independent source declaration oracle above is deliberately NOT the generated
# manifest consumed by deployment. Direct runtime consumers add a second boundary.
consumer_files = list((root/'src').rglob('*.cs')) + list((root/'samples').rglob('*.cs')) + list((root/'tools').rglob('*.cs'))
library = re.search(r'add_library\(NovaCore.Native SHARED ([^)]*)\)', cmake)
require(library is not None, 'Cannot resolve native runtime source ownership')
if library:
    pending = [root/'native/NovaCore.Native'/name for name in library[1].split()]
    included = set()
    while pending:
        f = pending.pop().resolve()
        if f in included: continue
        included.add(f)
        for name in re.findall(r'^\s*#include\s+"([^"]+)"', f.read_bytes().decode('latin-1'), re.M):
            child = (f.parent/name).resolve()
            if child.is_file() and child.is_relative_to(root): pending.append(child)
    consumer_files += sorted(included)
consumers = set()
for f in consumer_files:
    if not {'bin', 'obj'} & set(f.parts):
        consumers.update(re.findall(r'(?:shaders/|["\\])([A-Za-z0-9_.-]+\.spv)', f.read_bytes().decode('latin-1')))
require(consumers <= shaders, 'Runtime consumer missing from dependency closure: '+repr(sorted(consumers-shaders)))
try:
    authority = json.loads((native/'runtime-shaders.json').read_text())
    require(authority.get('schema') == 1, 'Unknown runtime shader manifest schema')
    require(authority.get('configuration') == a.configuration, 'Native manifest configuration mismatch')
    require(authority.get('shaders') == sorted(shaders), 'Generated deployment manifest differs from independent source closure')
    for key, name in [('cmakeSha256','CMakeLists.txt'), ('ownerSha256','RuntimeShaderDeployment.cmake')]:
        require(authority.get(key) == sha(root/'native/NovaCore.Native'/name), 'Stale native deployment authority: '+name)
    cache = (native/'CMakeCache.txt').read_text()
    require(bool(re.search(r'^CMAKE_BUILD_TYPE:STRING='+a.configuration+r'$', cache, re.M)), 'CMake configuration mismatch')
    binary = json.loads((native/'native-runtime-build.json').read_text())
    require(binary.get('configuration') == a.configuration and binary.get('dllSha256') == sha(native/'NovaCore.Native.dll'), 'Native link provenance mismatch')
    receipt = json.loads((package/'novacore-runtime-shaders.json').read_text())
    require(receipt.get('schema') == 1 and receipt.get('owner') == 'NovaCore runtime shader deployment', 'Invalid shader ownership receipt')
    require(receipt.get('configuration') == a.configuration, 'Deployed shader configuration mismatch')
    require(receipt.get('authoritySha256') == sha(native/'runtime-shaders.json'), 'Deployed authority differs from native authority')
    require(receipt.get('files') == {name:sha(native/'shaders'/name) for name in sorted(shaders)}, 'Deployed ownership/hash receipt differs from native runtime')
except (OSError, ValueError, KeyError) as error:
    require(False, 'Runtime shader provenance unavailable: '+str(error))
actual = {name.removeprefix('shaders/') for name in files if name.startswith('shaders/')}
require(actual == shaders, 'Shader inventory differs from native target dependencies: '+repr(sorted(actual ^ shaders)))
for name in shaders: same('shaders/'+name, native/'shaders'/name)
same('NovaCore.Native.dll', native/'NovaCore.Native.dll')
for prefix, source in [('assets/vehicles/modular-starter', root/'assets/vehicles/modular-starter'), ('wwwroot', root/'tools/NovaCore.ConstructionEditor/wwwroot')]:
    expected = set()
    for f in source.rglob('*'):
        if f.is_file():
            relative = prefix+'/'+f.relative_to(source).as_posix(); expected.add(relative); same(relative, f)
    require({name for name in files if name.startswith(prefix+'/')} == expected, 'Content inventory differs: '+prefix)
for f in (root/'assets/visual/SRV01').glob('*.glb'): same('assets/visual/SRV01/'+f.name, f)
for name in ['BepuPhysics.dll', 'BepuUtilities.dll']: same(name,root/'external/bepu/2.5.0-beta.29/lib'/name)
same('third-party/Bepu/LICENSE.txt',root/'external/bepu/2.5.0-beta.29/LICENSE.txt')
same('third-party/Bepu/ATTRIBUTION.txt',root/'external/bepu/ATTRIBUTION.txt')
for name in ['NovaCore.exe','NovaCore.dll','NovaCore.deps.json','NovaCore.runtimeconfig.json','NovaCore.ConstructionEditor.dll','NovaCore.Triangle.dll','NovaCore.Diagnostics.dll','NovaCore.Recorder.exe','NovaCore.Recorder.dll','NovaCore.Recorder.deps.json','NovaCore.Recorder.runtimeconfig.json']:
    require(name in files, 'Missing player entry/dependency: '+name)
for name in ['NovaCore.Recorder.exe','NovaCore.Recorder.dll','NovaCore.Recorder.deps.json','NovaCore.Recorder.runtimeconfig.json']:
    same(name,root/f'tools/NovaCore.Recorder/bin/{a.configuration}/net10.0-windows'/name)
deps = json.loads((package/'NovaCore.deps.json').read_text())
for target in deps['targets'].values():
    for library in target.values():
        for name in library.get('runtime', {}): require(pathlib.PurePosixPath(name).name in files, 'Missing managed dependency: '+name)
manifest_sha = hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(files.items())).encode()).hexdigest()
result = {'judgment':'PASS' if not errors else 'FAIL','repository':str(root),'package':str(package),'files':files,'fileCount':len(files),'bytes':sum(v['bytes'] for v in files.values()),'packageSha256':manifest_sha,'configuration':a.configuration,'native':str(native),'runtimeInventory':sorted(shaders),'runtimeConsumers':sorted(consumers),'shaders':len(shaders),'errors':errors,'scope':'Repository-layout canonical build output; explicit verified terrain cache required. Not standalone publish qualification.'}
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
raise SystemExit(bool(errors))
