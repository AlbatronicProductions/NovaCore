# Post-M16.2 qualification and repository convergence

2026-09-30. **PASS — bounded maintenance/convergence qualification complete.**
Post-M16.2 engineering maintenance only, with no milestone assignment.
AD4 is the sole production writer. Nothing is staged or published.

## Candidate and preservation

Canonical tree: `E:\NovaCore`, branch `main`, HEAD
`b0ccfd8f17643868a8d9011968a4566bc8a13f69`. Immutable bank `m16.2` peels to
`cd8fdcf977dee633f57da790e931a388b922048f`. HEAD alone does not identify this
uncommitted candidate.

- [Initial preservation](preservation-baseline.json), exact tracked
  [accepted shader diff](accepted-shader.diff) and added-file recovery archive
  `accepted-added-files.zip` identify all 35 initial changed/additional files.
  Their bytes matched the accepted shader source seals. The pre-existing index
  was empty; no index/staging/history operation was performed.
- [Production identity](production-identity.json) seals all 14 current unbanked
  production/build/test files. Unchanged tracked source is identified by HEAD.
  [Final identity](final-identity.json) covers all changed/additional files except
  its own self-reference, and [final tracked diff](final-tracked.diff) records the
  exact patch. Added files are sealed individually in that identity.
- [Accepted shader evidence](../runtime-shader-deployment/README.md) retains its
  own historical result and clean/incremental/test-before-package receipts.
  The later correction did not reopen or alter that accepted deployment design.

## What changed and why

The accepted source-owned shader correction makes native dependency/output
membership authoritative for all three runtime consumers, including generated
siblings, matching configuration provenance and hashes. Strict deployment and
independent source/consumer verification replace wildcard accumulation. Unknown
or modified destination files are preserved and refused; only verified obsolete
owned shader outputs may be removed. Native test outputs stay with test producers.

This convergence pass adds five bounded test-contract corrections:

1. Facility input follows its actual CMake `test-shaders` producer.
2. Window prerequisites use the accepted generated shader owner, with exact
   membership/hash checks and the separate independent package oracle.
3. The default terrain assertion follows the shared prepared-surface header.
4. Color draw ordering is separated from the prepass, while zero depth-only
   clear extent/lifetime, orbit depth and draw ordering remain protected.
5. Horizon source guards follow an audited common vertex transform and retained
   TES factor contract. Original bad-case witnesses and numerical/culling bounds
   remain; guards were not updated merely to accept different bytes.

[Contract mapping](contracts.md) records protected outcomes, demonstrated causes,
KEEP/MIGRATE/SPLIT disposition and replacement coverage. No test outcome was
retired. No shader algorithm, rendering/resource lifetime, simulation, gameplay,
UI layout or quality policy changed in this convergence pass. Current docs now
identify the direct-prepared/retained-TES split and current qualification separately
from immutable bank history.

## Gates and evidence

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS, zero warnings/errors | PASS, zero warnings/errors |
| Complete managed Graphics | 116 PASS, 0 FAIL / SKIP / EXCLUDED | 116 PASS, 0 FAIL / SKIP / EXCLUDED |
| Canonical native presentation/facility | 3 PASS | 3 PASS |
| Native regional physical gate | PASS | PASS |
| Prepared raster, tessellation, mapped memory, GPU-memory owner | PASS | PASS |
| Managed Player configuration/presentation | PASS, 55 checks | PASS, 55 checks |
| Launcher regression | PASS, 19 tests | PASS, 19 tests |
| Configuration-correct package after tests | PASS | PASS |

The final [gate receipts](gates.json), [selected logs](logs/),
[native CPU results](native-cpu-results.json), [Debug package](package-Debug.json)
and [Release package](package-Release.json) identify their actual inputs/results.
Both configurations selected the RX 6800 XT. Each deployed exactly the current
source-owned 66-output closure; this observed count is not the acceptance oracle.
Source/output membership, all generated siblings, provenance and every hash are
checked. The package retains no test-only or retired HUD shader output.

