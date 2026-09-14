# Independent verification and reproduction

Architecture review only, 2026-09-13. Lead sole evidence writer; three read-only investigators covered current KSA input/state, current KSA actuator/physics, and current NovaCore admission plus independent red team. No investigator modified production, tests or dependencies or ran a speculative benchmark.

## Independent red team

| Attack | Source/evidence and disposition |
|---|---|
| Is the KSA path only inferred from names? | Input investigator followed GLFW callbacks, action mapping, buffered drain and worker copy. Physics investigator followed controller outputs into actual core/nozzle state, disturbances and BEPU/Verlet. Independent reviewer spot-checked frame wakeups, core and solid-motor semantics. PASS for the available installed-identity-matched cached source |
| Does one frame's input safely drive arbitrary backlog? | No. KSA's snapshot/edge collapse is not a NovaCore command-time contract. Proposal freezes exact prospective E, preserves previous commands before E and limits replay claim to identical accepted streams. PASS after correction |
| Is FC universally fixed10Hz? | No: frame wakeup reset plus additional bounded scheduling. Source map corrected; no universal-frequency claim |
| Does existing banking justify divergent architecture? | No. Retained reports contain measured exact endpoint, tick/debt and partition witnesses. Those outcomes justify the timing/commit constraints; banking alone does not. PASS |
| Will a dedicated queue still mutate physical StateRevision? | New proposed CommandRevision/history are separate; physical changes still require checked admission and private expectation acknowledgement. No silent state/revision exemption. PASS |
| Can events be handled and contact automatically continue? | No: scheduling and consumption change the captured readonly TimelineRevision. Proposal preserves blocker/episode-stop disposition; no event bypass or arbitrary rebind. PASS |
| Is E >= horizon precise enough? | Replaced with the minimum eligible B(n) using one owner snapshot, explicit open/closed boundary, checked horizon and fixed epoch after acceptance. E applies after the endpoint at E, to subsequent work. PASS |
| Does zero debt require a fake tick? | No. Explicit command-only owner operation; generic NoDebt and contact AwaitingDebt are distinguished. Control/physics dt remains zero. PASS |
| Can bounded storage promise every edge? | No. Exactly-once concerns accepted identities; capacity/refusal/dedup/gap handling and administrative reservation are explicit. Short opposing edges need not generate thrust. PASS |
| Can focus loss immediately neutralize old backlog? | Not under the selected prospective contract. Ingress revoke is immediate; canonical neutralization at R=(E,sequence) is prospective. Only unapplied old-lease commands after R cancel; earlier same-epoch outcomes remain. Pending latency is observable. PASS after precision correction |
| Does Pause already stop all NovaCore advancement? | No: explicit-time APIs and generic debt service can advance paused. Contact refuses paused. Proposed initial active scope excludes pause; future gameplay service needs its own explicit freeze, without changing administrative API semantics. PASS |
| Are KSA keyboard/UI warp policies identical? | No. Keyboard action guards, GUI enum routes, >30x held clearing and requested pulse/rate scaling differ. Proposal centralizes all producers in one policy and initially supports1:1 only. PASS |
| Is a requested SAS torque physical attitude authority? | No. Reuse pure math only; hardware availability/allocation and realized moment must qualify. Generic spool/gimbal slew are not claimed from current KSA source. Solid motor shutdown remains model-specific. PASS |
| Does common contact/flight control qualify pad assist or liftoff? | No. Contact captures fixed force/refuses nonzero torque; dynamic wrench admission and regime transition remain explicit later gates. One dynamics owner per vehicle/interval, never two independently integrated poses. PASS |
| Can a refused publication still spend fuel/advance timers? | Not silently. Future controller/actuator/propellant evolution must stage with the one pending endpoint. Retry cannot re-step or replay pulses. Canonical committed/private-invalidated stays terminal. Required future gate, not claimed implemented |
| Could renderer/camera acquire command authority? | All device/UI producers submit intent; owned exact boundary commits it. Camera is presentation-only; no per-display update may integrate throttle or choose contact physics dt. PASS |

Final independent judgments: both the NovaCore owner reviewer and the KSA input reviewer returned **PASS for the architecture and nonactuating first implementation scope**, after the above corrections. The physics reviewer agreed with the ownership direction subject to the explicitly retained dynamics/actuator gates. No reviewer qualified an implementation that does not yet exist.

