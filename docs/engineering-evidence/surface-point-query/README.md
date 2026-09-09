# Immutable canonical surface-point query candidate

2026-09-09. **SURFACE-POINT QUERY CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE.**
Lead judgment: **PASS**. Cleanup: **PROJECT CONTROL CLEANUP REQUIRED**.
The initial candidate below required performance follow-up.
The [bounded performance qualification](performance-qualification.md) is the
current closeout report and supersedes the initial cost/judgment below.

The bounded physical API, readiness and renderer-preservation gates pass. The
follow-up stages normal qualification to avoid already-rejected stencil work,
preserving exact outputs while materially reducing realistic footprint cost.
Classified private build output still requires manual disposal. The implementation
remains unbanked; neither this report nor performance qualification authorizes
banking or contact response.

Proposed accomplishment title: **Provide immutable canonical surface-point queries
for spacecraft contact**. No milestone number is assigned.

## Baseline and scope

- Initial HEAD/main/origin/main: `aaf3e7a032a806e7e547c2830d1f95d97aaf620b`.
- Latest banked step: M13.6, `90fef759243dd67918cd556e19027159e5a5eada`,
  annotated tag `m13.6-cpu-cached-terrain-residency-keys`.
- Initial working tree and index were clean. Created `codex/surface-contact-query`.
- M13 remains closed. No commits, staging, tags, pushes, merges or milestone naming.

Only canonical terrain → immutable physical query was implemented. No response,
grounded state, translational integration, propulsion, collider, solver or launch
behavior. Native rendering, shader code, assets, terrain H, residency, facility
grading and launcher deployment were not edited.

## Existing authority and cheap proof

Existing `PlanetaryTerrainDefinition.EarthProductionCubeV5` and explicit-generation
`PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient` provide full physical H:
signed global plus regional recomposition, accepted natural terms, clamps and
facility grading. Existing prepared normals use a much wider stencil and are not
automatically a qualified contact normal. The new service never uses them.

Readiness was the other missing seam. CPU global/regional data publish once, but
the regional loader previously exposed no digest of the bytes actually decoded.
Re-reading a filename after load would not prove immutable provenance. Capturing
digest/header/length in the winning published snapshot closes that gap without
new streaming or changing any height arithmetic.

Before production edits, the normal proof declared the existing 2e-6 m height,
1e-3 m transport and 5e-5 rad physical-normal accuracy ceilings. A central finite
difference initially appeared good, then failed the expanded polar probes. The
single bounded revision added rotated and non-dyadic one-sided checks; rotated
checks alone were insufficient. The completed revised method passed 564 probes
against a differently oriented, differently spaced fourth-order oracle.

[Cheap proof](normal-cheap-proof.json) retains the actual failed witnesses and
revised summaries. It used fingerprinted deployed assemblies; their embedded
version provenance predates the current documentation HEAD, so it is not offered
as proof of a fresh current build. The production tests below rebuilt current
source and independently reproduced its numerical result. The permanent tests
contain the proof locations/recipe; bulk per-point diagnostic rows were retired.

## Contract and implementation

The [current query contract](../../surface-point-queries.md) owns detailed usage,
normal mathematics, failure semantics and lifecycle. In brief:

| Concern | Contract |
|---|---|
| Owner | Existing canonical CPU physical oracle; Core consumer interface injected by application composition. |
| Input | Stable body ID and finite canonical body-fixed unit direction. No camera/context from rendering. |
| Output | Immutable status, full authority, normalized direction, FP64 point/height/normal and qualifying stencil scale. |
| Identity | Body, terrain source/version, generation 4, radius, actual global/regional digests, natural/support identities, query-policy version. |
| Lifetime | Once-published immutable CPU datasets; provider has no native resource or disposal work. |
| Readiness | Both complete datasets verified against fixed production manifest before a provider exists. |
| Invalidation | Exact identity comparison by consumers; new authority required for revisions. Current datasets cannot hot-replace. |
| Publication | Metadata and decoded regional data belong to the same winning snapshot. |
| Determinism | FP64 deterministic operation order; immutable explicit physical generation; zero per-query I/O/allocation. |

Production files:

- `src/NovaCore.Core/Surface/PhysicalSurfacePointQuery.cs`: generic immutable contract.
- `src/NovaCore.Graphics/PlanetaryPhysicalSurfacePointQuery.cs`: bounded CPU provider.
- `src/NovaCore.Graphics/PlanetaryLocalTerrain.cs`: actual decoded-package identity.
- `src/NovaCore.Graphics/TerrainAssetDistribution.cs`: null-safe malformed-manifest
  handling required by explicit unavailable semantics.

Permanent tests are `PhysicalSurfacePointQueryTests.cs` plus two registered headless
cases. No fixture data was added or altered. Public acquisition uses the fixed
production asset ID through trusted application content configuration, not an
arbitrary caller-provided digest. SHA equality proves content consistency; the
application's trusted production manifest supplies authority.

## Physical results

Fresh source-built Debug and Release produced the same correctness results:

