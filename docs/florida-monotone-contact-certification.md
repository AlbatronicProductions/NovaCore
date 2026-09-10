# Monotone contact on Florida's physical grading

**UNBANKED candidate: Certify monotone point contact on the existing Florida
grading plane.** M14.8 is the banked baseline; M14 remains open.

`FloridaContactProvider` is an internal, read-only Simulation responsibility. It
proves a clear interval or a unique approaching root for one complete authored
point geometry. It does not advance time, schedule events or apply a response.
No launcher or visible route invokes it.

## Supported inputs and coverage

The caller supplies the actual transaction engine, independently current state
revision, complete one-feature geometry, acquired physical query, current Sol
definition and reference graph, and a canonical search interval. The interval
must lie in one whole-second cell, within **[-3,600, +3,600] seconds relative to
the authoritative Earth Cartesian seed at SimulationInstant.Zero**. This is
seed-relative qualification, not a moving numerical anchor or general mission-date
coverage. In particular, it does not cover the normal 2024 launcher start time.

The point can have an arbitrary fixed COM offset. The physical craft must have
finite positive mass, valid inertia and immutable constant-root-force translation;
angular velocity and body torque must both be exactly zero. Its stored attitude
then represents a constant physical rotation. No small-angular tolerance exists.

Only `SolAnalyticalDefinition.Instance`, its current Earth-under-fixed-Sun model,
identity time mapping, current root/frame identities, and the acquired canonical
Florida full-weight grading branch are supported. M14.8 performs the existing
model/root/observation admission. A Core internal capability implemented explicitly
on the acquired sealed Graphics query identifies that physical composition.
Copying public query provenance into a wrapper cannot supply that capability.
Simulation retains no Graphics project dependency.

Admission copies physical segments and revisions during the single-writer read
phase. It retains immutable geometry, graph and terrain owners, not a live state
view. It checks the engine's own clock and timeline, conservatively refusing every
pending event through the closed search endpoint, including no-op events. Start
must not precede either segment epoch or the current clock. Every certificate,
refinement and comparison use rechecks both state and timeline revisions, engine,
segments, geometry contents, graph, system and terrain. A changed context is stale.

## Physical relation and domain proof

All physical input double values denote their exact stored binary values. Let
`U`, `E`, `N`, `R`, `a`, `IE`, `IN` and `B` come from
`FloridaFacilitySupport.Region`; the proof does not normalize or duplicate these
vectors/constants. The current inner extents are 64 m east and 56 m north, blend
width 128 m, and plane altitude 15.134892258793116 m.

For constant normalized craft attitude Q and authored offset r:

```
p(t) = p_COM(t) + Q r
q(t) = Q_E(t)^-1 [p(t) - p_E(t)]
T(t) = U . q(t)
f(t) = T(t) - (R + a)
```

On the existing full-weight branch, with `d=q/|q|` and `c=U.d>0`, canonical physical
height reduces mathematically to `(R+a)/c-R`. Therefore the continuous radial gap
is `g=f/c=f|q|/T`: f and g have identical zeros and signs. This is a real-valued
identity; it is not a claim of bitwise cancellation in the FP64 height evaluator.

The entire interval, rather than just its endpoints, must satisfy outward proofs:

* `T>0` and finite position, derivative and composed relation;
* `R |E.q| <= IE T` and `R |N.q| <= IN T`, excluding the blend;
* the actual early guard `c >= c0`, where
  `c0=1-((IE+B)^2+(IN+B)^2)/R^2`;
* the final height clamp is inactive. Since `c<=|U|`, proving
  `R+a > R|U|` suffices. Earlier finite base clamps cancel in the full-weight
  mathematical composition; they are not asserted inactive.

For the near-unit but not exactly unit stored U, set `K=U.U` and
`q_perp=q-U*T/K`. With positive T and c0, the guard is equivalent to
`T^2 (K-c0^2) >= c0^2 K |q_perp|^2`. Evaluating this relation avoids the severe
dependency loss of separately dividing large interval norms. The acquired terrain
owner supplies current finite composition/provenance; the proof adds no terrain
asset, collider, sampled surface or replacement plane.

## Restricted continuous motion enclosure

The Earth model is the current two-body ODE `p_E''=-mu*p_E/|p_E|^3`, using the
existing Cartesian seed/epoch and Sun gravitational parameter. No orbital-element
copy or propagated FP64 anchor becomes authority. A cubic seed Taylor enclosure
is used only over the declared one-hour coverage.

Let H=3600 s, r0 and v0 be the seed, R0=|r0| and V0=|v0|. The first-exit bootstrap
uses `Ahalf=mu/(R0/2)^2` and verifies `D=V0 H+Ahalf H^2/2 < R0/2`. Thus
`rmin=R0-D>0` and `vmax=V0+Ahalf H` bound the continuous orbit in both directions
from the seed. The seed acceleration and jerk are evaluated outward from the
ODE. Differentiating that ODE gives the conservative snap norm bound

