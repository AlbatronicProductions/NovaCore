"""Retain bounded gate results; raw rerunnable logs remain under build."""
import json, hashlib
from pathlib import Path

root = Path(__file__).resolve().parents[2]
build = root / 'build/modular-craft-first-playable'
evidence = root / 'docs/engineering-evidence/modular-craft-first-playable'
reports = []
for name in ['gate10-qualification', 'gate10-regressions', 'gate10-extra-regressions', 'gate10-launcher-regression', 'gate10-final']:
    path = build / (name + '.json')
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    rows = data if isinstance(data, list) else [data]
    compact = []
    for row in rows:
        item = {k: v for k, v in row.items() if k != 'output'}
        output = row.get('output', [])
        if isinstance(output, str): output = output.splitlines()
        item['outcomes'] = [line for line in output if any(token in line for token in ['PASS', 'passed', 'Build succeeded', 'Error(s)', 'FLIGHT_ORACLE', 'CONTACT_IMPULSE'])]
        compact.append(item)
    reports.append(dict(file=path.relative_to(root).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest(), results=compact))
(evidence/'gate10-results.json').write_text(json.dumps(dict(reports=reports, invocationCorrection='The initial Launcher invocation used net10.0 and executed no test. Corrected net10.0-windows invocation passed 19 tests; original raw failure retained.', finalSource='gate10-final.json reruns builds, Gate 9, dynamics and flight after the final publication guard.'),indent=2)+'\n')
measures = {name: json.loads((build/(name+'.json')).read_text(encoding='utf-8-sig')) for name in ['gate10-measure','gate10-final-measure']}
(evidence/'gate10-measurements.json').write_text(json.dumps(measures,separators=(',',':'))+'\n')
