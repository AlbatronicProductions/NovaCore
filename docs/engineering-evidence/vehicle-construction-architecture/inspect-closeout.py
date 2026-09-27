"""Read-only campaign preservation/storage inventory; output JSON to stdout."""
import hashlib
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
package = Path(__file__).resolve().parent
entry = json.loads((package / 'entry-seal.json').read_text(encoding='utf-8-sig'))
def git(*args):
    return subprocess.check_output(['git', *args], cwd=root).decode('utf-8').strip()
def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()

changed, missing = [], []
for name, old in entry['files'].items():
    path = root / name
    if not path.is_file():
        missing.append(name)
    elif sha(path) != old:
        changed.append(name)
names = set(git('ls-files', '-z').split('\0')) | set(git('ls-files', '--others', '--exclude-standard', '-z').split('\0'))
names.discard('')
generated_seal = 'docs/engineering-evidence/vehicle-construction-architecture/identity.json'
new = []
for name in sorted(names - entry['files'].keys() - {generated_seal}):
    path = root / name
    if path.is_file():
        new.append({'path': name, 'bytes': path.stat().st_size, 'sha256': sha(path)})
index = Path(git('rev-parse', '--git-path', 'index'))
if not index.is_absolute(): index = root / index
refs_hash = hashlib.sha256(git('show-ref', '--heads', '--tags').encode()).hexdigest()
index_hash = sha(index)
allowed = {'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDesign.cs', 'tests/NovaCore.Simulation.Tests/Program.cs',
           'src/NovaCore.Simulation/Properties/AssemblyInfo.cs', 'src/NovaCore.Simulation/Spacecraft/SpacecraftStateStore.cs',
           'src/NovaCore.Simulation/Spacecraft/SpacecraftStateView.cs', 'src/NovaCore.Simulation/Transactions/SimulationState.cs'}
preexisting_launcher = ['tests/NovaCore.Launcher.Tests/Program.cs', 'tools/NovaCore.Launcher/LaunchCommandBuilder.cs',
                       'tools/NovaCore.Launcher/LaunchConfiguration.cs', 'tools/NovaCore.Launcher/ScenarioCatalog.cs']
groups = {}
for file in new:
    category = 'disposable-cache-retained' if '/__pycache__/' in file['path'] else 'evidence' if file['path'].startswith('docs/') else 'content' if file['path'].startswith('assets/') else 'source-tests-tooling'
    groups[category] = groups.get(category, 0) + file['bytes']
artifacts = {}
for relative in ['src/NovaCore.Simulation/bin/Debug/net10.0/NovaCore.Simulation.dll',
                 'src/NovaCore.Simulation/bin/Release/net10.0/NovaCore.Simulation.dll',
                 'tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll',
                 'tools/NovaCore.ConstructionEditor/bin/Release/net10.0/NovaCore.ConstructionEditor.dll',
                 'tools/NovaCore.ConstructionEditor/bin/Release/net10.0/NovaCore.Simulation.dll']:
    path = root / relative
    if path.is_file(): artifacts[relative] = {'sha256': sha(path), 'bytes': path.stat().st_size}
result = {
    'judgment': 'PASS bounded construction engineering; manual acceptance PENDING; STOP FOR PROJECT CONTROL; UNBANKED',
    'head': git('rev-parse', 'HEAD'), 'entry_head_unchanged': git('rev-parse', 'HEAD') == entry['head'],
    'refs_sha256': refs_hash, 'refs_unchanged': refs_hash == entry['refs_sha256'],
    'index_sha256': index_hash, 'index_unchanged': index_hash == entry['index_sha256'],
    'entry_files_compared': len(entry['files']), 'changed_entry_files': sorted(changed), 'missing_entry_files': missing,
    'only_authorized_entry_changes': set(changed) == allowed and not missing,
    'preexisting_launcher_edits_preserved': all(name not in changed and name not in missing for name in preexisting_launcher),
    'changed_entry_hashes': {name: sha(root / name) for name in sorted(changed)},
    'added_files_excluding_this_seal': new, 'added_storage_bytes_by_category': groups,
    'build_artifacts_not_deployed': artifacts,
    'ksa_dll_sha256': sha(Path('E:/Kitten Space Agency/KSA.dll')),
    'self_reference_policy': 'identity.json is excluded from its own added-file hash set; all other current nonignored additions are inventoried',
    'status': git('status', '--porcelain=v1')
}
print(json.dumps(result, indent=2))
