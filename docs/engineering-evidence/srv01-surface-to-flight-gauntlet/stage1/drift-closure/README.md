# Stage 1 drift closure and qualification handoff

2026-09-17. **REVISE — manual acceptance pending. UNBANKED. Stage 2 NOT OPEN.**
This is a follow-up to the historically unchanged [stopped Stage 1](../README.md),
not a rewrite of its failed result. The accepted admission design is unchanged.

## What happened

One bounded causal investigation established **E. NUMERICAL REPRESENTATION CAUSE**.
One fixed native body-coordinate correction was made. Cheap affected checks passed,
then the exact previously failed 1,200-interval witness passed on its single recheck.
Remaining automated Stage 1 gates passed. Project Control must inspect the new
supported visual route, including the disclosed first-use stall, before promotion.

Baseline HEAD/main/origin/main/remote main:
`ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`.
Branch: `codex/srv01-supported-contact-admission`.
All12 stopped draft seals matched before work; all9 original Stage1 evidence
files remain unchanged. Before/after source identities and final refs are in
`identity.json`; preflight historical tags and raw seals are in `preflight.json`.

## Failed witness, vector and history

| Exact original result | Value |
|---|---:|
| Peak penetration | 0.09574638975773375 mm |
| Support | 600/600 |
| Final-window horizontal drift | 0.11947824532399198 mm — FAIL |
| Unchanged limit | 0.061 mm |

The diagnostic baseline reproduces that exact drift. Root **Y is vertical**;
horizontal axes are **X/Z**, not X/Y. From600 to1200, material-O displacement is
`(+0.00011947824532398954, 0, +2.4129427166714644e-11) m`.
All600 increments have positiveX. This is steady accumulated post-settling drift,
not opposing motion hidden by a norm or initial touchdown mistaken for drift.
The native COM gains the sameX displacement as canonicalO. The stable support
feature set persists throughout the final window; feature-order startup changes
do not explain the steady accumulation.

`causal-measurements.json` preserves exact three-arm inputs, native initial state,
authored children/tensor, witnesses at120/600/1200, raw/solver-facing contacts,
constraint handle, impulse state, extrema and raw-output hashes. Its compact JSON
format avoids retaining a bulk trace. `diagnostic-probe.cs.txt` is a cold
reproduction input only and is never compiled by NovaCore projects.

## Geometry, statics and initial conditions

Unchanged profile `srv01-fourhorn-upright-box-envelopes/1`:
`cd21bff65805e758df0c3bf5b187f93e82cc4feb2396f4e7d5a75d684a344c74`.
Unchanged design:
`ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf`.
See [original physical authorship](../geometry.md) for all8 ordered boxes /7parts.

The original assembly+X axis is rotated upright onto root+Y. MaterialO starts at0.
Slab topY=-1.7m; slab16x2x16m, centre(0,-2.7,0). Gravity(0,-9.81,0)m/s².
Mass705kg, COM=(734.4/705,0,0)m. Original full tensor retained:
diag(190.47255,731.600649113476,731.600649113476) kg m² (exact source/JSON wins).
Four corners of the main aft .639x.52x.52m envelope touch at equal height; its
footprint is X/Z=±.26m. The COM projection is strictly inside, centred in that
footprint. Other children clear the plane. No initial penetration, lateral
velocity, angular velocity, lateral gravity, or authored lateral support bias.
The physical envelopes deliberately fill curved/hollow visual regions; they are
not claimed to be exact visual triangles. No asset, primitive, ordering, placement,
physical mass/COM/inertia, material, force or solver setting changed.

## Frame / O-COM and contact audit

Independent material-O↔COM position and rotational-velocity oracles passed. Native
COM drift propagates to the same canonicalO drift, closing an export-only error.
The immutable moving-origin epoch is preserved. Canonical doubles remain authority.

The settled solver receives one convex constraint(handle0), four main-aft contacts,
stable feature IDs -517,-529,-513,-533 (raw -5,-17,-1,-21). Onlychild2 touches.
The compound coverage selector is bypassed for this single-child convex result;
it does not discard another supporting child here. Body/world/shape remain the
same, with no manifold reset. Warm-start impulses continue normally. Normal
impulses total approximately115.27N s, consistent with705*9.81*dt; tiny tangent
impulses oppose contact-point slip. No rolling constraint or damping was added.
Existing friction0.5, maximum recovery2m/s, spring frequency30Hz/damping ratio1,
8iterations/1substep and exact admitted dt are unchanged.

