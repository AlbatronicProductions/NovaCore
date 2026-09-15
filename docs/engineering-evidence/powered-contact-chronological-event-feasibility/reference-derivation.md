# Independent chronological reference

The reference in chronology.py/ref uses Fraction arithmetic, not candidate
logarithms. It explicitly constructs the powered trajectory, transitions
at exact rational h=fuel/q, and constructs the coast trajectory. No contact
backend is called. Splitting the reference at h is allowed by this ticket.

Let f=Fx+k*Fy, N=gm-Fy during power, m=m0-qt. Newton-Euler equations are
v'=f/m-k*g and w'=(e*Fx-c*N)/I. During coast v'=-k*g and
w'=-c*g*mf/I. Normal/roll/pitch velocities are constrained to zero; reaction
loads satisfy the declared rank-three basis plus checkerboard selection.
There is no -q*v term in acceleration: the exhaust-relative wrench is
already F. Variable carried momentum/energy flux is not a second thrust.

Expand 1/(m0-qt)=(1/m0)*sum(n=0..16)(qt/m0)^n + remainder. Integrate this
polynomial exactly to obtain v(t), then integrate v for x and N*v for
sliding work. Angular acceleration is affine, w quadratic; theta and N*w
are exact rational polynomial integrals. Normal impulse is an exact affine
integral. Work is -k*integral(N*v) and -c*integral(N*w), including coast.

For delta=q*h/m0<1, the omitted velocity is bounded by
|f|*h/m0 * delta^17/[18*(1-delta)]. Position error <=H times that;
sliding work error <=k*g*m0*H times it. All other reference outputs here
are exact rational quantities. Per-case bounds are recorded and required
<1e-45 before Candidate C is evaluated. Input model constants are shared;
no candidate solution helper, logarithm or candidate endpoint feeds ref.

Admission uses exact affine reaction endpoints and possible stationary
points of v/w in each phase. The ordinary/realistic maximum mass ratio is
small; no high-depletion rational expansion is quietly admitted. Negative
mass/rank/support/friction regimes fail before candidate evaluation.

The reference validates THIS rigid frozen-patch ODE, not BEPU compliance,
quaternion propagation, contact creation or any changing lever geometry.
