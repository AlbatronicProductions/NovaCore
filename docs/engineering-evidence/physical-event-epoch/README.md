# Internal physical-event epoch candidate evidence

## Current closeout status

2026-09-10: Project Control has technically accepted the M14.6 candidate.
It remains UNBANKED. This closeout changes evidence retention only; no production
or permanent test edits and no validation reruns. See the [file-by-file retention
ledger and reconstruction recipe](RETENTION.md).

Latest technical result: [ordinary-class migration and final validation](ordinary-class-migration/README.md).
Resolved mechanism: [runtime accounting](runtime-accounting/README.md).
Measurement surface: [42-window inventory](zero-allocation-inventory/README.md).
Historical narrowing: [matched A/B](matched-full-suite-ab/README.md) →
[epoch group](structural-omission/README.md) → [Allocation phase](allocation-phase/README.md) →
[GC region](gc-region/README.md) → accounting proof →
[servicing boundary](measurement-boundary/README.md) → inventory → ordinary migration.

The reports below and in stage directories retain their original stage-specific
failures, uncertainty, stop decisions and storage snapshots. They do not override
the final accepted result. Their old scratch commands/counts are historical and
were not executed or remeasured during this evidence-only closeout.

## Original architecture investigation

2026-09-09. **PHYSICAL EVENT EPOCH CANDIDATE — FOLLOW-UP REQUIRED**.
Lead judgment: **REVISE**. Proposed accomplishment: **Establish internal exact
physical-event epochs**. Unbanked; no milestone number assigned.

Subsequent bounded [debt-gate classification](debt-gate-classification.md) identifies
the reproduced full Debug failure as allocation-counter-only (8,160 bytes).
Its mechanism and candidate causality remain unproven. The initial observations
below are preserved as recorded; the linked follow-up was the next gate state at that stage. Current acceptance is indexed above.

## Baseline and scope

