# Revised development mission: physical feasibility stop

## What happened

**ESCALATE TO PROJECT CONTROL — VEHICLE / ENGINE / MISSION PRODUCT CHOICE REQUIRED.**

The new Project Control decision removes the fully propulsive return/landing requirement from current mission sizing. The former 24.798 km/s, 2,018,900 kg profile is therefore revisited without discarding its qualified exact-resource and bounded dynamics mechanisms.

Cheap source/analytical checks found that a much smaller reference mission still exceeds SRV-01's plausible current storage envelope. No successor profile, storage geometry, density, COM or inertia was installed. No production/permanent-test changes, Stage-5 run, contact tuning or expensive requalification occurred. The predecessor and its evidence remain intact.

## Current development mission and exclusions

Use the inherited 400 km circular reference orbit. Bound meaningful maneuvering to a 400-to-500-to-400 km transfer pair and one 0.5-degree plane adjustment. Provision one deorbit-initiation impulse to an osculating perigee 1 km below mean surface; this establishes a deorbit setup budget, not safe return or landing. These are explicit finite reference choices, not claims of a uniquely optimal mission.

Exclude fully propulsive descent/landing, hypothetical drag, atmospheric braking, thermal/reentry requirements, repeated cycles and future flight-controller capability. The current profile cannot be called a minimum sufficient **qualified trajectory** without guidance/trajectory proof. A generous containment contradiction appears before that cost is necessary.

## Mission delta-v budget

Independent arithmetic uses current Earth mu `398600435507022.6 m3/s2`, mean radius `6371008.8 m`, and authored dry mass 630 kg. `derive.py` does not invoke the production mass aggregator, engine or physics solver.

| Component | Derivation | m/s |
|---|---|---:|
| Ascent/insertion | Ideal surface-at-rest transfer to inherited 400 km circular orbit; no rotation credit in nominal sizing | 8146.92520349604 |
| Maneuver reserve | Transfer pair 112.0739608924 plus 0.5-degree plane adjustment 66.9557978028; round upward | 180 |
| Deorbit initiation | 117.9720926839 to below-surface osculating perigee; round upward | 118 |
| Gravity/current-physics allowance | Conservative gravitational impulse over finite burn, solved jointly with budget | 1999.35198813534 |
| Development margin | Explicit 10% of the subtotal; retains former development-margin policy, not a future landing allowance | 1044.42771916314 |
| **Reference total** | Sum | **11488.7049107945** |

The exact physical site ground radius gives central gravity `9.82017654546559 m/s2`; mean-surface gravity `9.82022320346619 m/s2` is a conservative ascent bound. Actual effective rotating-site support acceleration differs; this sizing does not replace the frame model.

For initial T/W 1.5 and constant thrust, full burn time is `ve/(1.5*g_launch)*(1-exp(-D/ve))`. Thus gravitational-impulse allowance is at most `g_max*burnTime`. Solve `D=1.1*(ascent+maneuver+deorbit+allowance)` at ve=3072. This is not an optimized gravity turn, loss measured from a trajectory, or proof of controlled near-dry flight. Losses/reserves/margin can all be removed and the current tank still fails the more favorable reference ascent-only fit.

## Provisional performance reassessment

