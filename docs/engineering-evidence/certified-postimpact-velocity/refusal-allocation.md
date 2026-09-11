# Bounded refusal-path allocation classification

**PASS — CERTIFIED POST-IMPACT VELOCITY CANDIDATE — READY TO RESUME ACCEPTANCE.**
The remaining acceptance matrix was not resumed. Candidate remains unbanked on
`codex/certified-postimpact-velocity`, baseline
`e5fde08bcae834d8abc107cd26e7601fbfa3e1e0`. No milestone number is assigned.

## Baseline and measured-window audit

Before instrumentation, all seven candidate production/test fingerprints matched
the original [identity record](identity.json). HEAD/main/origin/main and M14.12
remained at the banked baseline; all M14.1–M14.12 tag objects/targets matched.
Index empty. Original witness: 256 bytes across eight calls, checked entry/exit PASS.

R1, R2, R3, R4, R5, R6, R7 and R8 are eight successive executions of the **same**
`CertifiedPostImpactVelocityMath.Select(input,response,preimpact,out _)`, expecting
`UnresolvedResponse`. They are not eight different refusal scenarios. Inputs are
the already constructed exact-rational fixture with COM X=`2^54`, normal X=1,
relative normal speed=-1, zero lever, identity attitude, mass/inertia=1.

The caller creates fixtures before measurement, executes its original 32 warmup
calls and 101 ordinary timing samples, sorts the timing array, then measures eight
calls. Each call compares the returned enum directly with `expected` and increments
an integer completion count. There is no lambda/delegate/interface/enumeration,
array/collection creation, boxing, string formatting or exception construction in
that measured loop. The local `Nanoseconds` function and JSON report occur after
the window. Warmup/timing diagnostic strings and the timing array are outside it.

`OrdinaryAllocationMeasurement` is a ref struct. Its constructor enters the checked
1 MiB no-GC region before opening the counter. `Complete` closes the counter,
guarantees exit, then prints. Entry/exit errors propagate; no fallback or retry.
This source audit alone did not attribute the allocation.

## Disposable decomposition and exact six-run matrix

Only `CertifiedPostImpactVelocityTests.MeasureRefusal` was temporarily changed.
Setup, warmup, timing work, production arguments and expected statuses were retained.
The eight-call loop was split into eight checked 1 MiB windows. Each window contains
one Select call plus the original enum comparison/completion increment. It closes
before per-call output, enum formatting, total addition or any control allocation.
All eight deltas and statuses are reported before the unchanged exact-zero total
assertion throws. Failures remain failures; reporting does not make them passes.

Both diagnostic configurations were built before execution. Fresh processes ran
Debug 1/2/3 then Release 1/2/3. No builds during either matrix and no retries.

| Configuration | Run | R1 | R2 | R3 | R4 | R5 | R6 | R7 | R8 | Total bytes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Debug | 1 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |
| Debug | 2 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |
| Debug | 3 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |
| Release | 1 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |
| Release | 2 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |
| Release | 3 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 32 | 256 |

Every one of 48 measured windows entered/exited successfully and returned the
expected `UnresolvedResponse`. Every run completed 8/8 calls and then failed the
unchanged exact-zero assertion. The existing separate `PositiveControl()` detected
**152 bytes** for `byte[128]` in all six processes. No new control mechanism.

Primary classification: **A — ALL EIGHT REFUSAL CALLS ALLOCATE CONSISTENTLY**.
This applies to the eight measured repeated calls; it does not claim that every
distinct refusal path was measured separately.

## Narrow production reachability and cause

After the stable matrix earned static attribution, the exact path was examined:

```text
MeasureRefusal -> Select -> ArithmeticSupported -> Environment.Version
                                                    -> new Version(10, 0, 12)
```

The installed .NET 10.0.12 `System.Private.CoreLib.dll` was read offline through
`System.Reflection.Metadata`/`PEReader`, without loading a profiler or running a
new probe. Its `Environment.get_Version` IL is:

```text
1F0A161F0C73E82A00062A
ldc.i4.s 10
ldc.i4.0
ldc.i4.s 12
newobj 0x06002AE8  // System.Version::.ctor(int32,int32,int32)
ret
```

