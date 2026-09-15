$ErrorActionPreference='Stop'
$repo='E:\NovaCore';$evidence=$PSScriptRoot;$prior=Split-Path -Parent $evidence;$ordinary=Split-Path -Parent $prior
$root=Join-Path $repo 'build\powered-contact-allocation-recheck';$allocation=Join-Path $root 'allocation';$scratch=Join-Path $root 'timing'
if(Test-Path -LiteralPath $scratch){throw 'Existing timing run: no overwrite/retry'}
if((Get-Content -LiteralPath (Join-Path $evidence 'native-equivalence.json') -Raw|ConvertFrom-Json).result -ne 'NATIVE_EQUIVALENCE_PASS'){throw 'Native gate not passed'}
New-Item -ItemType Directory -Path $scratch|Out-Null
$old=[IO.File]::ReadAllText((Join-Path $ordinary 'Program.cs'))
[IO.File]::WriteAllText((Join-Path $scratch 'NativeTypes.cs'),$old.Substring(0,$old.IndexOf('internal static class Program')))
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><NovaCoreUsesBepu>true</NovaCoreUsesBepu><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>Timing</StartupObject></PropertyGroup>
 <ItemGroup>
  <Compile Include="NativeTypes.cs"/><Compile Include="$evidence\Timing.cs"/>
  <Compile Include="$allocation\ActiveCorrection.cs"/><Compile Include="$allocation\RetainedMapper.cs"/>
  <Compile Include="$prior\allocation-closure\Attribution.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Resources\PropellantInteger.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Actuation\PoweredFlightNumerics.cs"/>
  <Compile Include="$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs"/>
 </ItemGroup>
</Project>
"@
$projectPath=Join-Path $scratch 'Timing.csproj';[IO.File]::WriteAllText($projectPath,$project)
Set-Location -LiteralPath $repo
dotnet build $projectPath -c Release -p:ContinuousIntegrationBuild=true --nologo | Tee-Object -FilePath (Join-Path $scratch 'build.txt')
if($LASTEXITCODE -ne 0){throw 'Timing build failed; STOP'}
for($i=1;$i -le 3;$i++){
 dotnet (Join-Path $scratch 'bin\Release\net10.0\Timing.dll') (Join-Path $ordinary 'inputs.json') (Join-Path $ordinary 'off-com-8.json') (Join-Path $scratch "process-$i.json") $i | Tee-Object -FilePath (Join-Path $scratch "stdout-$i.txt")
 if($LASTEXITCODE -ne 0){throw "Timing process $i failed; STOP, no retry"}
 Copy-Item -LiteralPath (Join-Path $scratch "process-$i.json") -Destination (Join-Path $prior "candidate-process-$i.json")
}
