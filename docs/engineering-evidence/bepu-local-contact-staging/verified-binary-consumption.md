# Verified BEPU binary consumption

Lead judgment: **PASS**.

**VERIFIED BEPU BINARY CONSUMPTION — READY FOR PROJECT CONTROL ACCEPTANCE**

UNBANKED. Dependency/bootstrap only. No solver implementation, Windows trust
repair, milestone assignment, staging, commit, tag, merge or push.

## Baseline

Branch `codex/bepu-local-contact-staging`.
HEAD/main/origin/main: `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
M14.17 tag `m14.17-certified-continuation-publication` still targets
`a890a12c5cfb6c1eb4ee15906a21161bc4930988`.
Windows SDK 10.0.303, runtime 10.0.12. Against the initial 501-file fingerprint
record, only Simulation's project file changes. Existing production and permanent
test C# files are byte-identical. A new dependency-only smoke test is added.

## Independent qualification and identities

The user-supplied Production Development Work reports were inspected read-only:
`analysis/bepu-independent-qualification/REPORT.md`, `admission-record.json`,
`linux-result.json`, and `analysis/bepu-acquisition-assessment/ASSESSMENT.md` under
the external project `g-p-6a9b1882b430819186f804e03f2e9735`.
No external report was moved into production. Their qualification provenance and
hashes are retained in the manifest/results. Current original archives and extracted
DLLs match the independently downloaded/verified Linux identities exactly, including
NuGet content hashes and both exit-zero signature results.

[GitHub run 34737772989](https://github.com/AlbatronicProductions/NovaCore/actions/runs/34737772989)
was independently observed as successful. Qualification at 2026-09-13T04:23:42Z
used healthy Ubuntu 24.04.5, SDK 10.0.303, runtime 10.0.11, standard NuGet signature
and timestamp validation. No imported Windows roots or trust bypass. This
authenticates these specific artifacts, not future downloads or vulnerability health.

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| BepuPhysics 2.5.0-beta.29 nupkg | 442,721 | D4A952F5417B53B08DE20D507C21745C9DD004E2529864098DFEC7A2CFF11E42 |
| BepuUtilities 2.5.0-beta.29 nupkg | 149,735 | 8168EC4905F3AD5F923564557DAAB2BAB1100EE46B424CA5F1C97BD7684DE19F |
| BepuPhysics.dll | 880,640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163,328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

The [production manifest](../../../external/bepu/2.5.0-beta.29/manifest/bepu-2.5.0-beta.29.json)
retains whole-archive SHA-512, separate NuGet content hashes, signed-chain
fingerprints, source commit, framework, license, qualification and update policy.
Its SHA-256 is `C76F2DE75DAC81C8695109977E1F8A4DCCAFF29DA0BB96884C46C0B33362240E`.

## Repository layout and verification

`external/bepu/2.5.0-beta.29/{packages,lib,manifest}` contains original archives,
unmodified extracted net8.0 DLLs, and pinned provenance. License accompanies them.
`Bepu.references.props` declares explicit strong-name references and relative
HintPaths. `Bepu.targets` contains reviewed literal trust pins and built-in MSBuild
[VerifyFileHash](https://learn.microsoft.com/en-us/visualstudio/msbuild/verifyfilehash-task)
checks; missing files are errors, not fallback requests.

Before compiler/reference consumption, hashes of the DLLs, manifest and license
must match. Resolved reference bytes and build/publish output bytes are also
checked. No timestamps, filename, version string or mutable manifest alone can
authorize bytes. Pins/build logic themselves remain reviewed trusted inputs.

`VerifyBepuPackages` checks both original archives by SHA-256 and whole-archive
SHA-512. It runs explicitly for clean/update qualification and automatically for
consumers under `ContinuousIntegrationBuild=true`. Ordinary incremental builds
do not rehash archives. There are no BEPU PackageReferences, package locks, NuGet
warning suppressions, signature-verification changes or machine trust changes.

## Reference integration and transitive closure

The initial direct-reference smoke exposed that unused raw DLL references do not
automatically copy downstream. The final build boundary therefore adds approved
references to Simulation and consumers after the SDK expands transitive project
references. No artificial production call was added to force assembly usage.
An additional disposable intermediate project proves indirect propagation.

Runtime binding inspected all 736 Physics and 91 Utilities type definitions.
Physics depends on Utilities. All other referenced assemblies resolve within the
installed .NET shared framework: System.Runtime, Numerics.Vectors,
Runtime.Intrinsics, Collections, Threading, Threading.Thread, Console, Memory and
Runtime.InteropServices. No third third-party assembly, native runtime asset,
analyzer or package build asset is required.

Both assemblies have version 2.5.0.0. Physics public-key token is
`9345ce38ee48a1cd`; Utilities token is `a595ef937c270ac9`. The smoke requires exactly
one loaded assembly of each name, loading from the application directory with the
approved hash. It constructs no world, body, mesh, contact, solver or staged state.

## Build, clean export and output validation

| Gate | Result |
|---|---|
| Full solution Debug, CI provenance enabled | PASS; 0 warnings/errors; 3.47 s |
| Full solution Release, CI provenance enabled | PASS; 0 warnings/errors; 3.89 s |
| Simulation Debug and Release | PASS, including full solution builds |
| Fresh managed candidate export, empty cache and sources, Debug | PASS; 1.910 s |
| Fresh Release outputs with hostile same-version cache/source | PASS; 1.762 s |
| Debug/Release dependency runtime binding | PASS |
| Direct and indirect Simulation consumers | PASS |
| Publish without rebuilding, published runtime binding | PASS |
| Triangle Debug/Release output identities | PASS, both exact DLL hashes |
| Final restored provenance/build/runtime smoke | PASS |
| Git diff check | PASS |

The clean export copies only tracked current source for Simulation/Core/
EphemerisFormat plus the candidate dependency files and new smoke test. It starts
without bin/obj or restored BEPU assets. It is an **unbanked candidate export**, not
a claim that a candidate commit exists. Full solution builds use the canonical
tree's existing native DLL/shaders/terrain assets; clean dependency validation
does not duplicate or qualify that unrelated native/content bootstrap.

Both the isolated cache and a competing local source offer altered same-ID/version
archives; cache DLLs are also altered. Build assets contain no BEPU package entries.
The approved repository DLLs still compile, copy, load and publish. Real global
NuGet cache/configuration are not changed. Parent environment is restored.

The prepared CI workflow covers Windows/Linux and Debug/Release, empty package
sources, exact SDK, archive verification, builds and output/publish binding.
Workflow actions are pinned to verified upstream commit IDs. It has **not been
pushed or executed**. Current consumption execution evidence is Windows; Linux
independently authenticated the artifacts, not this new build integration.

## Tamper, missing and substitution controls

All alterations occur only in disposable exported trees and are restored through
guaranteed cleanup. The final reproduction has 23 recorded steps plus an explicit
CI-build archive-tamper witness. The retained script includes that CI witness too.

| Control | Outcome |
|---|---|
| One changed byte, BepuPhysics.dll | Build rejects, MSB3952 |
| One changed byte, BepuUtilities.dll | Build rejects, MSB3952 |
| Changed manifest bytes | Build rejects, MSB3952 |
| One changed byte in each original nupkg separately | Provenance rejects, MSB3952 |
| Changed Physics archive during a CI-mode build | Build rejects, MSB3952 |
| Each DLL renamed away from expected name | Build rejects, MSB3954 |
| Missing manifest | Build rejects, MSB3954 |
| Missing original Physics package | Provenance rejects, MSB3954 |
| Valid wrong managed assembly substituted at Physics path | Build rejects, MSB3952 |
| Corrupted previous build-output DLL, publish --no-build | Approved source is published; runtime hash/identity PASS |

The wrong-assembly fixture is NovaCore.Core 1.0.0.0 renamed to the expected DLL
path; both name/version are wrong. Hash identity is the decisive gate rather than
a version-parser fallback. No unqualified assembly is executed.

Two preparatory runner checks were corrected: missing-file errors use MSB3954,
and publish legitimately re-resolves the approved repository reference instead of
consuming the corrupted build-output copy. The final runner tests those real
contracts, including published runtime identity. Earlier intermediate logs are
disposable; conclusions and final witnesses remain reproducible.

## License and size

Apache-2.0 license copied byte-for-byte from upstream source commit
`f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`. Neither archive nor the non-truncated
upstream source-tree inventory contains a NOTICE file. License and attribution
are copied to `third-party/Bepu` in output/publish directories.

| Retained artifact class | Bytes |
|---|---:|
| Original nupkgs | 592,456 |
| Approved extracted DLLs | 1,043,968 |
| Manifest | 5,426 |
| License | 10,140 |
| Attribution | 374 |
| Above artifacts total | 1,652,364 |

The small additional build rules, tests, documentation and concise evidence are
source, not duplicate packages. Archives preserve signed provenance; extracted
DLLs simplify deterministic consumption. Both copies are justified.

## Build-time overhead and runtime impact

Three interleaved paired repetitions per build mode compare identical disposable
source/reference closures with and without verification targets. Control removal
exists only in the disposable tree, with no production opt-out. Both arms are
warmed first; ordering reverses for the middle pair. No timing threshold is invented.

| Mode | Control median | Verified median | Observed median difference |
|---|---:|---:|---:|
| Incremental build | 0.942 s | 0.985 s | +0.043 s |
| Clean-output Rebuild | 1.400 s | 1.724 s | +0.324 s |

Incremental ranges: control 0.930–1.067 s, verified 0.984–0.998 s. Rebuild ranges:
control 1.382–1.444 s, verified 1.723–1.730 s. These are bounded local build-wall
observations; Rebuild clears outputs but retains SDK/compiler/cache state. They
are not universal cold-machine costs or isolated CPU hashing benchmarks.

Standalone DLL/manifest/license verification median 0.379 s including MSBuild
startup; hash-task totals 7–8 ms. Full package provenance median 0.380 s including
startup; hash-task totals 10 ms. No premature hashing optimization warranted.

**Runtime frame cost: ZERO added work.** Verification lives in build targets;
production C# and runtime semantics remain unchanged. Deployed files/deps metadata
increase storage. No claim that all future BEPU physics or startup metadata costs
are zero. No renderer/GPU campaign was run.

## Update policy and review

The [dependency README](../../../external/bepu/README.md) defines official explicit
version acquisition, healthy independent signature/timestamp qualification,
archive/content/DLL identities, closure/license/advisory review, compatible
regressions and deployment validation, Project Control acceptance, then banking.
No floating updates, same-version substitution or automatic updates. Raw references
are not enumerated by ordinary NuGet package audits, so release/update review owns
advisory and trust-freshness checks.

Strongest review objection: copied or transitive references could bypass source
pins. Direct/indirect consumers, resolved-reference checking, output/publish hashes,
hostile cache/source and runtime identity checks address it. The build does not
defend against a malicious edit to its own reviewed trust pins or arbitrary mutation
after a completed verified build. Deployment must preserve qualified output bytes.

## Evidence and reproduction

[verified-binary-results.json](verified-binary-results.json) retains exact artifact
identity, external report hashes, final 23-step witnesses, closure, production
fingerprint comparison, outputs and all bounded timing samples. The retained
manifest carries package provenance; original external authentication reports are
unchanged and remain at their supplied location.

Run `tools/dependencies/Test-BepuBoundary.ps1` for the isolated boundary controls.
For the same build comparison, pass its generated checkout path to
`tools/dependencies/Measure-BepuVerification.ps1`. Full solution build commands use
`-c Debug` / `-c Release -p:ContinuousIntegrationBuild=true`.
No stack profiler, bulk runtime capture or solver fixture is needed.

The generated dependency-validation directories are disposable after concise
results are retained. Prior AuthRoot/trust recovery material is outside this
cleanup scope. Final cleanup accounting is recorded in `verified-binary-cleanup.json`.

Automatic approval review rejected the reviewed cleanup before execution as
"blocked by policy". No retry or alternate deletion mechanism was used. Disposed:
0. Remaining reviewed new scratch: **1,867 files / 214,657,246 bytes**.
The three concise new evidence files occupy 38,396 bytes before this cleanup
paragraph; original archives/DLLs stay in the approved repository boundary.

Manual cleanup, only the three inspected generated directories (no reparse points):

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\build\bepu-binary-validation\20260913-045054-162', `
  'E:\NovaCore\build\bepu-binary-validation\20260913-045157-600', `
  'E:\NovaCore\build\bepu-binary-validation\20260913-045232-566' `
  -Recurse -Force
```

This outstanding evidence-hygiene action does not change the technical dependency
qualification. It must not be reported as completed cleanup.

## Git and handoff

Tracked edits: `.gitattributes`, `NovaCore.sln`, Simulation's project file.
New: root build targets, BEPU artifact boundary, dependency test, two reproduction
scripts, prepared CI workflow, and concise evidence updates. Existing staged set
remains empty. The old unbanked PackageReference proposal is fully superseded.
All existing runtime and permanent test implementation fingerprints are unchanged.

Return UNBANKED. Stop for Project Control before any solver implementation.
