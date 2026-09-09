# Bounded SAS allocation classification

Date: 2026-09-09. Authority: Project Control's bounded SAS banking-gate classification ticket. No banking authorization exercised.

**Primary classification: INTERMITTENT UNATTRIBUTED SAS FAILURE.**

**Lead judgment: ESCALATE TO PROJECT CONTROL.**

**CONTACT RESPONSE TRANSACTION CANDIDATE — FOLLOW-UP REQUIRED.**

## Baseline and preservation

Branch `codex/contact-response-transaction`. HEAD, main and origin/main remain `35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7`. M14.3 tag target remains `79978cf8b0fd783d43a661df6df53a2c0202af4a`. All 16 accepted payload-ownership fingerprints match before/after. Nothing staged. No canonical production or permanent test file changed in this investigation; the only retained addition is this evidence package. Existing candidate documentation/source/tests remain unbanked.

## Original failure and measured workload

The previously retained full Release run stopped at `Program.cs:282`, `warm SAS proof evaluation allocation`, after 29/35 groups. Its assertion did not report a delta: **the original failing byte count, objects and stack remain UNKNOWN**. No present experiment recovers those missing historical values. In particular, do not replace the unknown delta with 128 or with M14.2's 8,160 bytes.

The original SAS proof function and all its prior checks are retained. One explicit final warmup precedes 100,000 evaluations of `SpacecraftSasController.TryEvaluate`: current identity quaternion, zero body angular velocity, equivalent Y-axis pi/2 target, inertia (1,1,1), proportional gain (10,10,10), zero derivative gain, maximum torque (100,100,100). The original hash accumulation remains inside the window. No extra warmup, retries, no-GC region, runtime setting change, allocation tolerance or subtraction was introduced. Completed runs preserve hash `B88A70CC5EF45185` and all prior proof assertions. The reporter's `completed` field means the loop returned normally; it is not a new assertion of every individual result status.

Managed thread ID, OS thread ID, process ID, GC counters, runtime identity, binary fingerprints and allocation-context samples are recorded in `runs.json`. Example corrected full Release on .NET 10.0.12: PID 34572, OS thread 9796, managed thread 2, GC counts (8,7,6) unchanged, zero bytes and zero object callbacks. Context pointer, limit, allocated-byte fields and large-object field were unchanged through that window.

## Observer validation and a rejected diagnostic result

Rebuilt the existing M14.2 exact-allocation profiler with object type/size and bounded managed stack snapshots. Profiling is child-process scoped; ordinary tiering/PGO remains enabled. BASIC_GC notifications preserve background GC rather than selecting legacy MONITOR_GC. Allocation-context inspection accepts only the recognized native counter instruction sequence and aborts otherwise. This is a runtime-specific diagnostic, not a production dependency.

The first positive control was invalid: CLR and P/Invoke loaded separate copies of the profiler DLL and used different globals. Before SAS attribution, the resolver was corrected to use the exact CLR-loaded path. The valid control then observed one `System.Byte[]`, length 128, size/counter delta 152 bytes on the measured thread. Setup/wrapper warmup windows are distinct from SAS and must not count as SAS allocations.

The first disposable reporter put JSON serialization in the SAS proof method, textually after the ending counter. Debug isolated runs were 0/0/0/0/0; Release isolated runs were 128/128/128/128/128. Two candidate full Release runs and baseline isolated/full Release controls also reported 128. These are **contaminated diagnostic results**, not reproduction of the original failure.

Exact callbacks identified four 32-byte `System.Text.Json.JsonEncodedText` objects, total 128, on the measured thread. Stack snapshots reached `Program.<<Main>$>g__SasSignFrameContinuityProofTests|0_8` then `Program.<Main>$`. A JIT event for the proof method occurred in the native window. The allocation-context pointer advanced 128 bytes, with no GC collection change. A separate 46-byte `System.String` callback was also observed; callback byte totals are not blindly equated with thread-counter accounting. Its accounting contribution was not separately established. These callbacks, source-local JSON dependency and the following bounded intervention establish reporter contamination; they do not establish the precise CLR code-generation submechanism or original incident causality.

Moved only disposable JSON reporting into a `[MethodImpl(NoInlining)]` helper called after the ending counter and observer window. The same SAS warmup and loop then reported zero bytes and zero SAS callbacks. The permanent SAS test does not contain this JSON reporter. Do not attribute its prior failure, or the separate matched-workload 128-byte result, to this newly introduced reporter.

Attribution runs are not performance measurements. Exact callbacks, native metadata/stack inspection, profiler flags and altered test-method compilation can perturb execution. Native windows bracket the managed counter window with small wrapper margins. The full suite and SAS workload were not reordered. Current default runtime rolled forward to installed .NET 10.0.12; explicit child `--fx-version 10.0.11` controls recovered the prior recorded runtime without changing production configuration.

## Bounded reproduction result

Plan: isolated five/configuration; full Debug at most three and Release at most five. The contaminated isolated matrix was discarded as contract evidence and repeated once with corrected reporting on .NET 10.0.11. There was no further search after these bounds.

