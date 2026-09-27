# Fresh frozen GPU evidence contract — offline readiness

**2026-09-22. Diagnostic/recovery qualification passed offline. STOP for Project Control review and authorization before any resumed GPU stage. Blackout cause UNRESOLVED; Manual Player acceptance ON HOLD; UNBANKED.**

This revision adds diagnostics and their qualification only. It launches no NovaCore renderer, creates no Vulkan device in its tests, changes no rendering quality, and makes no driver, system, KSA, commit, tag, push or milestone change. The accepted tessellation-domain and moving-pupil dependency/preparation corrections and their permanent regressions are preserved.

## Historical discrepancy remains unresolved

The retained historical frame 16,200 reports 810,442 visible triangles; the later CPU reconstruction reports 398,565. The difference remains **411,877 triangles**. The historical journal did not retain the exact prepared pupil basis, camera/projection inputs, prepared physical vertices or visibility/compaction membership. The old prepared-generation number and aggregate counters cannot supply those missing authorities. No exact historical replay is synthesized or claimed.

The [previous reconciliation](visibility-reconciliation.md) and its witness remain unchanged. A fresh capture can reconcile its own frame; it cannot retroactively identify the missing historical population or prove the blackout cause.

## Production ownership and capture boundary

The new native path records actual mapped inputs and copies actual GPU buffers. It does not recompute a convenient substitute pupil or physical surface on the CPU.

1. At the end of the recorded terrain frame, before command-buffer completion, record a 4,096-byte input/ownership header. It contains the current prepared generation, topology hash/family, current and incoming counts, actual mapped pupils and camera/projection inputs. The same header is fragmented into the existing bounded causal journal for every frame that submits the terrain statistics query.
2. At approximately one-second intervals, reserve one of two preallocated readback slots without waiting. Record six buffer copies after the frame's terrain work, with shader/host-write to transfer-read and transfer-write to host-read barriers. Sources are the prepared physical vertices, source topology indices, visibility flags, compacted indices, cull counters and indirect draw command used by that frame.
3. Bind the slot to the actual successful queue submission sequence and frame fence; retain its present result. No new fence wait is introduced.
4. After the existing fence signals, **before regional preparation/publication or topology replacement**, verify the completed frame and prepared generation still match the slot. Read the already completed pipeline query and release the independent slot to the writer. Swapchain recreation and cleanup use the same completion hook before resource destruction, after their existing successful idle waits.
5. A background writer hashes and publishes the immutable slot. It never reads live renderer buffers. The offline reader joins its header to the recorded frame, queue submission and GPU-completion event, then checks exact triangle membership.

Ownership mismatch, query unavailability after the completed fence, occupied capture slots, capacity overflow, storage failure or missing durable publication stops qualification. None reduces terrain density or silently drops a required capture.

## Retained authority

| Required fact | Retention and reconstruction |
|---|---|
| Prepared pupil and generation | Raw previous/current/incoming pupil structures, published regional pupil, generation, incoming generation, prepared/cull/raster identities, topology hash and family. The reader requires prepared = cull = raster and checks the raw current pupil identity. |
| Prepared geometry | Exact 64-byte physical-vertex records and source index buffer, from the submitted generation. These bytes are retained because rebuilding them from a later CPU approximation would repeat the historical evidence gap. |
| Cull, compaction and draw population | Exact visibility flags, used compacted index prefix, 42 counters and the indexed indirect command. The reader reconstructs the selected source triangles and compares the complete triangle multiset with the actual draw prefix. Atomic compaction order can vary; duplicates and membership cannot. |
| Tessellation/work | TCS/TES, clipping and fragment query results, maximum outer-factor bits, draw/dispatch/group counts, topology counts and existing phase/timestamp diagnostics. Reader checks cull accounting, overflow, invalid triangles, indirect bounds and TCS population agreement. |
| Camera and projection | Actual mapped camera, planetary GPU constants and presentation structure, including their original floating-point bits; viewport, body/surface mode, swapchain and physical/terrain generations. Existing breadcrumbs retain interpreted position, altitude and direction. |
| Preparation/publication state | Raw preparation control and demand input structures; current/incoming dependency-job phase, generation, topology and cursor; physical preparation cursors/readiness and published pupil. |
| Residency and resources | Catalog count and resident bitmap, scratch/payload/working/topology bytes and peaks, capacities, spare/incoming handles and allocation/reuse counts. Existing memory-budget records distinguish support from unavailable results. A bounded lifetime ledger preserves allocation/buffer/image creation, destruction and handle reuse even after their birth records leave the rolling journal. |
| Frame and GPU progress | Frame ID, record/completion QPC, actual submission sequence, submitted/completed frame, command/fence identity, present result and swapchain generation. All sampled source-buffer handles must resolve to one lifetime covering that frame. |
| Reconstruction inputs | SHA-256 seals for the actual runtime binaries and compiled shaders, diagnostic/native source, immutable production/regional terrain packages and elevation oracle. The native path checks the three actual terrain input paths against the sealed roles; the observer verifies hashes before launch and after exit. |
| Termination | Durable observer progress/stop decision, native failure/phase or producer stop reason, completed cleanup, exact child exit code and whether termination was required. Abrupt exit preserves earlier complete captures without qualifying an unfinished frame. |

