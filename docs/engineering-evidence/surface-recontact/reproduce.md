# Reproduction and preservation

All commands below are CPU/file checks. **Do not relaunch NovaCore while the
recorder-integrity stop is active.** A new live attempt requires Project Control
disposition. Do not alter/delete the preserved package or pinned journal.

From `E:\NovaCore`, using the repository's .NET SDK and authoritative Earth data:

```powershell
./tools/physics/qualify-surface-recontact.ps1
python tools/verify-player-package.py --output build/surface-recontact/package-recheck.json
python tools/physics/seal-surface-recontact.py --output build/surface-recontact/recheck-seal.json --compare build/surface-recontact/pre-native-seal.json
```

The first command builds managed Debug/Release and runs all listed CPU contact,
reference and preservation suites, then performance and live-input preparation.
It does not invoke the player or change the existing native renderer. A future
source rebuild can change candidate bytes: the final seal must be checked before
claiming identity with this native run. The authoritative physical terrain inputs
remain required; no global-only fallback was substituted.

Permanent tests are in `tests/NovaCore.Graphics.Tests/ModularFloridaTests.Surface*.cs`,
`tests/NovaCore.Simulation.Tests` (ordinary-response/transaction cases), and the
existing modular/reference suites. High-precision numerical fixtures and their
generator remain in the repository; no proprietary KSA material is required.

Normal flight inputs can be regenerated CPU-only:

```powershell
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll --surface-live-fixtures
```

This generates short/long/terrain descending `.ncflight.json` fixtures under
`build/surface-recontact/live-inputs`. Each is advanced to rest and its checkpoint
reloaded offline. Future authorized UI reproduction uses the normal Load flight,
Save flight, construction/return and Quit commands; it does not inject physics.
The actual native saved endpoint is separately preserved as
`build/surface-recontact/native-short-grounded.ncflight.json`.

Recovery of the pinned run (reads journal, writes only its derived recovery report):

```powershell
dotnet tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.dll recover C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\5612981fe98946d6b9d51655112a9697 5612981f-e989-46d6-b9d5-1655112a9697
```

Expected: durable 1,230,976, faults 65, dropped 3,383, Clean/Complete false.
Do not relabel that result as clean because the final Shutdown record exists.

Keep: canonical 134-file package; `entry.json`, pre/post/final seals and per-file
package manifest; latest qualification logs plus the superseded test failure;
native grounded save; native log/recovery/OS evidence and raw pinned banks;
compact causal terrain/manifold witnesses; this report, numerical fixtures,
permanent tests and reproduction tools. No cleanup was performed here.
No generated bulk is staged. Git HEAD, refs and index remain unchanged.
