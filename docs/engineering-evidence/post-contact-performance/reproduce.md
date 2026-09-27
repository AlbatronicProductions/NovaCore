# Reproduction and preservation

Canonical tree: `E:\NovaCore`. This ticket permits one bounded native witness.
The launch marker `build/post-contact-performance/native-launch.json` consumes
that authorization. Do not remove it or relaunch without Project Control.

Offline, from the repository root:

```powershell
pwsh -NoProfile -File tools/physics/qualify-post-contact.ps1
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll --post-contact build/post-contact-performance/recheck-replay-Release.json
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll --post-contact build/post-contact-performance/equal-duration-replay.json 537
python tools/physics/analyze-post-contact.py --offline-only --output build/post-contact-performance/recheck-offline.json
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug --no-restore
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --no-restore
python tools/verify-player-package.py --output build/post-contact-performance/recheck-package.json
```

The CPU replay reads, without rewriting, the preserved previous native input
`build/surface-recontact/retry/native.json.input.ncflight.json`, SHA256
`8502d85afb64021f5666770525da605e1920848d3af54f6e629a84dd40ee7414`.
The permanent `--swept-clearance` suite independently generates its own fixtures:
it has no historical build-directory input dependency. The qualified short craft
has source digest `79234b824be311679ef21bc7a52497c4d0d238abbb89332089d218e106995c2e`.

The full driver contains no Vulkan launch. It deliberately omits live-fixture
generation because that would overwrite earlier input identities. Native timing
storage is tested with the explicitly CPU-only target:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
cmake --build build/native-ninja-release --target NovaCore.Native NovaCorePostContactTimingTests
& build/native-ninja-release/NovaCorePostContactTimingTests.exe build/post-contact-performance/recheck-timing.csv
```

Recorder durability/retention commands remain those in the preceding
[native retry recipe](../surface-recontact/native-retry/reproduce.md), with fresh
outputs beneath this ticket. Do not invoke a recovery operation that overwrites
the pinned sessions. `inspect-journal` is read-only.

The authorized native route uses the canonical
`tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`, ordinary persisted
display settings, and existing opt-in arguments `--qualify-editor <new-report>`
`--qualification-tank surface-retry`. The adjacent input is an exact copy of the
preserved input. `NOVACORE_POST_CONTACT_TIMINGS` points at `native-timing.csv`.
There is no alternate renderer or simulation path. Qualification now holds the
verified FreeFlight successor for one simulated second before normal close.
The90s wall deadline,25s simulated deadline, mandatory-recorder fault monitor and
physical refusal boundary remain enabled. This describes reproduction; it is
not permission for another exposure.

After an authorized witness:

```powershell
python tools/physics/analyze-post-contact.py --output build/post-contact-performance/analysis.json
python tools/physics/summarize-post-contact-journal.py C:/Users/Tyler/AppData/Local/NovaCore/MinimumRecorder/11b82e566ff44eb2b3379c782fdd89c8 build/post-contact-performance/journal-summary.json --flight-report build/post-contact-performance/native.json
python tools/physics/seal-post-contact.py --output build/post-contact-performance/final-preservation.json --compare build/post-contact-performance/source-freeze.json
```

The analyzer uses nearest-rank percentiles, no discarded outliers, whole-frame
start-to-start cadence, explicit GPU identity/deduplication and debt reconciliation.
CPU/GPU overlap prevents adding their independently measured times. This run has
602 distinct GPU samples for 604 frames; all 261 flight frames have a joined
sample. Stage probe timestamps
remain included in service time; outside probe setup/read costs are separate.

Keep both pre-existing pinned recorder sessions byte-for-byte:
`5612981fe98946d6b9d51655112a9697` (failed, incomplete) and
`b0a8e1482b15419a9b6ca5734428b404` (accepted recorder qualification).
Keep all of `build/surface-recontact/retry` unchanged. The entry and final manifests
verify these, settings, index, branches/tags and KSA identity. Canonical package
changes are explicitly authorized here; source/package are frozen immediately
before the new witness and must remain identical afterward.

Evidence budget: concise permanent reports below250 KiB, reproducible raw timing
and fixture JSON kept only while this unbanked performance decision is active.
No large capture, shader dump, screenshot archive or duplicate player package is
required. Unresolved forensic recorder journals remain protected independently.
