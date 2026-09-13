# Staged finite-body contact: dependency follow-ups

Final qualification follow-up: [Hosted consumption dispatch blocker](hosted-consumption-qualification.md).
Local qualification remains valid; hosted candidate CI cannot start under the
current no-commit/no-push boundary because the candidate is unpublished. UNBANKED.

Latest: [Verified BEPU binary consumption](verified-binary-consumption.md) qualifies
the exact independently authenticated package/DLL pair through a fail-closed build
boundary. Dependency candidate ready for Project Control acceptance, UNBANKED.
The prior PackageReference proposal is superseded. No solver implementation has
resumed; Windows host trust remains abnormal and has not been modified.

## Historical admission and Windows recovery attempts

Latest follow-up: [Healthy AuthRoot baseline availability](healthy-authroot-baseline.md)
confirms no healthy comparator is available. Sandbox/Hyper-V are disabled; the
current CryptSvc token query was access-denied. A read-only baseline collector
is prepared. The expected CRLs ACL remains unqualified and no repair is proposed.
[AuthRoot ACL qualification](authroot-acl-qualification.md)
proves CryptSvc `RegCreateKey` denial on `AuthRoot\CRLs`, requesting Read/Write,
Delete. The exact healthy child ACL remains unqualified; no repair was proposed
or performed. [Windows host trust recovery](host-trust-recovery.md) preserves the
earlier store-level CAPI2 evidence. The registry CTL remains stale and ordinary
chain trust still fails.
[Prior dependency trust recovery](trust-recovery.md) preserves package and chain
identity evidence. Adapter implementation has not resumed. The initial attempt
below remains a historical record; its no-retry statement applies to that attempt only.

2026-09-12. UNBANKED. No milestone assigned. This is a stopped dependency
admission attempt, not an implemented or validated solver candidate.

## Baseline and scope

HEAD/main/origin/main: `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
M14.17: `m14.17-certified-continuation-publication`, target
`a890a12c5cfb6c1eb4ee15906a21161bc4930988`.
Started on clean main and created only `codex/bepu-local-contact-staging`.
`baseline.json` records all refs after branch creation and SHA-256 identities of
501 tracked source/test/native/sample/tool files. Final comparison found only
`src/NovaCore.Simulation/NovaCore.Simulation.csproj` changed.
No implementation or permanent test file changed. Nothing staged.

The project-file proposal pins BepuPhysics and BepuUtilities to exactly
`[2.5.0-beta.29]` and enables a package lock. It has NOT restored successfully.
The failed restore emitted a lock containing only project references; that
incomplete generated lock was removed, not retained as dependency evidence.

## Dependency identity

`dependency-provenance.json` records current official package vs installed DLL
SHA-256 comparisons, exact nuspec metadata, source commit and license.
Both comparisons passed before adding the package references.

| Package | Version | DLL SHA-256 |
|---|---|---|
| BepuPhysics | 2.5.0-beta.29 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities | 2.5.0-beta.29 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

Official package source: NuGet.org. Source repository:
`https://github.com/bepu/bepuphysics2`, commit
`f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`. License: Apache-2.0.
Packages supply managed net8.0 assemblies; the intended deployment is ordinary
NuGet runtime dependencies, never a dependency on the installed KSA directory.
Deployment has not been validated.

## Actual blocking result

One command was executed, with SDK 10.0.303 and ordinary settings:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet restore src/NovaCore.Simulation/NovaCore.Simulation.csproj --verbosity minimal
```

Exit code: **1**. Both packages produced both errors:

- **NU3018:** The repository primary signature's signing certificate is not
  trusted by the trust provider.
- **NU3028:** The repository primary signature's timestamping certificate is not
  trusted by the trust provider.

Repository signer reported by NuGet:
`CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft,
L=Redmond, S=Washington, C=US`.
Signer certificate SHA-256:
`1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D`.
Reported validity: 2024-02-22 19:00 through 2027-05-18 19:59:59 local time.

This establishes normal package trust verification failure on this host. It
does not establish package byte mismatch, malicious packages, certificate
revocation, the specific chain failure, or a Bepu architecture defect. DLL hash
equality is not a replacement for successful normal package verification.

No signature verification disablement, trust-store change, alternate DLL
substitution, offline verification override, runtime-policy change or retry was
performed. Resolving the host/package trust path requires separately reviewed
action; the implementation did not continue around it.

## Confirmed time clarification

Project Control explicitly confirmed keeping the assessed mapping:

`T(n) = SourceStart + floor(n * 1_000_000 / 60)`.

Public NovaCore time remains 1,000,000 ticks/second. Private 60 Hz endpoints use
16,666/16,667-tick intervals, with checked arithmetic and float dt derived from
the admitted interval. No time implementation changed. The brief's conflicting
60-ticks/second phrase is not a mandate to change canonical time.

## Read-only preparation retained for resumption

The independent authority review identified existing engine-owned read APIs in
`SimulationTransactionEngine.ContactProof.cs` and coherent motion evaluation in
`SpacecraftMotionEvaluator.cs`. Proposed adapter binding must retain the engine
identity, copied source state, revisions and entry epoch; a borrowed live view
is not a durable source snapshot.

Proposed readonly staged receipts should carry private world identity and a
successful generation. Reading a receipt must revalidate source/configuration,
world lifetime and current private frontier. Private advancement must never
advance canonical state. A post-step qualification failure must invalidate the
advanced world, preventing retries from an unacknowledged future state.

Review identified negative tests for foreign engines, source/configuration
changes even with reused numeric IDs, pending canonical events, expired
receipts, wrong threads, disposed worlds, unsupported torque, and canonical
nonmutation. These are design findings only; none was implemented or tested.

## Predeclared validation, not executed

After dependency admission and implementation: focused adapter tests, Debug
and Release full solution builds, Simulation/ReferenceFrames/Precision in both
configurations, affected M14 predecessor gates, lifetime, repeatability and
checked isolated zero-allocation measurements with positive control.

Performance campaign: three fresh Release processes, each 128 warm steps plus
1,024 measured full-step/export samples under normal runtime behavior. Separate
equivalent fixtures for allocation measurement. Preserve the ticket's
0.05/0.10/0.25/0.50 ms median/P95/P99/max gates and 8 MiB retained-storage bound.
Stop on the first acceptance failure; no retry-until-green behavior.

No adapter, contact, precision, allocation, timing, memory, lifecycle or
determinism acceptance result exists from this attempt. No full solution build,
test suite, renderer/GPU campaign or performance benchmark was run.

## Evidence and next boundary

Retained package budget: 128 KiB. Retain this stop report, baseline identities,
dependency provenance and final identity/status check. No assessment scratch
was copied; previous assessment cleanup remains separate and was not retried.
Normal ignored NuGet/MSBuild restore metadata may reflect the failed restore
and is not accepted deployment evidence. No bulk diagnostics were created.

After Project Control resolves or explicitly authorizes resolution of package
trust, resume at normal exact-version restore and verified complete lock-file
creation. Do not treat the current project-file proposal as a working candidate.
Do not substitute a package version or bypass trust verification by inference.

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

Final classification: **BLOCKED**.

Proposed accomplishment remains: **Qualify staged finite-body contact stepping
in a bounded local Bepu world**. UNBANKED. STOP FOR PROJECT CONTROL.
