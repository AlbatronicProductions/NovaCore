# Bounded reproduction

These commands document the completed gates. They are not authorization to restart acceptance after the declared visual stop. Use PowerShell from `E:\NovaCore`; stop on the first actual failure.

```powershell
dotnet build NovaCore.sln -c Debug --no-restore -v:q
dotnet build NovaCore.sln -c Release --no-restore -v:q
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --contact-servicing-cheap
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --contact-servicing-only
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --contact-servicing-only
```

Performance was exactly three fresh Release processes, no retries, each invoking:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --contact-servicing-performance
```

Full Simulation omits the focused switch. The existing M14.18/M14.19 focused switches are `--local-contact-staging-only` and `--persistent-contact-only`. All were run in both configurations.

M14.17 focused cases, each configuration:

```powershell
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll '--case=Certified Florida continuation publication'
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll '--case=Certified continuation acceptance gaps'
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll '--case=Certified continuation allocation matrix'
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll '--case=Contact development canonical presentation'
```

ReferenceFrames, Precision and BepuDependency executables use `tests/NovaCore.<name>.Tests/bin/<configuration>/net10.0/NovaCore.<name>.Tests.dll`. Launcher uses `net10.0-windows`, not `net10.0`.

Native libraries must be rebuilt before full solution build when native sources change. Use the repository's configured MSVC developer environment, then `cmake --build build/native-ninja --parallel 4` and `cmake --build build/native-ninja-release --parallel 4`. Existing build documentation remains authoritative for tool installation/configuration.

No profiling, global runtime changes, allocation tolerance, suite-order edits or arbitrary warmup changes are required. The new allocation gates use `OrdinaryAllocationMeasurement` and its independent positive control. Compare current hashes with `identity.json` before relying on retained results. The later title-only sample change did not change qualified Simulation code/binaries.