At600 and1200, nativeCOM velocityX≈1.19e-5m/s and angularZ≈-4.33e-6rad/s;
contact lever armY≈-2.7416244m. The actual contact-point combination
`vCOM.X - omegaZ*rY` is about-7.74e-13 and-1.36e-12m/s respectively.
Thus the constraint sees essentially stationary support while the COM translates.
The quaternionZ/W remain locked at0.7071068. Their required per-step component
change≈2.55e-8 is below half an FP32 ULP≈2.98e-8. The corresponding pose rotation
rounds away but translation advances. Ten seconds*1.19e-5m/s predicts the observed
0.119mm direction and scale. This connects representation to the physical drift;
it is not a claim that every float difference is causal.

## Controlled comparison and one correction

| Arm | Sole experimental change | Drift (mm) |
|---|---|---:|
| Original draft | None | 0.11947824532399198 |
| Negative control | Nearest canonical float quaternion, omit additional cold normalization | 0.11947673192480923 — FAIL |
| Fixed private basis | Coherent constant native-body coordinates about the same COM | 0.0001601069504782939 — PASS |

The negative control returns to the same rounded quaternion and does not cure the
drift. There was no pose search. The fixed-basis arm has bounded signed motion
(273 positive/327 negative final-window increments), retaining the same physical
support features. Ordered list permutations are not lost contact coverage.

The single production correction is restricted to `LocalContactWorld.cs` and
`LocalContactWorld.Assembly.cs`:

```
B = [0 -1 0; 1 0 0; 0 0 1]
x_native_body = B * x_assembly_about_COM
q_native = q_assembly * inverse(B)
I_native_inverse = B * I_assembly_inverse * transpose(B)
q_export = q_native * B
```

This is one cold signed-permutation convention, fixed for the entire supported
episode. Children and tensor are expressed coherently; import and export undo
each other. Upright native orientation begins atidentity so tiny support rotations
remain representable nearzero components. No adaptive reorientation, reimport,
warm-start clearing, constraints, clamps, velocity zeroing, extra friction, dt
change or solver strength change. All64 native corners and world inverse-inertia
torque responses match independent physical oracles. Original nonassembly
arithmetic paths remain unchanged. No new high-precision solver architecture.

## Mandatory KSA gate

Required: **YES**. Actual `E:\Kitten Space Agency` inspected, build
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.

| Current installed file | SHA-256 |
|---|---|
| KSA.dll | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

Direct installed IL: KinematicStates.CopyToBepu(06001B34),
ConstraintSim.PushBodyStateToSim(06000688),UpdateVehicleFromSim(0600068B),
Vehicle.CreateColliderCompound(06002F1B),VehicleProperties.CopyToBepu(06001B40),
ConstraintSim.UpdateShape(0600068E). COM-relative child/body representation,
retained state, and dirty-property-driven shape updates were read directly.

