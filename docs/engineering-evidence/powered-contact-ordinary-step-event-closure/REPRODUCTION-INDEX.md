# Surviving reproduction index

All commands run from PowerShell 7 at `E:\NovaCore`, using the recorded SDK 10.0.303 / runtime 10.0.12 and pinned BEPU. Output directories must be fresh. These commands preserve the ability to reproduce; **none of these build/numerical/performance campaigns was run during consolidation**. Obtain the appropriate Project Control scope before new numerical work. Never retry until green.

## Active ordinary closure

W — ten physical cases, exact inputs, key near-end/mid/tiny/zero-event controls and repeats:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\reproduce.ps1'
```

Inputs/reference generation remain in `prepare.py` and the unchanged sibling `powered-contact-chronological-event-feasibility/chronology.py`. Preserve that dependency; the sibling is not disposable just because its Candidate C production design was rejected.

F — independent arithmetic replay of the two saved ordered impulse-work traces, with no solver or profiler:

```powershell
python -B 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work\check-work.py' 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\off-com-8.json' 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work\off-com-trace.json'
python -B 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work\check-work.py' 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\yaw-baseline-8.json' 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work\yaw-baseline-trace.json'
```

To recapture rather than audit saved traces, follow `friction-work/reproduce.md` and its pinned source/observer preparation; this is separate from cleanup.

Z → N → P — allocation/positive-control/consumed bits, native equivalence, then separately authorized timing:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\recheck.ps1'
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\native.ps1'
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-recheck\timing.ps1'
```

Keep `performance-payoff/Program.cs`, `allocation-closure/Correction.cs`, `Attribution.cs`, and `projection-contract/identity.json`: the current reproduction derives the qualified mapper from those sources and validates their targeted hashes. They are active dependencies, not obsolete duplicate code. `Timing.cs` performs the three-process plan via its wrapper. No performance rerun is authorized by this handoff.

## Decisive Candidate-A failures

The new wrapper checks `candidate-a-reproduction-inputs.json`, existing pinned tracked infrastructure and the recorded baseline; it does not hash retired planning documents. Numerical source, csproj compile definitions, workload and expected failure remain unchanged. Outputs are compared completely, not accepted merely because an executable exits 1.

```powershell
# A1: original finite-eight-sweep centered32N failure (complete raw result SHA).
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\reproduce-candidate-a.ps1' OriginalWorld 'E:\NovaCore\build\candidate-a-original-reproduction'
# A2: selected D+12, absolute checks pass, all13 paired checks fail.
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\reproduce-candidate-a.ps1' SelectedPaired 'E:\NovaCore\build\candidate-a-paired-reproduction'
# A3: bounded stage/sweep/downstream attribution; no production correction.
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\reproduce-candidate-a.ps1' SelectedTrace 'E:\NovaCore\build\candidate-a-trace-reproduction'
```

The wrapper was statically checked during consolidation; executable requalification was deliberately not repeated. Historical source/results retain prior run evidence, not a newly observed pass.

A3 copies the three hash-checked frozen paired outputs into its fresh `reproduction/` input directory before running the unchanged trace. Those copies are comparison inputs, not a new A2 run. A3 also consumes the retained baseline-tiny null-control witness; its hash is included in the targeted manifest.

## Historical-only campaigns and archive semantics

Scalar/two-row primitives and early convergence are historical-only; their small leaf executables are retired. Their hypotheses, bars, results and original hashes survive in the Candidate-A summary, `validation.json`, copied early convergence report/results, and consolidation manifest. Other exploratory duration/basis/D/active-set matrices remain frozen source/result anchors for the fallback. Their superseded broad `reproduce*.ps1` campaign wrappers are retired, not advertised as current commands. No claim that every abandoned matrix remains one-command runnable is made.

Original `identity.json` and preflight/freeze records are **historical provenance**, not current membership manifests. They deliberately retain hashes of files later consolidated; do not use them to assert present-tree equality. The current manifest and targeted reproduction input list supersede that membership role. Historical reports' retired-script links now lead to labeled retirement records. Remaining code/formula source was retained where it provides independent reference, causal detail or a genuine future fallback responsibility.

Raw physical result sets remain at their original paths except proven duplicate numerical copies. The manifest identifies the surviving exact copy and original hash for each duplicate; distinct process-attempt records are preserved separately. No process ID is treated as deterministic numerical output.
