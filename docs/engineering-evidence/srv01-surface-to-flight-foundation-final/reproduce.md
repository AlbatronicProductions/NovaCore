# Reproduction from the final candidate

Use baseline ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5 plus the exact candidate paths in
bank-prep.json. Require all702 identity.json hashes before qualification. Read
qualified-responsibilities.md and the historical checkpoint's own scope before using
an older witness. Never restore an old checkpoint over the active candidate.

Prerequisites: qualified repository BEPU bytes, .NET10 SDK, x64 VS/CMake/Ninja/Vulkan
toolchain, pinned stock catalog/GLBs, current global elevation and authenticated
regional earth-florida-m12 physical dataset. KSA is reference-only, never runtime input.
The exact installed commands/tool paths used are retained in build.ps1.

From PowerShell7:

~~~powershell
Set-Location -LiteralPath 'E:\NovaCore'
& '.\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\build.ps1' Debug
& '.\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\build.ps1' Release
& '.\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\validate.ps1'
~~~

Each build explicitly builds native prerequisites. validate.ps1 runs76 bounded gates
and stops on failure; no broad default GPU campaign. Launcher test copies use the
unchanged harness's five-parent layout under the disposable build root. Performance
is separate; --florida-slab-costs is the existing128/1024 operation population, and
historical per-stage recipes define their own populations. Do not silently rerun
timing, apply retired micro-gates or relabel old measurements as current fresh processes.
consolidate.py extracts logs only. It is a one-time closeout collector with an explicit
three-source-change assertion, not a perpetual baseline-updating command.

## Launch the accepted shared Florida scenario

After a Release build, the normal launcher expects its existing executable locations.
Copy the exact verified Release outputs there; no alternate scenario resolver is needed:

~~~powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-foundation-final\prepare-launcher.ps1'
& 'E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe'
~~~

Use FL Launchpad -> Play. For the bounded READY/Space route, use that same built sample
with --scene=srv01-florida-support. Both resolve authenticated site/slab/stock construction;
GUI owns its existing current-time Solar navigation and autostarts only after preparation.
Manual freezes display epoch at J2000 and starts on Space. GUI holds the final site-relative
endpoint while Solar exploration continues. This distinction is explicit, not duplicated
physical scene setup. No new manual run was performed during closeout.

## Historical reconstruction / compact retained inputs

check-history.py resolves the sealed historical inputs against current files, baseline
Git and retained archives without modifying any candidate source. Its result is
historical-source-resolution.json: Stage3, Stage4, initial failed Stage5, stock restart
and prior presentation inputs all resolve. The two old stock test files missing from
earlier archives were reconstructed in memory from their recorded edits and proved
byte-exact by the already-retained SHA256 pins; historical-stock-test-inputs.zip retains
only those two inputs. It contains no build products or external source.

The old causal probe's relative references to build/srv01-stage5 identify disposable
outputs, not irreplaceable authority. In an isolated historical source copy, overlay
Stage4's sealed snapshot and failed Stage5 inputs as their original reproduction.md
directs, build Graphics Debug to that expected artifacts path, then use the retained
Probe.cs/Probe.csproj. No old binary directory is needed once these inputs are secured.
Unique diagnostic capture scripts/native probes remain evidence-only reproduction tools;
their source/state hashes and failed witnesses remain historical. Do not include bulk KSA
decompilation or recreate every trace to review this candidate.
