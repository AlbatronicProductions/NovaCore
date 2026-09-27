# Horizon submission boundary — offline correction, UNBANKED

2026-09-22. **No live GPU exposure during this bounded revision. Blackout cause UNRESOLVED. Manual Player acceptance ON HOLD. Overall judgment remains REVISE; neither incident PASS outcome is claimed.**

## Finding

The original CPU singularity cannot be dismissed solely from CPU visibility estimates. Production submits the selected terrain generation for GPU preparation, culling and compaction. It does **not** reject each triangle on the CPU. Triangles retained by GPU culling reach TCS before fixed-function clipping, including triangles conservatively retained near the eye plane.

The exact original 477-triangle fixture is now permanently reproduced and reconciled:

| Pre-correction classification | Original pinned cases | Meaning |
|---|---:|---|
| 1. Rejected before GPU submission | 0 | No per-triangle CPU rejection was found at this boundary. |
| 2. Valid after production transformation | 435 | The actual FP32 transform model yields finite factors where the prior camera-basis FP64 oracle did not. |
| 3. Production-reachable invalid arithmetic | 0 | None in this exact original fixture. |
| 4. Unreachable at tessellation | 42 | Submitted for GPU compute, but rejected by the production GPU-culling source model before compaction/draw. This is **not CPU rejection before GPU submission**. |

That does not clear the neighborhood. The final Release sweep covers **876 camera/topology states and 606,961,728 triangle/state evaluations**, finding **144 pre-correction invalid triangle/state cases retained for the indirect draw**. All 144 have finite canonical prepared normals. A retained current generation is exercised through all 18 adjacent publications by the actual managed coordinator with explicitly synthetic completion acknowledgements. Settled finest-level cases also reproduce the defect; it is not confined to coarse transitions.

There are 272,163 detailed anomaly/proximity records: 173,312 class 2; 98,707 class 4 at the TCS boundary; 144 class 3 under the previous expression. After correction, every evaluated factor is finite, including the counterfactual factors of culled triangles. Counts are triangle/state occurrences, not unique patches or observed GPU faults.

## Evidence boundary

**Observed offline:** production topology, pupil-direction resolution, canonical CPU physical heights/normals, scene camera constraints, high/low body-camera encoding, production reversed-infinite projection builder, selector/coordinator ownership and source-level FP32 VS/culling/TCS arithmetic. The original fixture preserves its exact submitted high/low values; another root/body coordinate round trip shifts them by micrometres and must not replace it.

**Not observed:** this machine executing the anomalous TCS input, a captured GPU physical buffer for these poses, the original incident's last submission, or a driver fault caused by these values. Scalar CPU execution is not bitwise execution of the AMD shader. GPU floating-point contraction and physical-preparation differences remain hardware-parity limits. The defect is a demonstrated production arithmetic-domain hole for valid boundary inputs; its occurrence or consequence in the September 22 blackout is unproven.

The culling and vertex-source hashes are guarded by the regression, so a changed production shader cannot silently reuse the old model. The corrected scalar arithmetic is a shared GLSL source file compiled directly by a CPU-only native regression as well as by the actual shader compiler. This avoids relying only on a separately transcribed correction.

## Submission chain and case records

1. `SolarSystemScene.GpuConstants` supplies the encoded body camera, forward cone, viewport and FOV. `CameraRenderSnapshotBuilder` supplies the actual combined FP32 matrix. `UpdateProductionBillboard` selects/stages a topology and pupil; it does not filter individual indices for the camera.
2. Native current/incoming preparation resolves the physical vertices. Its publication/readiness contract and valid normals admit the generation. The offline replay uses the matching canonical CPU authority; it does not label a synthetic acknowledgement as GPU completion.
3. The NCSM GPU cull shader checks indices, physical finiteness and normals, then planet support, the enclosing view cone, frustum sphere and triangle support. The same rules are used for current and incoming bindings. The test models those precision boundaries, including float-encoded displacement/support radii.
4. The compact shader copies only visible triangles into the bounded index buffer. The native path issues an indexed indirect draw. VS subtracts the FP64 body camera, casts to FP32, rotates into root coordinates and applies the combined matrix. TCS computes factors before later clipping.

Every detailed record identifies case, LOD, topology hash, triangle and vertex indices, physical positions, root-relative values, clip values, edge inputs/intermediates/results, finite status, CPU disposition, preparation/compute inclusion, GPU cull disposition, draw inclusion and pre/post classification. Join `Name` to `cases.jsonl` for the full camera matrix, altitude, direction and retained pupil. [Two compact witnesses](horizon-witnesses.json) are retained with this report.

The sweep uses the pinned location, both equatorial coordinate axes and the actual north pole; altitudes 10.001, 49.999, 50, 50.001, 80, 1,000 and 1,000,000 metres; pitch 0 and ±10⁻⁷, ±10⁻⁵, ±10⁻³ radians; additional finest-level bearings at 45°, 90° and 180°. All 18 levels are checked near the ground, with settled selection and retained coarse cases at other altitudes. Main boundary transport is 3440×1440. The original unculled CPU fixture is also rerun at 2160 pixels high. These are bounded tests, not exhaustive certification of every camera state.

