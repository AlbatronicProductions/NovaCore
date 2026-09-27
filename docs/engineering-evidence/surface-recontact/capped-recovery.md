# Ordinary recovery, separate from impact damage

The pinned critically damped constraint uses
`bias = min(depth / h, depth * omega / (omega*h + 2*zeta), R)`.
Here `R=1.5 m/s`, frequency is 30 Hz, and damping ratio is one. At finite slice
duration `h`, saturation starts at `R*(h + 2*zeta/omega)`. Its infimum as
`h→0` is about 15.915 mm. This transition is continuous, finite, and an ordinary
native solver branch. It does not establish touchdown strength.

The provisional candidate incorrectly used this infimum as a refusal boundary.
A long-craft terrain seam exposed the error. At step 171, child 4 / triangle
2029 produced raw depth 0.0180618316 m with normal
`(-0.9946841, 0.100060165, -0.024320517)`. Native mesh reduction retained its
depth and changed its normal to `(0.03661941, 0.9993293, 0.000114363924)`.
Parent feature 132973572 resolves to the same triangle/child/local feature.
Parent position matches triangle A after the correct child-offset addition.

The triangle is regular in parameter space; the skinny-triangle hypothesis was
rejected. Installed KSA uses a radial patch axis, so the raw normal also passes
its 0.1 filter. Changing that threshold, tilting its axis to terrain slope, or
rewriting the manifold would not be direct adoption. No such change was made.

The post-solve dense native-face witness measured about 1.736 mm geometric
penetration. It is not a collision-time oracle for the raw depth. Raw triangle
depth cannot serve as a whole-craft burial or damage metric, particularly after
ordinary mesh reduction. The retained pre-correction witness is reproducible
with the seam fixture; its compact values are recorded above.

The correction removes saturation-as-invalid, preserves finite row validation,
and keeps the standard KSA material, cone and native solver. Checkpoint burial
admission separately observes authoritative geometry per convex child. No new
restitution, velocity clamp, physical impulse scaling or static-foot reinterpretation
was introduced.

Permanent checks:

- Native scalar response versus an independent recurrence at all five admitted
  dyadic slice sizes, below/at/above saturation and deep saturation, with
  separating/zero/closing velocities. 1,433 positively saturated steps.
- Six short/long/asymmetric moving-terrain routes, including the exact failing
  long-craft route, both travel signs and diagonal/corner crossings.
- Captured accepted contact neighborhoods stay inside prepared terrain across
  native mesh replacements.
- Three equivalent inclined-plane tessellations, with actual compound geometry,
  independent plane penetration and sampled mechanical energy measurements.
- 27 pad landing/rocking/rolling/tipping cases with published-endpoint energy,
  exact grounded reload, live resource consumption and physical relaunch.
- Per-child burial refusals, NaN/infinite native rows and unchanged source state.

Neither this qualification nor the unchanged solver predicts structural survival.
Damage/destruction remains outside this responsibility.
