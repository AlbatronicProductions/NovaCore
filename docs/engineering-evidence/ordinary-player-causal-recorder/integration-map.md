# Minimum recording owner map — proposal, no production change

| Responsibility | Existing owner | Required distinction |
|---|---|---|
| Ordinary startup | `tools/NovaCore.App/Program.cs`, before `PlayerApplication.Run` | Reliable minimum recording admission without an environment flag; visible initialization refusal. |
| CPU frame/update/draw | `native/NovaCore.Native/NovaCoreNative.cpp:2603,2464,2861–2865` | Independent loop ordinal assigned before Update; explicit entry/return/exception, separate from terrain frame. |
| Host callback / message dispatch | `Update` and native message loop | Background heartbeat does not prove either owner progressed; include hidden/minimized suppression. |
| Acquire | `NovaCoreNative.cpp:2467–2470` | Returned result and swapchain incarnation; OUT_OF_DATE can return without submission. |
| Record | `Record`, around `NovaCoreNative.cpp:2167` | Command-buffer identity plus recording incarnation, begin/end result. |
| Submit / fence / idle | Wrappers in `CausalNative.inl` | Attempt versus successful submit; exact queue/fence/record associations; startup signalled fence proves no work; queue/device idle scopes are independently observable. |
| Present | `CausalPresent` wrapper | Call entry/returned result, submission/image/swapchain association; never imply scanout. |
| Error | `App::Check`, before existing diagnostic fault path | Preserve failed result and operation identity without modifying error/cleanup semantics. |
| Incoming generation | `CreateProductionBillboard`, around `NovaCoreNative.cpp:1874–1877` | Current/incoming/prepared generation and resource incarnations must remain distinct. |
| Publication | `InspectProductionBillboardPublication`, around `:2596–2601`; `RegionalPhysicalPreparation.inl` | Publication identity and prior/current resource association after the existing successful fence. |
| Resources | Existing create/free/destroy wrappers | Creation result, independent birth identity, ownership, retirement; bind/map/unmap associations needed for full memory-lifetime claims. |

`a.frame` increments during Upload, after Update entry/fence/managed callback. Existing `causal.frame` is assigned later in Draw. Reusing either as an early CPU-loop ordinal yields stale labels. Instrumentation must add its own identity rather than move existing frame semantics.

Legacy resource-birth identity uses a causal serial that is zero while that recorder is disabled. Minimum recording needs its own incarnation counter and a self-contained checkpoint of still-live resources so historical ring rollover does not erase provenance.

`CausalScope` emits End during exception unwinding. New parsing must distinguish normal return, exception, failed VkResult and marker-only entry. None is interchangeable with GPU completion.

## Perturbation audit required before later PASS

Keep all current Vulkan arguments, feature/extension lists, API version, buffer usage, descriptor contents, resource lifetime, terrain quality, waits and submissions identical. Ordinary minimum recording must not set legacy `causal.Active()`. No new GPU query or readback is justified by minimum host breadcrumbs.

The implemented path must measure absolute emit/call-boundary costs, median/P95/P99 and recurring worst case, allocation counts, retained producer/helper memory and measured write/flush volume. Compare an equivalent CPU/native-mock workload with recording disabled and enabled; label this CPU evidence, not native GPU pacing qualification. Admission/session failures, worker stalls, ring exhaustion, partial writes and parser recovery need permanent adversarial coverage before requesting a benign GPU stage.

No final capacity, cadence or overhead result is selected by this proposal. Those depend on the accepted durability contract and measured event rate, resource limits and persistence service time.
