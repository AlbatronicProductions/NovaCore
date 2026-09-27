# Gate 1 bounded closure — STOP after independent review

## WHAT HAPPENED

Project Control explicitly authorized one additional correction cycle for the two previously proven Gate 1 defects. That exception was used; the known witnesses now pass. The required complete admission review proved additional independent correctness failures. Gate 1 is not promoted, Gate 2 has not begun, and no third correction was applied.

Gate 0 remains PASS. Only preservation verification was performed; its entry snapshot and all prior evidence remain retained. This report supersedes the current-status portions of the original `report.md`, `redteam.json` and `reproduce.md`, which remain historical evidence of the first stop.

## ROOT CAUSE — DEFECT 1

Interface-only propellant ports checked resource identifier syntax without resolving that identifier in the authoritative catalog. The used-resource dependency digest collected stores and consumer mixtures but omitted resources referenced only by service ports. An unknown resource could therefore be admitted, and a changed passthrough-only resource revision could evade the document dependency check.

## ROOT CAUSE — DEFECT 2

Socket-placement records were dereferenced before null validation. Malformed anchor/count/member data lacked early, field-specific guards. A null record reached `set.Anchor` and raised `NullReferenceException` instead of the normal refusal.

## CORRECTION

Production edits are restricted to `AssemblyConstructionDefinitions.cs` and `PartStandard.cs` in this exception cycle:

- Every port resource resolves by exact ID in the already validated authoritative `ResourceTypeData` table before catalog construction. Positive revision and strict catalog schema remain the existing contract; no new resource schema or latest-version lookup was introduced.
- Port-only resources now participate in the canonical used-resource digest, including the full authoritative record and revision. Changing revision/content changes the seal; the existing document compiler and static runtime restore reject the stale dependency. Unused catalog entries do not replace referenced resources or pollute the dependency set.
- Socket groups, axes, placements, anchors and member lists receive early validation before compatibility evaluation or canonical ordering. Null/default/incomplete/unsupported entries are refused rather than synthesized. Diagnostics name the definition and offending field. Attachment frame diagnostics now identify their definition/node.

The physical tolerance helper and shared vector parser described below were not changed. No schema/product-policy redesign, CraftDocument implementation, editor behavior, greybox balance, electrical/launch/support change, KSA write or deployment occurred.

## TESTS

All Gate 1 branches ran against the final source:

| Branch | Result |
|---|---|
| Existing Part Standard baseline | PASS — 33 checks |
| Original three red-team witnesses | PASS — unresolved resource refused, dependency revision sealed, null placement controlled |
| Additional bounded-closure coverage | PASS — 55 checks |
| Complete-boundary admission audit | FAIL — additional physical-consistency and malformed-vector failures |

The 55 checks cover exact ID/revision/content, unresolved/malformed/ambiguous resources, wrong-schema/unknown fields, definition/resource enumeration, dependency changes and stale compile/runtime restore, source immutability, every authored anchor at 1/2/4/8, deterministic roundtrip, null/missing/default/duplicate/malformed/unsupported placement data, direct invalid/reflected/nonfinite frames, and field diagnostics.

These passing branches do not mean every malformed transform is safely refused: the full review found a separate shared JSON-vector decoding defect, including a socket-axis position path. The entire malformed-input admission boundary therefore remains unqualified.

## INDEPENDENT REVIEW

Two independent reviewers inspected the complete boundary and actively sought further defects. Each independently executed a counterexample through the freshly built catalog loader/compiler. Root then retained and ran permanent witnesses in `ModularCraftTests.AdmissionAudit.cs`.

**Additional defect A — physical consistency tolerance.** `PartStandard.Near` uses `1e-10 * max(1, |a|, |b|)` for volume consistency. At small volumes, that absolute floor permits very large relative errors in the definition's own density/geometry/capacity relationship.

Permanent ordinary-density witness: density 1,000 kg/m³, radius 1e-5 m, length 3e-5 m, volume 9.424777960769381e-15 m³, correctly reconstructed spatial tensor. Density × volume is 9.424777960769381e-12 kg; declared capacity is 9.424777960769382e-9 kg, **1,000 times** that amount. The full catalog compiler accepts it and its saved bytes reload identically. A second adversarial density of 1e20 kg/m³ amplifies the same error: declared 1 kg versus geometric 942,477.796 kg is accepted. Neither witness proposes game balance or a new spacecraft definition.

