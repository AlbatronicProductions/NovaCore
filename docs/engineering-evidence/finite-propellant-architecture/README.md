# Finite propellant and exact exhaustion architecture

**Architecture investigation only. PASS recommendation; stop for Project Control. No implementation, staging, banking or milestone assignment.**

Verified baseline: HEAD/main/origin/main and remote main `cad23d1ba9476a75d9897b8c20924cc3f1497db9` (documentation provenance repair). M14.23 `6c9830237bc29bbd3c9cf5f4644b6409c32eaa9b`, tag `m14.23-prepared-single-engine-actuation`, is BANKED. The current checkout is `codex/single-engine-actuation-preparation`; it already points at that public baseline. No branch/ref change is needed for this evidence-only investigation.

## Decision

Recommend **RESOURCE-FIRST SEGMENTATION UNDER THE EXISTING TRANSACTION OWNER**:

`genuine active M14.23 proposal → resource-owner exact availability/segmentation → sealed resource/mass-model successor proposal → future private physical evaluation → joint canonical resource/actual-actuator/mass/paired-state/clock/debt/history commit → private acknowledgement`.

The first implementation ticket should stop at the sealed resource/segment preview. It must not apply thrust, consume canonical fuel, change canonical mass or publish actuator state.

Seven authorities remain separate: requested commands; prepared engine demand; available canonical resource; realizable powered segment; proposed resource successor; proposed physical successor; eventual joint canonical commit. Contact/free-flight selection does not own fuel.

## Decisive findings

1. Desktop has no canonical propellant store or consumption path. Current physical mass is one immutable FP64 scalar, with separately stored fixed inertia. Existing exact event types provide identity, not a general exhaustion executor.
2. M14.23's active capability must remain the source. A copied preview is not authority. Successful engine sealing already consumes the private preparation cursor, so resource refusal must retain and retry the same active engine proposal, not reprepare the interval.
3. Naive FP64 consumption is insufficient: a positive tiny burn can repeatedly subtract to the same mass. A positive physical event can round to zero seconds; a rounded full-interval demand can falsely classify exact endpoint exhaustion.
4. A bounded exact ledger covers every finite nonnegative binary64 mass/flow and admitted integer interval without growing precision: **34 UInt64 limbs / 272 bytes per integer**. A resource-owned exact ratio describes exhaustion; it is not forced into M14.6's UInt64-denominator format.
5. Existing M14.19/20 bindings reject mass/force/inertia changes and their acknowledgement does not update them. Future changing-mass powered motion needs an explicitly qualified sibling/extended publication and solver-continuity responsibility. Do not weaken the banked checks or append fuel subtraction after an old physical commit.

## Exactness contract

Use the accepted M14.23 binary64 flow bits `q` as the constant-flow model, not an unrounded reconstruction of thrust/exhaust speed. Let `S=1,000,000` and exact resource unit `Q=1/(S·2^1074)` kg. Decode source bits into integers:

`U = remaining mass / Q`, `V = q·2^1074`, `C = V·n`, where `n` is the exact interval tick count.

- `U>C`: whole interval powered; successor `U−C` stays positive.
- `U=C>0`: whole interval powered; empty exactly at the target, no interior split.
- `0<U<C`: exhaustion at exact relative tick offset `U/V`; successor zero; exact remainder `(nV−U)/V`.
- Empty source: full unpowered interval, no consumption; demand/latch remain requested/prepared.
- Zero realized demand: full unpowered interval, unchanged resource.

No floating product, quotient or subtraction determines authoritative availability, event identity or successor mass. FP64 copies are observations only; small exact residuals never vanish through a roundtrip. This is exactness of the admitted numerical model, not a claim of exact physical thrust parameters or exact integrated trajectory.

One positive-thrust/zero-flow M14.23 underflow case must refuse at resource compatibility admission. It is an inconsistent demand, not a valid fractional exhaustion event. Valid positive mass/flow ratios, including subnormal durations, remain representable and must not terminate a future admitted simulation merely because they are non-integral or round to zero in a display value.

Details and analytical witnesses: [exhaustion-event-contract.md](exhaustion-event-contract.md).

## Resource and mass decision

Choose **C: staged mass-property successor**, with canonical resource ownership distinct from dry-body authorship. The first scalar resource uses a fixed exact amount, stable resource/model identity and one authorized engine/feed binding. It is not a tank network.

For the smallest declared mass-law fixture, recommend an **ideal point reservoir at dry-body COM**: total mass is dry + remaining; COM stays fixed; the point contributes exactly zero inertia. This is a deliberate mathematical model, not a claim about real tanks or the banked engineering article. No existing article mass is silently relabelled dry. Source mass/inertia consistency must be checked at cold binding; a resource attachment cannot add mass to an already authored wet body. Future spatial tanks replace the versioned mass-distribution law without changing owner, ledger or segmentation semantics.

Future dynamics must use the evolving mass throughout the powered segment and an independently qualified exhaust/angular-momentum model. Constant mass integration followed by an endpoint mass edit is not acceptable. The current nonphysical slice changes no mass and therefore does not qualify those dynamics.

Starvation preserves Enabled intent and separately reports NoFeed. It does not destroy the engine, discard throttle or create a restart consumable. Refueling remains unimplemented; the ideal memoryless law has no inherent new-ignition requirement once an independently authorized future feed restoration is admitted. A safing interlock would be a separate explicit policy.

## Revision, history and atomicity

Recommend a dedicated source `ResourceRevision`: increment only on canonical resource state changes, exclusively in the eventual joint transaction. Do not use CommandRevision or M14.23's private cursor. Physical StateRevision advances once per published interval; future actual ActuatorRevision reflects actual hardware/frontier progression. An internal exhaustion split does not alter public clock authority or TimelineRevision.

Use one bounded joint physical-publication record with exact resource before/after, resource revision, engine/interval provenance, exact split witness, mass-law identity and paired physical endpoint provenance. No independent fuel-event queue or duplicate unbounded histories. One resource owner decides shared expenditure; a second independent consumer/proposal cannot spend the same source.

All validation/preparation precedes fixed writes in the existing exclusive owner phase. If canonical combined commit succeeds but private acknowledgement fails, all canonical successors remain committed and private continuation is invalidated. Neither rollback of fuel alone nor a misleading ordinary-refusal result is permitted.

## Evidence and next scope

- [Current Desktop source map](current-novacore-map.md)
- [Resource ownership and future atomicity](resource-authority-contract.md)
- [Exact arithmetic, event and segment contract](exhaustion-event-contract.md)
- [Candidates, one implementation ticket, test bar and launch dependencies](architecture-candidates.md)
- [Current KSA source versus retained official history](ksa-resource-convergence.md)
- [Independent red team and preservation](verification.md)
- [Refs and fingerprints](identity.json)
- [Small arithmetic witness script](arithmetic-witness.py)

Investigation evidence budget: **96 KiB**. Retain summaries, source anchors, arithmetic witnesses and reproduction/search instructions only. No proprietary code, new build tree, profiler, trace or production prototype. No formal allocation/performance/acceptance campaign was run. The ledger and segmenter are a design recommendation, not implemented or performance-qualified code.

Future banking must include referenced accepted architecture evidence in the exact staged tree before staged-tree checks/builds, commit/tag, atomic push and remote verification. This investigation stages nothing. Do not repeat post-bank provenance repair.

**Next recommendation: Prepare finite-propellant segmentation from one genuine prepared engine proposal. Do not implement automatically.**
