# Manual retest and engineering reproduction

## Project Control route

Double-click `E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe`. Keep its adjacent output directory intact. This is a framework-dependent Windows x64 build using the existing .NET 10 core, Windows Desktop and ASP.NET Core runtimes plus repository datasets, not a published standalone distribution. The ASP.NET dependency belongs to the retained diagnostic frontend; the normal route opens no browser/server.

1. Choose display settings, **Start NovaCore**, then **New vehicle** from the game/menu.
2. Browse categories/cards. Choose **Command core**, move its ghost in the viewport, then click to place. Only root-eligible content appears until a root exists.
3. Choose **Short tank** (repeat later with **Long tank**). Hover the root's visible compatible aft connector, inspect the snapped ghost, then click. Right drag orbits to expose connectors; middle drag pans; wheel zooms; **Focus craft** recentres.
4. Attach **Engine adapter and feet** to the tank's aft connector, then **Main engine** to the adapter's engine connector. Authored frames own the connections.
5. Choose **Attitude block**, select **8** symmetry (or X/Shift-X), and hover the tank's radial group. Inspect all eight copies from suitable viewpoints; one click places the group. Undo/redo is atomic. Bin/Delete cancels held work; Escape opens the menu.
6. Right-click a part for contextual configuration. **Fill consumables** explicitly prepares stores/charge; launch never silently refills. Name the craft, **Save / Load → Save new**, create a new draft and reload from the in-game list.
7. **Launch vehicle** switches to Florida in the same window. Z ignites, X cuts off, WASD/QE request physical attitude control, mouse operates the existing camera and F refocuses. Flight status remains visible in fullscreen.
8. **Menu → New vehicle / retained design** restores the design. **Return to flight** resumes the retained physical owner. Editor navigation is not physical recovery or rewind. Physical flight stays **1×**; warp is outside this qualification.
9. Repeat the relevant route with the other tank. Judge interaction/discoverability separately from engineering assertions.

`C:\Users\Tyler\Documents\NovaCore\Craft\UX-Review-20260922.craft.json` is the agent-owned visual-review copy, not a stock product template or substitute for construction acceptance. User saves use that Craft directory. The closing visual-review session was exited; no automated flight window remains active.

## Build and test

Run from `E:\NovaCore`. Dependencies: .NET 10, Windows SDK/MSVC x64, CMake/Ninja and Vulkan SDK (1.4.357.0 used here). Existing authoritative Earth/Florida data is required: `earth-florida-m12` remains physical-authority data, not an optional global-only fallback. Use the accepted dataset manifests and setup documentation; do not regenerate/remove datasets as part of retesting. KSA is not required to compile and must remain unchanged.

In an x64 Visual Studio developer environment, with Ninja/Vulkan available:

```powershell
cmake -S native/NovaCore.Native -B build/modular-player-revision/native-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/modular-player-revision/native-debug
cmake -S native/NovaCore.Native -B build/modular-player-revision/native-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/modular-player-revision/native-release
python -X utf8 docs/engineering-evidence/modular-player-revision/verify.py
powershell -NoProfile -File docs/engineering-evidence/modular-player-revision/qualify.ps1
python -X utf8 docs/engineering-evidence/modular-player-revision/seal.py
```

The retained native CMake caches identify exact compiler/SDK paths. `verify.py` builds the whole solution with matching NativeBuildDirectory, then replays every unique preserved DLL command from `modular-craft-first-playable/gate12-results.json` plus current ABI/asset/editor regressions. `verify.py --affected` reproduces the final 10-command subset after hosted-feedback/F-admission changes. Prefer full verification for a freshly rebuilt candidate.

`qualify.ps1` runs actual short/long NovaCore.exe routes sequentially in both configurations, samples process memory and saves `app-*-final` files. It opens interactive windows; do not supply user input during automation. It uses `build/modular-player-revision/craft-library` for saves and does not persist launcher settings. Each route constructs its own document from cards/native placement input. Unique document IDs make cross-run fingerprints differ; exact identity comparisons occur within each route.

`seal.py` verifies the fixed revision boundary, protected entry files, refs/index/worktrees, KSA preservation and candidate/native/content identity, then regenerates derived JSON reports. It refuses unexpected changes and never restores/deletes files. A seal is not a test run. The app-host EXE alone is insufficient identity: managed DLLs, native DLL, shaders and content are sealed together.

This candidate came from local builds. Dependent Triangle/Launcher/Editor outputs were refreshed by MSBuild; no prepare-launcher/deployment script or manual deployment copy was performed for this correction. Those diagnostic executables are not the normal player route. The earlier camera campaign's manual deployment does not establish this application's provenance.

## Retention

Keep `entry.json`, recovery ZIP/index and protected prior work. `inventory.json` identifies builds/logs/test craft, KSA user recovery/reference save and the visual-review craft. Historical one-off refactoring scripts under build are non-idempotent: **do not rerun them**. Exploratory failures are retained and are not closing PASS evidence. No deletion is authorized.

**ENGINEERING PASS + INTEGRATION PASS + UNBANKED. STOP for Project Control manual Player retest.** No milestone, commit, tag, push, banking, deployment promotion or next production front.
