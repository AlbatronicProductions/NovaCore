> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Independent verification and stop disposition

Two read-only reviewers independently checked equations, signed bookkeeping and payoff feasibility. A third read-only source reviewer inspected the actual ordered solver and tangent coupling. Reviewers did not modify candidate/evidence files or perform solver/world runs.

Overall: **REVISE for ticket completion**. The narrow cause is proven; one bounded recommendation is justified. The missing actual D payoff report prevents claiming complete phase-13 acceptance.

## Twenty required attacks

| # | Attack | Disposition | Evidence / limitation |
| --- | --- | --- | --- |
| 1 | Basis transport blamed again | PASS | Identical-basis measured failure retained; valid transport not blamed. |
| 2 | Duration normalization treated as exact prediction | PASS | Duration-only current-softness residual measured; dimensional scaling is distinguished from solution prediction. |
| 3 | Load removal double-counted | PASS | P load-only and successor load32/load0 comparisons match after one accepted shift. |
| 4 | Mass blamed without isolation | PASS | Mass-only normal error 8.450358618405929e-7; current inverse mass used. |
| 5 | Depth/bias ignored | PASS | Factorial signed normal contribution -7.236124344470185e-5. |
| 6 | Separating velocity ignored | PASS WITH LIMIT | Zero/actual/nominal-bound body-Y witnesses retained; point-speed overshoot at the nominal bound disclosed. |
| 7 | Source angular velocity ignored | PASS | Separate angular-only control has negligible effect here. |
| 8 | Lever/effective-response changes ignored | PASS | Geometry-only transported control passes; input/response matrices retained. |
| 9 | Tangent feedback missed | PASS | K_NT delta-tangent matches normal residual feedback within 8.67e-19; normal ordering contributes too. |
| 10 | Twist falsely blamed | PASS | Direct normal/twist coupling is zero here and remaining coupling is negligible. |
| 11 | Common correction sign wrong | PASS | D = guess - [q^T r/(q^T A q)] q removes excess common reaction; no duplicate load shift. |
| 12 | Stale softness | PASS | Current h/omega alpha and bias are recomputed per case. |
| 13 | Reference/equations changed across initializer arms | PASS | Initializer comparisons use the same current equations; four factorial A matrices/guesses are byte-identical. |
| 14 | D uses oracle solution | PASS | D is constructed from A/rhs/guess before the reference; no oracle parameter/call in initializer. |
| 15 | More sweeps adopted without cause | PASS | 12 is first sampled pass, not an untested minimum; no more-sweep recommendation or adoption. |
| 16 | Thresholds weakened | PASS | Physical bars remain 1e-4, proof bar 1e-12, positivity/friction checks retained; no clamp-as-repair. |
| 17 | Stacked causes lack payoff | LIMITED | One derived common residual class only; arithmetic payoff retained, actual D report missing. |
| 18 | Synthetic evidence unexplained | LIMITED | Same common finite-iteration mechanism; different artificial source history. Four predicted passes, two legitimate initial cap refusals. |
| 19 | Departure confused with convergence | PASS | Geometric support/separation admission and numerical accuracy remain distinct. |
| 20 | Complete coast qualification overclaimed | PASS SUBJECT TO REVISE | Overall REVISE; no coast installation or resumed prerequisite qualification. |

## Strongest surviving objection

The compiled [Payoff.cs](Payoff.cs) buffers results and writes only after all cases. The correct moderate-normal friction refusal occurs before that write. Thus earlier source-ordered coast/identical/tiny D calls did not leave measured output. The record must not relabel the subsequent independent equation prediction as a recovered measurement.

The exact exception was:
```text
System.InvalidOperationException: diagnostic current friction caps, no clamp
  at CoastPayoff.Check ... Payoff.cs:17
  at CoastPayoff.RemoveCommonResidual ... Payoff.cs:33
  at CoastPayoff.Run ... Payoff.cs:44
  at CoastPayoff.Main ... Payoff.cs:76
```
No diagnostic-payoff.json exists. One payoff process; no retry. The guard is numerical feasibility, not a permission failure.

Arithmetic audit independently verifies:
- the homogeneous map matches retained A iterates;
- original coast D is derived from current residual before reference use;
- every reconstructed normal update remains positive;
- tangent/twist remain interior after each update;
- original coast predicted error approximately (5.069e-9 N s, 6.810e-9 m/s, 1.357e-8 rad/s);
- moderate-normal/combined-normal-tangent D tangent magnitude 0.09002167204944703 exceeds cap 0.08654382766644907.

A full constrained-domain correction is not established. Do not add a clamp or second correction to rescue those arms.

## Comparison limitations

The cold eight-sweep arm activates one tangent clamp; the unclamped homogeneous proof is not applied to that arm. All accepted-coast curve and 17 controlled witnesses remain unclamped. Synthetic arithmetic propagation is permitted only after initial cap checks and branch verification.

The nominal body-Y-bound control includes fixed angular velocity. Its maximum point speed is slightly above the admitted point-speed bound; it is a near-bound sensitivity witness only.

Same-build captured-system determinism is shown. No new cross-platform, world-continuation, tiny-duration, off-COM, selector, allocation or performance qualification is claimed.

## Preservation / reporting checks

Final byte/ref/status checks are recorded in [identity.json](identity.json). No production, prior diagnostic, permanent test, prior evidence or Blender bytes are modified. The retained numerical JSON copies match their original output hashes. Only this new coast-accuracy evidence directory is added this ticket.

The reproduction wrapper's later already-attempted-project guard prevents retry after an aborted payoff. This is a safety/reporting guard only; the failed Payoff.cs and its numerical rules remain unchanged.

STOP FOR PROJECT CONTROL. UNBANKED.
