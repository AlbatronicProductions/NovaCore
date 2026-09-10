# Complete permanent Simulation zero-allocation inventory

Counting unit: each statically distinct measured counter window protected by an exact-zero assertion. The 42 rows map to 38 assertion sites: rows21–23 share one assertion,36–37 share one,38–39 share one. Rows1–40 are reached by the default37-group runner; rows41–42 are opt-in performance assertions. Loop sizes/modes do not create additional source sites.

Pattern A = CHECKED NO-GC ISOLATED PER-THREAD COUNTER; B = RAW PER-THREAD COUNTER WITHOUT CHECKED NO-GC ISOLATION; E = HYBRID (raw counter combined with timing/benchmark duties). All rows use GC.GetAllocatedBytesForCurrentThread and exact zero; no total-counter or object-observer zero gate exists in current permanent tests.

V = VULNERABLE TO PROVEN ALLOCATION-CONTEXT RETIREMENT EFFECT (static mechanism match, not a demonstrated false positive at that gate). R = ROBUST against that specific effect; L = LIKELY ROBUST, checked isolation but no local deliberate control. Robust does not mean fully requalified candidate acceptance.

Exposure G# refers to the default group number. G4 precedes epochs; G6 is epochs; G7 onward follows its region and increasing setup/history allocations. Opt-in paths bypass default group history but retain their own allocation-heavy setup. Pos: Present is local; Indirect is evidence from the qualified pattern elsewhere only, not a local control.

