# Current complete operation, validation and reconstruction inventory

Static current-source audit. No checks removed. Counts refer to the successful warmed
OFF/still-fuelled measured operation, not all branches, first ignition or arbitrary backlog.
The complete boundary is `PoweredContactTests.Qualification.cs:10-18`: observe copied
frontier; derive next integer credit; admit host credit; service; assert one publication.

## Source locator key

All files are under `E:\NovaCore` and hashes are in identity.json.

| Key | File |
|---|---|
| PF | src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredFreeFlight.cs |
| PC | src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredContact.cs |
| EP | src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.EnginePreparation.cs |
| PR | src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Propellant.cs |
| CM | src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.SpacecraftCommands.cs |
| WP | src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Powered.cs |
| W | src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs |
| PUB | src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs |
| MAP | src/NovaCore.Simulation/Spacecraft/Actuation/OrdinaryContactInputProjection.cs |
| NUM | src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs |

## Complete ownership chain

| Operation | Owner / authoritative input | Mutable dependencies | Output / next consumer | Lifetime / invalidation |
|---|---|---|---|---|
| Observation | PoweredFlightStorage, PF:110 | Last canonical observation and terminal flag | Copied frontier/clock/endpoint; host caller | Value only, no current-source execution authority |
| Host credit | Engine and clock, PF:150 | Full expected clock/source/world, host sequence, outstanding leases | Prepared debt/remainder plus matching expected-clock acknowledgement | One host phase; canonical accounting remains if later work fails |
| Command observation/closure | CommandStorage, CM:150/219 | Revision/state/sequence, pending queue, horizon, captured engine edges | Closed exact source boundary and requested demand | Closed demand frozen for interval; future source/lease/command mismatch invalidates |
| Engine demand | EnginePreparationStorage, EP:129 | Definition/capability, closed command revision/sequence, cursor, source physical/frame/clock | Sealed preview: force/moment/flow, latch, exact start/end | Active engine proposal; retirement/parent consumption/source change ends it |
| Resource source | PropellantPreparationStorage.Canonical | Exact units, definition, resource revision, canonical physical mass/inertia | Copied exact source and checked source mass | Binding plus each current source/acknowledgement generation |
| Resource proposal | Same owner, PR:110 | Genuine engine parent, exact source, positive integer interval | Exact consumed/successor units, powered/unpowered durations and proposed successor mass | Private seal+generation; expires on retirement, source/parent change or joint commit |
| Exact powered duration | Sealed resource preview | Current exact demand/resource and interval | Rational duration; contact mapper or M15.0 consumer | Interval-local, not next interval or arbitrary copied preview |
| Source mass/inertia | Exact-resource consistency proof and captured physical source | Definition, exact remaining units, source physical bits | Source-held numerical mass/inertia | Same acknowledged source; joint successor replaces it |
| World/body authority | LocalContactWorld + PoweredBinding | Receipt seal/frontier/pending, disposal/poison, expected authority | Permission for one native step | Original world/gen/body/config fixed; frontier advances and pending blocks next step |
| Contact source | LocalContactSource + configuration | Expected canonical authority advances; source T0/frame/end do not | Exact target and local/root mapping | Entire episode until invalidation/disposal; no T0 or moving-origin reset |
| Event mapper input | Engine selected contact consumer, PC:80/WP:86 | Sealed segmentation, source orientation/mass, fixed gravity/frame, target | Pure copied OrdinaryContactInput | One actual interval; contains no execution capability |
| Mapped native values | Same preparation, MAP:17 | Duty, source wrench/rotation/mass/inertia | Linear/angular FP32 acceleration and inverse mass; installation | Constant during native solve; source successor invalidates reuse |
| Native preparation | World, WP:112 | Live binding, pending flag, source frontier | Property-only SetLocalInertia, callback input, PreparedFrontier | Before one native step; poison covers unsafe failure |
| BEPU solve | Retained Simulation/body, W:124 | Private pose/velocity/manifold/impulses | Advanced private body | Ordinary 8/1 solve; failure destroys continuation permission |
| Export and seal | World then powered owner | Post-native authority, envelope/support, receipt generation/frontier | Copied endpoint, private pending receipt, joint proposal | One outstanding endpoint; publication-only retry if still valid |
| Transaction preparation | Engine, PF:257 | Current source/engine/resource leases, exact endpoint, events/debt/history/slot | Canonical successor, record, observation, ack targets | Prepared before final applicability; not yet canonical |
| Canonical commit | Existing exclusive owner phase, PF:297 | No ordinary interleaving permitted | Paired endpoint+mass, resource, actual actuator, clock/debt, revision, one history record; leases consumed | One fixed commit; never rolled back on ack failure |
| Acknowledgement | PoweredFlightStorage + PoweredBinding | Prevalidated destinations, successor observation | Expected authority/frontier update; pending cleared | Fixed writes; unexpected failure terminally invalidates continuation |

