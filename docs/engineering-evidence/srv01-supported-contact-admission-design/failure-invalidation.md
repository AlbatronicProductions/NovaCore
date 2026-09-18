# Future refusal, retry and terminal contract

This is a testable design; no failures were injected or tests run in this ticket.
The relevant snapshot includes canonical assembly/stores/properties/actuals,
clock/debt, revisions, history, events, private frontier/pending receipt and
native ownership. Preflight failures below make no changes to that snapshot.
Previously accepted host credit or earlier publications in the service call are
not rolled back: report their exact committed count/accounting separately.

| Case | Result/admission | Mutation, retry, invalidation |
|---|---|---|
| Foreign engine/assembly/launch/part profile | InvalidAuthority | Before native/canonical mutation; valid owner may still proceed |
| Unsupported design/shape, mismatched child mapping, nonfinite geometry, unrepresentable projection | OutsideProfile | Cold refusal; unwind only newly allocated resources; fix cold input, no installed owner |
| Stale StateRevision/TimelineRevision or complete paired state | StaleSource | Before step/commit; do not silently rebind; continuation unavailable until explicit terminal retirement |
| Stale stores/ResourceRevision, altered mass/COM/tensor, command/gimbal/actuator revision | StaleSource | Same, including numerically identical values with wrong revision; contact cannot acknowledge external edits |
| Invalid force/frame/surface/source/end or property generation | InvalidSource | Before step; no fallback to box/free flight |
| Wrong world/body/shape handle or generation; disposed world | InvalidReceipt/WorldUnavailable | Before use; no lookup through stale handle, no reconstruction; valid unrelated world unaffected |
| Wrong thread or reentrant call | WrongOwnerThread/Reentrant | No mutation; existing owner can proceed after call returns |
| Negative/overflow host duration, invalid debt arithmetic, unsupported rate/pause | InvalidInput/Overflow/UnsupportedClock | No credit/native write; input identity not consumed |
| Zero host credit | NoWork | No credit sequence advancement; may service existing debt separately |
| Duplicate/stale host sequence | InvalidSequence | No credit repetition; only next sequence accepted |
| Outstanding unpublished endpoint | OutstandingProposal | Refuse new credit/step; retain valid receipt for explicit publication retry |
| Insufficient debt / no full interval | AwaitingDebt | No step/publication; accepted earlier debt retained |
| Event before or exactly at target | PendingEvent | Before step when known; no event consumed/reordered. If event appears after pending endpoint, stale authority prevents unsafe commit; no automatic re-step |
| History/credit capacity, revision/frontier arithmetic overflow | HistoryCapacity/Overflow | Before step/credit; no partial mutation. Immutable capacity is terminal for this bounded episode; no silent hot resize |
| Source end reached | Completed | Hold copied endpoint; no further solve/debt spending/history growth; no free-flight handoff |
| Nonzero main/RCS request or gimbal edit | OutsideProfile | Before step/resources; first slice is deliberately unpowered |
| Default/fabricated/foreign/old/consumed proposal, mismatched staged target | InvalidProposal | No revision/debt/history change; duplicate cannot republish |
| Deliberate ordinary precommit refusal with valid pending endpoint | PreparationRefused | Native already advanced once, canonical unchanged; retry same receipt once checks pass, no double solve/debit |
| Cancellation of native pending endpoint | Retired/Invalidated | Canonical unchanged; native continuation terminal. Pure free-flight abort remains reusable as today |
| Solver/export/precision/coverage failure after native mutation | Invalidated | Canonical unchanged for that interval; private world poisoned; no second solve or free-flight fallback |
| Physical support/domain check fails | OutsideProfile + Invalidated | Preserve last canonical endpoint; no physical clamp/false support publication, no automatic removal/transfer |
| Canonical physical commit succeeds, private acknowledgement fails | CanonicalCommittedPrivateInvalidated | New endpoint/revision/history/time/debt authoritative; copied committed observation available; no rollback, retry or reconstruction |
| Canonical host credit succeeds, private accounting acknowledgement fails | CanonicalCommittedPrivateInvalidated | Debt and canonical sequence authoritative; physical state unchanged; no duplicate credit on retry |
| Contact passed to runtime/2 save/restore | UnsupportedConsumer | Refuse before writing misleading snapshot or replay; free-flight API remains unchanged |

Invalid source does not mean delete the canonical spacecraft. Native cleanup is
explicit owner lifecycle work. History remains deterministic; no native capability
escapes in failure results. Engine public ordinary clock mutations are not silently
accepted: final exact source checks remain mandatory even when private immutable
facts are safely reused.
