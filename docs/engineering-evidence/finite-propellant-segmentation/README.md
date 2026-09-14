# Finite-propellant segmentation qualification

**PASS — ready for Project Control review. UNBANKED; no milestone assigned.**

Baseline HEAD/main/origin/main/remote main: `cad23d1ba9476a75d9897b8c20924cc3f1497db9`.
Candidate branch: `codex/finite-propellant-segmentation`.
M14.23 remains banked at `6c9830237bc29bbd3c9cf5f4644b6409c32eaa9b`; M14.22/M14.21 and all historical tag refs remain unchanged.

This implements the [accepted architecture](../finite-propellant-architecture/README.md) through **sealed resource preparation only**. It adds no canonical resource-consumption method, mass change, actuator publication, physical integration, BEPU modification or powered-contact path.

## Exact resource and segment contract

`PropellantInteger` is a 34-UInt64 inline value: **2,176 bits / 272 bytes**. For S=1,000,000 ticks/second and Q=1/(S*2^1074) kg:

- initial source U = exact accepted binary64 kilograms / Q;
- V = exact accepted M14.23 binary64 kg/s * 2^1074;
- required C = V*n for exact integer ticks n.

IEEE sign/exponent/significand bits are decoded directly. Negative nonzero/nonfinite values refuse; signed zero is numeric zero. Availability does not use floating multiplication, division, subtraction, tolerances or clamps. The implementation uses bounded UInt128 limb intermediates and refuses final carry/borrow; no production BigInteger, dynamic limb array or heap event exists.

Verified bounds are 2,098 bits for V, 2,118 for initial U, 2,161 for V*positive Int64 ticks, and at most 2,119 for two finite authored mass ledgers summed. The engine source currently admits its exact 16,666/16,667-tick schedule. The wider helper bound is independently checked against long.MaxValue.

| Case, in order | Conditional result |
|---|---|
| Legitimate zero flow/demand | NoDemand; full unpowered interval; unchanged resource; no event |
| U=0 with positive demand | NoFeed; full unpowered; command/latch untouched |
| U>C | FullPowered; consumed C; successor U-C |
| U=C>0 | EndpointExhaustion; full interval powered; endpoint empty; no interior event |
| 0<U<C | InteriorExhaustion; powered U/V ticks; remainder (nV-U)/V; consumed U; successor zero |

Positive physical demand with stored flow zero refuses as IncompatibleFlow. It does not become fuel-free power.

The duration type stores a fixed exact numerator/denominator and compares against the integer interval without floating conversion. At most two copied segments expose the original BODY wrench then zero ENGINE wrench. Other forces are not canceled. No root-space freeze or average thrust is introduced. No public fractional tick/event queue is created.

Exact means exact for the admitted numerical model, not exact real-world parameters or an integrated trajectory. A positive powered duration below binary64 seconds range remains positive exact data. No approximate duration API was needed in this slice.

## Cold source and proposed mass law

One owner-issued source binds a stable resource/feed/engine/dry-body identity and versions, exact initial/remaining amounts and source ResourceRevision. ResourceRevision remains zero; no live writer exists.

CentralPointReservoirV1 explicitly models the resource as a mathematical point at independently authored dry COM: total mass=dry+usable; COM fixed; reservoir inertia zero; dry principal inertia unchanged. Cold binding requires finite positive dry mass/inertia, finite nonnegative resource, and exact-law source mass/inertia consistency. Invalid or overflowing wet mass refuses before installation.

Mass observation adds exact dry and resource units, then rounds ONCE to binary64 nearest-even. Exact resource storage does not roundtrip through that view. The proposal reports conditional source/successor mass properties; no canonical/BEPU mass is installed. Future changing-mass and exhaust-momentum dynamics remain unqualified.

The banked article is not reauthored or assigned added fuel. Positive-resource tests use dedicated dry-body fixtures. The article non-actuation witness uses an explicitly ZERO-resource adapter with unchanged structural mass and nonzero prepared engine demand.

## Genuine proposal ownership and refusal

The existing public M14.23 Preview wrapper still acquires the same exclusive owner phase. Its original source predicates were mechanically compared and remain identical, apart from extraction into a private in-owned-phase reader with an explicit guard. Resource code uses that reader; a copied engine preview cannot authorize preparation.

Preparation validates cold resource identity, active parent capability/lifetime, exact target, full canonical source, mass law, arithmetic and copied values before one fixed private seal update. It changes only private generation/parent/preview/active fields. Canonical amount, all revisions, commands, physical state, clock/debt/history and engine preparation cursor remain unchanged.

Only one resource and one outstanding dependent proposal are admitted. Resource-only retirement releases the slot but leaves the parent active. Repreparing the same valid parent yields equal conditional values and a fresh private lease; the old lease cannot replay. Parent retirement or source changes make the dependent preview stale. Generation overflow refuses rather than wrapping.

