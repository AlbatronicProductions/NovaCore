# Native-free reproduction

Run from `E:\NovaCore`. Never launch `NovaCore.exe` for this qualification.
Keep all test outputs beneath `build/recorder-overflow`; never point test output
or cleanup at the retained runtime session. Repeated runs should use fresh output
directories. Production source correction is two recorder files only.

Build isolated outputs, preserving the frozen canonical player package:

```powershell
foreach ($cfg in @('Debug','Release')) {
  dotnet build tools/NovaCore.Recorder/NovaCore.Recorder.csproj -c $cfg --artifacts-path build/recorder-overflow/artifacts
  dotnet build tests/NovaCore.MinimumRecorder.Tests/NovaCore.MinimumRecorder.Tests.csproj -c $cfg --artifacts-path build/recorder-overflow/artifacts
  dotnet build tests/NovaCore.Retention.Tests/NovaCore.Retention.Tests.csproj -c $cfg --artifacts-path build/recorder-overflow/artifacts
}
```

Use the matching `debug` or `release` directory beneath `artifacts/bin`:

```powershell
$test='build/recorder-overflow/artifacts/bin/NovaCore.MinimumRecorder.Tests/release/NovaCore.MinimumRecorder.Tests.exe'
$worker='E:\NovaCore\build\recorder-overflow\artifacts\bin\NovaCore.Recorder\release\NovaCore.Recorder.exe'
& $test build/recorder-overflow/recheck-full $worker
& $test batch-cuts build/recorder-overflow/recheck-cuts
& $test native-load build/recorder-overflow/recheck-burst 12.4429 3600 180 clean bursts
& $test native-load build/recorder-overflow/recheck-legacy 5.07 5400 180 loss legacy-pages
& $test native-load build/recorder-overflow/recheck-overload 100 4000 100000 loss
& build/recorder-overflow/artifacts/bin/NovaCore.Retention.Tests/release/NovaCore.Retention.Tests.exe build/recorder-overflow/recheck-retention $worker
```

`legacy-pages` is test-only: it retains the former 15-record observer transaction
schedule against the unchanged page/flush protocol. The original before/after
measurements also used the captured pre-correction Diagnostics binary. An initial
128-frame warmup emits 2,432 events before measurement; occupancy reports include
that warmup, so 2,433 is not the measured-phase-only high-water mark.

The native stub target is `NovaCoreMinimumRecorderMock` in the existing Debug and
Release CMake trees. Build only that target with the configured MSVC environment;
run it with a fresh output directory and the matching `$worker`. It uses generated
stub Vulkan functions, never the Vulkan loader/GPU. Do not invoke the app target.

Forensic analysis (read-only input; report elsewhere):

```powershell
python tools/analyze-minimum-recorder-load.py C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\5612981fe98946d6b9d51655112a9697 build/recorder-overflow/recheck-forensics.json
& $test inspect-journal C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\5612981fe98946d6b9d51655112a9697 5612981f-e989-46d6-b9d5-1655112a9697
```

`inspect-journal` does not rewrite the recovery report. Expected original result:
Complete=false, Corrupt=false, faults65, drops3383, open273, pending38,
Produced/Durable1,230,976. The session must remain pinned and bytewise preserved.

`build/recorder-overflow/entry-files.json` holds before hashes. `preservation.json`
checks protected entry/source/package/forensic identities; `qualification.json`
in this evidence directory retains concise outcomes and final candidate identities.
Keep the corrected isolated helper as the recorder qualification artifact. Do not
silently replace the frozen canonical player package or infer native acceptance.
