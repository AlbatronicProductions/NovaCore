# NovaCore engineering handoff

This document is the concise workflow handoff. The authoritative current
architecture and milestone state is
[NOVACORE_CURRENT_STATE.md](NOVACORE_CURRENT_STATE.md). Durable scope,
acceptance, KSA-reference, and authority rules are in
[ENGINEERING_RULES.md](../ENGINEERING_RULES.md). Do not reproduce their full
terrain narrative here.

Start with the [repository ownership map](repository-structure.md) for placement,
project dependencies, fixtures and generated state. Repository-local `.codex/` is
ignored scratch, not session authority. Investigation archives are indexed under
[engineering history](engineering-evidence/README.md); consult them for provenance,
not as an onboarding requirement.

## Repository rules

**M13 — NCSM1 Terrain Performance is CLOSED. There is no M13.7.**
M13.6 is the closed renderer baseline, commit
`90fef759243dd67918cd556e19027159e5a5eada`, tag
`m13.6-cpu-cached-terrain-residency-keys`. No unbanked M13 candidate or temporary
comparison bridge is awaiting preservation/acceptance. The separately authorized
Surface Interaction / Launch Foundation has banked **M14.1**, commit
`5b9b02ab1d2ba0e903809510908e4819984d4de2`, tag
`m14.1-canonical-surface-point-queries`, and **M14.2**, commit
`caa6d93ccd7c4e5924381e077a62585f99e5a32a`, tag
`m14.2-spacecraft-translational-authority`, and **M14.3**, commit
`79978cf8b0fd783d43a661df6df53a2c0202af4a`, tag
`m14.3-spacecraft-terrain-contact-observations`, and **M14.4**, commit
`28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c`, tag
`m14.4-atomic-contact-response`, and **M14.5**, commit
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, tag
`m14.5-isolated-analytical-contact-response`, and **M14.6 — Establish internal exact
physical-event epochs**, commit `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`, tag
`m14.6-physical-event-epochs`. M14 remains **OPEN**.

M14.1 through M14.6 are banked. There is no current unbanked development branch
or candidate. Begin from main and the [banked epoch contract](physical-event-epochs.md).
Internal canonical/reduced-rational identity and exact deterministic ordering exist;
public `SimulationInstant` and canonical publication remain integral. General
event-local transient execution and automatic contact discovery do not exist.

M14.5 still admits only one qualified authored point at represented canonical
`radialSignedGap == 0`: positive gap gives no response, negative gap requires
unsupported recovery. It is frictionless with restitution zero, no penetration
recovery, general multi-contact solver, persistent support/grounding or automatic
touchdown. **Exact event time is not exact representable event state**: M14.6 does
not guarantee an FP64 impact state satisfying strict represented-zero admission.
A certified event-local physical-evaluation/state contract remains absent.

