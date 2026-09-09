# VERIFY B - pacing and KSA equivalence red team

This review tests the proposed conclusion against `work-b.md/json`, `work-c-source.md/json`, and `ksa-history.md`. It spot-checks the retained KSA settings/main-loop source and NovaCore's present, benchmark and host-duration call sites. It does not repeat Work C's live assembly inspection or the lead's authenticated Discord review. No application, GPU test, build, Windows-event scan or production edit was performed. Earlier sealed evidence is unchanged.

## Judgment

**Over-rendering classification: INSUFFICIENT EVIDENCE.** The evidence supports retaining the current presentation policy and proposing no cap candidate. It does not support declaring the presentation path proven optimal, declaring the candidate safe, or attributing the manual freeze to pacing.

Project Control clarified that the approximately five-minute Florida Launch Site / Fullscreen Native / Normal Diagnostics run had **no measured FPS reading**. The lead's current display inspection reports a primary 3440x1440 display at 180 Hz and a secondary 2560x1440 display at 144 Hz. Those are current display configuration facts, not a record of the failed window's exact presentation target, scanout cadence or VRR state. Neither 144 nor 180 is a justified hard-coded target.

The strongest rejected inference is: "KSA exposes a limiter; NovaCore lacks one; therefore NovaCore over-rendered and caused the hang." None of its causal steps is established. KSA's executed limiter is not proven, NovaCore already requests FIFO, useful/displayed cadence is missing, and the freeze needs its own failure evidence. The lead's existing dump analysis may classify the failure boundary independently; this review does not override it.

## Adversarial checks

| Attack | What the evidence establishes | What it does not establish / decision |
| --- | --- | --- |
| No explicit sleep means uncapped | NovaCore has no explicit frame limiter; it requests FIFO and waits one graphics fence before host mutation. | FIFO and finite swapchain availability ordinarily provide presentation backpressure. No measured normal-session over-rendering follows from absence of sleep. **Reject a cap based on this inference.** |
| FIFO proves all presentation is correct | The creation path selects `VK_PRESENT_MODE_FIFO_KHR`; no benchmark override appears. | Requested mode is not an incident-level cadence measurement. Available modes, actual image count, driver/compositor behavior and display completion were not measured for the failed process. **Keep the gap explicit.** |
| One frame in flight means one queued image | One outstanding graphics submission is gated by one fence. Present semaphores are indexed by acquired image. | Graphics completion and presentation completion are different. More than one image may be owned by the presentation engine. Do not equate fence count, swapchain image count and presentation queue depth. |
| GPU-time reciprocal is displayed FPS | GPU timestamps measure bounded work intervals; prior performance runs measure throughput and CPU components. | They do not count actual displayed images. Present-return counts do not establish completed display either. The user's withdrawn numeric estimate must not reappear as measured evidence. |
| High fans prove waste or thermal failure | The user observed high fan speed during the incident. | No incident FPS, utilization, temperature, power, clock or throttle series proves excess work or a thermal cause. A demanding frame at useful cadence can legitimately saturate a GPU. |
| KSA's cap UI proves a production limiter | The installed KSA settings expose a VSync-dependent FPS control and write `Core.Time.FrameLimit`. The helper contains integer-millisecond sleep. | Work C found no direct production call to that helper across its bounded installed-product inspection. The UI's `Unlimited` label and the helper's positive initializer are not effective runtime limits. **Do not copy a dormant helper or assert universal absence of any limiter.** |
| KSA history proves excessive FPS caused instability | History includes misleading FPS reporting, a mailbox-to-FIFO VSync correction, high-FPS camera stutter, and image/frame synchronization changes. | Those are separate responsibilities. The records do not tie excessive FPS to this system hang. A corrected FPS display is not a display-completion probe; the camera scheduling correction is not a cap or TDR fix. |
| Copy KSA's two frame slots / three requested images | Current KSA separates frame synchronization slots from actual swapchain image resources. NovaCore already separates per-image present semaphores from its frame fence. | More live frames would change NovaCore's qualified mapped-data and publication lifetime. No latency, throughput or stability benefit is measured here. **Preserve the one-frame mutation contract.** |
| Refresh detection can use the primary monitor alone | The current primary is reported at 180 Hz. Installed KSA 5402 configures the primary monitor. | The actual window target, movement across monitors, focus/compositor mode and historical output are not joined. KSA revision 5406's later monitor-selection correction is not in installed revision 5402. **Do not borrow a capability from later history.** |
| FIFO or monitor capability proves VRR behavior | FIFO is the selected NovaCore policy; the display has a configured refresh. | Neither source audit proves effective VRR, adaptive-sync range, low-framerate compensation, driver overrides or actual scanout timing. A refresh-derived cap could interact with those policies; no such candidate is justified now. |
| Fullscreen paths are equivalent | NovaCore's inspected native path uses borderless `WS_POPUP`; KSA has explicit fullscreen and fullscreen-exclusive handling. | The UI word "fullscreen" does not establish identical presentation/compositor ownership. KSA's recoverable exclusive-mode-loss result is not equivalent to device loss or a NovaCore zoom event. |
| Benchmark flag proves an unlimited control | `--benchmark-frames` decrements a counter and requests termination. It uses the same FIFO path. | It does not bypass pacing. Prior fixed/traversal diagnostics are not automatically normal interactive cadence, and FIFO results must not be labeled an existing unlimited mode. Any future throughput mode requires an explicit, recorded policy. |
| Sleep can be inserted without behavioral consequences | Presentation pacing may be separate from simulation authority. | Sleep placement and scheduler granularity can affect input age, queue depth, frame variance and event responsiveness. KSA's integer sleep is not a validated NovaCore design. Do not insert arbitrary sleep, busy-spin, change terrain quality or clamp simulation duration to a refresh constant. |
| Source bounds clear long-run behavior | Work B identifies fixed frame resources, bounded topology/work/residency state and a pre-existing minimized-window busy loop. | Static bounds and short controls do not measure incident-level allocation trends or prove that the minimized path was entered. No busy-wait fix or general lifetime rewrite follows from this review. |
| The small cached-key diff clears candidate causality | Memory placement changes while polling, writes, fences and lifetime implementation remain unchanged. | A different legal memory type and reduced CPU cost can change driver paths or wall-clock spacing. Neither old/new short control timings nor the small diff prove the five-minute manual candidate innocent or guilty. |

