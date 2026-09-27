# Campaign report — Gate 1 stop

## WHAT HAPPENED

Gate 0 completed. A partial Gate 1 candidate added the Part Standard schema, typed interface/service/physical contracts, catalog/2 admission and permanent validation tests. Qualification demonstrated legacy-schema leakage; the sole bounded correction now explicitly refuses Part Standard extensions in the closed stock assembly compiler. Its permanent witness passes.

Independent review then demonstrated two separate remaining responsibilities needing correction: interface-only resource resolution/dependency sealing, and malformed socket-placement refusal. Both are executable failures. Per the authorization's one-correction-per-gate limit, implementation stopped. No second rescue was attempted; no later gate promoted. Successful baseline tests are not reported as a full Gate 1 PASS.

## CURRENT FINAL REPO STATE

HEAD remains `8c189b28ce2a68f97de734d1589acb500c41fd99`, branch main. No commit, staging, tag, push, deployment or banking. `entry.json` and `closure.json` retain Git/index/worktree identity and precise changed/added file hashes. Pre-existing modified/untracked bytes are recoverable in the verified ZIP; unrelated work remains unchanged. No deletion or cleanup occurred.

Source changes relative to campaign entry are confined to `AssemblyDesign.cs`, `AssemblyConstructionDefinitions.cs`, new `PartStandard.cs`, the test dispatcher and new `ModularCraftTests.cs`. Current construction work remains unbanked. No new vehicle assets were created.

## GATE 0–12 RESULTS

| Gate | Result |
|---|---|
| 0 — entry | PASS: 3,322 paths sealed; 205 pre-existing modified/untracked files byte-verified in recovery archive |
| 1 — Part Standard | FAIL / STOP: one correction used; two independent unresolved findings |
| 2 — CraftDocument | NOT RUN |
| 3 — transactions | NOT RUN |
| 4 — six definitions | NOT RUN; planning numbers independently reconstructed only |
| 5 — player construction | NOT RUN |
| 6 — save/reload | NOT RUN |
| 7 — compiler | NOT RUN |
| 8 — physical/services | NOT RUN; independent derivations retained as guidance only |
| 9 — Florida admission | NOT RUN |
| 10 — short flight | NOT RUN |
| 11 — long flight | NOT RUN |
| 12 — integrated qualification | NOT RUN |

## PART STANDARD IMPLEMENTATION

Partial candidate: distinct `novacore.part-standard/1` and `novacore.construction-catalog/2`; legacy catalog/1 refuses the new extension. Typed mechanical classes, mate roles/clock policies, authored collision/support provenance, concentric store volume/tensor laws, explicit service ports/routes, socket-group placements and configuration policy are represented and validated. Nonsemantic ordering is canonicalized. Old serialized definitions omit the null extension, preserving existing content identities.

Known failures make this candidate unqualified: unresolved interface-port resources are accepted, port-only resource revisions are omitted from dependency closure, and null socket-placement entries cause `NullReferenceException`. See `redteam.json` and permanent failing `--modular-gate1-redteam`.

## CRAFTDOCUMENT / EDIT TRANSACTIONS

Not implemented in this campaign. The existing editor and design/runtime separation remain intact. No migration, undo/redo or save-policy acceptance is claimed.

## SIX DEFINITIONS

No six-definition production catalog or greybox assets were authored. Existing development assets remain unchanged. The schema-only qualification fixture is test data, not a seventh spacecraft definition.

## SHORT / LONG QUALIFICATION

Independent planning arithmetic reproduced dry/wet 504/1304 kg and 564/2164 kg, TWR 2.40146/1.44709 and the accepted tank/inertia/support calculations. These are planning feasibility observations only; neither as-built craft nor physics execution exists yet.

## 8× SYMMETRY

Partial schema represents generic authored placement sets for 1/2/4/8. No player placement, atomic commit, compiler or runtime symmetry was implemented. Malformed placement validation is one demonstrated unresolved Gate 1 failure.

## VARIABLE-MASS QUALIFICATION

NOT RUN. Independent mathematical review found a viable extension of existing co-moving removal with a complete effective wrench, and distinguished rigid-point COM velocity from the derivative of the moving centroid. The proof obligations and BEPU refresh/event cautions are retained in `independent-review.md`; no dynamic or contact PASS follows from them.

## 32-JET CONTROL QUALIFICATION

