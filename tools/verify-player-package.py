"""Read-only canonical development package verification; no historical manifests rewritten."""
import argparse, hashlib, json, pathlib, re

p = argparse.ArgumentParser()
p.add_argument('--repository', type=pathlib.Path, default=pathlib.Path('.'))
p.add_argument('--package', type=pathlib.Path)
p.add_argument('--output', type=pathlib.Path, required=True)
a = p.parse_args()
root = a.repository.resolve()
package = a.package.resolve() if a.package else root/'tools/NovaCore.App/bin/Release/net10.0-windows'
native = root/'build/native-ninja-release'
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
    body = re.search(r'add_custom_target\('+re.escape(target)+r'\s+DEPENDS\s+([^)]*)\)', cmake)
    require(body is not None, 'Unresolved shader target: '+target)
    if body: shaders.update(re.findall(r'\$\{SPIRV_DIR\}/([\w.-]+\.spv)', body[1]))
# CMake can produce several shaders in one command. A requested output owns
# all its sibling outputs (including the nested-scale cull shaders).
groups = [set(re.findall(r'\$\{SPIRV_DIR\}/([\w.-]+\.spv)', body)) for body in re.findall(r'add_custom_command\(OUTPUT\s+(.*?)\s+COMMAND',cmake,re.S)]
while True:
    closure = shaders | set().union(*(g for g in groups if g & shaders))
    if closure == shaders: break
    shaders = closure
require(bool(shaders), 'No source-derived shaders')
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
    same(name,root/'tools/NovaCore.Recorder/bin/Release/net10.0-windows'/name)
deps = json.loads((package/'NovaCore.deps.json').read_text())
for target in deps['targets'].values():
    for library in target.values():
        for name in library.get('runtime', {}): require(pathlib.PurePosixPath(name).name in files, 'Missing managed dependency: '+name)
manifest_sha = hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(files.items())).encode()).hexdigest()
result = {'judgment':'PASS' if not errors else 'FAIL','repository':str(root),'package':str(package),'files':files,'fileCount':len(files),'bytes':sum(v['bytes'] for v in files.values()),'packageSha256':manifest_sha,'shaders':len(shaders),'errors':errors,'scope':'Repository-layout canonical build output; explicit verified terrain cache required. Not standalone publish qualification.'}
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
raise SystemExit(bool(errors))
