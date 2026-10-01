"""Bounded negative qualification against built Debug/Release native and app outputs.

All mutations occur in isolated copies under build/, never in canonical outputs.
Run after building both configurations: python tools/test-runtime-shader-deployment.py
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from unittest import mock

ROOT = Path(__file__).resolve().parent.parent
SCRATCH = ROOT / 'build/shader-deployment-tests'
SCRATCH.mkdir(parents=True, exist_ok=True)
results = []


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, data):
    path.write_text(json.dumps(data, indent=2) + '\n')


def case(config, name, action):
    # TemporaryDirectory cleanup is confined to this explicitly checked scratch root.
    with tempfile.TemporaryDirectory(prefix=config+'-', dir=SCRATCH) as directory:
        temporary = Path(directory).resolve()
        assert temporary.parent == SCRATCH.resolve()
        native_source = ROOT / 'build' / ('native-ninja' if config == 'Debug' else 'native-ninja-release')
        native = temporary / 'native'
        native.mkdir()
        for item in ['CMakeCache.txt', 'runtime-shaders.json', 'native-runtime-build.json', 'NovaCore.Native.dll']:
            shutil.copy2(native_source/item, native/item)
        shutil.copytree(native_source/'shaders', native/'shaders')
        package = temporary/'package'
        shutil.copytree(ROOT/f'tools/NovaCore.App/bin/{config}/net10.0-windows', package)

        def run_deploy(expected=0, extra=()):
            command = [sys.executable, str(ROOT/'tools/deploy-runtime-shaders.py'), '--repository', str(ROOT),
                       '--native', str(native), '--configuration', config, '--destination', str(package), *extra]
            proc = subprocess.run(command, capture_output=True, text=True)
            assert (proc.returncode == 0) == (expected == 0), proc.stdout+proc.stderr
            return proc.stdout+proc.stderr

        def verify(expected=0, repository=ROOT):
            command = [sys.executable, str(ROOT/'tools/verify-player-package.py'), '--repository', str(repository),
                       '--native', str(native), '--configuration', config, '--package', str(package),
                       '--output', str(temporary/'verification.json')]
            proc = subprocess.run(command, capture_output=True, text=True)
            assert (proc.returncode == 0) == (expected == 0), proc.stdout+proc.stderr
            # A crashed verifier is not an effective negative result.
            receipt = json.loads((temporary/'verification.json').read_text())
            assert receipt['judgment'] == ('PASS' if expected == 0 else 'FAIL')
            return receipt['errors']

        action(native, package, temporary, run_deploy, verify)
        results.append({'configuration':config, 'case':name, 'result':'PASS'})
        print(f'PASS {config}: {name}')


def baseline(n, p, t, deploy, verify):
    deploy(); verify()


def missing_source(n, p, t, deploy, verify):
    before = sha(p/'shaders/triangle.vert.spv')
    (n/'shaders/triangle.vert.spv').unlink()
    assert 'failed' in deploy(1)
    assert sha(p/'shaders/triangle.vert.spv') == before
    assert verify(1)


def missing_deployed(n, p, t, deploy, verify):
    (p/'shaders/triangle.vert.spv').unlink()
    assert any('inventory' in e for e in verify(1))
    deploy(); verify()


def corrupt_deployed(n, p, t, deploy, verify):
    (p/'shaders/triangle.vert.spv').write_bytes(b'corrupt module')
    assert any('Missing or mismatched: shaders/triangle.vert.spv' in e for e in verify(1))
    deploy(); verify()


def unknown_extra(n, p, t, deploy, verify):
    extra = p/'shaders/unowned-probe.spv'; extra.write_bytes(b'not owned')
    assert any('inventory' in e for e in verify(1))
    assert 'Unowned' in deploy(1)
    assert extra.read_bytes() == b'not owned'


def owned_obsolete(n, p, t, deploy, verify):
    extra = p/'shaders/previous-runtime.spv'; extra.write_bytes(b'previous output')
    receipt = json.loads((p/'novacore-runtime-shaders.json').read_text())
    receipt['files'][extra.name] = sha(extra); write(p/'novacore-runtime-shaders.json', receipt)
    sentinel = p/'keep-user-file.txt'; sentinel.write_text('preserve')
    deploy(); assert not extra.exists(); assert sentinel.read_text() == 'preserve'; verify()


def modified_obsolete(n, p, t, deploy, verify):
    extra = p/'shaders/previous-runtime.spv'; extra.write_bytes(b'user modified')
    receipt = json.loads((p/'novacore-runtime-shaders.json').read_text())
    receipt['files'][extra.name] = hashlib.sha256(b'previous output').hexdigest()
    write(p/'novacore-runtime-shaders.json', receipt)
    assert 'Modified obsolete' in deploy(1); assert extra.read_bytes() == b'user modified'


def forged_path(n, p, t, deploy, verify):
    sentinel = t/'sentinel.spv'; sentinel.write_text('preserve')
    receipt = json.loads((p/'novacore-runtime-shaders.json').read_text())
    receipt['files']['../../sentinel.spv'] = sha(sentinel)
    write(p/'novacore-runtime-shaders.json', receipt)
    assert 'Invalid owned shader name' in deploy(1); assert sentinel.read_text() == 'preserve'


def contaminated_source(n, p, t, deploy, verify):
    (n/'shaders/nonruntime-output.spv').write_bytes(b'test or retired output')
    deploy(); verify(); assert not (p/'shaders/nonruntime-output.spv').exists()


def wrong_manifest_agreement(n, p, t, deploy, verify):
    # Both copier and ownership receipt agree with an intentionally incomplete
    # generated manifest. Independent native dependency parsing must still fail.
    authority = json.loads((n/'runtime-shaders.json').read_text())
    authority['shaders'].remove('prepared_surface.vert.spv')
    write(n/'runtime-shaders.json', authority)
    deploy()
    assert not (p/'shaders/prepared_surface.vert.spv').exists()
    assert any('independent source closure' in e for e in verify(1))


def wrong_config(n, p, t, deploy, verify):
    authority = json.loads((n/'runtime-shaders.json').read_text())
    authority['configuration'] = 'Debug' if authority['configuration'] == 'Release' else 'Release'
    write(n/'runtime-shaders.json', authority)
    assert 'configuration' in deploy(1)
    assert any('configuration' in e for e in verify(1))


def wrong_cache_config(n, p, t, deploy, verify):
    cache = n/'CMakeCache.txt'
    configuration = json.loads((n/'runtime-shaders.json').read_text())['configuration']
    other = 'Debug' if configuration == 'Release' else 'Release'
    cache.write_text(cache.read_text().replace('CMAKE_BUILD_TYPE:STRING='+configuration, 'CMAKE_BUILD_TYPE:STRING='+other))
    assert 'CMake configuration' in deploy(1)
    assert any('CMake configuration' in e for e in verify(1))


def wrong_source_and_manifest(n, p, t, deploy, verify):
    # Remove a dependency as well as its generated inventory entry. The consumer
    # in an included .inl must independently prevent this mutually wrong agreement.
    authority = json.loads((n/'runtime-shaders.json').read_text())
    authority['shaders'].remove('frozen_capture_pack.comp.spv')
    write(n/'runtime-shaders.json', authority); deploy()
    repository = t/'repository'
    shutil.copytree(ROOT/'native/NovaCore.Native', repository/'native/NovaCore.Native')
    cmake = repository/'native/NovaCore.Native/CMakeLists.txt'
    cmake.write_text(cmake.read_text().replace('add_dependencies(NovaCore.Native NovaCoreFrozenPackingShader)', ''))
    authority['cmakeSha256'] = sha(cmake); write(n/'runtime-shaders.json', authority)
    receipt = json.loads((p/'novacore-runtime-shaders.json').read_text())
    receipt['authoritySha256'] = sha(n/'runtime-shaders.json'); write(p/'novacore-runtime-shaders.json', receipt)
    errors = verify(1, repository)
    assert any('Runtime consumer missing' in e and 'frozen_capture_pack.comp.spv' in e for e in errors)
    assert not any('independent source closure' in e for e in errors)


def wrong_dll(n, p, t, deploy, verify):
    configuration = json.loads((n/'runtime-shaders.json').read_text())['configuration']
    other = ROOT / ('build/native-ninja-release' if configuration == 'Debug' else 'build/native-ninja')
    shutil.copy2(other/'NovaCore.Native.dll', n/'NovaCore.Native.dll')
    assert 'link provenance' in deploy(1)
    assert any('link provenance' in e for e in verify(1))


def legacy_migration(n, p, t, deploy, verify):
    (p/'novacore-runtime-shaders.json').unlink()
    extra = p/'shaders/old-output.spv'; extra.write_bytes(b'old generated output')
    shutil.copy2(extra, n/'shaders'/extra.name)
    sentinel = t/'not-owned.spv'; sentinel.write_text('preserve')
    ledger = t/'FileListAbsolute.txt'; ledger.write_text(str(extra)+'\n'+str(sentinel)+'\n')
    deploy(extra=['--legacy-file-list', str(ledger)])
    assert not extra.exists(); assert sentinel.read_text() == 'preserve'; verify()


for configuration in ['Debug', 'Release']:
    for test in [baseline, missing_source, missing_deployed, corrupt_deployed, unknown_extra, owned_obsolete,
                 modified_obsolete, forged_path, contaminated_source, wrong_manifest_agreement,
                 wrong_config, wrong_cache_config, wrong_source_and_manifest, wrong_dll, legacy_migration]:
        case(configuration, test.__name__, test)

# Windows may deny symlink creation. Exercise the lstat branch deterministically:
# a dangling reparse point exists in lstat even when exists()/stat() would fail.
spec = importlib.util.spec_from_file_location('deployment', ROOT/'tools/deploy-runtime-shaders.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
with mock.patch.object(Path, 'lstat', return_value=type('Link', (), {'st_mode':0, 'st_file_attributes':0x400})()):
    try:
        module.safe_path(SCRATCH/'dangling.spv')
        raise AssertionError('Dangling reparse point accepted')
    except ValueError as error:
        assert 'Reparse' in str(error)
results.append({'configuration':'all', 'case':'dangling_reparse_lstat_guard', 'result':'PASS', 'method':'mocked lstat attributes'})
(SCRATCH/'results.json').write_text(json.dumps(results, indent=2)+'\n')
print(f'PASS {len(results)} runtime shader deployment cases')
