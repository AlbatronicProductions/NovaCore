# Gate 4 — six greyboxes and static feasibility

PASS, UNBANKED. Exactly six original definitions and six original procedural GLBs are in `assets/vehicles/modular-starter/`. No Blender/KSA content was used. Both test assemblies contain twelve instances and eleven named joints. Runtime delivery, variable-mass dynamics, allocation, Florida admission and flight remain Gates 7–12; this is not Player PASS.

Canonical catalog digest: `996a19fc0550ea5af01ae7748eea493263c08e1c54a8ef8ba3fda1c90eeeab4a`.

## Physical inputs and corrections

The accepted mass, resource, thrust and power budgets remain: core120 kg, tank120/180 kg, adapter100 kg, main engine100 kg, eight8 kg attitude blocks; propellant A320/640 kg at1000 kg/m³ and B480/960 kg at1500 kg/m³; main30,720 N at10 kg/s, each attitude jet15 N at15/3072 kg/s, exact2:3 mixture; battery90,000 J and enabled controller demand56 W. `nc.actuator.main/1` and `nc.actuator.attitude/1` are explicit actuator-model identities for later qualification, not inferences from display names or gimbal presence.

Each short store occupies .32 m³ and each long store .64 m³. The co-axial divider is sqrt(.125) m; outer radius .5 m; store spans .8148733086305042/1.6297466172610084 m. Tank envelopes remain nominal1.2 m diameter by1.5/3 m; socket-aligned conservative octagonal collision hulls have maximum radius .6494353201754364 m. Full-precision dimensions replace rounded planning prose. Authored binary64 primitive facts and aggregates use exact rational reconstruction with one final rounding, including all nine tensor entries.

Clearance review exposed a causal packaging conflict: finite aft attitude-exhaust envelopes intersected the original adapter rails, spokes and feet. The authored geometric core keep-out is explicitly10 mm **radius**,3° half-angle,4 m length; it covers all other hardware for both articles. This is a provisional geometric constraint, not a solved plume, thermal, aerodynamic or stress model. The adapter perimeter/feet move from±.65 to±.95 m, and four braces lie at22.5°+90°k between the eight jet paths. Brace extent reaches X=0 to preserve the tank load path. There are still thirteen effective mass regions in one100 kg adapter. Four integral .12×.12 m pads retain15 kN per-foot limits. The new dry COM is−.5800000000000001 m; the brace correction adds2 kg·m to the vehicle first moment. All revised inertia follows geometry; old planning tensors are superseded.

Engine geometry has a fixed flange/neck, joined moving head/bell, and authored pivot at its forward interface. Independent gimbal limits±.05 rad permit combined tilt .07069594 rad, with slew .04 rad/s. The conservative swept hull leaves .465922915 m ground clearance; the actual swept exit-disk bound is .484588859 m. Main exhaust has its own .25 m conservative aperture and complete gimbal envelope through the foot plane. Gimbal motion changes visual geometry and effective thrust direction, following the existing fixed effective dry-mass convention; articulated bell mass/reaction dynamics are excluded.

All four attitude exits are visible original nozzle meshes. Exits lie opposite their force directions. Moving the application point along its force line preserves torque. GLB accessor bounds are computed from stored FLOAT data; an independent review caught and corrected pre-quantization extrema.

## Independent numerical reconstruction

COM is measured forward from adapter origin; inertia is about current COM, kg·m². The authored signed symmetry yields zero off-diagonals.

| Craft/state | Mass kg | COM X m | Ixx | Iyy | Izz |
|---|---:|---:|---:|---:|---:|
| Short dry | 504 | .418650794 | 197.382430 | 666.705898 | 667.339231 |
| Short half | 904 | .565265487 | 252.382430 | 740.824502 | 741.457835 |
| Short full | 1304 | .621932515 | 307.382430 | 799.921846 | 800.555179 |
| Long dry | 564 | 1.097517730 | 215.682430 | 1560.827740 | 1561.461073 |
| Long half | 1364 | 1.333577713 | 325.682430 | 1846.484958 | 1847.118291 |
| Long full | 2164 | 1.395101664 | 435.682430 | 2092.522497 | 2093.155831 |

At planning gravity9.81 m/s², T/W remains2.401455883/1.447088018. Neutral per-foot loads3198.06/5307.21 N and equal two-foot loads6396.12/10614.42 N remain below15 kN. The2° support margin includes a60 mm allowance and exceeds .805/.778 m. These are static geometric/load assumptions, not qualified dynamic contact loads. Eight tangential jets yield90 N·m with zero net force; opposite axial pairs yield22.5 N·m. Revised inertia must enter Gate8 allocation, not old planning accelerations.

## Qualification and review

Permanent checks:160 definition/assembly checks; six assets through the actual production GLB reader;58 FLOAT accessor bounds; all socket positions/orientations;29 meshes/2124 triangles. Independent SAT checks all1,987 cross-part collision pairs and2,180 clearance/body pairs per craft, with no positive overlaps; manufactured blocked-cone and oblique-box witnesses exercise refusal. Both independent reviewers reconstructed mass/tensors and all service paths. The static topology reaches33 consumers and ten loads/56 W. Actual power delivery remains Gate7/8.

Reviewer `architecture_verifier` independently reconstructed all18 analytic primitives using exact rational arithmetic, recovered the original planning calculation, and found the GLB-bound error. Reviewer `capsule_closure` found missing load-path/packaging connections, checked4,440 actual FP32 vertices inside collision unions and independently performed712 conservative finite-cone comparisons per craft. The smallest conservative cone margin was2.124 mm. Final asset metadata correction preserved every BIN chunk byte. No unresolved material Gate4 defect remained.

Gates1–3, construction Stages1–8, SRV-01 and control/demand/allocation regressions pass. Simulation.Tests Debug/Release full managed builds pass. Graphics.Tests Debug/Release managed compile and copy pass; its full Build target encountered the pre-existing missing native DLL at `build/native-ninja-release/NovaCore.Native.dll`. No native/GPU build or player integration PASS is claimed here. Gate5 must prepare a current native candidate.

Cold timing/allocation/GC/tail measurements and regressions are in `gate4-results.json`. Catalog validation is cold work, roughly3–7 ms median across windows and about3 MB allocated per load; no invented budget or frame-loop/zero-allocation claim. Content is374,984 bytes; six immutable GLBs total158,156 bytes. Existing warmed runtime zero-allocation regressions remain passing.

## Reproduction and preservation

Run `python tools/vehicle-construction/author-modular-starter.py --check`. Regeneration without `--check` is limited to the fourteen original starter-content outputs. Run Simulation.Tests `--modular-gate4`, Graphics.Tests `--modular-greybox-assets`, and `check-modular-geometry.py` with Python+NumPy. `--modular-gate4-measure` reproduces measurements; `qualify-modular-gate1.ps1` retains earlier regressions. Geometry does not import generator helpers.

`gate4-seal.json` verifies HEAD/index/public refs/worktrees and all3,322 entry paths:3,313 unchanged, nine authorized source/test changes, no missing/unexpected changes. Internal Codex capture refs are reported separately. Existing recovery archives and all earlier evidence remain. KSA responsibility split was already settled by the accepted plan; this gate authored original numerical content, so no fresh KSA-history claim is needed. KSA writes0. No Git publication, deployment, banking or cleanup occurred.

Next: Gate5, direct editor and generic authored-socket construction.
