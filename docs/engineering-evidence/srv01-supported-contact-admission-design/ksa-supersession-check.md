# Bounded supersession check

2026-09-17, before NovaCore design. **PASS**, independently reviewed by
`ksa_gate_red_team` before the lead opened NovaCore implementation source for
this design. See verification.md for the attacks and access limitation.

- Recursively enumerated the actual install for KSA/BEPU DLLs and version manifests.
  Current root DLL is build 5438; newest installed version record is 5438.
  Older manifest files are history, not alternate live implementations.
- Verified product versions and SHA-256 of KSA.dll, BepuPhysics.dll,
  BepuUtilities.dll and KSA.deps.json. Read actual assembly methods afresh.
- Followed Universe production callers into VehicleUpdateTask, current bubble
  intake, horizon stepping, ready/apply and removal. Did not mistake old cluster
  naming, UI/debug helpers, cloth BEPU worlds or tooling for spacecraft ownership.
- Direct live-history review includes newer r5333/5341/5421 ownership changes and
  all returned entries after 2026-09-12 through visible latest r5448 on Sep 17.
  r5439–5447 concerns displays/editor/save/graphics/debris actions, not a replacement
  assembly-admission owner. r5448 adds/fixes dynamic clutter and shared dispatcher
  behavior; it is newer history, not proof of installed changes. No replacement
  vehicle lifecycle was found in this bounded check.
- Current configuration/branch inputs were inspected: simulation settings,
  ground clutter, active actuators, animation, ocean/locomotion, frame/parent and
  force-off-rails override. Native-sim retention and body removal are distinct.
- Tried to falsify retained-world claims: found explicit pool reset, 30-second
  unused-sim retirement, parent/frame membership changes and merge/split. These
  qualify the lifetime claim. Continuous ordinary same-body updates retain solver
  state; arbitrary topology changes are not certified by this design review.
- ShapesVersion was tested as a suspected competing vehicle rebuild owner. The
  current getter callers concern cloth/clutter caches; no vehicle reset hook was
  established. Do not invent one.

Scope of conclusion: current **available installed** spacecraft responsibility,
reconciled with directly read live engineering history. This is not an exhaustive
proof that remote unpublished KSA source contains no change.
