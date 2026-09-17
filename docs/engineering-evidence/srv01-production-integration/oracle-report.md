# SRV01 FourHorn independent reference generation

PASS for independent reference generation and finite convergence evidence. This
report does not claim production runtime qualification.

The authored physical input is
`E:/NovaCore/src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-FourHorn.json`,
SHA-256 `ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf`.
The generator refuses a changed input hash. All six fixed pairs resolve their
explicit part and actuator identifiers from that input. Roll uses the real
opposing tangential couple; pitch/yaw use their authored +45 N axial pairs.

## Independent method and retained provenance

The retained direct coupled oracle is
`C:/Users/Tyler/Documents/Codex/2026-09-16/srv-01-gate0/work/critic/check_recovery.py`,
SHA-256 `d2933344a9b7a776588ec350ae4efba3a219fadda259c8d55174413242849d59`.
Its six-equation Gaussian Newton/Euler solve is the algebraic source for this
scratch adaptation. Its old trajectories are not used as expected results.

The retained original reduced-equation generator is
`C:/Users/Tyler/Documents/Codex/2026-09-16/srv-01-gate0/work/prove_recovery.py`,
SHA-256 `47745ad05b27ceba10a0d367e612474af295f74fd585990435560cb1a24ba9cd`.
The new oracle deliberately uses the independently structured coupled solve.

The unchanged 24 schedules come from
`E:/NovaCore-SRV01-Proof/tools/assembly/fixtures/Reference-Endpoints.json`,
SHA-256 `2fc90b50e227462e87c35148d8c3fc862091f521f4b45ea54d0f07ba60160431`.
Only its case objects are read; its endpoint values are not imported.

Adaptations are limited to reading the new closed physical authoring data,
reaggregating all seven dry-part contributions, resolving sixteen nozzles and
six explicit pairs, and evaluating the retained direct equations with Decimal
precision 60 instead of binary64. Each authored JSON float is interpreted as
its exact binary64 value. Exact Fraction arithmetic supplies time, resource
flow and exhaustion boundaries. Main gimbal angles remain source-held; the
coaxial offset has identically zero additional moment. No C# runtime evaluator
or production helper is imported.

The generator retains every tensor component in its 6x6 solve. Last-place
Decimal cancellation residue is admitted only below 1e-45 and is not zeroed.
The independently checked binary64 canonical contribution order remains the
production admission responsibility.

## Results

All 24 scenarios retain their original case names, loads, durations, initial
motion, gimbal settings, main flag and part-pair selectors. The output retains
the existing `trajectory_rows/case/endpoint` layout and XYZW quaternion order;
additional provenance and selected-nozzle fields are additive.

RK4 refinement compares 256 with 512 steps/second, inserting the exact rational
exhaustion boundary. Worst endpoint differences across all 24 cases:

| Measure | Maximum difference |
|---|---:|
| Origin position | 7.134550897634767e-14 m |
| Origin velocity | 6.973153749131656e-14 m/s |
| Quaternion geodesic orientation | 4.697922286050306e-15 rad |
| Body angular velocity | 1.9775190850810346e-16 rad/s |

Two independent analytic witnesses also pass. The neutral straight main burn
agrees with the rocket equation to 4.392532041434315e-24 m and
1.2175773482188477e-27 m/s. The main-off roll couple from rest agrees with
constant axial angular acceleration to 2.0366415644324347e-17 rad; position and
linear velocity remain exactly zero in the oracle.

The final generator SHA-256 is
`fa0bc3f668778ab98f6ad58faf57abfda823dd0ebf2ceb69eb3e95dd20b21983`.
The final reference SHA-256 is
`3a173370e2a011fa8ebb6014ed22804fbaf7183b6e6e0524941039904e288c96`.
Two fresh processes produced byte-identical 62,704-byte reference files. Each
process required about 6.5 seconds locally; this is oracle cost, not runtime
performance evidence.

## Reproduction and handoff

Run Python 3.11 with its standard library only:

```powershell
python E:\NovaCore\build\srv01-integration\proof\four_horn_oracle.py
python E:\NovaCore\build\srv01-integration\proof\four_horn_oracle.py --suffix=-repeat
Get-FileHash -Algorithm SHA256 E:\NovaCore\build\srv01-integration\proof\Assembly-FourHorn-Reference-Endpoints*.json
```

The candidate replacement fixture is
`Assembly-FourHorn-Reference-Endpoints.json`. The lead owns promotion into the
test project and the production-vs-reference run. Existing nominal-interval
error tolerances remain unchanged: 1e-6 m, 1e-6 m/s, 1e-9 rad and 1e-9 rad/s.
The converged oracle does not validate production ownership, exact spending,
save/replay or per-nozzle identity; those tests remain required separately.

This task created four scratch files: the generator, reference, byte-identical
repeat reference, and this report. No production or test file was changed.
The only physical input written elsewhere was already authored by the lead.
Finite endpoint refinement is not a formal global integration error theorem.
