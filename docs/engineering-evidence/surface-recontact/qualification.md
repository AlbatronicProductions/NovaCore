# Offline qualification and native coverage

**Offline correctness PASS; overall campaign REVISE.** Debug and Release builds
have zero warnings/errors. Full Simulation tests, eleven physical/reference
cases in each configuration, construction connectors/editor/stabilization,
viewport, resource/control dynamics, flight, save/load, unified application,
Florida pad and scalable support regressions pass.

`qualification.json` resolves the original results, reference supplement,
closure results and final observer reruns. The earlier Release terrain failure
is retained: its two-second integrator-handoff assertion was incorrect. At 128
powered ticks the craft had risen 27.775 m with zero contacts; the conservative
clearance proof allowed free-flight ownership at tick 366. The corrected test
checks physical departure separately from that proof, with no altered force or
clearance policy. Final Debug/Release terrain checks pass.

| Required behavior | Permanent witness and conclusion |
|---|---|
| 1. Gentle feet-first | Short/long/asymmetric pad matrix; native short/long visual continuation |
| 2. Harder vertical impact | 1.5 and 7.5 m/s fixtures damp to rest; no invented upright bounce or structural survival claim |
| 3. Lateral skid | Pad matrix and moving-terrain seams; friction/energy falsification |
| 4. Asymmetric tipping | Asymmetric matrix, exact mass/inertia, tip checkpoint reload |
| 5. Side/body contact | Authored compound body contacts, not feet-only support |
| 6. Roll/tumble | Short/long/asymmetric roll and tip trajectories |
| 7. Separation/recontact | Foot rocking, tip-induced whole-craft separation, three powered hops each for short/long |
| 8. Continuing rest | Three 256-interval rest windows in six performance fixtures; no sleeping/landed bypass |
| 9. Florida pad | Pad matrix, native short/long, pad-side contacts retained |
| 10. Ordinary terrain | Full-H terrain, six moving seams, three equivalent inclined-plane tessellations; native telemetry only for dark view |
| 11. Grounded engine | Exact finite-resource command/force path and powered cycles |
| 12. Grounded RCS | Physical torque/resource witness and timed grounded RCS calls |
| 13. Relaunch | Short/long powered cycles and restored terrain physical rise |
| 14. Grounded save | Exact endpoint/frontier/source checkpoint; native UI save also succeeded |
| 15. Grounded reload | Deterministic cold reconstruction; native short reload continues at rest |
| 16. Wake/continued response | No sleep optimization; thrust/RCS/collision continue ordinary integration |
| 17. Back to free flight | Pad cycles and terrain clearance handoff; private native world retired |

The latest Project Control clarification supersedes the initial request for a
separate restitution mechanism. Critical damping need not bounce on a perfectly
upright vertical impact. Recontact caused by geometry, angular motion and thrust
is positively covered without fabricating restitution.

Representative Release checks: ordinary response 10,863 (including 1,433
saturated-response steps); numerical enclosure 1,043 / 87 high-precision
fixtures; base recontact 5,632; refinement 802; 27-case pad matrix 65,205;
six terrain seams 501,763; three plane tessellations 331,779; powered cycles
3,692; final terrain 2,218; performance assertions 6,427; live-input preparation
397. Counts are assertions, not independent physical scenarios. Modular
stabilization retains depth 1/2/4/16/64/96, 901 parts and exact 1,024 capacity
refusal coverage.

Adversaries cover stale identities, malformed/nonfinite native rows, full-child
burial, motion-bound refinement/exhaustion, immutable refusal, exact resource
debit, replay partitions, reference-frame conversion, terrain certificates,
edge-manifold filtering, friction direction and endpoint energy. Contact counts
are observations of the last native slice, not maximum counts over all slices.
Endpoint energy checks are falsification tests, not a substep passivity theorem.

Independent read-only review found no hidden landed-state replacement and
confirmed the transaction/persistence ownership. Finite native-depth and
per-child geometric refusal findings are closed with permanent adversarial tests;
final red-team review found no remaining demonstrated physical-authority defect.
Final disposition review verified every canonical package file, matched pre/post
seals, and agreed that recorder loss prevents native/campaign acceptance.

Native route details and exclusions are in [native-recorder-stop.md](native-recorder-stop.md).
No automated CPU result is presented as manual Player acceptance.
