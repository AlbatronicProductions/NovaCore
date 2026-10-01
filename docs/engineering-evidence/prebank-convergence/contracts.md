# Current contract migration decisions

2026-09-30; maintenance/convergence, no milestone assignment. Decisions below were
defined from current source and independently reviewed before executing the changes.
No production renderer/shader algorithm is changed to satisfy a stale assertion.

| Test | Protected outcome and current authority | Cause / disposition | Replacement coverage |
|---|---|---|---|
| Facility native GPU | CPU/GPU facility visibility parity, CMake test-shader producer | Harness supplies runtime directory; **MIGRATE** | Pass existing `test-shaders` output to same executable, retain every analytical/validation assertion; both configurations and runtime package verification |
| Window-route deployment prerequisite | Executing Triangle uses exact native-owned shader set/hashes | Full run exposed an old directory-glob assertion; **MIGRATE** | Read accepted generated authority with configuration/source identity/name checks, compare exact deployed membership and every hash; independent source/consumer verifier remains mandatory. All four window cases retained. |
| Production billboard default | Shared prepared terrain default, physical ground-clearance sampling | Define moved to shared header; **MIGRATE** | Assert default in `shaders/prepared_surface_contract.h`, physical GLSL inclusion and native raster inclusion/use, absence of duplicate GLSL define; retain both sampling assertions |
| Opaque handoff/order | Read-only reversed-Z orbit, color ordering and temporary background depth lifetime | First terrain draw is now a prepass; **SPLIT** | Scope terrain color draw after actual color-branch marker; retain orbit/depth checks; assert prepass before background, depth-only zero/full-extent clear before scene batches and all color draws |
| Horizon boundary | FP64 subtraction/FP32 transport, conservative culling and finite retained TES factor behavior | VS guard predates direct variant; **MIGRATE after audit** | Guard audited common vertex source plus unchanged current/incoming culls; additionally guard audited TCS/factor source; retain original bad-case witness, all numerical bounds and conservative rejection checks |

## Horizon source-model audit

`git diff 7bb03b0 40314c0 -- native/NovaCore.Native/shaders/production_spherical_billboard.vert`
shows direct-prepared compile specialization adding receiver/material varyings.
The old and current code both reconstruct the camera from high/low FP64 values,
subtract it from FP64 prepared body coordinates, convert the relative result to
FP32, rotate by the body quaternion, and apply the same combined matrix to
`gl_Position`. The operations lie outside the direct/TES preprocessor branch.
The retained variant additionally supplies the same relative position to TCS;
the direct variant does not invoke TCS. No old hash is replaced merely because
current bytes differ.

Current and incoming cull sources retain their guarded identities, physical
buffer owners and conservative precision boundaries. Those guard values are
unchanged. TCS screen-size row lengths, edge midpoint/distance/length, projected
width/height and alignment inputs correspond to the scalar model. The production
factor helper and `Corrected` both return one for faded/zero edges before unused
projective evaluation, saturate eye-plane/midpoint singularities, use the same
skew compensation endpoint and finite saturation, then the same [1,64] policy.
The deliberately uncorrected `Factor` fields retain the historical failure witness.
The corrected result is still required finite for every included tested edge.

The VS, TCS and factor helper now have fail-closed source identity guards. This
is source-model correspondence, not bitwise GPU execution parity. The default
bounded horizon cases remain the gate; no exhaustive horizon campaign is added.
`NovaCorePreparedSurfaceRasterTests` separately protects current native direct/TES
eligibility: NCSM1, physical generation, production cube, diagnostic/raster flags
and finite nonnegative uploaded factor demand. Negative factor/diagnostic probes
retain their TES owner. No fallback is removed.

## Qualification mode

Full managed Graphics uses the documented
`NOVACORE_P2S5F_ARTIFACT_INPUT=E:\NovaCore\assets\planetary-nested-scale-mesh`.
This exercises all 18 immutable fixture scales and current topology/serialization/
corruption/cull contracts. It does not claim regeneration or exhaustive regenerated
byte determinism. Neither exhaustive environment switch is enabled.

Before-change causal witnesses are retained under `build/prebank-convergence/before-*.log`.
Historical failed/interrupted/NOT RUN reports remain unchanged. Final receipts
will separately identify completed gates and any remaining blocker.
