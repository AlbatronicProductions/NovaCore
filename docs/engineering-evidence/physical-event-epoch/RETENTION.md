# M14.6 evidence retention closeout

Project Control technically accepted the candidate. This is evidence consolidation only;
no validation rerun, production/test edit, commit, tag, push or banking.

Deletion status: BLOCKED by automatic approval review (no specific reason beyond
"blocked by policy"). The deletion command did not execute. Actual removed:
0 files / 0 bytes. All 20 DISPOSE files remain in the working tree and index.
Updated report references describe the intended consolidated package, not completed
deletion. No alternate deletion mechanism was attempted.

Original package: 54 files / 412,521 logical bytes. Classify 34 KEEP and
20 DISPOSE; remove 98,439 logical bytes. This ledger is one new KEEP
record. Reports are changed only to fix retired-artifact references and expose the
accepted final result. No raw diagnostic artifact is whitespace-normalized or rewritten.

Retention budget: 384 KiB for this bounded package. The current source and permanent
tests carry the accepted implementation; the archive carries conclusions, exact
witnesses, identities and reproduction. Earlier stop states remain historical.

## File-by-file classification

All paths are relative to this package. The listed original results/provenance are
preserved by the named replacement before deletion. RETENTION.md itself is KEEP as
the concise lifecycle/reproduction ledger.

