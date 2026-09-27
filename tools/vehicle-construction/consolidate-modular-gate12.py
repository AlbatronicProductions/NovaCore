"""Compact qualification provenance. Raw rerunnable output stays under build.

Run after final builds/native routes and the Gate 12 preservation seal. This
does not build, launch, clean, deploy, change Git, or infer manual acceptance.
"""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / 'build/modular-craft-first-playable'
OUT = ROOT / 'docs/engineering-evidence/modular-craft-first-playable'


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def identity(p):
    return dict(path=p.relative_to(ROOT).as_posix(), bytes=p.stat().st_size, sha256=sha(p))


def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))


def write(name, data):
    (OUT / name).write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')


reports = []
for name in ('gate12-qualification', 'gate12-admission-regressions', 'gate12-final', 'gate12-final-graphics'):
    path = BUILD / (name + '.json')
    rows = read(path)
    compact = []
    for row in rows:
        assert row['exitCode'] == 0, (name, row)
        item = {k: v for k, v in row.items() if k != 'output'}
        lines = row.get('output', [])
        if isinstance(lines, str):
            lines = lines.splitlines()
        item['outcomes'] = [line for line in lines if any(s in line for s in ('PASS', 'passed', 'Build succeeded', 'Error(s)'))]
        compact.append(item)
    reports.append(dict(**identity(path), results=compact))

routes = []
measurements = []
for name, role in (
    ('gate12-native-short', 'Project Control manually accepted this earlier short session'),
    ('gate12-native-long', 'Historical long route; ground-clearance refusal, F input delivery inconclusive'),
    ('gate12-native-long-final', 'Final Release long application integration'),
    ('gate12-native-short-final', 'Final Release short application integration'),
):
    p = BUILD / (name + '.json')
    data = read(p)
    lines = data['diagnostic'].splitlines()
    end = next(s for s in lines if s.startswith('MODULAR_FLIGHT_END'))
    assert f"source={data['source']}" in end
    if name.endswith('-final'):
        assert data['exitCode'] == 0 and 'failed=False reason=none' in end
    route = {k: v for k, v in data.items() if k != 'diagnostic'}
    route.update(identity(p))
    route.update(role=role, terminal=end)
    route['compiled'] = re.search(r'compiled=([a-f0-9]{64})', end).group(1)
    routes.append(route)
    raw_windows = [s.removeprefix('MODULAR_INTEGRATED_WINDOW ') for s in lines if s.startswith('MODULAR_INTEGRATED_WINDOW ')]
    windows = [json.loads(s) for s in raw_windows]
    if name.endswith('-final'):
        assert len(windows) == 9 and all(w['complete'] and w['samples'] == 256 for w in windows)
    measurements.append(dict(route=name, first4096=[s for s in lines if s.startswith('MODULAR_FRAME ')],
                             phaseWindows=windows, completePhaseEvidence=name.endswith('-final')))

write('gate12-results.json', dict(
    schema='novacore.modular-gate12-results/1', reports=reports, routes=routes,
    finalSource='Final Debug/Release builds and 9918 application checks, plus nine preserved graphics regressions. No production edits after these runs.',
    scope='Engineering/integration only. Earlier short manual PASS is separately recorded; final campaign Player PASS belongs to Project Control.',
))
write('gate12-measurements.json', dict(
    schema='novacore.modular-gate12-measurements/1',
    capture='Normal GC; 64 warmup frames per phase then three consecutive 256-frame windows. Phase re-entry retains phase sequence. Capture never limits simulation.',
    boundary='Full managed host callback plus separate native frame interval. First4096 is the earlier bounded global window, not phase-resolved.',
    limits=['All final phase windows have rcsFrames=0; integrated active-RCS frame pacing is not measured.',
            'Zero median callback allocation reflects callbacks without a canonical 64Hz step; physical service is not allocation-free.',
            'Historical first long combined phase line was truncated at 4096 characters. It is excluded from complete phase evidence.',
            'Frame outliers are retained; these samples are not whole-game performance certification.'],
    runs=measurements,
))

runtime = []
for config in ('Debug', 'Release'):
    for relative in (f'samples/NovaCore.Triangle/bin/{config}/net10.0',
                     f'tools/NovaCore.ConstructionEditor/bin/{config}/net10.0-windows'):
        directory = ROOT / relative
        runtime.extend(identity(p) for p in sorted(directory.iterdir()) if p.is_file() and p.suffix in ('.dll', '.exe', '.json'))
        runtime.extend(identity(p) for p in sorted(directory.rglob('*.spv')))
seal = read(OUT / 'gate12-seal.json')
assert all(seal[k] for k in ('headPreserved', 'indexPreserved', 'publicRefsPreserved', 'worktreesPreserved'))
assert not seal['missing'] and not seal['unexpected']
inventory = []
for relative, decision in (
    ('docs/engineering-evidence/modular-craft-first-playable', 'KEEP compact campaign evidence'),
    ('assets/vehicles/modular-starter', 'KEEP six canonical greybox definitions/content'),
    ('build/modular-craft-first-playable/entry', 'KEEP original recovery ZIP and index; no cleanup authority'),
    ('build/modular-craft-first-playable', 'REBUILDABLE plus recovery/probes; retained pending separately reviewed cleanup'),
):
    paths = [p for p in (ROOT / relative).rglob('*') if p.is_file() and p != OUT / 'campaign-closure.json']
    inventory.append(dict(path=relative, files=len(paths), bytes=sum(p.stat().st_size for p in paths), decision=decision))
write('campaign-closure.json', dict(
    schema='novacore.modular-campaign-closure/1', engineering='PASS', integration='PASS',
    manualPlayer='Short earlier Release session PASS from user; final campaign acceptance pending Project Control',
    banked=False, head=seal['head'], preservation={k: seal[k] for k in ('headPreserved', 'indexPreserved', 'publicRefsPreserved', 'worktreesPreserved', 'entryFiles', 'unchangedEntryFiles')},
    changedEntryFiles=len(seal['changedEntryFiles']), sourceFiles=len(seal['source']), sourceSeal=identity(OUT / 'gate12-seal.json'),
    runtime=runtime, inventory=inventory, inventoryNote='Evidence subtotal excludes this self-describing closure file. Build total includes its entry subtree; overlapping totals must not be added.',
    provenance='Ordinary Debug/Release candidate builds refreshed sample/editor/test/launcher outputs. No prepare-launcher or deployment promotion, commit, tag, push, bank, or cleanup. Prior manually deployed camera candidate is historical, not this campaign deployment.',
    ksaWrites=0, sourceReview='gate12-integration.md',
))
size = sum(p.stat().st_size for p in OUT.rglob('*') if p.is_file())
assert size <= 2_000_000, size
print(f'Gate 12 compact evidence: {len(reports)} report groups; {len(routes)} routes; {len(runtime)} runtime identities; {size} bytes / 2000000')
