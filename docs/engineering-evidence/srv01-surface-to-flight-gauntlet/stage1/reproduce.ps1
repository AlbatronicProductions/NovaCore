param([string]$Repository='E:\NovaCore')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath $Repository).Path
$output=Join-Path $repo 'build\srv01-surface-to-flight-stage1\artifacts'
Set-Location -LiteralPath $repo
# Requires the retained unbanked candidate fingerprints in identity.json.
# This is reproduction, not authority to repeat qualification after a failed bar.
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --artifacts-path $output -p:ContinuousIntegrationBuild=true --nologo -v:q
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$runner=Join-Path $output 'bin\NovaCore.Simulation.Tests\debug\NovaCore.Simulation.Tests.dll'
foreach($gate in @('--assembly-contact-geometry','--assembly-contact-cheap','--assembly-contact-authority','--assembly-contact-physical'))
{
    dotnet $runner $gate
    if($LASTEXITCODE -ne 0){throw "STOP: $gate failed; no retry or later gate"}
}
