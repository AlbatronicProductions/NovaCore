# Normal-runtime tails: three processes, no retry

Units: microseconds. Full top-ten indexed observations and preparation/native
components are in candidate-process-1/2/3.json. No extra run or profiler was used.

| Process | B median | B P95 | B P99 | B max | Max sample index | Preparation at max | Binding/native at max |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 23.2 | 257.0 | 311.7 | 384.4 | 127 | 376.6 | 7.8 |
| 2 | 24.0 | 260.4 | 313.2 | 357.2 | 320 | 353.1 | 4.1 |
| 3 | 24.1 | 266.6 | 304.8 | 369.1 | 323 | 357.2 | 11.9 |

This is a repeating preparation-side elapsed-time pattern, not one isolated max.
The top-ten B sample indices lie in 29–192, 54–325 and 196–323 respectively.
P95 is about 0.26 ms in every process, over ten times each process median.
Standalone C, measured later in the same process, has medians 20.4/20.3/20.6 us
and maxima 37.2/45.3/35.3 us. Do not replace B's tails with C's later distribution.

All sampled A/B/C windows have generation collection deltas 0/0/0, including
their top-ten tails. The wider bracket, from before fixture loops through the
separate allocation controls, has 9/4/3 in each process. It includes cold fixture
construction/disposal and checked-region effects; it is not whole-process GC or
proof of a GC cause for the timed tails. A's maxima 74.7/43.8/11.6 us fall in its
binding/native portion. No synchronization is added except ordinary backend
single-thread work; scheduler interference, JIT/tiering and hidden runtime causes
are UNATTRIBUTED. No cause is inferred from sample index or magnitude.

The direct timestamp split localizes B's elapsed interval to preparation, not
specifically useful CPU arithmetic. Preemption can be charged to that interval.
Timestamp overhead is included and not subtracted; 10 MHz clock resolution is
0.1 us. Cold creation/check order is A then B, while timed order alternates.
Consequently differences of independent medians are descriptive, not a precise
causal measurement. B's preparation medians 20.7/21.3/21.5 us and C provide the
directly observed preparation boundaries.

1,024 samples per process support only about ten samples beyond P99. Top ten
and nearest-rank quantiles do not prove a universal upper latency bound. No
isolated budget was issued, so these tails are not converted into an invented
failure threshold. They prevent an unqualified integrated-payoff claim.
