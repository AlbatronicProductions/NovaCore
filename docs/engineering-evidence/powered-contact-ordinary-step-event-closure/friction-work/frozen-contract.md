# Frozen friction-work contract

Read directly on 2026-09-15. The prior reports and numerical results are unchanged.

Sources: [model-contract](../../powered-contact-chronological-event-feasibility/model-contract.md),
[reference derivation](../../powered-contact-chronological-event-feasibility/reference-derivation.md),
[chronology.py](../../powered-contact-chronological-event-feasibility/chronology.py)
(`ref`, lines 66-97; `thresholds` and `comparison`, lines 168-182), and the
[ordinary plan](../plan.md).

## Quantity and scope

For exact H=1/60 s, hp=fuel/q, hc=H-hp:

    W_t = -k integral_0^H N(t) v_x(t) dt
    W_twist = -c integral_0^H N(t) omega_y(t) dt

These are TWO separate signed works ON THE BODY, in joules. Both are
negative in the admitted positive-slip, positive-load regime. k=mu/4=1/8
when tangent sliding is enabled, otherwise zero. c=k*r when twist is
enabled, otherwise zero; r is the exact rational import of the original
binary64 sqrt(1.25) metres. There is no independent rolling-work quantity,
no normal work in the rigid reference, and no combined-work error gate.

Four normal pads contribute summed N. The reference uses ONE shared tangent
center (0,-1/2,0) and one twist row, not four independent Coulomb cones.
The engine mount is (0,0,e), inertial force (Fx,Fy,0), torque (-e*Fy,e*Fx,0).
During power m=m0-q*t and N=g*m-Fy; during coast m=8 and N=8*g. g=9.81.
I=(2,2,2) kg m^2. Rigid constraints impose vy=wx=wz=0. Pads, normals and
wrench are frozen within the reference. Positive pad loads, tangent slip
and enabled yaw are admitted throughout; no reversal/departure or evolving
contact geometry is modeled. The native compliant fixture is not this ODE.

The reference separately integrates changing-mass powered motion, then dry
coast, across exact hp. In `ref`, v'= (Fx+k*Fy)/m-k*g and
omega_y'=(e*Fx-c*N)/I. No duplicated exhaust momentum term occurs.

## Precision and literal bar

The independent Fraction reference integrates the inverse-mass polynomial
n=0..16 with a proved tangent-work remainder below 1e-45 J on admitted rows.
Twist work is an exact rational polynomial integral. The historical separate
700-digit candidate comparison required agreement within 1e-40 SI units.
Exported ordinary reference strings have 24 fractional exponent digits;
the existing native report parsed them as binary64. Those reporting values
are not a new oracle or extra reference precision claim.

Each of W_t and W_twist has a SEPARATE absolute error bar 0.07848 J. There is
no frozen interval-width/radius rule. A future enclosure must contain the
reference AND prove the original scalar-error requirement; narrow width
alone is insufficient. No work-outcome-only exemption was found.

Historical `comparison` treats error >= bar as material; the later ordinary
plan writes <=. A strict interior proof error <0.07848 would satisfy both.
No equality-edge outcome is accepted or evaluated in this revision.

The original prose calls these engineering materiality judgments derived
from support scales and lists m=8 kg, g=9.81, delta=.001 m. Their product is
0.07848 J. This is an arithmetic reconstruction consistent with the source,
NOT an explicitly documented historical derivation: the original file does
not state the equation m*g*delta. The literal scalar quantities, units and
bar are unambiguous. No physical-product requirement or tolerance is changed.

## Witness coverage

The chronological work-bearing rows are friction, off-com, tiny-positive,
tiny-zero, near-unloading, tipping-relevant and late-exhaustion. The ordinary
matrix additionally has matching sliding/yaw baselines, and uses mid-event
for the prior friction row. Ordinary centered/low-thrust rows have zero
enabled reference work. Off-COM carries both tangent and twist work and
normal/rotational coupling, making it the informative retained witness.

Retained off-COM reference: W_t=-0.10071250563561596 J,
W_twist=-0.012058014252498118 J (binary64 report values). Its midpoint/end
proxies remain unqualified. No new reference calculation or world run was
used to make a candidate work quantity agree with these values.
