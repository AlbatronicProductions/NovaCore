# Current KSA convergence — 2026-09-27

Read-only installed inspection and authenticated live-changelog review; no KSA
writes, gameplay, proprietary source or assets copied. Installed version
2026.9.22.5482, KSA.dll SHA256
`CE43D022E7DAC9BEB2352B6106164BC1F521DDA1AE9D0B898B8DBA0C59A69723`;
BEPU SHA256 `77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`.
Two independent readers inspected selected methods with the existing IL inspection tool.

| Responsibility | Installed mechanism / method token | NovaCore disposition |
|---|---|---|
| Clock | `Universe.GetJobSimStep` 06002FC1 derives dt from player time, achieved speed and simulation speed | ADAPT: preserve qualified exact 64 Hz and deterministic debt. This is not evidence of a KSA fixed64 Hz clock. |
| Publication/render | `PrepareFrame` 06001E50 waits for prior jobs, applies results, starts successor jobs; `OnFrame` renders separately. ApplyVehicleSolvers 06002F91 → task application 060032B3 → vehicle state 060030CB → cached per-frame pose 060030DD → render matrices 06003109 | ADOPT complete publication and once-per-display rendering; ADAPT existing exact transaction owner. No interpolation bridge established in this examined path; no new one required. |
| Active contact | FullPhysicsStepUntil 06001CEE chooses constrained/unconstrained integration from IsAnyConstrained, updated by actual awake constraints (060006C7); predicted bounds/collision detection 060006C2 | ADOPT distinction between a world existing and contact work being necessary. |
| Safe continuation | CertifyTerrainClearance 06001CF1; PairFreeFlight.ComputeApproach 06001947 derives a conservative time to collision from separation, velocity and acceleration; terrain anchor 06000683 predicts approach | ADOPT full-interval risk certificate, including possible thrust/RCS. ADAPT exact terrain H, FP64 site frames and64 Hz. |
| Lifetime | Existing constraint world reused (06001D12); unused world returned after30 simulated seconds (06001CD2) | ADAPT: NovaCore native receipts are tied to exact published frontier and cannot survive another integrator. Retire native solver after certified handoff, retain immutable terrain certificates in the profile. Do not copy an unexplained30 s policy. |
| Terrain reuse | Radius cache 06002CD0; dirty/version early return 060006BB; keyed blocks rebuilt only when absent/basis changed 06002CAC | ADOPT immutable keyed facts and cache reuse. Native approximation modes are not imported. |
| Contact cadence | Constrained slices up to1/60 (06001D05), eight solver iterations/one substep in060006AA | ADAPT existing64 Hz and already-qualified native subdivision; no contact retuning. |

The existing NovaCore render callback already services bounded debt and publishes
once, then performs camera/terrain/render preparation once per display frame.
There is no evidence of rendering once per native slice. Moving work to a growing
queue or slowing simulation is not adopted from KSA's overload control.

Authenticated history (observed through the signed-in Discord UI):

- [4022](https://discord.com/channels/1260011486735241329/1260112103134724146/1491645466851545119): frame averaging reverted; fixed physics stepping/interpolation described as an intended solution. Intent does not override current installed methods.
- [4549](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020): resource/derivative work moved out of repeated small collision steps.
- [4866](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037): predictive collision prepass avoids spurious speculative contacts and severe hitches.
- [5122](https://discord.com/channels/1260011486735241329/1260112103134724146/1533620555465494589): an impact crossed within one physics interval must wake ordinary physics.
- [5217 jobs](https://discord.com/channels/1260011486735241329/1260112103134724146/1535554540617859124) and [time](https://discord.com/channels/1260011486735241329/1260112103134724146/1535554538684289055): per-vehicle unconstrained/module jobs and wider exact time ownership.
- [5448](https://discord.com/channels/1260011486735241329/1260112103134724146/1550158045143634085): shared terrain samples avoid duplicate clutter preparation. This supports the reuse responsibility, not copying clutter policy.
- [5501](https://discord.com/channels/1260011486735241329/1260112103134724146/1552880853217050777), newer than installed5482: interaction prediction must include possible next-step thrust/attitude action. NovaCore's certificate bounds all admitted actuators, including currently inactive ones.

Latest visible channel messages reached5504; subsequent visible5502–5504 concern
interstellar RCS, shadows and clutter rendering, not replacement of these owners.
This is a scoped supersession check, not a claim to inspect every KSA change.
Responsibility is established; research stopped.
