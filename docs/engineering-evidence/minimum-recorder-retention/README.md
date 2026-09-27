# Minimum recorder runtime retention

Status: CPU qualification complete when `qualification.json` and `preservation.json`
both report PASS. Unbanked. No GPU exposure is authorized by this package.

## What happened / retention owner

`RuntimeRetention` owns only `%LOCALAPPDATA%\NovaCore\MinimumRecorder`.
Its public deletion API accepts no root or path. Session targets are GUIDs beneath
that fixed root; test access is restricted to a fresh GUID namespace below the
same root. No build tree, sealed evidence, KSA, or external cleanup target exists.
An independent hidden helper runs after observer disposal/exit. It never runs
inside rendering or journal persistence. Failed cleanup preserves evidence and
reports a warning. The former 16-directory admission veto is removed: accumulated
forensic evidence must not make recorder startup depend on retirement success.
Mandatory durable-storage initialization remains required.

## Eligibility and newest-two authority

Recovery is reconstructed from the binary journal and compared with the complete
reported recovery object. COMPLETE requires the sealed final close, final produced
and durable equality, valid checkpoint/head, no corruption, faults, drops, open
operations or pending submission/context. This alone is insufficient for deletion.
The exact expected file set, schema/session identity, finalization hashes and both
process incarnations must also validate; both owners must have exited. Unknown
content, missing metadata, legacy ownership, unreadable state or any ambiguity
preserves the directory.

Eligible sessions are ordered by UTC origin plus the durable final close's QPC
offset, verified against the finalized close metadata. Filesystem times are never
used. Keep the newest two eligible unpinned sessions; GUID order breaks ties.
Existing legacy COMPLETE sessions receive no invented closure provenance.

## Pin / concurrency / failure safety

`NovaCore.Recorder.exe pin-runtime <32-character-session-guid>` creates and flushes
`preserve.pin`. Its presence protects, even if malformed. A pin is acknowledged
only after acquiring the shared cleanup/pin mutex and persisting the file; a busy
owner requires retry. There is no automatic unpin or abnormal-retirement policy.

Producer and observer hold lifetime leases that deny deletion. Process IDs include
start times; access ambiguity preserves. Cleanup locks ancestors against rename,
rejects reparse points and hard links, revalidates under exact-file locks, then
retires the whole directory transactionally. No recursive-delete fallback exists.
Windows transactional filesystem support is optional: unavailable support means
preserve/report, never failure of recording. This is a platform limitation, not
permission to replace atomic retirement with partial deletion.

## Abnormal storage / accounting

Any abnormal or ambiguous session produces a visible storage warning on the next
player startup. Missing or stale status also warns. No abnormal deletion threshold
is introduced. Reports include clean, abnormal-protected, pinned and total bytes,
session IDs and reasons, other retained bytes and incomplete-accounting markers.
Clean disposition and ownership ambiguity are overlapping dimensions: the legacy
COMPLETE session counts as clean **and** abnormal-protected. Do not add those
columns to infer total bytes. Reparse targets are never traversed for accounting.

## Qualification / red team

Permanent `NovaCore.Retention.Tests`: 198 checks per configuration. Independent
review attacked active owners, concurrent startup, stale identity, actual durable
fault/drop/incomplete-close records, malformed/contradictory reports, manipulated
filesystem timestamps, late pins, junctions/ancestor rename, transaction faults
and killed cleanup. Survivor byte hashes were compared. Result: PASS.

Existing minimum-recorder durability suite: 8,524 checks per configuration.
Its cleanup now waits for the exact observer process to exit, because recovery
publication precedes release of the new lifetime lease. Native CPU mocks require
observer metadata publication after journal directory creation and before Ready;
that initialization order was corrected. These superseded failures and their
causes remain recorded here. No GPU conclusion is inferred from CPU mocks.

See `qualification.json` for final builds/regressions, `runtime-before-after.json`
for actual retained sessions and hashes, `identity.json` for the final candidate,
and `preservation.json` for entry-state protection. Reproduce with `reproduce.ps1`.
Large test journals are regeneratable; real runtime journals remain in place.

## Disposition

PASS only with all final evidence checks passing. STOP FOR PROJECT CONTROL.
No commit, tag, push, banking, milestone promotion, blackout reproduction or
Surface Recontact. This managed retention revision supersedes the benign retry's
source/package seal; that run remains historical evidence for its exact candidate.