| # | Gate | File / Method | Production Contract | Measurement Pattern | Checked No-GC | Exact Zero | Positive Control | Composite | Order Exposure | Vulnerability |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Speed preset access | [Program.cs:107](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:107) SolarUtcAndSpeedPresetTests | 100000 preset Get/label/rate accesses; checksum not MinValue | B | False | Yes | ABSENT | checksum != long.MinValue | G4 | V |
| 2 | Physical epochs | [PhysicalEventEpochTests.cs:238](E:/NovaCore/tests/NovaCore.Simulation.Tests/PhysicalEventEpochTests.cs:238) Allocation | construction/comparison/sort/serialization; 3x1024 Batch plus 1024 wire cycles | A | True | Yes | INDIRECT ONLY | No | G6 | L |
| 3 | Timeline topology | [Program.cs:1059](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1059) TimelineTopologyTests | 9900 schedule/cancel pairs; invariants checked separately | B | False | Yes | ABSENT | No | G7 | V |
| 4 | Clock steady state | [Program.cs:1110](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1110) ClockTests | 100000 advance/boundary/pause/resume/rate cycles | B | False | Yes | ABSENT | No | G8 | V |
| 5 | Host conversion | [Program.cs:1163](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1163) HostDurationTests | 100000 host advances at rate 5/7 | B | False | Yes | ABSENT | No | G9 | V |
| 6 | Single debt service | [Program.cs:1218](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1218) HostDurationDebtServiceTests | one preallocated 1000-event ServicePendingHostDurationDebt call | B | False | Yes | ABSENT | Completed AND event count 1000 | G10 | V |
| 7 | Repeated debt service | [Program.cs:1236](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1236) HostDurationDebtServiceTests | 499 completed service calls; clock50000/debt0/processed5000/pending0 independently | A | True | Yes | PRESENT | No | G10 | R |
| 8 | Per-event transaction | [Program.cs:1315](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1315) TransactionTests | 5000 ExecuteCanonicalPendingEvent commits; processed5000 | B | False | Yes | ABSENT | processed count 5000 | G11 | V |
| 9 | Canonical group | [Program.cs:1363](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1363) CanonicalGroupTests | one ExecuteCanonicalGroup consumes5000; IsComplete | B | False | Yes | ABSENT | IsComplete AND processed5000 | G12 | V |
| 10 | Clock orchestration | [Program.cs:1407](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:1407) ClockExecutionTests | one AdvanceAndExecuteOneCanonicalGroup at tick2 with1000 events | A | True | Yes | PRESENT | No | G13 | R |
| 11 | Celestial store | [Program.cs:472](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:472) CelestialContractTests | 100000 traversal/lookup/validation/hash cycles | B | False | Yes | ABSENT | traversal != 0 | G14 | V |
| 12 | System definitions | [Program.cs:515](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:515) CelestialSystemDefinitionTests | 100000 traversal/lookup/hash cycles | B | False | Yes | ABSENT | traversal != 0 | G15 | V |
| 13 | Body catalog | [Program.cs:545](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:545) CelestialBodyCatalogTests | 100000 identity/physical property lookups | B | False | Yes | ABSENT | checksum != 0 | G16 | V |
| 14 | SolAnalytical warm evaluation | [Program.cs:597](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:597) SolAnalyticalDatasetTests | 100000 ten-body evaluations/lookup/hash cycles | B | False | Yes | ABSENT | warm checksum != 0 | G17 | V |
| 15 | System time mapping | [Program.cs:649](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:649) CelestialSystemTimeAndProvenanceTests | 100000 exact time mappings | B | False | Yes | ABSENT | checksum != 0 | G18 | V |
| 16 | Ephemeris catalog | [Program.cs:677](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:677) CelestialEphemerisCatalogTests | 100000 source/payload lookups and hashes | B | False | Yes | ABSENT | checksum != 0 | G19 | V |
| 17 | Sampled evaluation | [Program.cs:695](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:695) CelestialSystemEvaluationTests | 100000 SampledDemo evaluations | B | False | Yes | ABSENT | No | G20 | V |
| 18 | SolMini evaluation | [Program.cs:709](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:709) CelestialSystemEvaluationTests | 100000 evaluations and root composition hash | B | False | Yes | ABSENT | composition != 0 | G20 | V |
| 19 | Lunar timed evaluation | [Program.cs:127](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:127) CelestialBodyOrientationTests | 100000 lunar evaluations; checksum; mean <10000ns | E | False | Yes | ABSENT | checksum != MaxValue AND ns <10000 | G21 | V |
| 20 | All-body timed evaluation | [Program.cs:127](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:127) CelestialBodyOrientationTests | 90000 orientation evaluations; successful evaluations/checksum | E | False | Yes | ABSENT | allEvaluated AND checksum != MaxValue | G21 | V |
| 21 | Circular propagation | [Program.cs:762](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:762) TwoBodyPropagationTests | 100000 circular evaluations; timing/iteration reporting | E | False | Yes | ABSENT | shared three-delta assertion at771 | G22 | V |
| 22 | Elliptic propagation | [Program.cs:765](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:765) TwoBodyPropagationTests | 100000 elliptic evaluations; timing/iteration reporting | E | False | Yes | ABSENT | shared three-delta assertion at771 | G22 | V |
| 23 | Propagation adapter/refusal | [Program.cs:768](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:768) TwoBodyPropagationTests | 100000 adapter/invalid-mu/hyperbolic/nonconvergent batches | B | False | Yes | ABSENT | shared three-delta assertion at771 | G22 | V |
| 24 | Spacecraft attitude | [Program.cs:173](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:173) SpacecraftAttitudeTests | 100000 prescribed attitude evaluations and hash | B | False | Yes | ABSENT | No | G23 | V |
| 25 | Spacecraft translation | [SpacecraftTranslationTests.cs:275](E:/NovaCore/tests/NovaCore.Simulation.Tests/SpacecraftTranslationTests.cs:275) Allocations | 100000 pure linear plus coherent motion evaluations | B | False | Yes | ABSENT | checksum > 0 | G24 | V |
| 26 | Contact generation | [ContactGenerationTests.cs:164](E:/NovaCore/tests/NovaCore.Simulation.Tests/ContactGenerationTests.cs:164) Allocations | 10000 four-feature batches; successes10000 independently | A | True | Yes | INDIRECT ONLY | No | G25 | L |
| 27 | Atomic contact response | [ContactResponseTests.cs:556](E:/NovaCore/tests/NovaCore.Simulation.Tests/ContactResponseTests.cs:556) Allocations | 256 EvaluateNext+ValidateAndCommit coupled transactions; histories/counts/revision/pending checked separately | B | False | Yes | ABSENT | No | G26 | V |
| 28 | Isolated response policy | [IsolatedContactResponseTests.cs:199](E:/NovaCore/tests/NovaCore.Simulation.Tests/IsolatedContactResponseTests.cs:199) Allocations | 10000 policy evaluations; all succeed | A | True | Yes | INDIRECT ONLY | all evaluations succeeded | G27 | L |
| 29 | Attitude integration | [Program.cs:201](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:201) SpacecraftAttitudeIntegrationTests | store/view evaluation/frame extraction loop and hash | B | False | Yes | ABSENT | No | G28 | V |
| 30 | Rigid rotation | [Program.cs:220](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:220) RigidBodyRotationTests | 100000 rigid-body evaluations and hash | B | False | Yes | ABSENT | No | G29 | V |
| 31 | Torque transaction lookup | [Program.cs:345](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:345) RigidBodyTorqueTransactionTests | 100000 rigid state lookups and ComputeHash; replacement evaluation is warmup only | B | False | Yes | ABSENT | No | G30 | V |
| 32 | Guidance | [Program.cs:245](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:245) FlightReferenceAndSasTests | 100000 flight-reference/target/SAS iterations and hash | B | False | Yes | ABSENT | No | G31 | V |
| 33 | SAS sign/frame proof | [Program.cs:289](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:289) SasSignFrameContinuityProofTests | 100000 SAS proof evaluations/hash | B | False | Yes | ABSENT | No | G32 | V |
| 34 | Orbit sampling | [Program.cs:362](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:362) AnalyticalOrbitSamplingTests | 10000 trajectory samples into preallocated257-vertex output | B | False | Yes | ABSENT | No | G33 | V |
| 35 | Celestial frame extraction | [Program.cs:825](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:825) CelestialFrameExtractionTests | 100000 evaluated frame extractions into preallocated values | B | False | Yes | ABSENT | No | G34 | V |
| 36 | Trajectory candidate creation | [Program.cs:904](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:904) CelestialTrajectoryReplacementTests | 100000 pure replacement candidates | B | False | Yes | ABSENT | shared commit-success AND two-delta assertion at908 | G35 | V |
| 37 | Trajectory commit | [Program.cs:907](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:907) CelestialTrajectoryReplacementTests | one ValidateAndCommit of prepared replacement | B | False | Yes | ABSENT | shared commit-success AND two-delta assertion at908 | G35 | V |
| 38 | Impulse evaluation | [Program.cs:987](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:987) CelestialImpulseEventTests | 100000 pure impulse evaluations | B | False | Yes | ABSENT | shared Completed AND two-delta assertion at989 | G36 | V |
| 39 | Impulse commit | [Program.cs:988](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:988) CelestialImpulseEventTests | one exact-time AdvanceAndExecuteOneCanonicalGroup impulse commit | B | False | Yes | ABSENT | shared Completed AND two-delta assertion at989 | G36 | V |
| 40 | Integral arithmetic/order | [Program.cs:424](E:/NovaCore/tests/NovaCore.Simulation.Tests/Program.cs:424) AllocationTests | 100000 instant/duration/rate/comparison iterations | B | False | Yes | ABSENT | No | G37 | V |
| 41 | Translation performance allocation | [SpacecraftTranslationTests.cs:297](E:/NovaCore/tests/NovaCore.Simulation.Tests/SpacecraftTranslationTests.cs:297) Performance | 101x1000 batches with1 and256 states; allocation plus timing and checksum | E | False | Yes | ABSENT | checksum > 0 | Opt-in | V |
| 42 | Isolated performance allocation | [IsolatedContactResponseTests.cs:234](E:/NovaCore/tests/NovaCore.Simulation.Tests/IsolatedContactResponseTests.cs:234) Performance | 3 modes; 21x128 isolated policy / transaction / observation-policy-transaction batches; sorted max delta0 | A | True | Yes | INDIRECT ONLY | No: max allocation only | Opt-in | L |

