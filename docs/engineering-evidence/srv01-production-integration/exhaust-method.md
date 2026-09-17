# SRV-01 current-KSA plume mechanism alignment

2026-09-17. This is the continuation authorized by the Project Control mechanism
clarification. No bank, physics change, KSA write, or proprietary asset import.
The preceding capacity/lifecycle correction and its manual PASS remain historical.
Project Control personally rechecked this final renderer revision and answered
**PASS** on 2026-09-17. The qualified source/build identities and manual runtime
witness are retained in final-identities.json and method-results.json.

## Direct current evidence

Installed root: `E:\Kitten Space Agency`. KSA.dll version
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA256
`A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`.
Current installed shader/configuration files and DLL metadata were read directly.
`exhaust-ksa-identities.json` and `exhaust-method-identities.json` pin the sources.
Direct official history remains the four messages linked in exhaust-correction.md;
no newer-history behavior is assigned to installed build 5438.

Current source witnesses (relative to that installation):

- `Content/Core/Shaders/VolumetricExhaust/VolumetricExhaust.vert:22-63`:
  three-station proxy deformation and nozzle-axis basis. Current constructor
  `06002337:0731` loads `BoxWithMidSeam`; installed mesh is 2,068 bytes, 32 position
  entries and 60 indices, stations -0.5/0/+0.5. It bounds integration, not an opaque cone.
- `VolumetricExhaust.frag:158-181,204-406`: expanding core radius, radial/axial
  density, outer shell, emission distinct from absorption, and original-template
  colors. `PhysicalDensity.glsl`: inverse-area and Mach-dependent density.
- `ExhaustGasDynamics.ComputePlumeRadius` 06002306: radius is nozzle radius plus
  a slope term and a curvature term proportional to axial distance to power 1.5.
  Vacuum curvature is zero (`ComputePlumeCurveCoeff` 06002305).
- `ComputeCoreLength` 0600230F and `ComputeVacuumPlumeLengthForDensity` 0600230B:
  the visible length is the smaller of template extent and density cutoff.
  `AddInstanceCore` 0600234B distinguishes the physical core, density-clipped
  visible limits and conservatively padded proxy.
- `PlumeData.Compute` 06002324: pressure, gas, Mach and exit conditions seed visual
  geometry/density. Its recomputed FX mass flow is a visual surrogate; it is not
  the canonical consumed resource. Main/RCS configurations separately permit FX
  diameter to differ from physical nozzle diameter.
- `VolumetricExhaust.frag:80-103,253-309`: density/shape/surface noise is sampled
  in emission-time and azimuthal coordinates, transported along the nozzle axis.
- `VolumetricExhaust.frag:409-442,522-573`: emitter/shock importance sampling,
  Beer transmission, independent radiance integration and zero-absorption limit.
- `VolumetricExhaust.frag:581-647`: opaque-depth clipping, nozzle-local ray bounds,
  template sample counts and spatial dithering. `VolumeBounds.glsl` bounds the
  expansion and tail; it also supports a bell-interior extension.
- `VolumetricExhaust.frag:33-35,468-488,671-673`, `ExhaustOit.comp:35-39`,
  `Upscaling/Compose.comp`, and `TransparenciesMsaa/TransparencyMsaaCompose.glsl`:
  weighted blended transparency, multiplicative transmission, normalized radiance,
  then opaque background times transmission plus radiance.
- `CreateRenderingPass` 06002363:036C/0388/03A7 confirms additive radiance/weight,
  multiplicative transmission and weighted-depth attachments in the current DLL.
  `Render` 0600234E:0359/0366/0394 performs normalize, denoise/upscale, composition.
- `UpdateFrameData` 0600233B:0031-0041 takes universe elapsed seconds for shader
  animation. Pre-render owns gathering/render work; it is not a solver-tick loop.
- `ExhaustAssets.xml:3-49,307-355`: RCS/main use the same material architecture
  with separate profiles. EngineALarge has zero absorption; RCS separates
  absorption, emission and refraction and suppresses diamond brightness.

## Decision by equivalent responsibility

