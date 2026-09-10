# Physical-event epoch: permanent servicing measurement boundary

**PHYSICAL EVENT EPOCH CANDIDATE — FOLLOW-UP REQUIRED**

Lead judgment: **ESCALATE TO PROJECT CONTROL**. The authorized test-only migration
is implemented, but the predeclared validation stopped on the first actual failure
in a different, unchanged canonical-group assertion. No retry, attribution,
additional correction, production edit, cleanup or banking followed.

## Baseline and identity

Branch `codex/physical-event-epoch`; HEAD/main/origin/main/M14.5:
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`.
All starting production C# fingerprints match. The epoch test itself is unchanged.
Full tag object/target identities and checked file counts are in `identity.json`.

| Tag | Unchanged target |
|---|---|
| m14.1-canonical-surface-point-queries | 5b9b02ab1d2ba0e903809510908e4819984d4de2 |
| m14.2-spacecraft-translational-authority | caa6d93ccd7c4e5924381e077a62585f99e5a32a |
| m14.3-spacecraft-terrain-contact-observations | 79978cf8b0fd783d43a661df6df53a2c0202af4a |
| m14.4-atomic-contact-response | 28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c |
| m14.5-isolated-analytical-contact-response | b53d48025cfb8142cb32a6fc6fceb6018e33cfcd |

Accepted candidate production SHA-256:

- PhysicalEventEpoch.cs: `0F4923E14C9370F18A0F77C73B6BCA88CB51E6D56EDC7D632D596ABC326DF3C2`
- PhysicalEventOrderKey.cs: `C65C540656C553610DC8ABC36D26967EA874A682E0E1D67E4EFA8374239C6FCF`
- SimulationEventHeaderComparer.cs: `0C5F4A0957EBCF2528243909BC0372CBA8C3084337051F295D5AE24863135D65`

## Resolved accounting cause

**CANDIDATE-TRIGGERED CLR ALLOCATION-CONTEXT ACCOUNTING EFFECT**.
The prior [runtime-accounting proof](../runtime-accounting/README.md) remains the
causal evidence; no further attribution was performed here.

With cumulative fields unchanged at 3,173,488 + 23,832,640, unused context changed
from 8,160 to zero. The thread counter therefore changed from 26,997,968 to
27,006,128: exactly 8,160, with zero observed object bytes. The retained deliberate
byte[128] observer control separately detected one 152-byte object. This is not
classified as a CLR bug, a production allocation, or an epoch/comparer allocation.

## Permanent test migration

Only `tests/NovaCore.Simulation.Tests/Program.cs` was changed in this ticket,
alongside this concise evidence package.

Old boundary: warmed servicing counter was open across ordinary GC activity and
one composite assertion combined allocation, clock, debt and event counts.

New boundary: original setup and warmup → checked TryStartNoGCRegion → counter
open → original 499 calls advancing 100 ticks each and requiring Completed →
counter close → EndNoGCRegion in finally → reporting and independent assertions.
Successful calls are explicitly counted; expected count remains 499. Entry false
fails Check; exit exceptions propagate as test failures. Neither path falls back,
retries, skips or reports a successful boundary.

The 1 MiB reservation reuses the qualified orchestration contract's existing
bounded pattern. Servicing storage is already preallocated and its required
workload allocation is zero, so the reservation does not scale with event/call
count. It is also sufficient for the separate known 152-byte positive object.
It is a measurement reservation, never a permitted allocation budget: exact zero
is independently asserted without tolerance, subtraction or a known-byte exception.

The separate positive-control body creates one non-inlined byte[128], retains it
through measurement, and requires a nonzero counter delta. It uses its own checked
region with the same reservation and counter architecture. Only the control itself
is warmed. It runs after servicing in the full suite, adding no servicing warmup,
and is selectable via `--servicing-allocation-control` for fresh-process validation.
It shares no workload/result implementation that could manufacture a passing delta.

The no-GC boundary prevents unrelated allocation-context retirement from being
charged to the workload. It does not prevent real object allocation from advancing
the counter, as the positive control demonstrates. Epoch Allocation() and its
existing region remain byte-identical.

## Predeclared plan and actual validation

One full solution build per configuration; then Debug 5 / Release 5 fresh full
Simulation processes; Debug 3 / Release 3 fresh positive controls; focused epoch,
translation/contact/response, ReferenceFrames and Precision gates in both
configurations. Stop on first test failure, no retry. This section preserves the
executed sequence; final accepted commands and environment identity are retained
in [ordinary-class-migration](../ordinary-class-migration/README.md). No profiler,
EventPipe or runtime policy overrides were present.

Both full solution builds passed with zero warnings/errors. Debug run 1 passed
37/37 registered groups. Its collector initially counted nested PASS messages as
additional groups; the parser was corrected to match exact registered names.
The already successful run was retained, not repeated. No rebuild occurred.

Debug run 2 then failed at the unchanged assertion in CanonicalGroupTests:

`preallocated canonical group execution allocates zero bytes`

The failing assertion at Program.cs:1365 combines counter equality, IsComplete,
and ProcessedCount == 5,000. Its output does not expose which operand failed.
**Allocation delta, completion result and processed count for this new failure
remain UNKNOWN.** No CLR/accounting/production causality is inferred. The exact
exception was System.Exception with the assertion text above, raised through
Check at line 1461 and CanonicalGroupTests at line 1365 (runner line 78).
The raw exception capture was retired; this responsibility was not investigated.

| Configuration | Run | Result | Allocation | Entry | Exit | Clock | Debt | Processed | Pending | Completed |
|---|---:|---|---:|---|---|---:|---:|---:|---:|---:|
| Debug | 1 | 37/37 PASS | 0 | PASS | PASS | 50000 | 0 | 5000 | 0 | 499 |
| Debug | 2 | 11 pass; canonical group fails; 25 not reached | 0 | PASS | PASS | 50000 | 0 | 5000 | 0 | 499 |
| Debug | 3 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Debug | 4 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Debug | 5 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Release | 1 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Release | 2 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Release | 3 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Release | 4 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |
| Release | 5 | NOT RUN: stop rule | — | — | — | — | — | — | — | — |

The table's operands are the corrected servicing gate, not the later canonical
group assertion. The new gate passed in both executed processes; that does not
satisfy the required ten-run qualification.

## Positive and focused gates

Predeclared positive semantics: one measured System.Byte[] of length 128, expected
152 counter bytes on Windows x64; permanent assertion requires nonzero rather
than encoding an object-header size as a portable runtime contract.

Both executed full Debug suites reported entry PASS, exit PASS and 152 bytes for
this control. The separately requested three fresh Debug and three fresh Release
controls were **not run** after the stop. These two embedded observations do not
substitute for that matrix.

All separately requested focused invocations were **not run** after the stop.
Debug run 1 did execute the full suite's epoch and M14.2–M14.5 groups successfully;
both executed suites passed the epoch group. ReferenceFrames and Precision were
built, but not executed in this ticket. No renderer/GPU campaign ran.

## VERIFY A — measurement red team

Strongest objection: GC isolation might conceal real allocation or alter later
suite process history. The independent positive workload reports 152 rather than
zero, the production servicing calls/warmup remain intact, region failures cannot
be ignored, and all seven servicing outcomes are exact. The local measurement
design survives this review. **Full acceptance remains incomplete** because the
required repeat matrix stopped. The later canonical-group failure could involve
process history, actual workload behavior or another operand; no attribution is
made and no broader migration is authorized here.

## VERIFY B — candidate red team

Strongest objection: test consolidation might alter epoch or ordering authority.
All production hashes and the epoch test hash remain identical, and the executed
full Debug epoch/translation/contact/response gates passed. Public integral time,
ordering, epoch arithmetic/comparator and serialization implementations are
unchanged. Source-identity review survived; remaining cross-configuration focused
validation is explicitly outstanding.

## Evidence, cleanup and next decision

Retain the prior causal chain reports, this migration contract, bounded matrix,
identity checks, first exception and reproduction commands. Raw build/test logs
and comparison trees remain disposable scratch. **No cleanup was performed**:
technical validation did not pass, so the prerequisite for consolidated manual
cleanup/classification has not been reached. Do not retry blocked deletion.

To reproduce the intended gates after further Project Control authorization, use
the commands in [the final accepted migration report](../ordinary-class-migration/README.md)
and the historical plan above when reconstructing this stopped stage. The test CLI
selectors are permanent; no observer or discarded comparison tree is required
to run the corrected gate. Do not automatically restart this failed sequence.

No staging, commit, tag, merge, push, bank or milestone assignment occurred.
Only the test file and this evidence package changed during this ticket.

Proposed accomplishment title: **Establish internal exact physical-event epochs**.

**STOP FOR PROJECT CONTROL.**