Started from clean `main`. HEAD, main and origin/main were and remain
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`.
Working branch: `codex/physical-event-epoch`. No index/ref publication performed.

| Banked tag | Commit |
| --- | --- |
| m14.1-canonical-surface-point-queries | `5b9b02ab1d2ba0e903809510908e4819984d4de2` |
| m14.2-spacecraft-translational-authority | `caa6d93ccd7c4e5924381e077a62585f99e5a32a` |
| m14.3-spacecraft-terrain-contact-observations | `79978cf8b0fd783d43a661df6df53a2c0202af4a` |
| m14.4-atomic-contact-response | `28b92f724f8decd3c9356aa5f9f5bbd6c9dd069c` |
| m14.5-isolated-analytical-contact-response | `b53d48025cfb8142cb32a6fc6fceb6018e33cfcd` |

WHAT: internal canonical/rational identity and deterministic ordering.
HOW: a 24-byte floor/reduced-fraction value and existing tie-order semantics.
BAR: exact normalization, comparison, binary round trip, overflow rejection and
unchanged integral order; no public fractional time or execution.
PAYOFF: the smallest time-identity prerequisite for future private physical events.
See the [current contract](../../physical-event-epochs.md) for the complete layout,
limits and intentionally unimplemented event-local/contact responsibilities.

Production edits are two internal types plus extraction of the existing equal-time
comparer helper. The live comparer still starts with integral `SimulationInstant`.
Tests add one group and two focused command switches. No other existing test body
was edited; banked contact/state/transaction implementations are untouched.

## Arithmetic and ordering proof

- Int64 floor plus UInt64 reduced proper fraction; default equals canonical zero.
- Signed Int128 division precedes floor addition. Negative remainder borrows one
  tick and adds the positive denominator. No negation of Int128.MinValue occurs.
- Checked addition and explicit Int64 floor bounds reject carry overflow.
- Denominator positivity/capacity checked before reduction; inputs beyond UInt64
  are explicit capacity failures rather than silently reduced into acceptance.
- Comparison cross-products use UInt128. `(2^64-1)^2 < 2^128`; supported products
  fit. An adversarial pair has cross-products above Int128.MaxValue differing by
  exactly one and still orders correctly.
- Independent test-only BigInteger oracle: 175 boundary + 1,000 seeded cases,
  zero represented error. Includes negative borrow/carry, canonical extrema,
  equivalent fractions and exact `15625/32768` ticks.
- 50 shuffled arrivals yield the same eight-key physical/priority/sequence/ID
  order, including canonical/rational interleaving and same-tick subevents.
- 48 integral headers yield 2,304 pairwise comparisons matching an independent
  pre-candidate comparator, both through the live comparer and embedded keys.
- Serialization covers exact bytes/values, golden `-1+1/2` vector, malformed and
  non-reduced input, unknown version/kinds, size bounds and unchanged short-write
  destination. Unknown certified-root kind is rejected, never approximated.

## Validation and stopping decision

| Gate | Debug | Release |
| --- | --- | --- |
| Full `NovaCore.sln` build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Focused physical epochs | PASS, exact cases and 0 warmed bytes | PASS, exact cases and 0 warmed bytes |
| Full Simulation, 37 groups | 9 PASS / 1 FAIL / 27 not reached | 9 PASS / 1 FAIL / 27 not reached |
| ReferenceFrames | 11 groups PASS | 11 groups PASS |
| Precision | PASS | PASS |
| Focused translation | PASS | PASS |
| Focused M14.3 contact generation | PASS | PASS |
| Focused M14.4 contact response | PASS | PASS |
| Focused M14.5 isolated response | PASS | PASS |

Both full Simulation runs stopped in `HostDurationDebtServiceTests`,
`tests/NovaCore.Simulation.Tests/Program.cs:1228`, at:
`repeated long-duration servicing is allocation-free`.

This unchanged assertion combines **five** conditions: current-thread allocation
counter unchanged; clock 50,000 ticks; zero pending simulation debt; 5,000 processed
events; zero pending timeline events. The output does not decompose them. The
failing operand, byte delta and causal owner are **UNKNOWN**. The name is not proof
of an allocation failure, nor does unchanged assertion text exonerate the candidate.
The new group precedes this test and could affect runtime state; the shared comparer
also participates in existing ordering. Candidate causality is not established or
excluded by these runs.

The preceding new epoch group passed in both full runs. Earlier hashes in both:
event order `871F0B2A00578823`, timeline `5B42511799128BEF`, mixed timeline
`EE895B4573F23F30`, clock `97E0A268E405BFE5`, host conversion `A6D4EC25DBF2641F`.
The timeline allocation check before the failed group reported zero bytes.

PAYOFF recheck: classifying this composite gate is a new causal investigation,
outside this identity/order ticket. No retry-to-pass, baseline campaign, profiling,
assertion change or production fix was performed. Independent contact gates were
run directly because the full suite did not reach them. They do not replace the
27 unexecuted groups or clear acceptance. Return the candidate for Project Control
to decide a bounded follow-up. Manual visual acceptance is NOT REQUIRED for this
identity/order change; automated acceptance is not yet cleared.

## Bounded performance observation

One retained warmed Release observation, 21 samples per operation. Values are
percentiles of **batch-average** nanoseconds, not individual-operation tail latency.

| Operation | Median ns | P95 ns | P99 ns |
| --- | ---: | ---: | ---: |
| Epoch comparison plus checksum | 41.412 | 46.899 | 47.510 |
| Normalization/construction plus hash/checksum | 188.092 | 207.660 | 213.647 |
| Copy and sort 128 order keys, per set | 13,238.281 | 113,223.438 | 114,028.125 |

Comparison/construction samples contain 16,384 operations; sort samples contain
128 sets. The large sorting tail spread is retained, not smoothed or explained
without evidence. This is not a production timeline speedup comparison, stable
whole-frame budget or optimization justification. Live integral events do not
use rational cross-products. No optimization campaign followed.

Combined warmed construction/comparison/sort/serialization measured **0 managed
bytes on the calling thread**. The permanent test uses the existing repository's
test-only no-GC accounting boundary, with success required and finally-protected
exit. It does not change ordinary runtime tiering or production GC policy. This
claim excludes setup, other threads and native allocations.

## Independent verification

VERIFY A arithmetic/order: independent reviewer found no blocker after attacking
negative floor normalization, Int128 extrema, UInt128 cross-products, same-time
ties and adversarial insertion order. Wider products and independent BigInteger
oracles protect the actual numerical risks.

VERIFY B compatibility/scope: independent reviewer found no identity/scope blocker.
Public time remains canonical, the timeline is not replaced, binary decoding is
strict, and unknown roots require a new typed contract. Exact rational time is
not asserted to qualify general contact state. The independent reviews do not
override the failed full-suite gate or establish its cause.

## Narrow reference provenance

KSA installed reference: `2026.9.7.5402`, revision
`487c3f340de24c6a81037120b6d1129c045c5400`.
Current `E:\Kitten Space Agency\KSA.dll` SHA-256:
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
Existing installed-production decompilation reused read-only:
`build/ksa-residency-reference/assembly-source/KSA/UniverseTime.cs` has an Int128
nanosecond lattice; `ConstraintSim.cs:834–858` accepts double durations and passes
float durations to solver mechanisms. UniverseTime source SHA-256:
`80275BF45075DAF852C8395FF6D9AF02CE67B3428BE6601F444263B55CD3D03F`.

**ADAPT** execution/presentation time separation; **INTENTIONALLY DIFFER** in
NovaCore's stronger exact rational ordering requirement. KSA is not proof of that
rational or general contact-state contract. No broad history research was needed.

Historical Mobile source:
`E:\NovaCore Mobile\src\dynamics\private-dynamics-event.js`, SHA-256
`6DAE4609E77B6236B65C3443FCAE886578B8FBD3CBDD9DD1111981E1DC820573`.
Reduced-fraction physical event precedent was inspected narrowly. The prior
architecture gauntlet's known prescribed-attitude floor fallback is explicitly
not adopted; desktop has no approximate canonical conversion. No proprietary or
Mobile source was copied into production.

## Reproduction and source identity

Run from repository root. Do not treat the failing full suite as accepted:

```powershell
dotnet build NovaCore.sln -c Debug --nologo -v:q
dotnet build NovaCore.sln -c Release --nologo -v:q
dotnet run --project tests/NovaCore.Simulation.Tests -c Debug --no-build -- --physical-event-epoch-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --physical-event-epoch-only
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --physical-event-epoch-performance
```

Full Simulation uses the same command without a selector. Independent selectors:
`--translation-only`, `--contact-only`, `--contact-response-only`,
`--isolated-contact-only`, each run in both configurations. ReferenceFrames and
Precision use `tests/NovaCore.ReferenceFrames.Tests` and
`tests/NovaCore.Precision.Tests` respectively, both configurations, `--no-build`.

Working-file SHA-256 (actual Windows bytes before Git line-ending normalization):

| Path | SHA-256 |
| --- | --- |
| src/NovaCore.Simulation/Time/PhysicalEventEpoch.cs | `0F4923E14C9370F18A0F77C73B6BCA88CB51E6D56EDC7D632D596ABC326DF3C2` |
| src/NovaCore.Simulation/Timeline/PhysicalEventOrderKey.cs | `C65C540656C553610DC8ABC36D26967EA874A682E0E1D67E4EFA8374239C6FCF` |
| src/NovaCore.Simulation/Timeline/SimulationEventHeaderComparer.cs | `0C5F4A0957EBCF2528243909BC0372CBA8C3084337051F295D5AE24863135D65` |
| tests/NovaCore.Simulation.Tests/PhysicalEventEpochTests.cs | `F601C9A61E76C16F82BCE9761431805CA7890A531897D54FE3912F42F6AB003D` |
| tests/NovaCore.Simulation.Tests/Program.cs | `872B9E2D2AB58CBAF7F573F0D916ACEB2DCD0BB72649D8B105D65DD59EC1C11C` |

## Evidence lifecycle

Retain this concise report, current contract and permanent test/reproduction code.
No raw log archive, profiler trace, comparison checkout or diagnostic capture was
created. Ordinary ignored build outputs remain normal rebuildable dependencies.
New disposable diagnostic output remaining: **0 bytes**. Disposed: **0 bytes**.
Previously existing workspace artifacts were not deleted or reclassified here.
