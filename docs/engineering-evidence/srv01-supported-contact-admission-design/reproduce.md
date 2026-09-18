# Reproduce this design review

This is source/ownership design evidence, not execution qualification. No builds,
benchmarks, new permanent tests, production patches or disposable output were
created for this ticket. Evidence budget: 192 KiB. Retain concise contracts,
method identities, history provenance, gate judgments and a bounded implementation
slice; do not retain KSA source, chat exports or assets.

## Baseline

From `E:\NovaCore` in PowerShell 7:

```powershell
git rev-parse HEAD main origin/main
git ls-remote origin refs/heads/main
git branch --show-current
git rev-parse 'm15.2-srv01-reusable-production-spacecraft-integration^{}'
git status --short
git diff --check
git diff --cached --check
git diff --name-only c3de150db8563864d23c4951b4630559453d8f56 HEAD -- src tests samples native assets tools
git show-ref --tags
```

Compare with repo-truth.json; inspect source changes if refs differ. Do not treat
an old report as evidence that a newer checkout has the same behavior.

## Current KSA first

Read actual installed metadata/hashes, not an old decompile:

```powershell
$ksaFiles = 'E:\Kitten Space Agency\KSA.dll',
    'E:\Kitten Space Agency\BepuPhysics.dll',
    'E:\Kitten Space Agency\BepuUtilities.dll',
    'E:\Kitten Space Agency\KSA.deps.json'
$ksaFiles | ForEach-Object {
    Get-FileHash -LiteralPath $_ -Algorithm SHA256
    (Get-Item -LiteralPath $_).VersionInfo.ProductVersion
}
Get-Content -LiteralPath 'E:\Kitten Space Agency\Content\Versions\v2026.9.X.5438.json'
& 'E:\NovaCore\docs\engineering-evidence\srv01-production-integration\inspect-current-ksa.ps1' -Types VehicleUpdateTask,PhysicsBubble,ConstraintSim,VehicleUpdateState,VehicleProperties,Vehicle,PartTree -List
dotnet 'C:\Users\Tyler\AppData\Local\Temp\ilspycmd-11.0.0.9375\tools\net10.0\any\ilspycmd.dll' --disable-updatecheck -m 0x06001C19 'E:\Kitten Space Agency\KSA.dll'
```

Substitute tokens from the owner map to inspect that exact build to stdout.
If build/hash differs, discover current methods again; tokens are not stable API.
Read the actual authenticated #live-changelog URLs in ksa-live-history.md and
search newer relevant entries before accepting the map. No KSA writes or updates
are part of reproduction. Independent gate review must pass before NovaCore
mechanism design or a later implementation begins.

## NovaCore inspection after the KSA gate

Read source directly at the verified revision, following callers as described in
novacore-owner-map.md. Example bounded searches (no build or runtime launch):

```powershell
rg -n 'TryPrepareAppliedSlot|TryPrepareAssemblySlot|InstallAssembly|IsAssembly' src/NovaCore.Simulation
rg -n 'CheckAssemblySource|PrepareAssemblyInOwnedPhase|PublishAssemblyInOwnedPhase|AdmitAssemblyHostTime' src/NovaCore.Simulation/Transactions
rg -n '15625|4096|128|Calculate|ObserveMass|PartDefinitionData' src/NovaCore.Simulation/Spacecraft/Assemblies src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Assemblies.cs
rg -n 'TryEndpoint|Timestep|Bind|Acknowledgement|Invalidated|Dispose' src/NovaCore.Simulation/Spacecraft/Contact/Staging
rg -n 'PositionO|VelocityO|MaterialVelocityAtCurrentCom|TryGetAssembly' src/NovaCore.Simulation samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs
rg -n 'Replay|replay|schema|PrepareAssemblyFlight' src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.AssemblySave.cs
```

Compare catalog/part declarations with the visual manifest; absence of collision
metadata must not be replaced by an inferred collider. Re-read the typed O/COM
evaluator and current resource interval guard. Current source wins over line
numbers in this packet. qualification-plan.md describes future execution only;
it is not permission to run tests from this design ticket.

Final non-destructive closeout compares67 captured tag refs, main/remote identity,
all44 preexisting untracked file hashes and the inspected KSA inputs. Git tracked
diff and index must remain empty. New evidence is untracked and therefore also
receives a direct trailing-whitespace/link check; git diff --check alone does not
inspect it. See closeout.json for exact results.

## Cleanup

New disposable paths: **none**. New disposable files/bytes: **0 / 0 B**.
There is no deletion command to execute. Existing unrelated build trees, source,
archives and the prior audit evidence are outside this ticket and left untouched.
All new files are concise retained evidence under this responsibility directory.
