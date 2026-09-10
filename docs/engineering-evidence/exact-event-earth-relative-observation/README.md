# Exact-event Earth-relative observation — UNBANKED

Baseline: `56c0ae95b7f514e2f0440dfe272dd2836f32506e`, banked M14.7;
branch `codex/exact-event-earth-relative-observation`. M14 remains open.

## Supported model and numerical design (before validation)

Only the current immutable `SolAnalyticalDefinition.Instance` is admitted: Earth
analytical Kepler payload, fixed identity Sun ancestry, identity time mapping,
identity Earth secular/periodic corrections, and the existing linear IAU_EARTH
orientation coefficients. Other instances/models are refused, not generalized.
Canonical requests reuse M14.3 body and observation evaluation.

Fractional requests use a whole-integer-second anchor within the current +/-500
Julian-year coverage. The anchor's seconds and microticks are exactly representable
in FP64 at this scale; mapping back must equal the anchor. Subtract this anchor
from E with `PhysicalEventDuration` before converting the local duration h (<1 s).
This avoids both rounding E and the banked microtick-to-double mapping ambiguity
at late epochs. The existing bounded universal-variable Earth propagator supplies
the anchor; no other planet is evaluated for a fractional observation.

For Sun-relative r,v and the authoritative Sun mu:
`a=-mu*r/|r|^3`, `j=-mu*(v/|r|^3-3*r*(r.v)/|r|^5)`;
`r(E)=r+v*h+a*h^2/2+j*h^3/6`, `v(E)=v+a*h+j*h^2/2`.
These are a bounded numerical continuation of the same two-body ODE.
Let H=|h|, R=|r|/2, A=mu/R^2, V=|v|+A*H. The bootstrap
`|v|H+A*H^2/2<R` bounds radius below by R. Differentiating jerk gives the
conservative snap bound `S=4*mu^2/R^5+24*mu*V^2/R^4`; local remainders are
at most `S*H^4/24` metres and `S*H^3/6` metres/second. This does not certify
the inherited Kepler anchor or physical ephemeris accuracy.

Earth orientation is `Q=Z(RA+90) X(90-DEC) Z(W) X(90)`.
For rates a=RA', b=-DEC', c=W' and u=Z(RA_anchor+90)X-axis,
`Q(E)=Z(a*h) Rot(u,b*h) Q_anchor Y(c*h)`. All three rates are retained.
The angular velocity retains the numerical +/-0.5 s stencil meaning:
with u_E=Z(a*h)u, n_E=Q(E)Y-axis, d=.5 s,
`delta=Z(a*d) Rot(u_E,b*d) Rot(n_E,2*c*d) Rot(u_E,b*d) Z(a*d)`;
the shortest quaternion logarithm divided by 2d gives root-frame omega.
It is not a certified derivative.

Numerical bars separate local truncation, FP64 arithmetic and inherited anchor
error. With unit roundoff u=2^-53, gamma(n)=n*u/(1-n*u), local duration uses
a gamma(5) conversion bound (including the local whole-tick addition). Dot/quaternion products use operation-count bounds;
tests use 128u for a short transform chain, plus 16 ULP of root-position scale
for subtraction/translation. Independent orbital controls use eccentric-anomaly
and circular trigonometric increments. Orientation controls use Euler matrices,
with a phase-scale bar for the inherited absolute W evaluation. These are
representation/algorithm bars, not claims about real Earth accuracy or contact.

The orientation oracle allows 16 ULP of the unreduced W angle: four phase
construction operations plus degree-to-radian/range-reduction and comparison of
two independently rounded paths, with a factor of two reserve. Its additional
256u covers the four quaternion products (7 rounded operations per component),
axis/normalization and vector transport on both paths. The matrix-stencil omega
comparison carries both endpoint orientation bars over its one-second span.
These conservative validation allowances are not machine-certified interval
arithmetic. Integrated anti-floor witnesses separately require the orbital
position, velocity and spin signals to exceed the numerical bars.

Feature position is p_COM+Q_craft*r_body; its velocity is v_COM+
omega_craft_root cross (Q_craft*r_body). Earth-relative direction comes from
Q_E inverse applied to (p_feature-p_E). The sole M14.1 query supplies H, physical
witness and qualified normal. Radial gap remains (radius-R0)-H. Terrain velocity
is v_E+omega_E_root cross (Q_E*witness_body); relative velocity is feature minus
terrain velocity. Units are metres, metres/second, radians/second.

Exact E identifies evaluated FP64 values. Observation is not contact, a root,
clearance, certification, admissibility or mutation authority.

## Predeclared validation

