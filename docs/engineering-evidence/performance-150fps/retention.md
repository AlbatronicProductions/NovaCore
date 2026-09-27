# MinimumRecorder storage authority and maintenance

**Historical policy:** superseded by the [final bounded-storage contract](../minimum-recorder-bounded-storage/README.md),
including exact current runtime disposition and candidate identities. This file
preserves the earlier qualification record, not current deletion/cap authority.

This is temporary developer/qualification infrastructure while blackout causality
remains unresolved, not accepted permanent public-player architecture. Required
source/permanent tests may support an M16.0 engineering bank; public release needs
Project Control's explicit KEEP / DEV-ONLY / RETIRE source decision. All runtime
raw sessions, capsules, dumps/captures and bulk forensic data are local-only, never
committed or public-packaged. Preservation and retirement policy below is unchanged.

Runtime root only: %LOCALAPPDATA%\NovaCore\MinimumRecorder. No cleanup API accepts
an arbitrary directory. Build output, engineering evidence and KSA are outside its
authority. No real incident has been reviewed, reclassified, unpinned or retired
by this campaign.

The ordinary-abnormal raw budget is 325 MiB (340,787,200 bytes). Critical, pinned,
active and unresolved-criticality evidence is exempt. Age, filesystem dates,
absence of a pin, pressure or capsule existence grant no retirement right.
The newest three useful ordinary abnormal raw sessions remain protected.

Automatic ordinary classification requires an intact sealed close and reconciled
durable sequence, no dropped/corrupted evidence, no serious recorder/device/error
evidence, no outstanding resource/submission/operation state, and a positively
observed normal exit of the exact producer incarnation. Recoverable Protocol-only
bookkeeping or an intentional unsuccessful close may be ordinary when all those
independent conditions hold. Missing provenance remains unresolved. COMPLETE alone
is insufficient, including for the existing clean-session retirement path.

The observer retains the actual producer process handle before validating its
incarnation. After journal disposal/report publication it waits at most one second
for actual exit and durably records observed facts. An unobserved exit stays
unresolved. This adds no renderer wait, producer I/O, Vulkan work or persistence
ACK dependency. Legacy sessions acquire no invented termination record. This is
not certification of physical monitor scanout: known display incidents stay pinned.

Above 325 MiB of positively ordinary abnormal raw, select the oldest eligible
session using validated recorder opening metadata. A lossless compact capsule
contains original raw files plus session/incarnation, timestamps, terminal state,
Produced/Durable, faults/drops/corruption, unmatched/pending, original heads and
checkpoint, resource/submission correlations, terminal event window, byte counts,
hashes, provenance, classification/review and reproduction metadata. Original
critical facts and uncertainty are never rewritten by a manual declaration.

The capsule is capped during compression at one journal event-window (8 MiB,
including SHA256 trailer). Incompressible evidence fails-preserve; nothing is
truncated to meet that cap. This bounds compact output, not all helper memory:
normal validation expands one bounded raw file at a time, including ~40 MiB banks.
The capsule is durably flushed, reopened/checksummed and reconstructed beneath a
fresh fixed-root staging capability. The UNCHANGED OrdinaryJournal parser must
reproduce the full recovery report and exact encoded checkpoint-state digest.
Missing automatic provenance remains absent. The same verified capsule handle is
leased through transactional raw retirement. All failures preserve raw. Compact
capsules are never deleted by raw cleanup.

Total-storage warning threshold: 512 MiB (536,870,912 bytes). The startup window
shows total, ordinary clean, ordinary abnormal raw, critical/pinned/unresolved raw,
capsules, active raw and other retained bytes. Categories reconcile without double
counting. SAFE MAINTENANCE invokes normal clean retention plus eligible ordinary
abnormal compaction. It cannot use total-storage pressure to delete protected data.
If protection leaves storage above the warning threshold, MANUAL REVIEW REQUIRED
lists identities, classifications, pins, sizes and preservation reasons.

Acknowledgement is bound to the displayed protected-survivor inventory. Unseen
protected changes refuse acknowledgement and refresh the view. An acknowledged
warning rearms only after an observed fall to/below 512 MiB and later recross, or
protected bytes grow by at least one retained event-window (8 MiB). Ordinary
session-ID/report/control churn does not rearm it. Malformed acknowledgements
fail open to warning, without granting deletion authority.

