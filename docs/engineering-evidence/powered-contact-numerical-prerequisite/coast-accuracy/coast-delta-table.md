> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Powered source, powered successor and coast equations

This table describes the immutable captured systems, not a newly executed world. Original capture: [coast-admission/rejected-input.json](../coast-admission/rejected-input.json). Derived equation inputs/matrices/kinematics: [controlled-witnesses.json](controlled-witnesses.json) and [convergence.json](convergence.json). Decimal values below are round-trip double representations; capture fields originating in FP32 were parsed as FP32 before widening, including omega and normals.

The successor/coast-source column is a captured physical state plus retained history. It is not itself an alternative solved coast endpoint. Historical native rounded cache/body fields are distinguished from the current diagnostic FP64 equation inputs.

| Quantity | Powered piece source/equations | Powered successor / coast source | Coast target equations |
| --- | --- | --- | --- |
| Engine force N | (0,32,0) | Producing load retained as provenance | (0,0,0); load removal already corrected |
| Engine torque N m | (0,0,0) | Unchanged | (0,0,0) |
| Gravity m/s² | (0, -9.81, 0) | Unchanged | (0, -9.81, 0) |
| Mass kg | 8.00390625 | Dry mass after powered piece: 8 | 8 |
| Inverse mass kg^-1 | 0.12493899463152758 | Refreshed coast-source native field: 0.125 | 0.125 in current equations |
| Inverse inertia kg^-1 m^-2 | (0.5, 0.5, 0.5) | Unchanged | (0.5, 0.5, 0.5) |
| Inertia kg m² | diag(2,2,2), zero off-diagonal | Unchanged | Unchanged |
| Source body position m | (-1.2228941470571186e-10, 0.49958550930023193, 9.193191985445992e-9) | (-5.701263350310626e-10, 0.49964091181755066, 7.562231729707491e-9) | Same captured successor; not a new coast endpoint |
| Source body quaternion | (3.501211820466921e-11, 3.389042518842089e-12, 1.1018538512153064e-11, 1) | (-1.5973015132786372e-9, 3.389042518842089e-12, 4.592270175240998e-10, 1) | Same captured successor |
| Source linear velocity m/s | (-1.0169570741069833e-10, 1.266525941900909e-7, 7.511074429089604e-9) | (-5.732312402528805e-8, 0.007093304768204689, -2.087629411562375e-7) | Same successor input |
| Source angular velocity rad/s | (1.503461355412128e-8, 3.675332775104734e-20, 2.035601687211397e-10) | (-4.1787228610701277e-7, -8.034676604968475e-18, 1.1474136840661231e-7) | Same successor input |
| Piece duration s | 1/128 = 0.0078125 | Cache produced by exact 1/128 | 17707/2000000 = 0.0088535 |
| Duration ratio | N/A | Retains producing h | 1.133248 |
| Omega rad/s | 188.4955596923828 | Unchanged | 188.4955596923828 |
| Twice damping ratio | 2 | Unchanged | 2 |
| Alpha = 1/[omega*h*(omega*h+2)] | 0.1955471038393578 | Provenance from producing equations | 0.16332567741181384 |
| Bias coefficient s^-1 = 1/(h+2/omega) | 54.2804784311515 | Depth refresh occurs before coast | 51.37735096539659 |
| Bias branch | min(depth/h, depth/(h+2/omega), 2): second branch active | All depths remain positive | Same equation; second branch active |
| Normal | (0, 1, 7.002423640933841e-11) | (0, 1, -3.1946030265572745e-9) | Same refreshed normal |
| Tangent basis T0 / T1 | (0, 0, -1) / (1, 0, 0) | (0, 0, -1) / (1, 0, 0) | Same refreshed basis |
| Contact feature order | (-4, -21, -5, -20) | (-4, -21, -5, -20) | Unchanged four rows |
| Friction coefficient | 0.5 | Unchanged | 0.5 |
| Tangent cap | 0.125 * sum(normal impulses) | Depends on retained / current cache | Same equation |
| Twist cap | 0.125 * sum(normal impulse_i * radius_i) | radius=sqrt(1.25) for each row | Same equation |
| Maximum recovery velocity m/s | 2 | Unchanged | 2 |
| Solver ordering | N0,N1,N2,N3; coupled tangent block; twist | No reorder | Same; eight sweeps |
| Actual producing cache / coast historical cache | (0.10504676424375983, 0.10504872380017406, 0.10505176230355139, 0.10505026063039369, 0.0000017310663727755002, -4.579949523737266e-7, 1.592006543710134e-17) | Captured exact diagnostic values, not rounded native cache | Transport + duration + load below |
| Current coast free acceleration m/s² | (0, -5.811952171791118, 0) | Previous engine acceleration recomputed at current mass: (0, -5.8100000000000005, 0) | (0, -9.81, 0) |

