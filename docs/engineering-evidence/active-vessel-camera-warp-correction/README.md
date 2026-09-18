# Active-vessel camera time-warp correction — closed to Project Control, unbanked

**Engineering PASS. Project Control manual acceptance PASS.** The manually accepted corrected Release runtime remains deployed. No further camera correction or manual retest is requested.

Original engineering gate judgment: **PASS — TIME-WARP SURFACE CAMERA CORRECTED; CORE ACTIVE-VESSEL CAMERA READY FOR MANUAL RETEST**. Project Control subsequently completed that retest and explicitly reported PASS; “ready for manual retest” is no longer the current pending status.

The supported vessel's orbit view now uses the reference basis carried by its copied focus observation. Earth rotation carries the retained local view; input still changes orbit/zoom. Full quaternion rebasing preserves pose through explicit future publisher-owned frame changes, including retained views while focused on a celestial. No departure behavior was implemented.

- [Manual acceptance and deployment provenance](manual-acceptance.md): user-reported PASS, independently reconciled 88/88 candidate/deployed hashes. PowerShell helper compatibility corrected; verification performed without redeployment.
- [Architecture and targeted KSA evidence](architecture.md): source authority, authenticated history, responsibility classification and future transition boundary. Prior investigation was not repeated during closure.
- [Independent verification](verification.md): all 18 attacks pass, exact source readset.
- [Regression results](regression.json): 96/96 Debug/Release gates, including final frame/epoch/cadence, normal camera, support, nonmutation, precision and exact-zero coverage.
- [Performance](performance.md), [native distributions](performance.json), [allocation/invariants](allocation-and-invariants.json): native 1x/120x/14400x and matched celestial captures, permanent 86400x outcomes, startup/GC tails disclosed.
- [Final identity](identity.json): Git, runtime, deployment, evidence and source seals. [Diagnostic retirement](retirement.json) identifies removed rebuildable outputs.

The entry trace (`warp-entry.json`) is immutable failure evidence. `warp-candidate.json` is the first corrected trace taken before the final publication-epoch guard/test completion; final candidate acceptance rests on the final-source 96-gate records and Project Control deployment/manual PASS. Historical prior camera packages and their acceptance chronology remain unchanged.

Scope: seven source/test files changed relative to this correction's entry (five presentation files, one of them comment-only, and two test files). Native/interop changes visible in cumulative Git status belong to the earlier camera work and are unchanged here. Physics, Earth rotation, time warp, support, terrain and contact are unchanged. FREE remains **DEFERRED**; HOME is **unbound**.

Source control remains unbanked: HEAD `45b1bbcdd8d0e1d42f79f3bb5124e80753ce0e8f`; M15.3 bank `544f25ad618bcad7a2753de69108ca3c4095df11`; annotated tag object `e1de2ac72f266c01f3c8fe2e3e475151f0423204`. No stage, milestone, commit, tag, push or banking. No engine controls, launch stack, powered departure, SAS/navball or next front.

## Reproduction and retention

Run scripts from the repository in PowerShell. `build-candidate.ps1` builds both configurations using the unchanged qualified native outputs; `validate.ps1` runs 96 gates. `prepare-launcher.ps1 -VerifyOnly` checks current deployment without copying. Omit VerifyOnly only when deliberately redeploying a built candidate. `build-observer.ps1 -Version entry|candidate` builds disposable overlays; `capture-performance.ps1` runs full populations, and `-MatchedCelestial` runs equivalent celestial-only populations. `consolidate.ps1` parses existing logs; it does not execute tests. `reproduce-warp.ps1` is the retained cheap trace tool and was not rerun during final closure.

Permanent package budget: **262144 bytes**. Retained reports, compact quantitative outcomes, hashes and reproduction tooling protect this camera responsibility; no KSA proprietary code/assets are copied. Exact entry sources, failed-measurement logs, regression logs and raw timing logs remain bounded local audit inputs under `build/`. The accepted candidate builds remain temporarily for Project Control's unbanked handoff and exact deployment provenance; they are not promoted to a permanent archive. Disposable diagnostic runtimes are retired with an inventory, while the verified deployment is preserved.

Storage at consolidation (decimal units): diagnostic/build output **0.508 GB**; disposable runtimes retired **0.175 GB**; permanent package approximately **0.237 MB**, below the 0.262144 MB budget. The **0.327 GB** accepted candidate build pair is the largest retained temporary category, specifically preserved for exact unbanked handoff/deployment verification. No prior package was purged. After retirement, all 88 deployment hashes still matched and the deployed 180-frame smoke exited zero with empty stderr.

**STOP for Project Control.** Manual PASS does not authorize banking or opening another production front.
