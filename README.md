# NovaCore

**A custom precision space-simulation and game-engine framework built for seamless travel from planetary surfaces to orbit and beyond.**

NovaCore combines planetary rendering and high-precision spacecraft simulation in a
custom C# framework with a native Vulkan renderer. Built independently of Unity and
other general-purpose engines, it owns its world coordinates, simulation time, physical
state, and rendering architecture. Surface interaction is advancing through qualified
engineering foundations; the complete spaceflight experience is still being built.

**Latest banked milestone: M15.0 — Segmented Powered Free Flight.**
NovaCore now supports bounded canonical powered spacecraft free flight with finite propellant, changing mass, exact exhaustion-to-coast segmentation, and atomic physical/resource/actuator publication. Powered retained contact, liftoff, Florida launch integration, gimbal/RCS, attitude-control hardware, and production spacecraft art remain under active development.

![NovaCore Earth against the Milky Way](docs/images/novacore-11a-earth-milky-way.png)

<p align="center">
  <img src="docs/images/novacore-11a-earth-moon.png" alt="NovaCore Earth and Moon presentation" width="49%">
  <img src="docs/images/novacore-11a-earth-sun.png" alt="NovaCore Earth, Moon, and Sun presentation" width="49%">
</p>
<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="NovaCore Mars presentation" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="NovaCore Solar overview at maximum time warp" width="49%">
</p>

*Earlier development captures: Earth, Moon, Sun, Mars, and Solar System time warp.
These images predate the current terrain and contact work; they do not demonstrate
completed atmosphere or cloud systems.*

## What Works Today

### Planetary Rendering

A native Vulkan renderer supports continuous camera travel from orbit to Earth's near
surface, with real NASA/NOAA imagery and elevation data and optional Florida regional
detail. Solar System views include planets and moons, Sun-driven day/night lighting,
Saturn's rings, and an interactive orbital map. Terrain data is verified and prepared
through a dedicated asset pipeline.

### Precision Simulation

High-precision world coordinates connect astronomical and local scales. Deterministic
simulation tracks celestial motion and exact simulation time, with Solar System time
warp from 0.1× to 7,776,000×. Camera movement and rendering detail do not change physical
terrain or simulated state.

### Spacecraft Physics

Spacecraft have independent translation and rigid-body rotation, with high-precision
position and velocity, force and torque handling, and coupled linear/angular impulse
response. Repeatable scenarios and automated validation exercise the physical systems.

### Qualified Surface-Interaction Foundations

For one authored contact point within supported terrain and motion conditions,
NovaCore can certify a contact event, calculate its isolated response, and validate
the resulting motion through the next scheduled simulation target. The earlier
M14.17 milestone added atomic publication of that certified continuation into
authoritative spacecraft state, subject to its clearance and applicability checks.

M14.18 established a separate **staged finite-body contact** foundation using BEPU Physics.
Qualification covers a box contacting an authored planar slab, including centered
and tilted impacts and continued support in a retained local solver world. NovaCore
keeps authority over double-precision position and velocity, orientation, exact time,
event ordering, and history. BEPU solves bounded local contacts and exports private
staged results for NovaCore to validate and publish.

Banked **M14.19 — Persistent BEPU Contact Publication** adds NovaCore-owned validation
and atomic publication of those endpoints into canonical state across repeated exact
simulation intervals. The private BEPU world retains its contact and solver continuity
after each successful publication. NovaCore remains the authority for physical state,
exact time, revisions, and history. Support is qualified at the published endpoints;
motion between or beyond those endpoints is not certified by this result.

**M14.20 — Host-Paced Canonical Contact Servicing** adds live host-time credit and
bounded catch-up while preserving exact simulation intervals and retained debt.
Presentation consumes copied canonical state once per display frame.

