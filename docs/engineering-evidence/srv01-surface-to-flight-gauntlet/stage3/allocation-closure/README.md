# Stage 3 allocation closure and handoff qualification

2026-09-17. **PASS — STAGE 3 QUALIFIED. UNBANKED.** This closes the historical 624 B stop in [the original report](../README.md); it does not rewrite that failure. No milestone, commit, tag or push. The downstream product gate is recorded separately in [Stage 4 promotion](../../stage4-promotion.md).

## Current repository and architecture

HEAD/main/origin/main/remote main remain `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`, branch `codex/srv01-supported-contact-admission`. All 67 historical tags are unchanged; nothing staged. [Identity](identity.json) seals 29 source/test inputs and accounts for all 24 Stage-2 seals, including six overlapping files retained in the two small source archives. No historical evidence is rewritten.

The same canonical SRV-01 commits its contact-owned predecessor, transfers permission to the existing free-flight consumer, and continues under the same stores, mass/COM/inertia, material origin, gravity, revision and history authority. Native world/body/generation remain allocated for identity/disposal, but contact motion permission is revoked. Missing contacts alone grant no release: every admitted free-flight interval needs a whole-interval clearance bound over the actual authored corners. No recontact, general CCD, new renderer, launch-capable engine, Earth launch or Florida capability is claimed.

The bounded fixture has prescribed upward separation speed 0.20 or 0.25 m/s, unchanged 600 N engine and initial 30/45 kg species stores. First contact interval is 16,666 ticks; free-flight increments are 15,625 ticks. This is one-way bounded continuation, not stock-powered liftoff from rest.

## Current KSA and live history

