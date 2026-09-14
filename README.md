# NovaCore

**Explore Earth. Watch the Solar System move. See spacecraft physics in action.**

NovaCore is a precision space-simulation and game-engine framework built for a
continuous journey from planetary surfaces to orbit and beyond. Explore Earth from
space down to its terrain, move between Solar System views, and try a powered-flight
demonstration where thrust, fuel use, and changing mass determine the motion.

**Available today:** an interactive exploration sandbox and focused physics demos,
built from source on Windows. The complete piloted landing-and-launch experience is
still in development.

[Get started](#quick-start) · [Choose a scenario](#scenarios-and-controls) ·
[Development status](#development-status) · [Explore the engineering](#under-the-hood)

![Earth against the Milky Way in an earlier NovaCore development build](docs/images/novacore-11a-earth-milky-way.png)

*Earlier development capture; terrain and visual presentation have evolved since this image.*

## What you can experience now

- **Explore Earth across scales.** Move the camera continuously from orbital views to
  near-surface terrain, with Sun-driven day/night lighting, NASA/NOAA source imagery
  and elevation, and optional USGS regional detail around Florida.
- **Tour the Solar System.** Focus on planets and moons, inspect Saturn's rings, and
  return to an interactive orbital map. Pause time or change the Solar simulation
  rate from 0.1× to 7,776,000× to watch celestial motion unfold.
- **Watch finite-fuel powered flight.** Start an authored spacecraft demo, see it
  accelerate as propellant and total mass decrease, then coast when its fuel runs
  out. Motion, fuel, and actual engine output advance together in the simulation.
- **Inspect contact physics.** Separate development scenes show authored bodies
  settling against a simple slab, with retained contact and a movable camera.
  These are bounded physics demonstrations; terrain landing gameplay is unfinished.

The powered-flight demo uses a development cube and one ideal engine in free space,
with no gravity or contact. Its fixed burn is started with **Space**; camera movement
does not pilot the craft. Solar System time-warp controls belong to the exploration
scene and do not extend this demo's qualified physics scope.

## Screenshots

<p align="center">
  <img src="docs/images/novacore-11a-earth-moon.png" alt="Earth and Moon in an earlier NovaCore development build" width="49%">
  <img src="docs/images/novacore-11a-earth-sun.png" alt="Earth, Moon, and Sun in an earlier NovaCore development build" width="49%">
</p>
<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="Mars in an earlier NovaCore development build" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="Solar System overview at maximum time warp in an earlier NovaCore build" width="49%">
</p>

*Repository development captures of Earth, Moon, Sun, Mars, and Solar System time
warp. These predate the current terrain and spacecraft work; they do not demonstrate
completed atmosphere or cloud systems, or the current powered-flight demo.*

## Quick start

### 1. Build the Windows preview

The documented development environment is **Windows 11, x64**. Install:

- Git with Git LFS, and the .NET 10 SDK.
- Visual Studio 2026 with **Desktop development with C++** and the Windows 11 SDK.
- CMake 4.4 or later, Ninja, and the LunarG Vulkan SDK, including `glslc`.
- A Vulkan-capable GPU supporting the renderer's required features, including
  double-precision shader arithmetic and tessellation.

Open a **Visual Studio x64 Developer PowerShell**. For a fresh checkout:

```powershell
git lfs install
git clone https://github.com/AlbatronicProductions/NovaCore.git
cd NovaCore
git lfs pull

cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release
dotnet build NovaCore.sln -c Release

dotnet run --project tools/NovaCore.Launcher -c Release
```

The native build creates the renderer DLL and shaders needed by the managed build;
keep that order. Run the commands below from the repository root. See the
[Windows build guide](docs/build-windows.md) for Debug builds and validation.

### 2. Try powered free flight

In the launcher, choose **Development - Powered Free Flight**, then launch.
This scene does not need the generated Earth terrain packages. To start it directly:

```powershell
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=powered-free-flight
```

Wait for **READY**, then press **Space**. The cube accelerates for approximately five
seconds, exhausts its finite fuel, and coasts until the eight-second episode ends.
**COMPLETED** holds the final state. Watch the displayed fuel, actual throttle, and
speed; move the camera independently. Relaunch the scene to restart the episode.

### 3. Prepare Earth and explore

Earth and Solar System scenes require the global terrain package. Generation needs
Python with **NumPy 2.3.5** and **Pillow 11.3.0**; see the
[asset provenance and dependencies](assets/earth/PROVENANCE.md). If that environment
is not your default `python`, set `NOVACORE_PYTHON` to its interpreter path.

```powershell
dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -- verify earth-surface-v5
dotnet run --project tools/NovaCore.Launcher -c Release
```

Choose **New Earth Renderer** for an Earth-focused view starting at 700 km, or
**Solar System Overview** for the orbital map. The launcher also offers window size,
resolution, and diagnostics settings. A fresh launcher's default is Solar System
Overview, so prepare the global assets before using that preset.

Terrain packages are generated locally and verified against the repository manifests.
There is currently no configured prebuilt terrain download; normal runtime does not
download assets. The [terrain asset guide](docs/terrain-assets.md) covers setup,
verification, cache location, and recovery from missing assets.

<details>
<summary>Optional: add Florida regional terrain detail</summary>

Florida refinement additionally uses **Rasterio** and **PyProj** in the Python
environment. In PowerShell 7, acquire the source data and build the regional package:

```powershell
pwsh tools/earth_data/acquire_florida_m12.ps1
dotnet run --project tools/NovaCore.AssetTool -- build earth-florida-m12
dotnet run --project tools/NovaCore.AssetTool -- verify earth-florida-m12
```

Then choose **Florida Launch Site** in the launcher. This is a terrain/site
exploration view, not a powered launch scenario. Without the regional package,
Earth exploration retains the global terrain base and reports the missing refinement.

</details>

## Scenarios and controls

| Start with | What it opens |
|---|---|
| **New Earth Renderer** | Earth at 700 km, using the production terrain renderer and Solar navigation. |
| **Solar System Overview** | The interactive Solar map, body focus, and time warp. |
| **Earth Far / Orbital View** or **Earth 700 km** | Earth exploration from the selected starting altitude. |
| **Florida Launch Site** | The anchored Florida site view; liftoff and flight integration are unfinished. |
| **Development - Powered Free Flight** | The finite-fuel, eight-second burn-and-coast demonstration. |
| **Development — Canonical Contact / Centered** or **Tilted** | Separate 20-second box-and-slab contact demonstrations. |

**Solar navigation** also applies to New Earth Renderer and Florida Launch Site:

| Input | Action |
|---|---|
| Left or right mouse drag | Orbit the focused body; look around in surface mode. |
| Mouse wheel | Zoom toward or away from the focused body/surface. |
| `1`–`0` | Select a celestial focus (`4`: Earth; `5`: Moon). |
| `E` | Attach/detach the surface-relative camera. |
| `W A S D` in surface mode | Move the camera; `Shift` speeds up and `Ctrl` slows down. |
| `,` / `.` | Decrease/increase Solar simulation rate. |
| `Space` | Pause/resume Solar simulation. |
| `R` | Return to Solar Map/home. |

**Powered-flight demo:** `Space` starts the episode; `W A S D` and `Q` / `E` move the
camera, mouse drag looks around, the wheel changes camera speed, and `R` resets the
camera. These keys do not provide manual thrust, gimbal, or attitude control.

**Contact demos:** wait for **READY**, then press `Space` to start the 20-second
episode. Use the same camera controls as the powered-flight demo. The final state
remains displayed; relaunch to restart. This also applies to the compound scenes below.

**Earth altitude presets:** drag to orbit/look, use the wheel to zoom and `W A S D`
to move in surface mode, and press `R` to reset the Earth view.

<details>
<summary>Direct scene commands and compound contact demos</summary>

After building Release and preparing Earth assets where required:

```powershell
# Solar System exploration
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=sol

# Earth-focused production renderer (the New Earth Renderer launcher route)
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=m12d-production-spherical-billboard

# Florida site exploration
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=sol --focus=earth --surface-site=florida-launch

# Authored three-box spacecraft contact article; these are direct CLI scenes
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=article-centered
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=article-tilted
```

Both `earth` and `sol` use the current production Earth renderer. The longer
`m12d-production-spherical-billboard` name is the existing Earth-focused launch alias.
The sample project retains its historical `NovaCore.Triangle` name; it hosts these
scenes as well as the original graphics demonstrations.

</details>

## What makes NovaCore distinctive

**Precision from local space to astronomical distances.** High-precision coordinates
and reference frames describe the world; camera-relative rendering brings it to the
screen without rescaling planets or orbital distances.

**Simulation you can reproduce.** Exact simulation time, ordered events, and explicit
state ownership support deterministic replay and repeatable physical scenarios.
Rendering and camera movement do not define physical state. Determinism is qualified
within documented runtime/build conditions, not promised universally across platforms.

**Fuel has physical consequences.** The new powered model evaluates thrust with
changing mass and evolving orientation. When fuel ends partway through an interval,
that interval is split into powered motion and coast. Physical state, remaining fuel,
and actual engine output are published together, preventing a visible mismatch.

**A purpose-built engine.** NovaCore combines a custom C# simulation framework with a
native C++/Vulkan renderer. Planetary detail, simulation time, spacecraft motion, and
presentation have explicit responsibilities that can be tested independently.

## Development status

**Segmented powered free flight is now banked and available to run.** It joins the
existing planetary exploration and authored contact demonstrations. This is an actively
developed framework: the demos expose working systems while the complete spaceflight
experience is still being assembled.

| Available now | Remaining development |
|---|---|
| Earth terrain exploration and Solar System presentation | Further terrain/material polish; production atmosphere, clouds, water, and weather. |
| Bounded powered free flight with finite propellant, changing mass, and exhaustion-to-coast behavior | Powered retained contact, liftoff, and Florida launch integration. |
| Independent spacecraft translation, rigid-body rotation, forces, torques, and qualified contact response | Gimbal/RCS, attitude-control hardware, broader flight controls, and production spacecraft art. |
| Authored box/compound contact against a planar slab | General spacecraft/terrain collision, landing gear, live contact acquisition/departure, and complete landing gameplay. |

The long-term goal is a continuous **surface → atmosphere → orbit → return → landing
→ launch** journey. The remaining items describe direction and unfinished scope, not
release dates or a claim that this loop is playable today.

## Under the hood

For developers, the key systems include:

- **Planetary rendering:** an 18-level terrain topology library, camera-relative
  precision, GPU preparation and culling, bounded tessellation, and synchronized
  publication of complete terrain resources.
- **Physical terrain authority:** one body-fixed height definition supports physical
  queries and render preparation; moving the camera or changing detail does not
  redefine the terrain.
- **Spacecraft simulation:** independent translation and rigid-body rotation, exact
  event/resource boundaries, and atomic state publication. Powered trajectories use
  numerical integration with explicit error limits; exact fuel accounting does not
  imply an exact trajectory.
- **Local contact solving:** BEPU Physics works inside a retained, bounded local world.
  NovaCore validates and publishes authoritative endpoints. Current qualification does
  not certify all motion between endpoints or cross-platform BEPU determinism.
- **Reproducible assets and evidence:** manifest-resolved terrain, content hashes,
  source provenance, permanent tests, independent reviews, and retained reproduction
  instructions.

### Documentation and evidence

| To learn about | Start here |
|---|---|
| Building and preparing assets | [Windows build guide](docs/build-windows.md) · [Terrain assets](docs/terrain-assets.md) · [Earth data provenance](assets/earth/PROVENANCE.md) |
| Engine design | [Architecture](docs/architecture.md) · [Engineering rules](ENGINEERING_RULES.md) |
| Planetary rendering | [Renderer contract](docs/planetary-rendering.md) · [Current-state overview](docs/NOVACORE_CURRENT_STATE.md) |
| Simulation and spacecraft | [Simulation time](docs/simulation-time.md) · [Spacecraft motion](docs/spacecraft-translation.md) · [Attitude dynamics](docs/spacecraft-attitude.md) |
| Powered free flight | [Physics contract](docs/engineering-evidence/segmented-powered-free-flight/physics-contract.md) · [Final qualification](docs/engineering-evidence/segmented-powered-free-flight/final-qualification.md) · [Manual acceptance](docs/engineering-evidence/segmented-powered-free-flight/manual-acceptance.md) |
| Validation and historical work | [Engineering evidence index](docs/engineering-evidence/README.md) |

### Published checkpoint

**[M15.0 — Segmented Powered Free Flight](https://github.com/AlbatronicProductions/NovaCore/tree/m15.0-segmented-powered-free-flight)**

Banked commit: `4607d8c802006d5e1a01c595ab608cf53a4dab6b`

Annotated tag: `m15.0-segmented-powered-free-flight`

The [qualification package](docs/engineering-evidence/segmented-powered-free-flight/README.md)
records the bounded physics, numerical, ownership, performance, and manual evidence,
including an unresolved historical allocation anomaly and its later passing runs.

Some engineering overviews still describe earlier checkpoints, and qualification
reports retain their original pre-banking wording. For this powered-flight release,
the published tag identifies the banked source; the linked reports preserve its
qualification history and limits.

<details>
<summary>Earlier foundations and milestone evidence</summary>

The [contact-clearance work](docs/engineering-evidence/postimpact-feature-coverage/README.md)
certifies one admitted authored point under supported terrain and motion conditions.
M14.17 added checked publication of that continuation. This did not establish general
spacecraft geometry or a complete landing system.

| Published foundation | Scope and evidence |
|---|---|
| [M14.18 — Staged Finite-Body BEPU Contact](https://github.com/AlbatronicProductions/NovaCore/tree/m14.18-staged-finite-body-bepu-contact) | Private box/slab solving and retained local contact; [qualification](docs/engineering-evidence/bepu-local-contact-staging/staging-qualification.md). |
| [M14.19 — Persistent BEPU Contact Publication](https://github.com/AlbatronicProductions/NovaCore/tree/m14.19-persistent-bepu-contact-publication) | Validated canonical endpoint publication with solver continuity; [evidence](docs/engineering-evidence/persistent-contact-publication/README.md). |
| [M14.20 — Host-Paced Canonical Contact Servicing](https://github.com/AlbatronicProductions/NovaCore/tree/m14.20-host-paced-canonical-contact-servicing) | Bounded live servicing, retained time debt, and copied presentation state; [evidence](docs/engineering-evidence/contact-episode-servicing/README.md). |
| [M14.21 — Authored Compound Spacecraft Contact](https://github.com/AlbatronicProductions/NovaCore/tree/m14.21-authored-compound-spacecraft-contact) | Three-box engineering article, qualified contact selection, and manually accepted centered/tilted scenes; [qualification](docs/engineering-evidence/compound-contact-coverage/formal-qualification/README.md). |

The subsequent [command authority](docs/engineering-evidence/spacecraft-command-authority/README.md),
[single-engine preparation](docs/engineering-evidence/single-engine-actuation-preparation/README.md),
and [finite-propellant segmentation](docs/engineering-evidence/finite-propellant-segmentation/README.md)
provide the command, actuator, and resource foundations used by powered free flight.
These engineering articles and models retain their documented scope; they are not
production spacecraft art, real tank models, or unrestricted landing/launch gameplay.

</details>

## Feedback, contributions, and license

Useful bug reports include the commit, selected scenario, build configuration, hardware,
reproduction steps, and relevant logs. Engineering changes should preserve the
[project's physical and ownership contracts](ENGINEERING_RULES.md) and come with
appropriate validation.

This repository snapshot does not include a project-wide license file. Bundled BEPU
has its own [license](external/bepu/2.5.0-beta.29/LICENSE.txt) and
[dependency provenance](external/bepu/README.md); Earth source credits and usage notes
are recorded in the [data provenance](assets/earth/PROVENANCE.md).
