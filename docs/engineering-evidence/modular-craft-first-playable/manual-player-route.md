# Project Control final manual route

The earlier short Release session has a recorded user PASS. This route identifies the final candidate and the long-craft checks still requiring Project Control's judgment. Engineering cannot self-declare Player PASS. No deployment or banking is implied.

From `E:\NovaCore`, open the qualified editor:

```powershell
tools/NovaCore.ConstructionEditor/bin/Release/net10.0-windows/NovaCore.ConstructionEditor.exe --catalog assets/vehicles/modular-starter/catalog.json --asset-root assets/vehicles/modular-starter
```

1. Place and commit the Command Core. Attach and commit Short Tank, Engine Adapter, then Main Engine at compatible stack sockets.
2. Select Radial Attitude Block, choose 8×, preview at a tank radial socket and commit the entire group. Confirm twelve parts. Undo/redo restores the group atomically.
3. Fill craft for launch, Check function, save, create a new document and open the saved craft. Inspect the restored geometry/configuration. Launch at Florida. This passes the exact saved/current document; it does not load SRV-01.
4. Confirm supported cold craft, engine OFF, neutral controls, and active-vessel follow. Orbit/zoom the camera.
5. Press Z and observe exhaust, contact unload and rising craft. Apply brief W/S, A/D and Q/E inputs, then combined inputs. Release and observe physical attitude/rates rather than camera-driven steering.
6. Press X after roughly 15–20 seconds of ascent. Confirm the main plume stops while motion and time continue. Test brief attitude input while fuel/power remain. Keep this to an ascent/coast route; landing and terrain recontact are unqualified.
7. Select a celestial focus with the number row, then F to return to the craft. Confirm retained camera orbit/zoom and that focus changes do not change the controlled vessel. Use ordinary brief human key holds; instantaneous automation taps were inconclusive in earlier native observations.
8. Close flight. Confirm the editor draft, dirty state and saved file are unchanged by the flight. Repeat using the Long Tank with the same remaining parts and route.

To skip repeated assembly for flight-only inspection, use Open craft on `build/modular-craft-first-playable/gate12-native-short-final.craft.json` or `gate12-native-long-final.craft.json`; these are files authored by the real editor qualification route. The captured flight draft was renamed after saving, so its digest differs from the on-disk saved snapshot. Launch always seals the current draft. These files supplement, not replace, the build/save/reload checks above.

Time warp and pause are intentionally unavailable in this live physical route (1×). FREE remains deferred and HOME unbound. There is no finite two-second terminal hold. A later ground-clearance refusal does not mean X removed momentum.

Exact source/build identities: `gate12-seal.json` and `campaign-closure.json`. Project Control supplies the final acceptance decision; no automatic banking follows.
