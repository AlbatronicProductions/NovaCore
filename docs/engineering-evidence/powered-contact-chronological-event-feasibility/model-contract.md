# Preregistered model and decision bars

Frozen before numerical execution, 2026-09-15. This is a standalone analytical
proof, not a production BEPU implementation or a new qualified spacecraft.
Only this evidence directory may change. No builds, old qualification runs,
timing campaign, source/test edits, or evidence retirement. Retained budget:
256 KiB including reproducible source and concise JSON; no disposable trees.

## Model

One interval H=1/60 s; four frozen world-space pads (sx*1,-1/2,sz*1/2) m,
up +Y, immutable feature order (-,-),(-,+),(+,-),(+,+). Dry mass 8 kg;
dry diagonal inertia (2,2,2) kg m^2, fixed COM; gravity 9.81 m/s^2.
These scales come from the existing numerical box, not the 1000 kg article.
CentralPointReservoirV1 changes scalar mass only: m=m0-q*t to h, then 8 kg.
Binary64 q=F/5120 and binary64 fuel=q*nominal_h are imported exactly as
rationals; actual h=fuel/q, never independently rounded. The tiny control
instead declares q=2 kg/s, F=16 N, exhaust=8 m/s, fuel=2^-1074 kg,
h=2^-1075 s. This is a numerical stress case, not a realistic engine.

Normal rigidity imposes vy=wx=wz=0. Sliding uses vx>0, shared tangent center
(0,-1/2,0), T=-k*N, k=mu/4=1/8 (mu=1/2). Optional positive yaw uses shared
twist torque -c*N, c=k*r; r is the once-rounded binary64 sqrt(1.25) m.
This is the declared continuous counterpart of the retained four-contact
patch convention, NOT four independent Coulomb cones and NOT BEPU's
discrete compliant radial map. Disabled axes have zero corresponding k/c.
Engine mount (0,0,e); force (Fx,Fy,0) fixed in world space within H;
torque (-e*Fy,e*Fx,0). No exhaust momentum term is counted twice.

Independent normal basis and zero checkerboard load rule are defined in
rank-conditioning.md. All reactions and slip/twist signs must remain strictly
positive throughout; no active-set changes, departure, or penetration model.
Normal displacement/velocity/penetration are identically zero by rigidity.

Nonzero yaw does NOT turn this into a nonlinear rotating-box oracle: pads,
normals and engine wrench are frozen. Report theta, and bound omitted
body-fixed force rotation by F*H*max|theta|/8 for velocity. Actual lever,
manifold and compliant-response error are UNQUALIFIED. Consequently the
fixed-model payoff is necessary evidence, not proof of real gameplay payoff.

## Fixed run list (no tuning)

All ordinary flows use nominal exhaust 5120 m/s. vx0=1 m/s on sliding rows;
wy0=.12 rad/s on twisting rows, otherwise zero. x0=theta0=0.

| Order | Name | F N | (Fx/F,Fy/F) | e m | nominal h/H | sliding | twist |
|---|---|---:|---|---:|---:|---|---|
| 1 | ordinary-centered | 32 | (0,1) | 0 | 1/2 | no | no |
| 2 | friction | 40 | (3/5,4/5) | 0 | 1/2 | yes | no |
| 3 | off-com | 80 | (3/5,4/5) | 1/10 | 1/2 | yes | yes |
| 4 | tiny-positive | 16 | (3/5,4/5) | 1/10 | exact 2^-1075/H | yes | yes |
| 5 | tiny-zero | 16 | (3/5,4/5) | 1/10 | 0 | yes | yes |
| 6 | low-thrust | 8 | (0,1) | 0 | 1/2 | no | no |
| 7 | near-unloading | 80 | (7/25,24/25) | 0 | 1/16 | yes | no |
| 8 | tipping-relevant | 80 | (3/5,4/5) | 1/10 | 1/2 | yes | yes |
| 9 | late-exhaustion | 8 | (3/5,4/5) | 0 | 99/100 | yes | no |

Cases 6-9 are a SMALL newly authored scale study, not banked powered-contact
fixtures. 8 N/5120 m/s is source-backed free-flight scale; 80 N, direction,
offsets, event locations and initial slip are explicit proof inputs. No
claim of actual departure: near unloading and tipping remain admitted.

## Numerics and stop rules

Rank and support admission precede candidate evaluation. Reference: exact
rational polynomial chronology with 1/m geometric series n=0..16 and an
analytic remainder bound; event split allowed. Candidate: independent
700-digit Decimal logarithmic temporal moments; one full-H endpoint kernel,
ZERO backend contact calls/substeps, no event endpoint private cache.
Do not reuse candidate solution helpers in reference. Require each output
error <=1e-40 SI units and reference tail <1e-45 (angular/support exact).
Stop at first model/numerical failure; technical harness errors are reported
separately. No candidate input/threshold changes to obtain a pass.

## Controls and physical materiality (before results)

KSA-like liquid control: positive starting fuel gives full prepared wrench
and frozen source mass through H, then actual fuel clamps to zero and mass
becomes dry. It abstracts the accepted ordinary OUTER substep lifecycle;
NOT a run of KSA or a bit-exact BEPU emulation. Report demand shortfall.
Distinct average control: exact ledger, full-H duty-scaled force/torque and
frozen source mass. This is a rejected exact-chronology substitute, included
to test whether C materially beats a much cheaper approximation. It is NOT
the traced KSA liquid shortage mechanism and is NOT approved for production.
Post-impulse control: contact coast at source mass, then add integrated
engine force/torque impulse at endpoint using dry mass; exact final ledger.
No re-projection. Reject any control that violates its own regime.

Materiality is an engineering judgment derived from source support scales,
not a newly banked product requirement. delta=min feature/1000=.001 m;
R=sqrt(1.5) m, H=1/60. Thresholds: displacement .001 m; velocity .06 m/s;
angular velocity .001/(R*H); normal/tangent impulse .48 N*s; angular impulse
2*.001/(R*H); work .07848 J. Angle threshold .001/R rad. Report absolute
errors against reference. NEGLIGIBLE <=1e-12 in every reported physical
quantity; MEASURABLE BUT NON-MATERIAL below every threshold; MATERIAL
PHYSICAL DIFFERENCE if any threshold exceeded; REGIME-CHANGING DIFFERENCE
only with certified changed sign/normal admissibility beyond numeric error.
Exact fuel/order/one debit/actual dry state remain separate zero-tolerance
authority requirements regardless of physical materiality.

A full C architecture PASS also needs a credible retained-world bridge and
material benefit beyond merely winning against a weak control. If C only
replaces A with a custom contact solver or realistic benefit is negligible
relative to that cost, reject C advancement; do not weaken old contracts.