The exact exhaustion witness includes deterministic resource/engine/interval/source facts, not a process-global lease or live object. It describes a WOULD-event and cannot execute or spend anything.

## Executed validation

- Debug and Release full solution builds: PASS, zero warnings/errors.
- Independent arithmetic/source tests: PASS in both configurations; 6,141 exponent/mantissa cases, 512 seeded cases, carry/borrow/capacity boundaries, rounding midpoint-cell oracle, quantum edges, tiny flows/resources, exact ratios, conservation and refusal/lifetime/nonmutation.
- Full Simulation: **66/66 Debug, 66/66 Release**, one fresh process each, no retry.
- Final independent review strengthened only the ratio oracle after those full runs. Rebuilt and reran the complete focused correctness group in Debug/Release: PASS. Production and measurement code did not change.
- M14.23 focused Debug definition/capture/identity/refusal and allocation tests: PASS; its complete registered groups also passed in both full suites.
- M14.22 command and M14.21 original physical groups: PASS in both full suites.
- ReferenceFrames, Precision and verified BEPU dependency tests: PASS Debug/Release.
- Source/ref/link/whitespace checks: PASS.

### Nonphysical article witness

| Case | Matched endpoints | Original final support | Reference peak penetration |
|---|---:|---:|---:|
| Centered | 1,200/1,200 bit-identical | 600/600 | 0.005819714882528193 m |
| Tilted | 1,200/1,200 bit-identical | 600/600 | 0.006945546380241474 m |
| Tilted, moving frame | 1,200/1,200 bit-identical | 600/600 | 0.006945746950922316 m |

Passed in Debug and Release full runs, plus focused Debug. Each retains the original world, 400/800 interval counts, 20,000,000 ticks, 1,200 revisions/history entries and zero final debt. This proves preparation/retirement does not actuate the existing article; it is not a powered article qualification.

### Allocation

Eight distinct checked no-GC gates each ran 128 warm operations then 1,024 measured operations: full, endpoint, interior, tiny interior, empty, no demand, copied observation and refusals. **Every result: exactly 0 bytes, entry/exit PASS, Debug and Release.** The existing deliberate byte[128] control reports **152 bytes**. Full suites also pass these gates. Timing is separate and runs under ordinary runtime behavior.

### Retained storage

| Retained component | Bytes |
|---|---:|
| Immutable resource/mass-law definition, including initial and dry exact units | 656 |
| Issued authority, including remaining units and ResourceRevision | 312 |
| One preparation slot, parent token, generation and 3,224-byte preview | 3,288 |
| Private seal object | 24 |
| Measured newly retained object graph | **4,280** |
| Additional owner reference | 8 |
| Bounded total including owner reference | **4,288** |

Object layout is Windows x64/.NET-specific. Component sizes derive from fixed field payloads and aligned object layout; the total graph was directly measured with checked allocation isolation around only definition/binding creation. All four constructed objects remain reachable. The extra owner field is reported separately. Exact integer=272, ratio=544, source observation=640, copied exhaustion witness=1,512, proposal token=16, progress=16 bytes. Copied values are not additional retained heap objects. No growing history is allocated.

### One Release cost measurement

256 warm operations, 4,096 timed interior preparations including source checks, exact classification, sealing, copied preview/segment/source observation. Retirement is outside timing; no force integration.

| Median | P95 | P99 | Maximum |
|---:|---:|---:|---:|
| 0.0325 ms | 0.0379 ms | 0.0712 ms | 0.1171 ms |

Report-only cost; no new threshold or physical-integration claim. Runtime was ordinary, without profiling/tiering changes. The later oracle-only assertion addition did not alter production binary or measurement-source bytes; both measured and final test-assembly identities are retained.

## Review, evidence and reproduction

[Independent red team](verification.md), [structured results](validation.json), [fingerprints](identity.json), [reproduction script](reproduce.ps1).

Two initial test-construction issues were corrected: explicit ReferenceFrameNode typing resolved an overload ambiguity; an incorrect expectation that 4,096 exact tiny debits would still round to 1 kg was replaced by the independently correct next-lower-double witness. No production arithmetic failure was observed. The independent review then required the direct powered-ratio oracle; it was added and passed. These are retained rather than hidden as an all-first-attempt success.

All 1,932 originally tracked files were fingerprinted: only the intended M14.23 reader file and test registration file differ; **1,930 remain identical**. Six new source/test files contain the new responsibility. All nine accepted architecture files and the preexisting staging blocker remain untouched. Blender and dependency/source identities are unchanged.

Permanent evidence budget: **128 KiB**. Retain five concise files, no raw stdout/build copies/runtime dumps. No new disposable comparison tree was created or unrelated scratch removed. Normal bin/obj outputs remain rebuildable development output.

Future banking must include the accepted architecture package and this package in its exact stage tree, verify dependency/link closure and build that tree. Nothing is staged, committed, tagged or banked here.

**PASS. Stop for Project Control. No automatic fuel commit, mass change, wrench application, powered contact or milestone assignment.**
