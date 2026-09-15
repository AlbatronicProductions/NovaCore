> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Source and accepted successor

Exact numeric values, binary32 bit strings, all eight transformed corners, contact rows, load facts, cache values and duration storage are in [rejected-input.json](rejected-input.json). Captures are before admission/invalidation. The middle `installed-powered-endpoint-before-coast-refresh` record has current endpoint/mass but deliberately still-old prestep rows; it is not used as a refreshed contact manifold.

| Fact | Powered source | Coast source after accepted powered installation/refresh | Classification |
|---|---|---|---|
| Position Y | .49958550930023193 m | .49964091181755066 m | EXPECTED PHYSICAL CHANGE |
| Quaternion X | 3.501211820466921e-11 | -1.5973015132786372e-9 | EXPECTED PHYSICAL CHANGE |
| Quaternion Z | 1.1018538512153064e-11 | 4.592270175240998e-10 | EXPECTED PHYSICAL CHANGE |
| Quaternion Y / W | 3.389042518842089e-12 / 1 | unchanged | PRESERVED |
| Linear Y velocity | 1.266525941900909e-7 m/s | .007093304768204689 m/s | EXPECTED PHYSICAL CHANGE |
| Angular X/Z velocity | 1.503461355412128e-8 / 2.035601687211397e-10 rad/s | -4.1787228610701277e-7 / 1.1474136840661231e-7 | EXPECTED PHYSICAL CHANGE |
| Actual solver normal | (0,1,7.002423640933841e-11) | (0,1,-3.1946030265572745e-9) | EXPECTED FRESH CONTACT-GEOMETRY/BASIS CHANGE; outside declared equality domain |
| Retained expected normal | original solver normal | still original solver normal | DIAGNOSTIC-ONLY DOMAIN RESTRICTION; intentionally not advanced by install |
| Ordered features | -4,-21,-5,-20 | unchanged | PRESERVED |
| Lever X/Z footprint | four corners (±1,±.5) m | unchanged | PRESERVED |
| Lever Y | -.49958550930023193 m | -.49964094161987305 m | EXPECTED REFRESHED CONTACT CHANGE |
| Contact depth range | .0004144906415604055.. .0004144907579757273 m | .00035905587719753385.. .0003590608830563724 m | EXPECTED PHYSICAL/CONTACT CHANGE |
| Normal point speed range | 1.1893172724430912e-7..1.3437346113587267e-7 m/s | .007092981090693229...007093628445716149 m/s | EXPECTED PHYSICAL CHANGE; positive separating velocity explicitly retained |
| Numerical piece mass | 8.00390625 kg midpoint | 8 kg dry | EXPECTED MASS PROGRESSION |
| Native inverse mass | .12493899464607239 kg^-1 | .125 kg^-1 | EXPECTED PRIVATE-STATE CHANGE |
| Inverse inertia | diagonal .5, offdiagonal0 | unchanged | PRESERVED |
| Normal cache sum | 1.3092774748802185 N.s | .420197510977879 N.s scaled private accepted values | EXPECTED PRIVATE-CACHE CHANGE |
| Tangent / twist cache | source solver values | accepted powered-solve values; native projections verified | EXPECTED PRIVATE-CACHE CHANGE |
| Producing duration | actual supplied binary timestep .01666666753590107 s | exact powered1/128 s | EXPECTED PROVENANCE UPDATE |
| Requested duration | exact1/128 s | exact17707/2000000 s | EXPECTED PIECE CHANGE |
| Accepted kind/piece | Preparation/0 | Powered/1 | EXPECTED PROVENANCE UPDATE |
| World/body/constraint/owner generations | 1/0/0 and generations1 | unchanged | PRESERVED |
| Canonical/frame epochs | not present in this standalone fixed-origin fixture | not present | NOT APPLICABLE |

The callback normals/offsets/depths and the subsequently extracted solver rows match in every row at both source and coast. The feature values originate in the callback, not independent solver feature telemetry. Pinned accessor source preserves the corresponding indexed mapping. No unexpected/stale geometry input is demonstrated.

The installed powered endpoint, accepted cache and failure linearization match the prior accepted run exactly. The diagnostic raw run differs from the prior raw report only in top-level checks139→140 for the additional read-only comparison assertion; normalized SHA-256 verifies this without needing the old scratch file.

`Supported` does not read duration or piece kind directly. Both old/new support accelerations use current inverse mass/inertia, as documented by the operator. Source current drive is -5.811952171791118 m/s²; successor previous powered drive is -5.8100000000000005 m/s²; successor coast drive is -9.81 m/s². Both are inward. Zero engine wrench does not mean zero gravity. The actual rejection precedes both normal-drive comparisons.

Duration does participate in the upstream bounding-box/collision refresh. The admission-only powered comparison reuses the exact coast-refreshed geometry and duration, so it isolates classification/load effects after refresh; it does not claim a separate alternate-duration collision experiment. No such experiment is needed to identify P15.