| Candidate runs | Runtime / observer | Result |
|---|---|---|
| Corrected isolated Debug 1–5 | 10.0.11 / profiler | 5/5 pass; all zero bytes and zero SAS objects |
| Corrected isolated Release 1–5 | 10.0.11 / profiler | 5/5 pass; all zero bytes and zero SAS objects |
| Full Debug 1 | 10.0.11 / profiler | 35/35; SAS zero bytes/objects |
| Full Debug 2 | 10.0.11 / plain | 35/35; SAS zero bytes |
| Full Debug 3 | 10.0.12 / plain | 35/35; SAS zero bytes |
| Full Release 1–2 | 10.0.12 / profiler, contaminated reporter | 29 pass, SAS fails at 128, five not reached, each |
| Full Release 3 | 10.0.12 / profiler, corrected | 35/35; SAS zero bytes/objects |
| Full Release 4 | 10.0.11 / profiler, corrected | 19 pass; lunar orientation fails; 15 groups not reached, including SAS |
| Full Release 5 | 10.0.11 / plain, corrected | 35/35; SAS zero bytes |

Corrected banked full Release control on 10.0.11: **34/34**, SAS zero bytes/objects, same hash; GC (6,6,6) unchanged. It lacks the candidate's contact-response group. A passing baseline does not establish candidate causality.

Release run 4 reported lunar-orientation allocation **12,336 bytes**, with 1992.6 ns/evaluation, before the SAS observer was active. This is an additional unclassified **profiled** suite failure. No object attribution exists for that earlier window. It is neither silently counted as a SAS pass nor promoted to a production defect. No lunar forensics or correction was attempted.

All corrected profiled SAS windows had unchanged collection counters and zero managed allocation callbacks. Those runs provide no positive proof of allocation-context retirement, GC or JIT accounting as the original cause. Later successful runs cannot establish the original zero-allocation blocker is resolved.

## Candidate causality and verification

Direct call path: test → SAS controller → configuration/inertia validation, quaternion canonicalization, FP64 vector/quaternion arithmetic and Math operations → value-type result → test hash. The proof function, controller, attitude evaluator, inertia and Core precision source are unchanged from banked source (see `provenance.json`). It does not read/mutate a spacecraft store, timeline, event, typed arena, revision or transaction. No candidate-specific object allocation was identified.

**VERIFY A:** strongest objection is indirect influence: the added earlier contact-response group and changed shared timeline layout can alter suite allocation pressure, JIT timing and GC timing. Direct source identity does not rule that out. A matching original attributed failure was not recovered, so candidate causality is neither proven nor absolutely excluded. This blocks stronger classification.

**VERIFY B:** strongest objection is observer perturbation. It actually occurred: inline JSON introduced measured objects despite reporting textually after the window. Separating the reporter fixes that disposable observer only. Positive allocation control still establishes the profiler can observe real managed objects, and plain corrected runs also report zero, but neither proves why the original permanent assertion failed. No permanent test boundary change is justified. Exact zero and original workload/warmup remain unchanged.

## Correction, gates and decision

Production correction: **none**. Permanent test correction: **none**. Only the disposable diagnostic observer was corrected. No no-GC solution, arbitrary warmup, tiering/PGO change or SAS optimization.

Disposable Simulation builds passed in Debug/Release; observer C++ build passed (export-visibility warnings only). Completed full candidate runs exercise all 35 groups, including contact response, translation and generation. Prior independent ReferenceFrames, Precision, M14.1 query/stale-snapshot and M14.3 terrain integration passes remain recorded in `../payload-ownership/independent-validation.json`; they were not rerun or claimed fresh here. Full solution builds were not rerun: no canonical production/test correction occurred, and the original attribution gate remains unresolved. `git diff --check` passed at handoff, with CRLF conversion warnings only.

The separate original matched Release 128-byte observation remains **SHARED / NON-CANDIDATE-SPECIFIC / UNATTRIBUTED**. Equal byte counts do not link mechanisms. No second investigation was opened.

Project Control receives an exhausted bounded classification, not a ready-to-bank result. The original failure delta, objects, stack and accounting mechanism remain unavailable. Decide the next acceptance/measurement responsibility explicitly; do not automatically repeat suites or extend this investigation.

## Evidence lifecycle

Package budget: 96 KiB. Retain this report, compact run/context results, source/binary identity, decisive allocation excerpts, tested observer/helper and reproduction recipe for the unresolved banking decision and the demonstrated observer pitfall. Do not retain copied production trees, SDK headers, binaries or bulk console output permanently.

Exact disposable inventory is in `disposable-inventory.json`. The existing three roots remain 1,433 files / 142,754,226 bytes. New scratch is isolated, ignored and untracked, with no reparse points; it contains no production assets/runtime deployment or permanent fixtures. No deletion was attempted. After reviewing the retained package, Project Control may manually remove only the four roots below. Existing denied cleanup was not retried; attribution incompleteness does not require keeping rebuildable copies once this evidence is accepted.

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\contact-allocation-discriminator', 'E:\NovaCore\.codex\contact-payload-layout-control', 'E:\NovaCore\.codex\contact-payload-ownership', 'E:\NovaCore\.codex\sas-allocation-classification' -Recurse -Force
```

No staging, commit, tag, merge, push, banking or next M14 responsibility. Stop for Project Control.
