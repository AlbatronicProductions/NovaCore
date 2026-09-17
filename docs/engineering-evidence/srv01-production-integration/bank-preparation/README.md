# SRV-01 bank-preparation packet — 2026-09-17

**BANK PREPARATION ONLY. No commit, tag, push, merge or banking performed.**

Project Control accepted the broader SRV-01 production integration, reusable
four-horn visuals, corrected exhaust and final personal visual checklist. This
packet prepares one atomic release without reopening those accepted mechanisms.

## Current truth and proposed identity

HEAD, main, origin/main, live remote main and merge-base are all
`09463ec6c323daf233205cb08306d38611c22e6e`. Branch is
`codex/srv01-production-integration`; M15.1 is the latest banked milestone in
actual main history and remote milestone tags. Sixty-six historical tags remain
unchanged. Only `E:\NovaCore` and `E:\NovaCore-Blender-Visual-StepB` are registered
worktrees. The latter owns the accepted editable Blender/tooling source and is
outside this bank. Exact initial refs, branches and path status: preflight.json.

Proposed next milestone: **M15.2 — SRV-01 Reusable Production Spacecraft Integration**.

Proposed commit: `NovaCore M15.2: Integrate SRV-01 reusable production spacecraft`.

Proposed annotated tag: `m15.2-srv01-reusable-production-spacecraft-integration`.

These names are proposals only; current-state documents preserve M15.1 as banked
and label SRV-01 accepted/unbanked. The root license and unrelated legal drafts
are unchanged. No next production front is authorized.

## Production responsibility and authority audit

Seven explicit part instances share four immutable definitions: capsule,
tank/service body, gimbaled main and four instances of the reusable four-horn
block. Sixteen RCS `(part, actuator)` tuples and one main engine consume through
34 explicit typed feed edges from two exact species stores. Structural ownership
never supplies implicit feed authority. Current contents contribute to assembly
mass, COM and full inertia; force moments use current COM.

The accepted source uses exact all-or-none shared consumption and exact exhaustion
boundaries, preserving separate protected M15.1 contact behavior. Canonical state,
not artwork or plume parameters, controls motion/resource/actual actuator state.
Socket/axis checks bind each promised visual horn to its physical tuple. The
renderer consumes copied state, published gimbal and stable nozzle IDs; idle and
terminal presentation emit nothing. One display callback follows bounded service,
not one renderer invocation per physics interval.

Cold prepared capacity derives from possible craft/nozzle instances and remains
valid through resize; the first READY active count is not an allocation bound.
All four runtime GLBs are hash-pinned and independent of Blender installation or
`.blend` inputs. No development tree is included. Canonical artwork/source/tools
remain external and unchanged where covered by the retained cleanup hashes.

## Identity, cleanup and dependency preservation

- Accepted candidate source/test/asset files: **55/55 unchanged**.
- Protected pre-exhaust-correction Simulation fingerprints: **25/25 unchanged**.
- Accepted runtime GLBs: **4/4 unchanged**.
- Inspected KSA files: **14/14 unchanged**, KSA writes 0.
- Unrelated local legal-review files: **30/30 unchanged**, excluded.
- Covered Blender source/review/tooling files: **123/123 unchanged**.
- Current global/Florida terrain, source raster and generation manifests:
  **5/5 unchanged**. Exact hash witnesses: external-preservation.json.

Four Blender files were open with another reader; ordinary Get-FileHash could not
open them. Read-only FileShare.ReadWrite streams verified their original hashes
without closing applications, editing files or claiming a mismatch.

The five obsolete worktrees named in the ticket are absent and unregistered.
The previous SRV-01 disposable root is absent. The external authorized historical
cleanup report records 207 deleted files / 763,709,025 logical bytes and preservation
of 2,787 files at that cleanup. This ticket independently rechecked the narrower
sets above, not all 2,787 again. No retired terrain payload was restored. Its exact
historical replay is intentionally unavailable; hashes/history remain.

Read-only audit observed unrelated broken historical build redirects during Git
ignored-directory enumeration. They are excluded and not used by this candidate;
this ticket neither follows them for cleanup nor repairs unrelated directories.

This ticket's rebuild/check output is separately excluded at
`E:\NovaCore\build\srv01-bank-preparation`: **1,743 files / 369,674,038 bytes**.
Results and reproduction are retained; automatic approval review rejected the
reviewed removal before execution as `blocked by policy`. No retry was attempted.
This is disposable output outside the bank, not an unresolved engineering gate.

Exact reviewed manual cleanup:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-bank-preparation' -Recurse -Force -ErrorAction Stop
```

Non-destructive verification (expected False after cleanup):

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-bank-preparation'
```

## Final bounded validation

Debug and Release full solution builds: **PASS, 0 warnings / 0 errors**.
Focused SRV-01 and original assembly production, exact accounting, mass/tensor,
main/gimbal, sixteen jets, deterministic reconstruction/replay, 24 independently
generated trajectories, refusal and allocation gates: **PASS in both**.

