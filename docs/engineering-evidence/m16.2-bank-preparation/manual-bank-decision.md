# M16.2 manual bank decision

Project Control explicitly elects to bank **NovaCore M16.2 — Player Entry,
Fullscreen Viewport & UI Architecture Convergence** manually, with open
qualification debt. Tag: `m16.2`. Parent: `m16.1` /
`40314c0f72396ea5f4ff39121f0b6821f1f96146`.

Accepted: Configuration → Loading → Gameplay; fullscreen viewport; overlay/menu
architecture; Vehicle Editor presentation and ownership; Exploration 0.1× ↔
authoritative pause; README refresh; existing construction/save/load/launch routes.

This decision supersedes the prior requirement to close every listed qualification
gate before banking. It does not certify those gates, repair runtime/test code,
or establish that publication has occurred. The original report and receipts remain
historical evidence. The new `manual-precheck.json` records the final build,
unchanged production inputs, current inventory and observed Git state.

Open debt is recorded verbatim in scope in [manual-tag-message.txt](manual-tag-message.txt):
69-versus-66 shader inventory; inherited Graphics assertions/path contracts;
native facility qualification; full Graphics Release not completed; non-96-DPI
not performed; 150 FPS whole-frame target; cold/global terrain fallback;
planetary fidelity; historical blackout causality; public-release readiness.

The assistant performs prechecks and the Release build only. Project Control
manually stages, inspects, commits, tags and pushes. Stop on build failure,
unexpected production diff, new runtime defect or blackout/system anomaly.
No fresh runtime launch is required by this decision or claimed by this precheck.

Final precheck: **PASS WITH RECORDED OPEN DEBT**. Release native configure/build,
solution rebuild and canonical App rebuild all returned zero; managed builds
reported zero warnings/errors. All 1,079 frozen inputs match. The index is empty;
the new manual allowlist includes README/docs. Parent/HEAD and 72 tag refs are
unchanged; remote main still matches the parent and `m16.2` is absent.

All package bytes except one generated static-web endpoint manifest match the
prior receipt. That manifest differs only in gzip `Last-Modified` headers:
substituting the prior build timestamp reconstructs its exact prior SHA-256.
The receipt records the new package hash. Package verification retains precisely
the accepted three-extra-shader failure; no qualification debt is relabelled PASS.

After the precheck succeeds, use these commands from `E:\NovaCore` in PowerShell
or Git Bash. Inspect the staged paths/diff before proceeding. The intended list
includes README/docs and excludes build outputs, caches, recorder data, KSA files
and player saves. A later production change invalidates the recorded precheck.

```sh
git add --pathspec-from-file=docs/engineering-evidence/m16.2-bank-preparation/manual-stage-paths.txt
git diff --cached --name-status
git diff --cached --check
git diff --cached --stat
git commit -m "NovaCore M16.2: Player Entry, Fullscreen Viewport & UI Architecture Convergence"
git tag -a m16.2 -F docs/engineering-evidence/m16.2-bank-preparation/manual-tag-message.txt
git push --atomic origin main refs/tags/m16.2
```

The commit/tag/push commands above are a manual handoff, not an execution receipt.
Use normal fast-forward publication only; do not force-push or overwrite a remote
change. Confirm the branch and tag push results before describing M16.2 as remotely banked.
