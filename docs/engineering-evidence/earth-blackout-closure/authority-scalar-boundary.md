# AuthorityScalars — capture-header defect proven and corrected offline

## Judgment

**PASS — frame-168 AuthorityScalars defect causally closed offline.** The production capture-header writer's out-of-bounds indexing is proven, corrected and offline-qualified in Debug and Release. **STOP for Project Control review and fresh bounded live authorization; no GPU relaunch occurred.** This PASS covers the diagnostic defect only.

The renderer's enclosing namespace defines `Width = 960` and `Height = 540`. Inside `FrozenFrameAuthority`, the unqualified expressions `h[Width]` and `h[Height]` bind to those renderer constants instead of `nc::frozen::Width = 23` and `nc::frozen::Height = 24`. The header's word array has **64 entries**.

This defect is present in the exact Debug DLL used by the marked retry, lies inside the entered-but-not-returned `AuthorityScalars` interval, and deterministically reaches the Debug bounds-report path in a CPU-only witness. It explains a concrete failure mechanism for the diagnostic recording stall. The stopped live thread's exact instruction pointer and CRT dialog state were not captured, so those are not retroactively claimed as observed.

This is a **diagnostic capture defect**, not proof of the September 22 blackout cause. Blackout cause remains **UNRESOLVED**; historical **411,877-triangle discrepancy remains UNRESOLVED**; manual Player acceptance remains **ON HOLD**. No banking or milestone promotion.

## Compiled production proof

Evidence root: `E:\NovaCore\build\earth-blackout-closure\authority-scalars`. The accepted DLL and source-at-entry were preserved before edits. `compiled-proof.json` records hashes and verifies that the disassembled native Debug DLL is byte-identical to the application DLL used in the live attempt.

| Compiled fact | Evidence |
|---|---|
| Width header argument before correction | Debug DLL preferred VA `0x18002EF20`, RVA `0x2EF20`: `mov edx,3C0h` — index **960** |
| Height header argument before correction | Preferred VA `0x18002EF4D`, RVA `0x2EF4D`: `mov edx,21Ch` — index **540** |
| Header access | `Header::operator[]` calls `std::array<uint64_t,64>::operator[]` |
| Debug range gate | Compare index against `0x40`; invalid index calls `_CrtDbgReport`, with debugger-break / fast-fail paths afterward |
| Corrected production width / height | Compiled Debug assignment lambdas pass **0x17 / 0x18**, the intended slots **23 / 24** |
| Corrected optimized Release object and linked DLL | Header begins at `rbp+0xC0`; viewport width and height are loaded from App offsets `0x2594/0x2598`, then stored at `rbp+0x178/0x180`. Those offsets equal header words **23/24**. Values are zero-extended; no clamp or mask is present. |

Preferred addresses are from the PE image; the live ASLR load base was not retained. The proof does not pretend to have sampled a live instruction pointer.

The original optimized Release object is also inspected. The source expressions are undefined out-of-bounds accesses; the retained optimized scalar block omits stores to the intended viewport fields. The safe Release lexical-scope witness reports index 960 using a checked header substitute. The original unchecked Release access was not executed, and observed Release stack corruption is **not** claimed.

`compiled/causal-excerpts.txt` retains the relevant function, Debug array accessor, atomic-load helper and corrected dimension assignments. Full disassemblies remain temporary active-investigation evidence. `compiled/release-scalar-excerpt.txt` retains the optimized block without inventing a Debug-like failure mode for Release.

## Compact before / after witness

[authority-scalar-witness.json](authority-scalar-witness.json) preserves the extracted expressions, enclosing declarations, source/binary hashes, compiled locations and actual Debug/Release CPU results:

| Version | Key expression | Header index | Stored viewport value | Result |
|---|---|---:|---:|---|
| Old | `h[Width] = a.extent.width` | 960 | 960 | Outside 64-word array |
| Old | `h[Height] = a.extent.height` | 540 | 540 | Outside 64-word array |
| Corrected | `h[nc::frozen::Width] = a.extent.width` | 23 | 960 | Valid write |
| Corrected | `h[nc::frozen::Height] = a.extent.height` | 24 | 540 | Valid write |

Both configurations execute the extracted assignments under the production enclosing dimension declarations. The old-key witness safely records both attempted indices through a checked substitute, without executing Release undefined behavior. The corrected witness uses the real Header and confirms all other words are unchanged. Changing the viewport to 960×582 changes the stored height to 582 while the slot stays 24. The permanent full-function regression additionally exercises 1×1 and 3440×1440. The Debug native-header assertion witness is separate and exits from its process-local report hook before any dialog.

The compiled Debug/Release paths and source comparison show that the old unqualified indexing path is gone. Source comparison also verifies that all 23 scalar right-hand expressions and all code outside this scalar block are unchanged. No generic bound clamp, modulo, masking or viewport-value substitution was introduced.

## Every immediate blocking or failure mechanism

The body has 23 scalar assignments. Its direct callees in the original Debug build are two `atomic<uint64_t>::load` calls and header/array accessors. The original inner body has no application loop or conditional wait. The following audit covers the body and marker boundaries, not unrelated renderer phases.

