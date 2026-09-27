# Final authorized GPU attempt — payoff gate NOT MET

Project Control accepted Stage A and explicitly authorized one final bounded GPU
correction, conditional on a proven path to the actual 6.6667 ms frame contract.
This review applies that authorization. The stop is an evidence/payoff failure,
not missing implementation permission. Production GPU corrections: **0**.
Final-attempt native verification exposures: **0**. No broad renderer work opened.

All measurements come from the already completed canonical Stage A witness;
no repeat native exposure was used for averages or diagnostic exploration.
[native-results.md](native-results.md) retains complete whole-frame/GPU
median/P95/P99/max, overruns, longest runs, headroom and outlier classification.
[final-gpu-payoff.json](final-gpu-payoff.json) retains the offline paired analysis.

The driver log identifies the current published detailed owner at frame448.
Through frame563, 116 non-main-engine held-contact frames have this owner:

| GPU responsibility, ms | Median | P95 | P99 |
|---|---:|---:|---:|
| Total GPU | 9.63375 | 9.73216 | 9.82620 |
| Detailed terrain draw | 8.07688 | 8.10952 | 8.12260 |
| Cull/compaction | 0.72548 | 0.72652 | 0.72724 |
| Necessary GPU recovery to 6.6667 | 2.96708 | 3.06549 | 3.15953 |

The draw alone exceeds 6.6667 in all 116 frames. This proves the dominant draw
responsibility, not that its entire duration is fragment work or removable.
Recovering approximately 37-39% of this draw is necessary merely for GPU closure
with other GPU work fixed. Actual player cadence imposes the additional CPU,
submission and publication constraint.

Native Update waits for the previous frame's single fence before completion
inspection, managed physical service and render preparation. Draw then records,
submits and presents. This is confirmed by native source around Update, Draw and
the owning message loop, and by monotonically correlated recorder identities.
The GPU duration and fence duration largely overlap; they are not added.
The fence timing also contains completion/capture bookkeeping. Moving a wait
label or overlapping CPU work cannot make an independently 9.6 ms GPU meet 150 FPS.

| Candidate responsibility | Absolute scope / recoverable evidence | Payoff decision |
|---|---|---|
| Cull short-circuit, immutable bounds or visibility reuse | ~0.73 ms held; even removing ALL cull leaves paired GPU ~8.91/9.01/9.10 ms | Insufficient as the one correction |
| Unchanged preparation/uploads | Published physical terrain already reused; ordinary pre-surface timestamp effectively zero in held frames; CPU uploads separately bounded | No repeated ~3 ms avoidable GPU owner established |
| Draw batching / immutable specialization | One published owner/indirect draw; mode specialization and early tests already present | No redundant draw/mode to retire |
| Buffer placement/lifetime | Existing role-specific local mapped buffers and exact fence-before-reuse contract are present | No new measured avoidable budget established |
| Positive material evaluation | Large draw scope, but exact safe recoverable milliseconds UNKNOWN; old altered-noise experiment is sensitivity only | No concrete correction passes proof gate |
| Lower tessellation, precision, density or altered contributing detail | Would change the accepted contract | Excluded; not attempted |

The smallest plausible new reuse work fails even outside contact. In 102
post-handoff frames with zero contact slices, granting the entire recorded cull
scope zero cost at the matching fence boundary leaves hypothetical cadence
7.0181/8.6610/9.9974 ms median/P95/P99 and 72/102 overruns. For 216 grounded frames,
the same optimistic model leaves all 216 over budget. These are fixed-work
counterfactual bounds, not native results or scheduling predictions. No claim is
made that all cull can actually be removed. Source changes cannot be justified by
subtracting unrelated timing percentiles or assuming every expensive instruction
is redundant.

CURRENT KSA was checked only for this equivalent responsibility: prepared geometry
then filtered presentation, immutable pipeline mode, shared per-fragment inputs
and contributing material selection. Installed-method identities and authenticated
history links are in [stage-b-review.md](stage-b-review.md). ADOPT those boundaries;
ADAPT NovaCore's FP64 body-fixed material identity. These principles are already
present. KSA authored texture content is not an exact replacement field. Neither
newer plume caching nor retired duplicate material loading proves a transferable
~3 ms saving in this terrain draw. No assets/code copied, no KSA writes/gameplay.

Independent CPU/performance and KSA/renderer reviewers both concluded that no
single output-preserving correction with sufficient evidence-based payoff is
established. This does not prove optimization impossible. It forbids spending this
final attempt on sub-budget polish or an unproved material change.

**REVISE — stop before implementation.** Keep Stage A, recorder maintenance and
the accepted physical/control/support outcomes. Current package/source unchanged;
existing Debug/Release, permanent regressions and package qualification remain
the evidence for this candidate. The conditional new renderer build/test/native
sequence is not entered because no GPU production change passed the gate.

Prepare this strongest honest candidate for Project Control banking consideration
with performance gap OPEN, blackout UNRESOLVED and Player acceptance HOLD. No bank,
commit, tag, push or milestone is authorized by this preimplementation gate result.
