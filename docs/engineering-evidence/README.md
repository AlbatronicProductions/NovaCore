# Engineering history and evidence

These are **historical/provenance packages**, not runtime inputs or competing
architecture specifications. Start from [current state](../NOVACORE_CURRENT_STATE.md),
[architecture](../architecture.md), [repository structure](../repository-structure.md)
and the relevant operational guide. A report's dated PASS/FAIL/unbanked wording
records its investigation stage. Follow subsequent accepted conclusions.

| Family | Continuing value | Read the conclusion first |
|---|---|---|
| [M14.2 banked spacecraft translation](spacecraft-translation/README.md) | Physical/time/frame contracts, numerical/replay validation, source identity, KSA ownership, bounded performance and the resolved allocation-accounting closeout. | [Translation contract](../spacecraft-translation.md). |
| [M14.3 contact generation](contact-generation/README.md) | Authored feature identity, exact-time geometry/velocity, canonical readiness, numerical/replay proof and bounded query-inclusive CPU cost. | [Contact contract](../contact-generation.md). |
| [Unbanked contact transaction](contact-response-transaction/README.md) | Atomic paired publication, exact-event impulse mapping, rejection, analytical oracle and complete transaction cost. | [Response contract](../contact-response-transaction.md). |
| [M14.1 surface-point queries](surface-point-query/README.md) | Bounded numerical/readiness proof, cost, source identity and exact raster-parity evidence for the banked physical API. | [Current query contract](../surface-point-queries.md). |
| [Earth-route convergence](earth-route-convergence/README.md) | Manual acceptance, original failures, final physical/presentation causes, hashes, compact visuals, recipes and raw-retirement provenance. | [Earth-route convergence](../earth-route-convergence.md), [production consolidation](../production-consolidation.md), [evidence cleanup](../diagnostic-evidence-consolidation.md). |
| [Repository debt retirement](repository-debt-retirement/README.md) | Original producer/consumer decisions, retired islands, compatibility questions and measured storage. | [Debt review](../repository-debt-retirement.md); later packages supersede its snapshot dispositions. |
| [Compatibility resolution](compatibility-contract-resolution/README.md) | Public-contract boundaries and the original 25-row matrix. | [Contract resolution](../compatibility-contract-resolution.md), then [implemented internal migration](../tiny-local-internal-authoring-migration.md). |
| `cache-lifecycle/` | Bounded inventory underpinning banked cache policy. | [Cache lifecycle](../cache-lifecycle-policy.md); [inventory](cache-lifecycle/inventory.json). |
| `graphics-validation/` | Package 1/2 exact environment, caller, dependency and regression proof. | [Package 1](../graphics-window-validation.md), [Package 2](../graphics-validation-package-2.md); current commands live in [build-windows](../build-windows.md#graphics-validation-contract). Package 2 is banked at `9409fec36e72f34f26cd4ab5fe47082e7dcafb20`. |
| [Repository structure](repository-structure/README.md) | Dated responsibility matrix and bounded structural corrections. | Current directory placement remains in [the repository map](../repository-structure.md). |

## Earlier investigations still stored directly under docs

Their paths are preserved for existing links, Git history and archive recipes.
They are historical supporting material, not additional onboarding requirements:

- [P2S5G workload](../M12D-P2S5G-workload-investigation.md).
- Florida [generation-4 seating](../florida-generation4-seating.md),
  [physical/rendered agreement](../florida-physical-rendered-agreement.md),
  [facility support](../florida-facility-support.md),
  [visible terrain investigation](../florida-visible-terrain-investigation.md).
- NCSM1 [KSA responsibility reference](../ncsm1-regional-residency-ksa-reference.md),
  [regional migration](../ncsm1-regional-physical-residency.md),
  [preparation deblocking](../ncsm1-regional-preparation-deblocking.md).
- Facility [light occlusion](../facility-light-occlusion.md) and
  [terrain presentation/material convergence](../terrain-material-surface-convergence.md).

The existing Earth-route archives contain selected clips/images, text, hashes,
source snapshots and numerical summaries. They do not restore a runtime consumer
of removed raw paths. Embedded historical command lines may include subsequently
retired arguments or old validation allowances; use current operational guides
for regression PASS/FAIL. In particular, current Graphics validation never
allowlists the OBS KMT VUID.

Preserve useful conclusions and reproducibility. Do not append bulk capture output
by default. Promotion/cleanup authority remains with
[ENGINEERING_RULES.md](../../ENGINEERING_RULES.md#diagnostic-evidence-lifecycle).
