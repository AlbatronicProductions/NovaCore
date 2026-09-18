# Reproduction and retained identities

The three authorized timing captures are complete. These instructions preserve reproducibility; they do not authorize a fourth capture or new investigation. Manual acceptance uses the separate uninstrumented recipe.

Current source is sealed by `../stock-florida-restart/candidate-seals.json` and its protected predecessor map. `capture-preflight.json` pins the observer input, generated scene, executable and DLLs before the first process. `capture-results.json` retains original indices, timestamps, critical neighborhoods, all selected >1 ms service / >6.67 ms display events, full population statistics and raw log hashes. Source helpers are small, independently authored evidence code and are not project compile inputs by default.

No working production or permanent test source was edited. `build-inputs.py` makes disposable copies of the existing Graphics test entry and sample scene. The custom MSBuild target adds the terminal proof to the Graphics test build and replaces only the sample's timing wrapper with `FloridaTimingProbe`. Debug storage invokes the original unchanged permanent method with the original sample implementation. The sensor build is used only by the three Release captures, never by the manual route.

Actual build/test commands, from `E:\NovaCore`:

```powershell
python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/final-closure/build-inputs.py
dotnet build tests/NovaCore.Graphics.Tests -c Debug --artifacts-path build/srv01-stage5-final-closure/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage3/native-debug -p:CustomAfterMicrosoftCommonTargets=E:\NovaCore\build\srv01-stage5-final-closure\generated\closure.targets --nologo -v:q
dotnet build/srv01-stage5-final-closure/artifacts/bin/NovaCore.Graphics.Tests/debug/NovaCore.Graphics.Tests.dll --assembly-florida-terminal-proof
dotnet build/srv01-stage5-final-closure/artifacts/bin/NovaCore.Graphics.Tests/debug/NovaCore.Graphics.Tests.dll --assembly-florida-presentation-storage
dotnet build samples/NovaCore.Triangle -c Release --artifacts-path build/srv01-stage5-final-closure/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage3/native-release -p:CustomAfterMicrosoftCommonTargets=E:\NovaCore\build\srv01-stage5-final-closure\generated\closure.targets --nologo -v:q
```

Three fresh sequential processes used the same arguments below, with each stdout/stderr directed to its own new `live-1.log`, `live-2.log`, `live-3.log`. Each process journal retains start/end UTC and exit code. No existing output was overwritten. Do not execute now:

```powershell
& 'E:\NovaCore\build\srv01-stage5-final-closure\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe' --scene=srv01-florida-support --benchmark-frames=4000 --log=renderer,vulkan
```

`analyze-captures.py` reads those exact three logs and journals, validates row continuity/terminal state/trajectory equality, and consolidates evidence. It does not launch anything. Repeating instrumentation could alter scheduling/first-use cost; the retained first results are not replaceable acceptance samples.

Final regressions used the previously sealed, uninstrumented `build/srv01-stage5-stock/artifacts/bin/{project}/{debug|release}/{project}.dll`: Simulation, ReferenceFrames, Precision and BepuDependency with no arguments; Graphics with `--assembly-florida-presentation`. All passed. Full Simulation registration contains 80 top-level groups; `finalize.py` verifies each group's exact PASS line rather than counting all nested PASS messages.

The existing Launcher test closure was copied unchanged from that artifacts directory to `build/srv01-stage5-final-closure/launcher/{debug|release}/net10.0-windows` so its five-parent repository-path assumption remains valid. Each executable passed 18/18. This tests the existing launcher; the dedicated SRV-01 CLI route is separately proved by parsing/presentation/live execution. There is no new GUI catalog entry.

After copies of disposable logs/binaries are deleted, retain all source/evidence and native/manual dependencies identified in `cleanup.md`. Rebuild native output as in the preserved Stage4 reproduction instructions if eventually released. Do not expect log-consolidation scripts to work after their raw disposable inputs have been deleted. The retained statistical witnesses, selected timing events and hashes remain authoritative; regenerating host timing cannot reproduce an identical scheduling trace.

Read-only manual preparation check (does not open a fourth window):

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\final-closure\launch.ps1' -PrepareOnly
```

The final source/ref/whitespace/validation audit is in `closure-verification.json`. No permanent instrumentation, new physics policy, KSA import, milestone or banking is part of this work.
