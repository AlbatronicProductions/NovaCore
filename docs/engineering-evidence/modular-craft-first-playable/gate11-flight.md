# Gate 11 — long craft through the same route

PASS — engineering qualification; actual desktop integration remains Gate 12. No production mechanism changed for this gate. The existing control gauntlet now accepts a tank-definition selector; the dedicated long test passes the long definition through exactly the same compiler, session, native input adapter, allocator and physical owners.

Debug and Release builds pass, with 8,064 checks each. Coverage: cold support, ignition, ascent, combined attitude demand, cutoff/coast, both signs of all axes with main on/off, release momentum, actual jet/gimbal response, exact state/ledger equality across host partitions, bounded journal rollover, identity refusal and atomic retry. Inputs are injected NativeInputState records through the ordinary adapter; actual keyboard/application evidence is Gate 12.

The shared Gate 10 basic route already qualified both craft for 1 s cold, 20 s powered and 10 s coast. Long craft: physical handoff at 1.125 s, origin height 972.349 m and vertical speed 102.053 m/s at cutoff; exact 80 kg A/120 kg B consumed. Independent inertial RK4 errors below 2.56e-8 m and 8.08e-11 m/s. Long native contact momentum residual below 6.80e-6 N s. Shared measurements are retained in gate10-measurements.json.

Independent architecture_verifier review PASS: no missing bounded Gate 11 responsibility or separate vehicle mechanism. Reviewed test helper SHA-256 `11693DC6A5FB5A6280E39355BCDBA9078171549A8597924FAA11BE7B5FB7FB6D`; raw results `8407186C4323509B8321A850E0E88C93A0368F8FCD8B72CEABFD87AC883AE23E`.

Reproduce: build Graphics.Tests Debug/Release with the corresponding `-p:NativeBuildDirectory=modular-craft-first-playable/native-debug` or `native-release`, then run each `bin/<configuration>/net10.0/NovaCore.Graphics.Tests.dll --modular-gate11`. Full raw rows remain in build/modular-craft-first-playable/gate11-results.json. Gate 10 regressions remain applicable because this gate only parameterizes tests; integrated source changes receive fresh Gate 12 regression. Preserve Gate 0 using the gate11-seal.json source/invariant inventory. No cleanup/deployment/Git history operation.
