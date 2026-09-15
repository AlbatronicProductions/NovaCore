# Game progression payoff and remaining Florida work

## What the first recommended accomplishment would enable

After the unresolved numerical prerequisite and powered-support implementation qualify:

authored spacecraft on authored slab -> engine OFF -> canonical ignite -> actual output -> finite propellant decreases -> supports unload -> off-axis torque changes support distribution -> shutdown/exhaustion -> supported coast/rest -> final copied canonical endpoint holds.

That is a generic launch-physics building block. **It does not yet qualify natural departure into the M15 free consumer.** Today no new capability exists; this is an architecture investigation.

## Remaining dependencies, grounded in current source

| Class | Remaining responsibility after supported-only slice | Source / limit |
|---|---|---|
| PHYSICS | M15-compatible free motion for the chosen asymmetric article/environment; consumer activation after physical support loss | PoweredFlightEvaluator.cs:28-35,100-110 is dry-8/spherical/no gravity; contact is 1,000 kg asymmetric |
| PHYSICS | Finite-body no-contact interval admission and future recontact acquisition/reactivation | Current LocalContactWorld checks bound/events, not a certified powered sweep; M14 Florida provider is restricted authored-point analytical contact |
| WORLD INTEGRATION | Author actual Florida pad placement, finite collision surface and initial vehicle pose in coherent Earth/local frame | FloridaFacilitySupport/terrain grading and FloridaContactProvider exist but do not constitute live BEPU finite-body terrain/pad hookup |
| WORLD INTEGRATION | Resolve finite local-frame/time/extent limits against moving/rotating Earth authority | Current contact uses fixed orientation and constant translating origin, finite slab and episode; no automatic arbitrary world rebasing |
| CONTROL | Bind actual player throttle/ignite/shutdown/axes to banked canonical command ingress | M14.22 request ownership exists; current powered scene submits authored start command; camera input is presentation-only |
| CONTROL | Appropriate attitude hardware for a steering promise beyond a fixed-engine launch | Requested modes/RCS intent are not implemented constrained SAS, gimbal or RCS actuation. Not mandatory to claim merely a simple player throttle/ignite vertical demonstration |
| PRESENTATION | Copied canonical status/pose integrated into the Florida scene and launch route | Current contact/powered development scenes are isolated; no renderer authority change is needed |
| ASSET | Explicitly authored engineering vehicle and pad geometry for the demonstration | Banked article/slab are sufficient engineering assets if deliberately bound. Final Blender spacecraft art is not a physics prerequisite |
| OPTIONAL LATER | Exhaust VFX/audio, polished craft/gear, atmosphere/aerodynamics, richer attitude control | Do not invent these as prerequisites for the first bounded vacuum/ideal-engine launch; required only when that product fidelity is claimed |

Source anchors: samples/NovaCore.Triangle/ContactDevelopmentScene.cs:43-77; PoweredFlightDevelopmentScene.cs:37-60,85-105; src/NovaCore.Simulation/Spacecraft/Contact/FloridaContactProvider.cs:48-87,121-132; Transactions/SimulationTransactionEngine.SpacecraftCommands.cs:161-191; tools/NovaCore.Launcher/ScenarioCatalog.cs.

A simple inequality T>mg is a useful centered reference, not the missing departure/consumer architecture. Florida geography cannot close these generic numerical and lifecycle gaps.
