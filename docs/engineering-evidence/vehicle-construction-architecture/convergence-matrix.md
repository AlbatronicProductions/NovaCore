# Convergence decisions

| Responsibility | Decision | NovaCore reason / delta |
|---|---|---|
| Reusable templates and repeated instances | ADOPT | Existing PartDefinitionData/PartInstanceData evolve; one shared engine definition, distinct placements |
| Immutable definitions, explicit revision and content hash | ADAPT | Existing deterministic catalog identity is stronger than mutable KSA load-time template overlays |
| Functional subparts | ADAPT | Preserve owner and meaningful moving/physical children; stable parent-local identity, no ID for every mesh |
| Connections/tree + independent service edges | ADOPT | Explicit endpoints/compatibility and future partition boundaries |
| Default capabilities | ADAPT | User explicitly requires independent opted-in service domains; DLV data YES, power/propellant NO at detachable boundaries |
| Editor machinery | ADAPT | Shared design/compiler, isolated mutable draft; KSA can retain live tree, prohibited by NovaCore canonical authority |
| Stock/player convergence | ADOPT | Same canonical design and runtime compiler; no vehicle-name branch |
| Save/runtime identities | ADAPT | Stable design-local IDs; separately fresh runtime capabilities; no runtime dynamic values in design |
| Fuel eligibility and graph lifecycle | ADAPT | Explicit inlets/stores/crossfeed; stable compiled reachability. Exact multi-resource accounting must preserve NovaCore invariants rather than copy floating clamp/partial-mixture artifacts |
| Electrical battery circuits and W/J abstraction | ADOPT | Production stored first, ordinary loads then draw; no direct-feed pool or voltage simulator |
| Electrical deterministic allocation/save | ADAPT | Stable module/store order and any relevant cursor retained in canonical state |
| Command/data network | INTENTIONALLY DIFFER | Installed KSA has no matching connector capability. Explicit user-required data path must not imply power |
| Asset identity/material origins | ADAPT | Accepted GLB hashes, engineering basis and fixed origins; physical authority independent from visual bounds |
| Canonical transactions and camera/control split | INTENTIONALLY DIFFER | Banked NovaCore deterministic publication and capability lifetime are mandatory, independent camera focus cannot select control |
| Staging | ADAPT | Save connection/action ownership only. Physical separation is explicitly outside this campaign |

Independent architecture review: a coherent evolution exists if common construction facts feed the admitted SRV facade and generic static consumer, with all mutable service state under existing SimulationState/transaction ownership. Merely placing another compiler/Step engine beside the old one is not acceptable.

Stage 0 arithmetic decision: extend generic consumption to exact rational Q quantities with a construction-derived finite precision bound. Existing integer SRV quantities remain unchanged. Pure consumption can exhaust each store at most once; no runtime propellant production/refill is admitted here. This permits ordinary unmatched fills and balanced concurrent draws without rounding or mixture leakage. See fuel-arithmetic-contract.md. Independent construction, service and accepted-asset reviews found no unresolved product choice. Stage 0: PASS — PROMOTE.