The independent reviewer also executed a density-1,000 case with capacity 1e-7 kg, **10,610 times** its physical law. This is definition self-inconsistency, not a future trajectory qualification failure.

**Additional defect B — malformed vector decoding.** The custom vector reader checks that an object has three fields, then directly requests x/y/z. Replacing z with bogus preserves the field count but throws `KeyNotFoundException`, outside the structured `InvalidDataException`/`JsonException` refusal boundary. The reviewer independently changed only `standard.collision[0].vertices[0]`. Root reproduced the same cause at both `construction.asset.materialOrigin` and `standard.socketGroups[0].axis.position`. The defect is in the existing shared reader, separate from the corrected null placement-record guard.

No additional production correction followed either finding. Independent source hashes match the final candidate and are retained in the exception seal.

**Architecture judgment:** the evidence warrants a bounded revision of numerical consistency admission and structured decoding contracts, rather than another arbitrary tolerance tweak or scattered exception patch. Physical comparisons need declared scale/range/error requirements; custom decoding must have a complete controlled-refusal contract. The evidence does not invalidate the accepted six-definition vehicle architecture or require a second compiler/editor/runtime owner. Such revision is not authorized by this closure and was not implemented.

## REGRESSIONS

Relevant prior construction Stages 1–8 all PASS (including exact fuel 2,075 checks plus 400 independent oracle cases; power 90; runtime 93). SRV-01 integration PASS, with the 16,260-check four-horn ownership suite and 24 independent trajectories. M15.5 live command authority 1,557, pilot demand 2,939, and pilot allocation 36,768 checks PASS. Existing exact zero-allocation admission/service witnesses remain 0 B.

Full concise output, elapsed time and process results are in `gate1-exception-results.json`. The admission-audit nonzero result is retained as FAIL, not converted into a passing gate because it was expected after diagnosis. No test expectations were weakened.

## BUILD STATUS

Managed Debug and Release builds of `tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj` and its Core, EphemerisFormat and Simulation dependencies PASS with zero warnings/errors. Source and output DLL hashes are sealed. No full native/graphics application build or integrated Player route is claimed.

## GATE 1 JUDGMENT

REVISE — STOP FOR PROJECT CONTROL. The two original witnesses are closed, but the complete Gate 1 suite/review is not clean. The malformed socket-input requirement cannot receive blanket acceptance while its shared vector reader still has the newly demonstrated escape. Gate 1 has used its initial correction plus the explicitly authorized additional cycle. No third correction was attempted.

## NEXT GATE OR STOP REASON

Gate 2 remains NOT STARTED because the explicit promotion rule requires stopping on any newly proven independent Gate 1 defect. Gates 2–12 remain unrun. No commit, tag, push, bank, milestone or Player PASS. KSA writes 0; no unresolved equivalent KSA mechanism was introduced beyond the already accepted owner/schema plan.

## Reproduction and preservation

From `E:\NovaCore`:

```powershell
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --no-restore
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1-redteam
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1-closure
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate1-admission-audit
```

The final command must currently fail and print the physical inconsistency and malformed-vector witnesses. This preserves the reproduction ability without retaining bulk generated input.

`gate1-exception-seal.json` records source/evidence/build hashes, preservation verification and storage. Original Gate 0 and first-stop files remain historical records; they were not silently rewritten. Pre-correction source recovery: `build/modular-craft-first-playable/gate1-exception/pre-correction-source.zip`, 67,502 bytes, SHA256 `756882c2c6d8b842f5bfef5f40a6f03b0796056b905ad045f774702f472c94ec`. No cleanup performed or authorized. Permanent evidence remains under the existing 2 MB budget; rebuildable diagnostic logs/recovery are separately inventoried in the seal.

Preservation verification found HEAD, branch/tag/remote refs, index, worktree registration and unrelated files unchanged. The complete `show-ref` set differs only under `refs/codex/turn-diffs/`: two old capture/checkpoint refs are absent and two new capture/checkpoint refs exist. Exact before/after entries are retained in the exception seal. Their update mechanism was not investigated; this task issued no ref-mutating command and did not restore or remove tool-owned refs. The old referenced object still resolves. Therefore a blanket claim that every ref remained identical would be false; this observation does not reopen Gate 0.