Actual authenticated live-changelog messages also read:
[revision4428](https://discord.com/channels/1260011486735241329/1260112103134724146/1506430180674113696),
[revision4440](https://discord.com/channels/1260011486735241329/1260112103134724146/1506560622605897728),
[revision5435](https://discord.com/channels/1260011486735241329/1260112103134724146/1548902968160550994),
and current revision5448 near messages1550158043986010133/1550158045143634085.
They discuss COM/bounds, quaternion order, scaled-part mass and groundclutter
mass/COM/hulls respectively. Groundclutter history is not evidence of a vehicle
fixed-basis implementation, and later changelog does not change installed identity.

**ADOPT** retained COM-body ownership and ordinary solver continuation.
**ADAPT** coherent native coordinate transport for the proven NovaCore precision
requirement. No claim that KSA uses this exact basis. No KSA source/assets copied
into production; installed files remain untouched. Current source and history
were both inspected; NovaCore summaries were not substituted for either.

## Exact 1,200-interval recheck

| Result | Value |
|---|---:|
| Peak penetration | 0.09573634067638892mm |
| Final-window max absolute minimum height | 0.07805452374398669mm |
| Maximum horizontal displacementX | 0.00015771793293595238mm |
| Same witness horizontal displacementZ | 0.000027555203172346356mm |
| Vertical displacementY | 1.0658141036401503e-14m |
| Horizontal norm / pose norm | 0.0001601069504782939mm /0.00016010695047829428mm |
| Max-drift interval | 736 |
| Final-window max speed | 3.7391090706191823e-6m/s |
| Final-window max angular speed | 2.1928465893355694e-6rad/s |
| Orientation drift | 9.418495956462632e-8rad |
| Support / contacts / feature-set changes | 600/600; min4; zero |
| Tick counts | 400x16666 +800x16667 =20000000 |
| Publications / revisions / history | 1200 /1200 /1200 |
| Timeline revision / remaining exact debt | 0 /0 |

PASS on the exact single failed-witness recheck. Later inclusion in full regression
is ordinary suite execution, not failure retry. No correction#2.

## Resumed qualification

- Native-only control versus publish/ack: all1200 native pose/velocity component
  bits identical; control canonical state unchanged. No solver reconstruction.
- Exact integer partitions30/60/150/240Hz plus40 delayed500000tick inputs match
  every prefunded reference endpoint/history; backlog drains4atmost percall.
  Original moving-frame velocity(.125,.0625,-.03125) and original epoch preserved.
  Completion overshoot17ticks stays debt. No timeline or resource change.
- Debug/Release zero allocation: complete host-credit/step/export/publish/ack/copy,
  no-work, four-step backlog, precommit refusal/retry and supported presentation:
  **0B each**. Checked1MiB helper; positive byte[128] **152B**. Independent predicates.
- Simulation conservative cold+warm managed/native upper bound3388744B Debug,
  3388168B Release; nativepool475136B includes body,shapes,solver,manifold state.
  History payload1881600B and credit payload76816B already included.
- Inclusive retained scene measurement:4431008B live managed heap delta including
  owner/history/profile, visual arrays/pins/metadata,640000B sample arrays, render
  submission and native-transfer array. Warmup returns from a NoInlining helper
  before fullGC baseline; measured objects remain rooted through collection.
  1200 completed/nonfailed workload asserted. Add nativepool475136B and actual
  Vulkan47-buffer memory requirements1363512B (46visual buffers +4416B submission):
  **6269656B**. Native23-mesh descriptors/vector bookkeeping is under1KiB; combined
  remains below8MiB with over2MiB headroom. Shared renderer device/swapchain,
  pre-existing generic meshes and driver-internal bookkeeping are excluded.
  This is owned retained storage, not process working set. `gpu-storage.cpp.txt`
  reproduces the read-only buffer-requirements query on the installed RX6800XT.
- Debug/Release full solution builds:0warnings/0errors. Full Simulation:
  **77/77 registered groups PASS in each** (92 printed PASS lines include nested
  test reports); original box/article/selector, M15.1 powered and M15.2
  assembly free-flight regressions retained. Graphics assembly/presentation,
  M14.17 certified continuation, ReferenceFrames, Precision, BEPU dependency and
  Launcher focused checks PASS in both. No renderer optimization/GPU campaign.

## Timing and cold costs

Normal GC; separate from allocation isolation. Each fresh Release process has128
warm and1024 measured complete operations. No discarded performance failure.

| Population/run | Median | P95 | P99 | Max (ms) |
|---|---:|---:|---:|---:|
| Unchanged qualification box1 | .0202 | .0232 | .0325 | .0967 |
| Unchanged qualification box2 | .0204 | .0207 | .0229 | .0264 |
| Unchanged qualification box3 | .0187 | .0192 | .0212 | .0434 |
| Supported SRV1 | .0463 | .0486 | .0519 | .0887 |
| Supported SRV2 | .0467 | .0590 | .1062 | .2276 |
| Supported SRV3 | .0461 | .0523 | .0949 | .1432 |
| Unchanged ceilings | .050 | .100 | .250 | .500 |

AllPASS. Candidate GC counts0/0/0. Worst sample indices134,713,228 differ; full
top8tails retained in `performance.json`. No repeating-index causal attribution.
Fresh candidate cold construction156.6552/172.9001/159.1119ms; first complete
physical operation50.2598/60.2308/52.2345ms. These are excluded from warm timing,
not hidden or optimized. Warm-type construction-only cost≈2ms.

| Live supported run | Display median | P95 | P99 | Maximum (ms) | Service maximum(ms) |
|---|---:|---:|---:|---:|---:|
| 1 | 5.5534 | 5.7125 | 5.8215 | 64.6243 | 63.0101 |
| 2 | 5.5554 | 5.7234 | 5.8169 | 53.8809 | 51.7013 |
| 3 | 5.5541 | 5.7283 | 5.8261 | 56.7202 | 55.1850 |

4000 nativeframes each; allcomplete1200publications with fuel30/oxidizer45/mass705.
Finite elapsed overshoot debt126/3735/3050 retained. Native renderer frame/fence,
CPU update/submit/present and GPU averages retained separately in `live-frame.json`.
Paced fence wait is synchronization, not spare compute. Render occurs once per
displayframe, never perbacklog interval. Medians/P99 meet150FPS-class context;
**maximum frames do not meet6.67ms**. Exact live maximum cause unassigned. Cold
first-use cost is measured separately but not asserted as definitive attribution.
No performance/cold-creation optimization was attempted.

## Visible route and manual gate

[Manual instructions](manual-acceptance.md), [launch](launch.ps1).
Same seven reusable parts from copied canonicalO; one existing support cube at the
physicalslab transform. OFF truth means no exhaust and unchanged resources.
READY preparation beforetime starts, boundedcatch-up, terminalcopied endpoint hold.
One cold visual-load cleanup omission found by redteam was corrected while
completing this route; it is not another physics correction. No asset/native
renderer policy changed. **Manual acceptance PENDING**; no older PASS reused.

## Failure matrix and red team

Permanent Cheap/Authority/AdditionalFailures cover inputnegative/zero/overflow,
duplicate/stale sequence, ownerthread/reentrancy, foreignauthority/world,
generation/frontier/default/oldreceipts, pendingendpoint admission, eventbefore/
attarget, exactdebt boundary, revisionoverflow, capacity/end, disposal/invalidation,
unsafeexport/precisionterminal, sourcephysical/resource/mass/COM/inertia/command/
gimbal/actuator/revision/timeline mutations, wrongconsumer, fixedrate/pausestate,
contactsave/restore refusal, genuine precommit retry, and both physical/credit
committed-private-invalidated paths. Precommit snapshots include canonical and
native authority; priorcommitted credit is never rolled back by a later refusal.

Independent read-only verifier attacked all32requested cases. No weakenedbar,
geometry/force/massretune, clamp, timestep/iterationschange, resetwarmstart,
CandidateA, authorityweaken, orpromotion/bank found. Its concrete findings were
closed: coldvisualcleanup; native-onlycomparison; inclusive storage; and storage
workloadcompletion/rooting. No productionfix beyond the fixedbasis physics
correction and minimalroute coldresourcecleanup.

Implementation-only mechanical failures remain disclosed: diagnostic Compile
override initially duplicated inputs; new test generic argument omission caused
CS0411 then corrected; first graphics slab assertion compared camera-relative
render data to absoluteposition then corrected; unchanged launcher test assumed
a five-parent repository depth, so identical binaries were run at the correct
isolated depth; an initial storage warmup local contaminated the GCbaseline, then
replaced by separate NoInlining warmup before qualified measurement. None was a
new physical/performance failure or an opportunity to relax a production contract.

## Reproduction and evidence hygiene

`reproduce.ps1 -Mode Build` prepares nativeDebug/Release thenfullsolutionoutputs.
Use `-Mode Focused -Configuration Debug` /Release for current focusedqualification,
`-Mode Full` forSimulation, and `-Mode Performance -Configuration Release` for the
three freshprocesscampaign. First failure stops. No run is launched by reading
this packet. Diagnosticmode selects only baseline/nearest-canonical/fixed-basis;
it reverses the fixedbasis in isolatedcompilerinputs for the firsttwoarms and
never edits production. Historical diagnosticfixedbasis applied the same rotation
after initialfloatprojection; production applies it beforeprojection. Both start
at exact nativeidentity and reproduced the exact same corrected trajectory.

Native prerequisites: VS18/MSVC14.51.36231, VulkanSDK1.4.357.0, .NET10.0.303.
KSA reproduction: installedassembly+methodtokens above with existing ilspycmd
11.0.0.9375 (`--disable-updatecheck -m <token> <KSA.dll>`); authenticatedhistory
links above. Do not copy KSA output into runtime/build dependencies.

Concise measurements, hashes, provenance, currenttests and cold diagnostic source
are retained. Rawlogs/builds are disposable. Exact inventory, cleanup commands,
retained manual executable and verification are in `cleanup.md`/`cleanup.json`.
The old124-file scratchroot was already absent; no deletion retry occurred.

**Judgment: REVISE.** Automated Stage1 engineering gates passed; manualgate and
review of the disclosed live/cold limitation remain. **STOP FOR PROJECT CONTROL.**
Stage2closed. No milestone, staging, commit, tag, push or banking.
