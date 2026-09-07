# Release SurfaceAnchor failure classification

**Classification: INSUFFICIENT EVIDENCE**

**Lead judgment: ESCALATE TO PROJECT CONTROL**

**Technical classification: M13.4 CANDIDATE — FOLLOW-UP REQUIRED**

Project Control has accepted the material candidate's architecture/performance.
This follow-up did not reproduce the historical Release headless failure on
either the banked control or the candidate. It therefore cannot classify that
failure as baseline-pre-existing or candidate-caused. This is decision-matrix
Case D, including its one bounded targeted repeat. No unrelated repair occurred.

## Candidate and baseline

Branch: `codex/post-m13.3-next-performance-target`.
HEAD/main/origin/main: `180eaf150ba5db6364e17dd48336690778f058f9`.
M13.3 annotated tag: `m13.3-prepared-physical-terrain`.
The ticket again transposes the commit suffix; verified Git identity above is
unchanged. M13.1/M13.2 tags remain unchanged. Nothing was staged, committed,
merged, pushed or tagged.

Only production diff:
`native/NovaCore.Native/shaders/production_terrain_material.glsl`.
It skips procedural material noise when its complete land-detail contribution is
zero. Hardware tessellation, refinement, geometry, physical authority and future
KSA-class displacement capacity remain unchanged. No candidate code revision.

Accepted active performance remains **17.12243 → 5.95516 ms total GPU**,
**11.16727 ms recovery**, native 3440 x 1440 on RX 6800 XT. No performance campaign
or GPU capture was rerun. No correction changes that accepted measurement.

## Exact historical failure

Test: **Canonical SurfaceAnchor physical terrain authority**.
Location: `tests/NovaCore.Graphics.Tests/Program.cs:2974`, local function
`CanonicalSurfaceAnchorPhysicalTerrainAuthorityTest`.
Execution position: **27 of 79** headless tests, after canonical geographic
handedness and before the anchored Florida transport fixture. Every test runs in
its own child process. Baseline/candidate order matches exactly.

Unchanged assertion:

```csharp
Check(maximumAuthorityError==0d && terrainChecksum>=0d && enu.IsValid && allocated==0,
    "production SurfaceAnchor authority precision and allocation");
```

| Predicate | Expected | Historical failing run | New complete runs |
| --- | --- | --- | --- |
| Maximum authority error | Exactly 0 metres | Not logged | Exactly 0 metres |
| Terrain checksum | Nonnegative | Not logged | Predicate passed; numeric value not printed |
| ENU validity | True | Not logged | Predicate passed |
| Thread allocations | Exactly 0 bytes | Not logged | Exactly 0 bytes |
| Entire conjunction | True | **False** | True |

The old two stock runs were 78/79 and threw before printing the predicates. Their
precise failed condition remains unavailable. Prior private logging probes cannot
retroactively supply it. New runs print authority error and allocation count as
zero, with anchor hash `0x5BE7729CD52A7AD1`. Do not invent a numerical error,
allocation amount, JIT cause or ambient cause for the historical failures.

## Equivalent banked control and candidate repeat

A detached, sparse M13.3 checkout was created under the new task scratch root.
Its managed Release test project was built from banked source, with zero build
warnings/errors. Assets were hard-linked to identical existing production inputs;
no multi-gigabyte asset copy was made. The original assets/cache were preserved.

The baseline reused the byte-identical native DLL: its C++ source has no candidate
change. The baseline fragment was independently compiled from banked source and
matched the banked hash. Thus no current fragment was silently used as the baseline.
Headless tests do not execute that fragment, but its baseline identity was still
protected. Normal candidate deployment was never rebuilt or replaced.

Source comparison covered 354 files across Core/Graphics/Interop/Simulation,
Graphics tests, sample and native responsibilities. The sole difference was the
intended material shader. Independent managed build hashes differ and are recorded;
compiled IL identity was not assumed. Native DLL bytes match exactly.

Both controls used the same machine, Release configuration, 79-test order,
`--category=headless` invocation, environment and content-identical assets.
Each ran from its own repository root with corresponding Release deployment.
The topology artifact input pointed to the same original read-only asset directory.

| Run | Complete Release result | SurfaceAnchor | Purpose |
| --- | --- | --- | --- |
| Banked M13.3 isolated stock build | **79 pass / 0 fail / 0 skip** | PASS | Decisive baseline control |
| Current candidate stock deployment | **79 pass / 0 fail / 0 skip** | PASS | Equivalent candidate repeat |
| Current candidate, earlier validation profile | **79 pass / 0 fail / 0 skip** | PASS | One targeted Case-D repeat |

The targeted repeat recreated only the earlier process-local Khronos manifest
and environment paths. It used the same candidate executable, native/managed
assemblies, source, full invocation and order. The earlier environment also did
not reproduce the failure. It was not a tolerance, order or test expectation change.

