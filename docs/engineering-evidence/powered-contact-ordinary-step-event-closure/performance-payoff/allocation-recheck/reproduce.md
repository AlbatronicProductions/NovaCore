# Reproduction (not authorization for another campaign)

Use the same repo/source identities in identity.json, .NET SDK10.0.303/runtime
10.0.12 x64, normal runtime configuration. No profiler, tiering/PGO overrides,
custom priority/affinity or global no-GC mode. This ticket's bounded executions
are complete; these commands preserve reproducibility for later authorization.

Prerequisite: the exact disposable output root below must be absent. Scripts
refuse existing phase outputs rather than overwrite or retry. Historical
allocation-closure/projection-contract files and the original off-COM witness
are inputs only. Build products are under build/, never production dependencies.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
& '.\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\recheck.ps1'
# Continue only after all six bits/exact ledger match and every map window is 0 B.
& '.\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\native.ps1'
# Continue only after the complete single native comparison passes.
& '.\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\timing.ps1'
```

recheck.ps1 derives the active source mechanically from the historical correction,
changing only diagnostic guards and Map visibility. It links unchanged banked
PropellantInteger/PoweredFlightNumerics and the qualified permanent allocation
helper. The exact ledger/raw source setup is cold. No per-event answer is cached.
native.ps1 derives the original world and replaces only the six input projections.
timing.ps1 extracts original native types and compiles Timing.cs. Three fresh
processes are run sequentially; first failed runtime gate stops, no retries.

The first timing build in this ticket exposed a diagnostic signed/unsigned
storage-report Math.Max mismatch. It was fixed before any timing process ran;
the retained Timing.cs contains that metadata correction. No historical mapper
or source was changed and no failed timing run is hidden. See verification.md.

Scripts write new native/process result files at their declared evidence paths.
For an independently authorized reproduction, preserve the current accepted
result package first or direct the report destinations to a separate reviewed
location; do not overwrite this historical run merely to compare it.

No tiny-event benchmark was added. That optional extension is unnecessary to
complete this bounded campaign and its changing projection regime was not assumed
equivalent without proof. No Candidate A benchmark is rerun.
