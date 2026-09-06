# Graphics / window validation review

Unbanked Graphics-validation package, 2026-09-05. Operational commands and the
definition of a full automated pass live in [build-windows.md](build-windows.md#graphics-validation-contract).
This report does not reopen Earth rendering, performance, compatibility or cache policy.

## Baseline and environment

- Clean `main`, HEAD and directly queried `origin/main` both
  `e9e993964dd79995c605db424d6aec9faf30bc30`; cache lifecycle already banked.
- P2S5H tag `m12d-p2s5h-earth-route-convergence` remains at
  `32ffac50ab5c06518ede24edfb5c531976d4ec99`.
- Windows x64; .NET SDK 10.0.303; MSVC 14.51.36231; Vulkan SDK/layer 1.4.357.0.
  Loader reports instance 1.4.309. Selected window GPU: AMD Radeon RX 6800 XT,
  Vulkan 1.4.315, AMD proprietary driver 26.8.1; Windows driver 32.0.21045.5002.
  Also enumerated: AMD integrated graphics, Meta and Virtual Desktop monitors.
- Initial deployed test-native SHA-256: Debug
  `7e91c7cc4e08384b8545c39ceaa60f2f54fe109190075ce3b42c86dd39edc013`;
  Release `48a6d48def689e76687a4dcc2f77398dc7cbeac356739c3bac345fa25c606b65`.
  Project copy selection was already configuration-specific. Initial managed
  runner did not print actual module identity; the isolated live Debug run did
  confirm the sample's exact Debug module and hash. Final harness proves the
  actual module for every case, rather than inferring it from copy rules.
- No NOVACORE or Vulkan layer-filter overrides were set for ordinary suite runs.
  The existing 18-scale production library was explicitly used for bounded final
  topology checks; the original isolated Debug topology regeneration is separately
  recorded. Optional exhaustive duplicate regeneration was not requested.

## Initial reproduction and causal decisions

Before source modifications:

```powershell
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Release
```

Each stopped after 27 passes at `Anchored Florida launch site`, with 1 failure,
0 skips and 58 unexecuted cases. Exit code: -532462766. Exact exception:
`M12 site fingerprint binds the same geographic reservation to the regional physical height: 0x9B0B082987CE3221`.
No window was reached by either as-is full run. The entire catalog was then
dispatched into individual original-runner processes to expose order dependence.

| Finding / non-pass | Class and causal evidence | Decision |
|---|---|---|
| Florida transport fingerprint | **B, fixture ownership.** Alone the unloaded-oracle fixture has terrain 0 m, root radius 6371009.050755245 m and hash `5B21D11ADAC71C6F`. Earlier facility/elevation tests publish global and regional datasets into process-wide state; there is intentionally no unload operation. Full-run observed hash differs without changing the site algorithm. | Fresh process per case. Preserve the explicit fallback transport fixture and its hash; assert the unloaded prerequisite. Production-data support/seating/live regressions remain separate and unchanged. |
| Fail-first runner | **B.** The first exception prevented 58 contracts from running, with no summary of missing coverage. | Continue per-case processes, named failures and exit status; explicit categories, zero-match error, exclusions separate from skips. |
| Release using Debug dependencies | **C.** Live regional test hardcoded Debug Triangle; direct and indirect GPU helpers and SPIR-V tests selected `native-ninja`. | Matching sample, native and shaders per configuration. Verify hashes and actual loaded paths; explicit shader inputs to diagnostic proof helpers preserve their existing callers. |
| Alternate output root | **B.** A review build under `build/graphics-validation-candidate` exposed 24 failures resolving sources/assets under `E:\` because tests walked exactly five parents. | Existing repository discovery owns paths; no guessed parent count or new production asset fallback. All affected test assertions preserved. |
| Opaque draw-order assertion | **E.** Test required statistics-query invocation immediately after an `if` with no intervening statements. Current renderer records publication metadata there, then draws the same NCSM1 indirect buffer in the required order. | Check the actual indirect draw between distant and bootstrap/focused-orbit draws; retain reversed-Z/read-only depth assertions. No renderer reordering. |
| Release regional reentry coverage | **B, proven wheel-isolation gap.** Fixed 1,000-frame run did not return to L17 after frame 740; it reached L8 at frame 740 despite the scripted site return at 660. Key/look input was gated but `WM_MOUSEWHEEL` was not. Controlled +120 Win32 message produced `Solar orbit distance=6941480340480 m` with the probe both off and on. | Extend only the existing regional diagnostic gate to wheel input. Permanent visible-window test proves ordinary wheel zoom remains live and probe wheel input cannot zoom. No added retry/drain frames or reduced physical assertions. The earlier failed run did not log its wheel messages, so individual desktop events in that recording cannot be reconstructed. |
| Shader unused output | **G, legal interface warning.** `planetary.vert` emits flat uint Location 11 for `planetary_production.frag`; generic `planetary.frag` does not consume it. SDK explicitly reports discarded unused output, not invalid geometry. | Keep shaders unchanged. Accept only the exact `WARNING-Shader-OutputNotConsumed` warning at vertex Location 11 Component 0. Log it; negative tests reject changed location, ID, severity and all VUIDs. |
| D3D11 KMT import | **G, measured OBS-hook interaction; exact caller still unresolved.** Real `[error] VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645` reproduced in all three final default Debug window cases: memoryTypeIndex 0, memoryTypeBits 0, D3D11_TEXTURE_KMT. All three pass with only `VK_LAYER_OBS_HOOK` disabled, same binaries/GPU/shaders and Khronos validation enabled. NovaCore source requests only swapchain device extension and has no Win32 external-memory import calls. OBS 32.2.2 is running; hook SHA is in provenance. Two earlier stack-instrumented repetitions did not reproduce the error. | Remove the old VUID-only allowlist. Keep the error fatal, preserve both profiles, and do not infer a benign import from successful presentation. Temporary stack instrumentation removed. Next: capture the invalid import call stack with OBS enabled and identify the hook/driver contract violation. No global hook/OBS setting changed. |
| Native presentation tests had no validation | **B.** Their private test device created an instance without a layer or messenger. Numerical PASS did not prove Vulkan validity. | Require Khronos validation in both configurations, capture instance creation through device teardown, and fail on errors. This is test-only device ownership. |
| Missing Epic overlay manifests | **D, host configuration.** Both newly validated native tests fail on loader errors for three absent EOS overlay JSON registrations; `vulkaninfoSDK --summary` independently reports the same paths. No VUID is emitted. With implicit discovery redirected to an empty diagnostic folder, both tests pass in both configurations while explicitly requesting Khronos validation. | Preserve default-environment failures. No registry/driver cleanup or hardcoded vendor-message suppression. Clean implicit-layer discovery is a disclosed comparison profile, not proof that the normal host is repaired. |
| Grid startup | **A, existing production defect outside the Earth acceptance route.** New window probe initially used `--scene=grid --objects=1`; native `CreateSubmission` unconditionally requires the production elevation oracle while this sample route does not supply it. It exits after creating a window/swapchain. | Window lifecycle regression uses the supported ordinary Solar route. Retain the grid failure and defer a bounded generic-scene resource-ownership decision; do not invent a dummy Earth resource or change P2S5H to make the test green. |

No obsolete test was deleted and no assertion tolerance was weakened. The stale
statistics-string expectation was modernized around its existing live invariant.
No ceremonial skip was found. Optional artifact output/exhaustive generation flags
are reproduction modes, not automatic skip conditions.

## Ownership and configuration audit

Native `Window` owns class/window registration and raw input; `Surface`, `Swap`,
`Recreate`, `Frame` and `Cleanup` own the same production Vulkan lifecycle used by
the sample. It always calls `ShowWindow(SW_SHOW)`; there is no hidden-window
production mode. Offscreen GPU proofs already own separate attachment/device
contexts and do not prove Win32 presentation. Window tests use real production
surface/swapchain code, not a test swapchain.

Resize/minimize/restore checks verify actual client/swapchain dimensions. Closing
uses WM_CLOSE and confirms a successful renderer exit and destruction of its HWND.
Repeated startup tests use fresh processes, the production sample lifecycle;
they do not claim unsupported in-process renderer reentry. Exception cleanup kills
only the child belonging to the failed test. No user input or foreground focus is
required for API checkpoints. Regional scripted input is explicitly isolated.

Debug window creation requests `VK_LAYER_KHRONOS_validation`; missing layer is a
prerequisite failure. Release does not request it. Both request surface, Win32
surface and debug-utils instance extensions and require a compatible present queue,
FP64, anisotropy, multi-draw indirect, tessellation, vertex stores/atomics and
pipeline statistics. The first suitable enumerated device is selected; there is
no test-only device override. GPU compute/proof APIs use their existing FP64 and
queue requirements. Their layer availability is now checked before tests run.

The production log callback now preserves severity and message ID. It does not
filter messages. Window validation rejects unknown warnings and every VUID.
The two native presentation tests also observe loader initialization errors;
older query/mesh proof metrics count validation errors after instance creation,
so those metrics alone do not audit host overlay registrations. Native presentation
tests and the environment inventory cover that distinct responsibility.

## KSA material comparison

Read-only installed KSA version `2026.9.7.5402`, revision
`487c3f340de24c6a81037120b6d1129c045c5400`, DLL SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
Current decompiled `KSA/Program.cs` creates the GLFW host window, attaches input
events, constructs the renderer from that window, and acknowledges swapchain
rebuild after updating dependent frame resources. On Windows its bootstrap window
can initially be hidden; that is not evidence of a headless acceptance path.

Issue: window and GPU attachment lifecycles need explicit owners. Current KSA
responsibility: application host owns window/input, renderer owns swapchain and
dependent rendering resources. **ADAPT responsibility:** test NovaCore through its
existing Win32 host and renderer lifecycle; keep existing offscreen proofs separate.
No historical KSA correction matching this intermittent KMT import or NovaCore's
fixture contamination was established; no KSA fix chronology is invented. No KSA
source copied and no GLFW/offscreen replacement introduced.

## Validation inventory

All rows below are permanent regressions. The invariant is named by the case;
its entry point is retained in the machine-readable evidence. Category
prerequisites and CI/visibility boundaries are defined once in build-windows.md.
HEADLESS includes source-contract checks even when a case name mentions GPU.

<!-- CASE_TABLE_START -->
| Case / invariant | Category | Initial isolated Debug | Final Debug | Final Release |
|---|---|---|---|---|
| Facility lighting authority | headless | PASS | PASS | PASS |
| MeshHandle | headless | PASS | PASS | PASS |
| Transport layout | headless | PASS | PASS | PASS |
| Transform conversion | headless | PASS | PASS | PASS |
| Camera relative | headless | PASS | PASS | PASS |
| Batches and capacity | headless | PASS | PASS | PASS |
| Resolved render transport | headless | PASS | PASS | PASS |
| Orbit curve transport | headless | PASS | PASS | PASS |
| Static reference-frame fixture transport | headless | PASS | PASS | PASS |
| Dynamic reference-frame fixture publication | headless | PASS | PASS | PASS |
| Celestial analytical fixture publication | headless | PASS | PASS | PASS |
| Celestial player torque controls | headless | PASS | PASS | PASS |
| Celestial SAS mode selection | headless | PASS | PASS | PASS |
| Celestial SAS control cadence | headless | PASS | PASS | PASS |
| Celestial SAS convergence | headless | PASS | PASS | PASS |
| Celestial SAS diagnostic indicators | headless | PASS | PASS | PASS |
| Camera snapshot allocation | headless | PASS | PASS | PASS |
| Planetary presentation pipeline | headless | PASS | PASS | PASS |
| Planetary presentation SPIR-V stride | headless | PASS | PASS | PASS |
| Focus target authority | headless | PASS | PASS | PASS |
| Planet material presentation | headless | PASS | PASS | PASS |
| Planet micro-normal foundation | headless | PASS | PASS | PASS |
| Planet surface scatter placement | headless | PASS | PASS | PASS |
| Planetary surface camera presentation | headless | PASS | PASS | PASS |
| Earth CPU elevation oracle | headless | PASS | PASS | PASS |
| Canonical body-fixed geographic handedness | headless | PASS | PASS | PASS |
| Canonical SurfaceAnchor physical terrain authority | headless | PASS | PASS | PASS |
| Anchored Florida launch site | headless | PASS | PASS | PASS |
| Earth route convergence | headless | PASS | PASS | PASS |
| Live NCSM1 regional physical residency | window | PASS | FAIL | PASS |
| Production window lifecycle | window | NEW | FAIL | PASS |
| Window validation message policy | headless | NEW | PASS | PASS |
| Regional diagnostic wheel isolation | window | NEW | FAIL | PASS |
| Florida facility support | headless | PASS | PASS | PASS |
| Surface-relative camera authority | headless | PASS | PASS | PASS |
| Near-surface inertial free-look | headless | PASS | PASS | PASS |
| SurfaceAnchor acquisition, ENU, and handoff | headless | PASS | PASS | PASS |
| Camera focus-position continuity | headless | PASS | PASS | PASS |
| Camera SurfaceAnchor handoff monotonicity | headless | PASS | PASS | PASS |
| Solar preset camera-path convergence | headless | PASS | PASS | PASS |
| Zoom motion-profile continuity | headless | PASS | PASS | PASS |
| Solar camera bounded-domain crash regression | headless | PASS | PASS | PASS |
| Surface visual-aim continuity | headless | PASS | PASS | PASS |
| Inertial visual-aim authority | headless | PASS | PASS | PASS |
| Cube-sphere planetary surface | headless | PASS | PASS | PASS |
| Production relaxed cube-sphere patch hierarchy | headless | PASS | PASS | PASS |
| Anchored spherical mesh-tier contract | headless | PASS | PASS | PASS |
| M12D-P2S2 spherical billboard topology proof | headless | PASS | PASS | PASS |
| M12D-P2S3 spherical billboard GPU runtime proof | gpu | PASS | PASS | PASS |
| M12D-P2S4 canonical natural terrain billboard binding | gpu | PASS | PASS | PASS |
| M12D-P2S5C production spherical billboard runtime | gpu | PASS | PASS | PASS |
| M12D-P2S5B production spherical billboard topology library | headless | PASS | PASS | PASS |
| M12D-P2S5F nested production scale-mesh topology | headless | PASS | PASS | PASS |
| M12D-P2S5G bounded spherical billboard surface interface | headless | PASS | PASS | PASS |
| GPU physical-height preparation | gpu | PASS | PASS | PASS |
| Multiscale physical terrain modifier foundation | headless | PASS | PASS | PASS |
| Single canonical physical surface authority | headless | PASS | PASS | PASS |
| Global/anchored physical frequency continuity | headless | PASS | PASS | PASS |
| M12D-P2A canonical hashed cell field proof | gpu | PASS | PASS | PASS |
| M12D-P2B multiscale natural terrain family proof | gpu | PASS | PASS | PASS |
| M12D-P2C1 prepared natural terrain | gpu | PASS | PASS | PASS |
| Generation-4 physical renderer integration | headless | PASS | PASS | PASS |
| Production material noise value preservation | headless | PASS | PASS | PASS |
| Displaced mesh and physical normals | gpu | PASS | PASS | PASS |
| Screen-space subdivision | headless | PASS | PASS | PASS |
| Terrain-v5 seams, mixed-LOD authority, and Florida classification | headless | PASS | PASS | PASS |
| Terrain asset distribution boundary | headless | PASS | PASS | PASS |
| Cache lifecycle cleanup | headless | PASS | PASS | PASS |
| Local terrain format and GPU compression | headless | PASS | PASS | PASS |
| M12 Florida regional physical surface | headless | PASS | PASS | PASS |
| Production cube-sphere GPU residency integration | headless | PASS | PASS | PASS |
| Production physical-normal tangent continuity | headless | PASS | PASS | PASS |
| Production surface body eligibility and transition ownership | headless | PASS | PASS | PASS |
| Production Earth material-state continuity | headless | PASS | PASS | PASS |
| Planetary camera terrain exclusion | headless | PASS | PASS | PASS |
| Close-ground reference-frame diagnostic | headless | PASS | PASS | PASS |
| Production terrain material synthesis | headless | PASS | PASS | PASS |
| Planetary terrain residency and surface frame | headless | PASS | PASS | PASS |
| Planetary patch topology and ABI | headless | PASS | PASS | PASS |
| Parent-child LOD geographic correspondence | headless | PASS | PASS | PASS |
| Opaque distant-detailed handoff | headless | FAIL | PASS | PASS |
| Planetary representation handoff | headless | PASS | PASS | PASS |
| Distant quaternion transform parity | headless | PASS | PASS | PASS |
| Distant visible hemisphere winding | headless | PASS | PASS | PASS |
| Continuous Earth distance visibility | headless | PASS | PASS | PASS |
| Camera drag isolation | headless | PASS | PASS | PASS |
| Sol system presentation and focus | headless | PASS | PASS | PASS |
| SolAnalytical Earth planetary scene | headless | PASS | PASS | PASS |
| Florida foundation seating | headless | PASS | PASS | PASS |
<!-- CASE_TABLE_END -->

| Additional harness | Invariant / continuing responsibility | Category and state |
|---|---|---|
| NovaCoreRegionalPhysicalTests | Regional pack dependency/address demand and bounded residency accounting; current physical-data responsibility | HEADLESS; both configurations recorded in results |
| NovaCoreFacilityVisibilityTests | Authored caster CPU/GPU parity, ray bounds, inactive caster, final lighting composition | GPU-NO-WINDOW; 102 cases; default loader error / clean-profile pass |
| NovaCoreSurfaceMaterialCoordinatesTests | Final body-fixed material receiver coordinates under camera/radius/orientation changes | GPU-NO-WINDOW; 540 components; default loader error / clean-profile pass |
| EarthRouteValidation (`solar`, `florida`, `regional`, `regional-isolation`) | Deterministic focus/site transitions through current scene owner | Visible diagnostic driver; regional/isolation exercised by permanent test, solar/florida modes not separately rerun |
| ProductionBillboardDesktopTraversal | Pupil/LOD/ownership, horizon/raster/directional/residency probes and frame readbacks | Visible diagnostic, opt-in; not an always-on suite or automatic manual pass; retained reproducibility tooling |
| SolarWarpVulkanTraversal | Time-warp/scene presentation stress | Visible diagnostic, opt-in; not rerun for validation-only work |
| SphericalBillboardGpuProof / NaturalTerrainProof | Existing offscreen parity and readiness tooling | GPU-NO-WINDOW; test-used paths exercised, sample CLI aliases not a separate acceptance claim |
| Topology generators / artifact flags | Immutable topology generation, serialization and provenance | HEADLESS regeneration or explicit fixture-input mode; output disabled by default |
| Launcher tests and six preset probes | Normal route construction and deployment ownership | HEADLESS configuration/probe checks; not rerun because no launcher/route selection or ordinary deployment-resolution behavior changed |
| P2S5H Florida manual gate | Continuous player-facing contact and presentation quality | MANUAL; accepted milestone preserved; not required or rerun here |

## Final results and limits

Counts are pass / fail / skip. Default-host failures remain authoritative; the
comparison profiles do not overwrite them.

| Category | Debug | Release |
|---|---|---|
| HEADLESS managed | 78 / 0 / 0 | 78 / 0 / 0 |
| GPU-NO-WINDOW managed | 8 / 0 / 0 | 8 / 0 / 0 |
| VISIBLE-WINDOW automated, normal host | 0 / 3 / 0 (KMT) | 3 / 0 / 0 |
| Native HEADLESS | 1 / 0 / 0 | 1 / 0 / 0 |
| Native GPU, normal host | 0 / 2 / 0 (missing overlay manifests) | 0 / 2 / 0 (same) |
| Native GPU, clean implicit discovery comparison | 2 / 0 / 0 | 2 / 0 / 0 |
| Debug window, only OBS Vulkan hook disabled | 3 / 0 / 0 | Not needed |
| MANUAL | Not required; no new manual claim | Not required; P2S5H preserved |

Initial isolated Debug: 85/86 passed; only the stale draw-order string failed.
Its original default topology mode regenerated all 18 scales, including S17 hash
`BCE444AFFB2D713B`, 712,106 vertices and 1,424,208 triangles. The 554,579,640
serialized bytes were in memory, not a new archive. Final bounded suites used the
existing explicit artifact-input mode and retained all numerical/topology checks.

Final Debug ran all 89 cases together after the correction. Release ran the full
88-case suite before adding the wheel regression (87 pass, one regional-coverage
failure), then all three corrected window cases; the unchanged headless/GPU cases
were not redundantly regenerated. Complete current case coverage is 89 per configuration.
Managed solution Debug/Release and native Debug/Release builds passed; managed
builds report zero warnings/errors. Native permanent test targets also built in
both configurations. `git diff --check` passed.

Release live support: 43 captures / 1,587 prepared vertices, levels 8â€“17, 40 pupil
identities; max contact gap 0.000001638196409 m; max prepared-base error
0.000997482451342 m. Debug with the OBS-layer comparison also passed the unchanged
physical assertions (max contact gap 0.000003124587238 m). The default Debug
regional case stopped on its VUID before numerical analysis; it is not counted as
a physical-regression pass.

Each final test-native module and sample-native module matched its configuration's
build SHA; each sample's 49 shaders matched the compiled inputs. Exact paths,
SHA-256 values, case inventory and failure messages are retained in
[results.json](engineering-evidence/graphics-validation/results.json).
Launcher selection and ordinary runtime deployment resolution were not changed,
so the conditional 15-launcher/six-route campaign was not invoked. The direct
ordinary-input regression verifies wheel zoom remains enabled. No terrain,
facility, shader, residency, TES, P3 scheduling, precision or quality change exists.

Comparison recipes (process-local environment only, not accepted production defaults):

```powershell
$env:VK_LOADER_LAYERS_DISABLE = 'VK_LAYER_OBS_HOOK'
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --category=window
Remove-Item Env:VK_LOADER_LAYERS_DISABLE
# For the two standalone native tests only: create an empty temporary folder,
# set VK_IMPLICIT_LAYER_PATH to it, run both tests with their matching SPIR-V,
# then remove the process override and empty folder. Khronos validation remains explicit.
```

Grid startup, intermittent KMT caller attribution and stale host overlay registrations
remain explicit follow-up responsibilities. The package improves the ability to
detect them; it does not close them by accepting error output. No full, unconditional
Graphics/Vulkan PASS is claimed for the default host environment.

For KMT: preserve the exact VUID, module identity and mode; reproduce with the OBS
Vulkan hook enabled while collecting a call stack or API dump at the import,
then compare a supported hook/driver revision and assign the exact caller. Risk: genuine invalid
external-memory import; missing proof: caller and valid memory-type properties.
For overlays: repair/remove the stale registrations through their owning installer
in a separate authorized host task, then rerun the two native tests with normal
discovery. For grid: decide generic versus planetary descriptor/resource readiness
without giving non-Earth demo scenes fake physical authority, then add a startup
regression. None requires weakening current Earth geometry or physical contracts.

Final classification: **PARTIAL GRAPHICS RESOLUTION â€” FOLLOW-UP REQUIRED**.

## Evidence lifecycle

305 explicitly inventoried, Git-ignored files were retired: **12,710,158 logical
bytes**. Groups: resolved terrain readbacks 3,692,248 bytes / 152 files; temporary
alternate test deployment 8,194,635 bytes / 21 files; logs and review scripts
823,275 bytes / 132 files. Every leaf and ancestor was checked against the build
boundary and reparse points. Only empty directories were removed afterward.
This is measured retirement, not a cumulative-write or allocated-disk estimate.
Successful regional tests also automatically retired their fresh readbacks; their
bytes were not metered before deletion. Total generated output was therefore at
least the measured amount, plus those unmetered temporary probes.

Permanent evidence retained: **128226 logical bytes** for this report and
the compact JSON inventory/provenance. Temporary raw paths in provenance are
intentionally retired; required fixture inputs and reproduction code remain live.

After cleanup the ordinary Release Florida route completed 120 frames, exit 0,
with exactly one visible Earth owner in every observed submission and transition
from legitimate global bootstrap to NCSM1. The normal Release launcher exists;
all 49 deployed shader hashes still match. Native Release SHA-256 is
`120b4a2c27a9193c9acd9e7b70a2a07e52bc0f70ec8514a086c2a42bfa7ee5de`.
No cache/runtime/asset rebuild or raw recapture was needed. This bounded smoke is
not new physical/manual acceptance.

No screenshots, videos or full GPU attachment archives were needed. Permanent
evidence budget: 256 KiB for this report plus compact result/provenance JSON.
Production assets, all small permanent fixtures and diagnostic tooling are retained.
Failed regional readbacks can be retired after their compact coverage/input summary
and reproduction recipe are verified; they are not permanent fixtures. No cache,
Git pack, LFS object, shader or terrain asset is a cleanup target.

Primary external references: [Khronos layer discovery](https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderLayerInterface.md)
defines the process-local implicit-layer search override used for the comparison;
[VkMemoryAllocateInfo](https://docs.vulkan.org/refpages/latest/refpages/source/VkMemoryAllocateInfo.html)
defines the Win32 imported-memory type constraint. Neither proves that the observed
KMT error is benign.