## Contact geometry

Offsets are already COM-referenced solver levers in the local world frame; do not rotate them a second time. Contact centers change from (0,-0.49958550930023193,0) to (0,-0.49964094161987305,0). Shape, feature identities, X/Z extent, mass model and inverse-inertia policy are unchanged by this ticket. Geometry-only controls transport impulses and preserve old depths to separate geometry from bias.

| Row | Feature | Producing lever m | Refreshed lever m | Producing depth m | Refreshed depth m |
| --- | --- | --- | --- | --- | --- |
| 0 | -4 | (-1, -0.49958550930023193, -0.5) | (-1, -0.49964094161987305, -0.5) | 0.0004144906997680664 | 0.0003590608830563724 |
| 1 | -21 | (1, -0.49958550930023193, 0.5) | (1, -0.49964094161987305, 0.5) | 0.0004144906997680664 | 0.00035905587719753385 |
| 2 | -5 | (-1, -0.49958550930023193, 0.5) | (-1, -0.49964094161987305, 0.5) | 0.0004144907579757273 | 0.0003590577107388526 |
| 3 | -20 | (1, -0.49958550930023193, -0.5) | (1, -0.49964094161987305, -0.5) | 0.0004144906415604055 | 0.00035905904951505363 |

World contact positions below are **derived** as captured body position + captured COM offset. They are not an additional collision-observer measurement.

| Row | Producing world point m (derived) | Refreshed world point m (derived) |
| --- | --- | --- |
| 0 | (-1.0000000001222895, 0, -0.499999990806808) | (-1.0000000005701264, -2.9802322387695312e-8, -0.49999999243776827) |
| 1 | (0.9999999998777106, 0, 0.500000009193192) | (0.9999999994298736, -2.9802322387695312e-8, 0.5000000075622317) |
| 2 | (-1.0000000001222895, 0, 0.500000009193192) | (-1.0000000005701264, -2.9802322387695312e-8, 0.5000000075622317) |
| 3 | (0.9999999998777106, 0, -0.499999990806808) | (0.9999999994298736, -2.9802322387695312e-8, -0.49999999243776827) |

## Derived source point kinematics

Point velocity is v + w cross r. Dot products use the actual row normal and tangent basis. The slab is static. Angular change is represented once through the point velocity; it is not added a second time as an independent source change.

| Row | Producing normal speed m/s | Successor normal speed m/s | Producing T0/T1 speeds m/s | Successor T0/T1 speeds m/s |
| --- | --- | --- | --- | --- |
| 0 | 1.339663407984304e-7 | 0.007092981090693229 | (6.404414923049683e-16, 3.1347167107060617e-18) | (-2.3161343084661477e-11, 6.361332161812436e-12) |
| 1 | 1.193388475817514e-7 | 0.007093628445716149 | (6.405149988829222e-16, 3.171470051381806e-18) | (-2.3161359154041157e-11, 6.361324127135831e-12) |
| 2 | 1.1893172724430912e-7 | 0.007093398962979336 | (6.404414923049683e-16, 3.171470051381806e-18) | (-2.3161343084661477e-11, 6.361324127135831e-12) |
| 3 | 1.3437346113587267e-7 | 0.007093210573430042 | (6.405149988829222e-16, 3.1347167107060617e-18) | (-2.3161359154041157e-11, 6.361332161812436e-12) |

