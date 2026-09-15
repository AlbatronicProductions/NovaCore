# Reproduction — evidence only, not authorization to resume

Use E:\NovaCore at the parent identity's exact17 source/test fingerprints, .NET SDK10.0.303,
runtime10.0.12. The root build\powered-contact-performance-revision must be absent after
reviewed cleanup: instrument.py refuses to overwrite saved preimages.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
python 'docs\engineering-evidence\powered-contact-production-integration\performance-revision\instrument.py' apply
if ($LASTEXITCODE -ne 0) { throw 'Instrumentation failed; stop' }
dotnet build 'tests\NovaCore.Simulation.Tests\NovaCore.Simulation.Tests.csproj' -c Release --artifacts-path 'E:\NovaCore\build\powered-contact-performance-revision\probe-artifacts' --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed; restore instrumentation, do not run' }
dotnet 'E:\NovaCore\build\powered-contact-performance-revision\probe-artifacts\bin\NovaCore.Simulation.Tests\release\NovaCore.Simulation.Tests.dll' --powered-contact-cost-probe
if ($LASTEXITCODE -ne 0) { throw 'Probe failed; restore instrumentation, no retry' }
python 'docs\engineering-evidence\powered-contact-production-integration\performance-revision\instrument.py' restore
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
```

Run only one fresh process. The script introduces timestamps into9 original files and creates
2 temporary diagnostic sources, saving exact preimages first. Restore is required even after
build/run failure; it does not delete build scratch. Verify hashes against the parent identity.
The one recorded process checks1024 same-operation time partitions and1152 complete history/
endpoint/native matches against observer-disabled execution. Statistics and all16 top samples
are retained in attribution.json; no bulk per-step logs are required.

No profiler, process config or solver change. Active instrumentation may perturb codegen;
its values assign bounded cost responsibility without claiming a runtime delay mechanism.
The probe is distinct from acceptance and does not call the failed gate retry-to-green.