NOT RUN. Independent geometry and ownership review identified the 32-bit representation boundaries and pure-couple oracles. Existing 16-jet controls remain unchanged and passed regression.

## FINITE-POWER QUALIFICATION

NOT RUN. Independent review derived the exact interior cutoff witness: 0.5 J / 56 W = 1/112 s, consuming 5/56 kg at 10 kg/s before coast. Rational duration/resource proof extensions remain unimplemented. Current power semantics were preserved and passed old Stage 4 regression.

## FIT / FUNCTION / ADMISSION

No combined readiness or launch claim exists. Gate 1 checks schema facts only; electrical connectivity, physical operation and launch admission remain separate future gates. No visual or structural fact grants operational authority.

## CRAFT COMPILER / COMPILEDCRAFT

Not implemented. Existing generic static compiler/runtime binding remain unchanged; their original bounded regression passed.

## FLORIDA SUPPORT / HANDOFF

Not implemented or exercised. Existing M15.3 ownership remains untouched. Adapter feasibility calculations do not qualify contact or departure.

## M15.5 CONTROLS

Unchanged. Existing canonical live-command authority, pilot-demand and pilot-allocation tests passed, including exact zero-allocation witnesses. No generic craft controls claimed.

## M15.4 CAMERA

Unchanged. No camera production file was edited; no new camera or player follow route was exercised.

## SHORT FLIGHT

NOT RUN. No ignition/ascent/cutoff/coast or player route exists for the new craft.

## LONG FLIGHT

NOT RUN. No second compiler/simulator/launch path was introduced.

## REGRESSION

Managed Simulation.Tests Debug and Release builds PASS, zero warnings/errors. Part Standard baseline PASS: 33 checks after the one correction. Prior construction Stages 1–8 PASS, including 2,075 exact-resource checks with 400 independent oracle cases, 90 power checks, 93 runtime checks and 62 article checks. SRV integration/24 independent trajectories, command authority (1,557), pilot demand (2,939), pilot allocation (36,768) PASS. Full outputs are bounded in `gate1-regression.json`.

The separate new red-team branch FAILS intentionally on real unresolved defects. No expectation was relaxed. No whole-solution/native graphics, launcher, physical flight, camera or integrated Gate 12 pass is claimed from these focused regressions.

## PERFORMANCE

No new editor/compiler/render/flight acceptance measurements were run after the Gate 1 stop. Existing regression witnesses preserved exact 0 B pilot admission/service allocation. Existing pilot-demand timing: median 0.0017 ms, P95 0.0034, P99 0.0035, max 0.0178, 2,048 samples; allocation service: median 0.0903, P95 0.1412, P99 0.1879, max 0.2491 ms, 1,024 samples, GC 0/0/0. These are old qualified workloads, not new Part Standard or gameplay performance qualification. No budget or headroom was invented. Storage inventory appears in `closure.json`; the evidence package budget is 2 MB.

## RED TEAM

1. Closed legacy schema accepted new Standard field: demonstrated, corrected once, permanent refusal PASS.
2. Interface-port-only species not resolved or included in used-resource identity: demonstrated, uncorrected.
3. Null authored socket placement raises uncontrolled exception: demonstrated, uncorrected.

Items 2 and 3 have different causes from item 1 and from each other. Combining them into one broad rescue would violate the one-causal-issue correction rule. The remaining campaign red-team cases depend on unimplemented gates and were not falsely marked tested.

## ENGINEERING JUDGMENT

REVISE — STOP / ESCALATE AT GATE 1. Not ready for integrated or manual acceptance. The original accepted architecture remains the design basis; no architecture restart or new production front was opened.

## INTEGRATION JUDGMENT

REVISE / NOT QUALIFIED. Integration Gates 5–12 were not reached. No playable candidate was deployed.

## MANUAL PLAYER ROUTE

Unavailable for this candidate. No manual Player PASS requested or self-declared. Existing accepted routes are preserved but do not stand in for new-craft acceptance.

## WHAT HAPPENS NEXT

STOP FOR PROJECT CONTROL. The two independent Gate 1 defects require further narrowly scoped correction authority before the campaign can continue. No milestone number assigned; no commit/tag/push/deployment/banking. KSA writes 0. The latest KSA clarification remains in force for genuinely equivalent unresolved responsibilities on any authorized continuation.
