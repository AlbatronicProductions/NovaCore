# Source-owned runtime shader deployment

Judgment: **PASS for this bounded deployment-ownership correction**, pending
Project Control acceptance. No milestone number assigned. No commit, tag, push or
bank. Recorded 2026-09-30 in `E:\NovaCore`.

## Identity and first gate

- Branch: `main`; initial working tree clean.
- HEAD: `b0ccfd8f17643868a8d9011968a4566bc8a13f69`.
- Immutable `m16.2` bank: `cd8fdcf977dee633f57da790e931a388b922048f`.
- Candidate is the uncommitted source change over that HEAD. Exact changed-source
  SHA-256 values: [source-identity.json](source-identity.json).

Before production changes, the native CMake graph, Triangle/ConstructionEditor/App
project chain, package verifier and M16.2 bank-preparation evidence were inspected.
The defect was present: Triangle collected `$(NativeRuntimeRoot)shaders\*.spv` with
`PreserveNewest`, then propagated that collection through project references.
`NovaCoreSurfaceMaterialTestShader` wrote into that same directory, although its
consumer was the separate `EXCLUDE_FROM_ALL` SurfaceMaterialCoordinates test
executable, not a dependency of `NovaCore.Native`. Native test execution could
therefore grow the next player package. Copy-only deployment also retained old
destination entries.

Existing source output and Release application contained 69 SPVs. The actual native
runtime target/output closure contained 66. The three outside that closure were
the retired `hud.vert.spv` / `hud.frag.spv` outputs and
`surface_material_coordinates_test.comp.spv`. The prior MSBuild FileListAbsolute
ledgers positively identified their destination ownership; matching native bytes
corroborated the legacy copies. The first Release rebuild removed three obsolete
owned entries from each canonical consumer. These are observations, not a fixed
count or filename-based deletion policy.

## Ownership and bounded implementation

1. `RuntimeShaderDeployment.cmake` wraps the existing shader producer/target
   declarations without changing shader sources, compiler commands or runtime
   algorithms. It traverses `NovaCore.Native`'s actual CMake dependencies and
   expands the complete OUTPUT group of each required producer. This preserves
   `prepared_surface.vert.spv` and both nested-scale cull siblings even when only
   another output from their command is an explicit target root.
2. CMake generates `runtime-shaders.json` with sorted logical membership,
   configuration and authority-source hashes. A link-time `native-runtime-build.json`
   records the native DLL hash and configuration. Canonical single-configuration
   Debug/Release roots are required; shared multi-config roots fail explicitly.
3. One shared MSBuild target invokes `deploy-runtime-shaders.py` for Triangle,
   ConstructionEditor and App. The generated list also supplies exact transitive
   Content items. Source/cache/configuration/link provenance is checked before
   destination changes. The selected DLL is always copied: switching native roots
   exposed a real `PreserveNewest` timestamp skip, which the strict verifier caught.
4. `novacore-runtime-shaders.json` owns deployed shader hashes. Unchanged obsolete
   owned entries are removed incrementally. Initial migration requires the exact
   old MSBuild destination ledger entry and matching native bytes. Unknown SPVs,
   modified obsolete files and unsafe/reparse paths are preserved and rejected.
   Unrelated files, saves, caches and stale native source outputs are not deleted.
5. Only the surface-material test producer moves to `test-shaders/`; its existing
   harness path follows. Facility was already produced there; its inherited wrong
   harness path is deliberately unchanged.
6. `verify-player-package.py` independently reconstructs the source-declared
   native target/output closure rather than trusting the generated manifest. It
   also checks named production consumers, including recursively included native
   `.inl` files, then exact deployed membership, hashes, configuration and DLL
   provenance. Existing assets/dependency/license checks remain active.

There is no second maintained name list or acceptance-count constant. Current
source declaration parsing is deliberately bounded to the repository's CMake
forms; an unresolved target fails verification. The consumer check supplements
the target owner; it does not remove target-owned exported/proof modules merely
because their shader paths are supplied by callers.

## Qualification results

| Check | Debug | Release |
|---|---|---|
| Fresh native root + cleaned managed App build | PASS, zero build errors | PASS, zero build errors |
| Repeated canonical incremental App build | PASS, zero warnings/errors | PASS, zero warnings/errors |
| Explicit native test targets + execution before App rebuild | Package PASS | Package PASS |
| Contaminated existing native shader output | Correct exact set | Correct exact set |
| Strict canonical package verification | PASS | PASS |
| Player configuration/presentation checks | 55 PASS | 55 PASS |
| Surface-material native GPU test | PASS, 540 components | PASS, 540 components |
| Stellar native GPU test | PASS, 24 raster cases | PASS, 24 raster cases |
| Facility native GPU test | FAIL, inherited missing path | FAIL, inherited missing path |

Fresh native builds used separate previously absent roots under
`build/shader-deployment/clean-native-{debug,release}` and generated all runtime
outputs. Managed outputs were cleaned with `dotnet clean`, then rebuilt against
those roots. Clean and canonical native DLL hashes differ; each matched its own
link receipt. No cross-root binary reproducibility claim is made.

