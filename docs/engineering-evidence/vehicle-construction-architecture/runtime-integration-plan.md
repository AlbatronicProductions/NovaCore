# Existing owner integration trace

Independent read-only review found a narrow shared-owner path. This is a Stage6 implementation plan, not qualification.

Implementation/qualification now completed: see runtime-instantiation.md. The following is the retained preimplementation trace.

- Add optional generic construction bindings and immutable service snapshots to the existing SpacecraftStateStore. Keep existing SRV assembly fields and closed admission intact.
- Add the service publication partial to the existing SimulationTransactionEngine. SimulationState remains the one canonical state, with its existing revision and clock. A session only composes these owners.
- Reuse ContinuationPublicationPhase.TryEnter/IsOwnedBy and existing clock preparation/install seams. Calculate fuel, power, bounds, target time and next revision before the first assignment. Commit one joint successor with no callbacks or rejecting work after the first write.
- Capture current state/revision, clock time/debt/rate/remainder/pause and timeline revision. Reject foreign, retired or stale capabilities. Serialized identities are observations, not reconstructed authority.
- Registration must exclude generic static slots from legacy motion/contact/torque/command ingress, including IsInstantaneous and TryPrepareAppliedSlot. A generic static construction is not an admitted physical-flight profile.
- Saved design remains separate from dynamic state. Restore through the same owner and replay bounded service commands; compare reproduced snapshots. Preserve energy/fuel joint bounds and cadence. Editor drafts cannot mutate live state.
- A single admitted static construction per proving session is the bounded first integration. Multiple independent service clocks in one state are prohibited. Future multi-craft publication must advance affected slots together.
- Retire physical runtime authority even when no optional control capability was issued. Old tokens fail after disposal or restore.

Required tests: stock/player convergence; independent inventories; wrong-thread, foreign/stale/disposed authority; expired views; state/clock/timeline conflicts; overflow/history-capacity and combined fuel/power failure without partial commit; replay continuation; legacy dynamics ingress in both directions. This does not admit DLV flight, staging or contact.
