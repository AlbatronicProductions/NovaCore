# Solver work and physical identity

The corrected off-COM native witness matches every original JSON section after
excluding only the BEPU assembly directory. See
[comparison](allocation-recheck/native-equivalence.json). The exact ledger,
all four stage snapshots, body/constraint identities, contact rows, impulses,
material values, stages, physical errors and existing bars match. The historical
work-proxy field remains labelled unqualified; this ticket does not turn that
proxy into an independent chronological-work observer. The separately accepted
friction-work evidence is preserved and not rerun.

Each timing fixture receives the original 120 ordinary preparation steps and
one target Timestep at float dt bits 1015580809 (the original 1/60 projection).
No event-sized step, extra constrained solve, duration scaling, load correction,
D, manual warm-start/cache transport or custom friction dispatch was added.

Each process observes 2,304 target substep callbacks across 128 warm +1,024
measured A/B pairs, matching 2,304 explicit target Timestep calls. Every target
has exactly one observed substep. Configured VelocityIterationCount is checked
as eight for every source/endpoint. The backend's internal per-iteration loop
is not separately instrumented; its observed loop count is UNAVAILABLE, not
fabricated from the configuration. The original fixed 8/1 policy is unchanged.
Two additional equivalent allocation fixtures per process also check one substep
and the same source/endpoint after A/B measurement; C contains zero solver calls.

All 2,304 source and 2,304 endpoint checks per process pass against the accepted
off-COM witness, including all 20 body/impulse bit values, contact feature,
offset/normal/depth and material fields. The 6,912 warm/measured target fixtures
across three processes are independent cold preparations followed by one retained
interval each, not a continuous 6,912-step spacecraft trajectory.

Native BEPU updates its own warm starts and manifolds normally. The zero count
in reports is **manualCacheWrites**, not a claim of no native cache mutation.