## Eight powered-source validations, individually reconciled

Shared body PF:46-59 checks: exact issuer reference and noninvalidated owner; StateRevision,
TimelineRevision, full clock and supported 1:1/unpaused policy; physical source bits; craft
definition; exact canonical resource and resource definition. It invokes the world comparison.
Cost **L exclusive**, with the nested world work separately charged to **D**.

| # | Call site | Can facts change since earlier source check? / reason to recheck | Stale consequence |
|---|---|---|---|
| P1 | PF:157 host entry | Yes: observation released phase; external source/clock/timeline may change | Wrong host credit acknowledged to stale episode |
| P2 | PF:169 host final | Pure clock preparation has no external callback; still protects final accounting applicability | Canonical debt/private expectation diverge |
| P3 | PF:330 first service iteration | Yes: host API returned and released phase | Servicing stale source or externally modified debt |
| P4 | PF:199 physical prepare entry | Commands and private engine/resource leases changed; canonical physical/resource did not | Wrong interval/parent/source used for native work |
| P5 | PF:239 post-native prepare | Native body/frontier/pending changed; canonical source should remain old | Sealing unsafe advanced continuation or mismatched source |
| P6 | PF:259 publication entry | Direct call shares phase; separately entered publish/retry may have intervening mutations | Publishing stale source/lease or substituted native endpoint |
| P7 | PF:291 publication final | Preparation has no phase release/callback; remains final canonical protection | Mixed canonical successor |
| P8 | PF:330 next loop | Yes: canonical joint commit and ack changed physical/resource/clock/revision/frontier | Failure to detect bad acknowledgement or next-interval authority |

P2/P4/P5/P6/P7/P8 are not identical deletable calls. Their immutable portions are possible
proof consumers; their changing stage/world/lease/ack obligations remain.

## Thirteen world-authority comparisons

Each P1-P8 calls WP:71-82 through PF:59, accounting for W1-W8. The five others are:

| # | Exact route | Boundary and distinct requirement |
|---|---|---|
| W9 | PC:93 -> WP:90 | Input permission; no pending endpoint, correct expected source |
| W10 | PC:98 -> W:128/187 -> PUB:71 | Prestep receipt/configuration/world/target validation |
| W11 | W:144 -> PUB:71 | Poststep source comparison while poison deliberately remains set |
| W12 | PC:101 -> WP:146 | Read freshly staged native endpoint with genuine receipt |
| W13 | PF:280 -> WP:146 | Read pending endpoint for exact publication match; may be retry |

Compared facts: engine/authority references; receipt owner, private seal, generation and
frontier; current StateRevision/TimelineRevision/full clock/exact resource; physical source
and craft definition; pending policy and `frontier == acknowledgedFrontier + pending`.
`CheckPoweredAuthority` additionally checks thread/disposed/invalidated, WP:62-64.
W11 intentionally uses the comparison beneath the poison check; it cannot be replaced by
blind success because the prestate was good. Receipt/endpoint substitution would publish
the wrong private state. Attribution D has **15 scopes**, not thirteen equal-cost buckets:
PreparePoweredInput and Step also contribute exclusive preparation/admission work.

## Other material checks in the successful operation

The table enumerates predicate responsibilities and all repeated readers. Callee arithmetic,
finite-value and normalization validity checks are included as named responsibilities, not
treated as authority checks that can be removed. Failure-only paths have additional checks.

