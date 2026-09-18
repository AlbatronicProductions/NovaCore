# Stage 5 — simplified Florida slab and Solar launcher convergence

**PASS — SIMPLIFIED FLORIDA SLAB ROUTE READY FOR MANUAL ACCEPTANCE.**

Manual acceptance **PENDING**. Stage 5 is not yet fully accepted. Stage 6 **CLOSED**. UNBANKED.

Baseline HEAD/main/origin/main/remote main: ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5. Branch: codex/srv01-supported-contact-admission. All 67 historical tag refs unchanged; nothing staged, committed, tagged or pushed.

## What changed

The obsolete full-facility presentation is bypassed in the Florida support route. One explicit finite support box now owns both physical contact and visible slab placement. Stock SRV-01 is centred with its authored aft support face on that box's top. No middle cube, arms, hidden collider or old facility caster participates.

**FL Launchpad -> Play remains the existing Solar world.** Its focus/planet switching, overview/orbits, continuous zoom, surface camera, celestial bodies and lighting remain. It consumes the same site/slab/spacecraft construction as the bounded manual route; it does not launch the isolated manual harness. The currently qualified contact episode still ends at 1,200 intervals and holds its copied site-relative endpoint. Solar exploration continues independently. No future spacecraft control, warp evolution, departure or launch capability is implied.

See [architecture](architecture.md) for authored dimensions and physical/render owner mapping; [direct current KSA and Discord history](ksa-convergence.md) for the mandatory ADOPT/ADAPT comparison.

## Physical and canonical proof

Unchanged stock definition digest: ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf.
Mass705kg; fuel30kg; oxidizer45kg; main/RCS OFF. J2000 slab/site digest:
3a00abf1bd7926bacc597fc8142d4bdef22a36301107fdbef59cc1b51bca336e.
No hash is used as physical authority in place of the authenticated value binding.

| Witness | Result |
|---|---:|
| Peak deepest authored corner penetration | 0.09750102006811368 mm |
| Settled maximum height error | 0.07901107027863397 mm |
| Settled horizontal drift | 0.0000932385073805606 mm |
| Settled relative speed | 0.0000055077650646174094 m/s |
| Settled angular speed | 0.0000021950650053798498 rad/s |
| Orientation matrix drift | 0.000004606867605723037 |
| Final support | 600/600 |
| Minimum finite slab-edge clearance, all corners | 23.243980844171386 m |
| Minimum exported native contacts | 4 |
| Settled geometric contact-feature changes | 0 |

All unchanged physical bars pass:20mm peak,0.122mm settled height,0.00365993m/s settled speed and original angular/drift bounds. The initial four support corners span +/-0.26m in the horizontal plane and enclose the independent COM projection. Initial all-corner slab-edge margin is23.24410285091158m. These are finite-box tests, not an infinite-plane assumption. Geometric feature stability does not assert identical internal manifold bytes.

There are exactly400 intervals of16,666 ticks and800 of16,667, totalling20,000,000;1,200 publications, StateRevision increments and history records; TimelineRevision unchanged. Stores/mass unchanged. All frontiers and histories match exactly across30/60/150/240Hz and the delayed40-frame schedule; delayed service exhausts its four-interval budget and drains retained debt without lost work.

Independent J2000 p/v/q/w oracle maxima: position7.861267317148919e-9m, velocity5.796914039811765e-13m/s, orientation matrix1.887379141862766e-15, angular velocity1.258635097526423e-19rad/s. Added actual-current-UTC Solar tests also complete1,200 intervals/600 supported and independently verify the split-angle position mapping. Their exact epochs and errors are in results.json. The added nonzero-epoch oracle checks position; it is not relabelled full p/v/q/w coverage.

Historical east-offset graded-ground qualification remains separate and was rerun successfully. Existing evidence was not rewritten.

## Allocation, storage and validation

- Complete/inertial export, backlog, no-work and copied presentation: **0 B**, checked entry/exit; deliberate control **152 B**. Debug and Release.
- Combined retained contact/presentation payload: **6,469,952 B <8,388,608 B**. Managed retained delta4,634,424; native pool475,136; reusable GPU mesh payload1,359,096; new slab GPU payload1,296. Includes owner/history/site/slab/render arrays. Shared terrain datasets, device/driver bookkeeping and common renderer resources are excluded; this is not total working set. Both configurations agree.
- Native and full solution builds: Debug/Release, zero warnings/errors.
- Full Simulation: **80/80** top-level groups in each configuration.95 PASS-prefixed lines include nested witnesses.
- ReferenceFrames, Precision and verified BEPU: PASS both.
-18 existing affected Solar/terrain/facility/camera cases: PASS both; no old Solar tests weakened.
- Launcher:18/18 both; deployed normal Release path rechecked18/18.
- New shared-consumer/current-epoch, physical, historical ground, allocation and presentation gates: PASS both.
- Independent15-point source/test red team: PASS after initial COM/corner, rotation/scale, current-epoch and truthful-status gaps were closed.

