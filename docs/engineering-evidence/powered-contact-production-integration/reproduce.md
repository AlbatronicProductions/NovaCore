# Reproduction and source map

These instructions preserve the ability to reproduce this stopped candidate. They are not
authorization to resume the failed acceptance campaign. Current state is REVISE/UNBANKED.

## Exact environment and inputs

Windows11 build26200, x64, .NET SDK10.0.303, host/runtime10.0.12; pinned repository BEPU
2.5.0-beta.29. No profiler, solver/tiering/PGO/global-GC configuration change. Original
accepted design and numerical evidence remain in their existing directories.

Use the candidate source fingerprints in identity.json. No committed candidate exists.

## Commands used

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --artifacts-path 'E:\NovaCore\build\powered-contact-production-integration\artifacts' --nologo -v minimal
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --artifacts-path 'E:\NovaCore\build\powered-contact-production-integration\artifacts' --nologo -v minimal
```

Each focused route was run once per configuration after its implementation compiled.
Cheap Debug was additionally rerun after the prequalification admission review corrections.
The six Release focused commands ran sequentially, stopping on any nonzero exit:

```powershell
$binary = 'E:\NovaCore\build\powered-contact-production-integration\artifacts\bin\NovaCore.Simulation.Tests\release\NovaCore.Simulation.Tests.dll'
foreach ($gate in @('cheap','physics','work','authority','sequences','allocation')) {
    dotnet $binary "--powered-contact-$gate"
    if ($LASTEXITCODE -ne 0) { throw "Stopped at $gate" }
}
```

Debug uses the equivalent `debug` path. The declared three-process timing campaign stopped
after its first command failed; no second/third invocation occurred:

```powershell
dotnet 'E:\NovaCore\build\powered-contact-production-integration\artifacts\bin\NovaCore.Simulation.Tests\release\NovaCore.Simulation.Tests.dll' --powered-contact-cost
if ($LASTEXITCODE -ne 0) { throw 'Performance gate failed; do not retry or continue' }
```

The seven CLI routes are in Program.cs. They are not yet registered in its ordinary
full-suite test array. Allocation uses the existing checked OrdinaryAllocationMeasurement
and independent byte[128] control; timing does not use its no-GC boundary.

## Native row observer provenance

`generate-work-observer.ps1` reproduces the permanent **test-only** NativeRows.cs from the
exact pinned upstream source, SHA256
`B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5`.
It asserts12 native call sites and byte-exact recovery after deleting recorder calls and
reversing names. The upstream file is disposable; its URL/hash, generator, Apache-2.0 license and
permanent generated test source remain. The generator never runs a world. The pinned
dependency DLLs are not modified or replaced.

## Production paths (relative to repository)

| Path | Responsibility |
|---|---|
| src/NovaCore.Simulation/Spacecraft/Actuation/OrdinaryContactInputProjection.cs | New bounded exact-duration transport mapper |
| src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFreeFlight.cs | Explicit physical consumer; version2 contact provenance |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/PoweredContactPreparation.cs | New cold one-use native preparation; finite fixture admission |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Powered.cs | New powered authority/receipt/input/acknowledgement binding |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs | Single prepared external-acceleration input |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactSource.cs | Resource-aware cold numerical fixture capture |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs | Powered one-outstanding and post-step invalidation checks |
| src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs | Powered expected-authority validation and old publisher exclusion |
| src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredContact.cs | New contact consumer routes and lifecycle |
| src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredFreeFlight.cs | Shared joint commit/host accounting owner; separate consumer selection |

## Permanent test draft paths

All below are under tests/NovaCore.Simulation.Tests:

- Program.cs: focused route registration only.
- PoweredContactTests.cs: cold fixture, genuine lease construction, cheap exact lifecycle.
- PoweredContactTests.Physics.cs: independent rational chronological oracle and native rows.
- PoweredContactTests.NativeRows.cs: generated, source-recovered test observer.
- PoweredContactTests.Work.cs: exact discrete row-work accounting and equivalence witness.
- PoweredContactTests.Authority.cs: refusal/nonmutation/terminal/property-only checks.
- PoweredContactTests.Qualification.cs: partitions, moving frame, allocation, stopped cost gate.

The original free-flight evaluator/numerics, exact segmentation, command authority, BEPU
binaries/pins, qualified allocation helper and existing test thresholds remain unchanged.
Shared owner source changed intentionally; protected dynamic regressions still need execution.

## Outstanding follow-up

Classify the complete-operation cost without attributing it from aggregate timing. Preserve
the raw timing label error and actual engine-off/wet population. Obtain Project Control scope
before resuming performance, adding normal-runner integration, building the visible route,
or running broad regressions. No acceptance is implied by successful focused tests.
