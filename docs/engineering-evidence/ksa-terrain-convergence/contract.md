# Candidate contract and Gate 3 bar

This is an unbanked architectural candidate. The development default selects
prepared render terrain; the banked path remains selectable with the temporary
compile-time `NOVACORE_PREPARED_RENDER_TERRAIN=0` comparison bridge. Main and
both milestone tags are unchanged.

Gameplay queries retain camera-independent generation-4 full terrain height,
including global, regional, procedural near relief and facility support. The
candidate prepares that height and normal at each current/incoming mesh vertex.
Rendering owns piecewise interpolation of those prepared vertices in the current
camera-relative FP32 domain. TES adds no geometric material displacement in this
slice; existing fragment detail remains. Exact identity at every interpolated
sample is deliberately not the new contract.

Shared inputs are production geography, physical data, modifier definitions and
facility support. Query authority remains high precision and deterministic.
Preparation and render publication retain the sole NCSM1 owner and readiness
fences. Render tessellation and gameplay queries are display/simulation-owned
respectively; simulation warp does not schedule preparation per simulated tick.

Florida's existing contact bar remains: less than 1 cm actual support-plane to
rendered-triangle gap across the nine foundation core points, including moving
L16/L17 returns. At those Florida contact triangles, prepared vertex versus
physical query error must stay below 3 mm, matching the existing test's scope.
This is not a planetwide GPU texture-sampling tolerance. These are physical
outcomes, not protected base-only implementation tests.

Ordinary terrain is measured at actual GPU TES positions plus endpoints and
interiors of every selected prepared triangle. Signed error means rendered
radius minus the gameplay full-H radius. Report both penetration and separation,
including worst location/triangle and 0–50 m, 50–100 m, 100–500 m and far buckets.
There is no existing universal centimetre tolerance in KSA or a current NovaCore
spacecraft contact solver that establishes one. A deviation of 10 cm or more
within 50 m is a provisional Gate 3 stop criterion for this zero-render-detail
slice, not a newly accepted production tolerance. Smaller deviations still
require the full visual/contact review; they are not automatically acceptable.
Florida's stricter demonstrated 1 cm rule is not relaxed.

Do not use KSA's material displacement amplitudes or 2 m collider spacing as a
proof of a total contact-error bound. No coordinate intermediate bit identity,
old H-in-TES source assertion, or old camera fade vetoes this migration.

Gate 3 must also show startup/Florida Vulkan success and a meaningful GPU signal
(prefer at least 3 ms active gain). Captured/instrumented shader runs are excluded
from timing comparisons. Full traversal, five-pose timing, publication, launcher
and adversarial tests run only after these cheap gates pass. A Gate 3 failure is
reported with concrete evidence and a bounded revision; it is not hidden by
relaxing thresholds or running a large unrelated validation suite.
