# Post-blackout storage hygiene — 2026-09-25

Scope: the six directories explicitly named by Project Control. Storage hygiene only; no production change, deployment, GPU exposure, Player acceptance, banking, Git-history change or KSA access. All sizes use logical bytes and decimal GB. Reparse points are inventoried without traversing their targets.

## Completed result

**18.026 GB net reclaimed: 26.924 GB → 8.898 GB.** Removed 4,625 reviewed files totaling 18.042 GB; the net figure includes approximately 16.9 MB of new compact witnesses and audit records. No unlisted pre-existing file disappeared. All 8,902 protected files remained byte-identical; HEAD, index, branch/tag/remote refs and worktrees are unchanged. All 723 candidate/physical-authority checks passed, both final native journals passed checksum/continuity inspection, and the bounded offline observer smoke passed 33 checks without GPU execution.

| Directory | Before MB | After MB | Net reclaimed MB |
|---|---:|---:|---:|
| `E:/NovaCore` | 26,882.516 | 8,856.99 | 18,025.53 |
| `E:/NovaCore-Archives` | 35.264 | 35.264 | 0 |
| `E:/NovaCore-Assessments` | 1.164 | 1.164 | 0 |
| `E:/NovaCore-Diagnostics` | 0.368 | 0.368 | 0 |
| `E:/NovaCore-M1420-Correction-audit` | 0.356 | 0.356 | 0 |
| `E:/NovaCore-Research` | 3.895 | 3.895 | 0 |
| **Total** | **26,923.563** | **8,898.03** | **18,025.53** |

Exact timestamped byte totals are in [result.json](result.json); rounded table values include final report bookkeeping. The current task manifest now explicitly retires 32 generated CPU artifacts while preserving 294 retained qualification artifacts and all current source/build identities. The pre-cleanup manifest remains unchanged as historical provenance.

The largest retained diagnostic categories are native evidence/candidate history (approximately 1.335 GB), V3 live evidence (0.732 GB), V2 live evidence (0.686 GB), and original-incident application material. These include unique hardware captures and unresolved historical witnesses; their authority was not guessed away. Git/LFS, terrain and other production assets are retained regardless of size. The current retest candidate is unchanged and available; manual Player acceptance remains ON HOLD. No banking or milestone promotion.

## Before deletion

Read-only inventory: **26,923,562,547 bytes** total; `E:/NovaCore` accounts for 26,882,516,452, including 23,106,880,374 of blackout diagnostics. The five historical side directories total 41,046,095 bytes and are retained intact. Only `E:/NovaCore` is a registered Git worktree. HEAD is `8c189b28ce2a68f97de734d1589acb500c41fd99`; the dirty working tree is existing production truth and is preserved.

The complete pre-cleanup final task manifest was verified (**711 source/build/qualification identities**) and copied unchanged to `build/post-blackout-storage-hygiene-20260925/pre-cleanup-task-manifest.json`. The final native authority gate seals **723 identities**. The protected byte inventory covers **8,902 files**, including all existing non-build source/assets, historical side directories, final candidate/build authority and final live evidence. Git metadata/history and every tracked path are excluded from deletion.

## Classification and reviewed deletion package

| Contents | Classification | Disposition / retention basis |
|---|---|---|
| Source, Git/LFS, assets, terrain/oracle/source packages, permanent tests, engineering reports, hashes and recipes | KEEP | No production/cache/Git cleanup |
| `native-adaptive-evidence/resume-20260925/candidate` | KEEP | Complete current isolated application required for retest; all files protected |
| `native-adaptive-evidence/native-final-20260925/live-native-matching` | KEEP | Entire final native run, raw rolling journal, cost checkpoint, frozen captures and validation; 436,673,107 bytes |
| Current native Debug/Release builds and final observer binaries listed by the current manifest | KEEP | Current qualification and reproduction capability |
| CPU-only mock/recovery recorder bulk | REGENERATABLE / DELETE | Identity explicitly records `gpuExecution:false`; retained run results, permanent mocks, hashes and compact samples; 17,196,696,448 bytes |
| Generated first-alarm regression tails | REGENERATABLE / DELETE | Enclosing `first-alarm-regression.json` passes; permanent test constructs the data; 318,771,968 bytes |
| Superseded compiled artifacts in `gpu-compacted-capture`, `publication-owner-correction`, `topology-lifetime-capture`, `frame-capture-ranges` | REGENERATABLE / DELETE | Selected compiled extensions only, not whole mixed trees; preserve source, build commands/logs, identities and current binaries; 429,756,569 bytes |
| `resume-20260925/terrain-python` temporary environment | REGENERATABLE / DELETE | Existing generator and NumPy/Pillow requirements, source provenance and exact restored package hashes retained; 97,213,700 bytes |
| Unique earlier live-GPU captures, original blackout materials, source-at-entry copies, uncertain outputs | KEEP / REVIEW BEFORE DELETE | Historical blackout causation remains unproven; no speculative deletion or loss of exact hardware witnesses |
| Archives, assessments, diagnostics, M1420 audit and research | KEEP / REVIEW BEFORE DELETE | Small historical packages, source snippets, reports and visualizations; all files byte-sealed |
| Audit `graphics-root/assets` and `graphics-root/build` reparse points | KEEP | Targets not traversed or changed |

