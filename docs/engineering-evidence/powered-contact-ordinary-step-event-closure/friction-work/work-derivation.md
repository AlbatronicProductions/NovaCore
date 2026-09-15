# Two methods: disposition and exact accounting

## Definitions are not interchangeable

A physical continuous work is integral(force dot point velocity) dt, with
the frozen separate twist torque integral. A discrete solver energy change
is the change caused by an actual ordered impulse application. An endpoint
or midpoint impulse/slip product is merely a proxy absent its own derivation.
A conservative physical enclosure needs proved assumptions about every
physical history it encloses. This revision qualifies the explicitly allowed
METHOD A discrete approximation on two witnesses. It does not claim to have
observed continuous forces or qualified Method B.

## Method A: exact ordered impulse-work approximation

Let u=(v,omega), W=diag(inverse mass, inverse world inertia) and M=W^-1.
For generalized impulse p=J^T delta_lambda and exact u_plus=u_minus+W*p,

    K(u_plus)-K(u_minus) = u_minus^T p + 1/2 p^T W p
                       = (J u_minus)^T delta_lambda
                         + 1/2 delta_lambda^T J W J^T delta_lambda.

This is the full linear AND rotational kinetic-energy identity, not scalar
endpoint slip times a final patch impulse. For actual rounded updates the
observable is defined directly as K(the stored after bits)-K(the stored
before bits). The seven captured inverse-metric components and six velocity
components are interpreted as exact rational numbers. M is inverted exactly.
Thus FP32 update rounding is included in the represented energy change; it
is not subtracted, tolerated or assigned to an unobserved residual bucket.
Equivalently, use the effective represented kick M*(u_plus-u_minus) in the
identity. This does not assert the intended cached impulse maps to velocity
in exact real arithmetic with no rounding.

The observer records EVERY actual row application: tangent block, four
normal rows and twist row. Normal-stage energy is accounted independently
and is never labelled friction. The exact sum of all 54 row changes must
telescope to the stage's K_end-K_start. External acceleration is integrated
before this contact stage, not mislabelled a friction-row update.

### Attribution and chronology

The actual fixed numerical order makes this discrete channel attribution
well-defined. It is not invariant under arbitrary reordering. Normal and
tangent rows couple through shared velocities and J_t W J_n^T; normal load
also changes friction caps. A conceptual zero-net sequence t:+a,n:+b,t:-a,
n:-b can leave final energy unchanged but allocate -a*b*J_t W J_n^T to
tangent and the opposite to normal. That is an illustration of why endpoint
data alone is insufficient, NOT a claim that this loop occurred in BEPU or
that its eight iterations have unbounded behavior.

The discrete quantity is compared independently to the frozen physical
reference as an approximation, as the ticket's Method A permits. Neither
an empirical small difference nor the algebra converts solver iterations
into physical time. No unique continuous physical channel decomposition or
general error theorem is claimed. A demand for a literal continuous-time
reconstruction would remain unfulfilled by this method; that is a different
claim from this bounded discrete-work qualification.

### Warm start

Each warm application is included once as the numerical seed of THIS solve,
then all signed new-minus-old corrections are included. For fixed geometry,

    lambda_warm + sum(delta_lambda_corrections) = lambda_final.

This telescopes exactly for every component in both traces. Adding the
warm cache to the final cache would be wrong. Multiplying final impulse by
final or midpoint slip would also omit the actual intervening normal and
rotational changes. Positive correction energies are retained, not clamped
to zero. Warm application is not asserted to be an independent new physical
friction event. Its role is justified only in the ordered discrete ledger.

Off-COM has 3 positive tangent and 3 positive twist application energies
(including extremely small warm effects); yaw baseline has 2 and 2. The
signed sums, not absolute dissipations or negative-only sums, are compared.

### Rotation / multi-contact scope

J_t contains both t and r cross t. All angular axes and the FULL native
world tensor enter K. Four normal rows are observed separately; the tangent
pair forms one block, as in native source. Twist includes n dot omega and
the same inertia metric. No extra rolling row is introduced. Ordinary
off-COM source stiffness/load redistribution is not replaced by the rigid
oracle's per-pad load rule.