| Location | Exact fact / authority | Change boundary and current reason | Failure consequence / cost |
|---|---|---|---|
| PF:41-44, observe/host/service | Owner thread, not executing group, exclusive phase acquisition | Every independently entered operation | Foreign/reentrant writes; A |
| CM:61-71 x10 | Capability, supported clock, source range, craft/root, no missed pending command, representable/nonregressing funded horizon | Command drain/close, engine initial/final and six engine reads | Stale/missed command authority; B |
| CM:150-160,219-229 | Exact current/open boundary, remaining due commands, event exclusion | Commands legitimately change before closure | Replayed or skipped demand; B |
| EP:129-148 | Owned phase, engine owner, supported clock, exact next start/end, closure, no active/consumed/gapped interval, event exclusion, physical/frame/epoch validity | Engine issuance after command closure | Wrong physical interval/demand; B |
| EP:154-171 | Ordered edge identity, sequence/cursor equality, finite throttle in bounds, finite thrust/force/moment/flow | Edge consumption/latch preparation | Replay, invalid numerical input; B |
| EP:183-185 | Current command authority/revision/sequence/closure, state/timeline/clock | Final engine preview admission | Seal stale engine demand; B |
| EP:212-224 x6 | Owned phase, issuer, active seal/boundary, command source, clock/state/timeline, physical bits and definition | PR:114/140 plus PR:166 x4; exposed preview APIs share reader | Retired/foreign/stale parent accepted; B |
| PR:110-142 | Owner/active/genuine parent; target and checked positive interval; generation capacity; exact source-mass consistency; exact segmentation; successor mass positive, powered/unpowered within interval; final parent/source consistency | Resource issuance | Fuel overdraw, stale source, invalid duration; B with J separately |
| PR:161-168 x4 | Owner/capability, active seal/generation, valid parent, exact source resource and mass | PF:203/242/262/292 | Forged/stale/retired resource accepted; B/J |
| PF:330-340 | End/frontier, no outstanding stages, exact next target, event, history, exact funded debt, four-operation service budget | Each loop; after publication values change | Step past authority/capacity or lose budget/debt; A/L |
| PC:85-94 | World/config, revision/actuator/resource capacity, history, checked positive funded interval, event | Last cheap gate before native preparation | Irreversible unsafe step; G |
| WP:90-107 | Authority, owned phase, expected clock/target, exact source mass/inertia, qualified latch/throttle, event, 16666/16667 ticks | Input preparation | Wrong native input/domain; D |
| MAP:20-44 | Positive ordinary interval, finite force/moment/gravity, positive finite source mass/inertia; exact duty within interval; ratio success/range; finite FP32 acceleration, positive inverse mass/dt | Fresh projected interval input | Numerical failure before solve; C/K |
| WP:114-122 | Owned phase, live world, no pending; poison around property/input mutation | Native property installation | Unsafe retained continuation; I |
| W:128-157 | Receipt/config/source; no pending, correct prepared frontier; exact next target; solver/coverage success; local envelope, quaternion canonicalization, root spacing and finite export | Native solve changes private state | Invalid endpoint/continuation; D/E/F |
| PC:104 | ContactPoints >0, ConstraintCount ==1, independent penetration <=.001 | Result of actual native solve | Outside supported physical model; F/G |
| PF:201-245 | No joint active proposal, generation capacity, genuine resource interval/frontier, successor revision arithmetic, poststep source/resource | Joint proposal issuance | Wrong/overflowed/stale successor; G/L/B/J |
| PF:261-294 | Joint active seal/generation, resource equals stored segmentation, EndpointOnly, event, checked funded debt, history, applied slot, exact native endpoint bits, prepared ack, final source/resource/slot | Publication entry/final; independent retry releases phase | Mixed/double canonical commit; G/L/D/B/J |
| WP:166-171 | Owned phase, live world/binding, engine, host versus pending, host sequence or endpoint frontier | Ack target preparation | Canonical/private disagreement; G/A |