**M14.21 — Authored Compound Spacecraft Contact** extends this foundation to a
three-box bus-and-pod engineering article. A NovaCore-owned, precision-aware
four-contact compound selector maintains stable supported contact in the qualified
cases. Release physics, complete-operation performance, and zero warmed managed
allocation checks passed. Project Control manually accepted both centered and tilted
presentations, including settling, camera independence, bounded delay recovery, and
the held final endpoint. Cold preparation and first-use frame hitches remain separate
from the qualified warmed performance.

These are qualified engineering cases. They do **not** establish live Earth terrain
collision, arbitrary terrain-mesh or spacecraft geometry, production landing gear,
Florida launchpad integration, live contact acquisition/departure, or complete landing
gameplay. The article is an engineering fixture; final spacecraft art, flight controls,
and grounded gameplay remain unfinished. BEPU repeatability is qualified within the
tested setup; cross-platform solver determinism is not claimed.

## Why a Custom Engine?

- **Precision across scales.** High-precision physical coordinates and camera-relative
  rendering connect planetary surfaces with astronomical distances.
- **Reproducible behavior.** Deterministic state and explicit physical updates make
  results testable and failures traceable.
- **Clear ownership.** Simulation owns physical truth; rendering owns its presentation.
  Performance work focuses on measured costs and bounded workloads.

## Current Development

M14.21 is accepted and **banked**, building on retained local contact solving,
canonical endpoint publication, and host-paced servicing.
The broader **Surface Interaction / Launch Foundation** remains open.

The qualified authored contact episodes now use live host elapsed time with bounded
servicing. General spacecraft geometry and terrain integration, live contact acquisition
and departure, and the end-to-end landing-and-launch loop remain unfinished.

## Where NovaCore Is Going

**Surface → Atmosphere → Orbit → Return → Landing → Launch again**

The goal is one continuous spaceflight experience, with planetary exploration, orbital
flight, and meaningful terrain interaction sharing the same underlying world.
Production atmospheres, clouds, and environmental systems remain future work, alongside
the broader spacecraft and gameplay systems needed to make that journey playable.

## Build & Engineering Documentation

Development currently targets Windows with .NET, MSVC x64, CMake/Ninja, and Vulkan.
Start with the [build and run guide](docs/build-windows.md) and
[terrain asset setup](docs/terrain-assets.md).

- [Current state and limitations](docs/NOVACORE_CURRENT_STATE.md)
- [Engine architecture](docs/architecture.md)
- [Planetary rendering and terrain](docs/planetary-rendering.md)
- [Spacecraft motion](docs/spacecraft-translation.md)
- [Contact clearance and validation](docs/engineering-evidence/postimpact-feature-coverage/README.md)
- [M14.18 staged BEPU contact qualification](docs/engineering-evidence/bepu-local-contact-staging/staging-qualification.md)
- [M14.19 persistent contact publication qualification](docs/engineering-evidence/persistent-contact-publication/README.md)
- [M14.20 host-paced contact servicing evidence](docs/engineering-evidence/contact-episode-servicing/README.md)
- [M14.21 authored compound contact qualification](docs/engineering-evidence/compound-contact-coverage/formal-qualification/README.md)
- [Engineering evidence](docs/engineering-evidence/README.md)

*Latest banked checkpoint: [M14.21 — Authored Compound Spacecraft Contact](https://github.com/AlbatronicProductions/NovaCore/tree/m14.21-authored-compound-spacecraft-contact).
Predecessors: [M14.20 — Host-Paced Canonical Contact Servicing](https://github.com/AlbatronicProductions/NovaCore/tree/m14.20-host-paced-canonical-contact-servicing),
[M14.19 — Persistent BEPU Contact Publication](https://github.com/AlbatronicProductions/NovaCore/tree/m14.19-persistent-bepu-contact-publication),
and [M14.18 — Staged Finite-Body BEPU Contact](https://github.com/AlbatronicProductions/NovaCore/tree/m14.18-staged-finite-body-bepu-contact).
See the [current scope and limitations](docs/NOVACORE_CURRENT_STATE.md) for the precise
engineering boundaries. Qualification reports retain their pre-banking status as
historical evidence; Git tags identify the published checkpoints.*
