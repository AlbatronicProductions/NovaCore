# Reproduce

Run from `E:\NovaCore`. Build matching native Debug/Release first if their outputs have been retired; canonical native Release identity is in `candidate-identity.json`. Ordinary development launch is `tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe` with no qualification arguments.

```powershell
python tools/vehicle-construction/author-modular-starter.py --check
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release
dotnet build tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj -c Debug
dotnet build tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj -c Release
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release
dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll --modular-connectors
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll --modular-connectors
python tools/vehicle-construction/check-modular-geometry.py
```

The independent Python audit needs NumPy (the bundled Codex Python used for this run provides it). It imports no generator helpers. Full regression command flags and results are listed in `validation.json`; raw task logs remain under `build/construction-connector-revision`.

Native routes, one at a time with exclusive application control:

```powershell
$candidate='E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe'
# Each route isolates its craft library in its result directory.
foreach($name in 'native-connectors','application-short','application-long') {
    New-Item -ItemType Directory -Force "E:\NovaCore\build\construction-connector-revision\$name-reproduce" | Out-Null
}
& $candidate --qualify-editor E:\NovaCore\build\construction-connector-revision\native-connectors-reproduce\result.json --qualification-tank connector-graph
& $candidate --qualify-editor E:\NovaCore\build\construction-connector-revision\application-short-reproduce\result.json --qualification-tank nc.tank.short-2
& $candidate --qualify-editor E:\NovaCore\build\construction-connector-revision\application-long-reproduce\result.json --qualification-tank nc.tank.long-2
```

These opt-in engineering drivers use the production form, authored candidates, hover, real native viewport messages, preview, visible controls and transactions. They cannot supply Player PASS.

Manual retest: start a fresh craft, place Command Core, three Short Tanks, then attitude blocks independently on all three hosts at 1/2/4/8 symmetry, followed by adapter/support and main engine. Focus/orbit as needed. Verify ghost and click placement, group undo/redo, intermediate deletion and restored endpoints, and exact save/reload. For flight use the existing short/long twelve-part qualification articles; do not infer 96-jet flight admission from thirty-part editor acceptance.

Original failure: use the preserved `canonical-release-before` package and original catalog, or the compact permanent own-host ray witness. The original model probe and `probe-output.txt` retain all three old RCS refusals. No authenticated live-history reproduction is claimed; the user accepted installed KSA evidence for this correction.
