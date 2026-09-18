> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Cheap KSA currentness and final convergence

Installed `E:/Kitten Space Agency/KSA.dll` remains version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`, identical to the accepted review. No KSA files were written.

Authenticated live history was checked before design edits, limited to changes after the accepted review. Current entries through 5460 do not materially supersede the accepted camera boundary:

- 5457: vehicle-part render-data cost, tests and profiler. [Entry one](https://discord.com/channels/1260011486735241329/1260112103134724146/1550450712499200031), [entry two](https://discord.com/channels/1260011486735241329/1260112103134724146/1550450713665081354).
- 5458: distant-sphere heightmap displacement. [Entry](https://discord.com/channels/1260011486735241329/1260112103134724146/1550468197554323540).
- 5459: volumetric compositing. [Entry](https://discord.com/channels/1260011486735241329/1260112103134724146/1550487805745045608).
- 5460: ground-clutter velocity serialization. [Entry](https://discord.com/channels/1260011486735241329/1260112103134724146/1550497428774326416).

Broader archaeology was not repeated. See the accepted [camera review](../post-m15.3-first-playable-launch-review/ksa-camera.md) and [live-history review](../post-m15.3-first-playable-launch-review/ksa-live-history.md).

| Equivalent responsibility | Classification | Candidate disposition |
| --- | --- | --- |
| Shared celestial/vehicle followable identity | ADOPT | Existing FocusTarget union, canonical spacecraft key |
| Viewed target distinct from controlled vehicle | ADOPT | Camera selection cannot select or mutate session ownership |
| Orbit/follow controller | ADAPT | Existing Solar controller consumes copied material-origin endpoint |
| Free camera | ADAPT | Existing free controller under explicit Solar detachment |
| Refocus | ADOPT | Explicit F restores bounded vessel view memory |
| Target retirement | ADAPT | Explicit generation/status and deterministic environmental-body fallback |
| Precision-relative transport | ADOPT | FP64 world calculation, existing relative encoded transport |
| Visual reference point / smoothing | INTENTIONALLY DIFFER | Material O matches NovaCore's current renderer; no KSA COM smoothing transplanted |
| General viewport/target registry framework | INTENTIONALLY DIFFER | One bounded current-vessel binding; a broader framework is outside this responsibility |

Final source recheck finds the same responsibility boundary. No unexplained divergence or current-history camera blocker was found.
