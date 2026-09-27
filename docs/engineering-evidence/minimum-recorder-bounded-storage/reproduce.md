# Reproduction (CPU only)

From `E:\NovaCore` with the intended unbanked source set:

```powershell
pwsh -NoProfile -File tools/physics/qualify-recorder-bounded-storage.ps1
& tests/NovaCore.Retention.Tests/bin/Release/net10.0-windows/NovaCore.Retention.Tests.exe --rolling-soak 1024
python tools/verify-player-package.py --output build/recorder-bounded-storage/package.json
```

The driver builds managed Debug and Release, runs recorder durability/fault suites,
retention/storage adversaries and the CPU-only `--qualify-storage-ui` entry. That
entry returns before recorder/GPU startup. The soak exercises the same admission,
complete-session retirement and launch-policy functions using real bounded journal
fixtures; its game/native-configure delegates are counters, not GPU calls.

Adversarial fixtures use GUID-only paths beneath
`%TEMP%\NovaCore.Retention.Qualification`, outside the capped real runtime root.
No production cleanup API accepts arbitrary roots or external evidence paths.
Bulk output is ignored under `build/recorder-bounded-storage`; it is not selected
for publication.

The authorized real maintenance operation was:

```powershell
& tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.exe retain-runtime
```

That command changes the fixed runtime store according to current policy; it is
not a read-only reproduction command. Retained capsule recovery is read-only:

```powershell
& tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.exe recover-runtime-capsule <session-guid-N>
# If a routine index exists:
& tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.exe inspect-runtime-index
```

With the preserved local campaign-entry manifest, this read-only verifier checks
retained originals and every retired file's lossless capsule identity independently
in Python, invokes the production capsule parser, and seals source/package/Git:

```powershell
python tools/physics/seal-recorder-bounded-storage.py --output build/recorder-bounded-storage/final-seal.json
```

It writes only its requested receipt. It deliberately checks the current entry
snapshot, not historical raw-path equality after an authorized representation
change. The selected compact results retain exact before/after totals and hashes.
No instruction here authorizes a GPU launch or public player release.