Immutable terrain packages are identified by hash, not copied into every capture. Exact prepared output and visibility membership are retained because hashes alone cannot reconstruct them. Per-section hashes allow later comparisons without duplicating whole files.

The physical seals are production terrain `38ec671f...bf47ae`, regional terrain `c45c6d94...5d3afe`, and elevation oracle `4600bc01...76317`. Full paths and full hashes are in the readiness manifest. No asset contents were changed.

## Bounded storage and cost

- Two 88 MiB mapped readback slots: **176 MiB of additional diagnostic buffer storage**, allocated at diagnostic setup. Capacity includes the current maximum topology of 712,106 vertices and 1,424,208 triangles. An excess ends qualification; it does not clamp production geometry.
- One 4 KiB header, emitted as 11 fixed-size journal records per terrain frame; no per-frame disk I/O, geometry hashing or bulk CPU geometry copy on the render thread.
- The existing 8 MiB shared ring and 64 MiB rolling journal remain bounded by record count. Added events shorten their time window; retained serials/QPCs establish the actual duration. Capture does not promise a fixed number of seconds.
- At most two published snapshot files plus one interrupted temporary file; each is bounded by 88 MiB plus its 4 KiB header. The worker publishes through flush and replacement and stops after storage failure. It does not accumulate snapshots indefinitely. The compacted buffer's unwritten tail is not persisted or hashed.
- Resource lifetime history is limited to 65,536 entries. Capacity/duplicate-birth/unmatched-destruction faults stop qualification. The observer persists this ledger outside the render process.
- No additional CPU fence wait is added. **GPU copies are additional work in the existing frame command stream.** They are explicitly labelled and included in the total GPU-frame timestamp. Their real driver, memory-bandwidth and frame-time cost remains unmeasured. “Off the critical render path” here means background hashing/writing and no new CPU blocking readback; it does not mean free GPU transfers.

The reader reconstructs **two sampled completed frames**, approximately one second apart. Intervening terrain frames retain exact small inputs and progress, not full prepared geometry or visibility arrays. A new pupil immediately before a machine-wide failure may therefore have input/progress evidence without a complete geometry checkpoint. The final in-flight GPU state and any data not flushed before a system hang cannot be promised. These limits must remain visible in incident analysis.

The observer requires two complete joined captures for a live stage and stops when durable publication is absent for three seconds after the first terrain authority record. The existing first-abnormality deadlines remain active. No unattended retry, stress or reboot loop was added.

## Format and offline inspection

`FrozenCapture.h` is the native schema authority; `FrozenSnapshot.cs` is the validating reader. Version 1 uses little-endian native ABI bytes in a 4,096-byte header: 64 64-bit words, sixteen 32-byte section descriptors, and 3,072 bytes of input storage. Each descriptor gives type, absolute file offset, byte length and stride. Header words 56–59 carry SHA-256; word 63 commits the snapshot identity. Hash coverage is the header with its digest zeroed, then used bulk sections in descriptor order.

Bulk sections 1–6 are physical vertices, source indices, visibility, compacted indices, counters and indirect command. Input sections 100–108 are camera (96 bytes), GPU constants (96), presentation (192), pupils (480), preparation (512), published pupil (160), residency (240), catalog residency (128), and demand inputs (576). Exact types and offsets are sealed with the source and native ABI. Vendor/device/driver IDs occupy header words 60–62.

Frame-authority hashing ignores only fields that become known after recording: submission/completion/present/query results, digest and commit; it restores the compacted descriptor to its recorded capacity. It preserves all original input bytes. Mutating camera or pupil bytes and recomputing a valid file hash still fails the journal join.

Inspection performs no renderer launch:

