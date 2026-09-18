# Stock Florida responsibility and current KSA comparison

Project Control's SRV-01 role decision separates the spacecraft from its future Earth launch stack. This restart uses the unchanged seven-part stock design (705 kg, 30 kg fuel, 45 kg oxidizer), engine and RCS OFF. Stage 6 remains CLOSED. No launch-stack, booster, SAS or navball implementation is part of this work.

The old Stage-4 numerical profile (24,798.239377037527 m/s, 2,018,900 kg wet) is **SUPERSEDED DEVELOPMENT SIZING EVIDENCE**. Its qualified resource, command, propulsion, exhaustion, mass-property and publication mechanisms remain preserved. The later approximately 11,488.705 m/s / 26,520 kg calculation is **FEASIBILITY EVIDENCE**, not adopted vehicle configuration. Neither profile is Florida physical authority. Their reports and exact inputs remain unchanged.

## Direct current installation and authenticated history

Inspected installation: `E:\Kitten Space Agency`. All eight installation identities in [the preceding source map](../rotating-florida-contact-closure/ksa-source-history.md) were rechecked and match. KSA.dll remains product `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`.

A read-only reviewer directly re-inspected the installed methods for coordinate transport, terrain coordinates, result ownership and retirement. The lead reopened the authenticated Discord `Kitten Space Agency / live-changelog` channel and searched `in:live-changelog CCF`. This was actual authenticated channel content, not NovaCore's prior description of it.

Relevant directly re-read history:

- [5429](https://discord.com/channels/1260011486735241329/1260112103134724146/1547728308756676691): fictitious forces follow the actual CCF physics frame, including a vehicle above the usual radius in another vehicle's bubble.
- [4867](https://discord.com/channels/1260011486735241329/1260112103134724146/1524584112093007943): corrected angular-velocity transport across CCI/CCF transitions.
- [4848](https://discord.com/channels/1260011486735241329/1260112103134724146/1523958088384643174): freefall may retain a CCF physics frame; physical consumer and coordinate frame are distinct.
- [5327](https://discord.com/channels/1260011486735241329/1260112103134724146/1539694656890077246): nearby terrain precision uses CCF anchors and local offsets.

Live history includes newer 5448/5449 ground-clutter work; it is not represented as code present in the inspected 5438 installation. No evidence of those changes replacing the directly inspected vehicle-frame/ready-apply methods was inferred.

| Responsibility | Current installed mechanism | NovaCore disposition |
|---|---|---|
| Full frame transport | `PhysicsStates.CcfStatesToCci` 06001B74 / inverse 06001B78, `ConvertTo` 06001B6D: position, velocity with spin, orientation and angular rate | ADAPT: exact epoch and canonical typed endpoint; independent matrix derivative oracle |
| Physical terrain coordinates | `TerrainPatch.Initialize` 06000651 / `GetLocalTerrainVertex` 06000694: CCF anchor and local basis | ADAPT: existing terrain-v5 grading capability, bounded 16 m full-weight patch |
| Ready/apply ownership | `PublishResults` 06001BF8, `ApplyResultsToVehicles` 06001BEA, vehicle apply 06002F31; failure 06001BF5 | ADAPT: retained native owner, exact canonical transaction and acknowledgement |
| Required frame/parent changes | `RemoveEligibleVehicles` 06001BEF removes old binding; full state conversion precedes next ownership | ADAPT as architecture provenance; this engine-OFF route does not execute rotating-site departure |
| Contact-specific fictitious force changes | `ComputeDerivatives` 06001B8F radializes centrifugal acceleration and omits Coriolis during contact | INTENTIONALLY DIFFER: preserve the qualified complete rotating-frame equations; no copied support correction |
| Terrain angular resistance / sleep | Pose callback uses terrain impulse resistance; `UpdateVehicleFromSim` 0600068B may place sleeping constrained engine-OFF craft on rails | INTENTIONALLY DIFFER: this qualification requires 1,200 actual retained native steps and independent support bounds |

No KSA source/assets were copied into NovaCore and no installation files were written.

## Handoff compatibility limit

The accepted Stage-3 nonrotating transfer is preserved and revalidated. This restart checks active Florida refusal of the free-flight/departure service, canonical/native nonmutation, and full inertial endpoint representation. It does **not** qualify executable rotating-site departure. `CreateFloridaSupported` has a Site and no Departure; Stage 3's prescribed-separation fixture has a Departure and no Site. Combining them would be wrong: the existing post-transfer evaluator assumes constant local gravity, and clearance assumes a nonrotating plane. No such combination is admitted.

## Reserved downstream work

After Stage-5 acceptance, Project Control may open **DEVELOPMENT EARTH-LAUNCH STACK ARCHITECTURE**. It must inspect the complete current KSA assembly/staging/feed/mass/identity/ownership chain and live history before selecting a launch concept. The separate future navigation/control front covers SAS, reference markers, navball, actuator allocation and player override. Neither front is opened here. Passing stock support says nothing about future booster/stack loading, contact or structural suitability.
