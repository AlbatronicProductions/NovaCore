# Stage 4 — finite provisional development propulsion

**PASS — PROMOTE (preserved Stage-4 checkpoint). UNBANKED. No milestone assigned.**

The downstream Stage-5 attempt subsequently failed settled-support qualification. Its shared source edits are an **unqualified draft**, not the Stage-4 tested tree. See [Stage 5](../stage5/README.md). Stage-4 source identities are in [identity.json](identity.json); overlapping checkpoint bytes are preserved in `../stage5/pre-stage5-overlap.zip`. Do not rerun `collect-results.py` against that later draft and overwrite this historical result.

## What happened / why it matters

Derived and implemented one immutable, explicitly provisional development profile on the existing canonical seven-part SRV-01. The profile supplies finite, mass-accounted resources and a coherent ideal thrust/mass-flow law sufficient for the calculated development mission. It does not introduce another spacecraft, engine, staging, refill owner, final tank balance or pressure-vessel claim.

Baseline HEAD/main/origin/main/remote main: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`. Branch: `codex/srv01-supported-contact-admission`. All 67 historical tags unchanged; nothing staged, committed or pushed. M15.2 remains the banked production accomplishment.

## Current KSA convergence

Direct installation: `E:\Kitten Space Agency`, build `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`. KSA.dll SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`.

The complete [source/ownership/history map](ksa-chain.md) covers authored engine and tank identity; geometry and capacity; species/density; feeds/availability; mixture; engine activation/request; configuration-specific gas/nozzle performance; momentum and pressure thrust; ambient response; mass flow; live finite withdrawal; exhaustion; dry behavior; mass/COM/full inertia; retained body update; ready/apply; save identity; staging/composition. Direct authenticated live-changelog review covered all returned pages for the relevant searches. Newer posts are distinguished from installed source.

**ADOPT:** finite species-aware containers, coherent engine performance, explicit feeds, resource mass in physical properties, retained vehicle/body ownership, prepared/ready/apply lifecycle. **ADAPT:** exact two-store resources, rational exhaustion, material-origin mass/COM/inertia, deterministic time/revisions/atomic publication and a versioned cold development overlay. **INTENTIONALLY DIFFER:** the current KSA live consumer discards partial-withdrawal results and clamps species separately; NovaCore retains exact balanced mixture/exhaustion. No implicit refill, general chemistry, tank-transfer system, visual geometry or KSA balance values were copied. The nozzle model is explicitly NovaCore's current ideal effective-exhaust-speed law, not an atmospheric nozzle simulation.

## Profile and physical derivation

Stock SRV-01 remains 630 kg dry, 705 kg initially wet, 600 N main thrust, T/W about 0.08675. It cannot lift off on Earth. Stock definitions/pins and main/gimbal/RCS qualification are unchanged.

| Current provisional property | Value |
|---|---:|
| Profile identity | `novacore.SRV01.development-propulsion/1` |
| Engine configuration | `novacore.SRV01.development-main/1` |
| Dry mass | 630 kg |
| Fuel capacity/initial full fill | 807,308 kg |
| Oxidizer capacity/initial full fill | 1,210,962 kg |
| Total propellant | 2,018,270 kg |
| Wet mass | 2,018,900 kg |
| Mass ratio | 3204.6031746031745 |
| Effective exhaust speed | 3072 m/s |
| Isp equivalent | 313.25682062681955 s |
| Surface weight / strict liftoff threshold | 19,805,409 N |
| Main thrust | 29,708,160 N |
| Initial T/W | 1.5000023478434603 |
| Initial net upward acceleration | 4.905023032344346 m/s² |
| Mixture | fuel:oxidizer = 2:3 by mass |
| Extent / total flow | 1934.125 / 9670.625 kg/s |
| Fuel / oxidizer flow | 3868.25 / 5802.375 kg/s |
| Full-store burn duration | exactly 3229232/15473 s = 208.7010922251664 s |
| Finite ideal delta-v | 24,798.239377037527 m/s |

[derivation.md](derivation.md), [mission-values.json](mission-values.json) and [derive-profile.py](derive-profile.py) retain the independent mission calculation. A 400 km reference orbit requires 8146.9252034960355 m/s ideal ascent/insertion and the same ideal propulsive return. No atmospheric braking or rotational launch credit is assumed. Explicit finite-burn/steering, maneuver and terminal-development allowances plus 10% margin yield 24798.23544769128 m/s required. Resource quantity is rounded upward to an exact 5 kg mixture unit; flow is rounded upward to 1/16 kg/s at initial T/W 1.5. These are provisional sizing assumptions, not an executed ascent/return trajectory.

An earlier 13.197 km/s sizing was rejected: its return allowance did not fund a no-atmosphere return. [The rejected rationale](initial-sizing-rejected.md) and its input JSON remain historical, not current qualification.

**Material limitation:** dry full-thrust acceleration would be approximately **47,155.81 m/s² (4806.91 local g)**. No controlled full burn to dryout, structural suitability, throttle controller, real-engine response, orbital flight, landing, or high-mass contact qualification is claimed. The mass/thrust follows the authorized finite mission sizing at modest initial T/W; it was not selected to shorten tests. This is **not final NovaCore player-construction balance**.

## Implementation and authority

