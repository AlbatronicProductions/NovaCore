# NovaCore engineering rules

## Authority and presentation

- Simulation owns truth: exact time, celestial definitions, hierarchy, ephemerides, physical constants, and authoritative state live outside Graphics.
- `CelestialSystemDefinition` is immutable runtime celestial authority. NCPE is serialized storage, not a second runtime model.
- Graphics owns presentation only. `PlanetaryPresentationSnapshot` and other renderer inputs are immutable derived data; presentation must never change celestial state.
- Camera-relative subtraction occurs in high precision before FP32 GPU transport. Do not relocate bodies, compress orbital distances, or inflate physical radii to solve a presentation problem.

## Determinism

Determinism is a first-class requirement. Hashes use explicit stable values and order. Do not depend on locale, host time, enumeration order, machine paths, or environment state. Paths and build locations never contribute to dataset identity. Preserve deterministic operation order when floating point participates in identity. CPU reference implementations remain correctness oracles for equivalent GPU presentation paths.

## Offline astronomy

Runtime projects must never depend on CSPICE, NAIF kernels, the NAIF adapter, EphemerisBuilder, network access, host calendars, or source discovery. The intended flow is official source → offline adapter → normalized data → NCPE builder → NCPE artifact → runtime loader → `CelestialSystemDefinition` → evaluator. Offline astronomy tools and source files never become runtime dependencies.

## Ephemerides and frames

Analytical and sampled systems share the generic evaluator. Sampled coverage is finite with no extrapolation; cubic Hermite position/velocity interpolation is versioned runtime policy. Provenance participates in identity. Celestial hierarchy, observation/camera origin, gravity hierarchy, launch frames, and SOI policy are separate concepts.

## Renderer and native boundary

GPU LOD, culling, label selection, and representation handoff are presentation-only. They do not enter simulation identity, persistence, replay, or dataset hashes. Renderer resources have explicit native ownership and deterministic lifecycle; no Vulkan handle or C++ ownership-bearing object crosses the managed boundary. Native/managed ABI changes require explicit fixed-width layout, offsets where applicable, and focused managed/native validation.

## Architecture and delivery discipline

- Prefer bounded tickets with an explicit authority boundary, acceptance gate, and stop condition.
- Measure before optimizing. Performance comparisons require an equivalent workload fingerprint; a fixed-pose improvement is not evidence of dynamic player-facing correctness.
- Do not broaden scope after a failed test. If implementation exposes an architectural conflict, stop and request explicit review rather than silently redesigning.
- High-risk architecture and authority changes belong to the designated architecture owner (currently Sol). Terra may implement bounded work whose design and invariants are already fully specified.
- Rejected production architectures are removed after their accepted replacement is ready. Do not accumulate hidden compatibility modes, dormant renderers, or runtime bridges merely because tests reference them.
- Do not optimize systems already scheduled for architectural retirement unless the work is required for migration correctness or safety. Preserve them as recovery paths until their accepted replacement passes its retirement gate.
- Do not stage or commit a rendering milestone before its required physical Desktop acceptance unless the user explicitly changes that gate.
- Main-branch NovaCore milestone commits follow `NovaCore <milestone>: <description>` unless a specific non-milestone maintenance operation justifies otherwise. Do not rewrite already-pushed history solely to rename an older commit.

## Diagnostic evidence lifecycle

**Keep the ability to reproduce the evidence. Do not keep bulk evidence by default.**

This section is the authoritative diagnostic-evidence policy for Active Development.
Diagnostic output is temporary by default. Permanent retention requires an explicit
production, regression, reproducibility, provenance, or continuing-development
justification. Size, uniqueness, generation cost, historical interest, or the
difficulty of an investigation does not by itself justify retention.

The normal lifecycle is:

diagnose → capture evidence → prove cause → implement → validate → manual /
production acceptance where required → consolidate evidence → purge bulk output
→ final smoke → bank

Pre-banking evidence consolidation is the preferred default. A milestone is not
cleanly ready to bank while large resolved diagnostic output remains without a
documented justification. Banking still requires the applicable acceptance gates
and explicit authorization; this lifecycle grants neither automatic deletion of
unreviewed files nor permission to bank.

### Active and resolved investigations

Active Development may generate large evidence when needed to establish causality:
raw frames, D32 depth, HDR attachments, GPU readbacks, TES output, pixel provenance,
object/depth IDs, material decomposition, videos, workload traces and A/B datasets.
Do not discourage necessary diagnostics merely to save disk space. Record the
unresolved responsibility, owner and next decision; retain needed output while it
is unresolved. Large diagnostics must have a lifecycle.

After the appropriate acceptance gates pass, retain the final report and concise
causal conclusion, tested source/runtime identity, useful hashes, reproduction
recipe, permanent regression tests/fixtures, useful diagnostic tooling/source,
and minimal representative visuals where they add long-term value. The record
must explain what failed, what was measured, the cause, the accepted correction,
what was tested and how to reproduce the investigation.

