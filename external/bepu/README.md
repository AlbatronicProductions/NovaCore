# Verified BEPU binary boundary

Only BepuPhysics and BepuUtilities **2.5.0-beta.29** are admitted. The original
NuGet.org packages were authenticated independently on healthy Linux by normal
NuGet signature and timestamp validation. Exact provenance and content hashes are
in [the manifest](2.5.0-beta.29/manifest/bepu-2.5.0-beta.29.json); this admission
does not authenticate a future version or same-version replacement.

The original archives remain provenance. The unmodified `lib/net8.0` DLLs are
extracted build inputs; retaining both avoids extraction on every build while
preserving the signed source artifacts. Git attributes preserve all pinned bytes.

## Build ownership

Simulation declares `NovaCoreUsesBepu`. Root `Directory.Build.targets` imports the
small dependency boundary. After the .NET SDK expands transitive project
references, projects depending on Simulation receive the same explicit strong-name
references, pinned repository-relative HintPaths, and license content. This is
needed even before Simulation calls BEPU. It also creates ordinary SDK `deps.json`
runtime entries. There is no runtime loader, hashing loop, solver world, or frame
work added to NovaCore.

`VerifyBepuBinaries` uses MSBuild's built-in `VerifyFileHash` before reference
resolution/compilation. The reviewed targets pin the manifest's exact bytes as well
as the DLLs; changing a manifest cannot authorize replacement bytes. Resolved
references and copied/published outputs are checked too. These build files and
their pins are trusted, review-controlled inputs, not a defense against someone
authorized to rewrite the entire build. Unsupported custom builds that omit the
repository targets are outside this boundary.

No BEPU PackageReference, package lock, global-cache path, package-server lookup,
signature bypass, or host trust modification is used. Other dependencies retain
their normal policies. Built-in SDK hash checks are portable; no PowerShell is
needed for ordinary builds. The red-team reproduction scripts use PowerShell 7.

## Required clean / CI / update qualification

```powershell
dotnet msbuild src/NovaCore.Simulation/NovaCore.Simulation.csproj -t:VerifyBepuPackages
dotnet build tests/NovaCore.BepuDependency.Tests -c Debug -p:ContinuousIntegrationBuild=true
dotnet build tests/NovaCore.BepuDependency.Tests -c Release -p:ContinuousIntegrationBuild=true
dotnet run --project tests/NovaCore.BepuDependency.Tests -c Release --no-build
dotnet publish tests/NovaCore.BepuDependency.Tests -c Release --no-build -o build/bepu-published
dotnet build/bepu-published/NovaCore.BepuDependency.Tests.dll
```

`ContinuousIntegrationBuild=true` enforces archive SHA-256 and whole-archive
SHA-512 as well as DLL verification. The retained **NuGet content hash** is a
distinct signature-aware hash recorded by independent qualification; it is not
misrepresented as the whole-file SHA-512. Exact archive identity binds the original
content-hash result. Ordinary incremental builds need not rehash archives.

The prepared [CI workflow](../../.github/workflows/verified-bepu.yml) runs the
dependency closure on Windows/Linux in Debug/Release with empty sources and an
isolated cache. It does not provision NovaCore's unrelated native/terrain assets.
Do not describe a prepared workflow as a completed hosted run.

For cache, tamper, missing-file, indirect consumer, and publishing controls:
`pwsh -File tools/dependencies/Test-BepuBoundary.ps1`. It exports a fresh managed
candidate source closure into a unique ignored build directory. All altered bytes
and cache/source settings are confined there, with restoration in `finally` blocks.
Retain the compact results; remove only reviewed generated scratch after review.

## Licensing and updates

[LICENSE.txt](2.5.0-beta.29/LICENSE.txt) is the Apache-2.0 license from upstream
source commit `f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`.
Neither approved package nor that source tree contains a NOTICE file.
[ATTRIBUTION.txt](ATTRIBUTION.txt) and the license accompany build/publish outputs.
Upstream source and binaries are unmodified.

For each update: choose an explicit version; acquire official archives; independently
verify signatures/timestamps on a healthy environment; capture archive, NuGet
content and extracted DLL hashes; review assembly closure, source/license/NOTICE
and vulnerability advisories; update manifest and reviewed pins together; run
compatibility, regression, tamper and deployment gates; obtain Project Control
acceptance; bank only after approval. No floating or automatic updates. Raw
references are not covered by ordinary NuGet package-audit enumeration: release
and dependency-update review owns advisory and trust-freshness checks.