```powershell
& 'E:\NovaCore\build\earth-blackout-closure\observer\bin\NovaCore.Causal.Observer\release\NovaCore.Causal.Observer.exe' --inspect-frozen '<retained-session-directory>'
& 'E:\NovaCore\build\earth-blackout-closure\observer\bin\NovaCore.Causal.Observer\release\NovaCore.Causal.Observer.exe' --verify-authorities 'E:\NovaCore\build\earth-blackout-closure\frozen-capture\readiness-manifest.json'
```

Synthetic CPU fixtures are explicitly marked in both the file header and observer identity. Relabelling them as real GPU evidence fails validation. Mock launch mode is restricted to the two diagnostic mock executable names. No test result below is hardware rendering evidence.

## Final qualification

Evidence root: `E:\NovaCore\build\earth-blackout-closure\frozen-capture`.

| Check | Result and evidence |
|---|---|
| Native Debug/Release; application and Graphics-test Debug/Release; observer Release | Build PASS. All managed builds report zero warnings/errors. `native-*-final.log`, `app-*-build.log`, `managed-*-build.log`, `observer-build-final.log`. |
| Current-size capture | CPU-only Debug and Release PASS: 712,106 vertices, 1,424,208 triangles, 712,104 synthetic visible triangles. Two final files of 85,457,428 bytes each; exact source/visibility/compacted membership, frame inputs and submission/completion join pass. `qualified-large`, `qualified-large-release`. |
| Normal small capture | PASS; five publications replace bounded slots, two latest remain. `qualified-normal`. |
| Stale completion, unavailable storage, occupied slots | All fail closed as expected, no overwrite of an owned slot or false qualification. `qualified-stale`, `qualified-storage`, `qualified-backpressure`. |
| Abrupt child exit during a later capture | Expected failure, exit 42. Earlier complete capture remains readable; later submitted frame is incomplete. `qualified-abrupt`. This is CPU process recovery, not an OS-blackout durability proof. |
| Corruption and reconstruction regression | 13 checks PASS: physical hash damage, uncommitted header, wrong completion/pupil, forged bounds, visibility/compaction mismatch, valid-hash camera/pupil mismatch, oversized draw, corrupt journal, synthetic-as-live rejection, resource-handle reuse. `qualified-format-regressions/regression-results.json`. |
| Observer recovery | Normal traffic, high traffic and bounded cleanup linger PASS. Simulated fatal result, unreturned fence and ring flood fail closed as expected. `qualified-recovery-*`. |
| Accepted pupil handoff | Debug/Release shared production-header schedule regressions PASS, including moving pupils, delayed dependencies, all-resident and generation/topology replacement. No Vulkan entry point. `NovaCoreRegionalPupilLifetimeTests-*.log`. |
| Accepted tessellation arithmetic | Debug/Release 100,000 ordinary finite cases unchanged and 2,400 singular-neighborhood cases PASS. `NovaCoreTerrainTessellationTests-*.log`. |
| Accepted horizon submission boundary | Debug/Release focused 24 states, 22,460,448 triangle-state evaluations each: 42 legacy invalid draw cases, zero corrected invalid draw cases. Original 477-case witness retains 435 production-transformed finite cases and 42 GPU-cull rejects. CPU/source-model qualification only; `horizon-debug`, `horizon-release`. The earlier full 876-state evidence remains unchanged. |
| Presentation outcomes | 18 CPU checks PASS in each native configuration. |
| Seals and preservation | `readiness-manifest.json`, `authority-tests.json`, `preservation.json` and refreshed parent `current-task-manifest.json`. HEAD/index/refs, ordinary executable, unrelated work and historical live evidence preserved; scoped diff check clean. |

## Readiness decision and required stop

**Ready for Project Control review of the diagnostic contract and offline qualification. No authorization to expose the GPU is implied.** The readiness manifest deliberately has `liveGatePassed: false`, `projectControlExposureAuthorized: false` and an empty allowed-stage list. Old live gates do not satisfy the new contract. The parent qualification file also continues to prohibit exposure and banking.

If Project Control separately authorizes a fresh staged replay, the initial distant-Earth stage must first qualify this diagnostic transport, validate its two joined captures and measure its added cost. Stop and inspect after each stage; do not skip directly to horizon, orbit or the original pattern. No such stage was launched here.

Remaining unknowns are the historical 411,877-triangle membership difference; the incident's last exact GPU/driver state; hardware parity and cost of the accepted corrections; real GPU readback/barrier behavior and diagnostic overhead; and device-fault retrieval and persistence during a real machine-wide blackout. None identifies NovaCore, a driver, GPU, PSU or Windows as the cause. Manual acceptance and every campaign/banking boundary remain on hold.
