# Reproduction and preservation

This completed ticket consumed its **one native launch**. Do not run the player
again without new Project Control authorization. No automatic retry or benchmark
loop is permitted. Canonical Surface Recontact physics remains frozen.

From `E:\NovaCore`, offline verification commands (use fresh output directories):

```powershell
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release
dotnet build tests/NovaCore.MinimumRecorder.Tests/NovaCore.MinimumRecorder.Tests.csproj -c Release
dotnet build tests/NovaCore.Retention.Tests/NovaCore.Retention.Tests.csproj -c Release
dotnet build tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj -c Release
python tools/verify-player-package.py --output build/surface-recontact/recheck-package.json
$worker='E:\NovaCore\tools\NovaCore.Recorder\bin\Release\net10.0-windows\NovaCore.Recorder.exe'
$test='tests/NovaCore.MinimumRecorder.Tests/bin/Release/net10.0-windows/NovaCore.MinimumRecorder.Tests.exe'
& $test build/surface-recontact/recheck-recorder $worker
& $test batch-cuts build/surface-recontact/recheck-cuts
& $test native-load build/surface-recontact/recheck-burst 12.4429 3600 180 clean bursts
& tests/NovaCore.Retention.Tests/bin/Release/net10.0-windows/NovaCore.Retention.Tests.exe build/surface-recontact/recheck-retention $worker
& $test inspect-journal C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\b0a8e1482b15419a9b6ca5734428b404 b0a8e148-2b15-419a-9b6c-a5734428b404
```

Repeat matching Debug commands for Debug qualification. The independent native
transport stub is `build/native-ninja-release/NovaCoreMinimumRecorderMock.exe`;
it accepts fresh output directory and recorder executable, and loads no Vulkan.
The original [recorder reproduction](../../minimum-recorder-native-load/reproduce.md)
retains the overload and rotation/cut procedures.

The CPU route test is `NovaCore.Graphics.Tests.exe --surface-retry-fixture`.
It constructs an ordinary saved flight from the existing short-craft recipe,
daylight epoch18,000,000,000µs, x80m, physical clearance0.01m and velocity
(0.1,−0.1,0)m/s; advances only the frozen64Hz engine and production controller;
asserts source-immutable read observations and the entire contact/actuation/handoff
chain. It writes `build/surface-recontact/retry/native.json.input.ncflight.json`.
**Preserve the original input before rerunning this test**: a fresh logical
session identity changes serialized bytes. The qualified native input hash is
`8502d85afb64021f5666770525da605e1920848d3af54f6e629a84dd40ee7414`.

Retain without alteration:

- Canonical Release package and `build/surface-recontact/retry/package.json`.
- `build/surface-recontact/retry/source-freeze.json`, entry/final preservation
  manifests, native input, `native.json`, process/OS records and player log.
- Original failed forensic session
  `C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\5612981fe98946d6b9d51655112a9697`.
- New explicitly pinned native session
  `C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\b0a8e1482b15419a9b6ca5734428b404`;
  `recorder-manifest.json` contains hashes of every retained file after pinning.
- `seam-route-unsuitable.log`, preserving the CPU-only rejected input path.

Use `inspect-journal` for read-only recovery. Do not use a recovery command that
rewrites the original session report. Expected new result: COMPLETE=true,
P=D15,283, zero fault/drop/open/pending. Expected original result remains
COMPLETE=false,P=D1,230,976,faults65,drops3383,open273,pending38.

Source-freeze digest is SHA256 over UTF-8 canonical JSON (`sort_keys=True`,
separators `(',',':')`) of the path→SHA256 manifest. Package digest uses the
existing package verifier. Native session metadata captures PID plus process
start ticks and executable/library hashes. User branches/tags, HEAD and index
must remain unchanged; Codex turn-diff bookkeeping refs are reported separately.

The ordinary-player interface is unchanged. The opt-in qualification mode used
`--qualify-editor <report-path> --qualification-tank surface-retry`; it loads only
the adjacent `<report-path>.input.ncflight.json` and closes automatically after
verified FreeFlight, fault, or90s wall deadline. This documents the completed run,
**not authorization to launch it again**.
