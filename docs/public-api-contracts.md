# Public compatibility and API contracts

This is the support-state authority for the surfaces listed below. It separates
intentional developer support from C# visibility. Other APIs/formats retain their
existing contracts; this is not a whole-SDK support declaration. The earlier
[compatibility audit](compatibility-contract-resolution.md) and its 25-row matrix
remain historical evidence. The [repository map](repository-structure.md) owns
placement; this document owns these compatibility decisions.

## Support policy

- **SUPPORTED:** preserve the stated source/behavior or format contract and its
  existing regressions. A public spelling is retained when it serves a defined
  responsibility, even when local code uses an equivalent lower-level operation.
- **SUPPORTED BUT DEPRECATED:** behavior remains supported during migration. A
  decision must name the replacement and removal condition. No in-scope API is
  put in this state by this package; a preferred newer format is not itself a sunset.
- **INTERNAL IMPLEMENTATION DETAIL:** no ordinary external C# contract; include
  named friend assemblies when auditing consumers. Visibility of an enclosing
  type matters as well as a method's modifier.
- **HISTORICAL ONLY:** provenance, not an active API guarantee. A historical name
  alone does not place a reachable API in this category.
- **UNRESOLVED EXTERNAL CONTRACT:** preserve the existing surface while the named
  support/removal decision is open. This is not permanent support by inertia.

No repository semantic-version compatibility or deprecation schedule was found.
Do not invent a release/date deadline. A future breaking change needs an explicit
support decision, migration guidance and affected regression validation. Absence
from search, NuGet or GitHub releases cannot establish absence of source/DLL users.

## Contract matrix

`Core`, `Format`, `Platform` and `Graphics` below mean the public managed assembly
surfaces `NovaCore.Core.dll`, `NovaCore.EphemerisFormat.dll`,
`NovaCore.Platform.dll` and `NovaCore.Graphics.dll`. Confidence distinguishes
measured implementation from unknown external consumption.

