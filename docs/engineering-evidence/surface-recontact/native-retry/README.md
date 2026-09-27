# Canonical recorder deployment and one native Surface Recontact retry

2026-09-27 — **REVISE. FROZEN · UNBANKED. STOP FOR PROJECT CONTROL.**

The corrected recorder is deployed and passed the bounded native workload. The
real-terrain contact → grounded RCS → powered relaunch → FreeFlight chain passed.
Recurring production contact-continuation cost prevents campaign acceptance.
Blackout remains unresolved; banking remains HOLD. Exactly one player process was
launched. No further launch, physics correction, commit, tag or push followed it.

## Candidate and pre-native checks

Canonical entry: `E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe`.
Package SHA256: `b542d8908055543b41f5543dee8fda2ba4e7f54d36c33ac8b30c6bb030bd7d6e`.
Source/test freeze SHA256: `2f57a80a0f2a972a5bb1d8742fee92f169b634cdab8564e5335bceccd1b146a2`.
These are manifest digests, not commits. HEAD remains
`8c189b28ce2a68f97de734d1589acb500c41fd99`.

- Debug and Release: zero warnings/errors; recorder **22,708** checks each;
  retention **198** checks each; journal cut qualification **342** checks.
- Native-free Vulkan stub: 35,303 events durable, no GPU loaded.
- Offline observed-burst qualification: 78,855 events, no fault/loss/debt,
  zero producer allocations; 2,433/8,192 maximum occupancy under injected latency.
- CPU route: 1,259 checks in each configuration, 537 physical ticks, actual
  terrain contact, grounded RCS and powered FreeFlight restoration.
- Package: 134 files, 66 shaders, corrected recorder/Diagnostics identities.
  Frozen native DLL remains `2244a96be294c23032ab531d8865ec0d1be2c147a01afa4ce0c4f2a4152f7be6`.

The existing qualified batching correction retains the 8,192-event ring and
maximum 1,020-event durable transactions. Required observations add only observer
watermark brackets, full-run commit maximum, and an explicitly selected test
route/read observer. Producers, event coverage, journal format, Vulkan, resources,
contact values, terrain filtering and simulation source are unchanged.

Watermark brackets are independently tested against producer/ACK interleavings;
they report bounds, not a fabricated exact high-water value. This run's lower and
upper bounds coincide. Corrupt/nonmonotonic brackets refuse. No renderer waits
for persistence or performs routine journal I/O.

## Native witness

Session `b0a8e148-2b15-419a-9b6c-a5734428b404`, PID41100, exit0. The process lasted
42.915s including the pre-GPU storage notice. Recorder lifetime17.431s;
623 rendered frames, approximately14.077s rendering. Window3440×1440, game
viewport3440×1322. One direct UI observation showed the upright powered craft
beside the slab. No display/GPU/system anomaly was observed; the bounded System
event query returned no warning/error/critical events. This does not close blackout.

The ordinary save loader admitted a daylight short-craft fixture at x80m, outside
the slab, with velocity(0.1,−0.1,0)m/s. No touchdown teleport, velocity clamp,
landed-state bypass or invented restitution was used. The production controller
received held Q, release and Z through the existing native input boundary.

| Witness | Canonical sequence | Evidence |
|---|---:|---|
| Real terrain | 6 onward | Current solved body/static pair, world/generation and feature IDs;110 sampled frames |
| Stable ground | 102 | Four terrain contacts; speed0.0000457m/s; angular speed0.00000122rad/s; mass1304kg |
| Grounded RCS | 110/114/118 | Eight actual jets, four terrain contacts; mass falls to1303.99267578125kg |
| Released ground | 178 | Four terrain contacts, jets0; ignition requested here |
| Powered separation | 179 | Main on, fuel consumed, upward velocity0.21834m/s, contacts0 |
| FreeFlight | 544 | Consumer FreeFlight, contacts0, world retired; y222.330m, vy81.731m/s |
| Normal close | 548 | Failed=false; renderer teardown and process exit0 |

Terrain identity comes from the current solved manifold, never stale `Deepest`
data. Raw manifold/energy diagnostic capture was disabled throughout. Grounded
save/reload was already proven in the earlier native run and was not repeated.

The initial CPU-only3m/s seam recipe settled sideways; ignition consequently drove
it laterally into the existing terrain-refinement refusal near x192m. Its log is
preserved. It was unsuitable for an upright relaunch witness; no physics was
changed to force it through. The gentle fixture still crossed terrain generations.

## Recorder acceptance — PASS for this bounded retry

Independent raw-journal reconstruction verified all15,283 sequence/checksum-valid
events,686 submits/686 matching completions,278 resource births/278 retirements,
zero open operations, zero pending submissions and zero live resources at close.
Produced=Durable=15,283; COMPLETE, clean, zero faults/drops/corruption.

