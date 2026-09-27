# Final bounded runtime storage contract

Project Control's final policy and expected-recording clarification supersede the
earlier 325 MiB ordinary allowance and exempt critical raw growth. This is a
storage/admission correction to temporary qualification instrumentation, not a
recorder, renderer or physics redesign.

## Admission and accounting

The only production maintenance root is
`%LOCALAPPDATA%\NovaCore\MinimumRecorder`. Every file counts. Admission and
maintenance share one fixed-root mutex and anchored path capabilities. Startup
waits up to 30 seconds for an existing maintenance owner; contention is not
misreported as proven capacity exhaustion. No cleanup runs on renderer,
simulation or persistence critical paths.

| Charge | Bytes |
|---|---:|
| Two fixed journal banks | 83,894,272 |
| Two checkpoint/head pages | 8,192 |
| Session metadata maximum | 1,048,576 |
| Recovery report maximum | 2,097,152 |
| Six small metadata files, 4,096 each | 24,576 |
| Observer failure text maximum | 65,536 |
| **Maximum complete new session** | **87,138,304** |
| Root-control publication reserve | 1,048,576 |
| Preventive maintenance threshold | 419,430,400 |
| **Hard total runtime cap** | **536,870,912** |

Known session filenames have enforced bounds before opening/writing. Zero-length
ownership/admission locks add no payload. Retained sessions remain charged at
`max(actual bytes, maximum session bytes)` until whole-session retirement, even
after process exit. This conservative promise prevents exit-time metadata races.
New admission requires all existing actual bytes, outstanding promises, a complete
new session and control-publication headroom to fit. Capsule/index publication
also charges old and new bytes during replacement, not only final size.

The expected startup is maintenance → reservation → MinimumRecorder → game.
Clean ordinary sessions are retired first. Older ordinary/critical raw changes
representation only after a bounded capsule is flushed, reread, checksummed and
recovered through the same journal parser. Transactions replace complete session
directories atomically; rollback/process interruption preserves the whole original.
No retained session loses individual banks, journals or heads.

Newest useful raw is a preference: it does not keep a safely retireable ordinary
session at the cost of routine recording. Newest critical/pinned raw and explicit
`raw-preserve.json` remain protected. Unknown files, active owners, invalid parser
authority, incompressible/oversized capsules or uncertain filesystem ownership
remain untouched. PIN preserves evidence; older pinned bytes may be preserved
losslessly in a validated capsule without changing criticality or pin identity.

## Capsule and index semantics

Strong schema-2 capsules retain **every original session byte**, including inactive
banks, uncommitted tails, pin metadata and a contradictory old report. Their
manifest preserves original hashes, authoritative epochs and reconstructed
recovery. Recovery happens in RAM; no expanded raw copy is written under runtime.

Only automatically classified ORDINARY, unpinned, fault-free, drop-free,
uncorrupted, reconciled sessions with no pending/open/live-resource authority may
enter the ordinary rolling index. Protocol faults, manual reclassification,
contradictory reports, missing terminal windows and exceptional RAW-PRESERVE stay
lossless. This is narrower than ordinary raw retirement eligibility.

The index retains the newest 64 exact summary records inside 8 MiB. Each has the
original identity/epoch, capsule and raw-file hashes, original authority metadata,
exact encoded recovered checkpoint, identical recovery report and up to 64
original contiguous committed terminal records ending at durable close. The
normal checkpoint parser and termination classifier validate reconstruction;
terminal events must agree with retained checkpoint events. No synthetic original
journal or close is manufactured.

Older **routine summary entries deliberately age out** into a checksummed
count/epoch/digest-chain receipt. That receipt is not reconstructible historical
evidence and is not described as lossless archival. Critical/pinned/ambiguous or
fault-bearing capsules never enter this path. A summary exceeding the per-entry
bound stays in its lossless capsule. Index replacement and eligible capsule
retirement form one transaction; persistence or validation failure deletes neither.

## Exceptional fallback and scope

After eligible retirement/consolidation is exhausted, evidence whose uniquely
required bytes cannot safely be retired is preserved. The player may launch with
`RECORDER STORAGE EXHAUSTED / MAINTENANCE REQUIRED` and an explicit unrecorded
warning. That branch does not configure recorder coverage and cannot start a
recorder-required engineering qualification. Neither a busy maintenance owner nor
the newest-ordinary preference authorizes that fallback.

The inherited store was already above the new cap. Its one-time remediation uses
strictly reducing atomic raw-to-lossless-capsule replacements. It is reported as
an inherited over-cap bootstrap, not as a previously cap-compliant state. Normal
operation has the stricter before-write transient reservation check.

No external evidence promotion, KSA access, native/GPU run, render-quality change,
physics change, commit, tag, push, milestone acceptance or bank is authorized here.
All raw/capsule/index runtime data and bulk generated logs remain local-only.
