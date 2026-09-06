# Graphics Validation Package 2

Unbanked follow-up, 2026-09-06. Package 1 and accepted P2S5H are preserved.
Operational policy and commands have one owner:
[Graphics validation contract](build-windows.md#graphics-validation-contract).
The bounded machine-readable evidence is in
[package-2.json](engineering-evidence/graphics-validation/package-2.json).

## Baseline and scope

- Initial HEAD, origin/main and live remote main:
  `e963e8cf9ed5841f76a5f954b0db37162d3a93df` (banked Package 1).
- Branch `main`; initial `git status --short` empty.
- P2S5H dereferenced tag: `32ffac50ab5c06518ede24edfb5c531976d4ec99`.
- No Git staging, banking, history/cache maintenance, structure consolidation,
  renderer redesign, shader edits or performance work.

Runtime changes are confined to Earth-oracle startup ownership and an opt-in
validation-error call stack. All other changes are validation harness/regression
and documentation/evidence. No terrain/facility/NCSM1/TES algorithm changed.

## Vulkan environment and decision

The measured loader is 1.4.309; the installed SDK is 1.4.357.0. The device is
AMD Radeon RX 6800 XT, AMD Vulkan driver 26.8.1, API 1.4.315. The integrated AMD
GPU remains discoverable. Canonical isolation does not replace or select the ICD.

The SDK's nine explicit registrations are Khronos validation, profiles,
shader-object, synchronization2; LunarG api-dump, gfxreconstruct, monitor,
screenshot and crash-diagnostic. Their manifests and binaries exist. Registration
value zero makes a manifest discoverable, not necessarily active. Only Khronos is
requested by canonical validation. The inventory retains exact paths and hashes.

| Implicit responsibility | Registration and target | Measured activation |
|---|---|---|
| OBS `VK_LAYER_OBS_HOOK` | HKLM 64/32-bit Khronos keys; `C:\ProgramData\obs-studio-hook\obs-vulkan64.json` / 32-bit counterpart; `graphics-hook64.dll` exists | Instance/device layer inserted in ambient and Epic-clean cases; absent from chain when OBS-disabled or canonical |
| AMD switchable graphics | Display-adapter keys `0002`/`0003`, `VulkanImplicitLayers`/`VulkanImplicitLayersWow`; driver-store `amd-vulkan64.json` / 32-bit counterpart; `amdvlk64.dll` exists | Inserted in ambient, OBS-disabled and Epic-clean; canonical still loads the AMD ICD without this implicit layer |
| Steam overlay / fossilize | HKLM 64/32-bit Khronos keys; Steam installation manifests/binaries exist | Listed but not inserted for these non-Steam processes; activation conditions preserved in inventory |
| Epic EOS | HKLM missing managedArtifacts manifest and HKCU two missing Launcher/Portal manifests | Discovery errors; no Epic layer insertion or module loading inferred from registration |

Exact stale 64-bit-discovery paths:

1. `C:\Program Files (x86)\Epic Games\Epic Online Services\managedArtifacts\98bc04bc842e4906993fd6d6644ffb8d\EOSOverlayVkLayer-Win64.json`
2. `E:\Epic Games\Launcher\Portal\Extras\Overlay\EOSOverlayVkLayer-Win32.json`
3. `E:\Epic Games\Launcher\Portal\Extras\Overlay\EOSOverlayVkLayer-Win64.json`

The second entry is itself a 32-bit-named manifest registered in the 64-bit user
view. There is also a missing HKLM WOW6432Node EOS Win32 target, outside these
x64 executions. No registry key or third-party installation was edited.

| Profile | Exact layer-discovery difference from ambient |
|---|---|
| Ambient | Actual user registrations and activation; OBS running with the user's capture setup |
| Canonical | Fresh empty implicit directory; only SDK Khronos explicit manifest, absolute real DLL path; inherited activation/validation-disable settings cleared; Khronos explicitly forced in both configurations |
| OBS-disabled | Only `VK_LOADER_LAYERS_DISABLE=VK_LAYER_OBS_HOOK`; stale Epic discovery and AMD remain |
| Epic-clean | Process-local implicit directory containing the same valid OBS, Steam and AMD manifests, with absolute binary paths; missing Epic entries absent; explicit discovery unchanged |

The Epic-clean comparison initially omitted the driver-registered AMD manifest;
that incomplete profile was corrected before the final comparison matrix. Final
loader traces prove AMD and OBS both remain in the set of valid layers.
Directory-based discovery also changes their instance-layer insertion order
(ambient: OBS then AMD; Epic-clean: AMD then OBS). Device insertion remains
Khronos then OBS. This ordering difference is retained explicitly; the Epic-clean
window comparison is not used to isolate the KMT subcause.

Khronos documents these process-local discovery overrides and their elevated
process limitation in the [loader layer interface](https://github.com/KhronosGroup/Vulkan-Loader/blob/main/docs/LoaderLayerInterface.md).
Canonical runs fail immediately when elevated or when discovery exposes a foreign
layer. They require the SDK manifest/DLL and verify actual window-process module
identity. No VUID is suppressed. The sole pre-existing exact unused vertex-output
warning remains logged and narrowly checked as in Package 1.

`--ambient` is a diagnostic boundary, not an alternate definition of regression
success. It reports registrations, manifest/binary existence, loader-visible
names, loaded window modules and unchanged strict test results. Loader debug traces
prove layer-chain insertion separately from DLL presence: OBS can inject its DLL
for other graphics APIs even when its Vulkan layer is disabled.

## OBS KMT causal classification

**B — third-party implicit-layer interference.** The exact diagnostic remains:

`VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645`: `memoryTypeIndex=0`,
`VkMemoryWin32HandlePropertiesKHR::memoryTypeBits=0x0`,
`VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_TEXTURE_KMT_BIT`.

The failing callback's stack leads from Khronos validation directly to
`C:\ProgramData\obs-studio-hook\graphics-hook64.dll+0xb16b`, then NovaCore's
presentation call. Hook SHA-256:
`49c0ddeac72b130d4f8ae90510219949022c1f992adb48d81a4972f2cd6c2585`.
The installed binary's instruction at RVA `0xb167` performs the indirect call;
`0xb16b` is its return address. Native source contains no D3D11/KMT/import-memory
operation; it submits `vkQueuePresentKHR` for its live swapchain after rendering.

OBS's [published Vulkan capture implementation](https://github.com/obsproject/obs-studio/blob/32.0.4/plugins/win-capture/graphics-hook/vulkan-capture.c)
creates and owns a shared D3D11 capture texture, obtains its shared handle, builds
the Vulkan import allocation and selects memory from image requirements. This
explains the measured binary call pattern; that published version is architectural
corroboration, not a claim that its source exactly matches the installed binary.
The import resource belongs to OBS capture initialization, not NovaCore's terrain,
facility or swapchain-image allocation. Capture-resource lifecycle belongs to OBS.
The callback supplied no named/object-handle entries, so no numeric imported handle
or exact texture instance is fabricated. The invalid type constraint is measured;
the reason the external handle query returns zero bits is not established here.

With identical NovaCore binaries/shaders, disabling only OBS's Vulkan layer removes
this VUID. The Epic-clean window comparison passed with OBS still active; because
the import is intermittent and instance-layer order differs, that pass is not
evidence that stale Epic registrations caused or repaired the OBS import.
Canonical presentation and repeated resize/minimize/restore/teardown are clean.
No NovaCore invalid-import parameter or resource-lifetime defect is evidenced.
The validation failure remains fatal to the ambient test runner, even if the
underlying interactive runtime continues after logging the callback.

No production workaround is introduced. `NOVACORE_VULKAN_CALLSTACK=1` logs error
objects supplied by validation and module-relative return addresses; it changes
neither severity, callback return, geometry nor synchronization.

## Grid startup ownership

`SampleOptions` defaults to grid. Grid, frames, fixture, fixture-dynamic and
celestial paths pass no `NativeRuntimeAssets`; only Earth/Solar resolve the Earth
pack and elevation path. The entry calls `nc_run_renderer`, then shared
`RunRenderer -> CreateSubmission`. Before this correction, `CreateSubmission`
unconditionally required/read the 67,108,864-byte Earth elevation oracle.

Binding 33 is consumed by `planetary_physical_authority.glsl` in physical terrain
preparation. Grid's triangle shader does not statically use it. Generic rendering
does not acquire an Earth physical responsibility merely by creating a Vulkan
device or selecting a physical-generation option.

The existing immutable production-pack owner now gates oracle allocation/read,
binding-33 publication and generation-4 global preparation scheduling. Earth's
missing/dimension-invalid oracle remains fatal. Generic paths leave the unused
binding unbound and schedule no Earth preparation. Null-safe teardown already
supports the absent buffer. There are no dummy paths/buffers or fallback heights.
The new grid/frames regression explicitly selects generation 4 and exercises
create, render/present, resize, minimize, restore and teardown for both routes.

Other generic paths share the corrected entry by inspection; they are not all
claimed as separately live-tested. Existing shared allocations/pipeline creation
are retained: decomposing all generic/planet resources would exceed this ticket.

KSA reference: current local `KSA.dll`, SHA-256
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`;
decompiled `KSA/Program.cs` window/renderer setup around 773–798 passes a window,
depth format, present mode and Vulkan version to the renderer, not Earth elevation.
**Adapt responsibility:** window/device startup is distinct from planet physical
data ownership. This supports the bounded owner gate; it does not justify copying
KSA code or undertaking a generic resource redesign. No KSA layer-isolation test
contract was established, and no history search was needed for this decision.

## Validation and evidence

| Surface | Debug pass/fail/skip | Release pass/fail/skip |
|---|---|---|
| Managed HEADLESS | 78 / 0 / 0 | 78 / 0 / 0 |
| Managed GPU, no window | 8 / 0 / 0 | 8 / 0 / 0 |
| Managed canonical visible window | 4 / 0 / 0 | 4 / 0 / 0 |
| Native CPU | 1 / 0 / 0 | 1 / 0 / 0 |
| Native canonical GPU | 2 / 0 / 0 | 2 / 0 / 0 |

| Ambient comparison | Debug production lifecycle | Native GPU Debug / Release |
|---|---|---|
| Actual ambient, OBS running | FAIL: KMT VUID | 0/2 pass in each: stale Epic discovery errors |
| OBS-disabled only | PASS | 0/2 pass in each: stale Epic discovery errors |
| Epic-clean, valid layers retained | PASS, with ordering limitation above | 2/2 pass in each |
| Canonical | PASS in both configurations | 2/2 pass in each |

All four native/managed configuration builds passed. The 15 launcher regressions
and six deployed `CreatePlan` probes passed. A 500-frame canonical Release Florida
smoke passed with zero VUIDs, all 670 regional dependencies ready, NCSM1 publication
and stable foundation seating at frame 440. It is automated scenario evidence,
not a new manual gate. No player-visible Earth change was introduced.

Both deployments match their native build hashes and all 49 shader hashes each:
Debug native `1bcbd28c20f70c2a284dce82ca14e8f279d9d7a2be998835528a2bca292724c5`;
Release native `3a9f6fbed66169dc4200ca86bb6b501233b900a77082b3d0b72151b043606cd5`.
Window tests record actual native and validation DLL paths/hashes. No residual
test process or window remained. Final preflight checks also passed with hostile
inherited `VK_LOADER_LAYERS_DISABLE=~all~` and a nonexistent requested layer;
the parent shell retained its original values and strict validation stayed active.

Complete 90-case suites used the final production runtime/shaders. Subsequent
harness-only inventory and preflight tightening received focused native GPU,
GPU-query, grid/frames, repeated lifecycle, message-policy and inherited-setting
rechecks; the physical test oracles and production artifacts did not change.

Final measured counts, comparison results, runtime/shader hashes, build summaries,
launcher routes, Florida smoke and storage accounting are recorded in the linked
JSON. Complete suites used the existing checked-in 18-scale topology library,
not topology regeneration. No skips, layer-error allowlists or performance claim.

Package 1 reports and evidence are preserved as historical records. Permanent
Package 2 evidence has a 256 KiB budget: report, compact case results, input/module
hashes, representative call stack, layer-chain excerpts and reproduction recipes.
Full temporary logs, copied loader manifests, probe binaries and resolved raw
readbacks are disposable. No asset, cache, generator, permanent fixture, current
deployment or pre-existing workspace output belongs to that cleanup.

## Remaining bounded responsibilities

- OBS/AMD external-import compatibility: OBS owns the intercepted capture import;
  the zero-bit handle-query subcause needs an upstream capture/driver investigation
  if ambient OBS Vulkan capture compatibility is required. Canonical validation
  does not certify that interoperability.
- Stale Epic registrations: software installation hygiene remains external;
  canonical tests neither repair nor require machine-wide registry edits.
- Generic/planetary shared allocations beyond the inappropriate oracle dependency:
  investigate separately; no evidence here justifies deleting shared resources.
- Other generic scenes have the same corrected startup owner by source inspection;
  only grid/frames received the new explicit live lifecycle coverage.

Repository structure consolidation, public compatibility retirement, cache policy,
Git/LFS storage and renderer/performance debt remain out of scope.