Focused numerical/refusal/replay/frame tests Debug/Release; allocation in three
fresh processes per configuration, no retries, using unchanged checked 1 MiB
measurement and its positive control. Ordinary Release timing is separate:
64 warmups, 101 samples of 16 calls for canonical, fractional, component owners,
and an eight-observation batch; report median/P95/P99. Setup/acquisition excluded.
Full Debug/Release builds and one full Simulation suite each, ReferenceFrames,
Precision, focused M14.2-M14.7 and bounded headless physical-terrain integration.
No graphics/window campaign. Stop on a material unexplained gate failure.

## Result and qualification

**PASS — candidate ready for Project Control acceptance, UNBANKED.**
See [machine-readable results](validation.json) and [identity](identity.json).
Permanent package budget: 64 KiB, for equations, compact results, provenance and
reproduction only. No raw build/test logs or disposable comparison tree is retained.

| Gate | Debug | Release |
|---|---|---|
| Final full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Final full Simulation | 39/39 PASS | 39/39 PASS |
| Focused observation numerical/authority cases | PASS | PASS |
| Fresh allocation processes | 3/3, 0 bytes | 3/3, 0 bytes |
| Positive byte[128] control | 3/3, 152 bytes | 3/3, 152 bytes |
| M14.2–M14.7 focused gates | PASS | PASS |
| ReferenceFrames / Precision | 11 groups / PASS | 11 groups / PASS |
| Existing physical query, stale snapshot, canonical contact integration | 3/3, no skips | 3/3, no skips |
| New real-terrain observation integration | 1/1, no skips | 1/1, no skips |

Final default Simulation registration includes the new allocation contract.
Adding that call did not change the already measured allocation/timing bodies;
final builds and both full suites were repeated and passed. Development also fixed
a malformed nonfinite test fixture and a test-only assertion-name resolution error;
these were not production numerical or allocation witnesses. No acceptance
allocation run was retried. No unexplained gate failure remains.

Canonical intersection: 20 complete observations compare every corresponding
double bit, identity and status; 15 refusal cases compare status/surface status and
default output. Multi-feature geometries and alternate celestial definitions are
deliberately outside the new supported domain, not canonical compatibility claims.

Independent local-orbit oracles: 27 circular/eccentric cases, maximum position
difference 7.276e-12 m and velocity difference 8.135e-12 m/s. Independent
decimal-phase Euler/matrix-stencil checks: 15 cases, maximum direction difference
8.546e-11 and angular-velocity difference 1.515e-15 rad/s. The direction maximum
includes inherited large-absolute-phase rounding at late epochs. Twenty-five
integrated fractional cases include half/third/15625-over-32768, near-boundary,
tiny, negative and late odd microticks. Direct anti-floor signals exceed the bars:
15.143 mm orbital position, 3.067e-9 m/s orbital velocity, 3.646e-11 orientation.
This guards the epoch-to-helper wiring separately from the local formula oracles.

Frame qualification uses explicit lever arms and cross products; four relative
controls cover approach, separation, numerical stationarity and tangent motion.
Twenty-one refusal/admission/capacity controls and 128 seeded/reordered queries
protect default output, exact identities and deterministic bits. A real later
torque transaction proves old live-backed views are refused and retained values
remain independent. No clock, timeline, history or state mutation is performed.

Error propagation must retain owner boundaries. Feature-position error includes
COM error plus offset length times craft-orientation error and arithmetic. Relative
body-position error adds Earth-origin error and feature–Earth distance times Earth
orientation error. A norm is 1-Lipschitz, so that position bound also bounds its
radius contribution; normalizing direction divides by the nonzero radius (with
the corresponding normalization arithmetic). Witness error includes Earth-origin
error plus witness radius times orientation error and query-position error.
Transported velocity error includes origin-velocity error, lever length times
omega error, omega magnitude times lever error, and cross-product arithmetic.
The final subtraction adds both transported-velocity errors.

For radial gap the physical H query remains its own numerical/model owner. The
tests qualify composition at the returned direction. They do not invent a global
terrain Lipschitz/continuity bound from one qualified normal. The analytical radial
height oracle supplies its known slope; real terrain integration checks the exact
canonical returned H/witness/normal. No total physical-accuracy or root certificate
is claimed. Unsupported input/model checks are refusals, not approximation fallbacks.

## Bounded performance

Release ordinary-runtime samples, microseconds; setup excluded. Batch row is the
whole eight-observation operation. Analytical surface figures isolate the new
numerical path, not production terrain cost.

| Work | Median us | P95 us | P99 us |
|---|---:|---:|---:|
| Canonical observation | 16.056 | 16.425 | 18.719 |
| Fractional observation | 5.906 | 7.306 | 9.300 |
| Eight fractional observations | 52.925 | 58.681 | 59.788 |
| Spacecraft owner | 1.294 | 1.463 | 2.588 |
| Earth owner | 3.050 | 4.013 | 5.925 |
| Analytical terrain control | 0.138 | 0.144 | 0.150 |
| Real Florida terrain, complete observation | 59.563 | 295.338 | 337.175 |
| Real regional terrain, complete observation | 83.688 | 94.069 | 100.275 |
| Real inland terrain, complete observation | 62.044 | 64.413 | 66.013 |