## Warmup, failure behavior and retained validation

Every A entry is checked by Check/Require; false throws. Every A exit is EndNoGCRegion in finally; exceptions propagate. No fallback/retry exists. For B/E, entry/exit behavior is N/A. No inherited no-GC region brackets any B/E gate: prior regions have exited before these methods. All allocation assertions with Composite=No are independent; other rows recommend SPLIT without changing any expected operand. The JSON records these fields individually for every gate.

| # | Existing warmup (no increase proposed) | Known validation from prior ticket | Allocation-assertion handling |
|---|---|---|---|
| 1 | Get(0) plus earlier preset checks | Prior Debug1 and Debug2 group PASS | SPLIT |
| 2 | 3 modes x4096 Batch | Prior Debug1 and Debug2 group PASS | KEEP |
| 3 | 100 schedules; earlier topology/cancellation tests | Prior Debug1 and Debug2 group PASS | KEEP |
| 4 | one cycle before counter | Prior Debug1 and Debug2 group PASS | KEEP |
| 5 | one advance on separate warm clock | Prior Debug1 and Debug2 group PASS | KEEP |
| 6 | one-event separate engine service | Prior Debug1 and Debug2 group PASS | SPLIT |
| 7 | original first 100-tick service unchanged | Prior Debug1 and Debug2 group PASS | KEEP |
| 8 | EvaluateNext plus earlier commits; no new batch warmup | Prior Debug1 and Debug2 group PASS | SPLIT |
| 9 | one-event group on separate engine | Prior Debug1 PASS; Debug2 composite FAIL; operand UNKNOWN | SPLIT |
| 10 | one-event separate-engine orchestration | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 11 | hash plus TryGetIndex | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 12 | hash plus TryGetNode | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 13 | Get and physical-property lookup | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 14 | system evaluate plus definition hash | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 15 | TryMapTime zero | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 16 | source lookup plus hash | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 17 | successful sampled evaluation and hash | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 18 | one evaluate | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 19 | per-body evaluate plus 100-call report probes | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 20 | per-body earlier calls; stopwatch start/stop/reset | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 21 | explicit circular evaluate plus prior oracle cases | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 22 | prior elliptic/hash cases; no dedicated extra loop | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 23 | explicit adapter plus prior refusal cases | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 24 | one zSpin evaluate | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 25 | 20000 coherent evaluations; prior pure numerical tests | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 26 | 20000 Generate calls | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 27 | 4 independently prepared 256-transaction batches | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 28 | 256 policy evaluations | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 29 | view lookup plus earlier integration cases | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 30 | one coupled evaluate | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 31 | one replacement evaluation | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 32 | one SAS evaluate plus earlier reference cases | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 33 | one SAS evaluate | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 34 | one sample call | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 35 | one extraction | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 36 | two explicit creation evaluations | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 37 | earlier transactions/replay; no additional measured-fixture commit | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 38 | one explicit evaluate plus replay | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 39 | earlier transaction/replay; no extra fixture commit | Prior Debug1 group PASS; Debug2 unreached | SPLIT |
| 40 | one arithmetic/scale/compare each | Prior Debug1 group PASS; Debug2 unreached | KEEP |
| 41 | 500ms evaluator warmup per state count | Not run in last correction ticket; no new dynamic evidence | SPLIT |
| 42 | 4 warm timing batches plus21 timing samples per mode | Not run in last correction ticket; no new dynamic evidence | KEEP |