3072 m/s is not treated as immutable. It corresponds to 313.256820627 s. NASA's Shuttle reference identifies approximately 313 s OMS performance for MMH/N2O4; that supports the existing storable-bipropellant-like comparison, not an exact validation of NovaCore's 2:3 fixture mixture or any engine at sea level. [NASA Countdown, printed page 2](https://www3.nasa.gov/centers/kennedy/pdf/146694main_Countdown2003a.pdf).

One modest same-class sensitivity uses 330 s (3236.1945 m/s). NASA's historical chemical-technology study describes a greater-than-330-second MMH/NTO development goal, versus approximately 323 s then state of the art. Here 330 s is only a generous comparative value, not an installed or newly qualified engine, hard physical upper limit, or KSA gameplay number. [NASA study 20090028718](https://ntrs.nasa.gov/api/citations/20090028718/downloads/20090028718.pdf). Its indexed primary-source abstract was available; the direct NTRS open returned an error during this audit.

No exotic performance was selected. The current geometry/density would require approximately **396.60 s even for the nominal lossless ascent-only fit**, or **652.55 s for the same finite reference budget/constant-thrust loss allowance**. The latter is conditional on unchanged mixture density, dry mass and envelope, not a recommended engine or a universal mission Isp. Selecting a technology with materially different species/density/nozzle/energy behavior is a product decision.

## Current KSA engine and storage gate

The directly inspected installation remains `E:\Kitten Space Agency`, product `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, DLL SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. The complete current storage chain/configuration hashes and authenticated history are retained in [ksa-source-history.md](../rotating-florida-contact-closure/ksa-source-history.md).

Additional direct engine inspection:

| Responsibility | Current method token / finding |
|---|---|
| Reaction/mixture authority | `CombustorTemplate.ResolveReaction` 06002556; `MixtureReaction.AtMixtureRatio` 060028FD. Authored technology and mixture choose reaction tables. |
| Chamber conditions | `CreateConfig` 06002557; `CombustorConfig.ComputeConditions` 06001544; `FixedReactionTable.Lookup` 06002906. Pressure/efficiency yield gas properties. |
| Physical nozzle | `DeLavalNozzleTemplate.Create` 06002571. Exit/throat areas and efficiencies are explicit physical inputs; FX diameter is separate. |
| Flow/performance | `ComputeMassFlowRate` 06001574; `ComputeExhaustVelocity` 06001576; `ComputePerformance` 0600156E. Gas/nozzle/ambient state derives mass flow, exhaust and pressure effects. |
| Thrust/effective speed | `NozzlePerformance` 06001577–7A, `RocketPerformance` 0600157C. Momentum plus pressure thrust; effective speed F/mdot; Isp is derived. |
| Consumption | `Combustor.ConsumePropellant` 0600153B and controller aggregation 060015A4. One resource path and realized nozzle performance. |

Direct configuration hashes: `Content/Core/CorePropulsionAGameData.xml` = `D6A7E3EE26ADD68B8CCC572DCCD94ED24B26764C2191C919EF60A86B4924D48B`; `Content/Core/Reactions.xml` = `FEC730270EBA5E1A6DEBA169EAE79F66150AA04409EA90AC5D4D83CB1E814F61`. The latter identifies generated thermochemistry input, not a NovaCore-authorized physical table. No KSA values/assets/source were copied.

Authenticated `in:live-changelog specific impulse` search returned five records. [Revision 2905, 2025-11-24](https://discord.com/channels/1260011486735241329/1260112103134724146/1442679965236854785) describes the transition from simple thrust/Isp values to controller/core/nozzle ownership, chemical reaction, chamber-pressure effects and momentum/pressure thrust. [Revision 5342, 2026-08-21](https://discord.com/channels/1260011486735241329/1260112103134724146/1540216172379635835) removes a separate part-tooltip calculation path in favor of actual runtime part data. Source is current evidence; history explains the responsibility, not NovaCore's parameters.

**ADOPT:** explicit physical storage/engine inputs, distributed mass moments, full aggregation and retained-body property update; derived performance shared with presentation.

**ADAPT:** NovaCore's exact per-store authority, finite ideal-model scope, deterministic successor and source-mass/atomic publication. A bounded physical storage profile must specify its own geometry, density, placement and fill approximation.

**INTENTIONALLY DIFFER:** no copied KSA performance/volume balance, assets, automatic refill or atmospheric implementation. Do not import a complete chemistry/fluid/editor architecture merely for this feasibility check.

## Species/density and containment

Use externally supported reference densities only for feasibility: MMH **870 kg/m3**, N2O4 **1450 kg/m3**, both at **20 degrees C**, from [NASA-STD-8719.12A, Table 5-28, page 113](https://standards.nasa.gov/sites/default/files/standards/A/0/nasa-std-871912a.pdf). These are rounded reference values, not a new authoritative NovaCore thermodynamic state. At the existing 2:3 mass ratio, mixture specific volume is `.4/870+.6/1450 m3/kg`; effective bulk density is 1144.73684210526 kg/m3. Species remain in separate compatible compartments; they are not physically premixed by this algebra.

The authored tank envelope is 2 x 1.4 x 1.4 m, **3.92 m3 gross**. This is used only as a generous necessary containment ceiling. It is NOT promoted to a storage region or integrated for inertia. It grants every cubic metre to liquid, allowing no walls, ullage, plumbing or dry hardware. Actual usable storage would be smaller.

| Cheap comparison | Required propellant | Required liquid volume | Tank fit |
|---|---:|---:|---|
| 3072 m/s, ascent/insertion only; no loss/reserve/margin | 8304.84161190 kg | 7.25480416671 m3 | FAIL, 1.8507x gross tank |
| Same, generously credit all 407.862935178 m/s initial site speed | 7193.95780115 kg | 6.28437692974 m3 | FAIL |
| 330 s, ascent only | 7179.99602056 kg | 6.27218043175 m3 | FAIL |
| 330 s plus all site speed credited | 6255.19051974 kg | 5.46430436207 m3 | FAIL |
| Smaller complete reference budget, 3072 m/s, rounded finite stores | 25890 kg | 22.6165517241 m3 | FAIL, 5.7695x gross tank |

Giving every one of the eight collision envelopes to propellant would total only **7.05097952 m3** before subtracting overlap or any dry parts. The reference budget still needs 3.2076x that deliberately unrealistic amount. Reusing capsule/engine volume as tank space would itself change the vehicle and is not authorized. These are conditional geometric fit ceilings, not proofs against every conceivable redesigned article.

## Arithmetic-only reference profile: not installed

| Quantity | Value |
|---|---:|
| Dry / wet mass | 630 / 26520 kg |
| Fuel / oxidizer | 10356 / 15534 kg |
| Fuel / oxidizer volume | 11.9034482759 / 10.7131034483 m3 |
| Exact mixture | 2:3; upward rounding to 5 kg total units |
| Thrust | 390720 N |
| Total flow / extent | 127.1875 / 25.4375 kg/s |
| F/mdot | exactly 3072 m/s |
| Initial local weight / TWR | 260431.081986 N / 1.50028175217 |
| Full-burn duration | 203.557739558 s |
| Rounded finite ideal delta-v | 11489.0791677365 m/s |
| Dry thrust acceleration | 620.190476190 m/s2, 63.2418283706 standard g |

The old 29.7 MN was not retained. Even this smaller constant-thrust illustration produces unacceptable-to-assume near-dry control behavior; it is not knowingly promoted as a controlled development configuration. Storage already fails first. No new thrust-control mechanism, technology, topology or engine rating was implemented to rescue it.

## Storage-region physical contract and mass moments

The intended minimal future contract is the explicitly versioned, part-bound analytic storage region described in [the authority report](../rotating-florida-contact-closure/spatial-resource-authority.md). It includes independent physical interior dimensions/placement, compatible species, finite volume/density, fill law, and derived moments. It must keep exact store quantities separate if their distributions differ.

**Volume/dimensions/placement: not authored. Resource COM/inertia: not selected. Partial-fill model: not adopted.** The required liquid volume is already grossly incompatible with the present article. Assigning invisible external storage, super-density or a tensor chosen from .122 mm would violate the new decision.

KSA's fixed normalized spatial tensor scaled by remaining mass is an available bounded approximation, not a free-surface model. Whether that approximation and its drain kinematics fit a revised NovaCore product remains to be authored and qualified. Current point-removal free-flight equations cannot acquire distributed-drain qualification just by replacing their tensor.

## Contact prediction, requalification and readiness

No revised physical COM/inertia exists, so no revised contact-effective-mass prediction is legitimate. The previous prediction belongs to the predecessor point profile and remains historical. BEPU/contact parameters, geometry, gravity and .122 mm support outcome are untouched.

Stage-4 affected requalification: **NOT RUN**; no successor profile was installed. Existing bounded mechanism/checkpoint remains preserved, with its provisional mission sizing now subject to revision.

Stage-5 readiness: **NO**. Profile/volume authority fails before contact prediction, ordinary local/Florida rechecks or further qualification. No test, performance, allocation or manual campaign was repeated. Stage 6 remains CLOSED.

## Independent red team and next decision

Independent review verified mission components, loss-bound algebra, rounded exact mixture, flow/thrust consistency and volume arithmetic. It confirmed the conclusion does not rely on the 10% margin or loss estimate: more favorable ascent-only and modest-engine/spin-credit cases still exceed the present tank.

No hypothetical landing/drag budget was retained; no exotic engine, manipulated density, inferred tank geometry, arbitrary inertia, contact tuning, overwritten profile identity or new qualification claim was introduced. Required product choice concerns engine/propellant technology, mission/article suitability, or explicit vehicle/storage/staging changes. Those alternatives are identified, not selected.

**PHYSICAL-ENVELOPE COMPATIBILITY: FAIL for the current article and assessed storable-bipropellant domain.**

**ESCALATE TO PROJECT CONTROL — VEHICLE / ENGINE / MISSION PRODUCT CHOICE REQUIRED.**

UNBANKED. No milestone assigned. No Stage 6. STOP FOR PROJECT CONTROL.

Reproduce analytical results: `python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/development-profile-feasibility/derive.py`. This reads retained witness/source constants and writes only `results.json`; it does not run contact or create build output. Current Git/seal/disposable closure is shared with [the causal evidence](../rotating-florida-contact-closure/cleanup.md).
