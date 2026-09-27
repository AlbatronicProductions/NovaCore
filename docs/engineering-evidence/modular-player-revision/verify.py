"""Rebuild and replay preserved CPU/ABI regressions; never publishes a candidate.

Run from the repository root with Python 3.11+. Native Debug/Release builds and
the authoritative Florida dataset must already exist (see reproduce.md).
Actual windowed application routes are separate so performance is not polluted.
"""
from pathlib import Path
import datetime
import hashlib
import json
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[3]
affected = "--affected" in sys.argv
OUT = ROOT / ("build/modular-player-revision/regressions-final" if affected else "build/modular-player-revision/regressions")
OUT.mkdir(parents=True, exist_ok=True)
rows = []

def run(args):
    started = time.perf_counter()
    result = subprocess.run(["dotnet", *args], cwd=ROOT, capture_output=True)
    output = result.stdout + result.stderr
    log = OUT / f"{len(rows):02d}.log"
    log.write_bytes(output)
    row = dict(arguments=args, exitCode=result.returncode,
               elapsedMs=round((time.perf_counter()-started)*1000, 3),
               log=str(log.relative_to(ROOT)), bytes=len(output),
               sha256=hashlib.sha256(output).hexdigest(),
               outcomes=[s for s in output.decode("utf-8", errors="replace").splitlines()
                         if "PASS" in s or "passed" in s or "Build succeeded" in s
                         or "FAIL" in s or " Error(s)" in s or " Warning(s)" in s])
    rows.append(row)
    (OUT / "results.json").write_text(json.dumps(dict(
        checkedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        results=rows), indent=2), encoding="utf-8")
    print(f"{len(rows):02d} {'PASS' if result.returncode == 0 else 'FAIL'} {' '.join(args)}", flush=True)
    if result.returncode:
        print(output.decode("utf-8", errors="replace")[-5000:])
        sys.exit(result.returncode)

for config in ("Debug", "Release"):
    run(["build", "NovaCore.sln", "-c", config,
         f"-p:NativeBuildDirectory=modular-player-revision/native-{config.lower()}", "-v:q"])

baseline = json.loads((ROOT / "docs/engineering-evidence/modular-craft-first-playable/gate12-results.json").read_text(encoding="utf-8-sig"))
seen = set()
for report in ([] if affected else baseline["reports"]):
    for row in report.get("results", []):
        args = row.get("arguments")
        if not args or not args[0].endswith(".dll") or tuple(args) in seen:
            continue
        seen.add(tuple(args))
        run(args)
for config in ("Debug", "Release"):
    for option in (("--modular-viewport", "--modular-editor-regressions", "--modular-gate10-flight", "--modular-gate12-application") if affected else ("--modular-viewport", "--modular-greybox-assets", "--modular-editor-regressions")):
        run([f"tests/NovaCore.Graphics.Tests/bin/{config}/net10.0/NovaCore.Graphics.Tests.dll", option])
print(f"REGRESSION PASS: {len(rows)} build/test commands; Player PASS remains pending.")