| Measurement | Result |
|---|---:|
| Ring peak and maximum volatile lag | Exactly1,233 events |
| Ring headroom | 6,959 events /84.95% |
| Durable transaction median/P95/P99/max | 1.7175/2.501/5.4591/30.3401ms |
| Persistence CPU | 296.875ms over17.431s |
| I/O | 8,458,317 written bytes; approximately485,233B/s |
| Average event throughput | 876.75events/s |
| Retained logical session size | 83,916,383bytes |
| Recorder producer median/P95/P99/max | 20.3/26.1/39.2/93.4µs |
| Timed producer allocations | 0calls /0bytes |
| Recorder measurement retained bytes | 4,479,176 |
| Persistence managed retained /peak working set | 621,936 /58,900,480bytes |

Producer scope is281 measured flight frames. Longest durable transaction covers
all post-startup Commit calls, including any internal rotation; constructor
initialization is excluded. This native session made zero rotations. It does not
repeat the earlier669s exposure: offline rotation/fault/burst evidence remains
necessary support. All five session-captured package identities match the frozen
canonical manifest; unchanged native source/DLL proves no new GPU submissions,
waits, fences, features or resource-usage behavior from these recorder changes.

## Performance — production REVISE

| Owner/scope | Median | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|
| Physical service,281callbacks | 9.0166ms | 142.3734ms | 163.4347ms | 295.4551ms |
| Qualification read observer | 0.0036ms | 0.0162ms | 0.0885ms | 1.3628ms |
| Whole renderer,623frames | 5.963ms | 82.972ms | 156.899ms | 1110.031ms |
| Service allocation per callback | 157,536B | 1,975,872B | 5,166,480B | 5,596,752B |
| Observer allocation per callback | 48B | 152B | 3,688B | 3,744B |

Service total8,358.3083ms and126,451,392 allocated bytes; observer total3.3179ms and
49,392bytes. Process sampled peak working set3,414,978,560bytes is whole application,
not recorder-exclusive memory. Native GPU reported mean4.299ms; full GPU
percentiles were not retained. Six periodic text timing samples cannot establish
a full distribution. Fence mean6.436/P9510.068/max12.650ms. The1.11s whole-frame
maximum includes the cold saved-flight/presentation load, separate from recurring
service tails.

**83/281 service callbacks exceed25ms;61 have zero physical contacts.** There are
55 consecutive above25ms callbacks. World4's37 callbacks have median73.8063ms;
world5's17 callbacks have median145.1106ms. All54 exceed25ms, but only four change
world/mesh generation. Rebuilding terrain alone does not explain sustained cost.

The proven production mechanism is post-contact continuation/refinement:

1. `LocalContactSource.CreateCraft` bounds surface speed using authored minimum
   hull half-width and native slice duration.
2. As ascent crosses approximately8.192/16.384/32.768/65.536m/s, dyadic refinement
   reconstructs the private world and terrain from the untouched canonical endpoint.
3. Native slices rise16→32→64→128→256 per64Hz interval; service can execute four
   intervals in one callback.
4. Conservative `CraftClearance` retains native continuation after contacts reach
   zero, until approximately222m here. Increasing speed therefore increases cost
   before the actual consumer handoff.

Mesh transition callbacks include95.3644 and170.6412ms; world recreation reaches
295.4551ms. These timers encompass exact service preparation, clearance, BEPU,
terrain work and publication. Exact milliseconds per inner subsystem were not
isolated; do not label the full service time "terrain preparation." The source and
generation/speed witnesses establish a production owner and exclude qualification
capture as its cause. No quality reduction or physical correction was attempted.

## Preservation and review

Pre/post checks pass for frozen source, package, simulation/Core/native/assets,
original failed session's ten files, settings, HEAD, index and user branches/tags.
Only Codex turn-diff bookkeeping refs differ from earlier campaign snapshots.
The new healthy session was explicitly pinned using `pin-runtime`; original
forensic files were not edited or deleted.

Independent reviewers reconstructed recorder correlation and challenged the
contact chain/performance attribution. Both conclude **overall REVISE**. Two
harness findings—failure-report overwrite and timing from requested rather than
observed RCS input—were fixed and qualified before this only native launch.

The text diagnostic logger dropped72,561characters during burst logging. This is
separate from the mandatory recorder, whose full journal and physical281-row
qualification record are intact. Missing text is not claimed as evidence.

See [qualification.json](qualification.json) for compact causal rows, identities,
measurements and retained-artifact hashes; [reproduce.md](reproduce.md) for offline
verification. The next bounded responsibility, if Project Control authorizes it,
is continuation/refinement and conservative handoff cost, including terrain
reconstruction. No further native retry or correction is authorized here.
