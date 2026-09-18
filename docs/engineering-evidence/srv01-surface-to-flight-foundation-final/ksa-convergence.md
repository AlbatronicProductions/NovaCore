# Consolidated KSA provenance and responsibility decisions

This consolidates the **direct installed-source and authenticated history inspections
already performed in this front**. No new broad research campaign was conducted.
Installed hashes were rechecked during closeout and still match those inspections.

Root: `E:\Kitten Space Agency`. KSA.dll version `2026.9.10.5438`, product
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.

| File | SHA-256 |
|---|---|
| KSA.dll | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

These identify inspected KSA dependencies, not a change to NovaCore's separately pinned BEPU.

| Responsibility | Direct installed witness | Decision |
|---|---|---|
| Existing canonical assembly admitted into retained native world/body | InitializeConstraintSim 06001C26, AddVehicle 06000686, CreateColliderCompound 06002F1B | ADOPT lifetime, ADAPT bounded source-bound SRV envelope representation |
| Source/staged buffers and ready/apply | Prepare 06001BBC, GetNewProps/GetReadOnlyProps 06001C46/47, ApplyResultsToMainThread 06003115 | ADAPT; retain NovaCore exact resource/revision/history/final mutable checks and same-phase ack |
| Source mass through constrained solve, updated properties afterward | FullPhysicsConstrainedStep 06001C19, ConsumePropellantFromActiveNozzles 06001C5B/06001B46, CopyToBepu 06001B40 | ADOPT mass cadence, ADAPT exact balanced finite withdrawal/exhaustion and atomic successor |
| Support status, interval consumer and native lifetime are separate | FullPhysicsEndFrame 06001C03, FullPhysicsStepUntil 06001C02, CertifyTerrainClearance 06001C05, ReturnConstraintSim 06001C25 | ADOPT separation, ADAPT qualified fixed-slab transfer; no copied pooling timer or generic recontact |
| Finite authored storage/feeds/resource mass | PartTree propellant/inert aggregation 06001AB0/1/4; RecomputeMassProperties 06001B43; retained Stage4 full ownership map | ADOPT finite species/feeds; ADAPT exact two-store law; INTENTIONALLY DIFFER from live partial-withdrawal handling to protect exact balanced exhaustion |
| Site-local collider and visual transforms | BuildCollisionShape 06002854, AddColliders 06002855, BoxColliderTemplate.CreateShapeInto 060013A5, ResolveLaunchPads 06000691, UpdateStaticObjectCollider 06000692, GetAxesCcf 0600034F | ADOPT explicit physical shape/shared site pose; ADAPT authenticated NovaCore finite slab and canonical frame |

Actual history source: authenticated Discord **live-changelog**,
https://discord.com/channels/1260011486735241329/1260112103134724146.
Retained direct-review records include source tokens, dates, message links and decisions:

- [Admission source map](../srv01-supported-contact-admission-design/ksa-current-owner-map.md)
  and [history](../srv01-supported-contact-admission-design/ksa-live-history.md).
- [Stage2 source-mass/ready lifecycle](../srv01-surface-to-flight-gauntlet/stage2/closure/ksa.md):
  history4428/4549/5174/5434, later5441–5448 separately identified.
- [Stage3 source/history](../srv01-surface-to-flight-gauntlet/stage3/allocation-closure/README.md):
  4646/4659 support versus consumer;5173/5174/5177 stale bounds, false landed state and once-only jet consumption.
- [Stage4 complete finite-resource chain](../srv01-surface-to-flight-gauntlet/stage4/ksa-chain.md):
  current live owners distinguished from planner; finite containers, mixture, mass/tensor and ready/apply history.
- [Final slab source/history](../srv01-surface-to-flight-gauntlet/stage5/simplified-slab/ksa-convergence.md):
  5091/5095/5330/5338/5366/5382/5398 static/site/collider changes; assets/configuration hashed.

Newer live-history entries (through5450 in the slab review) are **engineering history**,
not assumed installed5438 behavior. No bulk decompiled source, authored KSA mesh/texture,
vehicle balance or chat dump belongs in the bank. No KSA installation is a runtime dependency.
