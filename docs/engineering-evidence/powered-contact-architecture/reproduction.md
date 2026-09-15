# Reproduce the architecture assessment

Read-only source audit. No build, runtime solver step, allocation/performance campaign, source instrumentation or implementation is required.

## Repository identity

From E:/NovaCore:

~~~powershell
git status --short
git rev-parse HEAD main origin/main
git branch --show-current
git rev-parse m15.0-segmented-powered-free-flight 'm15.0-segmented-powered-free-flight^{}'
git show-ref --tags
git diff --check
~~~

Expected baseline/head/main/origin/main: 49057fecceb0f725d5f551ec40e2780971b0d81d.
M15.0 tag object: 72c387e0bbe512a569650978522bbb3a0022ffc3; peeled commit: 4607d8c802006d5e1a01c595ab608cf53a4dab6b.
Investigation stayed on codex/segmented-powered-free-flight; no ref change was needed for evidence-only work.

## Source navigation

~~~powershell
rg -n 'SetLocalInertia|IntegrateVelocity|ScaleAccumulatedImpulses|inverseDt' <pinned-BEPU-source>
rg -n 'TryCreate|Step|ValidateAuthority|Acknowledgement|PrepareService' src/NovaCore.Simulation/Spacecraft/Contact/Staging
rg -n 'PreparePowered|PublishPowered|ServicePowered|LastBoundary|Active = false' src/NovaCore.Simulation/Transactions
rg -n 'DryMassKilograms|TryStageMass|TrySeconds|Derivative|Epsilon' src/NovaCore.Simulation/Spacecraft/Actuation tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests*
rg -n 'InverseMass|Acceleration|PredictedDepth|Distance' src/NovaCore.Simulation/Spacecraft/Contact/Staging/CompoundContact*
~~~

PowerShell callers should use rg directory arguments plus -g filename filters rather than pass unexpanded wildcard paths when necessary. These searches identify source responsibilities, not qualification results.

## Pinned dependency

Read external/bepu/2.5.0-beta.29/manifest/bepu-2.5.0-beta.29.json, verify actual DLL/package SHA-256 against its accepted values. Official source template:

https://raw.githubusercontent.com/bepu/bepuphysics2/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/{path}

Use identity.json path/hash list; hash exact HTTP response bytes. Inspect in memory or a separately reviewed temporary location; do not replace the dependency or retain a new full source/build tree. Existing build/compound-coverage-investigation source witnesses remain untouched and reproducible from pinned upstream.

## KSA provenance

Read-only installed binary E:/Kitten Space Agency/KSA.dll, retained current source E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA, and official history E:/Kitten Space Agency/Content/Versions. Verify identity.json hashes before using anchors. Match current binary to retained source provenance; if it changes, do not call old decompilation current.

Retain summaries/source anchors and official revision/date references only. Do not copy proprietary code or assets.

## Reasoning witnesses

- Inspect exact tiny event fixture and float dt/reciprocal code: a positive 2^-1075 s event with representable binary64 effect cannot enter plain float Timestep.
- Compare constant-a position updates: semi-implicit p1=p0+(v0+a*h)*h versus p0+v0*h+a*h^2/2.
- Derive log-mean constant-force delta-v, then centered support impulse with arithmetic mean mass; their difference refutes a universal one-effective-mass adapter.
- Follow engine private cursor sealing, exact resource lease, fixed canonical publication and ack to verify no speculative fuel consumption and no postcommit replay.

These are source/algebraic proofs. No new powered-contact trajectory, storage, allocation or performance result is claimed.
