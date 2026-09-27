# Gate 8 — physical/service qualification

Judgment: **PASS — bounded physical preparation.** UNBANKED; no live admission or
manual Player PASS. Gates 0–7 are preserved. Gate 9 continues automatically.

The existing construction fuel/power ledgers now compose exact ordered power and
propellant events for the fixed-load battery profile. The existing actuation and
dynamics owners consume those immutable phases. Prepared 32-bit allocation uses
actual jet geometry; the main mechanism honors authored clocking and slew. The
existing `LocalContactWorld` prepares the full convex compound and native mass
reference changes. No new mutable ledger, control authority or publication owner
was introduced. See [derivation](gate8-derivation.md) for equations and bounds.

Qualification closes 1,911 checks in **both Debug and Release**, including:

- Exact 0.5 J / 56 W outage at 1/112 second; no later fuel debit, continuing coast.
- Fuel-before-power, tied/multiple-bus events, maximum/zero intervals, subdivision
  invariance, failed proposals and unchanged source states.
- Saved arithmetic identity, malformed signed parser widths, 32,001 explicit
  power-bus snapshot roundtrip and bounded numerical conversion.
- Every simultaneous attitude request and every legal main-engine clock, positive
  physical authority, finite slew, target arrival and unpowered hold.
- Rocket-equation velocity/displacement, no mass-flux spin impulse, adaptive
  high-rate precision admission/refusal and an independent 60-digit Decimal
  unequal-fill/nonprincipal-spin oracle.
- Actual BEPU convex support, full tensor preparation, 1,001 unequal-fill native
  recenterings and malformed native proposal refusal before writes.

| Physical preparation result | Short | Long |
|---|---:|---:|
| Initial mass | 1,304 kg | 2,164 kg |
| Convex regions | 69 | 69 |
| Conservative finite-pad static load maximum | 7,399.624 N | 12,572.373 N |
| Authored per-foot load limit | 15,000 N | 15,000 N |
| Two-degree COM margin | 0.860952 m | 0.834761 m |
| Five-second native support maximum movement | 0.298500 mm | 0.214339 mm |
| Maximum measured penetration | 0.298531 mm | 0.214894 mm |
| Recenter material-origin velocity discrepancy | 1.92e-14 m/s | 1.56e-14 m/s |

The independent Decimal oracle converges internally below 1.01e-20 and differs
from candidate velocity by at most 2.75e-15 m/s for its two unequal-fill fixtures.
These are measured oracle results, not a universal trajectory error claim.
Pad loads are static equilibrium bounds; impact/landing is not qualified.

Two independent read-only reviews PASS: `architecture_verifier` for services,
arithmetic, snapshot identity and control; `capsule_closure` for dynamics,
numerics, clocking and actual native preparation. Review found and closed ordinary
gate defects: changed-scale snapshot aliasing; signed parser scratch; explicit
bus JSON size; zero/reversed/rounded gimbal targets; fixed-jet restriction;
unbounded high-spin acceptance; interpolation overshoot; exhaust/solid confusion;
and equal-share/point-foot load assumptions. Permanent witnesses remain.

The 21 preserved qualification suites pass (construction stages 1–8, Part
Standard admission/adversarial suites, SRV-01 and M15.5 control/allocation). Gates
2–7 pass in both configurations. Managed solution and desktop editor builds pass
in Debug/Release using the already-built matching native candidate directories.
An initial solution build without that property selected absent default
`build/native-ninja` outputs; the explicit existing candidate path resolves the
copy prerequisite. No native correction or rebuild was needed.

Prepared physical proposals were measured with normal GC: median 0.34–0.44 ms,
42,048–56,304 allocated bytes. The observed maximum is 5.6296 ms with a Gen0
collection; another boundary sample included Gen1. Repeated tails and all three
windows are retained in [measurements](gate8-measurements.json). This excludes
canonical publication, contact stepping and rendering, which remain later
integration measurements. No zero-allocation or whole-frame PASS is claimed.

Preservation: HEAD/index/public refs/worktrees unchanged; 3,303/3,322 entry files
byte-identical, with 19 explicitly authorized changed entry files. Existing
recovery ZIP/index remain. New files are sealed in `gate8-seal.json`. Ordinary
managed build output directories were refreshed; no launcher preparation,
deployment/promotion script, Git history operation or cleanup was performed.
KSA writes: 0. This gate enforces the accepted plan's already-settled finite-power,
physical-law and ownership decisions; no unresolved KSA policy was invented.

## Reproduce

From repository root with the existing native Debug/Release candidates:

```powershell
pwsh -NoProfile -File tools/vehicle-construction/qualify-modular-gate8.ps1 -IncludePreservedRegressions
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate8-oracle | python tools/vehicle-construction/check-modular-dynamics.py
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --modular-gate8-measure
python tools/vehicle-construction/seal-modular-evidence.py --gate 8
git diff --check
```

Rebuildable logs remain under `build/modular-craft-first-playable/`. The compact
results, independent oracle and distributions are retained here. No deletion is
authorized. Gate 9 must prove the exact authored craft enters the canonical
Florida owner, with real site/rotation transport, cold controls and camera.
