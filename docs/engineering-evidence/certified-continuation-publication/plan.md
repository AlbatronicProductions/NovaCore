# Certified continuation publication candidate plan

Baseline: a4211617eef1e1d23bf8a4f8495388a35f9ed1dd, clean main. Candidate branch:
codex/certified-continuation-publication. Unbanked; no milestone assigned.

## Contract

One Simulation-owned publisher consumes checked M14.15/M14.16 evidence, requires
N == SourceStart < alpha < T == SourceEnd and debt >= T-N, and directly installs
the qualified paired endpoint with clock/debt, one StateRevision and one reserved
history record. TimelineRevision is unchanged. No physics is repeated at commit.
The existing single-writer authority phase must fence clock/timeline/state aliases;
retained observations copy values and borrowed state views cannot read newer slots
under an old captured revision.

Current installed KSA/BRUTAL/Bepu hashes match the preceding read-only assessment.
Adopt preparation -> staged results -> game-owned application. Adapt checked
admission. Intentionally preserve certified endpoint bits and failure-atomic
state/time/debt/revision/history; KSA application normalizes and contains fallible
work after writes. Direct KSA application timing is unavailable: cost comparison
will remain UNMEASURED, with source-level operation/cadence comparison.

## Predeclared validation and measurement

Development: focused compile and candidate tests while implementing; fix only
candidate defects. Final: one full solution build in Debug and Release, focused
candidate tests, one full Simulation process per configuration, ReferenceFrames,
Precision, relevant M14.1-M14.16 selectors and real Florida Coast/Force gates.
Stop on unrelated gate failure; no automatic allocation forensics or retries.

Timing is separate from checked no-GC allocation measurement. Use 8 warmed fresh
publication fixtures, then 128 independent successful publications in Release.
Measure preparation/preflight, history preparation/reservation, write-only commit,
total publication, copied observation and representative refusals separately.
For expensive source/private-chain reconstruction use 16 fresh samples following
2 warmups. Report nearest-rank P95/P99 and maximum, Stopwatch resolution, sample
count and instrumentation limits; no removal of outliers or timing retries.
Fixture preparation is outside consuming publication timing. Do not repeatedly
publish a consumed receipt or reset production authority to manufacture samples.

Allocation: exact zero expected for warmed preallocated publication/commit,
observation and refusal paths. Independently report legitimate source/proof/setup
and replay-construction allocation. Use equivalent fresh prepared fixtures for
consuming measurements and the current positive allocation control. No GC
isolation in timing. Measure 8 independent operations for consuming allocation
and 32 calls for reusable reads/refusals, all setup outside the window.

Independent reviews cover event/source admission, atomicity/borrowed views/history,
and KSA/cadence/performance. No GPU campaign or game launch is planned.

## Evidence lifecycle

Retain baseline identities/fingerprints, contract, concise validation/refusal/bit
witnesses, timing/allocation summaries, reviewer conclusions and reproduction
commands. Target permanent evidence <= 150 KiB. Temporary command output only;
no comparison build tree, runtime dump, profiler, or bulk KSA source copy.
