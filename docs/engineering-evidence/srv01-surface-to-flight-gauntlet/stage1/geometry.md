# Explicit physical authorship and independent geometry gate

Profile: `srv01-fourhorn-upright-box-envelopes/1`.
Digest: `cd21bff65805e758df0c3bf5b187f93e82cc4feb2396f4e7d5a75d684a344c74`.
Bound design: `ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf`.

This is new physical authorship, not collision data extracted from a GLB or
inferred from mass/inertia. Existing part definitions, instances, masses, stores,
assets and authored visual geometry are unchanged. Each collider records its
original part key, definition digest, part-local pose and compiled assembly pose.

| Part | Box dimensions X,Y,Z (m) | Part-local centre (m) |
|---|---|---|
| Capsule | 1.354,1.440,1.456 | .677,0,-.008 |
| Tank | 2,1.4,1.4 | 0,0,0 |
| Main aft body/bell | .639,.52,.52 | -.3805,0,0 |
| Main flange | .061,.56,.56 | -.0305,0,0 |
| Each of four RCS blocks | .368,.185,.368 | 0,-.2075,0 |

All local rotations are identity. Original part placement/rotation comes from
the pinned SRV01-FourHorn design. Compound children subtract the canonical wet
COM exactly once. No compound builder calculates mass or recentres the assembly.
Native inverse inertia comes from the original complete canonical tensor.

Authoring provenance: independently read the original NovaCore visual recipe
`E:\NovaCore-Blender-Visual-StepB\tools\blender\srv_parts\specs.py`, SHA-256
`A3675CFC91AB96EF3BED34F84F6C369720C7D5C0E703A1D62EF9F8FEDB51F1D1`.
That source explicitly describes **appearance only**. Lines 37–88 provide
dimension/transform references informing these newly declared physical envelopes.
No Blender tool was launched and no visual artifact changed. The main split uses
the existing flange boundary, avoiding extension of its .28 m flange radius down
to the .26 m bell mouth. It was chosen before solving, not after penetration data.

These boxes conservatively fill curved corners and hollow nozzle cavities. They
are not exact triangle geometry. Their suitability outside one declared upright,
OFF placement is unqualified. The visual bell mouth is a planar annulus, while
this physical approximation has a square support footprint; manual acceptance
and full physical qualification must not treat them as identical surfaces.

Upright rotates assembly +X onto local +Y; O initially remains zero. The slab top
is at root Y=-1.7, half-extent8 m, thickness2 m. Wet mass705 kg and COM.X=734.4/705
are independent analytical expectations. Scalar corner enumeration proves no
initial intersection, only four aft-box corners touch and the projected COM is
strictly inside their footprint. All seven parts remain represented by eight
convex boxes. Native material/selector/8-iteration/1-substep policy is unchanged.

The profile's 0.061 m flange is the smallest feature. Existing precision policy
therefore gives tolerance61 micrometres. Full-run drift **fails** that policy.
The authoring is not qualified merely because the first geometry gate passed.
