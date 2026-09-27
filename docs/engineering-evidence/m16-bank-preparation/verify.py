"""Read-only, pre-staging bank verification. Never builds, launches or writes Git."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--manifest-sha256', required=True)
a = p.parse_args()
root = Path(__file__).resolve().parents[3]
folder = Path(__file__).resolve().parent
env = dict(os.environ, GIT_OPTIONAL_LOCKS='0')

def git(*args):
    return subprocess.check_output(['git', *args], cwd=root, env=env).decode().strip()

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def require(condition, message):
    if not condition:
        raise SystemExit('REVISE: ' + message)

identity = folder / 'file-identities.json'
require(sha(identity) == a.manifest_sha256.lower(), 'manual inventory seal differs')
seal = json.loads(identity.read_text())
inventory = json.loads((folder / 'bank-inventory.json').read_text())
require(git('rev-parse', 'HEAD') == seal['git']['head'], 'HEAD differs')
require(git('branch', '--show-current') == 'main', 'branch differs')
require(sha(root / '.git/index') == seal['git']['indexSha256'], 'index differs; this is a pre-staging check')
require(git('remote', 'get-url', 'origin') == seal['git']['origin'], 'origin differs')
require(git('remote', 'get-url', '--push', 'origin') == seal['git']['origin'], 'push origin differs')
refs = subprocess.check_output(['git', 'show-ref'], cwd=root, env=env)
require(hashlib.sha256(refs).hexdigest() == seal['git']['refsSha256'], 'local refs differ')
require(not git('tag', '--list', 'm16.0'), 'local m16.0 already exists')
require(not git('ls-remote', '--tags', 'origin', 'refs/tags/m16.0', 'refs/tags/m16.0^{}'), 'remote m16.0 already exists')
require(not git('diff', '--cached', '--name-only'), 'index has staged changes')
for name, value in seal['files'].items():
    path = root / name
    require(path.is_file() and sha(path) == value['sha256'] and path.stat().st_size == value['bytes'], 'selected file differs: ' + name)
for name in inventory['delete']:
    require(not (root / name).exists(), 'intended deletion reappeared: ' + name)
paths = (folder / 'stage-paths.txt').read_text().splitlines()
require(paths == sorted(inventory['addOrModify'] + inventory['delete']), 'stage path list differs')
self_path = identity.relative_to(root).as_posix()
require(set(seal['files']) | {self_path} == set(inventory['addOrModify']), 'identity coverage differs')
require(not set(paths) & set(inventory['excludeLeaveUntracked']), 'exclusion overlaps staging')
require(all(not n.startswith(('build/', 'MinimumRecorder/', '.ksa-', 'docs/legal/')) for n in paths), 'forbidden staging root')
changed = set(git('diff', '--name-only', '-z').split('\0')) - {''}
untracked = set(git('ls-files', '--others', '--exclude-standard', '-z').split('\0')) - {''}
require(changed | untracked == set(paths) | set(inventory['excludeLeaveUntracked']), 'current changes contain unclassified or missing paths')
frozen = json.loads((root / 'docs/engineering-evidence/minimum-recorder-bounded-storage/candidate-identity.json').read_text())
all_names = set(git('ls-files', '--cached', '--others', '--exclude-standard', '-z').split('\0')) - {''}
runtime_names = {n for n in all_names if (root / n).is_file() and
    (n.startswith(('src/', 'native/', 'samples/', 'assets/', 'external/', 'tools/NovaCore.')) or
     n in {'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'NovaCore.sln', 'global.json', 'NuGet.Config'})}
require(runtime_names == set(frozen['runtimeSourceManifest']), 'runtime source membership differs')
for section, base in [('runtimeSourceManifest', root), ('permanentTestManifest', root), ('packageManifest', root / 'tools/NovaCore.App/bin/Release/net10.0-windows')]:
    for name, value in frozen[section].items():
        expected = value if isinstance(value, str) else value['sha256']
        require((base / name).is_file() and sha(base / name) == expected, section + ' differs: ' + name)
package = root / 'tools/NovaCore.App/bin/Release/net10.0-windows'
require({f.relative_to(package).as_posix() for f in package.rglob('*') if f.is_file()} == set(frozen['packageManifest']), 'package inventory differs')
print('PASS: exact pre-staging inventory, frozen source/tests/package, Git and absent m16.0 verified.')
print('Cleanup remains blocked pending Project Control disposition. No staging or banking performed.')
