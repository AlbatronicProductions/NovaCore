# Affected validation of the first ready/apply correction

SDK10.0.303, runtime10.0.12, existing Windows host and runtime configuration.
Only affected Simulation project closure was built. Full solution/suites and remaining
parent qualification were conditional on the control; they were not resumed.

## Build

- Debug initial attempt: one CS0246 in new Lifecycle test (missing Translation namespace
  import for SpacecraftPhysicalProperties). Mechanical test import added before execution.
- Debug corrected build: PASS, zero warnings/errors.
- Release build: PASS, zero warnings/errors.

No performance gate was retried. No physical expectation or test threshold was changed.

## Focused runs

Each row ran once in Debug and once in Release, fresh processes, all PASS:

| Route | Protected outcome |
|---|---|
| --powered-contact-lifecycle | 11 fixtures x3 frontiers; exact full records/native endpoint identity versus standalone API; ready cleanup/refusal/retry; original/prepared mapper equality |
| --powered-contact-cheap | Literal1/60 six native bit witness; full-powered, exact endpoint exhaustion and retained dry continuation; joint successor/duplicate refusal |
| --powered-contact-physics | Existing11 numerical/impulse/penetration rows and unchanged acceptance bounds |
| --powered-contact-authority | Full existing owner/stale/nonmutation/lease/event/debt/history/retry/terminal/export/property-only matrix |
| --powered-contact-allocation | Five exact-zero contact windows and deliberate allocation control |
| --finite-propellant-cheap | 6141 binary64 and512 random cases; exact conservation, mass law, authority/nonmutation |
| --powered-free-flight-authority | M15.0 owner, nonmutation, atomic successor, revision, exhaustion, lease, endpoint-only, terminal, event/debt/history |
| --powered-free-flight-allocation | M15.0 seven exact-zero windows plus positive control |

Contact allocation in both configurations: complete dry continuation0, genuine exhaustion
and continuation0, no-work0, backlog0, mapper0 bytes. Every checked no-GC entry/exit passed.
Deliberate byte[128] control152 B. Free-flight seven windows0 B, control152 B.
No new collector/profiler/runtime configuration or timing-isolation boundary.

The Lifecycle mapper equality test is value equality; it is not separately a signed-zero
bit proof. The common unchanged projection finish, retained six-bit literal test and exact
native/physical trajectory comparison supply complementary numerical evidence.

## Predeclared control campaign

Exactly three fresh Release processes planned, first failure stops. Existing prime128,
then cold control preparation, warm128 and measured1024 complete operations per process.
Control is CenteredBaseline, engine OFF, still-fuelled .000013020833333333334 kg.

| Run | Median | P95 | P99 | Max | Result |
|---|---:|---:|---:|---:|---|
| 1 | .0683 | .0732 | .0963 | .1417 | FAIL median |
| 2 | -- | -- | -- | -- | NOT RUN, required stop |
| 3 | -- | -- | -- | -- | NOT RUN, required stop |

Milliseconds. Limits .050/.100/.250/.500 unchanged. Worst sample index504. GC0/0/0,
one owner thread, solver8/1, existing exclusive owner phase. No synchronization added.

The full operation includes copied observation, exact host admission, commands/resource,
step/export/preparation/publication and acknowledgement. No multiplication by renderer work.
No visible route, manual acceptance or full production-powered qualification is claimed.

## Storage and cold cost

Measured managed cold-allocation conservative upper bound4,942,336 B plus retained native
pool458,752 B = **5,401,088 B**, below8,388,608 B. Required history4,857,624 B included,
1200 records of4048 B plus array header. This is not process working set or an exact managed
heap retention census; transient cold allocations make it an upper bound.

Delta versus prior same-fixture upper bound:104 B. Composition of added fields: one bool
plus long in existing owner and three PoweredBinaryScale values in each of preparation and
binding; both copies counted while reachable. No new per-interval heap owner or collection.
Measured cold setup2.4176 ms; separate from warm timing. No cold optimization attempted.

First control failure stopped all subsequent qualification. The final authorized median
investigation is separate, diagnostic-only, and documented in ../final-median-closure/.