| Measure | Result | Declared bar |
|---|---:|---:|
| Probes | 564 | Global, regional, support, interpolation knots, axes, poles, actual clamp roots |
| Qualified | 527 | All returned normals independently tested |
| Explicitly unqualified | 37 | No substituted normal or authoritative point |
| Actual clamp creases rejected | 12/12 | Zero silent averaged contact normals at those roots |
| Maximum height difference | 9.094947017729282e-13 m | 2e-6 m |
| Maximum point difference | 0 m | 1e-3 m |
| Maximum independent normal error | 2.181364183438901e-7 rad | 5e-5 rad |
| Maximum analytic support-plane error | 4.691547355542352e-8 rad | 5e-5 rad |
| Maximum squared-unit-length error | 4.440892098500626e-16 | 16 FP64 epsilons |
| Outward orientation | Pass | Positive dot with radial direction |

Rejected samples include actual clamp creases, some global interpolation knots,
and unresolved polar samples. There is no blanket polar exclusion. This is sampled
numerical qualification, not a universal differentiability proof or full-surface
contact coverage. Future contact generation must explicitly handle unqualified
normals; this ticket does not invent a feature-normal/manifold response.

Repeated queries, reverse ordering and changing the ambient renderer generation
preserve every result component bit-for-bit. Nearby points on the analytic
support plane preserve normal continuity. The regional boundary probes are
tested against full canonical H and an independent local derivative, not assumed
smooth where the physical source is not smooth. Camera/LOD are absent from the
interface and evaluation call graph, and no render cache is read.

Readiness tests cover unloaded/global-only data, malformed package, stale fixture
preload followed by successful production-path load, mismatched body/version/hash/
coverage, malformed/null/missing manifest, invalid path/direction, unsupported
bodies, once-only reload behavior and immutable failed acquisition.

The standalone [CPU-only proof](cpu-only.json) actually acquired and queried full
Florida authority in a process with no `NovaCore.Native` or Vulkan module loaded.
Its tiny reproducible source is retained in `cpu-only/`. Simulation project
references remain unchanged; no simulation-to-Graphics dependency was added.

After building candidate Graphics Release, the standalone proof can be rebuilt in
temporary output with:

```powershell
dotnet run --project docs/engineering-evidence/surface-point-query/cpu-only/Proof.csproj -c Release -p:NovaCoreRoot=E:/NovaCore --artifacts-path E:/NovaCore/build/surface-point-query-proof -- E:/NovaCore
```

The already-run equivalent external project used the same source and explicit
assembly references. It did not pass through the Graphics harness, whose normal
startup intentionally loads the native ABI for dependency validation.

## Initial performance and payoff limit — before bounded qualification

Initial source-built measurements, 1,024 warmed samples per location, zero allocated
bytes, followed by 64 batches of 16 serialized calls. These are CPU timings on
this host, not contact-solver timings or GPU frame costs.

| Release location | Median us | P95 us | P99 us | Mean batch-16 ms |
|---|---:|---:|---:|---:|
| Florida support | 32.9 | 33.3 | 35.8 | 0.5255 |
| Regional interior | 254.4 | 257.5 | 260.5 | 4.0634 |
| Inland | 115.6 | 117.6 | 119.2 | 1.8517 |
| Unqualified pole | 462.9 | 466.2 | 472.2 | 7.4019 |

Debug medians are 308.4 / 2563.3 / 1112.7 / 4329.0 us in that order; it is not a
production performance measurement. Full Debug/Release records and percentiles
are retained in `debug-final.log` / `release-final.log`.

Manifest acquisition after data loading: 1.599 ms Release / 1.8442 ms Debug.
First query after acquisition, including cold query-method JIT: 5.2286 ms Release /
6.2764 ms Debug. This excludes global/regional loading and unrelated process
startup; it is not claimed as total cold-start time. The later `firstProbeUs`
fields are merely first probes in an already-warmed correctness-test process.

Four regional queries are roughly 1.02 ms by linear estimate; 16 were measured at
4.06 ms. The latter occupies about half of an 8.33 ms **CPU frame interval** if
serialized on a frame-critical thread. It must not be added directly to GPU time.
No actual spacecraft sampling rate, worker scheduling or integrated contact
CPU headroom exists yet. Therefore small bounded sampling is feasible, but
comfortable general leg/point/multi-craft capacity is not proven. That limitation
is returned to Project Control rather than hidden behind a fake microsecond bar,
caching, a new scheduler, or reduced physical quality.

## KSA/BRUTAL relationship — ADAPT

Current installed KSA `2026.9.7.5402` assembly fingerprint:
`a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`.
Inspected cached production decompilation is under
`E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA/`; it is decompiled
production code, not original developer source. Current assembly/source hashes
match the architecture prerequisite's verified reference. No broad rerun occurred.

- `Celestial.cs:791` onward owns the CPU terrain-height composition; material
  data affecting its own modifier is distinct from rendered GPU-only relief.
- `TerrainPatch.cs:519` onward builds CPU terrain vertices.
- `ConstraintSim.cs:621` onward uses physical triangles for collision, and
  `:1029` onward derives contact normals from actual manifolds.
