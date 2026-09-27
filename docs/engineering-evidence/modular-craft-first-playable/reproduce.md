# Reproduction at the Gate 1 stop

Run from `E:\NovaCore` using the repository's .NET 10 SDK. No KSA, browser, native graphics or deployment is needed for these witnesses.

```powershell
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --no-restore
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1-redteam
```

Baseline expects 33 passing checks. Red team deliberately remains a failing permanent regression: it prints the two resource-identity failure booleans and `NullReferenceException`, then exits nonzero. Do not interpret a successful build or baseline as Gate 1 PASS. No test expectation was weakened to hide these defects.

The exact passing regression invocations, measured elapsed times and concise outputs are retained in `gate1-regression.json`. These include the old construction Stages 1–8, SRV integration, canonical controls, pilot demand and allocation. Debug and Release managed Simulation.Tests builds both passed with zero warnings/errors. No whole-solution/native/player build is claimed.

`entry.json` is the pre-mutation baseline. `closure.json` reports changed/added paths, hashes, HEAD/refs/index/worktree preservation and storage. Compare its source hashes before associating a new run with this candidate. It excludes itself to avoid a recursive hash. The byte-verified recovery archive and index copy remain under `build/modular-craft-first-playable/entry`; no cleanup permission is implied.

No build, test, compiler, editor or runtime depends on the planning conversation or recovery ZIP. The new tests are opt-in qualification branches and do not alter existing test expectations.
