# SRV-01 exhaust correction

2026-09-17. Candidate branch `codex/srv01-production-integration`, banked baseline
`09463ec6c323daf233205cb08306d38611c22e6e`. UNBANKED.

## Manual acceptance correction and cause

Project Control withdrew the earlier manual PASS: the reusable spacecraft moved,
but its firing main engine had no visible exhaust. Numerical tests did not establish
visual acceptance. This correction requires new personal inspection of exhaust.

The native transport has a proven capacity defect. `CreateSubmission` in
`native/NovaCore.Native/NovaCoreNative.cpp` allocates/maps/describes the initial
active object count. READY has 37 objects. The camera uses 96 bytes and each object
80 bytes: 96 + 37 * 80 = **3,056 bytes**. The first main plume is object index 37,
whose transform starts at offset 3,056, entirely beyond that range. Two RCS plumes
use indices 37/38, requiring 160 more bytes. `Upload` copies the current count
without checking native capacity. The draw's first-instance index causes the
vertex shader to read those out-of-range objects.

The managed array has capacity 54, but that capacity was never communicated to
native storage. Managed presentation tests therefore passed without testing this
native lifetime boundary. A swapchain resize can incidentally recreate storage
using a higher current count; no count-driven growth or bounds check exists.
The exact GPU result is undefined: this report does not claim a driver-specific
zero read or that no other visual defect can exist. The defect is consistent with
the original 37 craft meshes moving while added plume transforms are invalid.

Endpoint, realized actuator flags, gimbal transform, mesh and draw submission
already exist. Independently, the original static opaque cone fails the new
smooth-exhaust quality requirement even once transport is repaired.

Cause investigation stopped at that finding. Project Control then explicitly
answered **Proceed with the proven correction**, authorizing the bounded repair.

## KSA source and official history

Actual installation: `E:\Kitten Space Agency`, build
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.
KSA.dll SHA256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`.
The companion exhaust KSA report and manifest preserve eight inspected installed
file identities and twenty directly decoded current method-body identities.
KSA writes: **0**. No KSA shader, texture, mesh or expressive asset is imported.

The authenticated official Discord `#live-changelog` was inspected directly:

- [Revision 5437, 2026-09-14](https://discord.com/channels/1260011486735241329/1260112103134724146/1549063240342839398): visual plume computation moved from physics to the renderer, restricted to visible plumes. This agrees with installed 5438's pending-plume/render ownership.
- [Revision 5422, 2026-09-09](https://discord.com/channels/1260011486735241329/1260112103134724146/1547231315370320014): nearby visual plumes merge downstream while individual nozzle plumes remain near their exits; obsolete throttle/pressure template modifiers were removed.
- [Revision 5120, 2026-08-02](https://discord.com/channels/1260011486735241329/1260112103134724146/1533561008793194528): corrected compact-engine plume placement after applied-transform import. Authored nozzle attachment is an explicit responsibility.
- [Revision 4217, 2026-04-29](https://discord.com/channels/1260011486735241329/1260112103134724146/1499040168328564818): engine-specific presentation emission addressed visibility; appearance is not a reason to change physical thrust.

History is engineering intent/evolution; current installed implementation remains
the behavioral source. Newer revision 5447 is not attributed to installed 5438.

**ADOPT** nozzle-instance ownership, realized-state input, authored exit/gimbal
attachment, reusable immutable presentation resources, and display-frame work.
**ADAPT** to copied NovaCore canonical observations and explicit sixteen-jet
identity. Current SRV propulsion is binary full-thrust/off, not a published
continuous throttle or exhaust-gas model. Do not invent those physical values.
KSA's atmosphere model, visual merging and shutdown tails are not required here;
the bounded effect must respect available state and immediate inactive-jet removal.

## Correction boundary

Prepare native capacity once and preserve it through resize; distinguish active
length from capacity and reject overflow before copying. Preserve the broad
frame-submission ABI and existing scene entry points.

Use the current-KSA method adaptation detailed in [exhaust-method.md](exhaust-method.md),
attached to the verified nozzle tuple and published gimbal. Clip it against opaque
depth and draw it once per display frame. Main and already-qualified RCS use the
same presentation mechanism. The first generic volume was superseded after
Project Control required method convergence, not only appearance convergence.
No simulation law, resource debit, actuator realization, asset or command changes.

The user-supplied appearance reference is
`C:\Users\Tyler\Videos\2026-09-17 00-50-50.mp4`.

## Final disposition

The initial manual PASS was withdrawn for missing exhaust. The capacity/lifecycle
repair received its own PASS, but that did not qualify the later mechanism change.
After the final KSA-method Release build ran, Project Control explicitly answered
**PASS** to bell attachment, core/falloff, gimbal, commanded RCS, camera independence
and shutdown/final-hold checks on 2026-09-17. See [README.md](README.md) for the
complete bounded result and [method-results.json](method-results.json) for exact
build, GPU, allocation, frame-tail and endpoint witnesses. UNBANKED.
