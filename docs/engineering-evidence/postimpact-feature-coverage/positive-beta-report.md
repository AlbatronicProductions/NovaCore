# Positive-beta qualification — bounded design stopped

Baseline HEAD/main/origin/main: dc274639f670d122764bb0e8f3d0c95ae201c7a4.
Branch codex/postimpact-feature-coverage. Candidate remains unbanked.

Lead judgment: ESCALATE TO PROJECT CONTROL.
POST-IMPACT TERRAIN-CONTACT COVERAGE CANDIDATE — FOLLOW-UP REQUIRED.

## Observed design outcomes

| Proposal | Intended parameters | Executed result |
|---|---|---|
| 1 | Tangent lever50m, normal lever-2.5m, mass8kg, spherical inertia20000, inward speed50m/s, gap25m, zero force; explicit alpha refinement1e-7/24 | M14.9 admitted a certified alpha enclosure[0,1]. The requested standalone refinement refused Unresolved/NumericalResolution. No later chain or coverage search. |
| 2 | Tangent lever1000m, normal lever-100m, mass8kg, spherical inertia8000000, inward speed1000m/s, gap200m | Considered and rejected by conservative static design screening: fixed lever-width request and footprint risk. NOT a measured refusal and not proof that its interval exceeds the request. No executable run. |
| 3 | Tangent lever100m, normal lever-10m, mass8kg, spherical inertia80000, inward speed200m/s, gap100m, zero force; original qualification requests | Provider admission succeeded; M14.9 alpha evaluation returned Unresolved. Exact Failure enum was not printed by the status assertion and is UNAVAILABLE. No later chain or coverage search. |

All use identity attitude, zero initial spin/torque, real acquired Florida center
terrain, current SolAnalytical Earth, t=0 to t=1s, and one authored landing point.
The lever is represented in body/root axes using the real initial Earth transform.

Three parameter sets have been proposed; the bounded design allowance is exhausted.
No retries, fourth fixture or production rescue. Proposal1 did not prove intrinsic
source admission failure: its EXTRA requested refinement was the failing step.
Proposal3 did not reach a certified source alpha. Neither demonstrates a coverage
algorithm defect.

## Oracle / beta / checked operations

The independent reviewer found the spherical-inertia local design coherent:
g(s)=x*(sin(omega*s)-omega*s)+h*(1-cos(omega*s)) plus Earth corrections.
This is only design guidance. No proposal reached the checked M14.13 final
velocities/M14.15 inputs required for the exact same-model post-impact oracle.
Therefore no physically suitable fixture was selected by that oracle, and no
candidate coverage search was executed.

NextEventCertified, clear-prefix completeness, beta existence/uniqueness, beta<T,
opaque receipt consumption, refinement, ordering, stale/foreign rejection,
fresh-process determinism and beta nonmutation: NOT QUALIFIED by this ticket.
No beta was constructed or executed; no canonical or private endpoint published.

## Preserved evidence and validation

All nine accepted candidate production/test path fingerprints match
identity-after-causal.json after restoration. All banked production files and
55 tags remain unchanged. Temporary source exporter and runner entry were removed.
Debug Graphics rebuilt after restoration: PASS, zero warnings/errors.

Initial diagnostic compilation had three test-reporter errors (Check qualification,
TryCreate status versus bool, nonexistent diagnostic Geometry field); corrected
before the two source runs. Neither represented production changes.

No Release source attempt, three-by-three replay, full Simulation, query,
ReferenceFrames, Precision or performance campaign was run. Those gates are
conditional on positive-beta success. Prior Coast EventFreeThroughTarget,
Force Unresolved/DepartureUnproved, validation and timing remain retained results,
not new measurements in this ticket.

## Independent review

Reviewer challenged supported domain, rotating-feature design, earlier-root proof,
source identity and bounded stop. Review supports stopping: the physical local
analogy does not qualify an actual post-impact trajectory without genuine source
receipts. Proposal2 is static screening, proposal1 a refinement refusal, proposal3
an unresolved source evaluation. An extra run solely to recover its unprinted
subreason would violate the declared stop. No false-positive coverage was issued.

BEPU GATE NOT REACHED. No general contact or solver responsibility was introduced.

## Reproduction

positive-beta-source-export.cs.txt retains the small noncompiled reporter for
proposal3. In a disposable candidate copy only, copy it into Graphics tests as
PositiveBetaProductionTests.cs and register:
("Positive beta source export", PositiveBetaProductionTests.Export).
Build Debug Graphics and invoke:
dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll
--case="Positive beta source export"

It requires real deployed Earth assets. It does not call coverage search. For
proposal1 change lever factors100/10 to50/2.5, gap100 to25, speed200 to50,
inertia80000 to20000; after alpha evaluation use
alpha.Proof.Refine(1e-7,24,c.Use), require certified status, then pass its Proof
downstream. These are the exact two source designs recorded; no authority is
injected. Do not treat re-execution as permission for a new fixture campaign.

Raw build output and repeated diagnostic trees are not retained. Exact observed
status lines are in positive-beta-results.json; design declarations are in
positive-beta-plan.md. No new scratch tree was created because both source runs
stopped before export. Existing comparison/causal scratch remain disposable and
were not retried after their previously reported policy blocks.

## Next decision

Project Control must reassess the fixture/source qualification approach before
another attempt. No demonstrated post-impact coverage defect justifies changing
production on this evidence. The prior accepted Coast correction and difficult
Force refusal are preserved.

Proposed accomplishment title remains:
Certify post-impact terrain-contact coverage for the admitted authored point.

No milestone number. STOP FOR PROJECT CONTROL.