## KSA responsibility decision

The source/history join is coherent when kept at the documented scope:

- **ADOPT, already present:** FIFO normal presentation and acquired-image-indexed present semaphores. These are existing alignments, not missing production corrections.
- **ADAPT only after measured need:** capability-aware presentation configuration, explicit frame/image ownership, observable failure results, and a potential user target or throughput policy. No executed KSA cap, VRR guarantee or perfect multi-monitor policy was established.
- **INTENTIONALLY DIFFER:** keep NovaCore's existing single-fence mapped-memory/publication contract and exact physical completeness. KSA's frame-slot count, retirement-age alternative and coarse representation do not license weaker NovaCore authority. Keep deterministic simulation separate from any future presentation target.

The bounded negative IL/reference search supports "effective limiter not established in the inspected current path." It does not prove reflection, other versions, external driver policy or every possible path absent. Likewise, zero-result Discord queries are search limits. History cannot fill a missing old causal diagnosis with analogy. Work C and the history report retain those limitations; no material correction to their combined conclusion is required by this review.

## Conditional next responsibility

**No new GPU run or paced candidate is required by VERIFY B.** Finish the existing failure/dump analysis first and return the unbanked incident to Project Control. A short successful cadence probe could not by itself clear the reported sustained-runtime hard hang. Do not add a default cap as a protective workaround for an unclassified failure.

If Project Control proceeds with a separate bounded diagnostic, its exact responsibility should be:

> Join normal interactive rendered/submitted/presented/displayed cadence to the actual window, monitor and swapchain identity, preserving the failed candidate's production semantics and keeping failure attribution separate.

That diagnostic should establish, before comparing any pacing mechanism:

1. Runtime/shader identity, exact normal route, diagnostic flags and environment; no fixed-camera driver, paused simulation or altered traversal silently standing in for the manual path.
2. Window-to-output identity, current refresh numerator/denominator, resolution, window style, focus, display changes, selected/available present modes, requested/actual image count and queue families. Current primary-monitor data alone is insufficient.
3. Joined monotonic frame/submission/image identities and durations for host work, fence wait, acquire, submit and present. Observe actual display completion through a supported observer if available; otherwise retain that field as unavailable. Separate GPU execution, queueing and display intervals rather than summing overlapping scopes.
4. Bounded event and resource summaries for sustained near-surface work and zoom transitions, with durable last-operation information. Keep rings/compact summaries, not bulk GPU archives. Match incident-level thermal/power or driver observations only when actually available.
5. If and only if material unnecessary work is then proven, define a distinct paced diagnostic and explicit throughput control. Record their effective modes, compare the same meaningful inputs, measure cadence and input/stutter effects, and preserve all simulation, geometry, quality and ownership contracts. Do not infer a payoff from GPU utilization alone.

The existing managed path converts elapsed host duration through the current simulation authority and its existing duration clamp (`Program.cs:216-219`). "Simulation independent" does not mean input/host sampling is identical at every display cadence. A future policy must preserve clock semantics, input processing and deterministic publication; it must not substitute `1/refresh` or move arbitrary sleeps into simulation. This ticket does not need a simulation redesign.

## Final VERIFY B disposition

**No blocker to reporting INSUFFICIENT EVIDENCE and no pacing candidate.** There is a blocker to a stronger claim that over-rendering is proved, ruled out, or responsible for the freeze. There is also a blocker to clearing M13.6 manual acceptance from the source/KSA audit or a prospective short probe alone.

The safe Project Control handoff keeps three statements separate: the failure classification from actual incident evidence; the missing presentation-cadence proof; and the already measured CPU benefit of the candidate. None substitutes for the others. M13.6 remains unbanked; no M13.7 or production presentation change follows from this review.

Created and retained by this verification: this Markdown report only. Disposable output: zero. No production source, prior evidence, build output or Git index was changed.
