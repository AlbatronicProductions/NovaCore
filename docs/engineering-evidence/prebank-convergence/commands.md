# Reproduction and recovery

Run from `E:\NovaCore`, Windows x64, matching toolchain in
[`../../build-windows.md`](../../build-windows.md). Keep interactive test windows
free of user/automation interference. Close ordinary NovaCore before rebuilding
its package; Windows correctly locks its loaded native DLL.

## Preservation

`preservation-baseline.json` records the initial branch/HEAD/peeled bank, index
identity, exact changed/additional file hashes and status. `accepted-shader.diff`
and `accepted-added-files.zip` preserve the accepted pre-convergence candidate,
without another canonical checkout. These are recovery material, not an
instruction to overwrite subsequent work or the index. Inspect current status
and compare hashes before any recovery. No checkout/reset/staging was performed.

The accepted shader report's existing clean/incremental/test-before-package
receipts remain valid for unchanged producer/deployer source. Current tests
generate their own shaders before final package verification. The independent
negative suite mutates disposable copies only.

## Final automated gates

From the VS x64 developer environment, build each configuration's native targets:

```powershell
cmake --build build/native-ninja --target NovaCore.Native NovaCoreRegionalPhysicalTests NovaCoreFacilityVisibilityTests NovaCoreSurfaceMaterialCoordinatesTests NovaCoreStellarProjectionTests NovaCorePreparedSurfaceRasterTests NovaCoreTerrainTessellationTests NovaCorePlayerGpuMemoryTests NovaCoreMappedBufferMemoryTests
# Repeat with build/native-ninja-release.
```

Run `NovaCoreRegionalPhysicalTests.exe` with the verified Florida cache file
identified by SHA-256
`c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.
Run `NovaCorePreparedSurfaceRasterTests.exe`, `NovaCoreTerrainTessellationTests.exe`,
`NovaCorePlayerGpuMemoryTests.exe` and `NovaCoreMappedBufferMemoryTests.exe` without
arguments in each build root. These are CPU-only native regressions; the GPU
presentation cases use the canonical controlled runner below.

```powershell
$env:NOVACORE_P2S5F_ARTIFACT_INPUT='E:\NovaCore\assets\planetary-nested-scale-mesh'
foreach ($configuration in 'Debug','Release') {
    dotnet build NovaCore.sln -c $configuration --nologo
    # Preserve output separately for each configuration; no exhaustive switches.
    $env:NOVACORE_HORIZON_REPORT="E:\NovaCore\build\prebank-convergence\repro-horizon-$configuration"
    $env:NOVACORE_OFFLINE_REPORT="E:\NovaCore\build\prebank-convergence\repro-offline-$configuration.json"
    $env:NOVACORE_OFFLINE_REPLAY_REPORT="E:\NovaCore\build\prebank-convergence\repro-replay-$configuration.json"
    dotnet run --project tests/NovaCore.Graphics.Tests -c $configuration --no-build
    dotnet run --project tests/NovaCore.Graphics.Tests -c $configuration --no-build -- --native-gpu
    dotnet run --project tests/NovaCore.Player.Tests -c $configuration --no-build
    python tools/verify-player-package.py --configuration $configuration --output "build/prebank-convergence/repro-package-$configuration.json"
}
python tools/test-runtime-shader-deployment.py
git diff --check
```

Require each command's exit code independently; the loop is a recipe, not an
aggregate PASS oracle. The initial diagnostic Debug run preceded the final window
prerequisite and depth-clear guards; its 112/116 result is retained as a failure.
The initial Release build hit the ordinary smoke process's native-DLL lock and
did not run Release Graphics. Neither attempt is relabelled as passing.

## DPI and ordinary smoke

Use Windows Display Settings to select the actual target display, record its
resolution/scaling, change 100% to 125%, then start NovaCore. The transition probe
uses the existing manual-start option in a process-local environment:

```powershell
$env:NOVACORE_QUALIFICATION_MANUAL_START='1'
& tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe --qualify-editor E:\NovaCore\build\prebank-convergence\repro-dpi.json --qualification-tank player-entry
```

Activate the window and click START. Require DeviceDpi 120, matching parent/host/
native extents and completed assertions. In a fresh shell without that override,
launch ordinary NovaCore without arguments and follow [`dpi.md`](dpi.md).
Restore original Windows scaling and verify resolution unchanged after the run.
Do not infer physical hardware keys, focus-loss or monitor transitions from this.

Raw recorder sessions stay under their existing owner and retention policy.
The first diagnostic offline workload/replay outputs were copied locally, then
the pre-existing historical output files were restored from their preserved
copies. Final runs use distinct paths, leaving those historical bytes unchanged.