Composition/guard/call overhead is approximately 1.425 us by subtracting separately
measured owner medians from the fractional median. This is a residual estimate,
not an isolated measurement or additive percentile guarantee. The canonical path
retains banked all-body evaluation for parity; the fractional sidecar evaluates
only Earth. Each complete query evaluates craft and Earth once and queries terrain
once. Work is bounded; no cache, I/O, GPU or unbounded refinement was introduced.
Real-terrain tail variation is retained, with no causal claim or further campaign.
These finite measurements do not establish throughput for a future root solver.

## Red-team findings and KSA boundary

- **A same-E:** PASS. Whole-second mapping is exactly representable over coverage;
  integer local differencing and direct anti-floor/oracle witnesses guard the path.
- **B frames/velocity:** PASS. Independent Euler axes, matrix stencil, transported
  lever arms and relative-motion controls retain units and cross-product sign.
- **C authority:** PASS after making composition private. No alternate producer
  accepting fabricated kinematic samples is exposed for performance attribution.
  Acquired real terrain, stale revision and complete default refusals pass.
- **D scope:** PASS. No other body, time system, discovery, certificate or response
  path is supported. Defensive mapping/duration checks are not falsely claimed as
  independently exercised model generalizations. Existing canonical method bodies
  are unchanged; the two touched celestial files only add `partial` declarations.
- **E KSA:** PASS on ownership equivalence, not on numerical algorithm equivalence.

Current installed `E:\Kitten Space Agency\KSA.dll` was reverified: version
2026.9.7.5402, revision `487c3f340de24c6a81037120b6d1129c045c5400`, SHA256
`A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`.
Matched retained decompile at `build/ksa-residency-reference/assembly-source`:
VehicleUpdateState.cs GetNewStates(230), preparation(296); PhysicsBubble.cs
temporary state preparation/detection(1369), later advancement(1412), explicit
ready-result application(664/694). This is installed/decompiled source evidence,
not an authored upstream checkout. Game authority owns persistent state; BRUTAL
concurrency/math and solver mechanisms remain consumers.

Official authenticated engineering history read during the architecture assessment:
[Revision 4659, June 18](https://discord.com/channels/1260011486735241329/1260112103134724146/1517030769091149885)
and [Revision 4866, July 8](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037)
explain preparing collision state before advancement and correcting speculative
high-warp contacts. Current source establishes the surviving boundary.
**ADOPT** authority/preparation/application separation; **ADAPT** bounded derived
sampling to NovaCore exact-time/FP64 contracts. No Bepu, margins, sleep, velocity
zeroing or solver-owned contact truth is imported. Observation remains evidence
for a future producer, without a mutation path.

## Reproduction

From repository root, use SDK 10.0.303 / .NET 10.0.12 and the required installed
physical Earth assets/cache. No profiler, tiering override or global GC change.

```powershell
dotnet build NovaCore.sln -c Debug --nologo
dotnet build NovaCore.sln -c Release --nologo
# For each configuration C = Debug, Release:
dotnet tests/NovaCore.Simulation.Tests/bin/C/net10.0/NovaCore.Simulation.Tests.dll --earth-event-only
# Three separate fresh invocations per configuration; stop on first failure:
dotnet tests/NovaCore.Simulation.Tests/bin/C/net10.0/NovaCore.Simulation.Tests.dll --earth-event-allocation
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --earth-event-performance
dotnet tests/NovaCore.Simulation.Tests/bin/C/net10.0/NovaCore.Simulation.Tests.dll
dotnet tests/NovaCore.ReferenceFrames.Tests/bin/C/net10.0/NovaCore.ReferenceFrames.Tests.dll
dotnet tests/NovaCore.Precision.Tests/bin/C/net10.0/NovaCore.Precision.Tests.dll
dotnet tests/NovaCore.Graphics.Tests/bin/C/net10.0/NovaCore.Graphics.Tests.dll --category=headless '--test=Exact-event Earth-relative production terrain'
dotnet tests/NovaCore.Graphics.Tests/bin/C/net10.0/NovaCore.Graphics.Tests.dll --category=headless '--test=Physical surface-point'
dotnet tests/NovaCore.Graphics.Tests/bin/C/net10.0/NovaCore.Graphics.Tests.dll --category=headless '--test=Contact generation production terrain'
git diff --check
```

Replace `C` with the configuration name. Focused predecessor selectors remain
`--translation-only`, `--contact-only`, `--contact-response-only`,
`--isolated-contact-only`, `--physical-event-epoch-only`, `--exact-event-motion-only`.
The new real-terrain test performs three site checks/timings and separate 32-call
zero-byte windows in each configuration; existing integration remains unchanged.
No manual visual acceptance is required and no GPU campaign was run.
