# Independent retention red team

Reviewer: `/root/retention_red_team`, read-only. Production writer: `/root`.
Disposition: **PASS** for the final mechanism and permanent adversarial coverage.

The reviewer attempted to falsify fixed-root containment, positive cleanliness,
closed ownership, pin precedence, newest-two ordering, concurrency safety and
whole-directory failure safety. Both configurations pass 198 retention checks.

Covered attacks include actual durable faults/drops/incomplete close, inconsistent
recovery JSON, missing/duplicate metadata, process incarnation reuse and active
leases, concurrent CPU startup, filesystem-time inversion, session/ancestor
junctions, rename attempts, late pin creation, injected failure at three deletion
stages and killing the worker after staging a deletion. Full survivor hashes
remain equal. Transaction support is tested on this host; unsupported platforms
preserve instead of deleting. There is no recursive fallback.

Review also required removal of the old 16-session admission veto, complete root
accounting with explicit unmeasured entries, conservative legacy COMPLETE
classification, precise failed-retirement reasons and serialized status reporting.
These are included in the final candidate.

Regression findings were not hidden: fixture cleanup previously raced observer
exit, and the native mock exposed observer identity publication before directory
creation. The final fixes wait for the exact fixture observer incarnation and
publish observer identity after journal creation but before Ready. Independent
review approved these changes. Both final native CPU mocks pass with 35,303
produced/durable records; durability passes 8,524 checks per configuration.

Windows TxF is an optional retirement mechanism, never a recording dependency.
Microsoft advises considering alternatives because availability may change:
[transaction programming considerations](https://learn.microsoft.com/en-us/windows/win32/fileio/programming-considerations-for-transacted-fileio-).
For this bounded policy, losing that capability disables retirement and surfaces
a preservation warning. It does not justify partial deletion or wider authority.

No reviewer GPU launch, KSA write, runtime cleanup, source edit or Git publication.
The root writer's separately authorized real-root pass preserved all three
sessions and every original file hash; see `runtime-before-after.json`.
