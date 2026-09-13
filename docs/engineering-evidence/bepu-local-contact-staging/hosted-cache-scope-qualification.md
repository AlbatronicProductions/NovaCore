# Verified BEPU consumption — hosted environment correction qualified

Lead judgment: **PASS**.

**VERIFIED BEPU BINARY CONSUMPTION — READY FOR PROJECT CONTROL ACCEPTANCE**.
UNBANKED. Solver **NOT STARTED**. Runtime frame work added: **ZERO**.

## Baseline and bounded correction

HEAD/main/origin/main remain `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
The candidate started on `codex/bepu-local-contact-staging`, with nothing staged.
All 17 inputs matched the previous qualified snapshot before modification.

The only implementation change is `.github/workflows/verified-bepu.yml`: remove
the invalid job-level `runner.temp` expression and add a PowerShell runner step.
That step takes `RUNNER_TEMP`, refuses an already-existing cache directory,
creates an empty `bepu-empty-cache`, checks zero entries, and exports
`NUGET_PACKAGES` through `GITHUB_ENV` for subsequent steps. No hardcoded host path.
The SDK-selection, empty-source, build, inspect, publish and inspect sequence is
unchanged. Both OSes and both configurations remain mandatory.

Checkout/setup-dotnet action commit pins and SDK 10.0.303 remain unchanged.
`dotnet new globaljson --sdk-version 10.0.303 --roll-forward disable` remains.
No dependency binaries, manifests, targets, project wiring, production C#,
existing tests, solver code or Windows trust settings were changed.

## Temporary identity and snapshot equivalence

Temporary branch: `codex/validation-bepu-cache-scope-20260913`.
Temporary commit: `fbeec05c78359189b25c33109d332f5b8c6474d5`.
Relative to prior temporary commit `2063928ef89ce79b12bf6f1f1184619c59ee8547`,
the entire diff is one workflow file, **+9/-2**. The other 16 inputs are byte-identical.

Corrected workflow SHA-256:
`FC6C0C08A3C71ABF783A72C07DE64641033A5D21F04DB804EDBC75C98F48F501`.

An alternate Git index created the snapshot without staging the canonical index.
All 17 committed input blobs were checked against current local SHA-256 values.
The temporary snapshot retains exact local bytes, including solution line endings.

[Draft PR #2](https://github.com/AlbatronicProductions/NovaCore/pull/2) triggered
the unchanged pull-request matrix. GitHub checked out merge witness
`13692c61bdb3c4721b8dc2ecba4d88b903c09b9c`. Its tree,
`5ab1968fa38e2a116cb5d82a6cf1f3bc9c51db99`, exactly equals the temporary candidate
tree. This was independently checked through GitHub's commit API. No differing
merge contents were accepted as candidate evidence.

## Hosted run and matrix

[Run 34740497443](https://github.com/AlbatronicProductions/NovaCore/actions/runs/34740497443):
**SUCCESS**, attempt 1, event `pull_request`. Created/started 2026-09-13T05:31:10Z;
completed metadata updated 05:32:19Z: **69 seconds** elapsed. No job retry,
skipped leg, allowed failure or correction after execution started.

| Hosted leg | Job ID | Job elapsed | Build elapsed | Result |
|---|---:|---:|---:|---|
| Windows Debug | 103679269911 | 65 s | 11.62 s | PASS |
| Windows Release | 103679269915 | 47 s | 13.77 s | PASS |
| Ubuntu Debug | 103679269867 | 38 s | 12.90 s | PASS |
| Ubuntu Release | 103679269916 | 34 s | 12.71 s | PASS |

Windows runners: Microsoft Windows Server 2025, 10.0.26100 Datacenter;
image `windows-2025-vs2026`, version `20260907.229.1`.
Ubuntu runners: Ubuntu 24.04.5 LTS; image `ubuntu-24.04`, version `20260907.300.1`.
Runner agent version 2.337.0. These are actual job-log observations, not assumed
meanings of `windows-latest`.

## Checkout, empty cache and package sources

Every job passed clean checkout of the equivalent merge tree, exact SDK setup,
cache creation, SDK pin selection and empty package-source configuration.
Both Windows logs report `D:\a\_temp\bepu-empty-cache`; both Ubuntu logs report
`/home/runner/work/_temp/bepu-empty-cache`. These are observed runner-owned paths,
not literals in the workflow. Each logs **initial entries: 0**, and subsequent
build/run/publish steps show the matching `NUGET_PACKAGES` environment value.

Each creates repository `NuGet.Config` containing
`<configuration><packageSources><clear /></packageSources></configuration>`
before the dependency build. No external package source or developer cache can
provide BEPU. Setup-dotnet caching is not enabled. SDK framework packs are normal
toolchain inputs; no attempt was made to exclude framework dependencies.

The existing `.gitattributes` preserves DLL, archive, manifest and license bytes.
`ContinuousIntegrationBuild=true` activates the existing archive/manifest/license/
DLL identity checks before reference consumption. Their success on both checkout
platforms establishes actual checked-out byte identity, rather than relying only
on local hashes. Missing/mismatched hashes remain hard errors.

## Build, runtime binding and publishing

All four builds completed with **0 warnings / 0 errors**. The job logs contain no
GitHub warning/error annotations. All four runtime inspections passed with
`--no-build`. All four publish steps passed with `--no-build`, and all four
published dependency executables passed.

Each inspection independently verifies both output file hashes, runtime resolution
from the application directory, strong-name/version identity, exactly one loaded
assembly of each name, framework-only remaining closure, and license/attribution
deployment. Thus eight inspection outputs agree: four build outputs and four
published outputs. No solver world or body was constructed.

## Exact BEPU identities

Both packages remain version **2.5.0-beta.29**, with extracted net8.0 assemblies.

| Artifact | SHA-256 |
|---|---|
| BepuPhysics nupkg | D4A952F5417B53B08DE20D507C21745C9DD004E2529864098DFEC7A2CFF11E42 |
| BepuUtilities nupkg | 8168EC4905F3AD5F923564557DAAB2BAB1100EE46B424CA5F1C97BD7684DE19F |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| Manifest | C76F2DE75DAC81C8695109977E1F8A4DCCAFF29DA0BB96884C46C0B33362240E |

Runtime output reports assembly version 2.5.0.0 for both. Physics public-key token:
`9345ce38ee48a1cd`; Utilities: `a595ef937c270ac9`. Type metadata counts remain
736 and 91. Package identities are enforced by the existing CI provenance gate;
DLL/output identities are also explicitly emitted by all eight inspections.
The earlier independent signature/timestamp qualification remains the admission
authority; this workflow does not claim a new signature-verification campaign.

## Cross-platform disposition

Windows Debug/Release and Ubuntu Debug/Release qualify the same dependency
consumption contract. The parser failure is corrected, with no subsequent
architecture contradiction observed. No unrelated local build or renderer
campaign was repeated. No timing/performance contract changed.

## Temporary Git retirement and restoration

Draft PR #2 was closed without merge. The temporary remote and local branches
were deleted and verified absent; remote main remains at the production baseline.
No temporary worktree was created. No tags were created or moved. Only the
authorized temporary validation branch was pushed; no banking occurred.

Per the final-restoration instruction, the canonical checkout was returned to
**main**, preserving the complete **uncommitted candidate**. Main is not a clean
working tree: those candidate changes intentionally remain. The development
branch `codex/bepu-local-contact-staging` remains available at the same baseline.
No reset, clean or candidate deletion was used.

Of the original 1,812 pre-existing files, only the authorized workflow changed.
All 1,814 files recorded after the correction remained byte-identical through
snapshot creation, hosted execution and branch retirement. Final staged set is
empty; `git diff --check` passes. Production and solver code are unchanged.

## Snapshot cleanup

**TEMPORARY SNAPSHOT FILES RETAINED — MANUAL PROJECT CONTROL CLEANUP REQUIRED**.

The five previously reported files remain, 530,615 bytes. Their exact manual
command is retained in `ephemeral-ci-qualification.md`; it was not retried here.
Three files created for this run remain, 516,587 bytes. Their useful input identities
and preservation results have been consolidated in concise evidence. Automatic
approval review blocked their nonrecursive removal before execution, and no
alternate mechanism was attempted. Total pending snapshot scratch: **8 files,
1,047,202 bytes**. No binary build tree, raw hosted log or runtime capture was added.

New run's separately reviewed temporary-file cleanup:

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\build\bepu-hosted-cache-scope\create_snapshot.py', `
  'E:\NovaCore\build\bepu-hosted-cache-scope\snapshot.index', `
  'E:\NovaCore\build\bepu-hosted-cache-scope\snapshot.json' `
  -Force
```

## Durable evidence

- `hosted-cache-scope-identity.json`: exact snapshot inputs, previous-candidate
  equivalence and preservation/ref results.
- `hosted-cache-scope-run.json`: run/job IDs, timestamps, result and merge tree.
- `hosted-cache-scope-jobs.json`: concise job-step, runner, cache, SDK, source,
  build and both emitted inspection witnesses for all four jobs.

Reproduce only under authorization: publish these 17 exact inputs on a temporary
branch from the recorded baseline, trigger the preserved four-leg workflow via a
draft PR, capture the same witnesses, close without merge, retire the branch.
No architecture or dependency identity change is needed.

Stop for Project Control. Dependency consumption is qualified; the candidate
remains unbanked and solver implementation has not started.
