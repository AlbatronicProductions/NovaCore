> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Reproduction

Run from `E:\NovaCore` with the installed .NET 10 SDK, Visual Studio C++ tools and Vulkan SDK recorded by the build. Keep native/managed candidate artifacts paired.

```powershell
& docs/engineering-evidence/active-vessel-camera/build-candidate.ps1 -Configuration Debug
& docs/engineering-evidence/active-vessel-camera/build-candidate.ps1 -Configuration Release
& docs/engineering-evidence/active-vessel-camera/validate.ps1
```

The validation script runs the three new camera gates, protected M15.3 assembly/Florida tests, focused Solar/terrain/input/ABI regressions, full Simulation/ReferenceFrames/Precision/Camera/BepuDependency suites, and Launcher tests in both configurations. Logs and exit records are written only under this task's build directory.

For isolated camera proofs after building:

```powershell
& build/active-vessel-camera/candidate/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.exe --florida-slab-camera-static
& build/active-vessel-camera/candidate/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.exe --florida-slab-camera-moving
& build/active-vessel-camera/candidate/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.exe --florida-slab-camera-costs
```

Performance baseline was built from clean entry HEAD `45b1bbcdd8d0e1d42f79f3bb5124e80753ce0e8f` before edits, with native output `build/active-vessel-camera/native-baseline` and managed artifacts `build/active-vessel-camera/baseline`. To recreate from scratch later, use an isolated checkout of that exact commit, populate the same required asset inputs, and build its native/runtime outputs into those baseline locations. Do not reset this working tree or substitute candidate dependencies. Baseline build/native logs and input/output hashes identify this run.

```powershell
& docs/engineering-evidence/active-vessel-camera/build-observer.ps1 -Version baseline
& docs/engineering-evidence/active-vessel-camera/build-observer.ps1 -Version candidate
& docs/engineering-evidence/active-vessel-camera/capture-performance.ps1 -Cadence live
& docs/engineering-evidence/active-vessel-camera/capture-performance.ps1 -Cadence fixed
```

Run timed capture sequentially without concurrent builds/tests. Each command captures two fresh processes per baseline matched, candidate matched and candidate new routes, 4,800 frames/process at 960×540. The observer uses the same production callback with a disposable compile overlay; source files are never rewritten. Fixed cadence feeds the existing assembly Advance API exactly 1/60 second per display frame. Live cadence retains ordinary AdvanceLive wall-clock servicing. Original live captures predate the added transition/after-callback-GC detail; their GC endpoint excludes the final callback. Fixed captures include those refinements.

`prepare-launcher.ps1` copies and verifies uninstrumented candidate files into the existing launcher/sample locations. Follow [manual-route.md](manual-route.md). No command in this package stages, commits, tags, pushes, banks or assigns a milestone.
