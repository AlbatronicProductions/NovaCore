# Current KSA application observation

Status: completed bounded reference observation, not NovaCore qualification. Read alongside `ksa-history-refresh.md` for the authenticated screenshot refresh and explicit visibility limits.

Observed 2026-09-21 local / 2026-09-22 UTC using the current installed `E:\Kitten Space Agency\KSA.exe`, version **2026.9.10.5438**. KSA.dll SHA-256: `a03e98153c6f353af6f280cd3bdc1c71c718008e2049f508dc6fbe54457a0aa8`. Window identity 5572800 and process 61044 remained the same through startup, game, construction, save/load, launch, flight and return. Read-only process inspection confirmed the installed executable path during flight.

## Live application observation

| State / responsibility | Entry and visible structure | Exit / persistence observed |
|---|---|---|
| Startup configuration | KSA.exe presents system, game type, starting situation and graphics/quality configuration inside its window; Start KSA begins loading. | The same window becomes the game. No separately managed configuration process. Existing choices were left unchanged. |
| Game and menu | Earth overview, then Escape opens a compact central pause menu over the scene. Resume, New Vehicle, Launch Vehicle, Save/Load, Settings and Quit have distinct roles. | Resume returns to the scene; New Vehicle enters construction in the same window. |
| Construction | Large central viewport; left category column and thumbnail grid; transform modes above the catalog; symmetry/inheritance/snap/bin below it. Camera controls occupy the upper right; assembly/resource feedback the right side; name, launch body, location and launch action the lower right. | Escape opens an editor-specific pause menu with Save/Load and Exit Editor. Construction is a state of the game, not a second executable. |
| Categories and cards | Capsules showed a root thumbnail; Fuel Tanks changed the adjacent grid to tank thumbnails and a size filter. A thumbnail tooltip showed basic properties. | Category changes preserve the craft. Selecting a first root made it visible in the central viewport. |
| Held part and connectors | Selecting a tank highlighted the card and exposed green connector rings on the craft. Moving into the viewport showed the carried model; moving near the craft changed its preview pose. | A placement click left the part on the craft without a second commit button. A later unplaced part remained held while menus were used; the Bin control removed that unsaved held part. Escape itself opened the pause menu. |
| Symmetry | The lower-left count chip cycled from None to 2 X. A subsequent held-part preview responded around the craft. Inherit and Snap were adjacent controls. | Count is an interaction setting rather than a form that creates a different vehicle authority. This observation does not prove NovaCore's required 8-member atomic behavior; its existing generic transaction contract must continue to prove that. |
| Selection and configuration | Right-clicking the placed tank highlighted it and opened a nearby contextual panel. The panel grouped structural/tree actions, transforms, connections, subparts and propellant configuration. | Closing the inspector retained the craft. There was no requirement to type instance/socket identifiers for placement. |
| Save/load | Escape → Save/Load opened Vehicle Saves over the editor. New requested the tree where more than one tree existed, then a name. Saved entries exposed Load, Delete and Overwrite through their context menu. | Created only `NovaCore_UX_Convergence_20260921`; the new row appeared. Loading that new save reported completion and part count 3. Closing the save panel returned to the editor. No unrelated save was overwritten/deleted. |
| Launch configuration and transition | Name, Earth and location were already available in the editor's lower-right Launch panel. Launch Vehicle prompted about an uncrewed craft. | Confirming switched the same window to the surface flight scene and its flight UI. The observed craft appeared on the launch surface; this was a workflow probe, not a propulsion or balance test. |
| Flight UI / return | Flight displayed resources, time/speed, attitude, navigation and control panels around the scene. Escape paused it. New Vehicle returned to an empty construction context while keeping the same process/window. | Exit Editor returned to the pre-existing flight view. Quit showed an in-product confirmation and closed the reference session. No recovery gameplay was needed for navigation. |

Initial-session observation limits: the route above was exercised directly and establishes hierarchy and interaction roles. That first session alone did **not** establish invalid-connector refusal, exact symmetry membership, or snapped versus arbitrary-surface-placement pose. The focused second session and source correlation below supply the bounded follow-up; no broader live invalid-connector coverage is claimed. KSA's wider surface attachment, transforms, crew, staging and navigation features remain outside this NovaCore slice.

## Source correlation (read only)

Current installed binary identity matches the reference source basis. Selected methods in the locally retained decompilation were inspected for responsibility boundaries; no proprietary implementation is reproduced here or authorized for NovaCore reuse.

- `Program` creates the game window, owns the main viewport and switches editor state within the application loop. Its editor path delegates to one `VehicleEditor`; it does not start another player executable for construction.
- `EscMenu` presents game/editor-specific navigation and calls the same editor lifecycle.
- `VehicleEditor` owns catalog interaction, held selection, connector preview, symmetry interaction, contextual panels, save/exit decisions and launch preparation.
- `VehicleEditor.OnCursorPos` / `OnMouseButton` and grab-preview methods connect hover, selection, held poses and release to the same editor part tree. The observed catalog/preview behavior is not a second saved vehicle model.
- `VehicleEditor.RequestExit` handles unsaved targets; disposal/launch publishes a vehicle through the game owner. Resource behavior on editor exit requires the authenticated supersession check, not assumptions from visual similarity.
- `VehicleSaves` owns the save browser. `Program` coordinates menu pause and restores scene operation when menus close.
- `Constants` locates user data under Documents/My Games/Kitten Space Agency. Runtime logs and saves are outside the installation. Installation hashes and pre-session user-data hashes were captured before launch.

## Preservation and history correlation

Entry inventories and the user-data recovery archive are in `build/modular-player-revision/`. The one new reference save is intentionally retained for reproducibility. Normal runtime log/settings/cache differences must be inventoried separately; do not silently delete or restore files.

Post-exit verification at `2026-09-22T03:39:20.0473614Z` is recorded in `build/modular-player-revision/ksa-preservation.json`: KSA process closed; all 1,592 installation files unchanged with no additions/removals; all 88 pre-existing non-log user-data files unchanged with no removals. The only added non-log user files are `meta.toml` and `vehicle.xml` for the newly named reference save. Runtime logs/crash/profiler output were outside that user-content inventory. No cleanup or restoration was performed.

The browser automation connection remained attached to a signed-out session, despite Project Control being signed in. Project Control supplied an authenticated channel screenshot through revision 5472. `ksa-history-refresh.md` records exactly which visible entries were read and what was not visible. It is user-supplied live history, not a claim of agent-authenticated extraction. Earlier authenticated relevant editor/save/resource history remains correlated separately.

## Focused second live session

A second direct installed-game session confirmed a tank snapped beneath the root and all eight held RCS members simultaneously visible in an axial view. Delete cancelled held work without removing the accepted stack. The first root is placed immediately by the KSA card; NovaCore follows Project Control's explicit carried-root requirement instead. No special live red-invalid-connector presentation is claimed: occupied/incompatible refusal was established from current source. Final installation and pre-existing user-content preservation are reported in `preservation.json`; only the named reference save was intentionally added outside the installation.
