# Scalable launch support — causal checkpoint

The existing rejection is an admission certificate, not a measured overload.
`CompiledCraftContact` requires exactly four rectangular feet and combines maximum
mass with independent COM extrema. It requires every statically possible foot
reaction to fit the rating, rather than proving the realized support solution.

An isolated probe of the unchanged canonical native solver (1 s, 1024 Hz,
vertical 9.81 m/s²; **not player qualification**) measured:

| Stock long tanks / eight RCS blocks each | Mass kg | Peak row N | Steady row N |
|---|---:|---:|---:|
| 1 | 2164 | 5807.493 | 5307.211 |
| 2 | 4008 | 10369.680 | 9829.620 |
| 3 | 5852 | 15012.413 | 14352.032 |

Each row in this centered witness corresponds to one foot. Production enforcement
must sum **all** solved rows belonging to each physical foot, every actual native
substep. The third case demonstrates why a feasible average is insufficient.
The unchanged 30,720 N engine gives the full second case T/W ≈ 0.782. Correct
ignition burns fuel on the pad until physical contact unloads; no immediate
release is implied.

## Current KSA reference

Installed KSA `2026.9.22.5482+40faabcc2a54baeb11b7b9f21beade6f7e6b524e`:
KSA.dll SHA256 `ce43d022e7dac9beb2352b6106164bc1f521dda1ae9d0b898b8dba0c59a69723`;
CoreLaunchPadAGameData.xml SHA256
`ebcf9277b08fb565878d03240123e8e9ac6705e11f41f3ca78af4d8303f3c3be`.
Read-only installed-method inspection establishes:

- `VehicleEditor.Dispose` / `VehicleLaunchMenu.LaunchVehicle`: aggregate mass,
  bounds, initial kinematics, ordinary vehicle; no generated clamps.
- `Vehicle.GetInitialKinematicStateForLocation`: bounds/COM/terrain/pad spawn and
  rotating-body velocity. Four lower corners are terrain samples, not feet.
- `ConstraintSim.ResolveLaunchPads` / `UpdateStaticObjectCollider`: location-owned
  static collider; `Vehicle.CreateColliderCompound` supplies authored craft solids.
- `PartContactLoad.ApplyForVehicle`: impact/deformation accounting subtracts ordinary
  weight impulse; it is not static foot-strength admission.
- `UpdateIsAnyConstrained`, physics-step lifecycle and collider removal keep contact
  loss, consumer selection and cleanup distinct. No launch release impulse.

ADOPT location-pad/full-craft collision and actual aggregate mass ownership.
ADAPT finite authored NovaCore load ratings, fail-closed physical admission,
precision and atomic publication. INTENTIONALLY DIFFER from permissive terrain
fallback because NovaCore requires authenticated Florida support. KSA writes: 0.
KSA research is complete; no fresh authenticated-history claim is made.

## Bounded correction direction

Keep authored legs, patch geometry, 15 kN ratings, spacecraft properties and slab.
Retire exactly-four/rectangle/universal-worst-share admission. Prepare a generic
physical support solution and enforce realized native reactions.

The existing compound/slab pair also globally reduces to four solved rows. Merely
generalizing the validator would leave a hidden support scaling restriction.
Use standard pinned BEPU one-body contact functions through project-owned
registered constraint descriptions, retaining each real child convex manifold.
Keep one dynamic compound and the original single slab. Do not split the slab:
geometric union alone would not prove seam/contact equivalence.

This checkpoint is causal evidence, **not implementation or qualification PASS**.
