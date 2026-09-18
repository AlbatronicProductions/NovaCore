# Independent red team

Read-only verifier `/root/architecture_verifier` reviewed the current implementation against the sealed entry sources, the user correction and the latest explicit Project Control manual PASS. No edits or suite reruns were made by the verifier. It verified all eleven entry-source seals, inspected both clean build logs and all 96 successful gate records, and returned **PASS: no production blocker found**. Runtime performance and deployment reconciliation are separate lead checks.

| Attack | Result |
|---|---|
| 1. Ad-hoc Earth counterrotation | PASS: compose named parent basis and retained quaternion; no Earth compensation angle. |
| 2. Florida coordinates hard-coded | PASS: generic descriptor, accepted site basis supplied by publisher; non-Earth-parent test. |
| 3. Earth rotation changed | PASS: no celestial rotation implementation delta. |
| 4. Stale target epoch | PASS: current clock and PresentationTicks required; stale sample refused. |
| 5. Target/environment epoch mismatch | PASS: target, parent and environment resolve from one published snapshot. |
| 6. Wrong orbit basis | PASS: full quaternion stored locally, reconstructed through current reference basis. |
| 7. Orbit/zoom regression | PASS: local input and distance tests, prior regressions and Project Control manual PASS. |
| 8. F resets physical state | PASS: presentation-only ownership; canonical bytes/save invariants. |
| 9. Celestial regression | PASS: Earth/Moon/Sun/Mars and retained F-refocus. |
| 10. Departure transition impossible | PASS: explicit publisher-owned descriptor change, same-new-epoch pose/roll rebase, including off-vessel retained view. Actual departure remains downstream. |
| 11. Warp multiplies camera work | PASS: production input deferred; Solar progression skips vessel placement; refresh reconstructs once/display. Tests include F and pause. |
| 12. New per-frame allocation | PASS within camera contract: exact zero. Full dynamic Solar publication remains 2320 B/display and is reported separately. |
| 13. Terrain masks wrong math | PASS: clear-view invariant populations record zero terrain corrections. |
| 14. FREE reintroduced | PASS: reserved action bit inert; FREE deferred. |
| 15. HOME rebound | PASS: native input source identical to entry; HOME unbound. |
| 16. Physics changed | PASS: no simulation-source delta; support and nonmutation gates pass. |
| 17. Manual acceptance self-declared | PASS: current acceptance comes from Project Control's explicit user report. |
| 18. Banking/milestone | PASS: HEAD/bank unchanged; index empty; no banking authorization. |

Release permanent-test maxima across 1x/120x/14400x/86400x: local offset 0.000014350 m; horizon-vector 0.000000508; distance 0.000013058 m; zero terrain corrections; unchanged canonical observations. Tolerances are 0.0001 m offset/distance and 0.000005 horizon-vector, accounting for FP64 subtraction at astronomical roots. Large-coordinate transport through 7e12 m remains separately covered.

Reviewed source hashes are sealed in `identity.json` and must match this independent readset:

| File | SHA-256 |
|---|---|
| native/NovaCore.Native/NovaCoreNative.cpp | 53E4E81A04364CBE9C357A697491EA0F6CA211B332080E1BDFA69E743885A99A |
| native/NovaCore.Native/NovaCoreNative.h | AADBEE1AE791C8EB6572995CA62323BC15C594810238F4DEB4DEC003BD6754BF |
| samples/NovaCore.Triangle/Program.cs | 21C9617B5C8D5E4376EDD73BD3ED9489117AC3B881F4D126353E0B797A614EA5 |
| samples/NovaCore.Triangle/SolarSystemScene.cs | 66A1910805F1C9351A3EFE927D8610663E99557D3ADA4AE86DA2619D0CC3B011 |
| samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs | 6E715D28BB54970E2D63D236BCF0D31BED143DC9C6B4CBEE172D59093B371167 |
| src/NovaCore.Graphics/FocusTarget.cs | 7448547501D3CD4E11D86C9C1812F8137757EA0222CE57DCB1596DBFF9C876E9 |
| src/NovaCore.Graphics/SceneObjectFocusObservation.cs | 6B9848F381AF69BA19AE97AC868E385E6F4AAC80A63BC0C1C732D613A01A771C |
| src/NovaCore.Interop/NativeRuntime.cs | F514342907FCC97EBB2A469A00E2E8C2BCFFD10A48323D7E61E6586D3518D4FE |
| tests/NovaCore.Graphics.Tests/AssemblyFloridaSolarTests.cs | 0EF49A0F829F12E1ECC882CB5EF494B6CA9D5302922246F1EEDD9BA727DC48FA |
| tests/NovaCore.Graphics.Tests/Program.cs | 1EBC0AA24FB20A4F257130621101CAE9CECEF0A041EB1222BDF6D92C058146F8 |
| tests/NovaCore.Graphics.Tests/ActiveVesselCameraTests.cs | 976B533BADE9DBF0CC69B517E7D56A0E10FBF89675B048AB23691A0031135178 |

Limits: managed tests do not establish GPU/native timing. Initial attempted whole-display NoGC measurement failed and is not relabeled a success. Future powered departure, mobile-surface frame-selection policies and FREE remain outside qualification.
