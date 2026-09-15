# Concrete bridge responsibility audit — NOT IMPLEMENTED

## Available seam and why the simple proposal is insufficient

Current NovaCore LocalContactWorld retains body/manifold state and runs
ordinary admitted canonical intervals with SolveDescription(8,1), but its
prepared source acceleration and mass do not implement event chronology.

The pinned BEPU [DefaultTimestepper](https://raw.githubusercontent.com/bepu/bepuphysics2/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/DefaultTimestepper.cs)
orders prediction/detection before Solve; its stage callbacks do not add
an event-aware constrained operator. The pinned
[PoseIntegrator callback contract](https://raw.githubusercontent.com/bepu/bepuphysics2/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/PoseIntegrator.cs)
allows repeated prediction/substep calls and discarded lanes. Resource
consumption cannot live in those callbacks. This is direct upstream source
inspection, not a new claim about KSA's design intent.

Supplying a single average force is not this chronological map: velocity,
displacement, support redistribution and friction work need different
temporal moments. Running stock Solve(H) and overwriting its endpoint with
C either discards its response or counts contact/propulsion twice unless
an additional ownership map exists. Post-solve impulse is already rejected.

## Concrete conditional design, with unresolved installation gate

1. Retain one BEPU world, body, feature identities and outer-H manifolds.
2. Prepare one exact owner ledger and event-integrated mass/force/torque and
   displacement moments; no canonical/resource mutations in callbacks.
3. After ordinary detection, an exclusive custom constraint block handles
   only an admitted fixed contact set. Suppress stock duplicate advancement
   for the same body/rows; ordinary BEPU dt remains float(H), never float(h).
4. That block computes the complete constrained endpoint and aggregate-H
   reactions. It must reconcile the banked compliant/friction law, finite
   iteration behavior, orientation and manifold changes. This ticket's rigid
   analytical kernel does NOT do that reconciliation.
5. Install successor scalar mass and fixed dry inertia at the owner boundary,
   body endpoint and compatible aggregate-H cache under one prepared owner;
   preserve producing feature/basis identity. Prove next ordinary step uses
   those caches correctly and cannot apply propulsion twice.
6. Canonical motion/resource/actual-engine/debt/history commit remains the
   NovaCore transaction, with private poisoning after unsafe partial apply.

This is a CUSTOM CONTACT SOLVER/block inside retained BEPU ownership. It is
a concrete responsibility allocation, not a proven stock-BEPU bridge. The
cache installation/next-step/compliance map is UNPROVEN. A force callback,
moment buffer or custom ITimestepper name does not remove that obligation.

## Exact remaining costs

| Responsibility | Conditional C disposition |
|---|---|
| Body/manifolds/warm starts | retain ordinary world; new compatible cache map required |
| BEPU dt | ordinary H; no standalone h solve |
| Chronology | one prepared kernel per outer interval, not per display frame |
| Mass | continuous law in kernel; successor property install; no source/midpoint shortcut |
| Basis/producing lever | still needed across refreshed geometry; frozen proof says nothing about retirement |
| Duration scaling | no event-h intermediate cache; outer-interval duration provenance remains |
| Load correction | custom variable-mass reaction law still needed; general equivalent UNPROVEN |
| D | UNRESOLVED; not removed |
| Iterations | analytical rigid kernel uses none; stock8/old12 correctness cannot be inferred |
| Private state | immutable ledger + admitted geometry, input state, temporal moments and one outer endpoint; no required event endpoint |

## Work and payoff versus A

Standalone C: zero backend calls; one logarithm; fixed scalar moments/work
primitives; no dense inverse or iterative sweep; one endpoint. Reference:
two analytical phases,17 rational acceleration terms plus polynomial work.
Controls: one ordinary constant-coefficient analytical step; post control
adds one endpoint impulse. These are mechanism counts, NOT production timings.

Conditional C bridge requires one outer contact pass plus a custom solver
and compatible cache export. A's retained operator currently uses its basis,
duration/load/D mechanisms and12 sweeps. Prior numerical preparation medians
are59.8/60.4/59.5us (max122.5/119.2/123.4us), excluding complete world and
publication; no new campaign. RemovingD saved~.3us median and did not justify
retirement. We do not compare those C# timings to Python proof runtime.

C removes the need for a tiny-event backend cache but has NOT demonstrated
a cheaper general contact law or installation map. It shifts much of A's
responsibility into a custom contact solver. Realistic advantage over the
cheaper average control is below all preregistered material bars; real
rotating/compliant gameplay benefit is additionally unqualified.

BRIDGE: UNPROVEN. PAYOFF OVER A: FAIL TO ESTABLISH. No production bridge
or revised8/12-sweep claim is authorized by these results.
