# Gate 10 physical continuation and clearance

This extends the accepted owner map. The six definitions, physical values and
catalog identity are unchanged. Installed KSA is read-only; the established
construction/instance/service/control boundaries remain ADAPT. No new KSA policy
choice arose. Flight does not introduce another ledger, state store, contact
authority or control capability.

## Canonical lifecycle

The existing transaction engine admits sequenced integer host durations into the
existing clock debt, then serves at most four fixed 15,625-microsecond intervals
per call. The 64 Hz cadence is independent of host partitions. Cold supported
state advances before ignition. Exact fuel/power phases and immutable numerical
proposals precede publication. The canonical state, revision, clock, remaining
debt, physical consumer and history install under the existing owned phase.

Native contact is private continuation. Its sealed receipt, clock, revision and
frontier must match; failed mutation invalidates that continuation without
publishing fuel, power or motion. Pure preparation refusal retains the source
and debt for retry. No external callback executes during preparation/publication.

Zero remaining contact rows plus a positive whole-hull interval clearance bound
permits SupportedContact to FreeFlight. The exact material-origin pose/velocity,
attitude/rate, inventory, vessel identity and control capability survive. The
private native world is disposed after acknowledgement. Free flight re-proves
clearance each interval. Approaching unqualified terrain refuses continuation;
terrain impact, landing and contact reacquisition are not inferred.

Physical records roll at 256 intervals; control admissions roll at 256 entries.
Each roll retains a canonical checkpoint, then replaces the old bounded window.
Absolute sequences never restart; expired control receipts refuse rather than
re-admit. The checkpoint is bounded history evidence, **not** a claim to serialize
or restore BEPU caches. CraftDocument persistence remains design-only; the legacy
static-service save API explicitly refuses physical craft. History size does not
terminate gameplay. Only source/coverage/numerical/integer bounds can refuse it.

## Rotating-frame dynamics

Let q rotate material coordinates to the site frame, c be instantaneous material
COM, w the body components of relative angular velocity, O the material origin,
Omega/alpha_f the site frame's analytical angular velocity/acceleration. Set
o=q^-1 Omega, w_abs=w+o, tau=tau_O-c cross F. The qualified proportional-removal
law gives:

```
w' = I^-1(tau - w_abs cross (I w_abs)) - q^-1 alpha_f + w cross o
q' = 0.5 q (w,0)
O'' = q [F/m - w' cross c - w cross (w cross c)] + g_site(P_C,V_C)
P_C = O + q c; V_C = O' + q (w cross c)
g_site = -mu r/|r|^3 - Omega cross (Omega cross r)
         - alpha_f cross r - 2 Omega cross V_C
```

Here r is the complete Earth-centred position in site components. V_C is the
velocity of the material point at the instantaneous COM; P_C'=V_C+q c'.
The accepted open-system law has no invented `-I_dot w` or `2 w cross c_dot`
term in material-origin acceleration. Transport to Earth adds the site origin's
full velocity and rotation. The IAU orientation derivative is analytical and
evaluated at RK sub-times, including whole-second crossings.

Native contact uses the same site transport, current full tensor and actual
compiled wrench. Exact service phases are split into native slices no longer
than 1/1024 second. Each slice recentres the compound at midpoint/end mass while
retaining material-origin position and velocity with FP64 residues. Endpoints
select the exact observed final quantities, avoiding cancellation interpolation.

An exact depletion event can be shorter than a normal FP32 duration. Such a phase
is never passed to reciprocal-dt native solving. It may be numerically omitted
only when its FP64 mass properties are identical and derived translational and
rotational increments are below `(contact tolerance * 2^-24)/256`; exact resource
debit remains unchanged. Force/gravity/frame/gyro bounds and bootstrap checks
derive this error bound. Otherwise preparation refuses. This is an explicit
bounded numerical approximation, not rounding inventory or changing event order.

## Whole-interval clearance

Compiled collision vertices and all corners of the admitted COM box bound a
radius R. Total installed thrust bounds F. Main gimbal envelope plus the sum of
individual jet torque magnitudes bound tau for every legal subset. Total maximum
flow times maximum store-to-COM distance divided by dry mass bounds C=|c'|.

