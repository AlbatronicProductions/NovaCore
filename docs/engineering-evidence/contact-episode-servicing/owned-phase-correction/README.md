# M14.20 owned-phase publication baseline correction

2026-09-13. **PASS — UNBANKED; STOP FOR PROJECT CONTROL.**

Final classification: **M14.20 OWNED-PHASE PUBLICATION BASELINE CORRECTION — READY FOR PROJECT CONTROL ACCEPTANCE**.

## Defect and current compile failure

The independently checked out public baseline `f0c8e0f2c16de98e492ef4b9b4a5a24f372e8b18` fails `dotnet build NovaCore.sln -c Debug -v:q` with zero warnings and one error:

```text
SimulationTransactionEngine.ContactServicing.cs(100,35): error CS0103:
The name 'PublishPersistentContactInOwnedPhase' does not exist in the current context
```

`git grep -n PublishPersistentContactInOwnedPhase f0c8e0f -- src` finds only that call. The banked [M14.20 report](../README.md) explicitly states that the M14.19 admission and fixed-write body was extracted into this helper. The source contradicts that retained evidence. Historical evidence is unchanged; this record appends the correction.

## Proven Git/source cause

- M14.19 `fc6fbf269d7a8022ce2fe184aea0dfc94716db05`, M14.20's parent `3e1028339e9e80648ed15c510644d871259c26bd`, banked M14.20 `49712256756e7d5433fc3ecf6aa1d1880a9f36a8`, and public baseline `f0c8e0f2c16de98e492ef4b9b4a5a24f372e8b18` all contain publisher blob `566c91cf68c8bec80680c3ff64c2e1b16fbc6f89`.
- M14.20 added the servicing caller but did not change the publisher file. The candidate branch still points at this banked commit; no later reachable publisher revision was found.
- The local object database retains the extracted publisher as unreachable blob `fd2759cce1c5dbeecc1c9c3ad5b7f41ef11a2bcc`, in transactions tree `e22f985b217ca6689e0ae56f2388b211ee3f7910`, Simulation tree `cb5fdd96ced1069ff61fc32b7938fb011a5c38c6`, and source tree `dae841d8ba63737863b4dcbff0925c8f35ee8bbd`.
- Candidate root tree `44d7f8d16848133593d80216737f2db4ea58ef68` differs from banked M14.20 only in this publisher, the Graphics test project wiring, and the unrelated untracked `staging-build-blocker.md`. A second root tree `d3e047e960b4a3aeedffe3ff1a6b8e9410551bd9` also contains the helper. Neither root tree has a containing commit in the inspected local object database.
- The September 13 reflog records M14.20 committed at 14:18:05 -0400 and main fast-forwarded at 14:18:06. The 14:22:43 post-bank commit changes only `NovaCore.Graphics.Tests.csproj`; it cannot have removed this helper.

**Conclusion:** the qualified publisher was omitted from the banked tree; this is not a later committed publisher regression. The exact staging/index/restore command responsible is not recorded by these Git objects or reflogs. An explicit staging-list omission or partial staging mechanism cannot be distinguished from this evidence, and is not asserted.

To prevent recurrence, the banking review should compare every qualified source identity with the proposed index/commit tree, including paths absent from its diff, then compile that exact tree independently of leftover outputs. This is a review recommendation, not an implemented workflow change.

## Exact accepted implementation recovered: YES

The normalized Git blob is `fd2759cce1c5dbeecc1c9c3ad5b7f41ef11a2bcc` (8,584 LF bytes). Restoring LF endings on lines 51–63 and 126–127, and CRLF on all other lines, yields **8,696 bytes** and SHA-256:

```text
7A5521A072992E7D5360B733DCE7EE29CA00EB093D923F7E0B544CCC744610CF
```

This exactly matches the publisher entry in the retained [identity.json](../identity.json). The line-ending layout was identified by bounded candidate conversion and accepted only on an exact cryptographic match. [accepted-source.json](accepted-source.json) preserves the recovered UTF-8 source with escaped line endings, so its bytes remain recoverable after Git garbage collection or checkout newline conversion. The raw Git blob's SHA-256 is `87F3F1EAE927C95DAAAE021AE38B21447E0996050A8196A5BC9C7F8BBA145D2B`.

