# Gate 10 — continuous short-craft flight

PASS — bounded engineering qualification. Gate 11 and application integration follow; no manual Player PASS.

The same canonical construction state, exact services and M15.5 control authority now advance cold support, powered unloading, rotating-Earth free flight and coast. The native contact world is private preparation and is retired only after a sealed handoff. Physical and control journals roll within fixed storage; neither imposes an episode endpoint. [Derivation](gate10-derivation.md) records the rotating-frame, variable-mass, contact-impulse, clearance and refusal proofs. No KSA policy was reopened; the accepted ADAPT owner map is preserved, KSA writes 0.

## Qualification

Final Debug and Release solution builds pass with zero warnings/errors. Per configuration: Gate 9 886 checks; rotating dynamics 23 checks; flight 78,307 checks. Earlier full qualification also covers editor builds, Gates 2–8, Gate 1 admission suites, construction Stages 1–8, SRV-01, M15.3/M15.4/M15.5 and allocation contracts. ReferenceFrames, Precision and BEPU pass. Launcher passes 19 tests after correcting the invocation framework directory; the failed invocation executed no tests. [Results](gate10-results.json) retain both provenance and final rerun association.

The short craft cold-soaks for one second, ignites, physically unloads at 1.0625 s, burns for 20 s and coasts for 10 s. Exact main debit is 80 kg A plus 120 kg B. Altitude relative to the initial material origin is about 3014.46 m and vertical speed 315.66 m/s at cutoff. Independent inertial RK4 differs by at most 1.19e-7 m and 2.18e-11 m/s. Native solved normal/tangent impulses independently explain stored contact recovery at ignition (momentum residual below 9.05e-6 N s), rather than hiding it in a loosened velocity tolerance.

Positive/adversarial coverage includes both signs/all axes with main on/off, combined demand, release momentum, 32-jet selection, real native input deduplication, exact fuel/power exhaustion (including subnormal event duration), host partition equivalence, journal rollover, foreign/stale admission, whole-hull terrain certificates and pole/antimeridian bounds. A post-native proposal failure leaves canonical state/reference, revision, clock and debt unchanged and poisons private continuation. A pure preparation refusal permits retry.

## Independent review

`architecture_verifier`: PASS, no remaining demonstrated authority/control/history/clearance defect. Reviewed final main source SHA-256 `1430298B190BB6E116874DBBF91A717CEA4BB6E0CA5A11E74340541C170C1542`; refusal tests `482FB7F9375A70CE9F632DDAE43D981789BF4AF1FB927B9DCB1F506B3F37781D`.

`capsule_closure`: PASS, rotating-Earth/material-origin transport, exact services, contact momentum accounting, conservative clearance and atomic publication. Independently verified enabled/disabled native diagnostics produce bit-identical 13 motion doubles and exact fuel/power for both craft. Reviewed native craft source `334788BDC0384CEFFF4F8FAAA6C6F5056CB18E150A76FF29241C4CDAE9E8FEDF`. Both reviewers were read-only.

## Performance and limits

[Measurements](gate10-measurements.json) preserve two runs without discarding tails. Final normal-GC run: three 256-interval windows per craft/boundary. Supported medians 0.21–1.38 ms, maximum 7.75 ms; powered medians 0.12–0.26 ms, maximum 1.06 ms; coast medians 0.11–0.20 ms, maximum 2.03 ms. Physical interval allocations are approximately 33–52 KB. Only warmed held-input plus actuation observation is 0 B over 1000 iterations. Separate retained storage is 508,016/507,944 B for short/long after 767 steps, 255 records, native pool 0. P95/P99, GC and repeated tails are in JSON. Rendering/editor/compiler work is excluded; integrated frame pacing remains Gate 12.

Checkpoints are bounded observational physical history, not BEPU cache restore or flight save/load. Host partition equivalence means physical state and exact ledgers; host sequence journals retain actual delivery history. Terrain exclusion is conservative; unqualified ground approach refuses continuation. Atmosphere, landing/contact reacquisition, warp, SAS and staging remain excluded. This qualifies the authorized ascent/cutoff/coast domain, not arbitrary orbital or return flight.

## Reproduce and preservation

From repository root, use PowerShell 7: `tools/vehicle-construction/qualify-modular-gate10.ps1 -IncludePreservedRegressions`. Native candidates must already exist at `build/modular-craft-first-playable/native-debug` and `native-release`. Run Release Graphics.Tests with `--modular-gate10-measure` for normal-GC measurements; run ReferenceFrames.Tests, Precision.Tests and BepuDependency.Tests from `bin/Release/net10.0`, Launcher.Tests from `bin/Release/net10.0-windows`. Consolidate with `python tools/vehicle-construction/consolidate-modular-gate10.py`; verify preservation with `python tools/vehicle-construction/seal-modular-evidence.py --gate 10`.

Gate 0 is preserved, not reopened. No cleanup, deployment, commit, tag, push or banking. Raw regenerable output remains under build pending reviewed cleanup; compact evidence stays within the campaign budget. Exact source and entry preservation are in [gate10-seal.json](gate10-seal.json).