| File | Decision | Continuing value / preserved replacement |
|---|---|---|
| `README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `allocation-phase/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `allocation-phase/matrix.json` | KEEP | Distinct causal experiment: exact per-run operands, process identity and non-allocation outcomes. |
| `allocation-phase/phase-omission.patch` | DISPOSE | The complete one-invocation control and arm identities are preserved in allocation-phase/README.md and preflight/matrix. |
| `allocation-phase/preflight.json` | KEEP | Small immutable runtime, binary, environment and exact suite-order provenance for this distinct comparison; not a restoration dump. |
| `debt-gate-classification.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `debt-gate-reporting.patch` | DISPOSE | Reporting operands, warmup, workload and source hashes are in debt-gate-classification.md; reconstruction recipe below. |
| `gc-region/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `gc-region/gc-region-omission.patch` | DISPOSE | The entry/exit-only guard is described in gc-region/README.md with immutable per-run records. |
| `gc-region/matrix.json` | KEEP | Distinct causal experiment: exact per-run operands, process identity and non-allocation outcomes. |
| `gc-region/preflight.json` | KEEP | Small immutable runtime, binary, environment and exact suite-order provenance for this distinct comparison; not a restoration dump. |
| `matched-full-suite-ab/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `matched-full-suite-ab/matrix.json` | KEEP | Distinct causal experiment: exact per-run operands, process identity and non-allocation outcomes. |
| `matched-full-suite-ab/preflight.json` | KEEP | Small immutable runtime, binary, environment and exact suite-order provenance for this distinct comparison; not a restoration dump. |
| `measurement-boundary/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `measurement-boundary/executions.json` | DISPOSE | Superseded four-process execution ledger; executed gate results and stop decision remain in its report; final accepted commands/results are in ordinary-class-migration/validation.json. |
| `measurement-boundary/final-status.json` | DISPOSE | Intermediate Git/storage capture superseded by final identity and this closeout; historical no-cleanup decision remains in its report. |
| `measurement-boundary/first-failure.txt` | DISPOSE | Exception message, method, line, unknown operands and stopped groups are already preserved in measurement-boundary/README.md. |
| `measurement-boundary/identity.json` | KEEP | Exact tested source/tag fingerprints for the named stage; identifies the accepted or audited content. |
| `measurement-boundary/plan.json` | DISPOSE | The exact bounded plan is preserved in its report; the final accepted plan is retained separately. |
| `measurement-boundary/validation-matrix.json` | DISPOSE | Duplicates the full executed/not-run matrix in measurement-boundary/README.md. |
| `ordinary-class-migration/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `ordinary-class-migration/cleanup.json` | DISPOSE | Duplicates the eight reviewed paths, totals and manual command in its report; no scratch cleanup is performed here. |
| `ordinary-class-migration/final-verification.json` | DISPOSE | Intermediate Git/status capture; accepted source identity and final technical results remain in identity.json and validation.json. |
| `ordinary-class-migration/identity.json` | KEEP | Exact tested source/tag fingerprints for the named stage; identifies the accepted or audited content. |
| `ordinary-class-migration/plan.json` | KEEP | Final predeclared bounded validation plan and environment digest. |
| `ordinary-class-migration/reconciliation.json` | KEEP | Authoritative 31-window migration checklist and preservation result. |
| `ordinary-class-migration/test-boundary.patch` | DISPOSE | Superseded implementation diff; current permanent tests/helper, reconciliation, accepted identity and validation preserve the final contract. |
| `ordinary-class-migration/validation.json` | KEEP | Final executed commands, PIDs and exact per-gate results including ten full suites and real-allocation controls. |
| `runtime-accounting/AccountingProbe.cs` | KEEP | Minimal final observer adapter needed to reproduce the A-H accounting witness; reusable M14.2 observer alone lacks these checkpoints. |
| `runtime-accounting/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `runtime-accounting/canonical-before.json` | KEEP | Original candidate and evidence identities before attribution; preserves pre-migration provenance. |
| `runtime-accounting/checkpoints.inc` | KEEP | Minimal final observer adapter needed to reproduce the A-H accounting witness; reusable M14.2 observer alone lacks these checkpoints. |
| `runtime-accounting/disposable-hooks.patch` | DISPOSE | Temporary insertion machinery replaced by the A-H hook-placement recipe below; minimal final observer adapters remain. |
| `runtime-accounting/disposable-storage.json` | DISPOSE | Intermediate scratch counts, superseded by the final migration report; not an accounting witness. |
| `runtime-accounting/final-verification.json` | DISPOSE | Restoration and no-drift result is already in the accounting report; canonical-before and accepted final identity remain. |
| `runtime-accounting/matrix.json` | KEEP | Distinct causal experiment: exact per-run operands, process identity and non-allocation outcomes. |
| `runtime-accounting/observer-inputs.json` | KEEP | Pinned observer/header/runtime hashes needed for safe fail-closed witness reproduction. |
| `runtime-accounting/positive.stdout.txt` | DISPOSE | Decisive one-object/152-byte result, thread association and 152-byte pointer advance already preserved in runtime-accounting/README.md; raw event/class IDs are not needed for that conclusion. |
| `runtime-accounting/preflight.json` | KEEP | Small immutable runtime, binary, environment and exact suite-order provenance for this distinct comparison; not a restoration dump. |
| `runtime-accounting/restoration.txt` | DISPOSE | Restoration result duplicated in runtime-accounting/README.md. |
| `runtime-accounting/source-quality.txt` | DISPOSE | Normalized-source comparison and the 121 line-ending-only differences already preserved in runtime-accounting/README.md. |
| `runtime-accounting/timelines.json` | KEEP | Decisive exact context, pointer, object-count and GC-event witnesses; cannot be replaced by byte-magnitude inference. |
| `runtime-accounting/timelines.md` | DISPOSE | Rendered duplicate of exact A-H checkpoints in timelines.json; decisive arithmetic and interpretation remain in README.md. |
| `structural-omission/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `structural-omission/matrix.json` | KEEP | Distinct causal experiment: exact per-run operands, process identity and non-allocation outcomes. |
| `structural-omission/omission-runner.patch` | DISPOSE | Group-only omission, order and exact outcomes are preserved in structural-omission report/manifests. |
| `structural-omission/preflight.json` | KEEP | Small immutable runtime, binary, environment and exact suite-order provenance for this distinct comparison; not a restoration dump. |
| `zero-allocation-inventory/README.md` | KEEP | Final report for this causal/architecture stage; preserves results, limits and reproduction. |
| `zero-allocation-inventory/api-accounting.json` | KEEP | Compact API/helper call-count cross-check supporting completeness of the 42-window audit. |
| `zero-allocation-inventory/final-verification.json` | DISPOSE | Intermediate Git capture; audit identity remains in test-source-identity.json and accepted final identity in ordinary-class-migration/identity.json. |
| `zero-allocation-inventory/inventory.json` | KEEP | Complete 42-window machine-readable contract/measurement inventory. |
| `zero-allocation-inventory/inventory.md` | KEEP | Human-readable 42-row inventory used for Project Control review, complementary to full JSON fields. |
| `zero-allocation-inventory/test-source-identity.json` | KEEP | Exact tested source/tag fingerprints for the named stage; identifies the accepted or audited content. |

## Reproduction without disposable patches

No reproduction is executed in this closeout. Final contract validation uses the
current candidate tests and the exact commands/plan in ordinary-class-migration.
The historical forensic comparison must use a disposable reconstructed fixture,
not the now-isolated permanent servicing window.

1. Start the historical runner from banked M14.5
   `b53d48025cfb8142cb32a6fc6fceb6018e33cfcd` using git archive in a disposable tree.
   Overlay the accepted PhysicalEventEpoch.cs, PhysicalEventOrderKey.cs,
   SimulationEventHeaderComparer.cs and unchanged PhysicalEventEpochTests.cs.
   Register `("Internal physical-event epochs", PhysicalEventEpochTests.Run)`
   immediately after Event ordering. Preserve all common group ordering, as recorded
   in matched-full-suite-ab/preflight.json. The original candidate had optional
   `--physical-event-epoch-only` and `--physical-event-epoch-performance` early-return
   selectors calling Run and Performance respectively. Preserve them for that fixture.
   This recovers the banked raw servicing/ordinary measurement bodies rather than
   undoing current permanent measurements heuristically. Original source and binary
   hashes remain provenance; regenerated instrumentation is not promised identical
   binary/codegen identity.
2. The servicing workload is 5,000 marker events at id*10 ticks, max 16 events per
   advance, preallocated 5,000-entry history. Preserve one 100-tick warmup service
   followed by 499 measured advance(100)/service calls, each requiring Completed.
   Open/close the original per-thread counter around those 499 calls. Capture scalar
   clock/debt/processed/pending/thread state before opening; print delta and all
   outcomes after closing. Keep exact expected 50,000/0/5,000/0/499 and zero bytes.
   Optional `--debt-service-only` invokes the complete existing debt group. Reports
   contain the original reporter hash, normal/omitted identities and matrices.
3. Structural controls are separate: omit only the epoch group invocation; then
   omit only Allocation() within that group; then retain its entire workload and
   guard only its existing GC-region entry/exit pair. Use the same reporter in both
   arms, original warmup and unchanged other phases. Each stage's report/manifests
   supply order, runtime, arguments and six-run bounds. No additional GC boundary.
4. For the final accounting witness reuse the retained M14.2
   [allocation-attribution-profiler.cpp](../spacecraft-translation/allocation-attribution-profiler.cpp)
   and its [build/decoder contract](../spacecraft-translation/allocation-attribution.md).
   Keep this package's AccountingProbe.cs and checkpoints.inc: these two small
   adapters add the bounded A-H storage absent from the base observer. Insert the
   include before the Profiler class and add AccountingProbe.cs only to the
   disposable test project. The accounting report supplies compiler/link settings;
   observer-inputs.json pins headers, observer and CLR hashes.
5. At runner startup set AccountingProbe.OmitRegion from
   `--omit-epoch-gc-region`, then Prepare(); the optional
   `--observer-positive-control` calls PositiveControl and returns. Prepare must
   fail closed on an unsupported decoded counter. Preserve identical instrumentation
   and compiled inputs in both normal/omitted arms. Only guard the existing epoch
   entry/exit pair with OmitRegion. No global runtime/tiering/PGO changes.
6. Point IDs 1..8 correspond exactly to A..H: before epoch entry, after entry,
   before epoch workload, after workload, before exit, after exit, before servicing,
   after servicing. Pass the current thread counter and caller region state. For
   C/D and G/H use the same saved before/after counter values as the measured delta.
   BeginWindow precedes the opening counter; StopCapture follows D (window 1) or H
   (window 2). E/F remain in finally around the existing conditional exit. DumpCapture
   only after H, before the original servicing assertion. Point uses approximate
   total, collection counts and thread ID; unavailable quantities remain unavailable.
7. Use the same child-only registration values from runtime-accounting/preflight.json,
   substituting only the rebuilt observer path equally in both arms. Warm the
   independent byte[128] control, require one object/152 bytes on the owner thread,
   then exactly Normal/Omitted/Normal/Omitted fresh full-Debug processes on the pinned
   runtime. Stop on decoder/runtime/non-allocation mismatch; no retries or new tracing.
   Follow the accounting report's observer-perturbation limitations. The retained
   timelines.json supplies the original A-H values and suspension witnesses.

The raw positive stdout is unnecessary: the existing accounting report preserves
one object, 152 object/counter bytes, correct owner thread and 152-byte pointer
advance with fixed limit/cumulative fields. No new observation is inferred here.
The final ordinary-class shared positive controls and their exact results remain
independently retained in validation.json and permanent test code.

## Preservation and scope

Keep all measurements/hashes in retained JSON unchanged. The six disposable patches
are superseded by current tests plus the recipes above; no extension-wide deletion
rule was applied. Distinct structural matrices are not duplicate experiments.
Preflight files remain only where they preserve exact binary/runtime/order provenance;
restoration/status dumps are retired when their result is already in a final report.

The two retained source adapters have continuing value solely for reproducing the
final decisive witness. They are not production dependencies, suite tests, compiled
outputs or a retained collection of intermediate experimental variants.

All deletions are bounded individual files inside this directory. No external
scratch cleanup or runtime smoke is authorized or executed here. Existing reports'
old scratch counts are not current storage claims. Final Git/count/fingerprint
verification is returned to Project Control; stop without banking.

## Manual completion after blocked automated deletion

Run only from the reviewed repository; this command removes the exact 20 classified
files from the working tree and index without changing commits or tags. It has not
been executed by the assistant.

```powershell
git -C E:\NovaCore rm -- `
  'docs/engineering-evidence/physical-event-epoch/debt-gate-reporting.patch' `
  'docs/engineering-evidence/physical-event-epoch/allocation-phase/phase-omission.patch' `
  'docs/engineering-evidence/physical-event-epoch/gc-region/gc-region-omission.patch' `
  'docs/engineering-evidence/physical-event-epoch/structural-omission/omission-runner.patch' `
  'docs/engineering-evidence/physical-event-epoch/ordinary-class-migration/test-boundary.patch' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/disposable-hooks.patch' `
  'docs/engineering-evidence/physical-event-epoch/measurement-boundary/executions.json' `
  'docs/engineering-evidence/physical-event-epoch/measurement-boundary/final-status.json' `
  'docs/engineering-evidence/physical-event-epoch/measurement-boundary/first-failure.txt' `
  'docs/engineering-evidence/physical-event-epoch/measurement-boundary/plan.json' `
  'docs/engineering-evidence/physical-event-epoch/measurement-boundary/validation-matrix.json' `
  'docs/engineering-evidence/physical-event-epoch/ordinary-class-migration/cleanup.json' `
  'docs/engineering-evidence/physical-event-epoch/ordinary-class-migration/final-verification.json' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/disposable-storage.json' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/final-verification.json' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/positive.stdout.txt' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/restoration.txt' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/source-quality.txt' `
  'docs/engineering-evidence/physical-event-epoch/runtime-accounting/timelines.md' `
  'docs/engineering-evidence/physical-event-epoch/zero-allocation-inventory/final-verification.json'
```

Then run `git diff --cached --check`, `git status --short`, and the staged/unstaged
name lists. Do not commit, tag, push or bank as part of this cleanup instruction.
