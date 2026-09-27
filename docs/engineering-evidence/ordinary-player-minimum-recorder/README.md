# Mandatory ordinary-player minimum recorder

Current runtime retention policy: [minimum recorder retention](../minimum-recorder-retention/README.md).
That managed revision supersedes the benign candidate's source/package identity;
historical run evidence remains unchanged. The original directory-count admission
veto below no longer applies. Only positively clean, closed, unpinned sessions can
retire; abnormal and ambiguous evidence is preserved.

Current bounded correction and single benign GPU retry: see
[benign revision](benign-revision/README.md). Its separate identity supersedes the
original candidate below. Clean, sealed, fully durable sessions can now report
COMPLETE; interrupted/unproven sessions retain terminal uncertainty. The original
manifests and first-run report remain historical evidence, not the current seal.

The following records the original offline candidate and its original contract.

Project Control accepted **durable prefix + explicit terminal uncertainty**. This
package implements that decision in the canonical unified application. It does
not replace the earlier feasibility report or diagnose the blackout.

Status: **offline qualification; FROZEN / UNBANKED only when identity.json and
qualification.json both say PASS**. Blackout cause unresolved. Player acceptance,
ordinary application relaunch, GPU exposure and banking remain on hold.

## Ownership and startup

`NovaCore.App/Program.cs` starts `NovaCore.Recorder.exe` and registers the shared
mapping with the native minimum producer before `PlayerApplication.Run`. Missing
helper, stale mapping, unavailable storage, quota exhaustion or failed native
registration refuses the session through the existing visible error dialog.
No `NOVACORE_CAUSAL_MAPPING` flag is required. The legacy heavyweight recorder is
unchanged and remains optional; minimum enablement never sets `causal.Active()`.

The producer owns a pre-touched 2 MiB ring of 8,192 fixed 256-byte records, a 4 KiB
header, a heap-preallocated incarnation table and 64 pending submission slots.
It attempts a single atomic producer lease; contention/full capacity refuses the
record, increments a sticky loss count and returns immediately. There is no disk
access, allocation or persistence wait in the native emit path. Hash deletion
backshifts live entries rather than accumulating retirement tombstones.

The independent process copies at most 15 records, commits them and releases those
slots. All journal work, checkpoints, recovery and summaries belong to this
process. Failure ends this persistence owner; it cannot resume publication from
speculative state. Producer progress can continue, with explicit coverage faults.

## Produced versus durable

`ProducedSequence` is the newest record accepted into volatile ownership.
`DurableSequence` is the contiguous prefix acknowledged **after** the bank data
and alternate head each complete `FileStream.Flush(flushToDisk: true)` (Windows
`FlushFileBuffers`). Cache visibility or an enqueued flush is never an ACK.
The storage platform must honor that primitive; hardware dishonesty is not a
software durability guarantee. A complete recovered head can survive a process
termination that occurred before its volatile ACK was published.

After restart, recovery validates committed authority and reports its strongest
contiguous valid prefix, stopping at the first missing/damaged record. It may
salvage complete records in a torn final page. Uncommitted tail bytes are ignored.
It always states **TERMINAL SUFFIX UNCERTAIN after DurableSequence**. An absent
return means *return not retained*, never proof that the call did not return.
Producer watermarks/loss flags in an unpersisted suffix may themselves be lost.

## Journal and checkpoint authority

There are two fixed banks (32 MiB checkpoint area + 8 MiB event area + 4 KiB
metadata each) and two 4 KiB head slots: **83,902,464 binary bytes/session**.
Records occupy separately appended 4 KiB pages; committing never rewrites an
earlier committed event page. A head contains session UUID, monotonic commit
revision, bank incarnation, checkpoint sequence/digest and committed page/sequence
range. Digests/record checksums detect torn or corrupt data. Equal revision with
different heads is an explicit ambiguity refusal.

Rotation writes the other bank's checkpoint from exactly the previously committed
state, flushes it, then flushes its head before acknowledgement. Only then can the
old bank be reused. Sequence numbers never restart. A damaged newer head/bank
cannot silently make an older recovery look clean.

The binary checkpoint bounds two 8,192-entry resource ledgers (birth, binding,
mapping, retirement), 1,024 open operations/recordings, 64 pending submissions with
up to 128 roles each, and 21 last-phase records. The 32 MiB bound exceeds this
encoded maximum. Capacity faults are explicit; encode and decode enforce matching
bounds. Session admission is serialized before GPU startup and capped at 16
retained sessions. Nothing is automatically deleted. Journal binary ceiling for
16 sessions is 1.25 GiB; compact summaries/manifests are additional bounded files.

## What evidence proves

Each event identifies session (journal/mapping), monotonic sequence, QPC time,
CPU-loop ordinal, terrain ordinal, thread, entry/return/exception, operation and
submission identities, generations and publication. CPU-loop ordinal is separate
from the existing production terrain counter. Update, Draw, command begin/end,
acquire, submit, fence waits, positive completion, present, idle, errors and relevant
resource operations are observed at the actual call boundaries.

Successful submissions retain command allocation/recording incarnation, fence,
queue, resource context and pupil/topology identity. Completion is emitted only
for saved successful submissions proved by the existing fence/queue/device wait.
An initially signalled fence, skipped out-of-date acquisition or failed submit
cannot manufacture completion. Present return **does not prove scanout**.

Identity uses resource kind + raw handle + monotonic birth. Reusing a handle cannot
reuse its birth. Birth, binding, mapping and retirement survive rollover, including
retired resources still in the submission ledger. Roles track the actual ordinary
descriptor inputs and fixed mesh buffers. The complete live-at-submit ledger also
retains other resources conservatively: membership is **not asserted shader
access**. Per-pending-submission role maps preserve association. Incoming admission,
topology publication and pupil physical/scratch swaps are separately recorded.

## Faults and limits

Overflow, producer contention, identity capacity, I/O failure, corruption, protocol
failure and observer unresponsiveness have separate flags. A five-second stale
observer heartbeat means unresponsive, not proof of process death. The helper
attempts a flushed failure sidecar on I/O failure; a failed volume may prevent
that too. Reports expose known faults through the recovered prefix and never
claim the final volatile fault state is known. Ordinary sessions are preserved in
`%LOCALAPPDATA%/NovaCore/MinimumRecorder/<session-id>`, including pre-GPU executable
hashes/session/process identity. After 16 sessions, preserve/review this material
before reclaiming it; silently dropping old unclean evidence is forbidden.

This qualification executes no Vulkan loader/driver operations. The C++ fixture
includes the exact production wrappers with CPU fakes generated from public Vulkan
declarations, seeds their resource footprint and checks raw forwarding counts.
It covers current production's one-command-buffer submit route, bootstrap queue
completion and the ordinary frame loop. GPU timing, actual device reset behavior,
visual product acceptance and hardware performance are **not** established.

See [qualification.json](qualification.json), [performance.json](performance.json),
[identity.json](identity.json), [red-team.md](red-team.md) and
[reproduce.ps1](reproduce.ps1). Reports are summaries; permanent source tests and
the binary schemas are the reproduction authority. No commit, tag or push.

Next cheapest proof is a separately authorized, bounded benign GPU run of this
exact frozen canonical package. No such authorization is implied here.
