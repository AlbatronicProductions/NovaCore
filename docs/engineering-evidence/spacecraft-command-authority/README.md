# Canonical spacecraft command authority — implementation qualification

Candidate, **UNBANKED**, no milestone assigned. Judgment: **PASS**, subject to Project Control acceptance. This is a non-actuating command responsibility, not propulsion, powered contact or live input integration.

## Baseline and responsibility

HEAD/main/origin/main/remote main: `8c291881b323be3e3038d57d2ac180eff826f065`.
Banked M14.21: `b6e8fa568585706ce38c0609aeb14db4a643123e`, tag `m14.21-authored-compound-spacecraft-contact`.
Candidate branch: `codex/spacecraft-command-authority`.

Three additive production files introduce private engine-owned command storage, an immutable issuing capability, bounded prospective ingress, exact command-only consumption, a separate CommandRevision and copied requested-demand observations. Every pre-existing production file remains byte-identical. The sole tracked edit is ten test-runner registration/route lines. See [identity](identity.json) for source hashes and the complete preservation check.

[Contract](contract.md) defines ownership, finite schedule, lifetime, admission, duplicate/no-op behavior, zero-debt operation, failure atomicity and the non-actuating integration boundary. The [accepted architecture](../spacecraft-control-architecture/README.md) remains distinct retained evidence; it was not reworked or staged.

## Decisive witnesses

- Zero debt: axes commit at E=0; CommandRevision increases; exact physical pair, physical revision, clock, debt and timeline remain unchanged.
- Backlog: old throttle 0.2; H=50,000 assigns new throttle 0.8 to E=50,000. Later credit raises H to 100,000 without moving E. Earlier endpoints through 33,333 keep 0.2; 50,000 commits 0.8.
- Same-epoch ignite/shutdown preserve sequence. Duplicate identities do not reapply; repeated held/latched/target/release state does not revise.
- Prepared target refusal leaves target/consumer/sequence/queue unchanged. Revision overflow refuses before writes. Closure, event, foreign identity, wrong thread, reentrancy, capacity, finite-source and unsupported pause/warp failures remain explicit.
- Seven ordinary slots plus reserved eighth revocation: accepted earlier requests retain order; immediate ingress seal does not rewrite funded history; neutralization applies prospectively.
- Identical eight-command accepted manifest under 30/60/150/240 Hz and delayed service: identical complete result sequence over 120 boundaries, exactly 2,000,000 funded/consumed ticks. This is not a claim about identical human/device sampling. A separate target admission/interleaving regression compares canonical facts while allowing ingress-progress diagnostics to differ.
- Centered, tilted and constant-moving-frame M14.21 fixtures: each 1,200/1,200 stored physical endpoints is bit-identical with non-actuating commands. Canonical stored paired fields match the same export; one physical revision/history entry per interval, unchanged timeline/world, zero remaining debt. The unchanged baseline geometry gates pass, including final 600/600 support and 400 x 16,666 + 800 x 16,667 = 20,000,000 ticks.

| Article case | Baseline peak penetration | Command path |
|---|---:|---|
| Centered | 0.005819714882528193 m | All paired endpoint bits identical |
| Tilted | 0.006945546380241474 m | All paired endpoint bits identical |
| Tilted, moving frame | 0.006945746950922316 m | All paired endpoint bits identical |

No renderer, device, camera, scene, Blender, force/torque, BEPU dependency or banked article/selector path changed.

## Validation

The predeclared [reproducer](validate.ps1) ran once, stopping on any nonzero exit. All **19 final gates passed**, with no final-matrix retry. SDK: 10.0.303. [Machine-readable results](validation.json) retain commands, exit codes and concise witnesses.

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Focused command lifetime/refusal | PASS | PASS |
| Five schedule/backlog partitions | PASS | PASS |
| Three article bit-equivalence sequences | PASS | PASS |
| Focused exact allocation + control | PASS | PASS |
| Full Simulation | 60/60 registered groups PASS | 60/60 registered groups PASS |
| ReferenceFrames | PASS | PASS |
| Precision | PASS | PASS |
| Verified BEPU dependency | PASS | PASS |

