> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Input ownership

| Action | Binding | Existing ownership / conflict disposition |
| --- | --- | --- |
| Refocus active SRV | F, rising edge | No prior F binding; camera-only mask |
| Detach free camera | Home, rising edge | No prior Home binding; camera-only mask |
| Orbit / free look | Existing mouse drag | Existing drag mapping and sensitivity |
| Focused zoom | Wheel | Existing Solar target-relative multiplicative 1.25 law; 0.5 m to existing Solar maximum; no hidden mode transition |
| Free speed | Wheel | Existing FreeCameraController speed law |
| Free translation | WASD / Q / E | Existing camera movement only; no spacecraft command implementation |
| Celestial focus | 1–0 | Existing Sun, Mercury, Venus, Earth, Moon, Mars, Jupiter, Saturn, Uranus, Neptune ordering |
| Solar overview | R | Existing reset |
| Solar pause/rate | Space / existing rate keys | Existing behavior; no new time controls |

E retains existing Solar surface attachment in celestial mode. In vessel/free mode it cannot toggle that attachment; E remains upward free movement. New F/Home edges are accepted only while the render window is foreground. Simultaneous actions resolve Home before F, then existing celestial focus, then reset.

Native input grows from 84 to 88 bytes by appending the mask at offset 84. Prior field offsets and the aligned host submission offset 104 remain unchanged. Native and managed layout checks cover the new field and the 112-byte host event. Runtime deployment must pair the candidate native DLL with candidate managed assemblies.

Existing legacy sample SAS/torque bindings remain untouched. This route does not dispatch them into the stock assembly. No Z/X, throttle, propulsion, guidance or navball changes exist in this diff.
