param([string]$Repository='E:\NovaCore',[string]$Output='E:\NovaCore\build\ordinary-recorder-benign-reproduction')
$ErrorActionPreference='Stop'
# CPU-only. This script never starts NovaCore.exe or a Vulkan device.
& "$PSScriptRoot/../reproduce.ps1" -Repository $Repository -Output $Output
if($LASTEXITCODE -ne 0){throw 'Base offline qualification failed'}
Set-Location -LiteralPath $Repository
foreach($configuration in @('Debug','Release')){
  & dotnet run --project tests/NovaCore.Graphics.Tests -c $configuration --no-build -- --benign-recorder-route 2>&1 | Tee-Object -FilePath "$Output/camera-$configuration.log"
  if($LASTEXITCODE -ne 0){throw "Camera route $configuration failed"}
}
& dotnet run --project tests/NovaCore.App.Diagnostics.Tests -c Release --no-build -- "$Output/app-diagnostics" 2>&1 | Tee-Object -FilePath "$Output/app-diagnostics.log"
if($LASTEXITCODE -ne 0){throw 'Application diagnostics failed'}
& dotnet run --project tests/NovaCore.Launcher.Tests -c Release --no-build 2>&1 | Tee-Object -FilePath "$Output/launcher.log"
if($LASTEXITCODE -ne 0){throw 'Launcher regressions failed'}
# Existing retained retry only, without any launch:
# python docs/engineering-evidence/ordinary-player-minimum-recorder/benign-revision/analyze.py
# python docs/engineering-evidence/ordinary-player-minimum-recorder/benign-revision/freeze.py
# reconcile.cs is a CPU-only full-prefix replay helper. Compile it in an ignored
# temporary net10.0-windows console project referencing the canonical
# NovaCore.Diagnostics.dll, then pass the absolute live-retry directory.
# All future GPU launches require fresh Project Control authorization.
