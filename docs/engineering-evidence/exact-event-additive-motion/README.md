# Additive exact-event spacecraft motion — bounded result

2026-09-10. **UNBANKED.**

**Lead judgment: ESCALATE TO PROJECT CONTROL.**

**ADDITIVE EXACT-EVENT SPACECRAFT MOTION CANDIDATE — FOLLOW-UP REQUIRED.**

Early normal Release passed 3/3. Final normal Release run 1 failed the unchanged
lunar-orientation gate with **12,336 per-thread counter bytes**. Validation stopped
at that first failure: runs 2–5 and candidate performance were not executed.
This is an acceptance failure witness, not attribution of actual object allocation
or a source-level culprit. No profiling, component control or further repetition
followed. No permanent allocation gate or runtime setting was changed.

## Baseline and preservation

- Main, origin/main and new-worktree HEAD: `00f226096b61a117184464b5619e464d39789574`.
- M14.6: `m14.6-physical-event-epochs` → `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- Sole writer: `E:\NovaCore-ExactEvent-Additive`, `codex/exact-event-spacecraft-motion-additive`.
- Old worktree: `E:\NovaCore`, `codex/exact-event-spacecraft-motion`, preserved untouched.
- Starting new worktree was clean. No staging, commit, tag, merge, push or banking.
- All six M14.1–M14.6 targets unchanged; 327 banked production files unchanged.
- Old 14 accepted candidate fingerprints and 167-file source closure match their
  captured starting hashes; old status still has eight modified tracked paths and
  four untracked entries (one is the evidence directory).
- Eight of nine banked Simulation test files are byte-identical. `Program.cs` adds
  three focused selectors and one final group. All existing test bodies, common
  group order, warmups, allocation boundaries and predicates are unchanged.

The older generalized mechanism was unaccepted: prior reported baseline Release
5/5 versus Candidate E 2/5. Bounded component controls did not isolate a reliable
source culprit; exact-event-only capability was not necessary for the prior
12,336 effect. Those are prior results, not rerun here. Canonical generalization
is avoided to reduce disturbance, not because it was proven to cause that effect.

## Additive mechanism and numerical proof

The [candidate contract](../../spacecraft-physical-event-motion.md) defines the
ownership boundary. Five new production files provide exact local duration,
fractional translation, fractional rotation and immutable coherent motion.
Canonical requests delegate to banked coherent motion; canonical callers never
depend on these helpers. Direct analytical fractional translation avoids a second
rounding at a stored floor position. Rotation evaluates banked dynamics to the
canonical floor, then one fractional RK4 terminal step, or the existing spherical
constant-rate model. Repeating only those equations in a new file is deliberate.

Focused Debug and Release results agree:

| Contract | Evidence |
| --- | --- |
| Exact local duration | 1,000 seeded BigInteger comparisons; signed extrema, borrow, overflow and UInt64 denominator limit |
| Canonical parity | 64 coherent success cases, all value bits; three complete/default refusal cases |
| Translation | 120 rational cases / 360 axes; zero and signed force, velocity, late epochs and large coordinates |
| Maximum position difference | 0.0001220703125 m, one binary64 ULP at the 1e12 m fixture scale; within gamma32 scale-derived bounds |
| Maximum velocity difference | 0 m/s against the independent exact-rational oracle |
| Principal-axis rotation | Maximum quaternion component-vector distance 1.1103267813128674e-16 |
| Mixed rotation | Quaternion distance 2.7841134087907843e-15; angular-rate vector difference 2.7722596276462393e-15 rad/s |
| Independent reference | Explicit midpoint 1,024/2,048/4,096 steps; final refinement difference 1.3557157818527375e-14 |
| Endpoint / refusal | Floor boundaries, negative floors, late canonical epochs, smallest/largest fractions, exact duration bounds, step capacity, finite/default/normalization refusals |
| Determinism | 128 stored queries reversed and 512 seeded repeat selections; bit-identical results |
| Authority | Translation/rotation segments, clock/debt, timeline/revision and transaction histories unchanged; retained value stable after later canonical mutation |
| Allocation | 1,024 warmups, 8,192 coherent calls; 0 bytes with the unchanged checked ordinary helper |

Translation's test-only BigInteger oracle decodes input doubles exactly. Its
gamma32 bound scales with the sum of the represented position, drift and
acceleration terms. Rotation analytical bars use the fixture's angular-rate and
acceleration fifth-derivative envelope plus operation-count rounding. Mixed and
boundary bars use independent reference refinement and a reference-step rounding
allowance. These are bounded fixture qualifications, not a universal global error
theorem. Exact epoch identity is not exact physical state or a contact certificate.

## KSA / BRUTAL boundary check

Read-only installed source reconstruction: `E:\Kitten Space Agency\KSA.dll`,
version `2026.9.7.5402`, revision `487c3f340de24c6a81037120b6d1129c045c5400`, SHA256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
Verified against the retained decompilation at
`E:\NovaCore\build\ksa-residency-reference\assembly-source` and its assembly identity.
This is installed assembly/decompiled implementation evidence, not an authored-source checkout.

- `KSA/VehicleUpdateState.cs`, `GetNewStates` (line 230): vehicle source and staged results.
- `KSA/PhysicsBubble.cs`, `FullPhysicsStepUntil` (1184), constrained step (1953),
  unconstrained step (2054), and Euler–Poinsot rotation (2437): separate bounded
  numerical/analytical mechanisms and temporary physics state.
- `ApplyResultsToVehicles` (664), `ApplySynchronizedResultsToVehicles` (694):
  explicit application to persistent vehicle authority, distinct from evaluation.
- KSA owns vehicle state and application; BRUTAL math/concurrency and solver
  mechanisms are subordinate. This narrow check does not reconstruct the whole engine.
- Authenticated official [Revision 4866, July 8](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037):
  collision prepass addresses speculative contacts/high-warp hitches; separate
  on-rails fast paths avoid unnecessary solver work for eligible objects.

**ADOPT:** persistent authority separate from transient evaluation and explicit
application. **ADAPT:** specialized bounded paths without rerouting accepted
canonical ownership. **INTENTIONALLY DIFFER:** NovaCore's exact rational identity
and read-only query do not need KSA's mutable physics-bubble execution or floating
solver time. No source/history conflict was found. Neither source nor history
proves this precise sidecar algorithm or attributes NovaCore's allocation witness.
Later revision 5421 was visible in history but is not claimed to exist in installed 5402.

## Validation and stop

SDK 10.0.303, net10.0 framework-dependent processes, default installed patch
roll-forward to Microsoft.NETCore.App 10.0.12. No profiler, observer, extra warmup,
tiering/PGO change or runtime override. Only DOTNET_ROOT location variables were
present. Normal processes were launched directly with the built test DLL and no
arguments; parent-only stdout capture does not modify the workload.

| Gate | Result |
| --- | --- |
| Early Release 1 / 2 / 3 | Each 38/38 PASS; every zero-required gate passed |
| Debug full solution | PASS, 0 warnings/errors after required native dependencies were built |
| Release full solution | PASS, 0 warnings/errors |
| Exact motion, canonical translation/coherent motion, rigid rotation | PASS Debug and Release |
| M14.2 / M14.3 / M14.4 / M14.5 / M14.6 focused | PASS Debug and Release |
| ReferenceFrames | 11/11 PASS per configuration |
| Precision | PASS per configuration |
| Full Debug Simulation | 38/38 PASS |

The first full Debug build encountered missing native DLL/SPIR-V deployment
outputs in the new clean worktree (MSB3030). Both native dependencies were then
built from unchanged source in this worktree, and the solution builds passed.
No native test or renderer/GPU validation campaign ran.

| Final Release run | Result | First failure | Gate bytes | Entry / Exit | Other contracts |
| --- | --- | --- | ---: | --- | --- |
| 1 | FAIL | Celestial body orientation: high-precision lunar orientation performance and allocation | 12,336 | N/A: unchanged timed raw-counter gate | 20 groups passed; 1 failed; 17 unreached |
| 2 | NOT RUN | Stop rule | — | — | — |
| 3 | NOT RUN | Stop rule | — | — | — |
| 4 | NOT RUN | Stop rule | — | — | — |
| 5 | NOT RUN | Stop rule | — | — | — |

The failing composite requires `lunarChecksum != ulong.MaxValue`,
`lunarAllocated == 0`, and `lunarNanoseconds < 10000`. Existing output positively
reports the allocation operand failing (12,336); timing is 1,952.7 ns/evaluation
and passes its threshold. Checksum is not reported; do not claim its independent
value. The 100,000 per-evaluation success checks completed before the assertion.
See [unchanged witness excerpt](final-release-witness.txt) and
[structured observations](validation.json). No attempt was made to classify objects,
runtime accounting or indirect candidate causality. The new exact-event group is
last and was not reached in this failed process.

Canonical-wrapper, rational and batch performance: **NOT RUN**, because final
acceptance did not pass. No replacement performance numbers are inferred from
correctness tests. The requested durable allocation-witness rule was **not added**:
its technical-PASS condition was not met.

## Verification

- **A — additive architecture:** PASS by 327 unchanged banked production hashes
  and new-helper reachability only from the new query. Whole-assembly/codegen or
  process-history effects are not exonerated by file identity. Release acceptance failed.
- **B — physics:** focused oracle/refusal/reference review passed. One read-only
  numerical verifier found a new terminal normalization-status mismatch before
  execution; it was corrected to banked `QuaternionNormalizationFailure` and
  covered by an extreme finite-quaternion fixture. No other concrete defect found.
- **C — authority:** value fields, fresh-view/single-writer precondition, complete/default
  output, deterministic replay and no-mutation fixtures passed. No execution rights.
- **D — old/new:** old canonical generalizations were not imported. Small RK4/analytical
  duplication is intentional. Old source and evidence remained unchanged. This does
  not prove any old generalization caused 12,336 or any new helper caused this witness.

## Evidence and reproduction

- [baseline.json](baseline.json): before-edit file identities, six tags and old state.
- [identity.json](identity.json): final preservation, candidate and tested assembly hashes.
- [validation.json](validation.json): ordered runs, independent printed observations,
  focused exits and hashes/lengths of disposable logs.
- [final-release-witness.txt](final-release-witness.txt): decisive unmodified output lines.
- [reproduce.md](reproduce.md): commands, ordering and stop boundaries; no automatic rerun.
- [storage.json](storage.json): exact reviewed scratch and retained evidence accounting.

No output has been deleted. Old comparison scratch remains under Project Control;
new small build/test logs are disposable after this result is accepted. New native
and managed build outputs are normal rebuildable development dependencies, not
permanent engineering evidence. The reviewed consolidated manual command is in
the reproduction instructions. No original candidate source belongs in cleanup.

**Proposed accomplishment title:** Evaluate coherent spacecraft motion at exact
physical-event epochs. No milestone number. Stop for Project Control.
