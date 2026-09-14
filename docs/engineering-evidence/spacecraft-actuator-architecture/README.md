# Spacecraft actuator realization architecture

Architecture investigation only. No production implementation, validation campaign or milestone assignment. Lead judgment and independent review are recorded in [verification](verification.md).

## Result

Prefer **hybrid engine-direct control plus staged attitude allocation and hardware realization**. Engine ignition/shutdown and throttle address engine hardware; attitude/rate/translation requests use a controller and a bounded allocator. Both paths produce checked physical demand. Neither applies rigid-body motion.

Keep five meanings separate: canonical requested command; controller output; actuator command; realized hardware state; physical force/torque/flow output. Proposed realization is also distinct from canonically committed realization.

Recommend exactly one first ticket: **Prepare single-engine actuation from canonical commands**. It establishes a bounded, source-bound command-transition handoff and a sealed single fixed-axis engine proposal, under an explicitly ideal instantaneous, zero-minimum proportional-throttle model. It produces proposed engine state, body force/moment and required mass flow with copied preview observations. It does not publish a live engine trajectory, apply force, deplete fuel or advance canonical actuator state. See the qualification and exclusion bar in [architecture-candidates](architecture-candidates.md).

This restriction is essential: a no-application slice cannot honestly claim that a physical interval committed. Future actuator/controller/resource successors must commit with the corresponding physical interval, under the existing transaction owner. No engine, fuel, pulse timer or controller may advance merely because preparation succeeded.

## Current identity

- HEAD/main/origin/main and queried remote main: `c70ccfacf3607d1342396c702682781b42bb525e`.
- Branch retained: `codex/spacecraft-command-authority`.
- Banked M14.22: `899f11f727205340bdd550dd65a853e253de0ed3`, tag `m14.22-canonical-spacecraft-command-authority`.
- Prior M14.21: `b6e8fa568585706ce38c0609aeb14db4a643123e`.
- All 1,913 pre-existing tracked files and all 62 tag refs are covered by before/after identity checks. The pre-existing untracked staging-build-blocker is preserved separately.
- Current installed KSA: build `2026.9.7.5402`, product suffix `487c3f340de24c6a81037120b6d1129c045c5400`. Installed binary matches retained decompilation provenance; no new decompilation and no claim about newer announced builds.

## Evidence map

| Record | Responsibility |
|---|---|
| [Current NovaCore map](current-novacore-map.md) | Reusable mathematics, command handoff gap, current contact restrictions |
| [Current KSA actuator map](ksa-actuator-map.md) | Fresh source inspection, ownership, actual limits and negative findings |
| [History and convergence](ksa-history-convergence.md) | Official history distinguished from current code; 15-responsibility matrix |
| [Actuator authority contract](actuator-authority-contract.md) | Cadence, state/engine/allocator/fuel/frames, atomic future handoff |
| [Candidates and first slice](architecture-candidates.md) | Three candidates, one winner, one bounded first ticket, Florida dependencies |
| [Verification](verification.md) | Independent attacks, reproduction, scope and preservation checks |
| [Identity](identity.json) | Current refs, source fingerprints, tracked/tag aggregates |

Evidence budget: **96 KiB maximum** for this package. Preserve summaries, references and reproduction ability; no proprietary source, bulk decompile, runtime trace or temporary build tree is retained. Existing banked reports remain intact and are referenced rather than duplicated.

## Scope of conclusion

This recommends architecture; it does not qualify new thrust, fuel, gimbal, RCS, powered contact, departure, Florida integration or control stability. KSA establishes equivalent responsibilities; NovaCore's existing exact timing, refusal, revision and publication witnesses require different commit mechanics. No Blender, rendering, BEPU binary, dependency or historical-tag change was made.

Return to Project Control. Do not implement automatically.