| ID | Surface | Visibility | Current internal consumer | Plausible external consumer | Current replacement | Current docs | Support decision | Migration path | Retirement condition | Confidence |
|---|---|---|---|---|---|---|---|---|---|---|
| NCPE-1 | `EphemerisArtifactCodec.TryRead` and v1 artifact/manifest types | Public, Format | EphemerisBuilder.Tests round trip, digest rejection | Offline reader of published format/source | V2 only with missing semantics supplied | Dataset format; this contract | KEEP — SUPPORTED PUBLIC COMPATIBILITY, offline v1 reading | Read v1; author complete v2 semantic inputs; validate/rebuild | Explicit end to legacy support and a loss-accounted artifact migration; not met | High contract/code; external census unknown |
| NCPE-2 | `TryBuild`, `CreateManifestText`, v1 input/body/interpolation/status/format types | Public, Format | In-memory v1 tests; no current CLI output path | Offline producer or round-trip tool | `NcpeV2Codec.TryWrite` + complete definition | Builder; this contract | KEEP — SUPPORTED offline compatibility construction/manifest output | New artifacts prefer v2; existing v1 tooling can continue | Explicit producer support sunset, ordering/error-provenance migration and regression parity; not met | High |
| NCPE-3 | `NcpeV2Codec`, records/hashes and v1 rejection status | Public, Format | Builder, format tests, internal runtime loader | Developer self-describing interchange tool | Current authority | Dataset format/runtime-loader | KEEP — SUPPORTED | No migration for v2; do not remove shared sample/hash types with v1 | Accepted replacement preserving semantic/hash contract | High |
| CAM-1 | `CameraMath.Right(in DoubleQuaternion)` | Public static, Core | No call site found outside declaration | Source/DLL camera tooling | `q.Rotate(Double3.UnitX)` | Camera guide; this contract | KEEP — SUPPORTED axis helper | Optional equivalent expression, preserving normalization/errors | Explicit helper-contract migration; no present reason to retire | High semantics; unknown external count |
| CAM-2 | `CameraMath.Up(in DoubleQuaternion)` | Public static, Core | No call site found outside declaration | Source/DLL camera tooling | `q.Rotate(Double3.UnitY)` | Camera guide; this contract | KEEP — SUPPORTED axis helper | Same as CAM-1 | Same as CAM-1 | High semantics; unknown external count |
| CAM-3 | `CameraMath.Forward(in DoubleQuaternion)` | Public static, Core | No call site found outside declaration | Source/DLL camera tooling | `q.Rotate(-Double3.UnitZ)` | Camera guide; this contract | KEEP — SUPPORTED axis helper | Preserve local negative-Z convention | Same as CAM-1 | High semantics; unknown external count |
| POS-1 | `RelativePosition(Double3 Value)` | Public readonly record struct, Core | No call site found outside declaration | Typed FP64 displacement values, record equality/deconstruction | `Double3` for a plain offset; frame-aware types for contextual positions | Precision guide; this contract | KEEP — SUPPORTED value abstraction | Optional `.Value`; retain origin/frame/units at caller; no blind absolute-position substitution | A separately justified value/coordinate contract migration | High representation; external coordinate meanings unknown |
| HOST-1 | `RuntimeHost` | Formerly public empty static class, Platform; removed | None; no sample/test use or documented facade | Theoretical identity-only uses; no actual downstream consumer identified | No replacement type; existing sample/native owners provide actual startup | Repository map; this contract | RETIRED — HISTORICAL ONLY | Remove any identity-only reference; choose actual startup APIs by responsibility, not a substitute placeholder | Satisfied by RuntimeHost resolution: no workflow/purpose/publication contract, always-empty scaffold, theoretical risk only | High reasonable engineering entitlement; not an absolute external-consumer census |
| PREP-1 | `PlanetarySphericalBillboardNaturalTerrainProof.Run` and report/level-result records | Public, Graphics | Triangle natural-terrain proof branch; NaturalTerrainTests | Developer GPU parity/proof runner | Existing explicit proof route | Planetary rendering/history; this contract | KEEP — SUPPORTED development proof | None; retain public name reflecting actual proof responsibility | Equivalent proof API plus external migration before rename/removal | High |
| PREP-2 | `PrepareProduction` | Public, Graphics | Forwards to incremental preparation with a fresh cache; no other local direct caller found | Developer one-shot physical preparation | `PrepareProductionIncremental` with fresh cache | This contract; XML summary | KEEP — SUPPORTED convenience entry point | Optional explicit cache; do not imply production NCSM1 ownership | Public preparation migration with equivalent inputs/results | High semantics; external usage unknown |
| PREP-3 | `PrepareProductionIncremental` | Public, Graphics | Two moving-runtime constructor delegates; two direct regression calls | Developer/reference preparation and reuse | Same current entry point; native NCSM1 path is not an API-compatible replacement | Repository map; this contract | KEEP — SUPPORTED development/reference preparation | No rename now; future extraction needs an explicit compatible transition | Preserve callers, query/cache/result behavior and declare public migration | High |
| PREP-4 | `PlanetaryProductionBillboardPhysicalCache` constructor, `Count`, `RetainOnly`; preparation record; proof constants | Public, Graphics | Moving coordinator, preparation, tests | Callers of PREP-2/3 | Existing accompanying contracts | This contract | KEEP — SUPPORTED accompanying developer surface | Migrate coherently with preparation; internal lookup/store stay internal | Same public preparation migration gate | High |
| INT-1 | Proof `Query`/`ResolveAssets`, cache `TryGet`/`Store`; enclosing `NcpeCelestialSystemLoader` | Private/internal | Declaring implementation and named friends | No ordinary external C# caller | Current implementation owners | Runtime-loader; implementation | INTERNAL IMPLEMENTATION DETAIL — KEEP current consumers | Internal changes still require affected callers/tests | Reachability and invariant proof, not a public API sunset | High |

## NCPE v1: explicit bounded support

[EphemerisArtifact.cs](../src/NovaCore.EphemerisFormat/EphemerisArtifact.cs)
implements the v1 reader/writer, manifest text and neutral data records. The public
codec can still emit v1; the current **CLI generator emits v2 only**. No tracked
`.ncpe` artifact or required production NCPE v1 asset exists. The normal Solar
route uses its authored compact definition. Public source and historical format
guidance establish developer exposure; binary/package interchange distribution
and particular external files were not established.

V1 support means existing valid v1 codec behavior, including canonical body-ID
ordering and stored per-body position/velocity error bounds. It does not promise
runtime reconstruction. The byte-only v2 loader continues to reject v1 with
`UnsupportedV1Reconstruction`. Its discriminator is required independently of
the offline codec. Shared `NormalizedEphemerisSample`/`EphemerisHash` also serve v2.

Migration to v2 is semantic re-authoring, not a header edit: supply explicit time
mapping, complete body identity/physical catalog, sources, bindings, payloads and
provenance through `NcpeV2Definition`. Preserve samples and their coordinate/time
meaning; write/read v2 and validate reconstruction/hash agreement. V2 preserves
declaration order in its identity; v1's permutation equality must not be imposed
on it. V2 has no named per-body error-bound fields: retain those measurements in
explicit migration provenance instead of silently discarding them. No general
converter exists, and missing semantic inputs must not be fabricated.