For proportional depletion, dI/dq = I_store/kg + Parallel(datum-c) is positive
semidefinite. Thus E=0.5 w_abs dot I w_abs satisfies E' <= |tau| |w_abs|. Over h:

```
U_abs <= sqrt(w_abs0 dot I0 w_abs0 / I_min) + tau_max h/I_min
U_rel <= U_abs + Omega_max
A0 = Fmax/mdry + 4 mu/r0^2 + 1.5 r0(Omega_max^2+alpha_max) + U_rel C
Vmax = (|V_C0| + A0 h)/(1 - 2 Omega_max h)
Amax = A0 + 2 Omega_max Vmax
D = h(Vmax+C)
```

The radius bootstrap requires D<r0/2, so central gravity and frame terms remain
inside the assumed radial interval. Nonfinite or nonpositive bounds refuse.
The downward hull envelope uses the actual minimum initial vertex height,
`min(0,V_C0.y*h) - Amax*h*h/2 - 2*C*h - R*U_rel*h - 2*tolerance`.
Positive clearance plus conservative full-weight radial projection certifies
the authored grading rectangle. Its **half extents** are 64 and 56 metres.

Outside that rectangle, a sphere of radius R+D+2*tolerance encloses the complete
swept hull around initial P_C. The same trusted physical query supplies an upper
radial terrain height over its angular cap. Downward norm/subtraction enclosures
and upward threshold/cap rounding prevent a rounded positive distance becoming a
false certificate. The slab's radial enclosure is included independently.

Every native slice also proves its whole occupied sphere remains above the
graded terrain and inside its full-weight domain. Its native world contains the
finite slab only; it cannot silently act as a general terrain collider. Motion
reach includes admitted speed, the established 2 m/s recovery bound, acceleration
and the existing 4 mm representation cushion. Failures poison private mutation.

## Terrain cap from current authority

The internal capability is implemented by the existing acquired physical-query
owner over its immutable published datasets:

1. Global bilinear height is bounded by all texels touched by the angular cap's
   latitude/longitude rectangle, including a one-cell arithmetic collar. Stable
   atan2 latitude retains polar horizontal components. Pole-containing or large
   worksets use the existing global maximum, never a guessed local maximum.
2. Regional residual is bounded by every published record's signed endpoints,
   including zero. Bilinear interpolation, fallback levels, missing sectors and
   [0,1] coverage cannot exceed it. The current conservative maximum is about128m.
3. Procedural macro and near bands are unshaped; meso alone applies the authored
   linear/ridge shape. Field value bounds and convex family blending give the
   physical bound. Existing rendering `ComposedBounds` is not reused: its
   macro/near shaping mismatch was discovered by independent review and remains
   separate from this physical certificate.
4. Grading is bounded by `max(geographic + naturalBase, planeMaximum)+naturalNear`.
   The plane maximum follows the grading owner's existing nonzero-support
   cosine test. A radius-scaled arithmetic enclosure covers cancellation.

Near Florida the resulting conservative terrain ceiling is approximately244m,
before hull clearance. This is a bound, not an elevation change. No dataset,
terrain sample, rendered mesh, contact tuning or proprietary KSA material changed.
Permanent tests retain both independent polar witnesses and sampled cap/hull
checks. Sample checks supplement the derivation; they are not its proof.

## Contact-release interpretation

Cold contact has approximately0.299mm/0.215mm compression for the short/long
craft. At ignition its existing spring/recovery solver supplies about0.003306/
0.006287m/s additional velocity. Independent collider-offset experiments isolate
this as penetration recovery, not extra thrust. Permanent optional native
diagnostics read actual solved normal and tangent impulses at every substep;
`m_mid*(v_after-v_before)-J_contact` recovers authored thrust plus site gravity.
The diagnostics are bounded, disabled in production, and invoke no callbacks.

Fresh uncompressed contact agrees with the RK velocity oracle near1e-8m/s. Its
roughly105/34 micrometre positional difference follows first-order native drift.
Post-handoff qualification instead compares the complete powered arc with an
independent inertial RK4 integration. There is no contact impulse to conceal in
that comparison and no physical tuning correction was needed.