The storage test was subsequently strengthened to include the1,296-byte native slab payload and rebuilt/rechecked both configurations. Runtime source stayed identical to the live measurements.

## Performance — current Stage-5 integrated contract

Three fresh Release processes,128 warm+1,024 measured complete operations. No other build/test work overlapped.

| Process | Median ms | P95 | P99 | Max | GC0/1/2 |
|---|---:|---:|---:|---:|---|
| 1 |0.0683|0.0707|0.0733|0.1508|0/0/0|
| 2 |0.0677|0.0684|0.0718|0.1345|0/0/0|
| 3 |0.0679|0.0804|0.1239|0.1710|0/0/0|

These do **not** pass the historical standalone0.050ms median micro-gate. That is not the current Stage-5 integrated acceptance contract. No threshold was relaxed or optimization performed in this correction.

Two sequential fresh960x540 Vulkan/RX6800XT live processes,4,800 frames each, one per actual entry path. A disposable compile overlay indexes the scene's existing timing arrays and takes GC snapshots at start/128-frontier boundary/end. It does not alter work, dt, publication or physics; row summaries are emitted only after completion. Manual binaries are separately sealed **uninstrumented** outputs.

| Population | Manual median/P95/P99/max ms | Solar median/P95/P99/max ms |
|---|---|---|
| Active display callbacks |5.5551 /5.9190 /6.3120 /67.1873|5.5555 /5.8365 /6.2107 /88.4521|
| Active bounded service |0.0027 /0.0572 /0.1405 /63.8015|0.0027 /0.0665 /0.2007 /82.5204|
| Display after128 publications |5.5552 /5.8726 /6.3053 /6.4408|5.5550 /5.7923 /6.0123 /11.3598|
| Service after128 publications |0.0026 /0.0511 /0.0707 /0.1817|0.0026 /0.0595 /0.0765 /0.1414|

Both total and warm GC deltas:0/0/0. Median/P99 satisfy the current approximately6.67ms display objective with contact headroom; maximum is explicitly not an every-frame6.67ms guarantee.

Cold scene preparation:461.1624ms manual /307.3013ms Solar, before host credit, excluding native GPU startup. A separate already-code-warmed owner construction population measured133.9169/134.4236/135.1483ms. These are distinct populations, not interchangeable cold claims.

Manual service maximum63.8015ms is callback1, followed by display67.1873ms at callback2. Solar maximum82.5204ms is callback0, followed by display88.4521ms at callback1. Thus the new worst service/display pairing is confined to first-use work. No comparable warm service recurrence occurred; individual JIT/constraint/host subcost attribution is not claimed.

Solar warmed display has11.3598ms and6.7157ms outliers. Its warmed service maximum is0.1414ms; the display delay's precise outside-service cause is **UNATTRIBUTED**. No renderer optimization or extra capture campaign was opened. Native full4,800-frame maxima67.298/88.600ms also include post-completion reporting and are kept separately in results.json.

Both runs retain exact stock stores/mass and complete1,200 publications. Terminal debts152/5,735ticks are admitted host credit beyond the finite episode, not unpublished required intervals; no credit is discarded.

## Preparation errors, provenance and scope

No failed engineering result is hidden. results.json records generation-selection/test setup, nullable compile, internal factory access, current-epoch oracle mismatch and observer deployment failures. The observer failed to load before Main on its first attempt; no physics or performance population ran. Its managed deployment was corrected before the two captures.

entry-seals.json seals2,935 pre-existing files.17 existing files have changed raw seals, plus2 new code/test files.16 changes are intended implementation/test paths. The other is the existing Launcher test file re-materialized from HEAD during restoration: it is Git-content-identical and has no semantic diff, but its raw entry seal differs. That byte-level difference is disclosed, not reported as an exact original-byte match. All pre-existing evidence, authored physical definition/assets, production solver policy, event/resource arithmetic and historical tags remain untouched.

source-seals.json, entry-changes.json, results.json and manual-binary-seals.json retain exact current provenance. The broader unbanked Stages1–5 working tree predates this correction and is not attributed to it. No README/legal/public-current-state change, new milestone, Stage6 or banking.

## Reproduce / manually inspect

See [manual-acceptance.md](manual-acceptance.md). Build.ps1 builds exact current sources; validate.ps1 runs the bounded gates; build-observer.ps1/capture.ps1 reproduce the two measured consumers. Run three independent Release Graphics test processes with --florida-slab-costs for the complete-operation table. No correctness run is replaced by an observer result.

See [cleanup.md](cleanup.md) for exact disposable paths, counts and reviewed manual commands.
