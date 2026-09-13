# Ephemeral hosted consumption qualification

Lead judgment: **REVISE**.

**VERIFIED BEPU BINARY CONSUMPTION — FOLLOW-UP REQUIRED**. UNBANKED.

## Production baseline and original candidate

Original branch `codex/bepu-local-contact-staging`; HEAD/main/origin/main remain
`3933d832b73511a8d08468b7b27a9b2820e2f8ee`. Nothing staged. Original tracked diff
remains 3 files, +19/-1. No existing production or permanent test implementation
changed. Solver: **NOT STARTED**. Added runtime frame work remains ZERO.

## Temporary branch and commit

Created only `codex/validation-bepu-binary-consumption-ci-20260913`, commit
`2063928ef89ce79b12bf6f1f1184619c59ee8547`, parent the production baseline.
A separate Git index and raw blob insertion preserved the canonical working files
and index without checking out another branch or copying a worktree.

The snapshot contains 17 changed/new files: attributes, solution, Simulation
project, root targets, the unchanged workflow, dependency smoke project/source,
and all ten files under `external/bepu`. No trust diagnostics, scratch, unrelated
evidence or solver code were included. All required input bytes match the local
candidate; exact SHA-256 and Git blob IDs are in `ephemeral-ci-results.json`.

Raw diff: 17 files, +686/-133. Preserving the local solution's CRLF bytes creates
a line-ending-only difference from its stored baseline. Ignoring line-end endings:
17 files, +554/-1. No local file normalization was performed.

Snapshot tree: `dab7526e517339046ca07e017253b660c6d96806`.
Workflow SHA-256: `0958D384A7B372CC68DF39201F9D03524182C451D3F21C382C3D0FB34298F718`.

## Hosted Actions result

[Run 34739828038](https://github.com/AlbatronicProductions/NovaCore/actions/runs/34739828038)
failed during workflow validation, before job scheduling. It records the temporary
commit above, event `push`, attempt 1. Created/started/updated timestamps all read
2026-09-13T05:14:58Z; the UI reports no execution duration. Do not interpret the
same-second metadata as a measured zero-duration runner job.

Exact annotation at `.github/workflows/verified-bepu.yml`, line 24, column 23:

> Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp

The job-level environment expression cannot use that context at that location.
Classification: **A. workflow/mechanical transport failure**. No architecture
contradiction was observed, but hosted reproducibility is still unqualified.

Jobs: **0**. Runner OS/image: **UNAVAILABLE**. Hosted checkout, package hash gates,
DLL/output hashes, build, smoke and publishing: **NOT REACHED**. There is one
workflow-validation error; no runner/compiler warning/error result exists.
Local qualified package/DLL hashes are preserved in the exact snapshot, not
misrepresented as observed hosted outputs.

## Trigger and retirement

A temporary [draft PR #1](https://github.com/AlbatronicProductions/NovaCore/pull/1)
was opened to use the prepared pull-request trigger without changing the workflow.
On first status inspection, GitHub had already recorded its workflow-validation
failure for the push. Only one run exists for this snapshot. No retry, edit,
dispatch rescue or production correction followed.

PR #1 was closed without merge. Remote validation branch deletion succeeded and
was independently verified absent; only remote main remains. Local validation
branch was deleted with an expected-SHA guard. No temporary worktree existed.
The historical commit and run remain witnesses. No tags were created or moved.
Neither main nor the original development branch was pushed.

## Original candidate preservation

After retirement, all **1,812 pre-existing tracked/untracked files** recorded before
snapshot creation still matched their SHA-256 values. This includes all original
candidate files and evidence. Canonical staged set remains empty; HEAD/main/
origin/main retain the baseline. This pass adds only concise result/report files
to canonical engineering evidence. Git diff check passes.

## Cleanup

The final filesystem check found all three previously approved validation trees
absent. Their historical 1,867 files / 214,657,246 bytes are no longer present.
No deletion retry against those paths was performed in this pass; the actor that
removed them is not inferred.

**CLEANUP REQUIRES MANUAL PROJECT CONTROL EXECUTION** for five new temporary Git
snapshot/reporting files, totaling 530,615 bytes. They are not a worktree or active
Git ref. Their useful results are consolidated in `ephemeral-ci-results.json`.
Automatic approval review rejected their exact nonrecursive deletion before
execution as "blocked by policy". No alternate mechanism was attempted.

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\build\bepu-hosted-qualification\snapshot.index', `
  'E:\NovaCore\build\bepu-hosted-qualification\snapshot.json', `
  'E:\NovaCore\build\bepu-hosted-qualification\hosted-response.json', `
  'E:\NovaCore\build\bepu-hosted-qualification\create_snapshot.py', `
  'E:\NovaCore\build\bepu-hosted-qualification\capture_result.py' `
  -Force
```

## Durable evidence and next scope

`ephemeral-ci-results.json` preserves the commit/tree, 17 exact input identities,
diff, GitHub timestamps/error, zero-job result, PR/ref retirement and original-file
preservation. Prior manifest, original packages/DLLs, local qualification results
and reproduction instructions remain unchanged.

Reproduction: publish the exact listed input blobs on an authorized temporary ref
parented to the baseline and use the unchanged workflow. The captured failure is
workflow validation, so no local build campaign is necessary to reproduce it.

The next correction, if authorized, is narrowly the workflow cache-environment
expression, followed by a fresh authorized hosted qualification. No correction
was made here. No dependency architecture, Windows trust work or solver work was
reopened. Stop for Project Control.
