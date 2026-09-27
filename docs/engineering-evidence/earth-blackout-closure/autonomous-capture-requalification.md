# Autonomous capture requalification — unbanked

**Superseded current boundary:** [topology-lifetime-capture.md](topology-lifetime-capture.md) preserves the subsequent topology-lifecycle transport qualification, ground V3 PASS and held-horizon V3 automatic stop on warm mutable-capture cost. The V1/V2 results below remain historical evidence.

Project Control authorizes ground-visible, horizon, then controlled movement/orbit after each clean boundary. The exact native-resolution/borderless incident route remains excluded until a final Project Control review. Manual Player acceptance is ON HOLD. The September 22 dual-monitor blackout and historical 411,877-triangle discrepancy remain UNRESOLVED.

## Rejected first optimized ground attempt

`build/earth-blackout-closure/autonomous-live-qualification/live-01-ground` completed 16,188 submitted/completed frames and 90 validated captures. All 235 tracked allocations were released; allocation high-water was 875,844,984 bytes. The observer reached its existing 90-second limit and the application exited normally. A later attempted manual close did not end the run.

Progression was rejected. The last 13 retained completion scopes measured median 2.0008 ms and maximum/P95/P99 7.9354 ms, against roughly 0.02 ms maximum in accepted distant/closer evidence. This scope includes query retrieval and the new mapped compacted-prefix read; it does not isolate memcpy timing. GPU transfer/barrier median was 1.54572 ms. Frozen membership remained consistent, but the transport failed its diagnostic-overhead objective.

The automatic alarm covered repeated fence cost, not completion CPU cost. Supervisor metadata retains the earlier observed values and a later repeated-cost raw window; the earliest raw alarming frames aged out. This gap is explicitly preserved, not retrospectively called an immediate stop. `ground-boundary.json` rejects progression. The candidate, settings proof, logs, summary, snapshots, supervisor witness and prior evidence are hashed before correction.

## Bounded transport correction

Transport version 2 returns compacted-capacity copying to the existing GPU transfer and retains per-slot immutable index reuse. It removes the new live device-local CPU read. This is a bounded correction to a diagnostic path, not a terrain, quality or synchronization redesign.

For prepared vertex count V and triangle count T:

| Path | GPU bytes | GPU copies | CPU live-terrain bytes |
| --- | ---: | ---: | ---: |
| Original | 64V + 28T + 188 | 6 | 0 |
| Rejected version 1 cold | 64V + 16T + 188 | 5 | 12 × visible |
| Rejected version 1 warm | 64V + 4T + 188 | 4 | 12 × visible |
| Version 2 cold | 64V + 28T + 188 | 6 | 0 |
| Version 2 warm | 64V + 16T + 188 | 5 | 0 |

Ground V=597,594 and T=1,195,184 imply 71,711,356 cold and 57,369,148 warm bytes (20% below original warm traffic). Copy sizes follow prepared population, not visible population, changed bytes, or allocation capacity. The compacted source copy contains a capacity tail; only the GPU indirect draw prefix is hashed and retained by the worker. Snapshot files remain independently reconstructable and contain their own index bytes.

Source buffers, destination offsets, counts and allocation births/deaths are retained. The two 88 MiB readback slots, cadence, submission/fence, resource states and worker ownership remain unchanged. The original two barriers are retained, including TRANSFER_WRITE → HOST_READ after copying. No new submission, wait, shader, renderer allocation, lock or disk I/O is introduced. Index reuse invalidates on generation, resource handle, topology hash/family, vertex/triangle count, section offset/size or missing prior GPU-read provenance. V0/V1 remain readable.

## First-alarm correction

The observer adds a separate qualification gate: completion CPU cost >1 ms in three of four captures. The existing >6 ms fence recurrence and all startup, operation, progress and shutdown deadlines stay unchanged. The new gate does not alter runtime synchronization.

Pending capture identity survives the fence boundary. Schedule, complete frozen authority, successful submission, actual copy telemetry, completion and cost must join exactly. Payload frame identity is used even when the record envelope already refers to the next frame. Contradictory resource/slot/capture/generation/pupil/submission facts and non-finite/negative costs fail closed. V0 legitimately lacks copy telemetry. A retained replay reports/excludes its missing leading authority rather than inventing it.

The existing signal-before-storage first-alarm mechanism seals the primary causal window, pins available/pending frozen files, and writes a bounded shutdown tail. Repeated alarms cannot replace the trigger. The measurement reader now includes the shutdown tail. This preserves observer-qualified alarms, subject to operating-system/storage availability; it does not promise a persisted record after an instantaneous whole-system failure.

## Hardware results and genuine stop boundary

Ground-visible V2 requalification passed: 16,032 submitted/completed frames, 12,249 waypoint frames, 89 validated capture publications, two exact retained snapshots, allocation peak 875,844,984 bytes, all 235 allocations released. Retained captures 88/89 join submissions 15,886/16,066, generation 15, pupils 108/110 and exact GPU membership (13,978/13,881 triangles). Each warmed transfer copied 57,369,148 bytes. No memory-budget pressure, recurrence alarm or GPU/device error occurred.

The last 13 capture timings are provisional:

| Scope | Median ms | P95/P99/max ms |
| --- | ---: | ---: |
| CPU capture recording | 0.2261 | 0.2717 |
| CPU completion inspection | 0.0174 | 0.0223 |
| GPU transfer/barrier | 2.05904 | 2.06260 |
| Full GPU capture frame | 4.65104 | 4.85244 |
| Capture-submission fence | 5.4823 | 6.0518 |
| Ordinary fence (2,231 samples) | 3.3248 | P95 4.0018 / P99 4.1544 / max 5.1629 |