- Game/physics owns these inputs. BRUTAL rendering is not contact authority;
  Bepu is a mechanism, not this candidate's identity or correctness contract.

Adopt the separation of simulation truth from presentation and adapt the query
boundary to NovaCore's stronger complete canonical H/FP64 agreement. KSA's patch
spacing, mesh normals, shader normal maps and solver tolerances do not prove a
NovaCore point-normal epsilon. None were copied.

Official history already browser-verified in the immediately preceding accepted
architecture work remains narrowly relevant:
[revision 4659](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885)
preserved high-precision sleeping state across representation work;
[revision 4979](https://discord.com/channels/1260011486735241329/1260112103134724146/1529376156976021616)
corrected stale force-evaluation state;
[revision 5338](https://discord.com/channels/1260011486735241329/1260112103134724146/1540029182623481867)
coordinated authored terrain modification and presentation. These support exact
state/physical ownership and the terrain-versus-pad distinction. They do not
establish this candidate's numerical error bar or cost. No new historical claim
is inferred from source filenames.

## Validation and renderer preservation

- Debug and Release managed builds: PASS, zero warnings/errors.
- Nine selected physical headless cases per configuration: PASS, no failures/skips.
- Terrain asset distribution and local payload contract: PASS per configuration.
  Thus 11 distinct relevant headless cases passed in each configuration; final
  additions to the new tests were rerun in both configurations, also PASS.
- Native regional identity/readiness and mapped-memory policy tests: PASS in both
  configurations, using existing matching binaries; native source was unchanged.
- Standalone CPU-only query: PASS, no native/Vulkan module loaded.
- Fresh source-exact baseline versus candidate: [raster parity PASS](raster-parity.md).
  Florida and inland at 3440x1440; four runs, 200 frames each, capture at frame 175.
  Exact full image/D32/HDR, prepared vertices and oriented triangle multiset;
  exact camera/light/pupil/publication/input bytes. Complete dependencies, sole
  NCSM1 owner. All 49 shaders and required production assets match.
- Khronos validation module loading verified in each private capture process;
  zero Vulkan errors. The same non-error shader-output warning remains visible.
- No normal launcher deployment was replaced. Private capture timings are not
  performance data, and these fixed-pose tests are not a new M13 stability soak.
- `git diff --check`: PASS; only line-ending conversion warnings.

## Initial independent verification

**VERIFY A — PASS with limitations.** Strongest challenge was false convergence
at a polar/discontinuous physical function. The first prototype failed; the
bounded revised method and independent current-source regression reject the
measured creases and qualify the retained samples. No alternate H, render normal,
pad collider or FP32 physical input was introduced. The verifier explicitly rejects
a claim of universally certified normals or comfortable general batch-16 cost.

**VERIFY B — PASS.** Strongest challenge was a once-loaded fixture being relabeled
by a later successful load, followed by malformed-manifest exceptions escaping
readiness. Actual winning-snapshot identity and permanent negative tests close
both. Null-safe manifest validation and malformed-root classification were verified
after correction. No remaining concrete identity/lifetime blocker was identified.

## Manual acceptance

**NOT REQUIRED for this physical API's visual gate.** There is no intended render
change, physical numerical acceptance passes and fresh exact image/depth/HDR
parity passes. This waiver does not establish contact gameplay, future contact
headroom, or any new indefinite stability claim.

## Evidence lifecycle and follow-up

Created raw parity output: 507,564,896 cumulative bytes across four sequential
captures; peak single raw slot 128,901,032 bytes. All 507,564,896 bytes were
disposed after compact digests were retained. Raw remaining: zero.

Automatic tool policy rejected guarded recursive removal of external rebuildable
parity source exports/hosts/object output before shell execution, stating only
`blocked by policy`. No broad retry occurred. Explicit nonrecursive removal of
the three raw capture filenames succeeded. Classified rebuildable parity output
remaining: **651 files / 44,274,331 logical bytes**. Separate standalone CPU proof
`bin/obj`: **32 files / 1,375,750 bytes**, also left for coordinated manual cleanup
instead of another broad automatic deletion. Total classified disposable:
**683 files / 45,650,081 bytes**. No production source or asset is in these groups.

See [exact manual cleanup commands](manual-cleanup.md) and
[parity storage inventory](disposable-remaining.json). Compact external parity
evidence is 33 files / 888,066 bytes at its cleanup checkpoint. The bounded
repository evidence package retains the essential summaries, provenance, hashes,
current regression outputs and CPU-only recipe. The external full reproduction
scripts and logs remain under `E:/NovaCore-Research/surface-query-parity-20260909/`;
they are not production dependencies. Ordinary candidate build outputs remain
available for validation and are not counted as diagnostic archives.

The subsequent bounded qualification resolves the measured query-cost blocker
without a solver or changed H. See its current performance, verifier, validation
and storage results before closeout. The original storage counts above describe
the initial checkpoint; the follow-up inventory adds its own generated output.
The implementation remains unbanked; no later milestone is assigned.
