param([ValidateSet('Build','Cheap','Physical','Failures','Schedules','Allocation','Storage','Performance','CoordinateAttribution')][string]$Gate='Cheap',
      [ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-powered-support'
Push-Location $repo
try {
 if($Gate -eq 'CoordinateAttribution') {
  Add-Type -TypeDefinition (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'coordinate-attribution.cs.txt') -Raw)
  [pscustomobject]@{Uncompensated=[ContactCoordinateAttribution]::Run($false);Compensated=[ContactCoordinateAttribution]::Run($true)}|ConvertTo-Json
  return
 }
 if($Gate -eq 'Build') {
  dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c $Configuration --artifacts-path (Join-Path $scratch 'artifacts') -p:ContinuousIntegrationBuild=true --nologo -v:q
  if($LASTEXITCODE -ne 0){throw "Build failed: $LASTEXITCODE"};return
 }
 $exe=Join-Path $scratch "artifacts/bin/NovaCore.Simulation.Tests/$($Configuration.ToLowerInvariant())/NovaCore.Simulation.Tests.exe"
 if($Gate -eq 'Performance') {
  if($Configuration -ne 'Release'){throw 'Performance requires Release'}
  # Qualification protocol only; do not resume after the retained failure without Project Control.
  foreach($run in 1..3){& $exe --assembly-powered-contact-performance;if($LASTEXITCODE -ne 0){throw "Run $run failed; stop, no retry"}}
 } else {
  & $exe "--assembly-powered-contact-$($Gate.ToLowerInvariant())"
  if($LASTEXITCODE -ne 0){throw "Gate failed: $LASTEXITCODE"}
 }
} finally {Pop-Location}