The first offline checker attempt rejected an exactly-diagonal metric
assumption: the observed constant world tensor has inverse YX approximately
-5.276695328188779e-23, despite spherical source inertia. The decoder was
corrected to invert the full captured symmetric matrix. The same trace was
reread; no world rerun, work-definition change, tolerance or physics edit.
Independent Gaussian-elimination inversion later reproduced every exact
row energy, independently of the lead checker's cofactor inversion.

## Method B: conditional physical enclosure, NOT QUALIFIED

This is the second and final method considered, not a fallback correction.
Let P_N be a proposed total nonnegative physical normal impulse, P_t total
tangent impulse, k=mu/4, rmax maximum fixed patch radius, and a known external
acceleration. IF fixed-geometry Newton-Euler histories have those totals and
physical tangent/twist total variations <=k*P_N and <=k*rmax*P_N, define

    U_t = H*||J_t a|| + P_N*max_i||J_t W J_Ni^T||
          + k*P_N*||J_t W J_t^T||
          + k*rmax*P_N*||J_t W J_tw^T||.
    W_t lies in u_t0 dot P_t +/- k*P_N*U_t.

Replace J_t by J_tw for the analogous twist bound. The triangle inequality
and bounded impulse variation justify these conditional bounds, including
changing slip direction or reversal. They do not mistake net impulse norm
for total variation or use the reference value to choose their widths.

The off-COM scales suggest a tangent uncertainty of order .03-.04 J could
fit the bar, but this is NOT a certified numerical enclosure. The required
physical measure/history premises do not follow from final native cache
caps. Numerical warm application and corrections can cancel; fixed geometry
during solving does not certify a continuous force/slip path. Native final
pose drift and compliant normal response also require a justified physical
interpretation, not an assumed midpoint/constant-force completion.

Therefore Method B is NOT QUALIFIABLE FROM CURRENT ASSUMPTIONS. Width alone
does not reject it; no global impossibility or minimum uncertainty >.07848 J
has been proved. No enclosure PASS, no outcome-only exemption, and no contract
responsibility mismatch are asserted. Method A succeeded without this premise.

## Cheap gate and independent comparison

Method A has a determinate exact represented-state energy sum and no missing
measurement uncertainty once all rows are captured. The cheap gate therefore
allowed observation; it did not predict an automatic PASS. Only off-COM and
one yaw-baseline confirmation were run. Candidate energies were calculated
before independent exact reference comparison. The reference uses Fraction
chronological integrals; the candidate uses recorded native KE differences.
They share physical inputs, not a solution/work computation.

| Witness | Discrete tangent J | Reference tangent J | Absolute error J | Discrete twist J | Reference twist J | Absolute error J |
|---|---:|---:|---:|---:|---:|---:|
| Off-COM | -.10489132573382745 | -.10071250563561596 | .004178820098211499 | -.012746373304393543 | -.012058014252498118 | .0006883590518954252 |
| Yaw baseline | -.16016054537093918 | -.16183186831436158 | .001671322943422384 | -.013582404554803251 | -.013582083825982605 | .0000003207288206458197 |

Every separate error is strictly below .07848 J. Off-COM tangent comparison
includes its reference tail 2.0911174678712777e-85 J in the upper bound.
Normal energy, excluded from friction, is -.040008379076944385 J off-COM
and -.1086114904891785 J yaw baseline. Total contact energy is respectively
-.15764607811516537 and -.2823544404149209 J.

The original native observations, contact sets, cached impulses, prepared
inputs, exact ledgers, states and float bits all match their retained files.
Only the copied DLL directory string differs; both BEPU DLL bytes are pinned.
No internal native-only shadow replay was used: source-level arithmetic
identity plus these runtime comparisons support the diagnostic equivalence.
There is no claim of a general JIT-equivalence proof beyond these witnesses.