The 8,160-byte investigation is resolved as **CANDIDATE-TRIGGERED CLR
ALLOCATION-CONTEXT ACCOUNTING EFFECT**, not a production allocation regression or
claimed CLR bug. The ordinary zero-allocation measurement class was hardened
without production changes. Final Simulation passed 5/5 Debug and 5/5 Release,
37/37 groups each; all 31 migrated windows measured zero. Fresh real-allocation
controls passed 3/3 per configuration, detecting 152 bytes each. Focused epoch,
ReferenceFrames, Precision and M14.2–M14.5 gates passed. The five unchanged timed
windows are not an active blocker. See [current validation and scope](NOVACORE_CURRENT_STATE.md#accepted-m146-validation-and-measurement-correction)
and [final evidence](engineering-evidence/physical-event-epoch/ordinary-class-migration/README.md).

No next milestone number or production responsibility has been assigned.
Project Control chooses the next responsibility from current banked truth.

- Work in `E:\NovaCore` and inspect `git status --short` before editing.
- Preserve unrelated and unstaged user work.
- Do not stage, commit, push, or tag without explicit instruction.
- Do not commit a rendering milestone before its required physical Desktop
  acceptance unless the user explicitly changes that gate.
- Generated terrain payloads belong in the manifest-managed cache, not ordinary
  Git history or build output.
- Follow the authoritative [diagnostic evidence lifecycle](../ENGINEERING_RULES.md#diagnostic-evidence-lifecycle):
  keep reproducibility, consolidate accepted evidence before banking, retire
  reviewed bulk output, and report storage totals for capture-heavy tickets.
- Use bounded tickets. If implementation exposes an authority conflict or needs
  a broader architecture change, stop and report it.

## Current planetary checkpoint

**NCSM1 / New Earth Renderer** remains the production Earth owner for all six
supported Earth/Solar/Florida routes. Banked M13.6 preserves accepted Florida
presentation and surface/orbit continuity.

Preserve the 18-level immutable topology library, exact-lattice moving/snapped
pupil, canonical `H(bodyDirection)`, prepared full physical relief and normals,
regional/facility participation, conservative curved-patch visibility,
compaction, KSA-parity bounded hardware tessellation, ordinary TES interpolation,
zero-visible re-entry and fence-confirmed atomic current/incoming publication.

Ordinary shading avoids unused diagnostic work and zero-contribution material
noise. GPU-consumed terrain working data prefers compatible device-local memory;
CPU-read residency keys prefer compatible cached memory. Both retain coherent
host mapping, compatible fallback and existing synchronization/lifetime contracts.

M13 did not establish universal 8.33 ms performance. Regional replacement remains
the principal measured residual limitation; no remaining single bounded,
quality-preserving >=1.5 ms recovery qualified for continuation. The
[current-state envelope](NOVACORE_CURRENT_STATE.md#final-accepted-performance-envelope)
is the concise performance checkpoint. Historical LiveKernelEvent 141 remains
**UNRESOLVED / NOT REPRODUCED / RESIDUAL UNCERTAINTY ACCEPTED**, as recorded in
[current state](NOVACORE_CURRENT_STATE.md#historical-livekernelevent-141-disposition).

## Physical and presentation boundary

- FP64 body-fixed height, displacement, physical normals, collision, clearance,
  and physical queries are one canonical physical authority. Full relief is
  prepared before TES; ordinary TES interpolates the published render surface.
  Gameplay and clearance retain full H queries, not interpolated render geometry.
- Material shading may consume cheaper deterministic derived classification.
- No material, fallback, LOD, cache, or renderer path may become a second
  physical authority.
- A complete outgoing representation remains authoritative until the incoming
  prepared geometry and draw payload are complete, GPU-ready, and
  fence-confirmed.
- Topology density and pupil triangulation describe presentation, not physical
  truth. `H(bodyDirection)` and the FP64 body-fixed point remain authoritative.
- Historical continuity measurements show a rare full pupil rebase may change
  the coarse factor-1 triangulated approximation by up to approximately 2.595 m;
  the adjacent L14→L15 difference
  is approximately 2.8 mm. Treat these as deferred presentation/morph behavior,
  not moving physical terrain or lost ownership.

## Next architectural decision and work boundary

M13 is closed at M13.6; M14 remains open with M14.1–M14.6 banked. There is no
next milestone assignment or unbanked production candidate. Project Control must
choose the next responsibility. Do not start implementation from a provisional
research ranking or treat this documentation correction as engineering authority.

- Preserve banked M13.1–M13.6 outcomes and future tessellation/displacement capacity.
- Historical exact-copy, residual-validation and lifecycle experiments are evidence,
  not approved production candidates. Do not restore rejected mechanisms.
- Do not reopen terrain optimization merely because further improvement is possible.
- Do not regenerate NCSM1 assets, enlarge the 50 m refinement range, weaken
  conservative visibility or create another physical/terrain owner without an
  explicitly authorized bounded responsibility and measured evidence.
- Keep canonical physical truth distinct from material quality and
  presentation/LOD continuity. Current limitations are in
  [current state](NOVACORE_CURRENT_STATE.md#known-debt).

Canonical Graphics validation remains strict and isolates third-party implicit
layers; ambient OBS/Epic interoperability remains observable under the
[Graphics Package 2 contract](graphics-validation-package-2.md).

## KSA reference workflow

For analogous planetary work, inspect the relevant current installation under
`E:\Kitten Space Agency\` before selecting a responsibility boundary. Record
what problem the technique solves, its pipeline stage and cadence, prepared
data, reuse, synchronization, continuity, and cost bound. Then classify the
NovaCore decision as adopt, adapt, or intentionally differ. Intentional
difference requires measured NovaCore evidence.

KSA is reference evidence only. Do not copy source, shaders, assets, constants,
or proprietary data; do not expose substantial KSA source in documentation or
create a runtime dependency.

## Validation order

1. Run focused parity/build/tests for the bounded change.
2. Run the canonical fixed-pose benchmark only when the workload fingerprint is
   equivalent.
3. Run focused dynamic validation for publication, coverage, and Vulkan state.
4. Run the required 3440x1440 physical trajectory and stop for manual Desktop
   acceptance.
5. Run broad regression only at the requested checkpoint.

A fixed-pose benchmark pass does not imply player-facing acceptance.

## Common commands

```powershell
dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.Launcher.Tests -c Debug
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=m12d-production-spherical-billboard --altitude=10.004 --p2s5c3-traversal --log=validation
dotnet run --project samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Debug -- --scene=sol --log=validation
git diff --check
git status --short
```

Configure and build native code from an x64 Visual Studio developer environment
using `build/native-ninja`. Resolve production assets with
`NovaCore.AssetTool`; runtime performs no implicit network acquisition. For
point-and-click physical acceptance, use **New Earth Renderer** in the
launcher. A launcher built in Release starts the Release Triangle runtime; a
launcher built in Debug starts Debug.

Unrelated retirement findings are tracked in [the subsequent debt ledger](repository-debt-retirement.md). Deferral is not permanent acceptance.
