# Method A: analytical gate and bounded observation plan

Declared before any diagnostic build or world rerun in this revision.

Exactly two methods were examined. Method B remains conditional and is not
instantiated. Method A survives the cheap gate as explicitly permitted
DISCRETE SOLVER IMPULSE-WORK ACCOUNTING, compared to the frozen continuous
reference as a numerical approximation. It is not a reconstruction of a
physical force-time history. No intrinsic unobservable uncertainty remains
in this declared discrete quantity if every actual before/after state is read.

For fixed represented mass/inertia M and generalized velocity u:

    K(u) = 1/2 u^T M u
    delta K = (J u_before)^T delta lambda
              + 1/2 delta lambda^T (J M^-1 J^T) delta lambda

The identity holds in exact arithmetic. For actual float updates, compute
K(u_after)-K(u_before) from exact rational interpretations of the stored
float bits. This includes the represented rounding effect without subtraction
or a residual exception. Sum tangent applications, twist applications and
normal applications separately. Their sum must telescope exactly to the
contact-stage total kinetic-energy change. Do not call that stage total
friction work. Signed corrections, including positive ones, are retained.

Warm application is counted ONCE as the current step's numerical seed,
followed by each new-minus-old correction. Its sum in impulse space must
equal the final accumulated impulse. Never add warm impulse to final impulse.
The energy sum uses the actual ordered velocity states, not an endpoint
impulse proxy. Numerical order fixes a discrete attribution; other orders
need not give the same component attribution. It is not physical time.

## Instrumentation

Keep original verified BEPU DLLs. Temporarily replace ONLY processor slot 3
in the evidence harness AFTER the unchanged 120 native preparation steps.
Use the same generic native processor base, native prestep/impulse types,
and exact pinned Contact4OneBodyFunctions source statements. Insert read-only
snapshots around its six original row calls. Copy the internal four-contact
friction-center helper verbatim because it cannot be called externally.
All native row methods remain calls into the unchanged pinned DLL.

Restore the original processor in finally after the one actual target step.
No shape/body/cache/state replacement. No force, dt, iteration, substep,
ordering or numerical expression change. This IS a temporary diagnostic
dispatch override, not an assertion of literally unchanged dispatch identity.

Record 54 applications: six warm + eight times six solve applications;
108 before/after observations. All six velocity components, seven inertia
components and seven accumulated impulse components are captured as float
bits. Finite buffer, no log output during the solve. Normal work is observed
separately. Source-normalized arithmetic equality and original native output
identity are required. Any changed native snapshot/state/cache bit invalidates
comparison and stops execution; do not repair physics to obtain equality.

## Runs / stop

One fresh Release off-COM process first: both tangent and twist enabled,
engine torque, four normals and strongest retained rotational coupling.
If it passes work and comparison quality, ONE independent yaw-baseline
process is permitted to check sliding/twist without engine wrench. No other
rows, repeats, retries, convergence sweep or performance run.

Before any second run: require original physical gates and all original
native output values equal (only the diagnostic binary's directory may
differ); exact impulse telescoping; exact energy telescoping; finite input;
separate |W_discrete-W_reference| <0.07848 J for tangent and twist. The
strict interior satisfies both inherited inequality formulations.

These two witnesses do not establish a universal error theorem. The current
ticket authorizes minimal witness instantiation; all other retained physical
results remain unchanged and no extra work measurements are invented for them.

One new disposable root only:
`E:\NovaCore\build\powered-contact-friction-work`.
Source extraction, generated diagnostic harness, bin/obj and raw comparison
output go there. Retain compact trace bits, derivation/checker and reproduction
instructions in this child, budget 128 KiB. No old scratch cleanup retry.
