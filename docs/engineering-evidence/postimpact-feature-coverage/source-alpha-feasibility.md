# M14.9 source-alpha feasibility: verified refusal operands

Read-only source review plus the lead's two authorized unchanged-input diagnostic
processes. No fourth fixture, additional execution, production edit, permanent
test edit or proof-limit change was performed by this reviewer. Neither diagnostic
called post-impact coverage search.

## Proposal 1: midpoint sign resolution, not admission

Admission produced a provider (`Unresolved/None`, the normal admission convention).
Evaluation certified the unique approaching alpha on `[0,1]`. Standalone
`Refine(1e-7,24)` reproduced `Unresolved/NumericalResolution`.

| Decisive operand | Observed value |
|---|---|
| Requested full width | `1e-7 s` |
| Successful bisections | `23` |
| Retained bracket | `[0.5000662803649902, 0.5000663995742798] s` |
| Outward full bracket width | `1.1920928955078128e-7 s` |
| Failed attempt | `24` |
| Midpoint | `0.500066339969635 s` |
| Point domain / all grading guards | PASS |
| Point gap | `[-2.346932888031006e-7, 7.553026080131532e-7] m` |
| Gap full width | `9.899958968162539e-7 m` |
| Point derivative | `[-49.98672410715207, -49.98672410701859] m/s` |

`FloridaContactProvider.cs:196-203` narrows only when the midpoint gap is strictly
one-sided. The point gap above contains zero, so the next half cannot be selected.
The derivative remains strongly negative. This is not `RefinementBudget`, loss of
existence/uniqueness, or a provider-time representation failure.

At that point, pre-BodyFixed relative-position component widths are
`[1.30385160446167e-8, 3.259629011154175e-9, 6.51925802230835e-9] m`.
After BodyFixed, q component widths are
`[8.60891304910183e-7, 3.632158041000366e-8, 9.73232090473175e-7] m`.
The Earth Taylor position remainder is only
`[1.9075634485071158e-17,1.907563448507141e-17] m`.

Thus the decisive gap-width growth occurs in the Earth orientation/BodyFixed
outward enclosure and projection, not the Earth translational Taylor remainder.
The diagnostic separates these stages; it does not separate every internal
trigonometric term from every rounded multiply/add. The local full gap width
divided by the minimum approach magnitude is approximately `1.98052e-8 s`.
That is a resolution scale, not a tolerance or a universal minimum root width:
midpoint sign bisection can stop when a particular midpoint lies too near alpha
even though the retained bracket is wider than that scale.

The extra `1e-7 s` standalone request is stricter than the retained M14.10
`1e-5 s` root-width request. This failure alone does not prove M14.10-M14.15
cannot qualify the source. Their own width and authority gates remain separate.

## Proposal 3: whole-box North grading refusal

Admission produced a provider with failure `None`. Evaluation reproduced
`Unresolved/GradingDomain`. The whole `[0,1]` motion enclosure is finite; positive
projection, East footprint, cap and final clamp all pass. The North footprint
predicate alone fails. No refinement was entered.

| Decisive operand | Observed value |
|---|---|
| Whole North coordinate interval | `[-84.09504301100971,84.09210919868202] m` |
| `R*abs(North dot q)`, upper | `535770259.0595214` |
| `56*(Up dot q)`, lower | `356771739.4514078` |
| Required comparison | left upper <= right lower |
| Actual ratio | `1.5017172040682139` |
| Whole derivative | `[-200.09101745939216,-199.88243412233834] m/s` |
| Start gap | `[100.00000017415731,100.00000113900752] m` |
| End gap | `[-99.9867255780846,-99.98672457505016] m` |

Both point endpoint domains pass. Endpoint North coordinate intervals are about
`[1.10362e-6,1.65123e-6] m` and `[-.005872102,-.005871533] m`.
The whole-interval box refusal is not evidence that the physical feature left
the grading rectangle. Source `FloridaContactMotion.cs:71-73` first forms
independent Cartesian component intervals, and `:135-136` projects them onto
East and North. Predominantly normal motion loses the cancellation that a
correlated normal displacement would have under that second projection.

For the current source basis:

- `sum(abs(North_i*Up_i)) = 0.8407254045026149`;
- `sum(abs(East_i*Up_i)) = 0.28279932929401974`.

For a central approximately constant-normal speed v over source duration d, the
leading artificial North halfwidth is `0.8407254045*v*d/2`. This estimates
`21.0181 m` for Proposal 1 and `84.0725 m` for Proposal 3, consistent with the
measured whole-box results. The simplified North screening inequality is
`v*d < 133.218289 m` before adding actual drift, second-order and arithmetic
enclosures or a robustness margin. This is neither an exact universal admission
threshold nor a physical speed/lever limit. Full production inequalities decide.

## Alpha feasibility map

For a robust source, all following requirements must have nontrivial margin:

1. Valid singleton Florida authority; fixed initial attitude; zero initial spin
   and torque; current analytical Earth; finite positive properties; no pending
   transaction; source within one integral one-second cell and seed-relative
   +/-3,600 seconds (`FloridaContactProvider.cs:65-102`).
2. Whole grading projection, East/North footprint, cone and clamp inequalities
   (`FloridaContactMotion.cs:127-143`).
3. `startGap.Lower > 0`, `endGap.Upper < 0`, and
   `wholeDerivative.Upper < 0` (`FloridaContactProvider.cs:120-132`).
4. Every midpoint needed by bounded refinement has a signed point gap and valid
   domain, before the requested full width is reached. At most 24 sign decisions;
   no fallback or selected approximate alpha (`:179-204`).
5. M14.10's fixed lever width and root/normal/normal-speed widths all fit the
   current request. The fixed lever cannot improve under time refinement
   (`FloridaContactKinematics.cs:68-94`).
6. M14.11-M14.15 each independently satisfy their response, represented velocity,
   pose and paired-propagation requests. M14.11 does not automatically refine
   a root whose response widths are insufficient
   (`CertifiedContactResponse.cs:36-71`).

The fixture's COM is constructed as intended feature position minus lever, and
pre-impact motion adds the fixed rotated lever back. Thus, with identity attitude
and zero spin, changing lever while adjusting COM consistently does not change
the ideal pre-impact feature trajectory. Actual source rounding and fixed-lever
interval width remain relevant. Lever/inertia strongly affect the response, but
Proposal 3 changed speed and gap as well; its demonstrated upstream refusal must
not be assigned to lever length alone.

There is no source-imposed general numeric cap on lever length or normal speed
separate from these admission/proof predicates. These two refusals do not prove
that every physically suitable positive-beta region is excluded. They also do
not establish a robust full-chain fixture. The finite design study must still
qualify the same-model response and coverage enclosures before classification A.

## Provenance / reproduction

Decisive values were independently re-read from the lead's completed JSON records:

- Proposal 1 SHA-256:
  `7D9B35146D2C06DAFA085F33B173AF8791C7B75CDC8C08657790CEF676F6D64D`.
- Proposal 3 SHA-256:
  `64AE9461978E2C49CEB6E2783826BB95D49588AFBE196CCF81A668217B49122B`.

The temporary original paths are `.codex/source-feasibility/proposal1.json` and
`proposal3.json`. The lead retains the bounded diagnostic source/results and
restores its temporary test registration. No extra run was required for this
review. Verify the equations at the cited current source locations; calculate
the two basis sums directly from `FloridaFacilitySupport.Region` in
`src/NovaCore.Core/Surface/FacilitySupportRegion.cs:68-72`.