Existing [format tests](../tests/NovaCore.EphemerisBuilder.Tests/Program.cs) retain
v1 round trip, canonical ordering, corruption and duplicate rejection alongside
v2 reconstruction/hash/copy checks. V1 is retained for the defined public
offline contract, not because an old fixture happens to call it.

## Camera and position semantics

[CameraMath](../src/NovaCore.Core/Camera/CameraMath.cs) is a small public vocabulary
for rotated local +X, +Y and -Z axes. Each helper delegates to
`DoubleQuaternion.Rotate`, including its normalization and invalid-quaternion
behavior. It does not resolve a reference frame or choose camera orientation.
The direct expressions in the matrix are equivalent operations, not evidence
that the helper contract is obsolete. There are no dedicated helper calls in
the current tests; existing camera/frame tests exercise the underlying authority.
This package does not claim a newly run helper regression.

[RelativePosition](../src/NovaCore.Core/Precision.cs) is an FP64 `Double3` value
wrapper with record value semantics. It contains no frame/origin ID, units,
automatic subtraction, finiteness check or narrowing operation. The caller must
retain that context. `FramePosition`/`ReferenceFrameResolver` remain the contextual
position authority; Graphics' `CameraRelativeRenderPosition` performs the actual
root-double subtraction before narrowing. Neither is a drop-in replacement for
an externally meaningful displacement. Supporting this value type does not make
it a second frame resolver or renderer precision owner. No direct local/sample/test
uses were found; the precision responsibility itself is current and distinct.

## RuntimeHost: retired

**RETIRE / RETIRED — HISTORICAL ONLY.** The RuntimeHost resolution applies the
explicit reasonable-engineering-entitlement rule: theoretical identity risk alone
does not establish a supported compatibility obligation. This supersedes HOST-1's
earlier unresolved decision without changing any other API decision in this document.

Before removal, `src/NovaCore.Platform/RuntimeHost.cs` declared only
`public static class RuntimeHost { }` in `NovaCore.Platform`. Reflection on the
existing Release `NovaCore.Platform.dll` confirmed a public, abstract/sealed type:
zero constructors, declared members, interfaces and attributes; no type initializer.
There was no XML documentation. Its only file-history commit is Milestone 1,
`12d3d83695a086f0960b28860e7685153b90fb4a`; it remained empty throughout subsequent
history. That milestone called Platform a future managed-host placeholder while
assigning actual startup to Triangle and native Win32/Vulkan. No implemented facade
or subsequent supported RuntimeHost workflow followed. Platform now has real,
separate logging consumers; its assembly/project remains.

Tracked repository searches and generated-code/config/XML/script searches found
no production, test, tool, sample, reflection-string, serialization, DI, plugin or
interop consumer. The declaration was the only non-document reference. Existing
contract/map references describe the audit decision; the two structure-evidence
references remain historical observations, not active consumers.

Live publication checks on 2026-09-06 found a public repository, no GitHub releases,
zero retained GitHub Actions artifacts and a 404 for the exact `NovaCore.Platform`
NuGet flat-container ID. Inspected package/workflow history has no formal SDK or
package-publication contract. Public Git source and locally built/deployed DLLs
did expose the identity; absence of listings does not disprove private feeds,
copied DLLs or source downloads. No actual downstream identity consumer, promised
facade or supported publication obligation was identified.

Removal can break hypothetical source aliases/`typeof` references, binary type
resolution, reflection by full name, assembly-scanning expectations or serialized
type names. A static type had no instance constructor or behavioral DI/plugin
contract. These are theoretical risks, not evidence of a supported consumer.
The always-empty history, absent supported workflow and explicit decision rule
justify retirement rather than indefinite retention. Internalization is unjustified
because no internal caller needs the type. No replacement or future facade is added.

RuntimeHost resolution baseline: initially clean `main`; HEAD, `origin/main` and
live remote main `74364dfda880e11e04074240df9c90bcd1f410c0`, the banked public API
contract package. P2S5H still targets `32ffac50ab5c06518ede24edfb5c531976d4ec99`.
The source removal and direct contract/map updates are an unbanked candidate.
Existing logging tests and both solution builds validate retained Platform usage;
no dedicated RuntimeHost test exists or needs to be preserved.

Resolution validation: full Debug and Release solution builds passed with zero
warnings/errors, including samples/tools. Precision.Tests passed in both
configurations, including retained logging-option behavior. Reflection on rebuilt
Platform assemblies confirms RuntimeHost absent and only `LogCategory`/`LogOptions`
exported; all three deployed copies per configuration match their source binary
SHA-256. Repository/generated-code searches now find only current decision docs
and historical evidence. Document links/anchors and `git diff --check` pass.
No other contract-matrix row changed; no runtime dependency appeared, so no
Graphics/Florida gate was reopened. Nothing staged, committed, pushed or tagged.

