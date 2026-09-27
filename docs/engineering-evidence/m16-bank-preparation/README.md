# NovaCore M16.0 — Unified Modular Spaceflight Baseline

2026-09-27. **BANK PREP — REVISE: authorized cleanup was blocked by tool policy.**
The engineering candidate remains **FROZEN / UNBANKED**. Project Control accepts
bank preparation, not an automatic commit/tag/push or Player/public PASS.

## Disposition

No production code, permanent test, runtime package, Git index/history/ref or
recorder-runtime content changed. No rebuild or NovaCore/GPU launch occurred.
Only current-facing documentation, defensive ignore policy and this concise bank
inventory were prepared. All frozen source/test/package hashes match the accepted
[bounded-storage candidate](../minimum-recorder-bounded-storage/candidate-identity.json).

One bounded cleanup inventory identified four safe targets totaling
**1,190,276,823 bytes**. The exact PowerShell deletion command was rejected by
automatic approval review with `blocked by policy`; no target was deleted and
no alternate deletion mechanism was attempted. **Reclaimed: 0 bytes.** Cleanup
must be resolved or explicitly deferred by Project Control before declaring this
bank preparation complete. This is a tool-policy blocker, not a candidate defect.

[cleanup.json](cleanup.json) records exact targets, classifications, before/after
scope accounting and preserved/deferred paths. Per-file target hashes and the
full entry inventory remain local at `build/m16-bank-preparation/`.

## Accepted outcomes and explicit limits

Project Control accepts the unified application, modular construction and
save/load, scalable RCS/support, canonical Florida pad, truthful readiness/launch
feedback, launch/flight, physical terrain/pad Surface Recontact, grounded commands,
powered relaunch, KSA-aligned contact, swept-clearance/post-contact authority,
Stage A CPU/contact performance, temporary mandatory qualification recorder,
bounded rolling recorder storage, Debug/Release/permanent regressions and package
preservation. This report preserves that acceptance; it does not rerun GPU work.

**OPEN:** stable 150 FPS / 6.6667 ms; detailed terrain/material draw remains the
GPU owner with roughly 3 ms recovery needed. No sufficient output-preserving
bounded GPU correction was proven. Historical blackout cause is **UNRESOLVED**;
Player/public PASS is **UNASSIGNED**. Permanent public instrumentation disposition
is **UNDECIDED: KEEP / DEV-ONLY / RETIRE**. Physical flight warp, full orbital
return and destructive-impact behavior remain outside accepted scope. See
[known limits](../../KNOWN_LIMITATIONS.md).

Historical final native measurements, not a new run (saved 3440×1440 configuration,
logged 3440×1322 viewport):

| Owner (ms) | Median | P95 | P99 | Max |
|---|---:|---:|---:|---:|
| Whole frame | 10.6952 | 16.2504 | 31.0350 | 1056.4140 |
| GPU | 8.90160 | 9.73857 | 11.22792 | 11.45100 |
| Managed physical service | 0.6176 | 3.9623 | 12.1393 | 287.0025 |
| Grounded terrain work | 0.6354 | 0.8095 | 1.5045 | 2.2870 |

377/377 whole frames exceed budget; longest run 377. CPU/GPU overlap; these
columns must not be summed. The [native record](../performance-150fps/native-results.md)
retains fence scope, recurring tails and matched-workload Stage A attribution.

## Frozen identity and qualification

- Branch `main`; HEAD `8c189b28ce2a68f97de734d1589acb500c41fd99`.
- Origin `https://github.com/AlbatronicProductions/NovaCore.git` (unchanged).
- Index SHA-256 `83aa8c801a40ba244619029a424f13401247a6e10ec307691e0254c9770a809e`; no staged changes.
- Runtime source SHA-256 `4259af1a2f78f177081fb2733d6fa2e2c5021d0eabca52c3f49287aed5a52279` (744 files).
- Canonical EXE SHA-256 `4a14ff8b27d9bd4d42867b4179679f02afb9d6647b2aa07f05e74d8b59f581c8` (162,304 bytes).
- Package SHA-256 `fdcd32679eef0fddff52f549b10cf7409e5f228a660f478e5d7a81e1a204b31d`.
- Canonical package: `E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows`;
  **134 files / 66 shaders / 12,952,746 bytes**, read-only verification PASS.