Full Simulation includes the original M14.17 certified-continuation, M14.18 private staging, M14.19 persistent publication, M14.20 servicing and M14.21 selector/article regressions. No unrelated GPU campaign ran. Precision's retained gate has exit status rather than a filtered PASS-prefixed line; its executable throws on assertion failure.

## Allocation, storage and cost

The unchanged checked 1 MiB allocation helper was used separately from timing. Entry/exit both succeeded. Every focused and full-suite Debug/Release result is exact zero for admission, commit, complete operation, observation/consumer, no-work, pending polling, and actual funded-backlog consumption. The latter advances to E=50,000, commits seven future commands and preserves 50,000 ticks of later debt. The independent byte[128] control detects **152 bytes**. No tolerance, subtraction, profiler or runtime configuration change was used.

Prepared retained command storage measured **1,608 managed bytes** for capability, private owner and eight-slot array. Queue value payload: 8 x 160 = **1,280 bytes**; embedded committed-state value: **152 bytes**; remaining **176 bytes** contain authority/order/bookkeeping and runtime object/array overhead. Include the engine's new 8-byte reference slot for a conservative combined bound of **1,616 bytes**. There is no production command-history allocation. Observation (224 bytes) and requested demand (168 bytes) are returned values, not retained heap objects. Measurement excludes pre-existing engine/physics storage and test-only external histories.

One fresh Release cost process, 256 warm operations then 4,096 measured admission + commit + observation + non-actuating projection operations, normal runtime:

| Median | P95 | P99 | Maximum |
|---:|---:|---:|---:|
| 0.0013 ms | 0.0014 ms | 0.0014 ms | 0.007 ms |

These direct command costs leave substantial headroom relative to 6.67 ms, but are not a whole-frame or actuator/physics benchmark. No larger performance campaign was needed.

## Independent review and development failures

The independent verifier's consolidated correction addressed escaped mutable storage, normalization non-idempotence/admission-partition dependence, ambiguous close success, signed-zero no-op writes, and the distinction between pending polling and actual backlog consumption. Permanent regressions cover each. See [red-team disposition](red-team.md).

Before final qualification, one compile failed because the new article test lacked its Transactions import. The first new article comparison also failed: it incorrectly compared staged motion (prior physical revision) with a general evaluator result (new revision and normalized observation). Source inspection identified the mismatched boundaries. The test now compares stored/export bits on both sides and independently checks canonical installed fields. Neither correction changed production physics, the numerical gates or a banked test. These were not hidden retry-to-green results; the final matrix began only after the authored comparison contract was corrected.

## Reproduction and retention

From the repository in PowerShell 7, run `& .\docs\engineering-evidence\spacecraft-command-authority\validate.ps1 -OutputPath <review-output.json>` for the declared plan. For a single witness, use the corresponding arguments recorded in validation.json; the command routes are `--spacecraft-command-cheap`, `--spacecraft-command-schedules`, `--spacecraft-command-article`, `--spacecraft-command-allocation` and `--spacecraft-command-cost` on the built Simulation executable.

Retain this summary, contract, red-team result, concise validation, identity and reproducer, within a 64 KiB evidence budget. Do not retain full stdout, dumps or comparison trees. The one temporary preflight fingerprint file is disposable after identity verification. Pre-existing unrelated evidence and historical refs are preserved. Normal ignored solution build outputs remain normal workspace artifacts.

Cleanup outcome: automatic approval review rejected deletion before execution as “blocked by policy.” No retry or workaround was attempted. One disposable file remains, **402,165 bytes**; nothing was deleted. Its useful provenance is retained in identity.json. Project Control can remove only this reviewed scratch file and then its empty directory:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\spacecraft-command-authority\baseline.json' -Force
Remove-Item -LiteralPath 'E:\NovaCore\build\spacecraft-command-authority'
```

## Disposition

**PASS. UNBANKED. STOP FOR PROJECT CONTROL.**

No propulsion, powered retained contact, Florida launch, Blender integration or next milestone has begun. Future actuator realization must explicitly qualify its physical authority/publication responsibilities; this command revision cannot hide force/configuration changes from retained-world validation.
