# Direct current-KSA site/contact gate

Inspected 2026-09-17 from `E:\Kitten Space Agency`, installed build `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`. KSA.dll SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. The same installation was directly inspected for [Stage 4](../stage4/ksa-chain.md). Source below means direct decompilation of that installed binary, not prior NovaCore summaries. Tokens permit precise reinspection without copying proprietary source into this evidence.

| Responsibility | Installed owner / method token | Direct finding | NovaCore disposition |
|---|---|---|---|
| Site coordinates and initial support | LocationReference 06000349/0600034F; Vehicle initialization 06002F11/06002F82 | Latitude/longitude to CCF; geometry/terrain placement; initial landed state and rotating-surface velocity/angular rate | ADAPT authoritative physical site and complete frame state; initial placement alone cannot certify settled support |
| Native frame lifetime | CCF bubble 06001B67; origin 06001B2A | CCF membership inside physics radius; origin retains body-fixed position while epoch advances | ADAPT immutable site binding and exact original epoch; no per-frame recreation |
| Frame transport | CCF/CCI 060030E4..060030E7; orientation/rates 06001B74/06001B78 | Position/velocity transform includes rotation cross position; angular transport includes frame rate | ADOPT full physical frame transport; ADAPT NovaCore FP64/typed canonical authority |
| Terrain physical owner | 06000662/63/64; initialize 06000651; update 0600064D; static upload 0600068F; grid 06000695; local origin 06000689 | Retained physical terrain samples/static upload in bubble coordinates | ADAPT current NovaCore terrain-v5 grading proof, exact dataset identity and bounded patch; no renderer-derived ground |
| Facility collider distinction | 06000691/92; authored collider 06002854 | Authored pad/static collider is a different owner from underlying terrain | ADOPT distinction; use existing graded ground outside the platform, no invented pad top |
| Rotating-frame forces | ComputeDerivatives 06001B8F | Computes centrifugal acceleration; when HasAnyContact it replaces direction by radial magnitude and omits Coriolis; otherwise includes -2 omega cross velocity | INTENTIONALLY DIFFER: preserve the full frame-equation terms and actual tangential gravity. KSA simplification is not a NovaCore physical-accuracy proof |
| Retained solver/apply | body 06000686; 0600068A; constrained solve 06001C19; readback 0600068D/8B; export 06001C32; ready/apply 06001BF8/1BEA/2F31 | Same retained body across solve/readback and copied canonical apply | ADOPT ownership/lifecycle; ADAPT exact NovaCore revisions/resources/atomic acknowledgement |
| Contact and release | collision 0600069C/6A1; constrained decision 06001C02; unconstrained 06001C1B; removal 06001BEF | Contact affects solver choice; body removal is a parent/frame mismatch responsibility, not simply no-support | ADOPT separate contact fact from ownership. Stage-5 engine-off slice does not generalize or qualify Stage-3 departure into a rotating site |

NovaCore-specific review also found that BEPU's existing `ConserveMomentumWithGyroscopicTorque` uses native angular velocity. A rotating-native design must distinguish absolute from relative rates. The draft adds the derived difference term; that choice remains unqualified after the support failure. Another reviewer proposed a nonrotating patch plus moving collider as an alternative. Neither alternative is proven a correction for the observed failure, and no architecture switch followed the stop.

## Authenticated live history actually inspected

Source: `https://discord.com/channels/1260011486735241329/1260112103134724146`, live-changelog, through the user's authenticated browser. Searches: bubble 25 results (all), fictitious 1 (all), Coriolis 0; terrain 194 results **first page only**. This is a bounded relevant-history inspection, not a claim to have read all 194 terrain results. Stage-4 resource/propulsion history coverage is recorded separately.

| Revision / date | Relevant engineering history | Current-source reconciliation |
|---|---|---|
| 4395 / 2026-05-15 | Complete kinematics per frame and conversion errors | Full position/velocity/orientation/rate chain inspected above |
| 4492 / 2026-05-27 | BubbleOrigin gravitation helper | Physical frame/environment ownership is separate from render camera |
| 4659 / 2026-06-18 | Collision before timestep; avoid low-precision overwrite | Retained solve/readback boundary inspected; NovaCore exact authority preserved |
| 5062 / 2026-07-28 | Origin snap/docking jump | Immutable original episode/frame origin required |
| 5276 / 2026-08-12 | Clutter collider owner follows bubble | Separate physical static ownership; no presentation authority |
| 5327 / 2026-08-19 | Float-derived terrain anchors caused about +/-0.4 m error; double anchor fix | NovaCore retains double authority before bounded local reduction |
| 5338 / 2026-08-20 | Ground flattening/planar decals distinct from pad | Current NovaCore ground/pad distinction retained |
| 5341 / 2026-08-20 | VehicleUpdateTask ownership | Prepared/ready/apply chain inspected |
| 5366 / 2026-08-26 | CPU terrain independent of renderer; mean-radius fallback | ADOPT physical/render separation; INTENTIONALLY DIFFER by failing closed on missing authoritative NovaCore data |
| 5421 / 2026-09-09 | Bubble merge/split during frame | Lifetime/invalidations inspected; no pooling/rebasing system copied |
| 5429 / 2026-09-10 | Missing fictitious forces for CCF bubble above physics radius | Direct 06001B8F inspection controls the installed behavior and exposes its contact-conditioned simplification |
| 5448 / 2026-09-17 | Clutter corrections | Newer history than installed 5438; not asserted as installed implementation |

This gate supported opening the objective site integration; it did not establish the completed draft's physical correctness. The subsequent failed settled-support evidence overrides any preliminary architectural confidence. No KSA source, assets, proprietary geometry or gameplay constants are candidate build inputs.
