# Reproduce the frozen generation

Use PowerShell 7, Python 3, the toolchain in [build-windows.md](../../build-windows.md)
and resolved Git LFS source. No main index staging or commit is needed to reproduce
the prospective unbanked generation. From its source root:

```powershell
python docs/engineering-evidence/stabilization-bank-gauntlet/prepare-reproduction.py --destination build/stabilization-bank-gauntlet/clean-source --output build/stabilization-bank-gauntlet/source-export.json
```

The destination must be absent. The script copies exactly inventoried source
bytes into an empty directory and creates only a fresh Git root marker. It does
not create a commit or copy any previous build/cache output. Then, inside that
root, run the documented native Debug/Release and managed solution builds.
Build these permanent native test targets in each configuration:

```powershell
cmake --build build/native-ninja --target NovaCorePreparedSubmissionTests NovaCoreRegionalPupilLifetimeTests NovaCoreTerrainTessellationTests NovaCoreCausalRecorderMock NovaCorePresentationResultTests
cmake --build build/native-ninja-release --target NovaCorePreparedSubmissionTests NovaCoreRegionalPupilLifetimeTests NovaCoreTerrainTessellationTests NovaCoreCausalRecorderMock NovaCorePresentationResultTests
python tools/verify-player-package.py --output build/player-package.json
```

Install/verify the two manifest-authorized terrain packages through AssetTool into
an explicit isolated cache. Required hashes: global
`38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae`
and Florida `c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe`.
Set `NOVACORE_ASSET_CACHE` for that process. No KSA or old diagnostic package is used.
Only after both terrain packages verify, run
`python docs/engineering-evidence/stabilization-bank-gauntlet/qualify-offline.py`.
Despite its historical filename, this is a CPU plus native/window regression
campaign, not headless-only: allow exclusive window control. It opens no blackout
replay route. Final source/package manifests are copied beside this report.

Run one native application route at a time with exclusive window control. Make
the result parent directory first; each route isolates its craft library:

```powershell
$app=(Resolve-Path tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe).Path
& $app --qualify-editor C:/evidence/stabilization/result.json --qualification-tank stabilization
& $app --qualify-editor C:/evidence/short/result.json --qualification-tank nc.tank.short-2
& $app --qualify-editor C:/evidence/long/result.json --qualification-tank nc.tank.long-2
```

These are engineering drivers through the production UI, not manual Player PASS.
Normal interactive launch uses the same EXE without arguments.

For hidden-output independence, copy the freshly built complete App package to a
new directory within that same source root, verify its manifest, temporarily move
only that fresh reproduction's generated `bin`/`obj` and native `build` trees into
a checked holding directory, and run the copied package. Keep repository assets
and the explicit verified terrain cache. This is a test of the documented
repository-layout package, not a standalone distribution claim.