Recovery inspected 7,957 local objects, reflogs, all-object metadata, relevant unreachable trees, candidate/public refs and retained qualification evidence. No garbage collection, ref rewriting, or article changes were performed.

## Correction and shared-body equivalence

The production correction restores the accepted extraction and adds one explicit entry guard:

```csharp
if (!_clock.PublicationPhase.IsOwnedBy(this))
    return new(PersistentContactPublicationStatus.ReentrantPublication);
```

The recovered helper lacked this explicit check. It is the sole production addition beyond the recovered implementation and directly fulfills this correction ticket's owner-phase requirement. `IsOwnedBy` checks both the owning thread and publisher identity. There is no new lock or phase.

Ordinary path: `PublishPersistentContact` (or its failed-acknowledgement test seam) → `PublishPersistentContactCore` → unchanged owner-thread/reentrancy checks → existing `TryEnter(this)` → shared owned body → existing `finally` exit.

Servicing path: `ServiceContactDebt` → unchanged `ServiceContactDebtCore` phase acquisition → `PublishPersistentContactInOwnedPhase` while held → caller's `finally` exit. The helper neither acquires nor releases the phase. There is one fixed publication body and no recursive acquisition.

The entire text from the first world/configuration validation through `return success;` is identical, after newline representation only, in the M14.19/public body, the recovered M14.20 source, and this correction. Its LF SHA-256 is `133DECA2CE4E0C93A20AA6FE06C771B01B8455EB142BDC728C05CED8BCF168C1`.

Therefore engine/configuration/world/generation/frontier and genuine receipt checks, exact request clock, `PreparePublication`, event refusal, exact debt arithmetic, revision overflow/history capacity refusal, paired slots, endpoint validation, represented bits, one StateRevision increment, unchanged TimelineRevision, deterministic history, same-phase recheck, fixed canonical commit, acknowledgement and irrevocable terminal behavior remain intact. No normalization, repropagation, replay, duplicate publisher or rollback was added. M14.20 host credit, interval budgeting, receipt advancement and servicing accounting source are unchanged.

The only test-file change directly protects the required helper contract: unowned phase, foreign publisher, and wrong thread refuse without mutation; the engine-owned helper publishes once and keeps the phase held; subsequent ordinary publication succeeds. Existing public WrongOwnerThread/ReentrantPublication and terminal tests remain unchanged.

## Validation results

All commands ran against the isolated correction at `E:\NovaCore-M1420-Correction`, detached at the specified public commit. Full results, source fingerprints, commands and timings are in [validation.json](validation.json).

| Gate | Debug | Release |
| --- | --- | --- |
| Full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| M14.19 persistent-contact focused suite | PASS | PASS |
| M14.20 cheap servicing/accounting suite | PASS | PASS |
| M14.20 full focused servicing suite | PASS | PASS |
| Publication and host-credit/service terminal failures | PASS | PASS |
| New explicit shared-body ownership regression | PASS | PASS |
| Full Simulation suite | PASS | PASS |
| Graphics contact-development presentation/lifecycle | PASS | PASS |
| M14.17 certified publication, acceptance gaps, allocation matrix | PASS | PASS |

Warmed publication complete-path, servicing complete-path, no-work and backlog allocation gates each measured **0 bytes**, with entry/exit checks passing. Independent positive controls measured **152 bytes**. Existing retained-storage bounds passed. The full Simulation suites also include M14.18 staging and earlier Simulation regressions.

One existing focused Release timing process per path was sufficient for this recovery plus constant-time ownership check. Each used the existing 128 warmup and 1,024 measured operations, without changed thresholds or retries:

| Path | Median ms | P95 ms | P99 ms | Max ms |
| --- | ---: | ---: | ---: | ---: |
| Persistent publication | 0.0159 | 0.0163 | 0.0184 | 0.0597 |
| Contact servicing | 0.0187 | 0.0191 | 0.0213 | 0.0304 |

