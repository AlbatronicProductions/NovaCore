# Positive-beta integration: predeclared fixture 1

Before production coverage search: one proposal, maximum three permitted. No
production changes. Retain Coast and Force fixtures and all work limits.

Use real acquired Florida grading-center terrain and current SolAnalytical Earth
at canonical t=0; target t=1s. One zero-radius authored landing point. Body attitude
identity, pre-impact omega/torque zero. Mass8kg, spherical principal inertia
(20000,20000,20000) kg m². Authored body lever is the t=0 root-frame direction
50*FloridaEast - 2.5*FloridaUp. Initial feature gap25m, inward surface-relative
speed50m/s, constant external force zero. Position/velocity are constructed using
the same real Earth transform and surface velocity as existing Florida fixtures.

Use existing alpha Refine(1e-7,24) before downstream qualification; this selects
no nominal alpha and increases no budget. M14.10–M14.15 requests remain those of
the existing coverage integration. No hand-selected post-impact velocity bits.

Independent local design guide: spherical inertia gives omega approximately
0.5rad/s after the inelastic impulse. With x=50,h=2.5, the leading gap is
x*(sin(omega*s)-omega*s)+h*(1-cos(omega*s)); initial curvature is positive and
later rotation makes the gap negative. Approximate beta-alpha is0.3s, well inside
the roughly0.5s remainder. Expected tangential travel is small relative to the
Florida grading footprint. Exact moving-Earth/represented-state oracle must
confirm signs, first approach and grading; the local approximation supplies no
production authority.

First execute only the real admission/alpha/response/private-propagation chain
and export its checked physical inputs. Run the independent high-precision
same-model oracle before any candidate coverage search. If physically suitable,
select this first proposal; then attempt production coverage once. If physically
robust but the certificate refuses, STOP without production rescue or another
fixture search. If prior chain cannot admit the proposal, report that boundary
before considering any remaining design allowance.

If successful: three fresh Debug and three fresh Release integration processes,
no retries; focused coverage and full Simulation D/R, Coast/Force regression,
bounded beta allocation/timing, independent red team. No unrelated campaigns.
Retained new evidence budget100KiB, excluding useful concise oracle inputs.

## Bounded design disposition before any coverage search

Proposal1: M14.9 admitted alpha [0,1], but the separately requested 1e-7s
refinement refused NumericalResolution. No coverage search ran. This was a
source-design gate, not a coverage defect or a reason to change production.

Proposal2 considered statically only: x1000,h100,mass8,I8000000,speed1000,gap200,
force0. Rejected without execution because kilometre-scale lever interval
rounding threatens the unchanged fixed 1e-12m lever-width request; its footprint
also spends more of the grading domain. No oracle/search run for this proposal.

Proposal3, LAST allowed set, before execution: x100,h10,mass8,spherical inertia
(80000,80000,80000),speed200m/s inward,gap100m,force0,identity attitude and zero
pre-impact omega/torque. Same real Earth/terrain and 1s target. No additional
standalone alpha refinement; the unchanged M14.10–M14.15 requests own their
existing bounded refinements. Local estimate omega~1rad/s, alpha~.5s,
beta-alpha~.3s, maximum separation~.066m, tangent excursion a few metres.
This is a new physical scale with a larger useful gap, not a changed proof
threshold. Export its checked chain and run the same-model oracle first.
No fourth set and no rescue after a physically suitable fixture is selected.
