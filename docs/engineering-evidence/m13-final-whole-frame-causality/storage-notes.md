# Closeout storage plan — read-only inventory

No deletion, move, on-disk compression, build or GPU run was performed by this worker. The exact file inventory, NTFS identities, hardlink names, Git classifications and in-memory gzip estimates are retained in storage-plan.json. This is a transient inventory while the lead's tests continue; the lead must refresh it and satisfy the stated closeout conditions before cleanup.

## Named cleanup roots

Initial inventory began at 2026-09-08T02:30:05 UTC. All 117 files below are Git ignored and none is Git tracked. No directory/file reparse point was found in these two roots; no sibling live-test GUID was enumerated.

| Exact location | Files | Logical bytes by name | Allocated bytes by name | Exclusive allocated bytes recoverable |
|---|---:|---:|---:|---:|
| E:/NovaCore/build/m13-final-whole-frame-causality | 81 | 199,046,110 | 199,172,760 | 132,063,896 |
| E:/NovaCore/build/regional-live-tests/c035332a73f94fed8db68c247b03628a | 36 | 458,943 | 520,192 | 520,192 |
| Total | 117 | 199,505,053 | 199,692,952 | 132,584,088 |

Exclusive logical data recoverable is 132,396,189 bytes. Allocation is measured using GetFileInformationByHandleEx(FileStandardInfo).AllocationSize, excluding directory/MFT metadata. Git classification uses NUL-delimited byte input/output; PowerShell/Python CRLF translation must not become part of a Git pathname. Logical directory size is not the same as reclaimable disk allocation.

## Protected hardlink

The only multiple-name file has NTFS device 4035975247, file ID 2814749767112824, two links and 67,108,864 bytes. Windows hardlink enumeration returned exactly:

- E:/NovaCore/assets/earth/runtime/earth_elevation_8192x4096.r16 — **KEEP**.
- E:/NovaCore/build/m13-final-whole-frame-causality/host/earth-data/earth_elevation_8192x4096.r16 — remove only this scratch name after closeout.

Removing the second name leaves the production asset intact and recovers none of its 67,108,864 data bytes. Do not follow the identity to the asset name and do not claim 199.5 MB of recovered file data. All other inventoried scratch files have one name. They become removable when the lead completes relevant validation and retains its bounded findings.

## Disposable groups and prerequisites

| Group | Continuing responsibility | Disposition after closeout |
|---|---|---|
| pixels.bin, prepared.bin, selected.bin — 124,943,408 logical bytes | Transient GPU readbacks consumed by retained placement_parity.py and candidate_parity.py | Remove once final exact-byte/hash/numerical summaries are retained. Regenerate using the retained tooling; no permanent raw buffers. |
| Private host deployment, its shaders, scoped layer manifest, two scratch shader-source copies | Reproducible isolated diagnostic runtime and controls | Remove after the private host is no longer running. Keep the normal deployed runtime/shaders and retained reproduction source/patches. |
| Debug-window-failed.log | Failure classification and reproduction evidence | Remove only after the lead retains the exact failure and classification in the final package. This worker does not declare the failure resolved. |
| The 36 frame-*.json files in the exact c035332a73f94fed8db68c247b03628a directory | Generated live-test frame camera/pupil/generation/regional observations | Remove once the failure's bounded report/summary is retained. Keep permanent test source and every other active or unrelated GUID. |

Deletion authority is restricted to the two exact roots. Preserve all production source, the normal deployment, assets, .novacore, .git and all five earlier untracked packages: m13-exit-assessment, m13-regional-preparation-blocker, m13-regional-preparation-convergence, m13-regional-replacement-lifecycle and m13-residual-regional-frames. Nothing outside the named roots becomes removable through this plan.

The lead should verify resolved absolute paths, reparse/link state, inventory identities and that no running process uses the private host before deleting listed scratch names. No automatic deletion command was run or scheduled. A new file created after this snapshot needs its own classification or refreshed inventory.

## Bounded evidence package

The evidence inventory contained 64 untracked, nonignored files and 16,978,071 logical bytes before this plan/notes were added. Its allocation recheck was 17,118,176 bytes, but validate.py, validation.json and whole.py changed during the live recheck. Those differences are recorded explicitly; these numbers are not presented as an atomic final package size.

For the 27 then-present JSON journals larger than 100,000 bytes, in-memory gzip level 9 with mtime 0 saves **14,579,937 bytes**, projecting **2,398,134 logical bytes** for that snapshot before the new plan/notes. Every proposed stream was decompressed and compared to its input. No gzip file was written and no plain journal was removed.

| Largest journal | Plain bytes | Proposed gzip bytes |
|---|---:|---:|
| regional-base.json | 2,598,438 | 149,569 |
| regional-local.json | 2,594,863 | 142,000 |
| regional-candidate.json | 2,591,370 | 141,302 |
| verify-b.json | 2,374,469 | 164,239 |
| work-a.json | 977,261 | 83,725 |
| placement-florida-capture-base.json | 646,098 | 38,828 |

At closeout, refresh new or changed journals, validate JSON, gzip losslessly, and verify the decompressed SHA-256 against the original before removing the plain copy. Update retained readers/report references to read .json.gz. Preserve reports, reproduction source/patches, small manifests and the bounded ISA comparison. Do not compress or mutate the five earlier evidence packages. The projected package has substantial margin below the 12,582,912-byte (12 MiB) cap; no loss of journal columns or additional raw retention is needed.

This snapshot cannot establish cumulative diagnostic output or cumulative write I/O. The private capture tooling repeatedly overwrites the same three raw paths; multiplying the current footprint by run counts would be an unsupported storage/I/O claim. Final closeout should report refreshed retained size, actual removed file allocation and the retained asset hardlink separately.