- 251 permanent-test file identities preserved. Accepted final Debug/Release
  builds had zero warnings/errors, 22,708 recorder checks and 389 retention checks
  each, plus CPU-only startup/storage UI qualification. Earlier accepted product
  and regression evidence is linked from the [campaign report](../performance-150fps/bank-candidate.md).
- Local/remote exact `m16.0` absent at preparation. Verify again immediately before
  manual bank; existence is a hard stop, never a tag replacement authority.
- Linked detached RCS worktree at
  `C:\Users\Tyler\.codex\worktrees\rcs-scalability\NovaCore` is preserved,
  not retired or treated as canonical.

[verification.json](verification.json) contains final measured preservation and
package checks. [file-identities.json](file-identities.json) seals the exact
post-documentation stage bytes; its own SHA is reported separately to avoid a
self-referential hash. Runtime/source and package aggregate algorithms remain
those of the accepted candidate.

## Exact manual inventory

- [bank-inventory.json](bank-inventory.json): every intended add/modify/delete,
  excluded nonignored file, modified-not-banked file and local-only root.
- [stage-paths.txt](stage-paths.txt): exact repository-relative pathspecs; no broad add.
- [excluded-paths.txt](excluded-paths.txt): exact preserved nonignored exclusions.
- Existing tracked historical archives are inherited, unchanged and not newly
  promoted; their public-release review is explicitly deferred. Nothing in this
  engineering bank authorizes publishing raw evidence.

The six explicitly promoted preblender files are offline physical-authoring
inputs/generators required by the accepted DLV provenance and importer. This is
reproducibility selection, not flight qualification for that earlier study.
Calculator regeneration requires NumPy 2.3.5; importing the existing inputs uses
standard-library Python. Re-authoring the accepted DLV also requires the separately
retained Blender acceptance files/GLBs identified by its provenance. Normal build
and runtime use versioned catalogs and do not require Blender or KSA.

Small selected reports, source manifests, summary logs and representative visual
witnesses under engineering-evidence are deliberate concise preservation. Raw
journals/capsules/indexes, capture payloads, copied executables, dumps, full build
receipts and diagnostic generations are excluded. No general authority is granted
to stage every path mentioned by a historical evidence manifest.

## Recorder and publication

MinimumRecorder is temporary qualification infrastructure. Necessary source and
permanent tests are selected; heavy capture, fault injection, observers and analysis
remain developer-only responsibilities. Engineering bank does not authorize public
GitHub/player release or make recording permanent public-player architecture.

`%LOCALAPPDATA%\NovaCore\MinimumRecorder` is outside this cleanup and untouched.
Its last qualified disposition is **344,438,174 bytes**, four retained pinned raw
sessions, three lossless capsules, no index; reservation headroom **91,528,403 bytes**
after existing promises and one complete new session/control reserve. These are
prior sealed measurements, not a new runtime-store audit. Original overflow remains
INCOMPLETE despite equal Produced/Durable watermarks. Full disposition and exact
session identities remain in the [storage report](../minimum-recorder-bounded-storage/README.md).

## Reproduction and manual handoff

Use [Windows build instructions](../../build-windows.md), then
`python tools/verify-player-package.py --output build/player-package.json`.
Normal development/player entry is the canonical `NovaCore.exe`, without arguments;
`NovaCore.Launcher.exe` is the legacy engineering/scenario entry. Builds require
resolved Git LFS source and the manifest-authorized global/Florida terrain cache;
the package is repository-layout development output, not standalone distribution.
The [storage recipe](../minimum-recorder-bounded-storage/reproduce.md) and historical
product recipes reproduce accepted checks. GPU exposure is separately authorized;
**none is authorized by this bank-preparation report**.

After the cleanup blocker is resolved or explicitly deferred by Project Control,
use the separately reported file-identities SHA with:

```powershell
python docs/engineering-evidence/m16-bank-preparation/verify.py --manifest-sha256 <reported-sha256>
```

Only after successful verification and Project Control manual disposition, the
exact proposed staging command is:

```powershell
git --literal-pathspecs add --pathspec-from-file=docs/engineering-evidence/m16-bank-preparation/stage-paths.txt
```

This command was **not run**. Compare staged paths against the allowlist before
manual commit. Expected commit message:
`NovaCore M16.0: Bank unified modular spaceflight baseline`.
Intended tag: `m16.0`. No commit, tag or push was performed or automatically scheduled.

**STOP FOR PROJECT CONTROL.**
