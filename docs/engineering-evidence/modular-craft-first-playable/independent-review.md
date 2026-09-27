# Independent review retained at the stop

These read-only reviews were requested for the user's explicit independent qualification requirement. They are preimplementation analysis except for the Gate 1 source review and executable counterexamples. They are not Gates 4/8/10/11 PASS.

## Gate 1 source review

Reviewer: `capsule_closure`. Independently found interface-only resource resolution/dependency omission and null placement dereference. Root reproduced both through `--modular-gate1-redteam`. Root separately found and corrected legacy schema admission once. No additional demonstrated Gate 1 defect was reported. Later flight/compiler gaps were correctly excluded from this gate's defect count.

## Six-definition planning reconstruction

Reviewer independently recovered the original planning calculation and reconstructed: short/long dry mass 504/564 kg, wet mass 1304/2164 kg, TWR at 9.81 m/s² 2.401455883/1.447088018. Full COM from adapter origin: 0.620398773/1.394177449 m. Inertia diagonals: short [250.769333,774.999960,775.433293], long [379.069333,2070.694507,2071.127841] kg m². No starter balance correction indicated.

Each short store occupies 0.32 m³; each long store 0.64 m³. Coaxial regions use inner cylinder radius sqrt(0.125), outer annulus to 0.5 m, shared spans 0.814873309/1.629746617 m. Adapter's 13 regions sum to 100 kg, COM [-0.6,0,0], diagonal [49.351,48.192166667,48.6255].

Neutral foot loads 3198.06/5307.21 N; equal two-foot loads 6396.12/10614.42 N remain below the proposed 15 kN per foot. Pure 2° geometric support margins are 0.565478/0.538457 m; planning values 0.505/0.478 include approximately 60 mm conservatism. At conditional ±0.05 rad gimbal, ground clearance is 0.488630 m. Content must declare the actual limit; this calculation does not authorize an unrecorded value.

32 jets can realize axial ±X and tangential ±t per block at radius 0.75 m. Opposed axial pairs give 22.5 Nm pitch/yaw; eight equal-sense tangential jets give 90 Nm roll with zero resultant force. Three axes use 12 distinct jets, 0.05859375 kg/s. Exhaust centerlines clear adjacent boxes by 42.893 mm; this is not plume-heating qualification (spreading above roughly 5.263° can impinge).

## Variable mass

Reviewer: `architecture_verifier`. Existing co-moving point-removal idealization can extend to distributed proportional co-moving removal plus one complete effective actuator wrench. Density law alone does not prove resolved internal tank-to-nozzle transport. No slosh/internal relative fluid momentum or additional rocket impulse is implied.

For fixed material origin, m = md + sum(ms); S = Sd + sum(ms rs); c = S/m; J = Jd + sum(ms [Ks + P(rs)]); I = J - mP(c), with P(r) = |r|²1 - rrᵀ. Then cDot = (SDot - mDot c)/m, IDot = sum(mDot_s [Ks + P(rs-c)]).

Outgoing co-moving angular flux is -IDot omega. It cancels IDot omega in the retained-body balance, leaving I alpha = torqueOrigin - c cross force - omega cross (I omega). Adding a naked -IDot omega or preserving retained I omega by rescaling spin creates an artificial impulse. Material-origin acceleration is R[force/m - alpha cross c - omega cross (omega cross c)] plus the qualified gravity law.

Rigid velocity at the instantaneous COM is vO + R(omega cross c). Derivative of the moving geometric COM additionally includes R cDot. Existing ToCom/ToOrigin use the former. For BEPU re-centering, delta = R(c1-c0); xBody += delta; vBody += omegaRoot cross delta; keep orientation/angular velocity, translate child offsets oppositely, install full inverse inertia and refresh bounds. This must preserve every material point's position/velocity. Existing endpoint refresh is a useful seam but does not qualify force/event integration over a depleting contact step.

Manufactured principal-axis witness: md=2, ms=1, msDot=-0.3, rs=(1,0,0), Ks_zz=0.2, Jd_zz=1, omega_z=2 gives Izz=1.8666667 and IDot_zz=-0.1933333. Correct zero-torque alpha_z=0; closed-system misuse gives +0.207142857 rad/s². Rotating-Earth flux must use absolute angular velocity; BEPU already supplies gyroscopic integration and must not receive it twice.

Required future witnesses: independent fills/full tensors; momentum balance with expelled flux; material-point invariance; supported spinning depletion and warm-start convergence; exact power/fuel boundary; rotating-frame equivalence; handoff continuity; save/replay of law identity. No dynamic PASS claimed.

## Finite power and 32-jet ownership

Reviewer: `ksa_frames`. No inherent second-owner requirement found. Constant enabled controller load is 56 W; compute powered duration h=min(T,E/P) exactly, then coast through T-h. Publish services/motion/time together through the existing transaction owner. Fuel shortage stops fuel/thrust but standby electrical loads continue. Zero power freezes gimbal actuation too, while gravity, time and camera continue.

Battery 0.5 J, 56 W, main 10 kg/s, interval 15,625 microticks gives h=1/112 s (8,928+4/7 microticks), total consumption 5/56 kg (A 1/28, B 3/56), followed by coast. Integer stock stores cannot represent every such remainder. The rational generic fuel proof needs one explicit lifetime command-power denominator factor, separate from fuel-depletion event count. Do not round duration, floor remaining fuel or invent a fuel event. This is future implementation work, not a repaired arithmetic claim.

The existing cold 54-row pilot allocator can retain ownership with a bounded 32-bit set and compiled geometry, testing bits 15/16/31, save identity, disjoint combined demands and every admitted fill. `(1u << 32)-1` is zero in C#; its bound needs an explicit count-32 case. Existing contact preparation rejects partial fuel events, OFF, RCS and gimbal; generic event substeps must be qualified inside existing preparation/publication authority. Full-step average force does not prove an exact power cutoff.

## KSA clarification

The accepted plan resolves the Gate 1 definition/identity/interface/explicit-service responsibility split. It is ADAPT: immutable definitions and explicit named interfaces/services, implemented independently for NovaCore's schema and deterministic authority. No unresolved equivalent KSA mechanism was introduced in this gate, so no fresh live-changelog inspection is claimed or needed to justify it. The user's later clarification is retained: before implementing a genuinely equivalent unresolved player-visible mechanism, inspect both current installed KSA and authenticated live history read-only and classify ADOPT/ADAPT/INTENTIONALLY DIFFER. Do not restart the campaign or repeat the broad study. KSA writes: 0.
