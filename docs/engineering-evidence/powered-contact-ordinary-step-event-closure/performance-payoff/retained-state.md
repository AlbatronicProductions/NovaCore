# No retained-state substitution

The active mapper body and Ratio helper are text-identical to the previous
retained correction after normalizing Map's private/internal visibility.
FixedInput/Import are unchanged. Core SHA-256:
F113D819540483A32B3863E97CD2516A7FA3DA9BD556364E7DABBECF1603C46C.

New mapper static state, heap caches, pools, thread locals, retained arrays,
mutable shared workspaces and precomputed per-event answers: **NONE**.
Map recomputes exact duration/duty inputs and every physical projection on each
call using the current linked banked fixed-workspace source.

Measured value sizes: FixedInput 3,832 bytes (raw immutable source numerators,
denominators and signs); Projected 48 bytes; Prepared 24 bytes. These are value
representations, not new retained heap buffers. TryRatio uses its existing two
35-word stack spans, not a second implementation. No total peak stack-depth claim
is made. The cold diagnostic BigInteger/JSON import is representation setup,
not a precomputed event answer or a live canonical resource owner.

The benchmark owns ordinary cold worlds/pools, input/expected-result documents,
capture arrays and three preallocated sample arrays. Those are disclosed test
fixture/reporting resources, not the mapper's allocation workaround. B executes
the complete Map and six float projections within the timer/checked window;
all output bits are consumed and validated. The fixed fixture's native inverse
mass is set during original-style cold construction; recomputed inverse mass is
checked against it, with no redundant body reconstruction in the measured step.

Cold cost per process (2,304 worlds, each with 120 preparation steps):

| Process | Mean cold preparation/world, us | Cold managed counter bytes | Maximum two-world native pool bytes |
|---|---:|---:|---:|
| 1 | 577.4163 | 115,028,328 | 6,029,312 |
| 2 | 617.8751 | 115,034,520 | 6,029,312 |
| 3 | 597.6268 | 114,985,320 | 6,029,312 |

Cold counter totals are allocation traffic during construction/preparation, not
retained storage or zero-allocation results. Pool values are allocator-reserved
native blocks for two simultaneous diagnostic worlds, not process working set.
No production memory/storage acceptance is inferred. Construction and disposal
are outside warmed timings; their effect on normal process history remains.