The coast body Y speed is 0.007093304768204689 m/s; small angular velocities explain the four distinct normal point speeds. The nominal support bound is 0.029999400011999758 m/s. In the separately controlled body-Y-bound case, angular velocity is retained, so some point speeds exceed that bound. That arm is a sensitivity witness only, not evidence of admitted applicability at the boundary.

## Retained/cache meaning and current limits

Cache order: N0,N1,N2,N3,T0,T1,twist. Normal/tangent units N s; twist N m s.

| Stage | Seven cache values |
| --- | --- |
| Pre-powered historical preparation cache | (0.3273193836212158, 0.3273193836212158, 0.32731935381889343, 0.32731935381889343, -5.777245348781435e-10, -1.1464568183683355e-10, 7.350670073853443e-20) |
| Accepted powered output (before coast transport) | (0.10504676424375983, 0.10504872380017406, 0.10505176230355139, 0.10505026063039369, 0.0000017310663727755002, -4.579949523737266e-7, 1.592006543710134e-17) |
| Coast transported | (0.10504676418943519, 0.10504872385449872, 0.10505176234518217, 0.10505026058876292, 0.000001729694584525331, -4.579949523737266e-7, -1.4789792010570797e-15) |
| Coast duration-scaled | (0.11904403542414906, 0.11904625621066296, 0.119049699574153, 0.1190479977116944, 0.0000019601729285241626, -5.190218637876209e-7, -1.6760502216395334e-15) |
| Coast accepted load-corrected guess | (0.17593452002144055, 0.17593674080795446, 0.1759401841714445, 0.1759384823089859, 0.0000019601729285241626, -5.190218637876209e-7, -1.6760502216395334e-15) |
| Coast coupled reference | (0.1577646253393485, 0.15776277872341601, 0.15776306689014494, 0.15776433717261995, -0.0000015603720592298042, 5.763078451060426e-7, 1.669051938547916e-15) |
| Coast measured eight-sweep output | (0.15661176541413346, 0.1572657722078961, 0.15871002331461712, 0.15817977656242638, 0.0005921246978503027, -0.00012376003510150124, -1.9679095692926786e-13) |

The accepted current-mass engine-load shift is +0.056890484597291506 N s to each normal after duration scaling. It cancels the common-normal projection of the external-load change for that preparation responsibility; it does not promise cancellation of every individual row in asymmetric geometry and does not remove source kinematics or bias differences.

| Stage | Tangent magnitude | Tangent cap | Twist magnitude | Twist cap |
| --- | --- | --- | --- | --- |
| Pre-powered historical preparation cache | 5.889900428394352e-10 | 0.1636596843600273 | 7.350670073853443e-20 | 0.18297708970259013 |
| Accepted powered output (before coast transport) | 0.0000017906284269367387 | 0.052524688872234875 | 1.592006543710134e-17 | 0.05872438740767197 |
| Coast transported | 0.0000017893023031718454 | 0.052524688872234875 | 1.4789792010570797e-15 | 0.058724387407671975 |
| Coast duration-scaled | 0.0000020277232564648874 | 0.059523498615082424 | 1.6760502216395334e-15 | 0.06654929458096945 |
| Coast accepted load-corrected guess | 0.0000020277232564648874 | 0.08796874091372818 | 1.6760502216395334e-15 | 0.09835204228908159 |
| Coast coupled reference | 0.0000016633976360316946 | 0.07888185101569117 | 1.669051938547916e-15 | 0.08819259053104817 |
| Coast measured eight-sweep output | 0.0006049199980928362 | 0.07884591718738412 | 1.9679095692926786e-13 | 0.08815241528965496 |

The powered-equation P control reuses the accepted powered output as a historical guess and reruns the unchanged transport/preparation. Its same-basis arithmetic can differ at last bits from the original cache (recorded in JSON); it is not the original powered solve repeated or an oracle guess. The **actual coast replay** is bit-identical to the prior coast result.

No canonical, resource, actuator or solver-world state was modified. Reference construction verifies the current equations; it is not an admissible production initializer.