Fifteen protected M15.1 command/resource/free-flight/contact routes passed in both
configurations. Native capacity canaries, actual production-SPIR-V exhaust tests,
managed presentation and four strict graphics/legacy-presentation cases passed in
both. GPU validation errors: **0**. Warmed required paths: **0 B**, deliberate
byte[128] positive control: **152 B**, checked entry/exit PASS.

The first build invocation failed before compilation because vcvars64 supplied
`Platform=x64` while the solution defines `Any CPU`. The reproduction script now
explicitly selects the existing solution configuration. The failed attempt is
retained in validation.json; no production source, test or solver correction was
made. validate.ps1 is the reproducible bounded bank check, run in the x64 VS
developer environment. It writes only build/srv01-bank-preparation.

Accepted broad numerical/performance/manual qualification remains valid because
all 55 source/test/asset seals match. No fresh broad benchmark or repeated manual
acceptance is claimed. Exact new gate witnesses and log hashes: validation.json.

## Accepted performance and limitations

Three independent Release processes, 480 frames at 1280×720:

| Run | Median ms | P95 ms | P99 ms | Maximum ms |
|---|---:|---:|---:|---:|
| 1 | 5.553 | 5.844 | 6.239 | 35.592 |
| 2 | 5.532 | 5.827 | 6.341 | 35.130 |
| 3 | 5.547 | 5.836 | 6.032 | 40.196 |

Maximum causes remain **UNATTRIBUTED**. No all-frame 6.67-ms or hitch-free guarantee
and no renderer-causation claim. Weighted plume overlap is approximate; complete
KSA gas/atmosphere/pressure-driven equivalence is not claimed.

Explicit synchronous save/restore remains allocating cold work. In the retained
three-process post-capacity-correction population, save maxima were
15.9494 / 18.8182 / 14.7316 ms and restore maxima were
14.1967 / 11.5437 / 17.8468 ms. No new asynchronous/hitch-free persistence claim.
The nominal RCS mass-density/artwork limitation in gate0.md also remains.

No arbitrary editor, staging/decouplers, docking, reentry, full atmosphere, full KSA
parity, liftoff/departure or Florida capability is introduced.

## KSA convergence and independent red team

Current directly inspected build 2026.9.10.5438 and official live-changelog evidence
remain the accepted reference. ADOPT/ADAPT covers nozzle-local density/core/outer
falloff, separate optical emission/extinction, advected independent noise,
depth-clipped integration, weighted transparency and render-owned per-nozzle work.
NovaCore assets/code are independently authored; exact resource and publication
authority intentionally remain NovaCore-specific. See ../exhaust-method.md.

Independent read-only verifier: **PASS**, no source defect. Its direct witnesses
include AssemblyDesign.cs identity/feed admission, AssemblyFlight.cs aggregate
consumption, AssemblyResources.cs exact exhaustion, AssemblyDynamics.cs current-COM
moments, StockAssemblyDevelopmentScene.cs socket and terminal checks,
Program.cs one-submission callback, ReusablePartVisuals.cs pinned GLBs and native
prepared-storage checks. Its documentation objection (save/restore hitch disclosure)
is resolved here and in current-state/architecture docs. No accepted quality or test
threshold changed.

## Exact bank and exclusions

bank-manifest.json classifies **every proposed path** and every changed/new excluded
path. bank-manifest.txt is the exact newline-delimited future Git pathspec. Both
are inclusion manifests, not evidence that staging happened. manifest.md prints
the complete grouped list. There is no alternate-index staging in this ticket.

Exclusions include all 30 unrelated legal-review files; the superseded initial
exhaust-ksa.md narrative (final mechanism is exhaust-method.md); the duplicate
pre-cleanup closeout.json; all ignored build/obj/bin, local scratch and experiments;
external Blender sources; KSA installation; retired historical terrain payloads.
Excluded reports remain locally intact. Reproduction-only source inspection,
oracle and intake recipes are retained evidence, never runtime dependencies.

## Banking actions — only after separate authorization

1. Reverify remote main/base, all qualified seals, manifest membership/content,
   unstaged changes, worktrees and historical tags. Stop on drift; no automatic rebase.
2. Explicitly stage using the exact manifest; compare cached paths to it and run
   cached diff check. Stop on any discrepancy.
3. Review the complete staged tree and protected identities, then make the single
   proposed milestone commit. Verify parent, tree, message and included paths.
4. Create the proposed annotated tag at that exact commit. Recheck remote main.
5. Fast-forward local main and push main/tag without force only if that later
   authorization includes publication. Verify remote commit, peeled tag and files.

The copy/paste Git sequence is in bank-actions.ps1.txt. It is an inert plan, not an
executed script. Required checks there deliberately stop instead of auto-rescuing.

**Final judgment: BANK-READY. STOP FOR PROJECT CONTROL BANK AUTHORIZATION.**
