# KSA source, history and tradeoff

## Actual installation identity

Read-only current root: E:\Kitten Space Agency. KSA build
2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9.
Lead and independent reviewer rehashed actual installed files this ticket:

| File | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4936792 | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| KSA.deps.json | 73752 | 23FCAA825C40F828349310C8DC480F292F9BE65DE6C4D378B36FBA64F7EE37B3 |
| BepuPhysics.dll | 880640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

This ticket explicitly permits accepted current KSA findings to be reused.
We reuse the prior direct decoded installed-5438 method evidence in
[ksa-current-lifecycle.md](../powered-contact-event-lifecycle-convergence/ksa-current-lifecycle.md),
including method tokens/hashes and resolved helper closure. No new KSA
mechanism claim is inferred from hashes. Historical 5402 decompiled source
is NOT relabeled current. No new KSA code copy, decompilation tree or run.

## Source-proven mechanism

FullPhysicsConstrainedStep 06001C19 prepares derivatives, performs ordinary
inner constrained slices <=1/60, then consumes propellant/recomputes staged
properties at the selected outer-substep boundary. CopyToBepu 06001B40
uploads staged inverse mass/inertia before that loop. IntegrateVelocity
060006BD uses prepared disturbances, current orientation and staged inverse
mass/inertia. ResourceAvailable 06002507 tests positive stores without
dt/demand. Combustor.ConsumePropellant 0600153B discards actual-consumed
feedback; Mole.ConsumeStored 0600149C clamps insufficient mass to zero.
Thus shortage need not shorten the already-completed physical response in
this traced liquid path. Command-time modifier and solid cutoff are separate
paths. Current pair-clearance horizons do not establish fuel-event horizons.
Do not generalize this to 'KSA never splits' or 'once per display frame'.

The KSA-like control isolates that full-prepared-wrench/frozen-source-mass
cadence for one outer interval. Its rigid ODE uses the same geometry as C
to avoid mixing lifecycle and numerical-solver error. It is NOT execution
of KSA, a performance comparison, or an emulation of its entire resource
network, controller, compliance, gyroscopic integration or pose integrator.

## Actual retained history; reused accepted direct inspection

Source is the real Kitten Space Agency live-changelog Discord channel
[1260112103134724146](https://discord.com/channels/1260011486735241329/1260112103134724146).
The previous gate directly read it through authenticated UI on 2026-09-14;
that accepted inspection is reused, not claimed freshly repeated here.
[Retained history record](../powered-contact-event-lifecycle-convergence/ksa-live-changelog.md).

June 5 revision4549 / message1512355160775721020 describes derivative and
propellant cadence across collision steps. August5 revision5177 /
message1534637795740750058 records excess consumption/chatter/overshoot
from repeated jet commands within physical updates. September13 revision5434 /
message1548873113746411602 concerns routing/feed/availability/drain fixes.
These are engineering history and explicit defect concerns, not independent
proof of current code behavior. The accepted current5438 source reconciles
the corresponding cadence and routing categories; not every historical fix
is asserted runtime-qualified.

RATIONALE UNPROVEN for the exact liquid shortage/changing-mass accuracy
tradeoff. No claim that KSA intentionally chose a particular error budget.
Simpler ownership, ordinary timesteps, retained manifolds and predictable
work are engineering implications of the observable structure, not invented
KSA motivations. Exact NovaCore event ordering and atomic publication are
different product authority requirements, not an assertion of superiority.

## Narrow current NovaCore/source checks

EnginePreparation.cs computes binary64 thrust/exhaust flow; resource
segmentation imports finite amounts into exact units. PoweredFreeFlightTests
has dry8/I2/default8N/exhaust5120; its9000N straight test is not our realistic
payoff evidence. EngineeringContactArticle is a different1000kg body; its
1200N resource-proposal test is non-actuating. LocalContactWorld retains
Simulation(SolveDescription(8,1)), source mass/inertia and source acceleration.
Its next-canonical-target Step has no chronological changing-mass kernel.
No architecture or source was changed.