Fresh direct installed-source inspection used `E:\Kitten Space Agency`, product `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`. KSA.dll SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`; BepuPhysics `77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`; BepuUtilities `E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68`. No proprietary source/assets copied or changed.

Current source: FullPhysicsEndFrame `06001C03` records support separately from consumer choice. FullPhysicsStepUntil `06001C02`, CertifyTerrainClearance `06001C05` and RunUnconstrainedVehicleStep `06001C1B` preserve one interval consumer and the same staged vehicle. IntegrateVelocityVerlet `06001B92` / ComputeDerivatives `06001B8F` include gravity and consume propulsion once. RemoveEligibleVehicles `06001BEF` concerns frame membership, not automatic removal upon support loss. RunPostWorkAndCluster `06001BE5` / ReturnConstraintSim `06001C25` separate resource retirement from consumer selection. Prepare `06001BC4`, PublishResults `06001BF8`, ApplyResultsToVehicles `06001BEA` and ApplyResultsToMainThread `06003115` establish prepared/ready/apply ownership; Simulate `0600069D` and TryResetForPool `06000683` protect failure poisoning.

The authenticated actual Discord live-changelog was reread, separately from source: [4646/4659](https://discord.com/channels/1260011486735241329/1260112103134724146/1516883816554168393) explains separate support/situation, collision before timestep selection, and preserving high-precision state; [5173/5174/5177 vicinity](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902) records stale sleeping bounds, false landed classification while firing and repeated jet consumption. [Latest visible 5434–5449 vicinity](https://discord.com/channels/1260011486735241329/1260112103134724146/1550158043986010133) did not establish a superseding spacecraft handoff method. Newer history is not assumed installed. This is a bounded relevant-history review, not an exhaustive future-history claim.

- **ADOPT:** one physical consumer; same vehicle; eligibility separate from support observation and native lifetime; failed private work cannot apply.
- **ADAPT:** fixed-slab interval proof, existing qualified RK4, exact NovaCore resource/revision/debt/history and sealed retry semantics.
- **INTENTIONALLY DIFFER:** no automatic recontact, KSA retirement timer, generic vehicle/pooling system or second canonical authority.
- Simplicity judgment: **KEEP** this bounded lifecycle. **SIMPLIFY LATER** only if a separately scoped shared ready/apply responsibility is proven useful. No refactor is required now.

## Exact allocation cause and single correction

The unchanged expanded witness was run once before modification and reproduced **624 B** with checked no-GC entry/exit passing. Scalar boundaries place every byte inside native collision detection; before/after retained object graphs and separately measured identical-array sizes reconcile the full ledger. [allocation-ledger.json](allocation-ledger.json) retains decisive output and restoration hashes.

| Managed allocation | Count | Bytes | Actual factory/lifecycle |
|---|---:|---:|---|
| NarrowPhase overlap-worker array, length 1 | 1 | 376 | `NarrowPhase<T>.OnPrepare`, lazy per-world workspace |
| ConstraintRemover worker-cache array, length 1 | 1 | 80 | `ConstraintRemover.Prepare`, lazy per-world workspace |
| Pool power-11 pointer table, length 1→2 | 1 | 40 | `BufferPool.PowerPool.Resize` |
| Pool power-12 pointer table, length 1→2 | 1 | 40 | same |
| Pool power-14 pointer table, length 4→8 | 1 | 88 | same |
| **Total** | **5** | **624** | **B — cold/preparation state belonging earlier** |

These are existing retained BEPU workspace arrays, not necessary per-publication heap metadata. The one correction prepares them on the existing per-world pool during cold construction: power 11/12/14 measured native capacities 32,768 / 32,768 / 81,920 B, then an empty `CollisionDetection` before shapes, body or slab exist. Source inspection proves that operation updates broad/narrow collision state and flushes; it does not integrate or solve. It changes no physical/canonical epoch, velocity, resource or manifold. No real step is moved outside measurement, no global cache or receipt pooling is introduced. Other contact profiles bypass the new conditional boundary.

The immediate unchanged witness passed **0 B handoff**, **0 B first successor**, positive control **152 B**. Expanded Debug/Release and full suites confirm it. Focused Release raw counters: handoff **31,091,192 → 31,091,192**; first successor **31,091,512 → 31,091,512**. Debug raw counters are retained in [validation.json](validation.json). Entry/exit remain hard failures. No correction #2.

Temporary scalar/object-graph instrumentation was restored byte-for-byte before normal builds. The exact pre-correction world and diagnostic recipe remain small reproducibility artifacts. Two early diagnostic-script anchor assertions and a new-test definite-assignment compile error were corrected before runtime proof; neither was a physical-gate retry.

## Independent physical proof

For the first axial successor, independently integrate `a(t)=600/(m0-(25/128)t)-9.81`, with `h=1/64` and `x=(25/128)h/m0`. Thrust velocity is `(600h/m0) Σ x^n/(n+1)`; thrust position is `(600h²/m0) Σ x^n/((n+1)(n+2))`, n=0..3. Initial position, initial velocity times h and gravity `-9.81h²/2` are separate. Geometric-series tails are bounded by **2.4318491339589914e-27 m** and **9.338300674402526e-25 m/s**. Production RK stages are not the oracle.

| Separation speed | Last contact y / vy | First flight y / vy | Whole-interval clearance lower bound |
|---|---|---|---|
| 0.20 m/s | 0.0008446691391317263 / 0.05069036781787872 | 0.000543086627900251 / -0.08929291966155098 | 0.000200938930144079 m |
| 0.25 m/s | 0.0016779420730916872 / 0.10069036483764648 | 0.002157609515294083 / -0.03929292264178322 | 0.0015559420730910211 m |

Both configurations: observed position error **0** at stored binary64 precision; velocity error **1.3877787807814457e-17 m/s**. Position bounds are **3.3559418548444316e-16 / 5.191441869814417e-16 m**, including series truncation, bounded RK quadrature error and a conservative engineering FP64 rounding budget. This is not a machine-checked universal FP proof. Both are far below one-thousandth of the existing 61 µm tolerance. The original logarithmic velocity witness (~3.25e-13) remains historical; the series avoids cancellation in that independent comparison.

Both endpoints preserve quaternion `(0,0,0.7071067811865476,0.7071067811865476)` and zero angular velocity. Local root gravity stays `(0,-9.81,0)` in the same frame/epoch authority. The translating-frame case uses 0.20 m/s relative separation and +0.05 m/s X frame transport: clearance error **0**, position transport error **2.168404344971009e-19 m**, velocity error **6.938893903907228e-18 m/s**. Original frame epoch is retained.

Angular admission tests turn toward a vulnerable authored corner: 0.00005 and 0.000099999 rad/s admit; 0.000100001 refuses. A small-clearance fixture changes from +2.7176450234867657e-6 m to -5.3114447607279116e-6 m under downward rotation and refuses. These test conservative admission/refusal, not a general tumbling trajectory oracle. The bootstrap allowance is numerical reserve, not a physical angular tolerance.

[proofs.json](proofs.json) retains full mass/COM/inertia/motion witnesses. Default JSON does not expose internal exact-store limbs; [exact-accounting.json](exact-accounting.json) explicitly supplies independently derived rational species amounts rather than treating `{}` as evidence. At ticks 16,666 / 32,291, fuel is **29.99869796875 / 29.997477265625 kg**, oxidizer **44.998046953125 / 44.9962158984375 kg**. Mass is **704.996744921875 / 704.9936931640625 kg**; COM X **1.0417069373581056 / 1.0417114466711892 m**; diagonal inertia `(190.47255,731.5971168708743,731.5971168708743)` → `(190.47255,731.5938052313458,731.5938052313458)` with zero off-diagonals. Changes follow consumption; they are not held artificially constant.

## Ownership, retry, expiry and determinism

Native frontier stays 1 after handoff; body handle/generation and sampled native pose points remain identical through free flight. One prepared proposal commits one resource/motion successor and one consumer change. Ordinary precommit refusal retains the proposal without re-solving; duplicate/stale/default/foreign proposals cannot debit or publish. Abort after native preparation invalidates its advanced private state without canonical rollback. Forced postcommit acknowledgement failure preserves the canonical commit and fails closed; it does not restore contact authority.

Mutable physical state, stores, mass, COM/inertia, command/gimbal, resource/actuator revisions, StateRevision, TimelineRevision, clock, rate, pause, debt and consumer tampering refuse without additional mutation. Owner-thread, reentrancy, insufficient debt, event boundary, capacity, old receipt and save/replay boundaries pass. Old M15.2 zero-gravity free-flight save/replay remains valid; a contact-origin handoff is explicitly outside that old save schema.

Host partitions **1/3/7/15/24/60** receive the same **63,541 ticks**, producing identical exact history and endpoint bits at matching frontiers. Default final frontier **3**, tick **47,916**, StateRevision/history **3/3**, TimelineRevision **0**, retained debt **15,625**. Clearance expiry repeatedly holds that state/debt and refuses native reacquisition or another unsafe interval. No double debit, skipped interval or duplicate integration.

## Storage and performance

Owned-storage conservative upper bound **1,065,176 B < 8 MiB**: managed construction **606,424 B** (includes temporary cold allocation, so overestimates retained managed bytes) plus maximum measured native pool **458,752 B**. Native cold **442,368 B**. Departure profile **80 B**; history payload **6,304 B**, host-credit payload **76,816 B**, already included, not added twice. No static-cache storage. Warmed-process cold creation sample **1.9788 ms**; no new presentation resources.

Three fresh Release processes, 128 warm + 1,024 measured per population, as predeclared in [performance-plan.md](performance-plan.md):

| Process | Population | Median ms | P95 ms | P99 ms | Max ms |
|---|---|---:|---:|---:|---:|
| 1 | complete handoff | 0.1137 | 0.1436 | 0.1988 | 2.0464 |
| 2 | complete handoff | 0.1240 | 0.1418 | 0.1772 | 0.2518 |
| 3 | complete handoff | 0.1255 | 0.1510 | 0.1717 | 0.5861 |
| 1 | first free flight | 0.10725 | 0.1335 | 0.1843 | 0.3203 |
| 2 | first free flight | 0.1196 | 0.1280 | 0.1499 | 0.2091 |
| 3 | first free flight | 0.1214 | 0.1420 | 0.1658 | 0.2899 |

All measured GC deltas `[0,0,0]`. Worst handoff index 55 in process1 and 56 in process3; process3 also clusters at 221–224. **Cause unattributed**, not discarded or claimed fixed. Cold preparation **161.5762 / 163.0153 / 165.8213 ms**, cold first handoff **48.4227 / 47.4733 / 47.3125 ms**. `firstMs` repeated in the free-flight row is this same handoff measurement. Whole-harness CPU **2500 / 2531.25 / 2578.125 ms** includes construction/disposal and is not operation CPU.

Single owner thread, no dispatcher or new lock/wait. Contact predecessor includes ordinary native collision/solve; successor does no native step. Independent review passes this narrow characterization under the current integrated contract, not retired 50/100 µs micro-gates. One transfer per episode and no new render route give no evidence of material integrated regression; this does **not** certify future visible-transition hitch freedom or whole-game FPS. No optimization followed.

## Regression, manual boundary and red team

Debug/Release full solution: **0 warnings / 0 errors** after explicit native prerequisites. Initial default-native build stopped with MSB3030 missing native/shader artifacts; unchanged native sources were built into isolated D/R directories before successful full builds. Full Simulation **79/79 registered groups in both** (94 PASS lines include nested tests). ReferenceFrames, Precision, verified BEPU, focused assembly/powered-support presentation, M14.17 continuation, powered-contact presentation all pass D/R. Launcher **18/18 both**; its unchanged five-parent fixture root was preserved in the disposable output layout. Full suites cover Stage1/2, M15.1/M15.2, exact stores, mass/COM/inertia, history and save/replay.

Two independent read-only reviewers passed the bounded mathematical/ownership and 30-attack red-team gates. No required metadata removed, no disguised real warm step, no hidden cache, no simultaneous consumers, no implicit release/reacquisition, no gravity discontinuity, no retry re-solve or postcommit rollback. The store-reporting gap was explicitly corrected in evidence. Reviewers endorse narrow performance disposition with the retained tails/cold caveats.

Manual required: **NO for this fixture. NOT APPLICABLE.** No Stage3 scene, control or presentation path was added; the headless millimetre-scale ~0.05-second prescribed separation is an ownership/clearance proof. No manual PASS is self-granted, and future player-visible departure still requires its own acceptance.

Startup residual: **UNCHANGED / SEPARATE**. White/unpresented startup precedes physical servicing in targeted observations, one **16,670.3851 ms**. Historical **595.898 / 1,228.1696 ms** callback witnesses remain unresolved; Stage2 powered causation was not established. No startup investigation here.

## Reproduction and disposition

See [reproduce.md](reproduce.md), [validation.json](validation.json), [measurements.json](measurements.json), and [cleanup.md](cleanup.md). All instrumentation is absent from current production. Source archives are concise exact predecessor inputs, not bulk build trees. Historical Stage1/2 and original Stage3 failures remain intact.

**PASS — STAGE 3 QUALIFIED.** Stage4 promotion may now be assessed; no propulsion implementation is authorized by this result. UNBANKED. No milestone assigned.