Manual actions are separate:
- KEEP establishes explicit pin protection.
- REVIEW / SEAL stores a bounded report and causal rationale bound to raw hashes.
- UNPIN removes only the explicit pin; base criticality is unchanged.
- RECLASSIFY AS ORDINARY requires a separate explicit action and a current sealed
  positive noncritical review. Base classification and original facts remain.
- RETIRE RAW still requires normal ownership, oldest/newest, budget and capsule
  eligibility; it cannot bypass them.

The UI initializes WinForms once before any maintenance window and reuses that
initialization for normal player startup. The CPU-only --qualify-storage-ui route
creates a maintenance HWND then reenters player initialization, without GPU or
runtime-root writes. The production recorder still starts before GPU startup.

Permanent tests cover automatic actual process exits, crash-after-close, GPU error
despite legacy COMPLETE, stale/missing provenance, review-only versus reclassify,
UNPIN semantics, stale raw review, over-budget protected survival, timestamp
tampering, newest-three, capsule write/flush/recovery/transaction failure cuts,
replacement denial, checksum/path/duplicate/size attacks, incompressible evidence,
normal-parser independent recovery, exact 512 MiB boundaries, warning suppression,
recross, protected growth, stale acknowledgement and safe-maintenance preservation.
Reproduce: pwsh -NoProfile -File tools/physics/qualify-recorder-maintenance.ps1.

Entry inventory under the corrected classification: 504,267,976 raw bytes:
251,918,824 pinned bytes and 252,349,152 legacy bytes with unresolved criticality.
All remain FULL KEEP. Positively ordinary abnormal raw at entry: 0 bytes; all
340,787,200 budget bytes remain available. Original native overflow evidence remains
INCOMPLETE (3,383 drops, 273 unmatched operations, 38 pending entries); equal
accepted Produced/Durable sequences never repair dropped events.

Final Debug/Release: 324 retention adversarial checks in each, 22,708 recorder
durability/fault/recovery checks in each, and CPU-only maintenance HWND integration
PASS in each. Independent read-only adversarial review PASS. Original recorder
wire/journal format, producer hot path and Vulkan behavior are unchanged.

After the single native witness, its COMPLETE raw session was explicitly pinned.
SAFE MAINTENANCE then ran against the actual runtime root, with no raw retirement,
no capsule creation and no failure. Snapshot total: 588,188,056 bytes; clean eligible
bytes 0; ordinary-abnormal raw 0; critical/pinned/unresolved raw 588,184,913;
capsules 0; active 0; retained control/report bytes 3,143. Status-report rewrite
can change control bytes afterward. The 325 MiB ordinary allowance is wholly
available; total exceeds 512 MiB by 51,317,144 bytes due to protected evidence.
MANUAL REVIEW REQUIRED is the correct outcome, not permission to reclaim raw.

| Session identity | Raw bytes | Pin | Effective classification / reason |
|---|---:|---|---|
| 11b82e566ff44eb2b3379c782fdd89c8 | 83,916,379 | yes | Critical/unresolved legacy provenance; explicit preserve |
| 5612981fe98946d6b9d51655112a9697 | 84,086,062 | yes | Original native recorder overflow incident; preserve full raw |
| b0a8e1482b15419a9b6ca5734428b404 | 83,916,383 | yes | Accepted recorder forensic witness; explicit preserve |
| bf47741ddda940b5bbeedaba9f3bea88 | 84,518,977 | no | Legacy unresolved criticality |
| c7de847a1f9b4ac389f61fe4a160517a | 83,915,867 | no | Legacy unresolved criticality despite complete recorder close |
| ea6841ccd91540c4b0ce3b0fc60b2b28 | 83,916,937 | yes | New positively ordinary clean witness, explicitly pinned |
| f967ea2abe4b4f55b5866bbfa2543a31 | 83,914,308 | no | Recovery metadata contradicts durable authority; preserve |

UNPIN of the new ordinary witness would only remove that explicit protection;
ordinary clean retention eligibility would still be evaluated independently.
UNPIN of the old critical/unresolved sessions cannot make them ordinary.

