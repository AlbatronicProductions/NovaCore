param([ValidateSet('Attribution','Correction')][string]$Mode='Attribution')
$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$root=Join-Path $repo 'build\powered-contact-event-allocation-closure'
$scratch=Join-Path $root $Mode.ToLowerInvariant()
if(Test-Path -LiteralPath $scratch){throw 'Previous output exists; review before any reproduction, no overwrite/retry'}
$evidence=$PSScriptRoot
$prior=Split-Path -Parent $evidence
$ordinary=Split-Path -Parent $prior
$old=Join-Path $ordinary 'Program.cs'
$oldMapper=Join-Path $prior 'Program.cs'
if((Get-FileHash -LiteralPath $old).Hash -ne '7DC5838082E1C4BEA99D270CBEB26A20E47CEA9999A15716A52EDCEF486B07C7'){throw 'Old exact type source changed'}
if((Get-FileHash -LiteralPath $oldMapper).Hash -ne '1529DE51803106572720B4D305BAFA2376C5C1C7E43D277C34A1324B79477A97'){throw 'Old mapper changed'}
if((Get-FileHash -LiteralPath (Join-Path $ordinary 'inputs.json')).Hash -ne '6920D051FA273A4AFAAA4787788B5D972C3B4B38372491A171743869031477D0'){throw 'Inputs changed'}
if((Get-FileHash -LiteralPath (Join-Path $repo 'tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs')).Hash -ne '3B6F2D9905EA52BFB131DF670F1E6A2116BA340C9EF7767DE1020197D473CFB7'){throw 'Helper changed'}
Set-Location -LiteralPath $repo
if((dotnet --version) -ne '10.0.303'){throw 'SDK changed'}
$overrides=@(Get-ChildItem Env:|Where-Object {$_.Name -match '^(COMPlus_|CORECLR_|COR_PROFILER|COR_ENABLE_PROFILING)' -or ($_.Name -match '^DOTNET_' -and $_.Name -notin @('DOTNET_ROOT','DOTNET_ROOT(x86)'))})
if($overrides.Count){throw 'Runtime overrides require review'}
New-Item -ItemType Directory -Path $scratch -Force|Out-Null
$text=[IO.File]::ReadAllText($old);$start=$text.IndexOf('internal readonly struct Rational');$end=$text.IndexOf('internal sealed class InputState')
[IO.File]::WriteAllText((Join-Path $scratch 'Rational.cs'),"using System.Globalization;`nusing System.Numerics;`n"+$text.Substring($start,$end-$start))
$text=[IO.File]::ReadAllText($oldMapper);$prefix=$text.Substring(0,$text.IndexOf('internal static class Program'))
$start=$text.IndexOf('    [MethodImpl(MethodImplOptions.NoInlining)]');$end=$text.IndexOf('    // Control for the call')
$map=$text.Substring($start,$end-$start).Replace('private static Prepared Map','internal static Prepared Map')
[IO.File]::WriteAllText((Join-Path $scratch 'RetainedMapper.cs'),$prefix+"internal static class RetainedMapper`n{`n"+$map+"}`n")
$extra=''
$startup='Attribution'
if($Mode -eq 'Correction'){
    $extra=@"
    <Compile Include="$evidence\Correction.cs" />
    <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Resources\PropellantInteger.cs" />
    <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Actuation\PoweredFlightNumerics.cs" />
"@
    $startup='Correction'
}
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>$startup</StartupObject></PropertyGroup>
  <ItemGroup>
    <Compile Include="$evidence\Attribution.cs" />
    <Compile Include="Rational.cs" />
    <Compile Include="RetainedMapper.cs" />
    <Compile Include="$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs" />
$extra
  </ItemGroup>
</Project>
"@
$projectPath=Join-Path $scratch 'AllocationRevision.csproj';[IO.File]::WriteAllText($projectPath,$project)
dotnet build $projectPath -c Release -p:ContinuousIntegrationBuild=true --nologo
if($LASTEXITCODE -ne 0){throw 'Diagnostic build failed; stop'}
dotnet (Join-Path $scratch 'bin\Release\net10.0\AllocationRevision.dll') (Join-Path $ordinary 'inputs.json') (Join-Path $scratch 'result.json') | Tee-Object -FilePath (Join-Path $scratch 'stdout.txt')
$result=$LASTEXITCODE
if($result -ne 0){Write-Output "STOP: $Mode returned $result. No rescue, retry or timing.";exit $result}
Write-Output "$Mode complete. Review result before subsequent gated work."