One subtle choice remains a declared proposal, not missing evidence: prospective input may have visible backlog latency. Timestamped input mapping or an emergency owner intervention could choose other semantics, but they require separately scoped authority and replay contracts. The selected first candidate does not promise those features.

## Reproduce source findings

1. Verify repository HEAD/main/origin/main, remote main and milestone tag against identity.json. Check clean tracked/staged state; preserve unrelated untracked files. No checkout/rebuild needed for this audit.
2. Hash installed `E:/Kitten Space Agency/KSA.dll`; compare FileVersion/ProductVersion and retained `certified-root-contact-kinematics/identity.json`. Hash the cached files listed in this package's identity. If installed identity changes, requalify source provenance before treating the cache as current.
3. Read current NovaCore `ENGINEERING_RULES.md`, `docs/NOVACORE_CURRENT_STATE.md` and banked formal article evidence. Search the actual source with rg for Input/Action/Control/Throttle/Engine/RCS/FlightComputer/PrepareWorker/UpdateModules/ComputeDerivatives, then follow call sites in current-source-map.md. Do not search only guessed class names.
4. For KSA start at Program.PrepareFrame and Vehicle.OnKey; follow InputEvents.ProcessInput, manual/FlightComputer copying, PhysicsBubble scheduling, ComputeControl, Rocket/Gimbal update, active nozzles, constrained/unconstrained dynamics, VehicleUpdateTask and ApplyResults. Inspect both state declarations and callers. Check UI routes separately from keyboard guards.
5. For NovaCore inspect native callback and NativeInputState, CelestialAnalyticalScene, pure SAS/target math, direct torque versus event-backed force, clock preparation, event comparer, owner phase, LocalContactSource/World callbacks/publication and ContactServicing. A search with no match is bounded to those inspected trees, not global proof of absence.
6. Read the existing ContactServicingTests and EngineeringContactArticleQualificationTests plus formal-qualification report for exact partition/debt/refusal witnesses. They were not rerun here. Do not cite the proposed control manifest as already tested.

Useful read-only commands from E:/NovaCore:

    git rev-parse HEAD main origin/main
    git rev-parse 'm14.21-authored-compound-spacecraft-contact^{}'
    git ls-remote origin refs/heads/main
    git status --short
    git diff --check
    rg -n 'PrepareFrame|PollEvents|ApplyInputEvents' build/ksa-residency-reference/assembly-source/KSA/Program.cs
    rg -n 'PrepareWorker|ProcessInput|ClearHeldPlayerInput' build/ksa-residency-reference/assembly-source/KSA/Vehicle.cs
    rg -n 'ComputeControl|UpdateModules|UpdateActiveNozzles|NextWakeupDeltaTime' build/ksa-residency-reference/assembly-source/KSA
    rg -n 'PrepareHostAdvance|ServiceClockSupported|TimelineConflict|ChangedAuthority' src/NovaCore.Simulation
    rg -n 'partition|reference|credit conservation' tests/NovaCore.Simulation.Tests/ContactServicingTests.cs

## Reproduce official history

Open the official KSA live-changelog links in history-convergence.md using normal authorized account access. The lead used the existing authenticated Discord tab; no API/token extraction. Channel searches were `in:live-changelog throttle`, `in:live-changelog input`, `in:live-changelog RCS`. Read the listed message/revision/date and compare to current source. Search result order is not chronology or current implementation proof. No raw message dump is required to reproduce the selected references.

## Integrity and evidence budget

Before writing evidence, fingerprinted all1,896 tracked working files by SHA-256. Aggregate: ordered git ls-files records, each `path bytes SHA256`, joined with LF and UTF-8 hashed as SHA-256. Recompute the same manifest to prove exact working bytes unchanged, including CRLF-sensitive dependency bytes. Final count/digest, all historical tag refs, baseline refs, index state and unrelated untracked file hash are in identity.json.

No new temporary tree, binary, diagnostic log, source extract or runtime dump was created. New permanent content is limited to this six-file architecture package, capped at96KiB after reconciling the complete required tables. Existing ignored/build scratch was neither modified nor reclassified as disposable. No cleanup was required or attempted.

Validation here means read-only source/reference checks, independent architecture review, local Markdown link checks and whitespace checks. It is not software compilation, numerical acceptance, allocation qualification, performance qualification, manual acceptance or banking.