## Concrete failure and owner correction

Pinned-site L14 triangle 1,105,326, altitude approximately 10.001 m and pitch −10⁻⁷ rad survives the culling model. An edge has clip W values approximately 16.202316 and **0**, midpoint distance **50.28208 m**, infinite projected demand and zero fade. The old expression computes infinity × zero and passes NaN to its factor clamp.

At equatorial Z, L13 triangle 1,007,535, an edge has both W values **0**, midpoint distance **19.040373 m** and fade **0.61919254**. Its old projected and compensated calculations generate NaN while the conservative culling support still retains the triangle. Fixed-function clipping later in the pipeline cannot protect TCS arithmetic.

The correct owner is the tessellation factor evaluator, not CPU selection or a visibility/quality clamp. `production_tessellation_factor.glsl` now:

- Returns the existing factor 1 before projective division when distance fade is zero, or the edge is degenerate.
- Applies the existing factor-64 limiting result to unbounded eye-plane projected demand while refinement is active.
- Handles midpoint-depth zero, projected overflow and the exact interpolation endpoint without infinity × zero.
- Keeps normalized alignment in its geometric domain when roundoff makes it slightly greater than one.

The CPU reference receives the equivalent boundary behavior. Range 50 m, target 3 pixels, maximum factor 64, inner-factor rule, partitioning, selected density, displacement, topology, culling and physical authority are unchanged. The correction can increase refinement where the old NaN happened to collapse to a low factor on a particular implementation; that cost still needs GPU qualification.

SPIR-V's `FClamp` does not provide a portable NaN recovery rule: which operand wins can depend on implementation. Vulkan also specifies NaN outer-factor patch discard for implementations supporting NaNs. Neither statement is evidence of a blackout mechanism. [Khronos extended-instruction specification](https://registry.khronos.org/SPIR-V/specs/unified1/GLSL.std.450.html), [Vulkan tessellation specification](https://docs.vulkan.org/spec/latest/chapters/tessellation.html).

## Validation and retained artifacts

Evidence root: `E:\NovaCore\build\earth-blackout-closure`.

| Check | Result |
|---|---|
| Full Release boundary sweep, canonical normals, publication witness and original fixture | 876 states; 606,961,728 triangle/state evaluations; corrected invalid factors 0 |
| Debug focused boundary regression | 24 states; 22,460,448 triangle/state evaluations; 42 old retained invalid occurrences, corrected 0; original 477 reproduced |
| Shared production scalar source, Release replay | 816,489 recorded edge inputs; exact agreement with offline corrected result |
| Shared production scalar source, Debug replay | 51,159 recorded edge inputs; exact agreement |
| Ordinary finite arithmetic preservation | 100,000 cases unchanged in each native configuration |
| Signed-zero/subnormal/depth-sign/refinement-boundary native tests | 2,400 cases in each configuration; all finite, existing range preserved |
| Original CPU unculled fixture | 477 failures before; 0 after, at 1440 and 2160 pixels high |
| Native, application and Graphics-test builds | Debug and Release pass in isolated diagnostic outputs |
| Production TCS SPIR-V validation | Both configurations pass; identical shader binary hash |

`horizon-final-release/` holds the full final `summary.json`, `classification-summary.json`, `cases.jsonl`, `anomalies.jsonl`, `original-pinned-audit.json`, `publication-witness.json`, `shader-probes.txt` and shader source identities. `horizon-acceptance-debug/` holds the focused Debug evidence. `horizon-full-v3/` preserves the failing pre-correction run (its site label `pole` actually meant equatorial Z; the final run corrects that label and separately tests the true north pole). The initial scratch run with the stale far-view near clip is not qualification evidence.

Permanent tests: `EarthHorizonSubmissionTests`, `RunTesCpuOnly`, and `NovaCoreTerrainTessellationTests`. The focused boundary test always exercises counterexamples; `NOVACORE_HORIZON_FULL=1` enables the bounded full sweep. The native executable accepts `shader-probes.txt` to exercise the actual shared GLSL arithmetic. No renderer entry point is used by these tests.

## Remaining unknowns and stop

- No positive evidence ties this numerical defect to the original blackout. Driver/GPU/PSU/Windows attribution is unchanged.
- Hardware parity and cost of the corrected near-ground/horizon/orbit route remain unqualified. CPU finiteness and the existing factor ceiling do not establish a safe GPU workload.
- The previously retained live visibility population versus reconstructed CPU population still needs a frozen pupil/prepared-generation replay. This correction does not resolve that discrepancy.
- The earlier live horizon observer stop remains a failed stage. Its shutdown/heartbeat correction has CPU evidence but has not passed the resumed live route.
- Exact incident location, trajectory and last completed GPU operation remain unknown. Real recoverable device-fault capture and preservation through a machine-wide blackout remain untested.

`qualification-blocked.json` remains closed. No horizon/orbit/final matching GPU stage was launched during this revision. No banking, milestone promotion, driver/system change, KSA write or replacement of the ordinary manual-test executable occurred.