Both passed their existing ceilings. Historical M14.19/M14.20 performance evidence remains historical; this does not claim a new broad performance qualification. No article tests, live acceptance, terrain campaign or rendering campaign ran.

### Isolated-build prerequisites and initial environment failures

After CS0103 was fixed, the first solution attempt reached missing native DLL/shader copy prerequisites in the fresh worktree (0 warnings, 50 MSB3030 errors). Unchanged native baseline source was configured and built for Debug/Release using the documented MSVC/Ninja/Vulkan environment, then both solution builds passed. No original-worktree binaries were reused.

Graphics root discovery requires a `.git` directory and does not recognize a linked worktree's `.git` file. An initial test stopped with `Repository root unavailable`. A disposable, separately initialized Git test root at `E:\NovaCore-M1420-Correction-audit\graphics-root` contains `assets` and `build` junctions to this correction worktree. Existing native identity/ABI checks remained active and verified the loaded correction DLL against the matching correction build. No harness/source bypass was introduced.

The extra M14.17 tests initially stopped at the missing local fixture cache. Setting process-local `NOVACORE_ASSET_CACHE=E:\NovaCore\.novacore\cache\terrain\v1` supplied the existing cache to the normal loader and its exact manifest/hash checks. No asset was regenerated or modified. Initial failures and successful executions remain in the raw audit directory.

## Corrected fingerprint and files

Publisher working bytes: **8,823**, SHA-256 `9E574A5C07685150DE078402B868676BB97BACF8EB4CE7F54E2EE6173ED0B8A3`.

Publisher LF/Git blob: `4c7aea197588e41a2f229ec8868fda88a490d214`; LF SHA-256 `E73147EA1BFEBE4C54825C47AC5750CA82B50E196E58A0B0926CD59EDC98ECD7`. Working-byte fingerprints describe this validation checkout; normalized identities survive ordinary Git newline conversion.

Changed: one production publisher file, `PersistentContactPublicationTests.cs`, and this correction evidence directory. Original qualification files, dependencies, solver settings, renderer, terrain, launcher and article remain unchanged.

## Reproduction

Use [reproduce.ps1](reproduce.ps1) from the corrected checkout after preparing native prerequisites per [Windows build instructions](../../../build-windows.md). It preserves the existing test settings and uses an isolated temporary Graphics root when needed. Point `NOVACORE_ASSET_CACHE` at the existing qualified cache for the extra M14.17 tests. It does not modify refs or stage/commit files.

Read-only history checks:

```powershell
git ls-tree fc6fbf2 src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PersistentContact.cs
git ls-tree 4971225 src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PersistentContact.cs
git ls-tree f0c8e0f src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PersistentContact.cs
git diff --name-status 44d7f8d16848133593d80216737f2db4ea58ef68 4971225
git show --stat f0c8e0f
git reflog --all --date=iso
git fsck --full --no-reflogs --unreachable
```

The uncorrected compile failure can be reproduced in another isolated checkout of `f0c8e0f`; do not restore over this correction or the stopped article.

## Preservation and disposition

All 12 pre-existing dirty/untracked files in the article worktree were SHA-256 checked before and after, including its stopped draft and evidence; every hash and porcelain status entry is unchanged. `codex/engineering-spacecraft-contact-article` remains at `f0c8e0f2c16de98e492ef4b9b4a5a24f372e8b18`, untouched and unresumed.

All original refs are unchanged. The M14.20 annotated tag object remains `ac96f952b9c44220cd93042c0d7c6e8e07d227d5`, peeling to `49712256756e7d5433fc3ecf6aa1d1880a9f36a8`. Main remains `f0c8e0f2c16de98e492ef4b9b4a5a24f372e8b18`. The correction index is empty; there is no correction commit, push, milestone tag, rebase or article resumption.

Raw build/test/recovery logs and preservation snapshots remain at `E:\NovaCore-M1420-Correction-audit` for Project Control review. Concise permanent provenance and accepted source are retained here. `git diff --check` passes.

**Lead judgment: PASS. Remain UNBANKED. STOP FOR PROJECT CONTROL.**
