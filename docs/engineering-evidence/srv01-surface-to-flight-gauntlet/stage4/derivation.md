# Provisional development propulsion derivation

Not final NovaCore player-construction balance. The unchanged seven-part SRV-01 dry mass is630kg. Stock initial30/45kg stores yield705kg and600N: T/W=600/(705*9.81)=0.0867539, below liftoff. Visual tank dimensions are expressly not permanent capacity authority.

Current KSA source/history gate passed before selection; see ksa-chain.md. Use existing ideal MMH-like/NTO-like species/model, exact2:3 mass demand and effective exhaust speed3072m/s. No KSA balance numbers or unimplemented atmospheric performance are imported. Reference physics: [NASA ideal rocket equation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/ideal-rocket-equation/) and [specific impulse](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/specific-impulse/).

## Independent review corrected the initial sizing

The first13.197km/s profile passed bounded software/numerical checks but did NOT satisfy the no-atmosphere complete mission budget. Its750m/s return allowance was insufficient. It was rejected before performance/promotion; retained initial-sizing-rejected.md and initial-profile-rejected.json are historical rejected inputs, not the current profile. No old result is rewritten as qualification of the revised profile.

## Accepted sizing method

Current SolAnalyticalDefinition Earth gravitational parameter398600435507022.6m3/s2, mean radius6371008.8m; reference circular altitude400km. Ideal two-impulse surface-at-rest to circular-orbit requirement8146.9252034960355m/s. Reverse ideal transfer to rest at the surface costs the same in this sizing model. No atmospheric braking or rotational launch credit is assumed. Deorbit is already included in the reverse transfer, not added twice.

| Responsibility | m/s |
|---|---:|
| Ideal ascent/insertion |8146.9252034960355|
| Ideal propulsive return including deorbit |8146.9252034960355|
| Ascent finite-burn/steering development allowance |2500|
| Return finite-burn/steering development allowance |2500|
| Orbital maneuver reserve |500|
| Terminal development reserve |750|
| Subtotal |22543.85040699207|
| Required with10% additional finite development margin |24798.23544769128|

These are explicit provisional sizing assumptions, not a simulated trajectory or precise aerodynamic losses. At the derived thrust and original wet mass, approximately209seconds of total full-power burn correspond to~2047m/s of surface-gravity acceleration*time; the two2500m/s allowances provide conservative development room without pretending this proves guidance losses. Actual ascent/return control and atmosphere remain unqualified.

Derive propellant as630*(exp(requiredDeltaV/3072)-1), round UP to the next5kg mixture unit. Result:2,018,270kg propellant, fuel807,308kg and oxidizer1,210,962kg. Wet mass2,018,900kg; mass ratio3204.6031746031745; ideal delta-v24,798.239377037527m/s. Resource capacity is explicitly provisional, finite and completely mass-accounted. It is not justified by the current visual tank or claimed as a real pressure-vessel design.

Select initial Earth-local T/W1.5 for modest positive launch acceleration, not shorter tests. Required minimum thrust is19,805,409N. Derive extent=1.5*wet*9.81/(5*3072), round UP to exact1/16kg/s. Result extent1934.125kg/s; fuel flow3868.25kg/s; oxidizer5802.375kg/s; total9670.625kg/s; thrust29,708,160N. T/W1.5000023478434603, initial net upward acceleration4.905023032344346m/s2. Isp313.25682062681955s. All numbers reproduce with derive-profile.py.

Exact full-store continuous burn duration3229232/15473 seconds (208.7010922251664s). Exhaustion generally lies inside an integer tick: rational segmentation remains authoritative. No refill or second consumption owner. Full burn expends~99.969% of wet mass, so changing mass cannot be ignored.

## Operating and qualification limits

Dry full-thrust acceleration is47,155.8095m/s2, approximately4806.9123 local g. THIS IS NOT a controlled continuous dry-out, structural suitability, real-engine response, autopilot or landing qualification. Existing ideal binary commands can express small impulses; this does not establish a flight controller. Stage4's explicit bounded objective is initial surface-gravity acceleration plus a finite coherent mission budget, not executed ascent/orbit/return. Both independent reviewers distinguished this from a product/topology STOP: thrust follows the authorized finite sizing at unchanged initial T/W, not an attempt to shorten tests.

The launch slice retains5m/s,2rad/s,100m runtime bounds. It admits only fixed-axis main/off commands, no initial angular motion, max125000ticks total and existing1..15625tick intervals. A conservative cold command-speed bound refuses high-acceleration sequences before a session exists. Existing stock gimbal/RCS and Stage2/3 remain unchanged. Interior exhaustion uses an explicit small cold fill; full-rated endpoint exhaustion at1/64s is checked in the exact resource kernel and refused as a canonical trajectory outside this slice. Full-profile long flight/control and high-mass contact integration are not claimed.

Stable configuration identity:novacore.SRV01.development-propulsion/1; engine configuration:novacore.SRV01.development-main/1. Vehicle/part/capability/store IDs remain existing SRV-01 identities. Apply only to the pinned stock digest, cold. Initial-fill variants are explicit immutable configuration inputs, never in-flight replenishment. Locate this identity to migrate/replace/split/retire once final construction balance exists. Old runtime/2 saves fail closed for this profile/environment until reconstruction has an explicit matching contract.
