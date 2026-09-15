$ErrorActionPreference='Stop'
$repo='E:\NovaCore';$evidence=$PSScriptRoot;$prior=Split-Path -Parent $evidence;$ordinary=Split-Path -Parent $prior
$root=Join-Path $repo 'build\powered-contact-allocation-recheck';$allocation=Join-Path $root 'allocation';$scratch=Join-Path $root 'native'
if(Test-Path -LiteralPath $scratch){throw 'Existing native run: no retry/overwrite'}
$result=Get-Content -LiteralPath (Join-Path $allocation 'result.json') -Raw|ConvertFrom-Json
if($result.result -ne 'MIGRATED_CONTRACT_ALLOCATION_PASS' -or $result.readBytes -ne 0 -or $result.repeated -ne 0 -or @($result.single|Where-Object {$_ -ne 0}).Count){throw 'Allocation gate not passed'}
New-Item -ItemType Directory -Path $scratch|Out-Null
$text=[IO.File]::ReadAllText((Join-Path $ordinary 'Program.cs'))
function Replace-Exactly([string]$text,[string]$before,[string]$after){if(-not $text.Contains($before)){throw "Missing source: $before"};$text.Replace($before,$after)}
$text=Replace-Exactly $text 'internal static class Program' 'internal static class NativeWitness'
$text=Replace-Exactly $text 'var baseline=input.GetProperty("baseline").GetBoolean();' @'
var baseline=input.GetProperty("baseline").GetBoolean();
        var fixedInput=new FixedInput(new ExactInput(input));
        var mapped=Correction.Map(fixedInput).Floats;
        float[] actual=[mapped.Ax,mapped.Ay,mapped.Wx,mapped.Wy,mapped.InverseMass,mapped.Dt];
        float[] expected=[(float)ax.Value,(float)ay.Value,(float)wx.Value,(float)wy.Value,(float)(Rational.Parse("1")/mass).Value,(float)h.Value];
        for(var k=0;k<6;k++)if(BitConverter.SingleToInt32Bits(actual[k])!=BitConverter.SingleToInt32Bits(expected[k]))throw new InvalidOperationException("FP32 transport changed");
'@
$text=Replace-Exactly $text 'InverseMass=(float)(Rational.Parse("1")/mass).Value' 'InverseMass=mapped.InverseMass'
$text=Replace-Exactly $text 'var backendDt=(float)h.Value;' 'var backendDt=mapped.Dt;'
$text=Replace-Exactly $text 'state.Linear=new((float)ax.Value,(float)ay.Value,0);state.Angular=new((float)wx.Value,(float)wy.Value,0);' 'state.Linear=new(mapped.Ax,mapped.Ay,0);state.Angular=new(mapped.Wx,mapped.Wy,0);'
[IO.File]::WriteAllText((Join-Path $scratch 'NativeWitness.cs'),$text)
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><NovaCoreUsesBepu>true</NovaCoreUsesBepu><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>NativeWitness</StartupObject></PropertyGroup>
 <ItemGroup>
  <Compile Include="NativeWitness.cs"/>
  <Compile Include="$allocation\ActiveCorrection.cs"/><Compile Include="$allocation\RetainedMapper.cs"/>
  <Compile Include="$prior\allocation-closure\Attribution.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Resources\PropellantInteger.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Actuation\PoweredFlightNumerics.cs"/>
  <Compile Include="$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs"/>
 </ItemGroup>
</Project>
"@
$projectPath=Join-Path $scratch 'NativeWitness.csproj';[IO.File]::WriteAllText($projectPath,$project)
Set-Location -LiteralPath $repo
dotnet build $projectPath -c Release -p:ContinuousIntegrationBuild=true --nologo | Tee-Object -FilePath (Join-Path $scratch 'build.txt')
if($LASTEXITCODE -ne 0){throw 'Native build failed; STOP'}
dotnet (Join-Path $scratch 'bin\Release\net10.0\NativeWitness.dll') (Join-Path $ordinary 'inputs.json') off-com (Join-Path $scratch 'result.json') 8 | Tee-Object -FilePath (Join-Path $scratch 'stdout.txt')
if($LASTEXITCODE -ne 0){throw 'Native witness failed; STOP'}
$old=Get-Content -LiteralPath (Join-Path $ordinary 'off-com-8.json') -Raw|ConvertFrom-Json -AsHashtable
$new=Get-Content -LiteralPath (Join-Path $scratch 'result.json') -Raw|ConvertFrom-Json -AsHashtable
$oldPath=$old.binaryIdentity.bepuPath;$newPath=$new.binaryIdentity.bepuPath
$old.binaryIdentity.Remove('bepuPath');$new.binaryIdentity.Remove('bepuPath')
$sections=@(foreach($key in $old.Keys){[pscustomobject]@{section=$key;identical=(($old[$key]|ConvertTo-Json -Depth 100 -Compress) -ceq ($new[$key]|ConvertTo-Json -Depth 100 -Compress))}})
$pass=@($sections|Where-Object {-not $_.identical}).Count -eq 0 -and $old.Count -eq $new.Count
$equality=[ordered]@{result= if($pass){'NATIVE_EQUIVALENCE_PASS'}else{'STOP_NATIVE_DIFFERENCE'};oldWitnessSha256=(Get-FileHash -LiteralPath (Join-Path $ordinary 'off-com-8.json')).Hash;newWitnessSha256=(Get-FileHash -LiteralPath (Join-Path $scratch 'result.json')).Hash;excludedField='binaryIdentity.bepuPath only (different build output directory)';oldBepuPath=$oldPath;newBepuPath=$newPath;sections=$sections;sourceAndEndpointSnapshots=$new.snapshots.Count;ordinarySteps=$new.backendSteps;substeps=$new.substeps;iterations=$new.iterations;nativeDtBits=$new.backendDtBits}
[IO.File]::WriteAllText((Join-Path $evidence 'native-equivalence.json'),($equality|ConvertTo-Json -Depth 8)+"`n")
if(-not $pass){throw 'Native comparison differs; STOP'}
Write-Output 'NATIVE_EQUIVALENCE PASS: every original section identical except assembly directory'