Ten command validations: CM:153 no-command drain + CM:223 close + EP:133/183 + EP:217 x6.
Source/slot lookups are bounded existing array lookups; event test is a heap peek, history
append is O(1). No historical record scan, solver reinitialization or hidden profiler belongs
to the complete path. Fixed-size copies are real work but not all invalidation-proof work.

## Six source-mass reconstructions

Common body PR:37-47: definition equals canonical resource definition; exact
`DryMassUnits + RemainingUnits`; banked `TryToKilograms`; capture physical source; compare
mass **bits** and dry inertia. Cost J includes that entire body, not only conversion.

| # | Location | Why now / inputs | Can exact mass inputs change before next site? | Potential proof owner |
|---|---|---|---|---|
| M1 | PR:124 | Establish source mass before exact segmentation | Not inside this continuous owned operation; separate API can expose later gaps | Acknowledged powered source; otherwise keep generic resource establishment |
| M2 | PR:141 | Recheck before resource seal | Pure preparation did not change canonical resource/physical source | Same bound exact-source proof plus final applicability |
| M3 | PR:167 <- PF:203 | Admit genuine resource preview to physical preparation | Separate prepare call can have gap; continuous service does not | Resource seal/generation + acknowledged source |
| M4 | PR:167 <- PF:242 | Recheck after native mutation | Native mass/pose changed; canonical source resource/mass did not | Same proof, but post-native failure/authority check remains |
| M5 | PR:167 <- PF:262 | Publication or independent retry admission | Exposed publication may follow external mutation/lease retirement | Same proof only after fresh live dependency check |
| M6 | PR:167 <- PF:292 | Final canonical applicability | No callback/release before commit | Same proof plus final live identities/leases/slot |

**One extra successor projection** at PR:132-134 is not a seventh source reconstruction.
It is required when deriving a changed resource successor; it belongs to B, not J.

An inductive invariant is available: PC:29 cold binding establishes exact source-resource
to physical mass bits; PF:297-300 jointly installs exact successor units and their prepared
mass; successful acknowledgement advances both expected values; failed ack invalidates.
The result can be owned by the existing powered/resource preparation stores. A naked cached
double, a matching numerical mass, or ResourceRevision alone is not that proof. Retiring the
lease, changing definition/units/physical bits or failing acknowledgement ends its validity.
Generic M14.24 APIs are not entitled to the contact-specific invariant automatically.

## Ratio projection lifetime and actual consumer

MAP:28 computes duty `(powered.Numerator * ordinaryTickDenominator) /
(powered.Denominator * ordinaryTickNumerator)` with NUM.TryRatio. MAP:33 computes ordinary
seconds `ordinaryTickNumerator / (ordinaryTickDenominator * 1,000,000)`.

| Projection | Failed control | Owner / stable lifetime | Invalidated by | Reuse conclusion |
|---|---|---|---|---|
| K1 exact powered duty | Zero numerator; NUM:37 returns before fixed quotient workspace | Genuine resource preview, exact start/end and source/parent generation | Resource/engine/interval substitution or retirement | Once per resource interval; not repeated in later current consumers |
| K2 ordinary duration | Nonzero 16666 or16667; 54 quotient iterations | Immutable admitted episode schedule | Different schedule/unit contract or episode binding | Two schedule-derived constants can be prepared by the unchanged routine; not an event cache |

There are **two calls**, not two duplicate ratios. K's aggregate `.03674296875 ms` does not
separate their elapsed shares. Dividing it by two would misattribute the zero fast path.

Mapped accelerations/inverse mass additionally depend on source attitude, frame, wrench and
mass. Their value is stable for one private step, never automatically for the next canonical
successor. Publication retry consumes the already staged endpoint and does not map or solve.

Current `OrdinaryContactInput.Duration` is returned and validated but **is not used by
InstallPoweredInput**. W:136 independently computes native dt from target/current exact
ticks. Permanent mapper replay tests do observe Duration. This is a duplicate duration
responsibility, not permission to remove the mapper output/contract or alter native dt here.
This table records pre-correction call sites. The later authorized correction reuses cold schedule facts and a bounded ready source lifetime; see correction-contract.md and validation.md. Exact numerical algorithms and native timestep remain unchanged.
