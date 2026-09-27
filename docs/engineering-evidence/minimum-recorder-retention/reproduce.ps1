param([string]$Repository='E:\NovaCore',[string]$Output='E:\NovaCore\build\minimum-recorder-retention-reproduction')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $Repository
New-Item -ItemType Directory -Force -Path $Output | Out-Null
# CPU-only base durability/native mock build and qualification. No player launch.
& "$PSScriptRoot/../ordinary-player-minimum-recorder/reproduce.ps1" -Repository $Repository -Output $Output
if($LASTEXITCODE -ne 0){throw 'Base qualification failed'}
foreach($configuration in @('Debug','Release')){
  $worker=(Resolve-Path "tools/NovaCore.Recorder/bin/$configuration/net10.0-windows/NovaCore.Recorder.exe").Path
  & dotnet run --project tests/NovaCore.Retention.Tests -c $configuration --no-build -- "$Output/retention-$configuration" $worker 2>&1 | Tee-Object "$Output/retention-$configuration.log"
  if($LASTEXITCODE -ne 0){throw "Retention $configuration failed"}
}
& dotnet run --project tests/NovaCore.App.Diagnostics.Tests -c Release --no-build -- "$Output/app-diagnostics"
if($LASTEXITCODE -ne 0){throw 'App diagnostics failed'}
& dotnet run --project tests/NovaCore.Launcher.Tests -c Release --no-build
if($LASTEXITCODE -ne 0){throw 'Launcher failed'}
# Real-root cleanup is deliberately separate from fixture qualification:
# tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.exe retain-runtime
# It accepts NO root override. Never use the old benign freeze script to reseal
# this changed candidate. Never start NovaCore.exe from this reproduction route.