Retire bulk raw frame archives, repeated GPU readbacks, complete attachment
sequences, duplicate captures, temporary videos and per-pixel/provenance dumps,
rebuildable diagnostic binaries/output and superseded temporary capture directories.
Raw bulk may remain only for a concrete documented reason why it cannot reasonably
be reproduced or why future engineering requires that exact artifact. Verify
duplicates by content/hash and retain shared provenance mappings where useful.
Do not purge unresolved evidence under a resolved-investigation classification.

### Videos, fixtures and tooling

AI-generated or user-supplied manual-acceptance videos are temporary by default.
After acceptance, retain the observation report and useful hashes/provenance;
optionally retain one compact representative clip with explicit historical or
regression value, otherwise retire the video within the authorized scope. Their
use during development alone does not justify committing large recordings.

Permanent regression fixtures remain first-class assets even when they originated
in an investigation. Retain bounded, purpose-labeled fixtures protecting physical
authority, serialization, deterministic generation, CPU/GPU parity, ownership,
publication, precision, intentionally retained compatibility, corruption rejection,
or another accepted production contract. Record the current test consumer.

Useful capture tools, GPU inspection helpers, parity probes, reproducible stress
tests, visualization modes and workload instrumentation may remain indefinitely
with a continuing-development justification. Keep diagnostic tooling conceptually
separate from production runtime, permanent regression tests and generated output.
Production must not depend on historical investigation artifacts. Production
source/assets, manifests and generators remain protected responsibilities.

### Storage and closeout

`build/` holds temporary/rebuildable development output and active diagnostic
evidence; it is not a permanent archival warehouse. Permanent conclusions belong
in bounded reports, tests, fixtures, provenance and reproduction tooling. Moving
huge temporary evidence elsewhere is not evidence retirement.

For unusually capture-heavy investigations, the final Active Development report
must include these quantities with explicit units:

- Diagnostic output created: X GB.
- Permanent evidence retained: Y MB/GB.
- Disposable after acceptance: Z GB.
- Any unusually large retained category and its specific retention reason.

Declare a size budget and retention reasons for permanent evidence packages.
Preserve historical reports and measurements; identify intentionally retired raw
paths and point readers to retained provenance and reproduction information.
Missing raw data is never evidence of a passing test. Operational archive and
safe-cleanup guidance is in [diagnostic output guidance](docs/diagnostic-output-policy.md).

Before authorized cleanup, verify retained evidence, exact target paths, Git status,
test/runtime/asset consumers and reparse points. Use bounded reviewed file lists;
do not infer that a mixed directory or junction target is disposable. Preserve the
active verified deployment. After cleanup, verify launcher/runtime, deployed
shaders, asset resolution, a bounded smoke and Git whitespace/status. Rebuild only
when required; do not regenerate bulk captures just to prove removal. Documentation-
only policy changes do not require expensive renderer validation.

Git and Git LFS are not default storage for raw AI-development evidence. Large
diagnostic artifacts may be committed only after explicit promotion to a permanent
regression/provenance artifact with a documented reason, plus normal commit
authorization. Workspace retention does not itself authorize Git/LFS inclusion.
Existing Git/LFS storage debt, cache policy, compatibility and unrelated repository
debt require separate bounded review; deferral does not make them permanent debt.

## KSA reference policy

The installed Kitten Space Agency tree is authorized read-only architectural evidence for analogous planetary systems. Relevant work must inspect the current local implementation rather than rely only on old summaries. Determine the problem solved, pipeline stage, prepared data, CPU/GPU and update cadence, reuse boundary, continuity rule, and cost bound. Classify the NovaCore decision as **adopt responsibility**, **adapt responsibility**, or **intentionally differ**. An intentional difference requires measured NovaCore evidence; existing code is not justification by itself.

KSA source, shaders, assets, constants, and proprietary data must not be copied into NovaCore, exposed substantially in NovaCore documentation, or made a runtime dependency. Implement accepted principles independently with NovaCore's FP64, deterministic, Vulkan-native contracts. Prefer `prepare -> publish -> reuse -> cull/compact -> draw` when it removes demonstrated duplicate work without weakening physical authority.

## Rendering acceptance

Rendering acceptance has two independent classes. A deterministic microbenchmark proves workload-equivalent timing, parity, and optimization effect. Physical player acceptance proves the complete orbital-to-surface trajectory, representation activation, low-altitude and grazing-horizon behavior, lateral movement, rotation, retreat, re-approach, and relevant time-warp states. A microbenchmark pass never implies physical acceptance. Manual Desktop testing is an authoritative milestone gate.

## Scope discipline

CSPICE is offline-only through a narrow C ABI. Managed correctness never relies on native finalization. CSPICE uses controlled `RETURN` failures; SHORT/LONG diagnostics are captured before reset. Third-party kernels and native artifacts remain untracked. Prefer small slices and focused tests while developing; run full regression only at checkpoints. Do not alter unrelated deterministic hashes or established contracts without a concrete reason and regression coverage.
