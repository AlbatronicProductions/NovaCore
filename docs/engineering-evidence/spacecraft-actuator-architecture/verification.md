# Verification and reproduction

## Judgment

**PASS — architecture recommendation only.** Independent actuator red-team review approved all 18 requested attack areas after examining the written maps, authority contract and first-slice recommendation. No implementation, physical qualification or banking approval is implied.

The lead was the only evidence writer. Two independent read-only investigators traced current KSA engine/resource and controller/allocator paths. A third independent verifier inspected current NovaCore authority and selected KSA source, then reviewed the written proposal. No agent edited production or tests, built, profiled or ran qualification.

## Independent red team

| # | Attack | Disposition / required boundary |
|---|---|---|
| 1 | Requested vs realized | PASS: five separate values; conditional prepared observation is not committed activity |
| 2 | Engine ownership | PASS: Off/Enabled, activity and hardware/feed availability are distinct |
| 3 | Canonical timing | PASS: original lattice, closed boundary/frozen prefix, no display dt |
| 4 | Spool/throttle | PASS: explicit ideal instantaneous zero-min proportional model, no invented physical spool |
| 5 | Propellant ownership | PASS: one joint-feed canonical successor; preview reports required flow only |
| 6 | Exact exhaustion | PASS: mandatory later bounded internal-event qualification before finite-fuel application; existing event identity is not misrepresented as execution |
| 7 | Gimbal ownership | PASS: allocation requests, hardware realizes authored geometry/limits |
| 8 | RCS allocation | PASS: indexed physical contributions, explicit pulse semantics and residual; no unbounded optimizer |
| 9 | Controller/hardware | PASS: requested SAS torque is not hardware authority; controller successor remains separate |
| 10 | Force boundary | PASS: body interval model, named COM/frame, one consumer; no once-transformed constant-root-force shortcut |
| 11 | Failure atomicity | PASS: prepare all successors; commit together only with physics; terminal private failure preserves all canonical commits |
| 12 | Revisions | PASS: separate actuator authority; no revision/cursor advancement during preview |
| 13 | Regime independence | PASS: shared hardware law, separately qualified dynamics consumers |
| 14 | Authored data | PASS: physical facts separated from control policy and source bound |
| 15 | KSA convergence | PASS: installed/current-source provenance, history intent and unproven exhaustion/atomicity distinguished |
| 16 | Blender/render authority | PASS: metadata/presentation only; no fuel/force/clock authority |
| 17 | Premature contact coupling | PASS: existing retained-contact restrictions remain; no powered contact |
| 18 | First-slice size | PASS: bounded transition handoff plus one sealed engine proposal; no fictitious canonical hardware trajectory |

Strongest attack: a final requested snapshot loses intermediate same-E engine edges. Resolved by including preflighted same-owner fixed capture in the first slice, with independent capacity/backpressure, protected neutralization storage, separate committed/consumed cursors and detectable bypass gaps. Repeated drain/refill means the existing seven pending slots do not alone bound the captured batch.

Second strongest attack: a no-physics evaluator might advance real hardware/fuel. Resolved by explicitly prepared-only output and no canonical cursor/revision advancement. Private fixture chaining cannot establish a live committed actuator history.

Other review corrections: Close/prefix identity is mandatory; rotating body thrust is an interval model rather than constant root force; hardware availability differs from assumed feed; shared consumers cannot independently spend the same fuel; controller successor was explicitly removed from the hardware-state meaning. No remaining architectural blocker was reported.

## Source and evidence checks

- Current HEAD/main/origin/main/queried remote main: `c70ccfacf3607d1342396c702682781b42bb525e`.
- M14.22 annotated tag object: `e943a128ef425ff7427b917f2bb04056e58282f5`; peeled milestone commit: `899f11f727205340bdd550dd65a853e253de0ed3`.
- All **1,913 tracked files** unchanged: aggregate `5C01C78EB2EE22CDA6C3269DC58ACFBB4C443DF01BB80F58524D08CD796F762B` before and after.
- All **62 tag refs** unchanged: aggregate `917BD05E46D9EAB193EC32EC0A1E1CE0E7E304F850E60715965ADDB0642231D7` before and after.
- Pre-existing untracked `docs/engineering-evidence/bepu-local-contact-staging/staging-build-blocker.md` unchanged: `3DAB84C722F700945E25F6276ACDCA4874968CA39AD724EB82047D020D7056E7`.
- Zero staged files and zero unstaged tracked files; `git diff --check` PASS.
- Exact installed KSA binary and retained provenance matched. This package fingerprints 22 current NovaCore/sample source files and 37 actuator-relevant KSA source files. The engine investigator independently matched all 20 non-null prior control-map source fingerprints.
- Three selected official changelog entries reread; remaining narrow history links are explicitly retained earlier witnesses. No claims use history in place of current implementation.
- No code/test/dependency/Blender/render changes. No validation or performance campaign. All numeric performance/allocation bars are future qualification requirements, not new results.

The evidence package is untracked and bounded to 96 KiB. Only these eight files are retained: README, current-novacore-map, ksa-actuator-map, ksa-history-convergence, actuator-authority-contract, architecture-candidates, verification and identity. No disposable build tree or raw browser/runtime output was created. Existing scratch was neither duplicated nor deleted.

## Reproduction

Run from `E:\NovaCore`, read-only. Current source takes priority if identity changes; do not apply old line references to new bytes.

```powershell
git status --short
git rev-parse HEAD main origin/main
git ls-remote origin refs/heads/main
git rev-parse 'm14.22-canonical-spacecraft-command-authority^{}'
git diff --check
rg -n -i 'propulsion|mass.?flow|propellant|fuel|gimbal|RCS|actuator|engine.state|spool|control.surface' src samples -g '*.cs' -g '!**/bin/**' -g '!**/obj/**'
rg -n 'LastEngineRequest|EngineRequestSequence|ClosedThrough|CommitNext|CloseSpacecraftCommandBoundary' src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.SpacecraftCommands.cs
rg -n 'Force|Torque|Mass|Inertia|ChangedAuthority' src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactSource.cs src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs
```

Inspect the mapped methods, not just matches. Compare the installed KSA DLL and mapped source hashes in identity.json before interpreting its code. If cached decompilation is unavailable, regenerate privately from the exact installed/retained DLL using the established local decompilation workflow; retain summaries and hashes only. This report does not authorize acquiring alternate binaries or importing proprietary source into NovaCore.

Open the exact official links in the history table with access to the official live-changelog. Verify revision/date and content; do not export the channel. If unavailable, label retained history as retained evidence rather than claiming a fresh read.

Tracked-file aggregate algorithm: for each path in `git ls-files` order, form `path + space + decimal byte length + space + uppercase file SHA256`; join records with LF and no terminal LF; hash UTF-8 bytes with SHA256. Tag aggregate: join the exact `git show-ref --tags` output lines with LF/no terminal LF and hash UTF-8. identity.json records both snapshots. This compares working-tree bytes, including newline representation.

No benchmark, full-suite repetition or numerical hardware qualification is required to reproduce this architecture finding. Project Control must authorize the first implementation ticket before any engine code is added.
