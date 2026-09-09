# Immutable surface-point query: private image/geometry parity

Result: PASS, 2026-09-09. This is a fresh bounded A/B measurement, not reuse of historical pixels.

## Scope and identities

Baseline sources were exported read-only from Git commit `aaf3e7a032a806e7e547c2830d1f95d97aaf620b`. Baseline Core, Graphics, Interop, Platform, Simulation and EphemerisFormat were built externally in Release. The original deployed libraries were preserved and hashed before lead builds, but are not used as the provenance basis: fresh source-exact external baseline builds avoid depending on unproven deployed managed assembly identity. Their binary hashes differ because external compilation paths differ.

Candidate uses the lead's freshly built Release Core and Graphics assemblies, recorded with current source hashes in `candidate-input.json`. All other assemblies, the private native capture DLL, the private sample driver, and all 49 SPIR-V files are byte-identical across A/B. Native capture DLL SHA-256: `0ce79ade1cccb3c28c89666a8a0cbb5d5a15a5d1cd98fe9d3178767c07b22b04`. Driver SHA-256: `43aa178cd772bb6fd964d5bcba80f986b6d2fb02689bbd9e08bddbe42286e6d4`.

No repository source, deployment, native build directory, shaders, assets or cache policy was modified by this proof. Parent remains the only production writer. All private builds/runs/outputs are under this external evidence directory. Assets are read from the normal repository/cache paths, without copying them.

## Measurement

Each of four processes completed 200 frames at 3440x1440. One full frame was captured at frame 175. Fixed J2000 epoch, paused simulation and zeroed user input keep state identical. Florida is the normal Florida Launch Site route. Inland is the supported land site at 50 m; it is not asserted to be a historical investigation's latitude/longitude.

| Pose | Prepared vertices | Selected triangles | D32 | HDR | Final image | Prepared bytes | Oriented triangle multiset |
|---|---:|---:|---|---|---|---|---|
| Florida | 712,106 | 339,054 | Exact | Exact | Exact | Exact | Exact |
| Inland land/50 m | 712,106 | 4,086 | Exact | Exact | Exact | Exact | Exact |

Exact means equal SHA-256 over complete raw bytes; no tolerance or image similarity test. D32 and HDR were also checked finite. Triangle triples are lexicographically sorted as complete triples, preserving winding and multiplicity; order produced by GPU compaction is not an authority. Prepared records compare as all 64 bytes per vertex.

Both poses match every captured camera, GPU, presentation, lighting, published-pupil and incoming-preparation constant byte. Capture publication identity is generation 1, pupil 1, physical generation 4; immutable topology hash `13611079496444506427`. Incoming generation 0, active preparation 0 and complete dependencies 1 prove the captured current owner was settled. Runtime global/regional/elevation asset fingerprints match. Logs show sole NCSM1 ownership, no anchored/global alternative owner. Complete exact digests, not abbreviated values, are in each `*.digest.json`. `parity-result.json` contains every comparison.

Khronos validation was forced through the existing supported canonical process-local layer discovery boundary. Live process module snapshots prove `VkLayer_khronos_validation.dll` loaded. The native Release startup banner says disabled because it reports compile-time automatic activation, not environment-forced loader activation; it is not used as proof. All four processes returned 0 with zero Vulkan validation errors/device-loss reports. The same non-error `WARNING-Shader-OutputNotConsumed` remains visible in all runs; it was not filtered or reclassified.

## Limits

This isolates effects of candidate Core/Graphics on the unchanged production renderer at two supported fixed poses. It does not exercise a contact solver, launch, moving spacecraft or arbitrary transition traversal. The new query service is additive and is not called by the renderer. Its query correctness and performance are separately owned by the lead's tests. The private hook adds transfer-capable attachments, one readback, host output and strict validation; these timings are not production performance evidence. No manual acceptance claim is made here independently of the ticket's waiver bar.

## Reproduction

Only run under the owning ticket's GPU coordination. Do not invoke historical `capture.py` runner/build functions; they use obsolete branches/paths and can write production files. Retained `prepare.py` imports only the two pure text-transform functions; `private-capture.patch` preserves the exact diagnostic transformation for review.

1. From this external directory run `python -B prepare.py`. It uses `git archive` at the pinned baseline into this directory, builds baseline class libraries, and builds the fixed private driver. It does not checkout/reset a branch or build repository projects in place. Requires .NET 10 SDK.
2. Enter the installed Visual Studio developer PowerShell using `C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1 -Arch amd64 -HostArch amd64 -SkipAutomaticLocation`.
3. Set `VULKAN_SDK=C:/VulkanSDK/1.4.357.0`; run `cmake -S private -B native-build -G Ninja -DCMAKE_BUILD_TYPE=Release`, then `cmake --build native-build --target NovaCore.Native --parallel 4`. All source includes come from the external pinned source export. No shader compilation or repository deployment occurs.
4. Use Python with NumPy, here `C:/Users/Tyler/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Run `-B parity.py setup`. This copies the private native DLL and unchanged production shaders into the external baseline host and creates a private canonical Vulkan manifest.
5. Run `-B parity.py run baseline-florida florida host-baseline`, then `-B parity.py run baseline-inland inland host-baseline`. Each child is limited to 200 frames with a 180-second process timeout, and only three guarded raw filenames can exist.
6. After the lead freezes/rebuilds candidate Release Core and Graphics, verify their source identities, then run `python -B candidate.py`. This creates the second host from the first and changes only those two DLLs. It rejects an already-existing candidate host.
7. Run `-B parity.py run candidate-florida florida host-candidate`, then `-B parity.py run candidate-inland inland host-candidate`.
8. Run `python -B compare.py`. All hashes, input bytes, manifests and non-candidate runtime identities must match. Any difference is FAIL; do not weaken comparisons.
9. Preserve compact logs, digests, host/source identities and these scripts. Retire the guarded raw slot and rebuildable private output after verification. No historical captures or production assets need copying.

Commands internally use repository working directory `E:/NovaCore` only for normal asset resolution. Florida arguments: `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate --solar-epoch=j2000 --benchmark-frames=200 --log=startup,validation,vulkan`. Inland substitutes `--surface-site=land --altitude=50`.

## Evidence lifecycle

A single raw slot peaked at 128,901,032 bytes. Four captures wrote 507,564,896 raw bytes cumulatively, never retained simultaneously; each successor replaced only the three proven raw files after digest retention. Final raw slot held 124,881,416 bytes before cleanup. No raw captures are retained. `storage-after.json` records final cleanup accounting. The attempted guarded recursive cleanup of rebuildable output was rejected before shell execution by automatic tool policy (`blocked by policy`, no further reason supplied). It was not retried. The narrow nonrecursive deletion of only pixels.bin, prepared.bin and selected.bin succeeded. Rebuildable baseline export, hosts, DLLs, native object output and archive remain disposable; `disposable-remaining.json` identifies exact paths, counts and logical bytes. This is not zero-debris closeout. Compact evidence does not contain production assets.
