# Provisional development propulsion derivation (before qualification)

Not final NovaCore player-construction balance. The unchanged seven-part SRV-01 dry mass is 630 kg. Stock initial 30/45 kg stores yield 705 kg and 600 N: T/W = 600/(705*9.81), below liftoff. Visual tank dimensions are expressly not permanent capacity authority.

Use existing ideal MMH-like/NTO-like species/model, exact 2:3 mass demand and effective exhaust speed 3072 m/s. No KSA balance numbers or unimplemented atmospheric performance are imported. Reference physics: [NASA ideal rocket equation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/ideal-rocket-equation/) and [specific impulse](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/specific-impulse/).

Current Earth gravitational parameter/radius, 400 km reference circular altitude: circular speed 7672.594 m/s; ideal two-impulse surface-at-rest to circular-orbit requirement 8146.925203 m/s. This is a sizing lower bound, not an ascent simulation. A 120 km periapsis-targeting deorbit impulse is about 81.428 m/s, not evidence of atmospheric capture.

| Sizing responsibility | m/s |
|---|---:|
| Ideal departure/ascent/insertion lower bound | 8146.925203 |
| Declared finite-burn gravity/steering development allowance | 2500 |
| Orbital maneuver allowance | 500 |
| Deorbit targeting reserve | 100 |
| Later return/landing-development allowance | 750 |
| Subtotal | 11996.925203 |
| Additional 10% finite development margin | 1199.6925203 |

The allowances are explicit sizing assumptions, not measured aerodynamic/gravity losses or a demonstrated complete return. No drag model is credited. A complete atmospheric or all-propulsive return is NOT established by this budget. Stage4 qualifies the finite profile and bounded launch acceleration, not execution of the whole mission.

Derive required propellant as `630*(exp(1.1*budget/3072)-1)`, then round UP to the next 5 kg mixture unit. Result: 45,610 kg, split fuel 18,244 kg / oxidizer 27,366 kg. Wet mass 46,240 kg, mass ratio 73.39682539682539, ideal delta-v 13,196.945461 m/s. The resource is large but finite: continuous full burn lasts roughly 205.856 seconds and loses about 98.64% of initial wet mass. It is neither an infinite-store approximation nor an unchanged-mass shortcut.

Select initial Earth-local T/W 1.5: net initial upward acceleration approximately half local gravity, allowing observable launch acceleration without extreme initial acceleration. Derive extent rate `1.5*46240*9.81/(5*3072)`, round UP to the next exact 1/16 kg/s. Extent 44.3125 kg/s; fuel flow 88.625 kg/s; oxidizer 132.9375 kg/s; total 221.5625 kg/s; thrust 680,640 N. Weight 453,614.4 N, T/W 1.5004814663, initial net upward acceleration 4.9097231834 m/s². Equivalent Isp = 3072/9.80665 seconds.

Exact full-store continuous burn duration is 145952/709 seconds. Exhaustion is not generally an integer tick; existing rational segmentation remains authoritative. No store refill or new consumption owner is permitted.

Near dry mass, continuous full thrust would produce about 1080.381 m/s² thrust acceleration (~110 local g). Stage4 does NOT qualify a controlled continuous full-thrust dry-out trajectory, structural loading, autopilot or throttle controller. Existing binary command intervals can represent small pulses, but that is not a controller claim. The bounded qualification keeps the existing 5 m/s, 2 rad/s, 100 m runtime envelope, admits only short fixed-axis development commands and retains stock gimbal/RCS behavior unchanged. More general flight/control qualification remains explicit future work.

Stable configuration identity: `novacore.SRV01.development-propulsion/1`; engine configuration `novacore.SRV01.development-main/1`. Vehicle/part/capability/store IDs remain existing SRV-01 identities. Only a cold application to the pinned stock digest creates effective immutable data; initial-fill variants are explicit configuration inputs, never in-flight replenishment. When construction balance is authoritative, locate this identity and migrate, replace, split or retire it. Old runtime/2 saves must fail closed for this profile/environment until reconstruction has an explicit matching contract.
