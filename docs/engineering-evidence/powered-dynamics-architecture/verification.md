# Independent review and preservation

Architecture review only. No production implementation, benchmark or physical qualification is claimed.

## Review responsibilities

The lead authored all new evidence. Separate read-only workers inspected current NovaCore dynamics, publication/lease ownership, and exact BEPU/current KSA source. A reviewer independently challenged the complete proposed architecture against all 22 user attacks; a second reviewer challenged the numerical equations/adapter specifically. Neither changed candidate production/tests or authored the proposed implementation. Current-source facts were verified rather than inferred from past ticket status.

## Findings that changed the draft

1. **Constant inertia is not a zero-flux proof.** Resolved by an explicit proposed net-effective open-system wrench closure, with a counterexample showing outgoing angular momentum can exist despite constant stored I. This is a model decision, not real-nozzle certification or an M14.24 theorem.
2. **Conflicting older starvation advice.** Current M14.24-banked resource contract is later and explicitly Enabled+NoFeed. Preserve it; do not add OffOnStarvation. Preserve the still-valid accepted ActuatorRevision rule for every applied interval/frontier.
3. **Weighted derivative overflow.** The complete `(h*delta_lambda)*f` including RK coefficient must be safely evaluated; do not first materialize overflowing derivatives or underflowing h. Existing unscaled gyro/quaternion primitives may be reused only where their intermediates are proved safe in the admitted domain.
4. **Error estimator versus acceptance proof.** Acceptance explicitly requires every stage inside the declared domain and conservative component truncation/rounding/normalization bounds within budget. A step-doubling pass alone is insufficient. Implementation must establish width, domain, work and error bounds before claiming physical qualification.
5. **Same-epoch validity stripping.** Current `Spacecraft/Rotation/Transactions/RigidBodyTorqueTransactionEvaluator.cs:21-42` can read current-time state and construct a fresh torque record. The architecture now explicitly protects endpoint-only validity through same-epoch force/torque/attitude/paired replacement paths, copied-source identity/equality/hashes/seals, and requires a qualified explicit regime-transition receipt to regain propagation rights. The future test must attempt the replacement and then future integral/rational/paired/proof evaluation. Legal current-endpoint observation/preparation stays available.

The last finding initially returned REVISE from the independent full reviewer. It was resolved in documentation before final closeout; final reviewer disposition is recorded below after rereading the correction.

## Causal and authority attack matrix

| # | Attack | Architecture response |
|---|---|---|
| 1 | Exhaust double counted | Effective wrench once; logarithmic solution only an independent oracle |
| 2 | Wrong variable-mass equations | Declared net-wrench closure and explicit open-system balance; no real-nozzle overclaim |
| 3 | Old-mass integration then mass edit | Exact-law stage mass; analytic counterexample distinguishes the error |
| 4 | BODY thrust frozen in root | Coupled orientation at each derivative stage |
| 5 | Exact depletion disappears numerically | Exact ordered ratios plus scaled weighted RHS; nonzero tiny-effect witness |
| 6 | Wrong segment order | Powered prefix then unpowered from its private endpoint; no canonical intermediate publication |
| 7 | Motion without resource commit | One joint physical/resource successor; legitimate no-consumption intervals explicit |
| 8 | Fuel without evaluated motion | No stage debits; whole bundle refuses on physical failure |
| 9 | Hardware ahead of physics | Actual application state distinct from private preparation cursor; one commit |
| 10 | Early lease retirement | Retry retains matching sources; consumption tied to canonical success |
| 11 | Canonical rollback | Forbidden after success |
| 12 | Ack failure masked | All successors remain committed; leases dead; distinct terminal private invalidation |
| 13 | Incoherent mass/COM/inertia | Point law exact fuel plus dry mass; fixed dry COM/inertia |
| 14 | BEPU update assumptions | API feasibility only; no powered-contact qualification claim |
| 15 | Warm-start policy guessed | No invented mass-ratio scaling/reset; separate later qualification |
| 16 | Backlog stale resources | Fresh proposal from prior committed successor each exact interval |
| 17 | Render/host changes physics | Fixed original lattice; deterministic numerical work; copied presentation |
| 18 | Revisions/history diverge | State/Actuator once per applied interval; Resource iff exact change; one joint record |
| 19 | Free-flight architecture blocks contact | Shared authority/model data with a separate constrained adapter |
| 20 | Contact machinery bloats free flight | BEPU is absent from the first physical implementation |
| 21 | Another invisible preview layer | First ticket must integrate and publish actual resource/actuator/motion |
| 22 | Visible route bypasses authority | Canonical copied endpoint and sticky endpoint-only validity, including same-epoch replacement |

## Executed cheap evidence

`python arithmetic-witness.py`: PASS. Standard-library Fraction/400-digit Decimal only; no canonical state, production prototype or measured performance.

- Exact powered 15625/6 and unpowered 84377/6 ticks sum to 16,667.
- Changing-mass analytic delta-v:2.92825791916637668788 m/s; source-mass shortcut:2.92682926829268292683 m/s.
- Corresponding full-interval positions:0.04499181863057905995 m and0.04497048780487804878 m.
- h=2^-1075 seconds rounds to 0, but impulse/delta-v at the declared 1 kg dry fixture rounds to 1.482e-320.
- Constant-I hypothetical exit spin flux is nonzero, disproving automatic zero angular-flux inference.
- An ordinary gyro intermediate overflows for omega_x=omega_y=1e160 and I=(1,2,3), yet weighting by 2^-1075 s gives a finite Z increment near -8.2344e-5 rad/s. Full weighted arithmetic or explicitly proved safe intermediates are necessary.
- With 1 kg fuel, 1e-12 kg dry, q=T=64 and a powered 1/64 s, analytic delta-v is about 27.631 m/s, while the terminal RK contribution alone at illustrative dyadic depth 20 exceeds 158,945 m/s. A finite depth alone cannot qualify every finite mass ratio; this is not a recommended depth.
- Original1200-interval lattice has 400 intervals of 16,666 and 800 of 16,667 ticks; total 20,000,000. This is integer algebra, NOT a new physical trajectory run.

## Preservation and reproduction

All 1,952 initially tracked files were SHA256-fingerprinted before and after. No tracked file changed. All 64 historical tag refs unchanged. HEAD/main/origin/main and remote main remain M14.24; current branch remains `codex/finite-propellant-segmentation`. Initial untracked list was empty; new output is only this bounded evidence directory. No staging, commits, tags, merges, banking, cleanup of unrelated evidence, Blender or production/test changes occurred.

The identity record retains the aggregate byte fingerprint, exact encoding, selected relevant source hashes, local KSA identity/source hashes and pinned external references. Reproduce the source comparison with `git ls-files` order and SHA256 of each raw working-tree file; concatenate `path + TAB + uppercase hash + LF`, UTF-8 without BOM, then SHA256 that text. Tags use `git show-ref --tags` lines with LF. Check each relevant source hash against the record and the current historical commit; do not substitute stale qualification wording for refs.

Run the algebra witness, validate local Markdown targets, parse identity JSON, check trailing whitespace in this untracked package, then `git diff --check`, `git diff --cached --check`, `git status --short`, and local/remote refs. No solution build or acceptance rerun is required for unchanged source/tests.

Final independent full review: **PASS, all 22 attacks resolved at architecture scope**, after rereading the same-epoch correction. Independent numerical review: **PASS**, after rereading complete weighted-RHS and conjunctive error-acceptance rules. No remaining architecture blocker was identified. All implementation arithmetic/error, physical, atomic-failure, allocation, performance and manual gates remain required; neither review grants implementation or banking authority.
