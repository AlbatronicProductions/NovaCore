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

## Current candidate and stop boundary

The canonical SRV-01 surface-to-flight foundation (Stage1–5) has Project Control
manual acceptance **PASS** for the final simplified Florida slab route, including
**FL Launchpad → Play** and preserved Solar navigation. Use the
[final closeout](engineering-evidence/srv01-surface-to-flight-foundation-final/README.md),
not historical pending/failed child reports, to establish current candidate state.
Full final Debug/Release builds, Simulation80/80, Launcher18/18 and76 total gates pass.

Current baseline: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`.
Candidate: `codex/srv01-supported-contact-admission`, **UNBANKED**.
No milestone number; no stage/commit/tag/push/merge authority in this closeout.
Stop for Project Control's banking decision. Stage6 and the next gameplay front
remain closed. Preserve exact include/exclude lists and all historical tags.

Qualified support/powered support and fixed-slab consumer handoff do not establish
rotating-site powered departure, an Earth launch stack, orbital qualification or
player launch controls. Stock705kg Florida support is separate from historical
oversized Stage4 development arithmetic. Lighting/shadows, startup/first-present,
spacecraft-focus camera, SAS/navball/autopilot and atmospheric flight remain deferred.
Current KSA remains an internal engineering reference: retained direct source/history
provenance is consolidated, with no new public branding language or copied assets.

## Repository rules

Latest banked production checkpoint: **M15.2 — SRV-01 Reusable Production
Spacecraft Integration**, commit `c3de150db8563864d23c4951b4630559453d8f56`,
annotated tag `m15.2-srv01-reusable-production-spacecraft-integration`.
**M15.2 is BANKED; its original SRV-01 production-integration front is closed.**
The later accepted Stage1–5 candidate is described above; no further front is authorized.
Use the [accepted qualification](engineering-evidence/srv01-production-integration/README.md)
for the banked scope. The [bank-preparation packet](engineering-evidence/srv01-production-integration/bank-preparation/README.md)
is historical evidence of the pre-bank state, not current instructions.
Accepted limitations include unattributed frame maxima, approximate plume overlap,
no atmospheric/gas parity and synchronous save/restore hitches. Historical retired
terrain payloads need not be restored; current terrain authority remains required.

Previous banked production checkpoint: **M15.1 — Finite-Fuel Retained Powered Contact**,
tag `m15.1-finite-fuel-retained-powered-contact`, parent
`49057fecceb0f725d5f551ec40e2780971b0d81d`. The annotated tag identifies the atomic
engineering/license bank. M15.0 free flight remains at
`4607d8c802006d5e1a01c595ab608cf53a4dab6b`.

**M15.1 is BANKED.** Use the historical
[bank-candidate report](engineering-evidence/powered-contact-bank-candidate/README.md)
for qualification and the explicit attribution/whitespace identity migrations.
Technical/manual acceptance passed; numerical research and optimization are closed.
The operative [LICENSE](../LICENSE) and [public policies](../LICENSING.md) are
effective September 15, 2026. Old review drafts remain historical/non-operative.
No departure, Florida or next milestone work is authorized by this bank.

Earlier banked checkpoint: **M14.21 — Authored Compound Spacecraft Contact**,
`b6e8fa568585706ce38c0609aeb14db4a643123e`, tag
`m14.21-authored-compound-spacecraft-contact`. Inspect current Git refs before resuming;
later documentation commits may advance main without moving this milestone tag.
The [formal qualification](engineering-evidence/compound-contact-coverage/formal-qualification/README.md)
records passing article/selector physics, host schedules, warmed allocation and Release
performance, regressions and explicit centered/tilted manual presentation acceptance.
The historical [host-paced candidate report](engineering-evidence/contact-episode-servicing/README.md)
and formal report retain their pre-banking status and cold/whole-frame limits;
they do not reopen an active candidate. Resume from the
[current-state boundaries](NOVACORE_CURRENT_STATE.md).

Project Control accepted **M14.18 — Staged Finite-Body BEPU Contact**. Resume from the
[current-state boundaries](NOVACORE_CURRENT_STATE.md) and
[accepted qualification](engineering-evidence/bepu-local-contact-staging/README.md).
The approved tag is `m14.18-staged-finite-body-bepu-contact`; inspect Git refs for
actual banking state. [M14.19 — Persistent BEPU Contact Publication](engineering-evidence/persistent-contact-publication/README.md)
is accepted and banked at `fc6fbf269d7a8022ce2fe184aea0dfc94716db05`, tag
`m14.19-persistent-bepu-contact-publication`.
No live terrain/collider or gameplay integration is authorized.

Banked M14.15: [paired private spacecraft propagation](private-canonical-propagation.md)
at `dc274639f670d122764bb0e8f3d0c95ae201c7a4`, tag
`m14.15-private-canonical-propagation`. It produces conditional staged state only;
event coverage and canonical publication were deferred by M14.15.
Banked M14.15 acceptance: final normal Release passed
5/5 fresh processes, 46/46 groups each; remaining declared acceptance gates passed.
Historical real-object allocation witnesses remain retained. Candidate-associated
process/module/layout effect remains; production causality is not established.
No production/test correction was made; forensics is closed for this acceptance.
See [final acceptance](engineering-evidence/private-canonical-propagation/final-acceptance.md).

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

At the M14.13 checkpoint, the banked baseline was
`704c77f1aab899ccf3452ee3fc88471d1e311aca`, tag `m14.13-certified-postimpact-velocity`:
[certified final linear/angular bits](certified-postimpact-velocity.md).
- Banked M14.14 [private post-impact spacecraft state](private-postimpact-state.md) adds source-defined root pose, frozen dynamics and checked access at the same provider-owned alpha.
- M14.14 itself implements no remainder advancement, endpoint selection, canonical
  publication or contact reacquisition. Its accepted gates are retained in the
  [evidence](engineering-evidence/private-postimpact-state/README.md).
M14.8 is
`2657bb16cdee2a42d1e5f9be830d247fb3c607f6`, tag
`m14.8-exact-event-earth-relative-observation`; it owns same-epoch numerical
Earth/point observations. **M14.7 — Evaluate coherent spacecraft motion at
exact physical-event epochs**, tag `m14.7-exact-event-spacecraft-motion`, adds
read-only exact-event motion beside unchanged banked canonical evaluators.
The earlier final Release matrix stopped on the lunar 12,336-byte counter witness;
that cause remains unattributed. The authorized timed-family test-only split is
now validated in Debug/Release, followed by **fresh Release 5/5, 38 groups each**
and bounded exact-event performance with zero allocations. All 332 production
fingerprints matched the accepted candidate at closeout. Project Control accepted
M14.7 for production banking. See the [qualification](engineering-evidence/timed-measurement-split/README.md)
and [closeout](engineering-evidence/m14.7-closeout/README.md).
The superseded generalized mechanism and both development branches are retired.
The [banked epoch contract](physical-event-epochs.md) remains authoritative.
Internal canonical/reduced-rational identity and exact deterministic ordering exist;
public `SimulationInstant` and canonical publication remain integral. General
event-local transient execution and automatic contact discovery do not exist.

M14.5 still admits only one qualified authored point at represented canonical
`radialSignedGap == 0`: positive gap gives no response, negative gap requires
unsupported recovery. It is frictionless with restitution zero, no penetration
recovery, general multi-contact solver, persistent support/grounding or automatic
touchdown. **Exact event time is not exact representable event state**: M14.6 does
not guarantee an FP64 impact state satisfying strict represented-zero admission.
The banked [root-kinematics witness](certified-root-contact-kinematics.md)
qualifies read-only normal, velocity and lever enclosures. It supplies no
response/application contract or exact FP64 impact state.

The 8,160-byte investigation is resolved as **CANDIDATE-TRIGGERED CLR
ALLOCATION-CONTEXT ACCOUNTING EFFECT**, not a production allocation regression or
claimed CLR bug. The ordinary zero-allocation measurement class was hardened
without production changes. Final Simulation passed 5/5 Debug and 5/5 Release,
37/37 groups each; all 31 migrated windows measured zero. Fresh real-allocation
controls passed 3/3 per configuration, detecting 152 bytes each. Focused epoch,
ReferenceFrames, Precision and M14.2–M14.5 gates passed. The five timed windows
were deferred at that banked checkpoint; their later split is now qualified in
banked M14.7 above. See [banked validation and scope](NOVACORE_CURRENT_STATE.md#accepted-m146-validation-and-measurement-correction)
and [final evidence](engineering-evidence/physical-event-epoch/ordinary-class-migration/README.md).

No next milestone number is assigned. M14.7 is read-only evaluation, not contact
certification, root discovery or private event-local execution.

- The normal production worktree is `E:\NovaCore` on `main`; inspect
  `git status --short` before editing.
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

M15.1 retained powered contact is banked; M15.0 free flight remains separate.
Contact-to-flight transition remains unimplemented and requires Project Control
selection. The following earlier boundaries remain:
M13 is closed at M13.6; M14 remains OPEN. Banked M14.21 qualifies the authored
compound article and NovaCore-owned four-contact selector on the corrected M14.20
host-paced servicing path, retaining M14.19 canonical publication and private BEPU
solver/world continuity. Arbitrary spacecraft geometry, live contact acquisition/
departure, live Earth/mesh terrain, production landing gear, grounded gameplay,
Florida launchpad integration, flight controls, production spacecraft art and
rotating/rebased local frames remain outside qualification. Further production
work requires its own active Project Control ticket; no later milestone is assigned here.

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

## Banked M14.16 — post-impact terrain clearance

**M14.16 — Certify post-impact terrain-contact clearance for the admitted authored point**

M14.16 is the banked terrain-clearance responsibility following M14.15.

For the complete singleton authored point admitted by the existing M14.9–M14.15 chain
and its supported natural-terrain relation, M14.16 either certifies
`EventFreeThroughTarget` over `(alpha,T]` or refuses conservatively.

Banked outcomes:

- `EventFreeThroughTarget`
- `Unresolved`
- `Unsupported`
- `Stale`

Checked beta / next-root authority is intentionally not banked. Crossing/root evidence
is retained only as needed to prevent false clearance.

Real Florida acceptance:

- Coast: `EventFreeThroughTarget`, 63 visits, maximum depth 5.
- Force -0.2 m/s^2: `Unresolved / DepartureUnproved`, 25 visits, maximum depth 24.

M14.15 remains the staged private propagation authority. M14.16 adds checked terrain
clearance evidence; it does not publish canonical state.

Banked M14.17 separately admits certified paired continuation publication at canonical
T. M14.18 qualifies private BEPU solving; banked M14.19 adds a separate persistent-contact
publication boundary under NovaCore canonical authority. M14
remains OPEN; beta issuance remains outside the accepted singleton-clearance contract.