All six clean/test-order/incremental receipts have identical runtime membership
and shader hashes within their configuration. Canonical post-test and repeated
incremental whole-package hashes also match. See
[scenario-comparison.json](scenario-comparison.json). The clean runs preceded the
DLL `Always` copy correction; the root-switch and subsequent canonical incremental
runs exercised that final correction. The shader deployment owner was unchanged.

Canonical native source directories still contain the three extra old outputs;
their presence cannot alter deployment. Native qualification targets now produce
the surface-material test in `test-shaders/`. Isolated fixtures additionally inject
arbitrary non-runtime source output and obsolete owned destination output.

### Negative tests and independent review

[negative-results.json](negative-results.json): **31 PASS** (15 per configuration
plus a reparse-attribute guard check). Cases cover baseline/idempotency, missing
source, missing/corrupt deployed shader and repair, unknown extra preservation and
refusal, owned retirement, modified obsolete preservation, traversal rejection,
source contamination, manifest and cache configuration mismatch, wrong-configuration
DLL, and legacy-ledger migration.

Two independent-oracle attacks pass their expected rejection assertions:

- Removing `prepared_surface.vert.spv` from generated authority and deployment
  cannot fool the verifier's source-declared multi-output closure.
- Removing `frozen_capture_pack.comp.spv` from source dependency authority,
  generated authority and deployment together still fails the native `.inl`
  runtime-consumer check.

The reparse guard case mocks Windows `lstat` attributes, including a dangling entry;
it is not a claim of a physically created Windows symlink test. This host denied
the reviewer's attempted symlink creation privilege.

A separate read-only red-team agent independently recomputed 66 target-owned
outputs / 61 named consumers and ran six isolated deployment attacks:
[independent-red-team.json](independent-red-team.json). It found an initially
omitted `.inl` consumer and a dangling-reparse guard gap; both were corrected and
covered by the permanent suite. Its final review judgment was PASS, including the
DLL copy correction. The producer graph retains five additional target-owned
outputs beyond the named consumer subset; no proof module was retired from runtime
ownership by filename inference.

### Ordinary player smoke

Ordinary canonical Release `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`
was launched without qualification switches or process environment overrides.
Physical UI actions at 3440 x 1440 exercised:

- Configuration START, loading and Exploration entry.
- Comma: 1x to 0.1x; comma: authoritative pause; period: resume at 0.1x.
- Escape / New Vehicle: ordinary Vehicle Editor.
- Save / Load: existing `AD4 Editor Route 20260929`, 12 parts, dry 504 kg,
  propellant 800 kg; saved file was not overwritten.
- Launch vehicle: rendered craft on the Florida development slab, Flight 1x.
- Escape: Simulation Paused / Flight Paused; Resume: Flight 1x.
- Alt+F4: ordinary exit; no NovaCore window remained.

No missing-shader error, new runtime defect or system anomaly was observed in this
bounded route. [Flight witness](flight-smoke.png) was saved at 17:17:45 EDT on
2026-09-30. This is a startup/editor/launch/pause smoke result, not exhaustive flight,
planetary fidelity, DPI or performance qualification.

## Receipts and inherited limits

- [Debug package](package-debug.json), [Release package](package-release.json):
  exact logical/deployed shader inventories and SHA-256 values, selected runtime
  provenance files, native manifest, link receipt and ignored source extras.
- [Scenario comparison](scenario-comparison.json): each original local package
  receipt hash, whole-package hash, native root and native DLL hash.
- [Selected command logs](logs/): compact build/test receipts, including failures.
  Full local package receipts remain under `build/shader-deployment/`.

An initial direct Release native GPU invocation encountered the ambient loader's
missing Epic implicit-layer manifest. Its log is retained. Repeating through the
existing repository CANONICAL Khronos-only harness (without global environment or
registry changes) produced the results above. Both canonical native GPU harness
runs therefore finish **2 PASS / 1 FAIL / 0 SKIP**, not an overall graphics PASS.
The facility failure is the already documented old `shaders/` argument against a
`test-shaders/` producer. It has not been weakened, waived or repaired here.

Inherited graphics assertions, full Graphics Release gate, facility qualification,
non-96-DPI qualification, 150 FPS, global/cold terrain fallback, planetary visuals,
blackout causality, MinimumRecorder disposition and public-release readiness remain
outside this correction. Existing M16.2 historical evidence is unchanged.

## Reproduction and next action

Use [commands.md](commands.md) from the canonical tree with the documented toolchain.
Source diff is limited to native deployment declarations/owner, shared deployment
integration in the three projects, the surface test path, strict verifier,
focused regression suite and these directly related docs/evidence. No shader source,
rendering, simulation or UI implementation changed.

Return to Project Control for acceptance of this uncommitted candidate. No next
front, publication or banking action is taken.
