# Private post-impact spacecraft initial state

The unbanked candidate constructs a complete immutable simulation-side preparation
at a Florida provider's existing contact root alpha. Canonical simulation remains
the sole spacecraft authority. The preparation is neither a public snapshot nor
an installed spacecraft state.

`FloridaContactProvider.Proof.PreparePrivatePostImpactState` checks the original
root, kinematics witness, response/pre-impact receipts and their requests through
the existing final-velocity receipt's checked read. It copies the certified final
inertial-root linear velocity and body-frame angular velocity without further
selection, impulse application or rounding. Nonzero post-impact spin is retained.

The state captures source translation and rotation values, mass, principal inertia,
force and torque, source segment epochs, state/timeline revisions and the source
coverage. Its root retains the provider, geometry, terrain, Earth/frame and engine
identity. Its privately held issuing tuple retains response policy and numerical
provenance. The source epochs are provenance; the new private initial instant is
alpha. Source angular velocity is the admitted pre-impact zero spin, distinct from
the new nonzero initial angular velocity.

The COM position denotes the exact source expression
`X0 + V0*d + (F0/m)*d*d/2`, with `d = alpha - sourceEpoch`. Outward arithmetic encloses
that expression using the checked kinematics root enclosure and exact source-double
interpretation, including mass division and integral epoch conversion. This is not
the rounded acceleration of a different evaluator. A request specifies the maximum
full component width. Nonfinite or insufficient bounds refuse qualification; the
operation never refines the root or widens a request.

All pose components describe the **same alpha**. The box is qualification evidence,
not permission to independently choose coordinates or a nominal event time. Attitude
continues to denote the exact normalization of the frozen stored quaternion. No
new quaternion or represented COM-position bits are selected.

The privately issued receipt owns readiness. Default, mismatched and stale receipts
cannot expose a ready snapshot. `Read` checks the original root, complete retained
and supplied final realization, pose request and current source applicability before
returning the immutable values. Reads use the existing single-writer contract;
they are not locks. Same-owner refinement does not revoke older applicable evidence.
A changed issuing tuple requires requalification. Source mutation, changed identity,
pending boundaries or a public clock beyond the source start invalidate access under
the existing provider rules. There is no cache, global generation or consumed token.

No remainder, endpoint, publication or contact reacquisition is implemented. A future
private propagator must retain the correlation of `T - alpha` with this root. Current
nonnegative response residual does not prove any later interval contact-free. Support,
rest, persistent constraints and a general solver remain outside this specialization.

The ownership adopts KSA's complete captured preparation and private staging while
keeping game/simulation authority above scheduling and numerical mechanisms.
NovaCore retains its exact root and atomic canonical contracts; it does not import
float solver time conversion or sequential publication. This bounded isolated-contact
path does not establish the future general contact/constraint-solver architecture.

See [proof, validation and reproduction](engineering-evidence/private-postimpact-state/README.md).