| Mechanism | Production inspection and offline result |
|---|---|
| Lock or monitor acquisition | None in the scalar body or header accessor. The recorder's existing single CAS attempts ownership once; contention records a drop and returns rather than waiting. Existing contention/overflow fail-closed tests remain passing. |
| Wait, event or fence | None in this body. Frame 167's fence had already completed. Scalar and header witnesses execute without any GPU wait. |
| Atomic / cross-thread read | Frame and swap are existing lock-free 64-bit atomics. Disassembly shows a value load and Debug memory-order validation, with the compiled order constant valid. No seqlock, retry or version loop. |
| Generation / resource ownership | Reads already-published App scalars; no resource acquisition, retirement or publication occurs here. Publication remains owned by the render/update path before recording. Retained generation and buffer lifetimes match the preceding live report. No ownership redesign is justified by this defect. |
| Submission lifetime | Three reads use the existing `a.submission`: physical generation, terrain version and flags. The managed caller keeps its stack submission and pinned backing arrays alive through the synchronous native viewport call; callback/update returns before recording. The live submission pointer itself was not captured, so arbitrary corruption or an unrecorded address fault cannot be disproved from that session. CPU-owned valid-input probes return. |
| Mapped GPU memory | Not dereferenced inside `AuthorityScalars`. The entry's mapped-buffer address is identity context. The separate incoming-pupil read is operation 49, after this block, and was not reached in the live trace. |
| Native/API call | The scalar body makes no Vulkan, wait or callback call. Its Debug range-check helper can enter `_CrtDbgReport`; this is the proven failure path. Marker emission retains its existing QPC/thread-id calls. |
| Callback | No application callback in the body. CRT reporting can invoke an installed report hook or reporting UI. The CPU witness installs a process-local report hook that records the assertion and exits before any dialog; no system setting is changed. Actual live CRT UI state remains unobserved. |
| Retry/spin/seqlock/version loop | None in the body. Recorder copying/checksumming uses fixed bounded loops; it never waits for disk, reader progress or writer-slot reuse. |
| Header bounds / lifetime | **Proven invalid indices 960 and 540 into 64 words.** Width is the first offending store. Header storage is local and initialized before `HeaderClock`; dimensions do not belong in the indexing namespace. Corrected scoped keys pass native header bounds in both configurations. |
| Stack/runtime helpers | Header construction, stack probing and clock acquisition precede the recorded operation-8 entry. Debug accessors have normal call frames. No additional stack-growth wait, exception or OS scheduling mechanism was observed; arbitrary hardware/address faults are not claimed disproved. |

The capture slot is reserved only after header construction. Both slots were free at the captured interval start. Neither readback completion nor writer ownership can account for the proven bounds error.

## Frame-168 authority reconstruction

The source is the intact 6,820-record marked retry, its resource ledger and application log, preserved under `prepared-live-retry`. No missing historical geometry is synthesized.

| Input / owner | Retained or reconstructed fact |
|---|---|
| Frame / submission / completion | Recording **168**, submitted/completed **167**, submission sequence **230** |
| Command / thread / swap generation | **1888345303616 / 15916 / 2** |
| Active / incoming generation | **1 / 0** |
| Prepared / cull / raster pupil | **1 / 1 / 1** |
| Topology | Family **1**, hash **0x118D7D350CB66136**, **13,826 vertices / 27,648 triangles** |
| Viewport data | Actual **960 × 582**; lexical renderer defaults **960 × 540** are separate constants |
| Work in frame 168's retained recording | **13 draw events; 4 dispatches**, with group products **217 + 1 + 432 + 432 = 1082** |
| Physical generation / terrain version | Application context publication logs **4 / 5** |
| Slot / preparation state | Both capture slots free, identities zero; no active/incoming preparation fence pending |
| Exact header QPC, submitted flags, pointer and full memory contents | Not all retained; no bit-exact whole-header replay is claimed |

The viewport-index failure does not depend on missing memory or geometry. Its two bad keys are compile-time constants for every viewport. The corrected CPU fixture exercises 960×582, 960×540, 1×1 and 3440×1440 and checks adjacent authority fields. The remaining synthetic payloads are explicitly synthetic; their passing membership checks do not replace live GPU membership qualification.

## Correction and narrower instrumentation

The capture writer now explicitly qualifies all 23 scalar header keys as `nc::frozen::…`. This fixes the owning diagnostic code without renaming renderer dimensions, changing quality, altering terrain behavior or modifying resource/synchronization ownership.

The previous extraction dropped the enclosing renderer namespace's dimension declarations. Thus its header expressions bound to the desired enum values and concealed the production defect. The permanent extractor now retains those production declarations in an enclosing namespace. The parity reference explicitly contains the proven two-key correction before comparison; it is not falsely described as a successful execution of the unsafe original.

Each meaningful scalar store now has an entry and return marker. The 23 sub-operations are:

