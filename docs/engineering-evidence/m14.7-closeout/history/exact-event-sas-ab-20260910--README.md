# Matched Release SAS A/B discriminator

10 September 2026. **Lead judgment: ESCALATE TO PROJECT CONTROL.**

**OTHER — A/B MATRIX STOPPED AT EARLIER CANDIDATE ATTITUDE-INTEGRATION ALLOCATION FAILURE**

The required stop rule interrupted the predeclared B-C-B-C-B-C matrix on run 2.
SAS association remains unanswered. No retry, profiling, attribution, production
change, permanent test change or acceptance resumption occurred.

## Baseline and preserved candidate

- HEAD/main/origin/main: `00f226096b61a117184464b5619e464d39789574`.
- Branch: `codex/exact-event-spacecraft-motion`.
- M14.6: `m14.6-physical-event-epochs` at
  `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- M14.1-M14.6 tags remain unchanged; nothing staged.
- All seven previously retained production/test SHA-256 fingerprints matched
  before comparison. All 14 existing changed/untracked canonical files were
  snapshotted for non-mutation verification, including documentation/evidence.
- Prior acceptance state remains Debug 38/38 PASS; Release SAS witness 12,336
  bytes with entry/exit PASS, 31 groups passed and six unreached. The new test body
  had not executed before that historical SAS failure.
- SDK 10.0.303. Both observed child processes loaded
  `C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/coreclr.dll`.

## Pre-SAS source and registration audit

| Difference | Classification | Pre-SAS relationship |
|---|---|---|
| SpacecraftTranslationEvaluator shared analytical kernel and rational entry | Production | Canonical translation tests run before SAS and exercise the extracted kernel. |
| SpacecraftAttitudeEvaluator shared constant-rate kernel | Production | Existing attitude and rotation paths execute before SAS. SAS also uses unchanged canonicalization code in this type. |
| SpacecraftRigidBodyRotationEvaluator rational entry | Production | Existing canonical rotation body is unchanged and reachable before SAS; new rational entry is not called by common groups. |
| SpacecraftMotionEvaluator new exact-event method/result | Production | Existing canonical coherent entry remains; new method is not called by common pre-SAS groups. |
| PhysicalEventDuration | Production | New helper for rational evaluators; no call from common pre-SAS groups. |
| Program.cs: three selectors and final group registration | Test/registration | Selectors are tested before the runner, but no arguments activate them. The full test array is constructed before any group executes. One extra tuple slot and Action delegate are registered before SAS despite the new group being last. |
| SpacecraftPhysicalEventMotionTests | Test | Run body is last, after all 37 common groups. Its body cannot have executed before SAS under this invocation. |
| Current state, handoff, contract and retained evidence | Documentation | Not copied to comparison trees or loaded by Simulation. |

Compiled metadata reports the new test class as `Abstract, Sealed, BeforeFieldInit`.
Static field initializers construct value-type IDs, mass and initial physical
states; there is no explicit static constructor in source. Early type initialization
is permitted; its actual timing/allocation was not observed. Registering an Action
does not execute the Run body. Do not turn these facts into a causal attribution.

The exact SAS method including its nested helpers is text-identical after line
ending normalization. OrdinaryAllocationMeasurement and all other common test
files are identical. The only Program.cs differences are three selectors and the
appended registration. Existing five timed windows are unchanged. Normalized SAS
method and helper hashes are in `preflight.json`.

## SAS measurement contract

Group: **SAS sign/frame continuity proof**; method:
`SasSignFrameContinuityProofTests`, Program.cs:255 in the candidate.

Setup uses unit principal inertia; proportional gains (10,10,10), zero derivative
gains for correction, torque limits (100,100,100), and settled thresholds 0.001.
Separate damping configuration uses derivative gains (5,5,5). Fixtures establish
axis sign, reference-frame, quaternion-equivalence, hold and transition contracts.
Measurement uses identity orientation, zero angular velocity, a +90-degree Y
target, unit inertia and the correction configuration.

One identical SAS call immediately warms the measured path. The checked helper
requests 1 MiB with `disallowFullBlockingGC: true`; false entry throws. Only after
successful entry does it open GetAllocatedBytesForCurrentThread. The original
100,000 iterations evaluate SAS and mix requested Y torque bits into the existing
hash. Complete reads the exact counter delta, then exits through finally. Entry
and exit failures propagate. Reporting occurs after exit. RequireZero demands
exactly zero without tolerance, subtraction or fallback.

The loop has no independent per-iteration success/count assertion: it evaluates
and hashes the result. Do not invent such an operand. The existing pre-window
assertions below are independent of allocation; reaching the counter report
establishes that they completed successfully. Individual unprinted numeric values
remain UNAVAILABLE.

| Independent SAS predicate | Run 1 baseline | Run 2 candidate |
|---|---|---|
| Success and reduced orientation error for 3 axes x 4 signed angles | PASS; individual values unavailable | NOT REACHED |
| Prograde/normal/radial target mapping dot > 1-1e-12 and reduced pipeline error | PASS; individual values unavailable | NOT REACHED |
| q/-q error and torque equality; shortest-path step < pi/2 | PASS; individual vectors unavailable | NOT REACHED |
| Near-180 Y torque/error both positive | PASS; individual values unavailable | NOT REACHED |
| Hold identity, negative damping torque, rate magnitude < 0.2 | PASS; individual values unavailable | NOT REACHED |
| Hold zero-error/rate gives exactly zero torque | PASS | NOT REACHED |
| Prograde/radial jumps < 0.03 and fallback counts 0/0 | PASS; reported jumps both 0.02454369 rad | NOT REACHED |
| Normal/antinormal target alignment > 1-1e-12 | PASS; individual values unavailable | NOT REACHED |
| Preferred-up jump <= pi/2+1e-6; transitions exactly 2 | PASS; reported 1.570796 rad / 2 | NOT REACHED |
| Three mode-switch corrections reduce error | PASS; individual values unavailable | NOT REACHED |

Successful baseline summary also reports one-step 90-degree error 1.551161 rad
and hash `0xB88A70CC5EF45185`. These are the existing formatted values, not new
high-precision measurements. The shared deliberate byte[128] allocation control
is in the later Allocation group; no local SAS positive control was added.

## Comparison controls

`prepare.ps1` extracts the exact baseline's four-project dependency closure
(Core, EphemerisFormat, Simulation, Simulation.Tests) and Directory.Build.props:
165 files. The candidate starts from that same closure with the seven retained
candidate source files overlaid (167 files). No runtime assets outside this closure
are needed. Unchanged archive text is LF; candidate-source identity comparison
normalizes CRLF/LF only, with binary resources checked byte-for-byte. Every archive
input and candidate overlay is fingerprinted. No source instrumentation was added.

Both Release builds passed before the matrix with zero warnings/errors. The
matrix used the same absolute dotnet executable, same relative DLL argument, each
arm's root as working directory, and no command-line selectors. The baseline has
37 groups, the candidate 38; all common groups have identical order. Each child
was fresh, with matching complete environment hashes and runtimeconfig hashes.
Only inherited DOTNET_ROOT/DOTNET_ROOT(x86) were present among runtime-prefixed
environment variables; no profiler, tiering, PGO or GC override was added.

Loaded coreclr path was observed once from outside each child using the normal
process module inventory. This is not a debugger, profiler, allocation callback,
GC/JIT trace or in-process observer. Parent observation can affect scheduling;
both arms use the same mechanism. Actual allocation objects remain unobserved.

Comparison source, canonical file and output binary fingerprints were checked
before each run. No builds or retries occurred during the matrix.

## Matrix

| Seq | State | Groups passed before SAS | SAS reached | SAS bytes | Entry | Exit | Other SAS contracts | Exact-event body before SAS | Process result |
|---|---|---:|---|---|---|---|---|---|---|
| 1 | Baseline | 31 | Yes | 0 | PASS | PASS | PASS as detailed above | No; absent | 37/37 PASS, exit 0 |
| 2 | Candidate | 27 | No | UNAVAILABLE | NOT REACHED | NOT REACHED | NOT REACHED | No | 27 passed, attitude-integration failed, 10 groups unreached |
| 3 | Baseline | - | - | - | - | - | - | - | NOT RUN: mandatory stop |
| 4 | Candidate | - | - | - | - | - | - | - | NOT RUN: mandatory stop |
| 5 | Baseline | - | - | - | - | - | - | - | NOT RUN: mandatory stop |
| 6 | Candidate | - | - | - | - | - | - | - | NOT RUN: mandatory stop |

New decisive witness from run 2:

```text
ORDINARY_ALLOCATION gate=attitude-integration bytes=12336 entry=PASS exit=PASS
Unhandled exception. System.InvalidOperationException:
attitude-integration: expected zero managed allocation, actual=12336
at ...SpacecraftAttitudeIntegrationTests... Program.cs:209
at Program.<Main>$(String[] args) Program.cs:84
```

Run 2 process exit was -532462766. The failure is at the earlier spacecraft
attitude-integration group, not SAS. No investigation of that gate followed.
`matrix.json` preserves the run identities, exact runtime/environment fingerprint,
outputs and stop reason; the additional early-gate line is in `stop.json`.

SAS pattern: baseline **[0, not run, not run]**; candidate
**[not reached, not run, not run]**. There is no candidate SAS observation in this
experiment. The historical SAS 12,336 and new attitude-integration 12,336 are
distinct witnesses; equal byte magnitudes do not identify one mechanism.

## VERIFY A / VERIFY B

**Comparison quality:** preflight source, helper, order, build, runtime and
environment controls passed for the two executed processes. The planned SAS
comparison was incomplete because an explicit hard-stop condition occurred;
neither stable nor intermittent SAS separation can be classified from it.

**Causal language:** one candidate earlier-gate failure does not prove which
production method allocates, actual managed allocation, a helper defect, CLR
accounting, a pre-existing problem or shared cause with SAS. Candidate association
for the SAS gate is unestablished. No reuse of the historical 8,160 explanation.

## Next action and reproduction

Return to Project Control for a payoff decision on the newly earlier
attitude-integration witness before authorizing any further comparison. Do not
automatically continue this matrix, repeat acceptance or attribute objects.
No implementation is recommended from these observations alone.

`prepare.ps1` and `matrix.ps1` retain the exact setup and bounded orchestration.
They deliberately refuse existing scratch or an existing matrix result, so these
files cannot be invoked as an accidental retry. Reproduction requires new explicit
authorization and a separately reviewed evidence location. The canonical candidate
is the source of the seven preserved overlays; hashes are retained.

Evidence lives outside the repository to preserve every canonical candidate file.
Disposable comparison trees and raw outputs are classified for removal after
this witness and identities are retained. Storage/final non-mutation results are
recorded in `closeout.json`. No candidate evidence/current-state document was edited.

Cleanup was attempted once after evidence retention. Automatic approval review
rejected deletion with **blocked by policy**, without a more specific reason.
The command did not execute and was not retried. All 543 disposable files
(43,994,366 bytes) remain. The exact absolute target and absence of reparse points
were independently verified. Consolidated manual cleanup, if Project Control
chooses to perform it:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\exact-event-sas-ab' -Recurse -Force
```

**STOP FOR PROJECT CONTROL.**
