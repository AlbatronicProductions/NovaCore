# One correction: owner-prepared contact interval lifetime

The conditional Project Control steer authorizes implementation only after the direct KSA,
source lifetime and gross payoff gates pass. This design extends existing owners rather
than adding a new public packet or another authority. It is not a production acceptance.

## Issuance and invalidation

The existing PoweredFlightStorage records readiness only inside ServicePoweredDebt, after
its full canonical/world source check and before engine/resource/physical preparation.
The private ready frontier is scoped to one iteration under the continuously held existing
publication phase. It is consumed before the first canonical successor write. A finally block clears it on success, refusal, exception and terminal
failure, before another iteration or phase release. No caller can issue it or pass a trusted
boolean. Queries require the same engine owner phase, authority, world and source frontier.
Host admission and independently entered prepare/publish/retry never inherit readiness.

This is KSA's owner-controlled staged lifetime adapted to NovaCore's exact source authority.
KSA's mutable ready/apply buffers are not represented as immutable revision capabilities.
Existing engine/resource/physical seals remain the proposal authority; numerical copies do
not gain execution authority.

## Proven facts and consumers

Cold contact binding proves exact dry units plus source resource equals the physical mass.
Every successful joint successor atomically installs its exact resource and already-derived
mass, then acknowledges both. Fresh service entry checks exact resource and physical source
against that acknowledged pair. Six subsequent source-mass reconstructions can consume that
proof within the ready interval. Generic resource APIs and M15.0 retain exact reconstruction.
Successor resource arithmetic and mass derivation remain unchanged.

The two admitted ordinary durations (16666 and 16667 canonical ticks) are immutable numerical
facts of the episode schedule. Prepare their scales, plus the exact zero-duty scale, with
the existing ratio routine at cold preparation. The live mapper still validates its actual
powered numerator/denominator and exact interval. Only a valid zero numerator consumes the
prepared zero scale; a nonzero powered fraction is projected with the unchanged exact routine.
Complete products and FP32 conversion stay unchanged. The generic replay mapper remains
available with the same contract. Prepared numerical scales carry no canonical authority.

Four complete physical prepare/publication source checks consume the ready proof. Three
world comparisons during input/prestep/poststep consume it while keeping private identity,
receipt/seal/generation/frontier/pending, owner, disposal, configuration and event guards.
Native callbacks have no canonical mutation capability; cold transfer clears old native
aliases. The next full endpoint read checks the new receipt and live world after solving.

## Checks that remain

Both full endpoint/world reads; all four resource readers and their deep engine readers;
command revision/sequence/closed boundary; exact current resource; physical-content and
definition comparison; clock/StateRevision/TimelineRevision; both applied-slot checks;
joint proposal seal/generation; event/debt/history/revision arithmetic; exact staged endpoint
bits; acknowledgement admission; native numerical/coverage/export failures.

No arbitrary callback or phase release occurs between the last source checks and fixed
canonical writes. Physical commit and acknowledgement semantics are unchanged. A committed
successor followed by acknowledgement failure stays committed and poisons continuation.

## Failure and retry

Any precommit refusal clears only the transient ready permission. Existing engine/resource
leases and a sound pending endpoint retain their existing lifetime. An external retry enters
a new phase, performs full validation and publishes the already-solved endpoint; it never
restores the transient proof or solves again. Stale/replaced source, retired parents, wrong
receipt or poisoned/disposed world continue to refuse. External writes without revision
increments remain detectable through retained content comparisons.

## Measured design payoff

Same retained 1024-operation OFF, still-fuelled control; no new timings used for this decision.

| Disjoint whole work | ms/operation |
|---|---:|
| J, six source-mass reconstructions | 0.02659423828125 |
| K, both ratio calls in the zero-duty control, moved to cold numerical preparation | 0.03674296875 |
| G to L, four complete source checks including their nested world checks | 0.0128595703125 |
| D to D, three other complete world comparison leaves | 0.00478115234375 |
| Total | **0.0809779296875** |

The rounded user foundation 0.063337 gives 0.08097772265625. Either clears 0.080.
Do not add L to D again or divide mixed scopes by call count. This is gross old-work
avoidability, not measured net savings; new guards/cold storage and instrumentation effects
are not assigned invented timings. Runtime ceilings remain separate and unchanged.

## Implementation and bounded execution

Existing PoweredFlightStorage, resource in-phase readers, powered-world input checks and
cold numerical projection owner are the implementation surface. No banked free-flight
equations, solver, callbacks, step dt, geometry, publication writes or exact resource math
change. Added retained state is fixed-size fields only; no hot heap objects.

First compile and run affected correctness/lifetime/mapper/authority/allocation checks in
Debug and Release. Then run exactly three fresh Release control processes, 128 warm and
1024 measured complete operations each (existing cold priming retained). Stop at the first
failed metric. Only all three passes permit the parent's deferred qualification sequence.
No second correction, profiler, threshold change or retry-to-green is authorized.
