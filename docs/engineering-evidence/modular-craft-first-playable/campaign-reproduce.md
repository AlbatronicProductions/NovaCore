# Reproduce final campaign evidence

Run from `E:\NovaCore`. Preserve the current worktree and entry recovery; do not clean or reset. The existing x64 native caches are `build/modular-craft-first-playable/native-debug` and `native-release`. If rebuilding native, use the Visual Studio x64 development environment and the configured Vulkan SDK. Do not reuse the unrelated x86 `native-ninja-release` cache.

```powershell
pwsh -NoProfile -File tools/vehicle-construction/qualify-modular-gate12.ps1
python tools/vehicle-construction/author-modular-starter.py --check
```

The independent geometry checker requires Python with NumPy. The default shell Python on this host lacks NumPy; its first final recheck stopped at import and performed no geometric test. Use the existing bundled runtime (no package installation required):

```powershell
& 'C:\Users\Tyler\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools/vehicle-construction/check-modular-geometry.py
```

The qualification script builds the solution and editor in Debug/Release with their explicit matching native candidate directory, runs Gates 2–12, preserved graphics/control/reference/precision/dependency/launcher suites and complete admission/construction/SRV regressions. Gate-specific performance/oracle reproduction remains in the respective Gate 4–10 documents; it is not replaced by whole-suite elapsed time.

Actual native editor-to-flight routes (run one at a time, leaving the editor driver window alone until flight opens):

```powershell
$env:NOVACORE_CONTROL_TRACE='1'
tools/NovaCore.ConstructionEditor/bin/Release/net10.0-windows/NovaCore.ConstructionEditor.exe --catalog assets/vehicles/modular-starter/catalog.json --asset-root assets/vehicles/modular-starter --qualify-launch build/modular-craft-first-playable/repro-short.json --qualification-tank nc.tank.short-2
tools/NovaCore.ConstructionEditor/bin/Release/net10.0-windows/NovaCore.ConstructionEditor.exe --catalog assets/vehicles/modular-starter/catalog.json --asset-root assets/vehicles/modular-starter --qualify-launch build/modular-craft-first-playable/repro-long.json --qualification-tank nc.tank.long-2
```

For each flight leave at least 6 seconds supported, then Z, at least 15–20 seconds powered, X and at least 6 seconds coast before closing. Exercise controls and camera as described in the manual route; adding RCS changes the sampled workload. Capture nine complete 256-frame windows; inspect exitCode and terminal failed/reason, not just the helper's completion label. The helper's running per-frame invariant counter is not a count of distinct tests. Fresh identities/initial epochs and host-driven trajectory endpoints will differ; compare contract outcomes and within-session seals.

The final compact consolidator reads the named historical/final JSON artifacts; it does not silently replace them with new runs. Reproduction outputs use separate names to preserve original evidence. To recreate the exact compact package, retain the raw inputs listed in `gate12-results.json`, then:

```powershell
python tools/vehicle-construction/seal-modular-evidence.py --gate 12
python tools/vehicle-construction/consolidate-modular-gate12.py
git diff --check
```

`gate12-final.json` records a targeted final solution/editor build and `--modular-gate12-application` run in each configuration; `gate12-final-graphics.json` records the final nine preserved graphics/control flags listed in the qualification script. These targeted repetitions followed the last integration edits. Do not interpret historical failed or truncated observations as accepted final runs.

## Retention / cleanup inventory

KEEP source, tests, six greybox definitions/assets, compact evidence, accepted planning inputs, existing banked history and unrelated pre-existing work. KEEP `build/modular-craft-first-playable/entry/pre-existing-work.zip` and copied index as recovery until separately authorized. No asset source or unique evidence is automatically retired.

Native/managed build outputs, large logs, IO witnesses and UI capture JSON under build are regenerable but currently retained. `campaign-closure.json` inventories their current aggregate footprint and the exact runtime identities. Build aggregate includes the separately counted entry subtree; do not add overlapping totals. No deletion command is authorized by this inventory. No deployment script, commit, tag, push or bank is part of reproduction.