## Proof/preparation ownership

[The public proof class](../src/NovaCore.Graphics/PlanetarySphericalBillboardNaturalTerrainProof.cs)
combines an actual GPU proof runner with reusable physical-query preparation.
Its preparation methods use resolved Earth inputs, topology/pupil directions and
a caller-owned canonical cache; defaults include a native shader build path.
These are repository-oriented developer APIs, not a new portable engine SDK.
The public records/constants/cache are part of the same exposed calling surface.

The [moving coordinator](../src/NovaCore.Graphics/PlanetaryProductionSphericalBillboardMovingRuntime.cs)
registers preparation delegates in two constructors. The ordinary Earth sample
uses the cull-contract overload, which sets `nativeGpuPhysicalPreparation=true`;
its preparation branch bypasses `_prepare` and schedules native preparation.
Thus a constructor reference alone does not make the proof method the current
production NCSM1 preparation owner. The non-native/reference branch and focused
tests still use it. Native residency/readiness/publication remains authoritative
for the accepted production route. No implementation, residency or TES change
follows from clarifying this distinction.

`Run` accurately names permanent proof work. Moving preparation under a new public
type would be a compatibility migration, not an internal rename. Retain current
entry points; a later justified extraction must describe its replacement and
forwarding/removal policy instead of adding an unplanned duplicate API today.

## Original decision evidence — 2026-09-06 snapshot

Initially clean `main`; HEAD, `origin/main` and live remote main:
`a0595b31a90ed2b705b86255d7eaffb2477e18a9`. Prior debt packages, including structure
and Graphics, are banked. P2S5H remains
`32ffac50ab5c06518ede24edfb5c531976d4ec99` through
`m12d-p2s5h-earth-route-convergence`. That documentation package was subsequently
banked at `74364dfda880e11e04074240df9c90bcd1f410c0`; the RuntimeHost resolution
above records the later, separately authorized retirement decision.

Current tracked source, sample/test callers, project metadata and file history
were inspected. The four relevant Release DLLs exist. CameraMath and
RelativePosition were already exposed in published v0.5.0 source; RuntimeHost
dates to Milestone 1. No dedicated package/public API baseline or release-version
compatibility policy was found in inspected repository configuration. These
observations prove exposure, not a census of external consumers.

Live [repository metadata](https://api.github.com/repos/AlbatronicProductions/NovaCore)
reports a public repo with zero forks; the [release list](https://api.github.com/repos/AlbatronicProductions/NovaCore/releases)
is empty. Exact NuGet flat-container queries for `novacore.core`,
`novacore.platform`, `novacore.graphics` and `novacore.ephemerisformat` returned 404.
No claim is made about alternate IDs/private feeds, copied DLLs, source downloads,
private consumers or completeness of code search. The old phrase “separately
published v1 codec” is therefore replaced with **public source/assembly codec**.

KSA comparison is deliberately limited. Installed `KSA.dll` still reports
`2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`, SHA-256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`, matching the
banked reference provenance. Local save metadata and complete tree-serialization
responsibilities were reread; the earlier verified break announcements remain
historical evidence in the compatibility audit, not newly queried Discord data.

| KSA responsibility | NovaCore equivalent | Decision |
|---|---|---|
| Versioned saves and complete semantic serialization; explicit historical break announcements | NCPE complete semantic reconstruction and announced migration | ADAPT the explicit migration boundary; a version field cannot restore missing semantics |
| Game save/API consumers belong to KSA's own distribution | NovaCore public source/DLL consumers | INTENTIONALLY DIFFER: KSA history cannot authorize removing NovaCore APIs or prove no consumers |

No equivalent KSA contract for these exact helpers/NCPE was established. No KSA
source or assets were copied. That original package changed documentation only:
supported behavior and existing tests remained intact; no build/GPU/manual
acceptance result was claimed. Validation used Markdown path/anchor checks and
`git diff --check`. No temporary capture/artifact package or API removal occurred.

Historical closeout: all 32 checked relative document paths/anchors resolved;
all 13 matrix rows had the required columns; `git diff --check` passed. Seven
Markdown files changed; no builds/tests were required. The original classification
was **PARTIAL PUBLIC CONTRACT RESOLUTION — FOLLOW-UP REQUIRED**, with HOST-1
unresolved at that time. Its later resolution is recorded above; other surface
decisions remain unchanged.