| Responsibility | Classification | Final NovaCore method / bounded difference |
|---|---|---|
| Volume representation | ADAPT | Independently authored closed proxy bounds a nozzle-local density volume. A conservative box dispatches fragments; analytic capped-frustum intersections delimit vacuum integration. No KSA mesh. |
| Axial/radial field | ADAPT | Vacuum straight expansion, inverse-area radial dilution, density visibility threshold and template extent cap. No Gaussian cone. Original normalized appearance profile, with no claim of calibrated gas dynamics. |
| Core / outer structure | ADAPT | Smooth attached core and outer emissive wall share the expanding field; finite axial and radial falloff. Original colors and coefficients. |
| Optical transport | ADOPT | Emission and extinction are independent; integrate radiance with Beer transmission, including the exact zero-extinction limit. Main is pure emission; RCS has a separate weak extinction profile. |
| Overlap/composition | ADAPT | Weighted blended OIT with summed optical depth: exp(-sum(depth)) equals product transmission. Resolve before existing ACES/sRGB conversion. No sorting or purely additive replacement. |
| Noise | ADAPT | Independently generated continuous value noise in advected emission-time and seamless radial coordinates; stable nozzle seed, no copied texture. |
| Emitter importance | ADAPT | Equal-angle integration intervals concentrate samples near the nozzle, with bounded static pixel stratification. Forty samples at full resolution; no temporal/upscale filtering requirement. |
| Depth / occlusion | ADOPT | Clip the actual integration segment to opaque depth, not the proxy surface. No depth writes by exhaust. |
| Main / RCS binding | ADOPT | One realized producer per physical tuple, same renderer, distinct profile. Main seed 0; jet seeds 1-16 remain stable as active lists change. |
| Thrust / throttle | ADAPT | Consume actual binary output and immutable qualified exhaust velocity. No continuous throttle exists to modulate. No requested-command or mesh-derived force. |
| Time and ownership | ADOPT | Copied committed simulation time drives animation; one draw submission per display frame. No private renderer clock or work multiplied by ticks. |
| Width / length / pressure | ADAPT | Original bounded vacuum profile supplies visual expansion/visibility parameters. Pressure/gas/Mach-dependent modulation is unavailable in this admitted scene and is not fabricated. Environmental expansion remains a separate future authority. |
| Lifetime | ADAPT | Prepared immutable resources and per-nozzle tuple state; immediate off on actual shutdown or terminal presentation. Ticket explicitly forbids stale exhaust, so no KSA shutdown tails/stochastic duty-cycle policy. |
| Refraction / scattering / wind / merging | ADAPT (outside admitted subset) | No atmospheric scene/gas authority, plume trails, interacting engine cluster or current refraction requirement. These are explicitly absent, not claimed equivalent implementations. |

There is no assertion of full KSA gas-model equivalence, copied exact artistic
appearance, or atmospheric qualification. The original static/Gaussian effect is
superseded. The supported equivalent responsibility is nozzle-local vacuum volume
presentation, not a new physical nozzle solver. Canonical resource/force authority
and all accepted reusable GLB bytes remain unchanged.

## Original profile and transport

The independently authored normalized profile uses exit radius 0.32, expansion
slope 0.60, relative visibility cutoff 0.25 and a unit template cap. Visibility
length is min(1, (0.32/0.60)*(sqrt(2/0.25)-1)), from the field's on-axis inverse-area
cutoff. Radial visibility further trims the outer field. Main/RCS physical-scale
presentation caps remain 1.35/0.36 m with radial scales 0.28/0.095 m. These are
visual calibration, not measured temperature/pressure or a physical plume claim.
The immutable qualified engine exhaust speed is transported explicitly; an
original 0.0004 visual slowdown makes its advection readable at this scale.

The existing reserved 12 bytes after each object mesh handle carry copied time,
exhaust-speed bits and stable nozzle index only on this typed visual route. The
80-byte object, broad frame ABI and the 54-object/4,416-byte prepared transport
contract remain unchanged. No canonical mutation or per-frame allocation.

Two render targets accumulate weighted radiance/weight and optical depth. Both
use identical additive blending; independentBlend is unnecessary. The opaque
scene is preserved through the volume subpass and has an explicit dependency to
resolve. Legacy routes retain their original two subpasses and tone shader.
Full-resolution OIT avoids KSA's extra weighted-depth/denoise/upscale machinery;
it does not promise exact transport of differently colored overlapping media.

## Validation interpretation

Permanent `ExhaustPresentationTests.cpp` executes deployed production SPIR-V and
reads back actual blend targets/resolve. It checks pure emission, independent
extinction, exponential background attenuation, order invariance, doubled optical
depth, empty-after-active clearing, depth before/through/behind the volume, and
axial/inside-camera views. It is not a CPU transcription of the shader.

The first new offscreen run passed its image assertions but reported three loader
errors for already-missing Epic implicit-overlay manifests. No Vulkan API VUID
was reported. The qualified run uses a process-local empty `VK_IMPLICIT_LAYER_PATH`
while explicitly retaining Khronos validation; no registry/security setting or
installed file changed. This supported loader override is documented by
[Khronos](https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderLayerInterface.md).
Original failure remains retained in method-results.json. No validation-error
counter was weakened or reset to obtain a pass.
