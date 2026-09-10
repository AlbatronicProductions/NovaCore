# Exact-event spacecraft motion

**M14.7 — Evaluate coherent spacecraft motion at exact physical-event epochs — BANKED.**
Annotated tag: `m14.7-exact-event-spacecraft-motion`. M14 remains open. See the
[latest qualification](engineering-evidence/timed-measurement-split/README.md).

## Ownership and evaluation

`SpacecraftPhysicalEventMotionEvaluator` is an internal, pure query using a fresh
`SimulationStateView` during the existing simulation single-writer phase. The view
is not a deep snapshot or concurrency lock. The evaluator copies immutable linear
and angular segments and source revision before numerical work and publishes only
a complete return value. Missing subjects, pre-segment requests and bounded
numerical refusals produce established status/default outputs.

Canonical `PhysicalEventEpoch` requests delegate directly to banked
`SpacecraftMotionEvaluator`. Existing canonical translation, attitude, rotation,
coherent-motion bodies and their callers are unchanged. They acquire no dependency
on the new sidecars.

Fractional translation uses exact difference from the stored canonical authority
epoch, then one bounded local FP64 conversion and the banked constant-root-force
equations. Direct evaluation avoids rounding an intermediate floor position.
`PhysicalEventDuration` uses Int128 subtraction and exact unsigned magnitude and
fraction, bounded to the signed Int64 tick span. There is no production BigInteger,
absolute-epoch double conversion, saturation, epsilon snapping or cache.

Fractional rotation first invokes banked rotation at the requested canonical floor.
Only the positive fractional tick is evaluated by a new RK4 helper with the same
principal-inertia, constant-body-torque and quaternion equations. Spherical
torque-free motion retains constant-rate analytical dynamics. Exact duration bounds
are checked before FP64 conversion; nonspherical work reserves one additional step
within the existing cap. This intentionally repeats a small set of equations in
the sidecar rather than generalizing the banked integrator. Negative-floor standalone
rotation can use banked backward evaluation followed by the positive remainder;
coherent motion rejects requests before either stored authority epoch.

## Result and limitations

`SpacecraftPhysicalEventMotion` is an immutable, reference-free transient value:
spacecraft identity, exact event epoch, source revision, root frame, FP64 root
position/velocity, body-to-root orientation, body angular velocity, physical
properties and principal inertia. Revision is provenance, not permission to mutate.
Retaining the result does not retain a live view or prevent later canonical changes.

Exact time identity does **not** imply mathematically exact physical values,
represented-zero terrain gap, a certified contact/root, or M14.5 admission.
The sidecar adds no scheduling, execution frontier, physical response, publication,
remaining-interval execution, grounding or solver. Rendering is not an authority.

## Qualification status

Focused numerical, canonical-bit, refusal, replay, immutability and zero-allocation
tests pass in Debug and Release. All 327 banked production files retain their
starting hashes. Early normal Release passed 3/3; full Debug passed 38 groups.
The original final Release run 1 stopped at the lunar-orientation counter gate:
12,336 bytes. Its mechanism remains unclassified. A separately authorized test-only
split now times the five equivalent pure workloads under normal runtime behavior
and measures allocation independently under the existing checked helper. Focused
Debug/Release qualification passed, followed by a fresh **5/5 Release matrix** and
bounded candidate performance with zero warmed allocations. All 332 production
fingerprints remain unchanged. Median sidecar/wrapper costs in the retained fixtures
range from 0.64 to 2.03 microseconds per evaluation; report full distributions,
including the 48.26-microsecond canonical-wrapper P99 batch-average sample. No
further performance or contact capability is implied by M14.7 banking.
