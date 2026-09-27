# Reproduce

Use the repository's documented Windows toolchain and resolved LFS assets.
Build native Debug/Release, then managed solution Debug/Release with
`dotnet build NovaCore.sln -c <configuration> --no-incremental`.
Build native target `NovaCoreFacilityVisibilityTests` in each configuration.
Its SPIR-V is now `build/<native-dir>/test-shaders/facility_visibility_test.comp.spv`.
Run the executable with that path as its argument. On this host only, missing
Epic overlay manifests contaminate loader diagnostics: set the process-local
`VK_IMPLICIT_LAYER_PATH` to an empty task directory. Explicit Vulkan validation
stays enabled; no machine settings or driver registry are altered.

Run Graphics.Tests with `--florida-pad-authority`,
`--florida-pad-flight-envelope`, `--florida-slab-correctness`,
`--florida-slab-solar`, `--assembly-florida-presentation`,
`--florida-slab-camera-warp`, `--modular-connectors`, and modular gates 9–12.
Retain the prior construction/capacity suite and run the native stabilization
route for arbitrary-depth construction. Native window routes are sequential.

```powershell
$app = 'E:/NovaCore/tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe'
& $app --qualify-editor E:/evidence/short/result.json --qualification-tank nc.tank.short-2
& $app --qualify-editor E:/evidence/long/result.json --qualification-tank nc.tank.long-2
& $app --qualify-editor E:/evidence/two-tank/result.json --qualification-tank deep
& $app --qualify-editor E:/evidence/construction/result.json --qualification-tank stabilization
python tools/verify-player-package.py --output build/player-package.json
```

Create result parent directories first. `deep` is the existing diagnostic
selector, now explicitly a two-short-tank/13-part admitted flight witness, not
a promise of arbitrary-depth flight. Each route uses a fresh isolated craft
library. No revision-1 save conversion occurs.

For clean reproduction use the gauntlet's `prepare-reproduction.py` with a new
absent destination under `build`, then configure native from scratch and build
Release there. This exports exact prospective unbanked source, not banked HEAD.
No old binary/cache is copied. Verify the package, install/verify the two
manifest-authorized terrain packages into an explicit cache with AssetTool,
and run the canonical routes. Preserved terrain hashes and full independent
runtime procedure are in the parent gauntlet reproduction instructions.
