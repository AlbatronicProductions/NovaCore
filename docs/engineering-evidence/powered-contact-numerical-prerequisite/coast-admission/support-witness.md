> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Independent geometric support witness

Read copied stored pose and velocities from `coast-source` in [rejected-input.json](rejected-input.json), not the admission decision. The fixture is the unchanged box with half-extents(1,.5,.5) m above a16×2×16 m slab whose top is y=0. No quaternion renormalization was added.

Using binary64 arithmetic on those stored quaternion components, transform all eight local corners. Independently compute the lowest point with the oriented-box support function:

`gap_min = p.Y - abs(R_yx) - .5*abs(R_yy) - .5*abs(R_yz)`.

Both calculations return `-0.00035909069820483364 m`. The four bottom-corner gaps range from `-0.00035909069820483364` to `-0.0003590856666937925 m`. All eight corners remain within the slab XZ footprint. There is geometric overlap, well within the existing .020 m ceiling.

The convex hull of the four selected contact points projected into XZ has area **2 m²**. The COM projection is strictly inside, minimum edge margin **.5 m**. The projected actual bottom face independently has approximately2 m² area and COM edge margin `.4999999984026985 m`. Contact coverage is not marginal or degenerate in this snapshot.

Each current normal has upward Y=1, with Z=-3.1946030265572745e-9. Gravity projection is inward -9.81 m/s²; zero engine wrench does not remove gravity. The four selected contact depths remain positive. No normal reversal or support-set loss occurs.

Independent point velocity is `v + omega × r`. Bottom-corner vertical speeds are **+.007092981090693951 to +.0070936284457168665 m/s**. Actual selected-contact normal speeds are +.007092981090693229 to +.007093628445716149 m/s. Both are below the existing `.029999400011999758 m/s` support-speed bound, but they are positive separating motion. This is consistent with recovery while still compressed; the snapshot does not establish its full dynamical cause, physical departure or future support continuity.

Selected contact-point Y=-2.980232238769531e-8 m is a float-generated contact-point offset from the slab plane, not body penetration. The corner/support-function calculation deliberately avoids using that quantity as penetration. Likewise, do not use the pre-coast-refresh capture's earlier prestep geometry as new contact points at the installed pose.

Independent read-only source/geometry review reproduced the corner/support-function result, hull area/COM margins, callback/solver mapping and signed speeds from the saved data. No new world process or solver execution was used.

Conclusion: **physical contact geometry persists**. The refusal instead enforces the explicitly declared exact-normal numerical domain. Full powered-to-coast dynamics remain unqualified.
