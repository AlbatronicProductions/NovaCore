# Qualified root contact kinematics — unbanked candidate

2026-09-10. **PASS — return UNBANKED for Project Control acceptance.**
Accomplishment: **Evaluate qualified contact kinematics at provider-owned
certified roots.** No milestone number is assigned.

[Current contract and derivation](../../certified-root-contact-kinematics.md)
owns the supported domain, equations, qualification semantics and future consumer
boundary. This package retains measurements and reproducible validation, not
runtime inputs. Size budget: 40 KiB across this report and the two JSON files.

## Baseline and scope

Start: clean `main`, HEAD/main/origin/main at banked M14.9
`9698a08b84b9b40b0fa03bec8f8ba80d1d7404e7`. Development branch:
`codex/certified-root-contact-kinematics`. [Identity](identity.json) records all
M14.1–M14.9 tag targets and affected source/test byte fingerprints.

Production scope is three files: the new internal `FloridaContactKinematics.cs`,
additive direction/kinematics methods and an arithmetic-preserving orientation
helper extraction in `FloridaContactMotion.cs`, and partial declarations in
`FloridaContactProvider.cs`. All other **222 existing production C# files** are
byte-identical to the initial 224-file source inventory. There is no changed
timeline/clock/transaction/response mechanism, public time, frame authority,
terrain asset, launcher, native rendering or visible route. M14.5 still requires
represented canonical zero gap; no M14.4 canonical intent is emitted at a root.

Permanent tests add an independent decimal oracle and mathematical/real-terrain
cases. Existing fixture visibility and test registrations are the only changes
to banked test workloads. Affected current documentation is reconciled with
banked M14.9; dated historical reports remain unchanged.

## Decisive physical witness

The actual acquired Florida query supplies the existing plane. The fixture has
mass 8 kg, principal inertia (2,3,4) kg m², authored offset (1,-2,3) m,
nonidentity fixed attitude derived from stored (0,0,0.6,0.8), and nonzero constant
root force. Its canonical source cell is [0,1] seconds relative to the Earth seed.

| Field | Qualified result |
|---|---|
| Root enclosure, seconds | [0.480010986328125, 0.48001861572265625] |
| Independent root bracket, seconds | [0.48001532256603240966796875, 0.480015337467193603515625] |
| Normal X | [-0.8273275985351167, -0.8273275931022064] |
| Normal Y | [-0.2937003758881045, -0.2937003704344701] |
| Normal Z | [0.4788205718239731, 0.47882057182397797] |
| Relative normal velocity, m/s | [-1.0832613122806811, -1.0832575267214317] |
| Independent material velocity at oracle bracket center, m/s | -1.0832595112553633842299820061 |
| Root lever, m | Conservative component enclosures around (2.2,0.4,3), recorded in JSON |
| Refinement work | 1 for coarse request; 17 for fine request |

The center value above is diagnostic oracle output, never a production event
time/state. The independent whole-root bracket lies inside the qualified
enclosure. A derivative bound encloses the entire unknown-alpha normal and
material velocity, not just sampled values.

The oracle derives Earth acceleration/jerk from exact stored seed bits using
decimal arithmetic. Its guarded one-second domain gives snap at most
1.36e-13 m/s⁴, position remainder at most 5.67e-15 m and velocity remainder at
most 2.27e-14 m/s. A conservative decimal operation/physical-scale budget gives
1e-10 m and m/s oracle error. Whole-bracket variation uses component normal
derivatives, |n''| ≤ 2e-8, and |u_normal'| ≤ 10.4. These are test-oracle bounds,
not widened production qualification thresholds.

Mathematical controls independently cover rational and nonrational roots,
irrational inclusion by squared-endpoint inequalities, nonunit Up, quaternion
matrix rotation, all Earth orientation terms and material-point relative velocity.
Synthetic nonzero pole/spin rates exercise the actual production inverse-rotation
derivative against independently composed root angular velocity. No synthetic
test model can issue a provider certificate.

At start 3,599 seconds, the banked source coverage remainder remains
0.05123638785047009 m. A coarse approaching-root proof exists, but the same fine
kinematic request returns **Unresolved / NumericalResolution**. A sub-arithmetic
fixed-lever width and insufficient refinement budget also refuse without
destroying the original root proof.

## Lifetime and red team

Authority review: **PASS**. Private construction and checked reads preserve
same-owner root lineage. Tests reject default/clear/non-root proofs, mixed roots,
equal-time independently issued owners, copied terrain provenance, changed
geometry/model/graph/engine, timeline-only cancellation, canonical state/force
changes and direct torque changes with an unchanged timeline revision. Frozen
mass/inertia have no new mutator. Repeated and seeded reordered requests leave
root, state, timeline and pending count unchanged. Independent replay reproduces
the bounds but cannot borrow the old witness.

Math review: **PASS**. The strongest objection was sampled oracle agreement and
an algebraically cancelling synthetic velocity check. Final tests enclose the
entire independent root bracket and directly exercise the production derivative.
The source formulas required no mathematical correction. A copied value record
is not authority; future consumers must retain the checked witness boundary.

During fixture bring-up, a coarse request with zero refinement budget correctly
returned Unresolved because its widths were not met. The fixture now allows the
declared bounded refinement with the **same width requests** and succeeds after
one step. Zero-budget refusal remains tested. Routine new-test compile errors
were corrected; no failed permanent gate was waived or profiled.

## Allocation and performance

