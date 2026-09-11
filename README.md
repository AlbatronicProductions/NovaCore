# NovaCore

**A custom precision space-simulation and game-engine framework built for seamless travel from planetary surfaces to orbit and beyond.**

NovaCore brings planetary rendering, spacecraft physics, and surface interaction into one
coherent runtime. Built independently of Unity and other general-purpose engines, it owns
its rendering and simulation systems, with high-precision coordinates and deterministic
physical state at the core. The foundations are working; the complete spaceflight
experience is still being built.

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

### Terrain Contact & Landing Foundations

For the supported single authored contact point and terrain conditions, NovaCore can
locate a contact event, calculate its isolated physical response, and advance the
resulting spacecraft motion in a separate validated calculation. It can certify that
this point remains clear of the supported natural terrain through the next scheduled
simulation target, or report that clearance could not be established.

This is a bounded contact foundation: it does not yet cover a whole spacecraft hull,
multiple landing legs, persistent ground support, friction, or general contact solving.
It is not finished landing gameplay.

## Why a Custom Engine?

- **Precision across scales.** High-precision physical coordinates and camera-relative
  rendering connect planetary surfaces with astronomical distances.
- **Reproducible behavior.** Deterministic state and explicit physical updates make
  results testable and failures traceable.
- **Clear ownership.** Simulation owns physical truth; rendering owns its presentation.
  Performance work focuses on measured costs and bounded workloads.

## Current Development

The current focus is moving from validated post-impact calculations to safely applying
those results to the live spacecraft state. That integration is not yet implemented.
Persistent rest on terrain, release and liftoff, and broader collision handling remain
work toward a complete landing-and-launch loop.

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
- [Engineering evidence](docs/engineering-evidence/README.md)

*Current banked checkpoint: M14.16 — post-impact terrain-contact clearance for the
supported authored point. Surface interaction and launch foundations remain in development.*