1. Frame; 2. record QPC; 3. swap; 4. generation; 5. incoming generation; 6. topology hash; 7. topology family; 8. prepared pupil; 9. cull pupil; 10. raster pupil; 11. vertices; 12. triangles; 13. draws; 14. dispatches; 15. groups; **16. width; 17. height**; 18. physical generation; 19. terrain version; 20. incoming vertices; 21. incoming triangles; 22. incoming topology hash; 23. flags.

The return event follows the actual assignment. An exception cannot manufacture a return. Protocol **2** records scalar identity and header key while retaining outer operation 8, frame, command, resource/generation context and last successful outer operation. The observer still decodes protocol-1 historical journals. Bad scalar keys, order, ownership, mixed versions or incomplete completion fail closed.

The normal first eligible trace grows from 103 to **149 fixed 512-byte records**: **76,288 bytes**, an increase of **23,552 bytes**, within the unchanged 160-record cap. It still runs only once per application lifetime. No render-thread disk I/O, allocation, lock, wait or extra GPU command was introduced. Existing phase deadlines are unchanged and scalar events remain informational.

## Offline qualification

| Qualification | Debug | Release |
|---|---:|---:|
| Native renderer and observer builds | PASS | PASS |
| Extracted production recording/authority probe | **11,927 checks PASS** | **11,927 checks PASS** |
| C++ allocations measured in recording path | **0** | **0** |
| Consumer protocol checks | **256 PASS** | **256 PASS** |
| Outer interrupted-call classifications | **50/50** | **50/50** |
| Scalar interrupted-store classifications | **23/23** | **23/23** |
| Legacy protocol-1 prefix checks | **182 PASS** | **182 PASS** |
| Capture ownership probe | **28 PASS** | **28 PASS** |

The original scope witness reports index 960 in both configurations. A separate Debug child using the real native Header reaches `array subscript out of range` and exits with the expected witness code 71 from its local CRT hook; no renderer or Vulkan library is loaded by that probe.

The 42-command recovery matrix passes all expected outcomes, including scalar-width non-return, outer-call non-return, missing GPU progress mocks, fatal results, recorder overflow, shutdown phases, frozen-storage/backpressure cases and corruption checks. The scalar-stall mock retains `AuthorityScalars`, pending `Width`, last-returned `Groups`, key **23**, and stops on the unchanged one-second Record deadline. It is an injected CPU sleep, not a recreation of the actual CRT state. Observer-loss child exit is **1008.678 ms Debug / 1006.9168 ms Release**, both passing.

Existing tessellation, moving-pupil ownership and presentation regressions pass. Native shader outputs remain byte-identical to the accepted candidate. Native/observer binaries are isolated under this evidence root; the copied application candidate retains the previously qualified managed binaries and assets, with only its native DLL replaced. No application executable or GPU route was run in this responsibility.

CPU-only paired recording costs, 65 samples each, include all markers versus a corrected reference without markers. They do **not** measure GPU capture/readback or real-machine render overhead:

| Configuration | Marked median | P95 | P99 / max | Median paired added cost |
|---|---:|---:|---:|---:|
| Debug | 0.1178 ms | 0.1424 ms | 0.1729 ms | 0.1011 ms |
| Release | 0.0869 ms | 0.0924 ms | 0.0971 ms | 0.0751 ms |

## Preservation, limits and stop

`preservation.json` and `current-task-manifest.json` seal source/build identity, Git preservation, prior live evidence, accepted terrain regressions and ordinary application/settings hashes. The scoped changes are the capture writer, trace producer/decoder, CPU mocks/regressions, extraction and this report/index. No timeout, rendering-quality, synchronization, runtime ownership, driver/system or KSA change occurred.

The preservation audit caught two legacy qualification sidecars overwritten by the new consumer's compatibility check. Their raw journals were unchanged. Replaying copied journals with the preserved original consumer reproduced both original accepted report hashes exactly; those exact bytes were restored. The new consumer's results are retained separately under this revision. Final preservation therefore verifies the original reports as well as the raw evidence.

The temporary diagnostic package has a **4 GiB** bound. Diagnostic output created is approximately **2.339 GB (2.178 GiB)**. This report and compact witness retain approximately **0.023 MB** of permanent evidence within a **64 KiB** budget, in addition to the permanent regression/tooling source. Approximately **2.339 GB** of generated output is disposable after appropriate acceptance and authorized evidence consolidation. Exact pre-seal bytes are in `preservation.json`. Rebuildable binaries, symbols, disassemblies and mock journals remain temporarily available for this active Project Control review; none is promoted to a permanent raw archive. No evidence was deleted and nothing was banked.

The final comparison covers **3,557** entry files, **420** accepted build artifacts, **31** preceding qualification artifacts, **37** retained live-evidence files and **8** historical evidence files. All prior identities are preserved. The copied application's **250** managed/asset/shader files and **130** native shader outputs match their accepted predecessors. HEAD, index, refs, ordinary executable and saved player settings are unchanged.

**STOP for Project Control review before any future live authorization.** Hardware frozen captures, exact GPU visibility/compaction membership and real capture/readback cost remain unqualified. The original blackout and historical population discrepancy are not closed by correcting this diagnostic failure.