The pinned embedded production JSON is applied only to the exact existing stock design, cold. Capacities, initial fill, species, engine configuration and provisional status are explicit. Initial-fill variants are immutable and cannot address/refill a live owner. Configuration digest differs; canonical vehicle, part, capability and store identity remain continuous. Existing source guards reject implicit profile use and old runtime/2 save reconstruction.

Stored propellant contributes to exact total mass. With the existing point stores at material O, fixed first moment and origin inertia give `COM=S/M` and `I_COM=I_O−parallel(S)/M`. Independent full-tensor samples and endpoint bounds cover dry through full wet mass; 1116-bit exact mass fits existing 2176-bit storage. No new resource arithmetic or physics integration family was introduced.

The qualified launch slice is fixed-axis main/off under Earth-local 9.81 m/s² gravity, at most 125,000 ticks total, original 1..15,625-tick intervals, zero initial angular motion, and existing 5 m/s / 2 rad/s / 100 m envelope. A cold speed bound refuses excessive commands. This is the ticket's bounded positive-acceleration objective, not a full-flight controller.

## Validation

[validation.json](validation.json) preserves exact witnesses and execution notes.

- Full Debug and Release solution builds: **0 warnings, 0 errors**.
- Full Simulation: **80/80** registered groups in each configuration.
- Development focused tests: deterministic identity, exact stores, mass/tensor, independent thrust/impulse, exact resource oracle, analytical variable-mass trajectory, exhaustion/dry, invalid-profile/save/domain refusals: PASS.
- Eight full-wet powered intervals under gravity: final upward speed **0.6136787680578174 m/s**, position **0.03834344385779613 m**; maximum analytical velocity error **3.1019631308026874e-13 m/s**, position error **6.471384712825046e-11 m**.
- Five equal-total-time host partitions: exact endpoint bits and stores at every matching frontier; no second owner.
- Interior exhaustion: explicit small cold fill, rational powered duration **1/123784 s** in a 10 µs command, then dry requested-on/off. Full-rated 1/64 s dryout is checked by the resource kernel and refused as a canonical trajectory outside the declared motion envelope.
- Warm powered / interior-exhaustion / dry: **0 / 0 / 0 managed bytes**, checked no-GC entry/exit PASS. Deliberate control: **152 bytes**, Debug and Release.
- Stage 2 powered support and Stage 3 handoff focused gates PASS in both configurations. Existing 600/600 support, original moving epoch, exact publication/debt and one-consumer boundaries remain protected.
- ReferenceFrames, Precision, verified BEPU dependency, Launcher, and affected presentation/M14.17 routes: PASS in both configurations.

The first Launcher invocation used an unsupported deeper output layout; the same binaries passed from the documented five-parent layout without source edits. An inadvertently started full Graphics default runner was interrupted and is not counted. Required focused presentation routes passed. Neither incident is hidden as completed qualification.

## Performance

Three fresh Release processes; each population has 128 warm operations and 1024 measured complete host-admission/preparation/commit/ack/observation operations. Cold session preparation is excluded from warm timing. No renderer change or whole-frame claim. Exact rows and largest-sample indices are retained in [measurements.json](measurements.json).

| Process | Population | Median ms | P95 ms | P99 ms | Max ms |
|---:|---|---:|---:|---:|---:|
| 1 | Wet powered | .0859 | .0901 | .103 | 1.6261 |
| 1 | Interior exhaustion | .1586 | .1621 | .1682 | .1883 |
| 1 | Dry | .0502 | .0524 | .0539 | .0616 |
| 2 | Wet powered | .08615 | .0973 | .1346 | .1456 |
| 2 | Interior exhaustion | .1516 | .1632 | .1776 | 1.668 |
| 2 | Dry | .0186 | .023 | .0242 | .0457 |
| 3 | Wet powered | .0857 | .0885 | .0995 | .1872 |
| 3 | Interior exhaustion | .1515 | .159 | .1796 | 1.5849 |
| 3 | Dry | .0203 | .0215 | .0277 | .0604 |

All timed-region GC counts are `[0,0,0]`. Wet first-session preparation: 16.0333 / 17.0575 / 17.3947 ms, excluding already-loaded catalog/profile. Whole-harness CPU (which includes cold setup/disposal) and other cold values are in the JSON. Repeated late exhaustion tails in processes 2/3 and the wet process-1 tail remain **CAUSE UNATTRIBUTED**; no profiling, retries or optimization followed. These headless operations do not establish 150-FPS Florida performance. Historical 50 µs micro-gates were not reapplied as invented Stage-4 gates.

The separate white-window / first-present residual (16670.3851 ms in retained prior evidence) and late callbacks remain disclosed and were not reopened.

## Red team and promotion

Independent source/mission reviewers attacked all 25 ticket points. The return-budget issue was corrected before final-profile qualification. Final profile checks and source boundary review passed. The reviewers accepted the bounded Stage-4 objective and explicitly withheld claims of long flight/control and high-mass supported contact.

**Stage 4: PASS — PROMOTE.** The autonomous campaign then opened the objective Stage-5 physical/site gate. Stage 5 failed settled support and is stopped; Stage 6 remains closed. No milestone or bank follows from either result.

Reproduction: [reproduction.md](reproduction.md). Cleanup: [campaign cleanup](../stage5/cleanup.md).
