# Qualified owners and application boundary

The revision retains the unbanked construction candidate at entry HEAD `8c189b28ce2a68f97de734d1589acb500c41fd99`. Its compiler, simulation, resource and camera algorithms were not replaced. Compare `source-identity.json` and `preservation.json` for the exact revision delta; comparing only to HEAD also includes earlier protected work.

| Responsibility | Current owner and treatment |
|---|---|
| Player entry | `tools/NovaCore.App` produces `NovaCore.exe`. Its facade invokes the one construction/frontend entry. Both projects are included in the solution. |
| Catalog, accepted document, previews and edit history | The existing `ConstructionEditorSession`, Part Standard and sealed `AssemblyDefinitionCatalog`. No second in-game document implementation. |
| Placement and symmetry | Existing authored interfaces, `PreparePlacement`/`PrepareReconnect`, `PreviewEdit`, `AcceptPreview`. Frontend hover prepares the complete candidate; the placement click accepts that same validated candidate. |
| Save/load and dirty state | Existing `CraftDocumentStore` and session transactions. The new in-product browser presents these operations; exact saved digest determines file association through undo/redo. |
| Compile and admission | Existing `CraftCompiler` and `CraftLaunchAdmission`. Launch captures bytes and verifies source/document/compiled/catalog/asset identities before replacing the active flight reference. |
| Runtime | Existing `ConstructionApplicationSession`, canonical simulation transactions, finite resources and physical binding. The editor retains authored initial state separately from live consumed state. |
| Game, controls and camera | `ApplicationRenderer` hosts the former Triangle entry/callback without copying it. One Solar scene, the existing `PlayerFlightControlInput`, M15.4 camera and existing physical-epoch publication remain authoritative. |
| Renderer | One native renderer lease and one persistent child viewport. The application ABI is explicitly 64 bytes, version 1, with editor/scene/menu modes. The qualified 56-byte editor ABI remains separate. Inputs are admitted only to the active context. |
| Meshes | One shared `ReusablePartVisuals` upload lease. Flight verifies the borrowed identities and never disposes the application's lease. Six existing definitions/assets remain unchanged. CPU thumbnails are cold derived presentation, not physical geometry authority. |
| UI navigation | `DesktopEditorForm` now presents configuration, game, construction, launch and flight in one parent window/process. Menus suspend input and physical servicing; entering construction retains the live flight owner. Returning resumes it without wall-clock catch-up or physical rewind. |
| Time | Flight exposes its canonical physical epoch to the same Solar owner. Editor/menu intervals do not advance Earth separately. Physical flight warp remains excluded. |
| Disposal | Flight owns its physical session; the application owns the shared visual lease and editor. Native owns its child window, never the borrowed parent. Disposal is idempotent and renderer ownership remains singular. |

The old form-driven placement/OS-file-dialog handlers and their obsolete UI driver were replaced, with their meaningful camera, focus, capture, persistence and dirty-state checks migrated into the unified application driver. The historical entry recovery archive retains the pre-revision implementation. The standalone diagnostic/browser frontend is explicitly diagnostic; it uses the same construction owners. No normal product action starts an editor or flight process.

## Launcher disposition

| Existing responsibility | Disposition |
|---|---|
| `LaunchConfiguration`, `ScenarioCatalog` | REUSE display validation and supported choices. Broader engineering scenario selectors remain diagnostic. |
| `LauncherSettingsStore` | REUSE validated settings and atomic writes; preserve the existing diagnostic scenario/altitude preference when applying product display choices. No simulation state here. |
| `MainForm` player configuration role | MIGRATE into the application startup/settings overlay. Old wrapper retained for diagnostics. |
| `RepositoryLocator` | REUSE asset/repository resolution for ordinary double-click startup. |
| `AssetStatusService` / AssetTool subprocess wrapper | DIAGNOSTIC ONLY. Normal startup uses the existing production scene/asset validation and fails on unavailable authoritative data. |
| `LaunchCommandBuilder` | DIAGNOSTIC ONLY for child-process serialization; existing tests retained. |
| `NovaCoreProcessLauncher` | RETIRED FROM NORMAL ROUTE; remains diagnostic. |
| Triangle / standalone construction executables | DIAGNOSTIC ONLY. Their qualified owners are hosted by the normal product. |

Display changes retain parent HWND, child HWND, renderer lease, exact document and existing flight owner in both Debug and Release tests. The legacy unhosted F-key admission behavior is preserved; only hosted context admission is added. Flight feedback uses the existing scene's status projection and is also shown in the application so borderless mode does not hide it.