Token resolution confirmed `System.Version` and the three-int constructor signature
`200301080808`. Its four instance fields are `_Major`, `_Minor`, `_Build`,
`_Revision`, all Int32. The x64 object's four fields plus object overhead match the
**independently measured** 32 bytes per call. Constructor count comes from installed
IL, not a claimed allocation callback. The candidate newly accessed this allocating
getter on every arithmetic check. The positive control, static constructor and
subsequent exact-zero correction establish a real candidate-owned allocation,
not prior-context accounting or the test's output strings.

Candidate causality: **CANDIDATE PRODUCTION CAUSED**. This is ordinary framework
object construction requested by candidate code; it is not a CLR bug. The same
arithmetic helper is also reachable from qualification and checked Read, but their
complete acceptance measurements remain pending. No broader audit was performed.

No narrow observer, EventPipe, allocation callbacks or runtime/JIT settings were
needed. The optional observer was not justified once the installed getter identified
the mechanism. CoreLib identity and decoded metadata are in
[the machine-readable record](refusal-allocation.json).

## One bounded correction

Only `CertifiedPostImpactVelocityMath.cs` changed permanently. The supported runtime
version predicate now initializes once at the arithmetic class's lifetime boundary.
The runtime version is immutable for the process. Only the Boolean applicability
result is retained; no mutable physical/numerical result is cached. Windows/x64 and
all floating-point witnesses still execute on every call, so mutable thread FP
capability is never cached. The exact same version predicate remains in force.

This removes repeated metadata preparation at its owner. It does not add extra
warmup, retries, numerical tolerances, special zero-allocation exceptions, a new
solver dependency, candidate search changes or recipe changes. The existing
warmup remains exactly 32 calls. One-time metadata construction remains possible
at type initialization, outside the warmed allocation contract.

Before correction, temporary test changes were restored byte-for-byte. The focused
test SHA-256 again equals
`ee820d67d9a0b99cbf098cc45a6fd617adba4217e74bfcd0ce14f38ff4feee19`.
The other six original candidate fingerprints match except for the one explicitly
corrected math file, whose before/after hashes are recorded. All 229 banked
production C# files and all banked tags remain unchanged.

## Focused post-correction validation

Declared before execution: rebuild focused test project Debug/Release once; run
the normal permanent focused test once per configuration, fresh processes.
Both builds passed with zero warnings/errors. No remaining acceptance suite ran.

| Normal focused run | Response-resolution bytes | Exhaustion bytes | Entry/exit | Positive control | Result |
|---|---:|---:|---|---:|---|
| Debug | 0 / 8 calls | 0 / 8 calls | All PASS | 152 | PASS |
| Release | 0 / 8 calls | 0 / 8 calls | All PASS | 152 | PASS |

The original exact statuses/completion counts remain asserted. All focused
analytical/arithmetic/refusal/exhaustion tests completed. No allocation threshold
or permanent test source changed. The focused stdout residuals/candidate IDs match
the original six analytical witnesses; full real-root bit replay remains pending.
The JSON retains normal-runtime refusal timings separately from allocation; these
are local test observations, not whole-frame or general-solver performance claims.

## Reproduction and evidence lifecycle

The original candidate hashes and stopped output remain in `identity.json` and
`validation.json`; current math hash and this task's results are in
`refusal-allocation.json`. To reproduce the original matrix in a reviewed disposable
state, use the before-correction math fingerprint, replace only the measurement loop
as described above, and invoke the existing positive control after all eight reports
but before `RequireZero`. Build both configurations first, then launch three fresh
Debug and three fresh Release processes with:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --certified-postimpact-velocity-only
```

Use `Release` for that arm. Restore the test's verified original bytes afterward.
The correction is exactly the movement of the unchanged version comparison into
`CheckRuntimeVersion()` and an immutable class field; reversing only that local
edit restores the original math implementation. No new diagnostics are required
to rerun the current normal focused tests. Source hashes, not stale binaries,
define both comparison states.

Evidence budget remains 64 KiB for this package. Retain concise original witness,
matrix, IL/identity, correction, restoration and reproduction instructions. No raw
stdout, copied framework assembly, temporary instrumentation source, observer or
disposable build tree was retained. The ordinary build outputs were rebuilt from
the restored permanent tests. No cleanup of unrelated prior scratch was attempted.

The bounded isolated-contact specialization does not choose NovaCore's future
general constraint solver. Mandatory KSA/Bepu convergence assessment before
persistent support/rest remains unchanged. Stop for Project Control.