Fixed plan: 32 warmups, 101 ordinary-runtime timing samples, then eight separately
checked allocation calls. Setup allocates the provider; reusable operations use
the existing checked 1 MiB test-only no-GC measurement. Entry/exit failure is
fatal, exact zero is unchanged, and the shared byte[128] control detects 152
bytes. No production GC/tiering/PGO setting changes.

Release timings below are microseconds; [validation.json](validation.json)
retains both configurations, timing summaries and work/size values.

| Operation | Median | P95 | P99 | Maximum | Allocated bytes/call |
|---|---:|---:|---:|---:|---:|
| Admission and root certification, scratch preallocated | 841.7 | 856.6 | 890.9 | 915.0 | 1,672 |
| Qualified evaluation of already refined root | 83.2 | 83.9 | 86.5 | 95.9 | 0 |
| Qualification with 17 refinements | 2,245.7 | 2,259.7 | 2,274.3 | 3,133.2 | 0 |
| Checked witness read | 0.8 | 0.9 | 1.3 | 7.5 | 0 |
| Coverage-floor refusal | 17.0 | 17.1 | 17.1 | 21.9 | 0 |
| Stale refusal | 0.2 | 0.2 | 0.2 | 0.2 | 0 |
| Unsupported refusal | 0.1 | 0.2 | 0.2 | 0.2 | 0 |

The witness is 416 bytes, its returned snapshot 352 bytes and request 40 bytes.
No additional heap workspace is used. Existing admission's four scratch arrays
are reused; their banked payload is 3,520 bytes. A request performs at most 25
value evaluations and 24 sign/domain evaluations. These are one-point CPU costs,
not renderer or future contact-loop budgets. Repeated fine qualification has a
material millisecond cost; no unsupported scaling or microsecond threshold claim
is made.

## Validation

Final Debug and Release full solution builds: **PASS, zero warnings/errors**.
Full Simulation: **41/41 groups per configuration**, zero failures/skips. Focused
new mathematical and actual Florida witness tests pass in both configurations.
All declared reusable allocation paths are exactly zero; deliberate allocation
control is 152 bytes in both.

ReferenceFrames: **11/11 per configuration**. Precision: **PASS both**. Focused
M14.2 translation, M14.3 contact generation, M14.4 atomic response, M14.5 isolated
response, M14.6 epochs, M14.7 exact-event motion, M14.8 Earth observation and
M14.9 proof regressions pass in both. Relevant Graphics headless tests pass:
canonical physical query/stale snapshot (2), production contact generation (1),
exact-event Earth-relative terrain (1), banked Florida certification (1), new
qualified Florida witness (1), **6/6 per configuration, zero skips**.
No GPU/window campaign or manual visual acceptance was required.

`git diff --check`: PASS (only repository line-ending notices). No staging,
commit, tag, merge, push or banking occurred. M14.1–M14.9 refs remain unchanged.

## KSA / BRUTAL provenance

The current installed KSA version and cached matching source hashes were
rechecked; [identity.json](identity.json) records them. Current
`VehicleUpdateData.Prepare` / `VehicleUpdateState.PrepareFromVehicle` copy current
kinematics; `ConstraintSim.BeginContactPass` clears transient evidence;
`PhysicsBubble.PublishResults` exposes nonfailed results; and
`VehicleUpdateTask.ApplyResultsToMainThread` applies/synchronizes before render
events. **ADOPT** prepared evidence separated from application; **ADAPT** its
lifetime to NovaCore root/current-authority checks.

Previously authenticated official live-changelog reads from the preceding
assessment are reused narrowly: [4866 prepass/contact-state changes](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037),
[4867 angular-frame correction](https://discord.com/channels/1260011486735241329/1260112103134724146/1524584112093007943),
[4874 inertia correction](https://discord.com/channels/1260011486735241329/1260112103134724146/1524639263658999972)
and [4878 contact kinematics measurement](https://discord.com/channels/1260011486735241329/1260112103134724146/1524677633143476276).
These explain ownership/lifetime corrections; current installed source establishes
what survived. No new broad history reconstruction or private message was sent.
**INTENTIONALLY DIFFER:** NovaCore's unknown-alpha enclosure contract cannot use
float solver time, speculative margins, sleep flags or approximate endpoints as
exact evidence. Bepu and a generic vehicle framework are not introduced.

## Reproduction and storage

From `E:\NovaCore`, build `NovaCore.sln` with `dotnet build -c Debug --nologo -v
quiet` and the corresponding Release command. Run the matching configuration's
`tests/NovaCore.Simulation.Tests/bin/<Configuration>/net10.0/NovaCore.Simulation.Tests.dll`
with `dotnet`, once without arguments for all groups and once with
`--root-kinematics-only` for the independent mathematical gate. Existing focused
switches are listed in [validation.json](validation.json).

Run matching ReferenceFrames and Precision test DLLs. For Graphics, run
`tests/NovaCore.Graphics.Tests/bin/<Configuration>/net10.0/NovaCore.Graphics.Tests.dll`
with `--category=headless` and each exact `--test=` filter recorded in validation;
the new filter is `Qualified Florida root contact kinematics`. The real acquired
Earth runtime assets must be present. The new gate emits compact numeric JSON
witnesses/costs and checked allocation results. Timing and allocation remain
separate; no observer/profiler setup is needed.

Retained: this report, `identity.json`, `validation.json`, permanent tests and
current contract. No ticket scratch/comparison tree, raw stdout, dump, observer,
temporary reporter or profiler output was written. Disposable investigation
output created/remaining: **0 bytes**. Standard normal build outputs remain in
their usual locations. There was no filesystem cleanup or unrelated retirement.

Return UNBANKED. Stop for Project Control; no next responsibility is started.
