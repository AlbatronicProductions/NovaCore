# Reproduction and scope

All physics controls were run before Project Control stopped correction and requested physical-authority/profile feasibility. Do not rerun them as part of the closed ticket. The following preserves the ability to reproduce under a future authorized diagnostic, not permission to resume Stage 5.

## Inputs and cheap checks

Keep the current unqualified Stage-5 source/seals, predecessor archives and source data unchanged. `preflight.json` pins the executable inputs from `build/srv01-stage5/artifacts/bin/NovaCore.Graphics.Tests/debug`; `../reproduction.md` records original build provenance. Those binaries are intentionally retained, not silently replaced by later test-source builds.

Read-only checks:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/rotating-florida-contact-closure/mass-oracle.py
python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/development-profile-feasibility/derive.py
```

They regenerate concise analytical JSON only, not build directories or simulations. The first uses exact rational authored-data mass moments; the second computes the finite mission/containment stop. `analyze.py` reads the existing diagnostic logs and regenerates the witness/contact/frame summaries; it never runs the solver.

## Historical diagnostic harness

The exact final independently authored harness is retained under `reproduction`. It references the preserved Debug DLLs and existing root build properties. Reflection/readback is confined to this disposable harness; it is not a production feature or permanent test correction. The complete original state is recorded in preflight.

For a future explicitly authorized reproduction only:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
$evidence = 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\rotating-florida-contact-closure'
$scratch = 'E:\NovaCore\build\srv01-stage5-closure'
New-Item -ItemType Directory -Path "$scratch\probe" -Force | Out-Null
Copy-Item -LiteralPath "$evidence\reproduction\Probe.cs","$evidence\reproduction\Probe.csproj" -Destination "$scratch\probe"
dotnet build "$scratch\probe\Probe.csproj" -c Debug --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic build failed' }
foreach ($mode in 'canonical','raw-full','zero-rotation','flat-local','stock-local') {
    dotnet "$scratch\probe\bin\Debug\net10.0\NovaCore.Graphics.Tests.dll" $mode | Set-Content -LiteralPath "$scratch\$mode.txt"
    if ($LASTEXITCODE -ne 0) { throw "Diagnostic failed: $mode" }
}
Copy-Item -LiteralPath "$scratch\canonical.txt" -Destination "$scratch\canonical-contacts.txt"
python "$evidence\analyze.py"
python "$evidence\mass-oracle.py"
```

The final harness reads solver constraints in every selected snapshot; it need not rerun an otherwise identical canonical population just to produce `canonical-contacts.txt`. During the actual investigation that reader was added after the first populations, then one canonical readback run matched the prior motion/physical outputs exactly. This is recorded transparently, not presented as an independent performance sample.

Actual journal: initial harness build encountered two mechanical diagnostic source errors; they were corrected, then it compiled successfully. Five bounded diagnostic populations ran. A source-only contact-reader addition compiled successfully and one canonical constraint-readback replay matched exactly. No solver parameter, timestep, permanent test or production source was changed. No Release/full-suite/performance/profiler campaign ran.

## Source inspection

Current BEPU types read directly using existing ILSpy: `SpringSettingsWide`, `PenetrationLimitOneBody`, `Contact4OneBodyFunctions` from repository `external/bepu/2.5.0-beta.29/lib/BepuPhysics.dll`. Current KSA types/configuration/history are enumerated separately. Avoid bulk decompilation or retaining proprietary source.

## Cleanup

See [cleanup.md](cleanup.md) for exact counts/bytes, path-specific future manual command and non-destructive verification. No existing disposable root was removed. Older roots still carry preserved qualified checkpoints and build dependencies; their future deletion requires the stated retention release, not merely a name-based cleanup.