The final [shader negative suite](shader-red-team.json) passed **31 cases**:
missing/corrupt/extra artifacts, wrong configuration/cache/native DLL,
intentionally wrong source/manifests, mutually wrong deployment/verifier inventory,
source contamination, obsolete owned output and preservation/refusal of modified
or unknown files. The dangling-reparse guard uses mocked Windows attributes;
it does not claim a physically created symlink. Unchanged clean/incremental/
test-before-package evidence is reused from the accepted shader report. Final
package checks ran after native test generation. `git diff --check` passed.

Actual non-96 Windows DPI: **PASS for the required bounded routes** at 125%
(DeviceDpi 120), 3440 x 1440, with 1920 x 1080 transition receipt and return to
fullscreen. Configuration/menu/editor/gameplay and rendered-part picking were
exercised with automated native input. Windows scaling was restored to 100%.
See [DPI scope and witnesses](dpi.md), [native receipt](dpi-transitions.json) and
[ordinary smoke receipt](smoke.json). Ordinary Release smoke entered Exploration,
loaded a saved craft, selected its rendered core, launched Flight, paused and
quit normally; recorder admission and telemetry teardown remained intact.

The smoke package hash is
`528199b8fb1a7b3bc9c0d545a1314fcbab51480fe825aac26399d820a819a9c3`, matching both
the accepted Release receipt and post-smoke verification. The earlier shader
report's phrase “Physical UI actions” denotes automated native input in that
session; it is not evidence of physical human input. Its historical text is kept.

Full Graphics uses the currently authorized checked-in 18-scale fixture mode
`NOVACORE_P2S5F_ARTIFACT_INPUT`, not exhaustive regeneration. All cases run without
category exclusion. Native GPU/window cases use the existing process-local
Khronos-only canonical environment, with no registry/global layer changes.
See [commands and recovery](commands.md) for exact reproduction.

[Causal attempts](causal-attempts.json) preserve the cheap pre-correction failures,
the completed diagnostic Debug 112/116 run, the concurrent smoke DLL-lock build
failure, and the initial DPI foreground-precondition failure. No interrupted or
NOT RUN attempt is relabelled as passing. Historical offline workload/replay
output bytes were restored; final runs write distinct paths.

## Ownership, fallbacks and independent review

[Bounded audit](ownership-audit.md) covers canonical entry/UI/native resources,
build/deployment, tests and current docs. No broad provider retirement passed the
consumer/safety/payoff gate. Existing launcher/shared configuration, diagnostic
hosts, cold hierarchy, published-pupil retention, compatible memory retry,
physical-authority refusals, retained TES and MinimumRecorder have supported
responsibilities. They remain. No deletion count is an acceptance target.

Synthetic unloaded CPU elevation and diagnostic browser/web assets are **DEFER,
unchanged**: real consumers exist and no accepted complete replacement is proven.
They do not block the mandatory gates or create a new ownership conflict. They
need a separate supported-scope decision if retirement is later proposed.
There was no broad KSA research or claim of fresh authenticated history access.

[Independent reviews](independent-review.json) identify final source hashes,
resolved findings and visual limits. The contract reviewer rejected an initially
weak depth-clear assertion; the final guard rejects intervening depth/aspect
overrides. The ownership reviewer confirmed preservation and actual DPI evidence,
and the retained images show no material panel/control clipping or overlap.
Accepted shader red-team coverage is retained and its permanent negative suite
passed again on the final packages. Final read-only gate review checks evidence
and input hashes; it is not an independent test rerun.

Concise reports, receipts, source hashes, selected witnesses and reproduction
commands are retained here. Bulk runtime logs, recorder data, generated horizon
extracts and native outputs remain local/ignored under their existing owners.
No unresolved evidence, save, cache, compatibility provider or unrelated file
was removed. No new broad cleanup helper or alternate canonical tree was created.

## Limits and next action

This pass does not close 150 FPS, cold/global fallback, planetary visual fidelity,
historical blackout causality, MinimumRecorder public disposition or public-release
readiness. Physical held-key, focus-loss and monitor-transition permutations are
NOT RUN/unclaimed. Downsampled UI images do not certify native-pixel text quality.
No runtime algorithm/lifetime change required a new performance campaign.

Return the final candidate to Project Control. No commit, push, tag creation or
movement is authorized before final acceptance. Project Control chooses the
eventual normal-main commit message; no M16.3 or other milestone is created.