The exact allowlist contains **4,625 files / 18,042,438,685 bytes**. No live-GPU raw file is selected. File-level removal avoids recursively deleting mixed evidence directories. The deletion executor verifies absolute containment, every ancestor's reparse status, tracked-file exclusion, size and SHA-256 before removing each exact literal path. Unlisted content is retained. No compression or relocation substitutes for deletion.

## Six retained requirements

1. **Conclusion:** [native closure](../earth-blackout-closure/native-route-closure.md), [ordinary-frame classification](../earth-blackout-closure/native-ordinary-frame-contract.md), [V4 transport](../earth-blackout-closure/frame-capture-ranges.md), [topology lifetime](../earth-blackout-closure/topology-lifetime-capture.md), [publication correction](../earth-blackout-closure/autonomous-capture-requalification.md), plus earlier reports remain unchanged.
2. **Measurements:** per-run `summary.json`, `identity.json`, analysis/validation JSON, first-alarm metadata, logs and each 46-case `recovery-results.json` remain. The final native timing, progress and allocation evidence is untouched.
3. **Identities:** pre-cleanup task manifest, original source/build/candidate seals, and the deletion ledger retain exact path, size and SHA-256 for every removed artifact. No missing raw file is interpreted as a test PASS.
4. **Compact witnesses:** generated journals retain their original header and up to 32 populated physical-slot records in `compact-cpu-witnesses`. These are explicitly partial CPU fixtures, not complete raw journals or historical GPU replay substitutes. Small generated frozen fixtures and actual live-GPU witnesses remain in place.
5. **Permanent regressions:** `native/NovaCore.Native/CausalRecorderMock.cpp`, `FrozenCaptureMock.cpp`, and `tools/NovaCore.Causal.Observer/{FirstAlarmRegression,AdaptiveCaptureRegression,ProductionFrameRegression,FrozenRegression,RecordingCallRegression,StartupRegression,TopologyRegression}.cs`, extracted native probes and production owner source remain.
6. **Reproduction:** retained per-generation `qualify-recovery.ps1`, `qualify.ps1`, `qualify-focused.ps1`, native build commands and `restore-terrain.py` record dependencies and invocation. Follow the repository [diagnostic-output policy](../../diagnostic-output-policy.md): reproduce into a fresh isolated directory, adapt historical paths deliberately, and do not execute archived mutation scripts against current evidence.

The temporary Python environment can be recreated with the retained terrain generator's pinned requirements. Existing accepted terrain packages and original inputs are kept, so manual retest does not depend on recreating it. Current observer/native binaries remain available for offline checks; removed CPU journals regenerate through the retained mocks or `--test-first-alarm` / `--test-adaptive` cases. Do not regenerate bulk merely to prove cleanup.

## Preservation and final audit

Operational evidence: `build/post-blackout-storage-hygiene-20260925/`. `before.json` and `files-before.jsonl` record the read-only inventory; `delete-manifest.json` is the exact reviewed list; `protected-identities.json` is the preservation seal; `deletions.jsonl`, `after.json` and `verification.json` record actual execution and checks. `pre-cleanup-task-manifest.json` preserves the old seal; the current task manifest identifies intentional retirement explicitly rather than presenting missing artifacts as retained.

After deletion, compare all protected file hashes, Git HEAD/index/branch-tag-remote refs/worktrees, the 723 candidate/asset authority identities, final live journals and normal application/shader dependencies. Run a bounded offline observer smoke only; no application/GPU route is required or authorized by cleanup. The historical ordinary deployment path remains absent as already documented; cleanup neither deploys nor reconstructs it. The sealed isolated candidate remains the retest artifact. Manual Player acceptance remains ON HOLD.

The hygiene package has a **25 MB evidence budget**, primarily path/hash ledgers and partial generated-CPU witnesses. It does not copy native captures or production assets. Final actual before/after totals and any preservation exceptions are recorded in the companion `result.json` after execution.
