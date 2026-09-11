# Normal Release allocation acceptance

**PAIRED PRIVATE PROPAGATION CANDIDATE — FOLLOW-UP REQUIRED**

Lead judgment: **ESCALATE TO PROJECT CONTROL**. The hard 5/5 bar failed on run 2.
No production, permanent test, warmup, threshold, group order or measurement
boundary changed. No observer or profiler was started.

## Verified acceptance environment

Branch `codex/root-linked-private-propagation`; HEAD/main/origin/main/M14.14 remain
`f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe`. All 315 source/project fingerprints
and the six retained Debug/Release assembly hashes matched before and after this
ticket. All fourteen tags remain unchanged; nothing staged. Only the canonical
worktree exists; disposable differential trees are absent.

The permanent Release runner retains its normal 46-group array and original
loop. Neither disposable attitude selector is present. Existing validated binaries
matched [identity.json](identity.json), so no build was required or performed.

Predeclared: five fresh normal processes, no retries or rebuilds, stop on first
failure. Exact command, from `E:/NovaCore`:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
```

## Executed matrix

| Run | Groups | Attitude bytes | Attitude entry | Attitude exit | Process exit / result |
|---|---|---:|---|---|---|
| 1 | 46 passed | 0 | PASS | PASS | 0 / PASS |
| 2 | 3 passed, 1 failed, 42 unreached | Not reached | Not reached | Not reached | 1 / FAIL |
| 3 | Not run after hard stop | — | — | — | NOT RUN |
| 4 | Not run after hard stop | — | — | — | NOT RUN |
| 5 | Not run after hard stop | — | — | — | NOT RUN |

Run 2 failed **Solar UTC and speed presets**, ordinal 4, at the `speed-presets`
allocation gate. Its own no-GC **entry and exit both passed**. Exact witness:

```text
PASS SimulationInstant
PASS SimulationDuration
PASS SimulationRate
ORDINARY_ALLOCATION gate=speed-presets bytes=6192 entry=PASS exit=PASS
Unhandled exception. System.InvalidOperationException: speed-presets: expected zero managed allocation, actual=6192
```

The stack reaches `OrdinaryAllocationMeasurement.RequireZero`, helper line 44;
`SolarUtcAndSpeedPresetTests`, Program.cs line 133; and the normal runner, line 104.
[normal-release-acceptance.json](normal-release-acceptance.json) retains the exact
exception stack, matrix, command, identity checks and unrun gates.

## Interpretation and stop

Run 1 is a complete normal Release pass; it does not satisfy the five-run bar.
Run 2 is a fresh failure in the authoritative permanent acceptance environment.
Its allocation gate precedes attitude integration and the new propagation group.
Candidate causality and object/runtime mechanism remain **NOT ESTABLISHED**.
No inference about allocation-context retirement, tiering or a CLR bug follows.

The original 12,336-byte attitude witness and modified-runner 6,192-byte witnesses
remain in the [original report](README.md) and
[differential](attitude-differential.md). The fresh 6,192-byte magnitude does not
prove the same mechanism, particularly at a different gate. Prior observations
were not erased or declared fixed/false positives.

The prerequisite for accepting residual uncertainty was not met. Observer payoff
is re-established by this normal-run failure, but no observer is automatically
authorized to proceed: Project Control chooses the next bounded trigger/object
question. No deeper inspection, additional acceptance run or correction followed.

ReferenceFrames, Precision, separate focused M14.6–M14.14 regressions, additional
Florida/query/stale-source integration and the conditional final review remain
unrun. Existing unchanged-fingerprint Debug/build/analytical/Florida/replay/cost
evidence remains valid; none substitutes for this failed acceptance bar.

The numerical contract stays frozen: exact provider alpha, correlated translation,
asymmetric spin, final represented certificates and `EVENT COVERAGE UNKNOWN`.
No state/clock/debt/history mutation, publication or general-solver responsibility
was introduced. Bepu convergence gate remains not reached. Previous measured
costs are retained without new performance runs.

## Retention and Git

Two concise files retain this matrix and decisive failure; no temporary stdout,
worktree, observer or raw trace was created. No cleanup/deletion was needed.
Combined evidence budget is now **80 KiB**, accommodating this separately
authorized normal acceptance attempt while preserving earlier witnesses.
Current status links were updated; production/tests remain byte-identical.
`git diff --check` passes with line-ending warnings only. Nothing was staged,
committed, tagged, merged, pushed or banked. Candidate remains unbanked.

Proposed accomplishment: **Certify paired private spacecraft propagation to a
canonical target**. No milestone number. Stop for Project Control.
