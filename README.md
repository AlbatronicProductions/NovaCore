# NovaCore

**A precision space-simulation and game framework by Albatronic Productions.**

NovaCore is a custom-built foundation for exploring planetary worlds and flying
spacecraft across vast distances. It is being developed toward one continuous
experience: leave the surface, travel through space, and return to land.

![Earth against the Milky Way](docs/images/novacore-11a-earth-milky-way.png)

*Earth against the Milky Way in an earlier development build.*

**In active development.** Working simulation systems are available through source
builds and development scenarios. There is no packaged player release yet.

[Working today](#working-today) · [Project direction](#where-novacore-is-going) ·
[Source access](#trying-novacore-today) · [Technical documentation](#for-developers)

## A simulation across scales

Spaceflight connects very different scales: the ground beneath a spacecraft, a
planet's horizon, and the distances between worlds. NovaCore is built around
preserving that sense of scale, with high-precision positioning and a simulation
in which motion, rotation and fuel have physical consequences.

The goal is a cohesive space simulation where planetary exploration and spacecraft
flight belong to the same journey. Today's working systems are the foundations;
the full player experience is still being assembled.

## Working today

**Current banked milestone:** M15.1 — Finite-Fuel Retained Powered Contact.
Ordinary retained BEPU contact now joins exact propellant consumption and
exhaustion with atomic motion/resource/mass/actuator publication, followed by a
dry/off actuator transition and same-body dry retained contact. It does not
implement liftoff or departure.

- **Earth exploration:** move the camera from orbital views down toward terrain,
  with Sun-driven day and night, NASA/NOAA source imagery and elevation, and USGS
  regional detail around Florida.
- **Solar System views:** explore planets and moons, see Saturn's rings, and
  change the simulation rate to watch celestial motion unfold.
- **Spacecraft motion and propulsion:** simulate translation and rotation under
  forces and torques. Powered flight consumes finite propellant, changes spacecraft
  mass, and continues into coasting when the fuel runs out.
- **Surface interaction foundations:** explore an anchored Florida site and inspect
  separate demonstrations of simple bodies settling against a slab.
- **Finite-fuel retained powered contact:** apply finite-fuel propulsion while a
  body remains supported, with physical state, remaining fuel, mass and actual
  actuator state published together. This bounded contact path is separate from
  powered free flight.

These capabilities are exercised in separate development scenarios. The current
powered-flight demonstration uses a simple cube in free space, without gravity or
contact; piloted flight and complete terrain landing are still ahead.

## Screenshots

<p align="center">
  <img src="docs/images/novacore-11a-mars.png" alt="Mars illuminated against a star field" width="49%">
  <img src="docs/images/novacore-11a-solar-warp.png" alt="Solar System map showing planetary orbits with accelerated time" width="49%">
</p>

*Mars and the Solar System viewed with accelerated time. These earlier development
captures show the project's planetary scale; terrain, lighting and presentation
continue to evolve. They do not depict finished spacecraft or atmosphere systems.*

## Where NovaCore is going

**Surface → Launch → Atmosphere → Orbit → Interplanetary flight → Return → Landing**

That connected journey is the long-term goal. Work ahead includes flight controls,
finished spacecraft models, facilities, liftoff and landing interactions, richer
terrain, and atmosphere, cloud and water systems. Supported-contact departure,
surface-to-flight transition and Florida launch integration remain future work.

The intended player experience is straightforward: download a packaged build,
launch an executable, and enter the simulation. Today's source build and scenario
launcher serve development while that experience takes shape. No release date is
promised.

## Trying NovaCore today

Current access is for developers and technically comfortable source builders on
**Windows 11, x64**. The launcher opens focused development scenarios used to
exercise, diagnose and validate individual systems; it is not the final gameplay
flow.

Start with the [Windows build guide](docs/build-windows.md) and
[terrain setup guide](docs/terrain-assets.md). Earth and Solar System scenes
require **both the global and Florida regional terrain packages**, including when
starting away from Florida.

<details>
<summary>Source build and required Earth data</summary>

Install the toolchain in the build guide, plus Git with Git LFS, Ninja and
PowerShell 7. Terrain generation uses Python with **NumPy 2.3.5** and
**Pillow 11.3.0**, pinned in
[requirements.txt](tools/earth_data/requirements.txt). If needed, set
`NOVACORE_PYTHON` to that interpreter's path.

From a Visual Studio x64 Developer PowerShell:

```powershell
git lfs install
git clone https://github.com/AlbatronicProductions/NovaCore.git
cd NovaCore
git lfs pull

cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release
dotnet build NovaCore.sln -c Release

dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -- verify earth-surface-v5
pwsh tools/earth_data/acquire_florida_m12.ps1
dotnet run --project tools/NovaCore.AssetTool -- build earth-florida-m12
dotnet run --project tools/NovaCore.AssetTool -- verify earth-florida-m12
dotnet run --project tools/NovaCore.Launcher -c Release
```

Prepare both packages before using the launcher's default Solar System preset.
Packages are generated and verified locally; no prebuilt terrain download is
currently configured, and runtime does not download missing assets. See the
[terrain guide](docs/terrain-assets.md) for cache setup and recovery.

</details>

<details>
<summary>Development and diagnostic scenarios</summary>

These scenarios exercise specific NovaCore systems during development. They are
not intended as the final player flow or a list of finished game modes.

- **Planetary exploration:** New Earth Renderer and Solar System Overview open
  planetary views; Florida Launch Site opens the anchored site without liftoff.
- **Powered flight:** Development - Powered Free Flight shows a fixed burn, fuel
  exhaustion and coast. Wait for READY, then press Space. Camera movement does not
  pilot the craft. This scene does not require Earth terrain packages.
- **Contact:** the centered and tilted contact scenes show bounded box-and-slab
  interaction; they do not implement spacecraft landing on Earth terrain.

For the powered-flight scene directly, after building Release:

```powershell
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=powered-free-flight
```

The banked powered-contact scenario has a separate direct route:

```powershell
dotnet run --project samples/NovaCore.Triangle -c Release -- --scene=powered-contact
```

For powered contact, Space advances two inspection intervals, then starts live
dry continuation. The body remaining stationary on its support is expected;
camera movement does not pilot it. This path passed Debug/Release regression,
exact-zero warmed allocation contracts, retained-storage limits and integrated
frame qualification on the recorded host. This is not a universal FPS guarantee.

</details>

## For developers

NovaCore combines a C# simulation core with a native C++/Vulkan renderer. Detailed
contracts, implementation limits and reproducible evidence live in the documentation.

| Topic | Read more |
|---|---|
| Architecture and engineering principles | [Architecture](docs/architecture.md) · [Engineering rules](ENGINEERING_RULES.md) |
| Planetary rendering and data | [Renderer](docs/planetary-rendering.md) · [Terrain assets](docs/terrain-assets.md) · [Source provenance](assets/earth/PROVENANCE.md) |
| Simulation and spacecraft | [Simulation time](docs/simulation-time.md) · [Spacecraft motion](docs/spacecraft-translation.md) · [Attitude dynamics](docs/spacecraft-attitude.md) |
| Powered flight | [Physics contract](docs/engineering-evidence/segmented-powered-free-flight/physics-contract.md) · [Qualification](docs/engineering-evidence/segmented-powered-free-flight/final-qualification.md) |
| M15.1 retained powered contact | [Qualification history, limits and reproduction](docs/engineering-evidence/powered-contact-bank-candidate/README.md) · [Banked source](https://github.com/AlbatronicProductions/NovaCore/tree/m15.1-finite-fuel-retained-powered-contact) |
| Development state and history | [Engineering overview](docs/NOVACORE_CURRENT_STATE.md) · [Evidence index](docs/engineering-evidence/README.md) · [Powered-flight source checkpoint](https://github.com/AlbatronicProductions/NovaCore/tree/m15.0-segmented-powered-free-flight) |

Historical reports retain their original scope and status; the linked source
checkpoint identifies the published powered-flight implementation.

## Feedback and license

Useful bug reports include the build/commit, hardware, reproduction steps and logs.
For code contributions, read the [engineering rules](ENGINEERING_RULES.md) and
the intentional-contribution terms in [LICENSE](LICENSE), section 12.

NovaCore is source-available under the [NovaCore Source-Available License 1.0](LICENSE).
Original NovaCore material is owned by Tyler Alba, operating under the
project/developer name Albatronic Productions, unless otherwise noted.
Personal use and private personal modifications are permitted. Independent
creators may monetize their content and keep its revenue under the license.
See [NOTICE](NOTICE.md), [licensing](LICENSING.md), the
[Creator Content Policy](docs/legal/CREATOR-CONTENT-POLICY.md),
[Modding Policy](docs/legal/MODDING-POLICY.md) and
[commercial licensing](docs/legal/COMMERCIAL-LICENSING.md) for rights and limits.

Bundled BEPU retains its own [Apache-2.0 license](external/bepu/2.5.0-beta.29/LICENSE.txt)
and [dependency provenance](external/bepu/README.md). NASA, NOAA and USGS source
credits and data boundaries remain in [data provenance](assets/earth/PROVENANCE.md).
