# Evidence only. One fresh Release allocation process; never retries or runs timing.
param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$repo = 'E:\NovaCore'
$evidence = $PSScriptRoot
$parent = Split-Path -Parent $evidence
$scratch = Join-Path $repo 'build\powered-contact-ordinary-step-performance-payoff'
if (Test-Path -LiteralPath $scratch) { throw 'Scratch already exists. Review it before a new independent reproduction.' }
$source = Join-Path $parent 'Program.cs'
if ((Get-FileHash -LiteralPath $source).Hash -ne '7DC5838082E1C4BEA99D270CBEB26A20E47CEA9999A15716A52EDCEF486B07C7') { throw 'Retained candidate source changed' }
if ((Get-FileHash -LiteralPath (Join-Path $parent 'inputs.json')).Hash -ne '6920D051FA273A4AFAAA4787788B5D972C3B4B38372491A171743869031477D0') { throw 'Exact inputs changed' }
if ((Get-FileHash -LiteralPath (Join-Path $parent 'off-com-8.json')).Hash -ne '0607084EEAB96D11BE81EFDA642D11DB49435A9C2C9B83D9DFC5936BA3E21FE8') { throw 'Accepted prepared-output witness changed' }
if ((Get-FileHash -LiteralPath (Join-Path $repo 'tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs')).Hash -ne '3B6F2D9905EA52BFB131DF670F1E6A2116BA340C9EF7767DE1020197D473CFB7') { throw 'Qualified measurement helper changed' }
$unexpected = @(Get-ChildItem Env: | Where-Object { $_.Name -match '^(COMPlus_|CORECLR_|COR_PROFILER|COR_ENABLE_PROFILING)' -or ($_.Name -match '^DOTNET_' -and $_.Name -notin @('DOTNET_ROOT','DOTNET_ROOT(x86)')) })
if ($unexpected.Count) { throw 'Review runtime environment overrides before execution' }
Set-Location -LiteralPath $repo
if ((dotnet --version) -ne '10.0.303') { throw 'SDK identity changed' }
New-Item -ItemType Directory -Path $scratch | Out-Null
# Extract exactly the existing type, with unchanged operator/Value implementation.
$text = [IO.File]::ReadAllText($source)
$start = $text.IndexOf('internal readonly struct Rational')
$end = $text.IndexOf('internal sealed class InputState')
if ($start -lt 0 -or $end -le $start) { throw 'Cannot extract exact retained Rational type' }
[IO.File]::WriteAllText((Join-Path $scratch 'Rational.cs'), "using System.Globalization;`nusing System.Numerics;`n" + $text.Substring($start, $end - $start))
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$evidence\Program.cs" />
    <Compile Include="Rational.cs" />
    <Compile Include="$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $scratch 'PreparationAllocation.csproj'
[IO.File]::WriteAllText($projectPath, $project)
dotnet build $projectPath -c Release -p:ContinuousIntegrationBuild=true --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed; stop' }
if ($BuildOnly) { Write-Output "Build only complete. No measurement run. Scratch: $scratch"; return }
dotnet (Join-Path $scratch 'bin\Release\net10.0\PreparationAllocation.dll') (Join-Path $parent 'inputs.json') (Join-Path $parent 'off-com-8.json') (Join-Path $scratch 'allocation-result.json')
if ($LASTEXITCODE -eq 2) { Write-Output 'STOP: warmed candidate allocation. No timing, retries or optimization.'; exit 2 }
if ($LASTEXITCODE -ne 0) { throw 'Technical probe failure; stop' }
Write-Output 'C zero only; timing remains unqualified. Return to the declared plan.'
