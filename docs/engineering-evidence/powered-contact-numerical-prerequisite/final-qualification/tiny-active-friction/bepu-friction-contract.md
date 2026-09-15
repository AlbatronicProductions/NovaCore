> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Pinned friction contract

BEPU2.5.0-beta.29, upstream commit
`f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`. Dependency architecture/bytes are unchanged.

The four-contact convex one-body constraint stores four accumulated scalar normal impulses,
one shared two-component tangent impulse, and one shared scalar twist impulse. Material
friction comes from the narrow-phase callback through the manifold description. Normal
rows0..3 solve before tangent and twist. Warm start applies tangent, normals, then twist.
The friction center averages nonnegative-depth contacts, or all contacts when none qualify.
All four captured depths here are positive, so the center is their arithmetic mean.

For this four-row block, L=(mu/4)*sum(lambdaN), and B=(mu/4)*sum(lambdaNi*radius_i),
where radius_i is distance from contact i to the friction center. These use the current
accumulated normal impulses, not increments, per-contact independent caps, or source cache.
The captured mu=.5 gives .125. [Four-contact ordering and cap formulas](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs#L787-L822).

Tangent uses x=tau-Ktt^-1*Jt*v, then
tau_new=x*min(1,L/max(epsilon,|x|)), epsilon=double(1e-16f)
=1.0000000168623835263871646450439811815158464014530181884765625e-16.
It applies the difference from the previous accumulated vector. The disk is circular in
impulse-component space, not box-like. There is no tangent spring softness. This radial
map is not generally the Ktt-metric maximum-dissipation projection. If the guard is active,
it can yield interior impulse with nonzero slip. [Pinned tangent code](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TangentFrictionOneBody.cs#L46-L63).

Twist uses w_new=clamp(w-Jw*v/Kww,-B,+B), without the tangent denominator guard.
It is angular impulse only. [Pinned twist code](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TwistFrictionOneBody.cs#L28-L39).

The tangent basis comes from the current shared normal and the pinned FP32 helper, including
its normal.Z sign branch. Preserve captured axes instead of rebuilding an idealized frame.
[Basis implementation](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Helpers.cs#L21-L35).

This reference evaluates that constitutive/block law at the prerequisite's actual prepared
FP64 Jacobian/radius boundary. It is not a claim of bitwise identity to stock FP32 BEPU
iterations. The prepared radii are double1.118033988749895; the complete stored normal tilt
and moment arms are retained. Here Ktt=.2497930192171509134*I, so guard-inactive conditional
tangent KKT is equivalent to the radial fixed point. That fixture-specific equivalence is
not generalized to anisotropic tangent mass or a global normal-dependent cone optimization.

Source-content SHA256 independently retrieved during this ticket:

| File | SHA256 |
|---|---|
| ContactConvexTypes.cs | B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5 |
| TangentFrictionOneBody.cs | 3E102918561194C5871AF95F35F03A3FEE5FB39884CE4E60AC50F7E1CE195573 |
| TwistFrictionOneBody.cs | 554C03FBD6FAA9DB6D738FE613AF64BCB44EA88F2F1259C7E8536E5C51962440 |
| Helpers.cs | BF736A8D6E4628880DA1B0FC291B72E98E7269F95EB3C468CC84A7365DE75E85 |
| PenetrationLimitOneBody.cs | D18AAB6E0CE7EE26EA76E47EB6125B133AE6D2953EFB1BEE44E58BE5531E3341 |
