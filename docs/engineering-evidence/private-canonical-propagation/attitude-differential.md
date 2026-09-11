# Bounded attitude-integration differential

**G — MIXED / INSUFFICIENT EVIDENCE**

Candidate causality: **NOT ESTABLISHED** for the original 12,336-byte witness.
Lead: **ESCALATE TO PROJECT CONTROL**. Candidate remains **FOLLOW-UP REQUIRED**;
remaining acceptance was not resumed.

## Identity and design

Baseline HEAD is `f64dc07f23a0a765b9b07dd49b895a8f3cb5ebfe` (M14.14), also canonical
HEAD/main/origin/main. Candidate branch remains
`codex/root-linked-private-propagation`. All 315 current source/project
fingerprints matched before/after diagnostics, including the six candidate hashes
in [identity.json](identity.json). No production or permanent test was edited.

Two sparse detached worktrees used the banked commit. The candidate arm overlaid
only the new propagation source, its analytical test and current Simulation
Program.cs. Both built the Release Simulation project/dependency closure once,
with zero warnings/errors. No Graphics/native project was built or run.

The banked runner lacks isolated/prefix selectors. Identical disposable wiring
added an isolated call before existing selectors and an exit after the registered
target. Full arrays were retained: 45 baseline groups, 46 candidate groups.
The exact shared prefix has 27 predecessors and target ordinal 28. All predecessor
names, exact edits, commands, runtime configs and binary hashes are retained in
[attitude-differential.json](attitude-differential.json).

`Program.cs:280`, `SpacecraftAttitudeIntegrationTests`, is unchanged after LF
normalization (SHA256 `DFEF5A432C87D9073F3926F4D9D2C066A4FD4EEDE1FBD59618D81167A323E327`).
The helper is byte-identical. Surrounding candidate wiring alone adds the prior
new selector/final group. Banked underlying production is unchanged.

Original setup constructs the spacecraft store, frames and transactions, including
an initial frame extraction. Immediate warmup is one `view.TryGetAttitude` call.
The measured loop retains 100,000 lookups, attitude evaluations, frame extractions,
success checks and checksum mixing. The existing 1 MiB checked no-GC helper opens
the per-thread counter after entry and closes it before exit/reporting. Exact zero
and all workload checks remain. No timing responsibility is mixed into this gate.

All runs used the same canonical cwd, installed dotnet host and identical net10.0
runtime configuration with normal patch selection (10.0.12 installed). No tiering,
PGO, profiler or GC settings changed. No per-process CLR module trace was taken.
Selector wiring necessarily changes entry/runner IL; this limitation prevents
assuming equivalence to every runtime effect of the original full executable.

## Isolated matrix

Five alternating pairs, fresh process each time, no retries/rebuilds:

| Pair | Baseline bytes | Candidate bytes | Baseline exit | Candidate exit |
|---|---:|---:|---:|---:|
| 1 | 0 | 6192 | 0 | 1 |
| 2 | 0 | 0 | 0 | 0 |
| 3 | 0 | 6192 | 0 | 1 |
| 4 | 0 | 0 | 0 | 0 |
| 5 | 0 | 0 | 0 | 0 |

Every no-GC entry/exit passed. Nonzero runs failed the unchanged independent
zero-byte assertion. Their exact two failure excerpts are in the JSON.

## Suite-order matrix

Three alternating pairs, retaining all 27 predecessors in registered order:

| State | Run | Prior groups passed | Bytes | Entry | Exit | Result / process exit |
|---|---:|---:|---:|---|---|---|
| Baseline | 1 | 27 | 0 | PASS | PASS | PASS / 0 |
| Candidate | 1 | 27 | 0 | PASS | PASS | PASS / 0 |
| Baseline | 2 | 27 | 0 | PASS | PASS | PASS / 0 |
| Candidate | 2 | 27 | 0 | PASS | PASS | PASS / 0 |
| Baseline | 3 | 27 | 0 | PASS | PASS | PASS / 0 |
| Candidate | 3 | 27 | 0 | PASS | PASS | PASS / 0 |

One fresh existing positive control per arm detected **152 bytes** from byte[128],
with successful entry/exit and process exit 0.

## Judgment and payoff

The original **12,336 bytes never recurred**. The candidate nevertheless failed two
isolated runs at **6,192 bytes**. Baseline did not fail; all prefix runs passed.
This is an observed candidate-only association for a different intermittent
witness, not proof of the original witness's cause. A stable candidate-only
12,336-byte effect, baseline reproduction and all-clean differential are each
unsupported. Classification F's five acceptance runs therefore were not opened.

The strongest challenge is that two candidate-only failures could reflect an
indirect candidate assembly/registration effect. They cannot be dismissed. The
counter magnitude changed and the original suite-order effect did not reproduce,
so the evidence also cannot establish propagation allocation or a baseline defect.

Historical relationship: **SAME NUMERIC WITNESS / CAUSAL RELATION UNKNOWN** applies
to the original 12,336 versus historical reports. No relation between that
mechanism and current 6,192 is established. No CLR-bug claim is made.

PAYOFF: more uninstrumented runs would not identify an owner. An observer might
inform a bounded correction of the new witness, but this matrix has not localized
a mechanism and an opportunistic observer run could simply miss it. No observer,
bisection, production/test correction or tolerance change was attempted. Project
Control must decide the next bounded question; full acceptance remains blocked.

## Reproduction and cleanup

Use the saved candidate fingerprints; do not discard canonical work. Create two
detached `--no-checkout` worktrees at the banked SHA under
`E:/NovaCore/.codex/paired-attitude-differential/{baseline,candidate}`. In each use
`git sparse-checkout set --cone src/NovaCore.Core src/NovaCore.EphemerisFormat src/NovaCore.Simulation tests/NovaCore.Simulation.Tests`,
then checkout the same SHA. Overlay these canonical files into candidate only:

- `src/NovaCore.Simulation/Spacecraft/Contact/PrivateCanonicalPropagation.cs`
- `tests/NovaCore.Simulation.Tests/PrivateCanonicalPropagationTests.cs`
- `tests/NovaCore.Simulation.Tests/Program.cs`

In each disposable Program.cs insert the JSON's exact `test.selector` before the
first existing selector, and replace only the existing one-line runner loop with
`test.prefixLoop`. Leave the array and gate body unchanged. Normalize line endings
to LF for the edit, verify the method/helper hashes and identical prefix, then
build once per arm with the recorded build command. From canonical cwd execute
the recorded isolated command in B1/C1 through B5/C5 order, prefix B1/C1 through
B3/C3, then one positive control per arm. No retries or builds inside matrices.
These are reproducibility instructions, not authorization to rerun the campaign.

The two comparison trees contained **616 files / 40,545,373 logical bytes** before
retirement. All content was reviewed: banked files, copied canonical candidate,
the exact disposable selector edits and rebuildable outputs. No unique production
work or source assets belong to them. The JSON records final removal status.
Only this report and its compact JSON are newly retained; original failure and
candidate evidence remain unchanged. No copied build tree or repeated full stdout
is retained. Bepu/solver and propagation architecture remain outside this ticket.