```
S = 4 mu^2/rmin^5 + 24 mu vmax^2/rmin^4
position = r0 + v0 t + a0 t^2/2 + j0 t^3/6 +/- S |t|^4/24
velocity = v0 + a0 t + j0 t^2/2             +/- S |t|^3/6
```

Each component is enclosed by the norm remainder. The qualified position
remainder at the coverage boundary is at most 0.05123638785047009 m per component.
This bound limits useful refinement far from the seed; it does not modify Earth
motion. Craft translation is the unchanged quadratic constant-force relation,
with exact epoch-to-local-duration enclosure. Its fixed point offset uses the
exact normalization of stored quaternion bits.

Earth orientation reads the existing linear RA, declination and prime-meridian
model from its owner. It applies all three Euler rotations and the exact fixed
axis conversion, differentiating the rotation products analytically. It does not
use M14.8's finite-stencil angular velocity as a proof derivative. With W the sum
of absolute axial rates, `|Omega|<=W`, `|Omega'|<=W^2`. Over each requested interval,
relative acceleration A, velocity V and position P give
`|q''| <= A + 2 W V + 2 W^2 P`.

A binary midpoint is merely a Taylor expansion center. Outward value/derivative
enclosures at that center plus this second-derivative bound cover the whole
interval, including one-sided endpoints. Fixed root positions are differenced
before time-dependent interval terms to avoid artificial independent orbital
boxes. No dense sampling is a proof premise.

`FloridaBound` is private-purpose machinery for this provider: IEEE binary64
operations expanded to adjacent representable values, outward integer/rational
conversion and square roots, a bracket for pi, and bounded sine/cosine Taylor
series with explicit remainder after small-angle reduction. It refuses nonfinite
or out-of-range results. It assumes ordinary .NET IEEE arithmetic; runtime rounding,
tiering and GC configuration are unchanged. It is not a general interval-dynamics
or quaternion library. Permanent tests include an independent decimal seed
position/analytic-derivative oracle and independent decimal scalar identities.

## Results, root identity and bounded comparison

`CertifiedClear` requires a strictly positive interval range, or a positive final
value together with strictly negative derivative and positive start. It says only
that this complete admitted point geometry is clear over the declared interval.

`CertifiedUniqueApproachingRoot` requires positive start, negative end and
strictly negative derivative throughout. Continuity then proves existence,
uniqueness and a clear prefix. Initial touch/penetration, uncertain boundary
equality, tangency, possible multiple crossings, uncertain branch or weak slope
return `Unresolved` with a reason. General endpoint-equality certification is not
claimed. Unsupported inputs and stale authority have distinct status values.

The privately constructed immutable `Proof` retains its provider's frozen
relation/input identity and verified interval/function/derivative data. A default
proof is unusable. Provider/relation versions are fixed by implementation; there
is no caller-supplied version, proof flag, descriptor constructor or deserializer.
No interval midpoint, double time or rational approximation is the root identity.
The named continuous relation and its uniqueness proof can denote irrational roots
and rational roots outside `PhysicalEventEpoch` denominator capacity.

Existence certification and refinement are separate operations. `Refine` takes a
requested enclosure width (not a contact tolerance) and at most 24 deterministic
bisections. Unproved midpoint sign or exhausted budget returns `Unresolved` with
no replacement proof. A successful refinement changes the witness, never the root.
The original existence certificate remains a statement about its original domain.

Root/rational comparison encloses the exact rational before using separation or
physical sign. Root/root comparison validates both current contexts, uses shared
verified relation/search identity for equality, or performs at most 24 narrowing
attempts per root for separated order. Remaining overlap/touching is `Unresolved`.
No descriptor-ID tie break exists. Event priority cannot participate before proven
time equality. `PhysicalEventEpoch`, `PhysicalEventOrderKey` and the canonical
integer comparer are unchanged.

There is no certificate serialization in this candidate. Replay reconstructs and
revalidates the frozen relation against current authority; it never trusts a saved
certified flag. Cross-engine equality is conservatively unresolved unless time
separation is provable. In-memory use remains single-writer and allocation-free.

## M14.8 witnesses and future use

Admission retains two freshly evaluated M14.8 numerical endpoint observations.
For each, an independently enclosed physical radial gap supplies a sample-specific
`observed - mathematical` error interval. This is an a-posteriori bound on that
actual sample, including inherited FP64 anchor/evaluator/composition effects. It
is not a uniform error allowance for arbitrary observations, and it never supplies
sign or derivative evidence to the certificate. Tests also observe representable
refined enclosure endpoints without converting the root itself.

The provider does not supply exact event-local physical state or mutation rights.
A future consumer must preserve root identity, check current applicability, and
define certified event-local evaluation/application separately. M14.5 still
requires represented canonical `radialSignedGap == 0`; this candidate neither
snaps a sample to zero nor bypasses that admission. Fractional/root event execution,
general terrain discovery, rotating features, contact response scheduling,
support/rest and landing remain absent.

[Evidence, measurements, red-team results and reproduction](engineering-evidence/florida-monotone-contact-certification/README.md).
