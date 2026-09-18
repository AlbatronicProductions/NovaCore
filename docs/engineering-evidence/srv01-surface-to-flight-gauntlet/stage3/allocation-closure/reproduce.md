# Reproduction

Use the source seals in identity.json, .NET SDK 10.0.303, accepted repository BEPU binaries and existing native build tooling. No external BEPU package restore or binary changes. Current files are unbanked; preserve them before reconstructing a historical witness.

## Current qualification

In PowerShell 7 from `E:\NovaCore`, prepare unchanged native prerequisites explicitly, then build the full solution (the default native output was absent during this run):

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$env:VULKAN_SDK='C:\VulkanSDK\1.4.357.0'
foreach ($configuration in 'Debug','Release') {
    $suffix=$configuration.ToLowerInvariant()
    cmake -S native/NovaCore.Native -B "build/srv01-stage3/native-$suffix" -G Ninja "-DCMAKE_BUILD_TYPE=$configuration"
    if ($LASTEXITCODE) { throw 'Native configure failed' }
    cmake --build "build/srv01-stage3/native-$suffix"
    if ($LASTEXITCODE) { throw 'Native build failed' }
    dotnet build NovaCore.sln -c $configuration --artifacts-path build/srv01-stage3/artifacts -p:ContinuousIntegrationBuild=true "-p:NativeBuildDirectory=srv01-stage3/native-$suffix" --nologo -v:q
    if ($LASTEXITCODE) { throw 'Managed build failed' }
}
```

Use each built `.../artifacts/bin/NovaCore.Simulation.Tests/debug|release/NovaCore.Simulation.Tests.dll` with `--assembly-departure-validation`, then no argument for all 79 registered groups. `--assembly-departure-storage` reports owned storage; `--assembly-departure-performance` is the separate serial three-fresh-process population documented in performance-plan.md. Never average allocation or retry a failed performance population into a pass.

Run `NovaCore.ReferenceFrames.Tests`, `NovaCore.Precision.Tests`, `NovaCore.BepuDependency.Tests` in both configurations. Graphics routes: `--assembly-presentation`, `--assembly-powered-support`, `--certified-continuation-regression`, `--powered-contact-presentation`. Launcher uses an unchanged five-parent repository-root test assumption; copy its entire built output to `build/srv01-stage3/launcher/debug|release/net10.0` before executing it, so that assumption remains true. No renderer/GPU campaign is needed for this headless change.

`collect-results.py` extracts concise proof/measurement records from the named disposable logs without executing anything. It requires the logs to exist; retained results remain readable after cleanup. Use equivalent redirection names from validation.json and the script if reproducing that extraction.

## Historical 624 B witness and causal attribution

Do this only in a disposable copy of the exact source snapshot, never over unrelated candidate work. Replace only LocalContactWorld.cs with the exact entry in `pre-correction-world.zip` (SHA in allocation-ledger.json). Build Simulation Debug and run `--assembly-departure-validation`. The unchanged 16-world warmup and fresh-world handoff measurement reproduce the historical first-use 624 B failure; the first free-flight check is then unreached.

In that pre-correction snapshot, `attribute.py` inserts temporary scalar boundaries around actual service/native stages and snapshots the native object graph outside the allocation window. This historical script contains its original `r=Path('E:\\NovaCore')`: change that one root in the disposable script copy to the disposable source tree before running; do not invoke it against the live candidate. Its `finally` restores all changed source bytes before the diagnostic executable runs. The same-type array-size checks also occur outside measurement. No observer/profiler/EventPipe is required. Review exact anchored replacements before running against any later source; it deliberately refuses ambiguous anchors.

To inspect dependency ownership read the exact accepted BepuPhysics/BepuUtilities assemblies using the installed ILSpy tool. Relevant types: `BepuPhysics.Simulation`, `BepuPhysics.CollisionDetection.NarrowPhase<T>`, `ConstraintRemover`, `BepuUtilities.Memory.BufferPool`. Retain concise method/factory findings, not bulk decompiled source.

## Preservation

To reconstruct qualified Stage2 in a disposable tree: use `../qualified-stage2-overlap.zip` for its five overlapped source files, this directory's `pre-correction-world.zip` for LocalContactWorld.cs, and baseline Git for AssemblyDynamics.cs; exclude Stage3-only AssemblyDeparture.cs, AssemblyDepartureTests.cs, AssemblyDepartureProofTests.cs and AssemblyDepartureMeasurements.cs. identity.json proves all 24 original Stage2 seals preserved. Do not remove or overwrite current candidate files in place to run a historical baseline.

All disposable outputs stay under the exact reviewed roots in cleanup.md. KSA remains read-only and no proprietary source is retained.
