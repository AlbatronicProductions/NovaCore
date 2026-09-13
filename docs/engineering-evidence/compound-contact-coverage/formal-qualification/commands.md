# Qualification reproduction

Run from E:\NovaCore in PowerShell 7, using SDK 10.0.303 / runtime 10.0.12.
First verify identity; the script checks all 392 retained source/test/sample/dependency hashes.
It does not invoke old diagnostic injectors or automatically repeat a failed test.

These permanent focused routes reproduce the formal responsibilities. Builds and tests were
executed sequentially, with a stop on material failure. No profilers or alternate solver/runtime
settings were used. Earlier intermediate harness compilation was also performed; it is not a
separate acceptance repetition.

~~~powershell
Set-Location -LiteralPath 'E:\NovaCore'
& './docs/engineering-evidence/compound-contact-coverage/formal-qualification/reproduce.ps1' -Gate identity
dotnet build NovaCore.sln -c Debug --nologo
dotnet build NovaCore.sln -c Release --nologo
~~~

For each configuration Debug and Release, the focused allocation commands were:

~~~powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-allocation
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --compound-selector-allocation
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-allocation
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --compound-selector-allocation
~~~

Each command below starts a fresh Release process. The four physical routes executed once each.
Schedules had one initial metadata-only harness failure and one attribution reproduction, then
one corrected pass; the failure/SPLIT evidence is retained in results.json.

~~~powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-storage
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-physical-centered
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-physical-tilted
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-physical-mirrored
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-physical-moving
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-schedules
~~~

The following performance command was executed exactly three times in fresh processes. Each
contains exactly 128 warm complete operations followed by 1,024 measured complete operations.
Stop on the first failure; do not retry for a better number.

~~~powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-performance
~~~

Separate one-process selector cost and cold diagnostics:

~~~powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --compound-selector-cost
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-cold
~~~

Full suites were then run once per configuration, with 56/56 registered groups each:

~~~powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
~~~

Exact focused regression commands (ReferenceFrames, Precision, dependency, presentation,
M14.17 publication/gaps and Launcher for both configurations) are retained in
results.json/focused_regressions. Each recorded command exited zero.

Use launch.ps1 centered and launch.ps1 tilted for manual acceptance; each starts a fresh Release
window at 1280x720. The user waits for READY before Space, so native/presentation preparation is
not admitted as simulation debt. Output is saved only when the window closes. The launch script
overwrites that case's diagnostic logs, so retain useful evidence first if doing a later campaign.

Frame-statistics provenance: ContactDevelopmentScene captures every running callback interval
and the final completion interval into a bounded preallocated array, includes all samples in
nearest-rank percentiles, and logs every interval above 6.67 ms with preceding owner work.
Native lifecycle frame/GPU/fence summaries have different sample windows; do not combine them
as though they were synchronized per-frame samples.

Finally verify git diff --check, empty index, baseline refs/tags, accepted selector/protected
hashes and the identity guard. No banking follows from running these commands.