An approach wait of 6.0564 ms is retained in supervisor metadata. Two separated held-waypoint waits of 6.0518 and 6.0146 ms remain in raw evidence. They did not satisfy the unchanged three-of-four recurrence gate. These isolated samples do not establish long-tail stability.

The next authorized horizon attempt **failed during approach**, before any held horizon waypoint frames. All 4,303 submitted frames completed, but the existing recurrence gate stopped at frame 4,299 / submission 4,362 / capture 24. The three qualifying captures were cold reads from different newly prepared topology owners:

| Capture / generation | GPU bytes | GPU transfer ms | CPU completion ms | Fence ms |
| --- | ---: | ---: | ---: | ---: |
| 22 / 15 | 71,711,356 | 2.61536 | 0.0241 | 6.1392 |
| 23 / 16 | 77,640,316 | 2.80204 | 0.0147 | 6.2150 |
| 24 / 17 | 83,569,276 | 3.04604 | 0.0195 | 6.1372 |

This identifies increasing cold capture transfer cost, not a warmed horizon recurrence, GPU stall, or recurrence of the mapped CPU-read defect. Nearby ordinary waits remained approximately 3.31–3.45 ms median. Allocation peak was 954,878,264 bytes and all 252 tracked allocations were released. Orbit and the original incident route were not launched.

The exact alarm record (serial 247,889) matches the sealed bytes and hash. Stop signalling followed it by 12.617 ms; cleanup completed 251.4573 ms later. Sealed primary plus shutdown tail is contiguous from serial 116,818 through 248,443, with zero lost records or tail overflow. Unlike the earlier attempts, the first qualified raw alarm did not age out.

## Publication conflict found and corrected offline

Capture 24 completed on the GPU and reached the writer. A storage/publication failure left its valid committed `snapshot-0.tmp`; normal durable identity remained 23. The normal pin audit correctly failed because capture 24 was not published. A separate recovery-only copy validates its SHA256, all 11 prepared-authority fragments, submission/completion, resource incarnations, and exact visibility/compaction membership: 11,083 triangles, generation 17, pupil 27. The original evidence and failure judgment are unchanged.

Forced-overlap CPU experiments reproduce the production conflict: `MoveFileExW` replacement fails with error 5 while an old file is held using the observer's exact share flags. The unpinned control succeeds. The previous small-file regression did not force the reader handle to remain open across replacement.

The worker now uses `ReplaceFileW` for an existing snapshot. Existing pins continue reading the old immutable bytes; new readers obtain the new file. First publication retains `MoveFileExW` with write-through and no overwrite flag. Unexpected targets or other errors fail closed. Payload/header flushing and checked close precede publication, and durable identity advances only on success. The replacement API's write-through flag is unsupported and is not used; whole-system power-loss durability of the directory replacement is not claimed. [Microsoft API contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew).

Writer failures now retain operation, native error, capture/frame identity and error text after `Stop()`, including failures first discovered during shutdown. The historical live error code was not retained, so offline overlap proof does not manufacture that missing code. It does prove and correct an unsafe publication/pinning combination present in the live path.

## Remaining transport decision

**REVISE remains: the horizon cold-transfer cost gate failed.** The publication correction does not reduce those bytes. Repeating the same GPU approach has no new qualification basis. Thresholds, cadence, terrain quality and submission ownership remain unchanged.

The next defensible offline design is a topology-owned immutable **GPU-read** index witness, captured at the topology lifecycle boundary and reused by frame captures. Triggering index buffers existed 169, 131 and 92 frames before their captures, respectively; their 14,342,208 / 15,528,000 / 16,713,792 bytes offer a concrete scheduling opportunity. This is evidence of an opportunity, not proof that earlier copying meets cost or readiness requirements.

That design needs explicit source-handle/incarnation/hash/count identity, first copy/submission/completion authority, bounded cache residence, capture readiness, writer ownership and retirement rules. Sharing between slots alone cannot remove a first read for each genuinely new topology. Moving it earlier shifts work and needs qualification against ordinary-frame gates. No capture may be skipped while waiting. CPU source indices/hashes must not silently replace the accepted GPU-read provenance. This new diagnostic transport/lifetime decision is retained for Project Control review; no implementation or extra GPU exposure followed the real stop.

## Qualification and final status

Exact compiled production extraction preserves the renderer namespace and explicit header-slot correction. Offline probes cover six cold/five warm copies, exact byte accounting, all index-witness invalidations, unusable live compacted mapping, query-not-ready rejection, ownership refusal and malformed draw bounds. Zero-allocation and all 50 outer/23 scalar classifications remain required in Debug and Release.

The V2 evidence root is `build/earth-blackout-closure/gpu-compacted-capture`; hardware attempts are under `autonomous-live-qualification-v2`; the publication fix and recovered unpublished capture are under `publication-owner-correction`. Debug/Release qualification includes 11,929 recording checks, 810,992 ownership checks, 50 outer/23 scalar classifications, 53 first-alarm checks and 48 exact publication-overlap checks per configuration, plus 46 recovery commands and two observer-loss cases. Source reviews are independent; root is the only production writer.

Machine-readable preservation and stage boundaries are authoritative for executed results. No commit, tag, push, banking or milestone change occurred. Player defaults are restored. No NovaCore process remains. The original blackout and historical discrepancy remain UNRESOLVED; manual acceptance stays ON HOLD.
