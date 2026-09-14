# Changing-mass segmented propulsion and joint publication

**PASS — architecture recommendation, independently reviewed. No production implementation; no milestone assigned; nothing staged or banked.**

Current HEAD/main/origin/main/remote main: `89a432ce3b96a5dcb0751cfdc26bfd6776c883a1`, **NovaCore M14.24: Bank exact finite-propellant segmentation**. Current branch: `codex/finite-propellant-segmentation`. M14.24 annotated tag `m14.24-exact-finite-propellant-segmentation` has object `50aa4e984e6646a9334e4e87356a43ffdf138e73` and peels to that commit. M14.23/M14.22/M14.21 and all historical tags remain protected.

## Decision

Prefer **one bounded coupled free-flight physical evaluator plus one atomic joint canonical publication**, with shared existing quaternion/Euler mathematics and separate eventual contact integration. A private evaluator is an internal prepare stage, not another preview-only accomplishment. Retained BEPU remains the constrained solver when that later lifecycle is qualified; it does not become universal FP32 authority.

The first physical model must explicitly declare a **net effective engine-wrench closure**. M14.23 supplies thrust and moment that already represent the propulsion reaction, so adding another rocket-equation/exhaust impulse would double-count it. M14.24's point reservoir fixes COM and dry inertia, but does not by itself prove zero jet damping for a real off-COM nozzle. The proposed ideal closure resolves that ambiguity without inventing nozzle geometry or silently claiming real-tank fidelity.

The complete future chain is:

    command -> genuine engine proposal -> genuine exact resource segments
      -> private coupled changing-mass motion
      -> prepared joint successor
      -> one canonical resource/actuator/mass/paired-state/time/history commit
      -> fixed private acknowledgement -> copied endpoint.

## Decisive findings

- Current translation integrates constant root force with constant mass. Its paired facade evaluates translation and rotation independently. It cannot implement rotating changing-mass thrust by an endpoint mass edit.
- Use the exact powered/unpowered ordering and a scaled local-time numerical adapter. A bare double duration can become zero while the correct represented impulse is nonzero. Exact fuel/event authority and approximate numerical trajectory are explicitly different contracts.
- Body thrust must rotate at each coupled integration stage. Reuse current Hamilton quaternion/Euler signs and endpoint normalization; never recompute normalized bits during fixed publication.
- Current resource authority has no writer, actual hardware authority is not yet published, and existing paired commit omits mass. Those are concrete joint-publication implementation requirements, not reusable capabilities already present.
- M14.23 source identity includes the entire clock/debt state. New host credit must precede sealing; retryable pending endpoints cannot silently absorb later debt mutation.
- Later banked resource evidence chooses **Enabled + NoFeed** after starvation, superseding the older proposed Off-on-feed-loss policy. ActuatorRevision still advances once per applied interval/frontier; ResourceRevision advances only for exact resource change.
- A powered endpoint needs canonical **endpoint-only validity**. Merely holding the scene's display does not prevent existing lower-level constant-force queries from extrapolating it.
- Exact BEPU source supports in-place inertia/mass updates, but does not prove warm-start rescaling or continuous changing-mass contact accuracy. Its velocity callback also runs for discarded prediction work; no resource/hardware mutation belongs there.

## Evidence map

| Record | Retention purpose |
|---|---|
| [Current NovaCore map](current-novacore-dynamics-map.md) | Reusable math, source gaps, owner/clock/contact anchors |
| [Variable-mass equations](variable-mass-equations.md) | System boundary, no double counting, point-reservoir closure, analytical oracle |
| [Segmented integration contract](segmented-integration-contract.md) | Exact rational ordering, scaled numerical strategy, error and work bounds |
| [BEPU/contact assessment](contact-bepu-assessment.md) | Pinned API/source behavior and explicit remaining qualification |
| [Atomic publication contract](atomic-publication-contract.md) | Complete successors, revisions, temporal validity, source leases, failure linearization/backlog |
| [KSA convergence](ksa-dynamics-convergence.md) | Current source versus retained official history; adopt/adapt/differ |
| [Candidates and first physical ticket](architecture-candidates.md) | Three candidates, winner, bounded implementation/visible qualification bar and Florida chain |
| [Arithmetic witness](arithmetic-witness.py) | Reproduce exact split, old-mass error and tiny-duration counterexamples without production simulation |
| [Verification](verification.md) | Independent red team, resolution and preservation checks |
| [Identity](identity.json) | Baseline/ref/source/evidence fingerprints and evidence-budget accounting |

## Reproduction and limits

Read the named current source anchors at the recorded commit; inspect exact pinned BEPU URLs in the contact assessment and local KSA files at their recorded hashes. Existing banked qualification reports are historical evidence, not new results. The memory of earlier UNBANKED reports does not override current M14.24 refs.

Run `python docs/engineering-evidence/powered-dynamics-architecture/arithmetic-witness.py` from the repository. It uses standard-library Fraction/Decimal, emits a concise algebra witness and changes no files. It is not a prototype integrator, production test or benchmark.

No build, full-suite, allocation, contact, rendering or manual acceptance campaign ran in this architecture ticket. Future scaled numerical arithmetic, physical error envelope and powered-contact continuity remain implementation/qualification obligations. No current numerical performance or new zero-allocation claim is made. The banked resource preparation cost is reported in the candidate discussion only to prevent unsupported headroom claims.

Permanent evidence budget: **160 KiB**, consisting only of bounded reports, this algebra witness and identity metadata. No downloaded source tree, raw trace, dump, temporary build tree or binary is retained. Existing build/reference inputs remain untouched. Blender was not used.

Recommended next action is Project Control review of the [first physical ticket](architecture-candidates.md). Do not implement automatically, bank, assign a milestone, start powered contact or begin Florida launch.