Only three new complete suite runs were made; no dozens-of-runs campaign.
`baseline-stock.json`, `candidate-stock-repeat.json` and
`candidate-original-environment.json` retain test results, inputs, environment,
binary identities and original log hashes. `comparison.json` records equality
checks, source boundary, test order and final production/asset hashes.

## Dependency analysis

The failing responsibility follows this managed CPU path:

```text
SurfaceAnchor test
  -> SurfaceAnchorEvaluator.TryEvaluateBodyFixed / TryEvaluateRoot
  -> PlanetaryPhysicalTerrainAuthority.TrySampleHeight
  -> PlanetaryTerrainDefinition.SampleHeight / SamplePhysicalSurface
  -> CPU physical terrain evaluation and global/regional elevation datasets
```

`SurfaceAnchor.cs` composes the authoritative radius and body-fixed direction.
`PlanetaryPhysicalTerrainAuthority.cs` adapts the CPU terrain query.
`PlanetaryTerrain.cs` selects physical height; the CPU elevation oracle verifies
the R16 input's SHA-256. The test also checks ENU construction and allocations.
This path does not read or execute `production_terrain_material.glsl`, procedural
fragment noise, hardware TES or a Vulkan graphics pipeline. The harness loads
the native ABI for identity verification, not this shader for SurfaceAnchor.

Other tests in the complete suite do inspect shader source contracts, but that
does not make the SurfaceAnchor CPU query a consumer of fragment material noise.
Source independence supports the boundary; it is not substituted for the actual
banked A/B above and does not classify an unreproduced historical failure.

## Judgment and remaining evidence gap

No new Release-headless regression was observed in this bounded A/B. That is
weaker than proving the historical failure was pre-existing. M13.3 did not fail
in its control, so decision-matrix Case A cannot be claimed. Nor did the candidate
fail consistently, so Case B is unsupported. Both did not demonstrate varying
outcomes in these controls, so Case C is not proven either.

The candidate's historical versus current outcome varies, but this does not
establish an ambient mechanism or compare failure frequencies. **INSUFFICIENT
EVIDENCE** is the required Case-D classification. Stop at the bounded evidence
gap rather than manufacture a cause or repair unrelated code.

The missing proof is the four individual predicate values when the unchanged
stock full-suite failure actually recurs, correlated with runtime/environment
identity and a contemporaneous banked control. Project Control owns any further
bounded attempt. No M13.4 milestone title is proposed until that causal gate is
cleared. The preferred title from the ticket remains unassigned.

## Final validation and storage

All three new complete Release suites passed honestly as shown. The two historical
78/79 failures remain retained; they are not rewritten as successes.
Prior accepted Debug headless (79), GPU (8 each), window (4 each), native,
15 launcher regressions, six routes, dynamic/warp and post-cleanup Florida
500-frame smoke remain valid prior evidence. No normal candidate binary, shader
or asset changed, so those expensive checks were not repeated.
Candidate-specific material tests were included and passed in the new complete
suites. Shader/asset/deployment identity was reverified in both configurations.

Release candidate fragment:
`8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`.
Banked control fragment:
`4b79673d14c90df52703a322c116415ec9e833cf2247731ca5ee1ab7b563df06`.

The temporary control contains 713 files, 2,031,635,908 logical bytes. Of those,
2,002,790,600 bytes were references to existing hard-linked assets/dependencies;
only 28,845,308 bytes were exclusive disposable payload. This is not two GB of
new or reclaimed disk allocation. Exact retained/removed accounting and final
checks are in `closeout.json`. **Automatic approval review rejected cleanup as
"blocked by policy" and provided no more specific reason.** No deletion occurred;
the baseline checkout and small layer profile remain disposable, not permanent
evidence. No alternative automatic deletion was attempted. Reproduction source,
identities and small numerical/text summaries are the permanent evidence. No GPU
readback was created, and no permanent fixture was removed.

The user may run the guarded `manual-cleanup.ps1` after reviewing
`disposable-manifest.json`. It validates exact roots, file inventory, detached
baseline revision, clean worktree and original production asset hashes before
removing this control. It does not remove unrelated worktrees or Git history:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\post-m13.3-next-target\surfaceanchor-classification\manual-cleanup.ps1'
```

`git diff --check`: PASS. Complete final status:

```text
 M native/NovaCore.Native/shaders/production_terrain_material.glsl
?? docs/engineering-evidence/post-m13.3-next-target/
```

No stage, commit, push, merge, tag or M13.5 work. Return unbanked to Project Control.

## Bounded reproduction

`classify.py` records setup and the two-control sequence. Create a fresh detached
M13.3 sparse checkout at its explicit scratch path using `git worktree add
--detach --no-checkout`, sparse-checkout `src tests samples native tools`, then
checkout detached HEAD. Run `classify.py setup` and `classify.py controls` from the
normal repository root. Fresh labels/output location are required to preserve
these records. The earlier-profile targeted repeat used the retained parent
`validate.canonical()` environment and the same `execute()` helper. Do not rerun
the performance or GPU capture scripts to reproduce this CPU classification.
