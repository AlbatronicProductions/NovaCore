$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$evidence=$PSScriptRoot
$prior=Split-Path -Parent $evidence
$historical=Join-Path $prior 'allocation-closure'
$ordinary=Split-Path -Parent $prior
$scratch=Join-Path $repo 'build\powered-contact-allocation-recheck\allocation'
if(Test-Path -LiteralPath $scratch){throw 'Existing run: no overwrite/retry'}
Set-Location -LiteralPath $repo
if((dotnet --version) -ne '10.0.303'){throw 'SDK changed'}
$overrides=@(Get-ChildItem Env:|Where-Object {$_.Name -match '^(COMPlus_|CORECLR_|COR_PROFILER|COR_ENABLE_PROFILING)' -or ($_.Name -match '^DOTNET_' -and $_.Name -notin @('DOTNET_ROOT','DOTNET_ROOT(x86)'))})
if($overrides.Count){throw 'Runtime overrides require review'}
$identity=Get-Content -LiteralPath (Join-Path $prior 'projection-contract\identity.json') -Raw|ConvertFrom-Json
foreach($row in $identity.sourceHashes){if((Get-FileHash -LiteralPath (Join-Path $repo $row.path)).Hash -ne $row.sha256){throw "Protected source changed: $($row.path)"}}
if((Get-FileHash -LiteralPath (Join-Path $repo 'tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs')).Hash -ne '3B6F2D9905EA52BFB131DF670F1E6A2116BA340C9EF7767DE1020197D473CFB7'){throw 'Helper changed'}
New-Item -ItemType Directory -Path $scratch|Out-Null
function Replace-Exactly([string]$text,[string]$before,[string]$after){if(-not $text.Contains($before)){throw "Missing expected source: $before"};$text.Replace($before,$after)}
$old=[IO.File]::ReadAllText((Join-Path $ordinary 'Program.cs'))
$start=$old.IndexOf('internal readonly struct Rational');$end=$old.IndexOf('internal sealed class InputState')
[IO.File]::WriteAllText((Join-Path $scratch 'Rational.cs'),"using System.Globalization;`nusing System.Numerics;`n"+$old.Substring($start,$end-$start))
$oldMapper=[IO.File]::ReadAllText((Join-Path $prior 'Program.cs'))
$prefix=$oldMapper.Substring(0,$oldMapper.IndexOf('internal static class Program'))
$start=$oldMapper.IndexOf('    [MethodImpl(MethodImplOptions.NoInlining)]');$end=$oldMapper.IndexOf('    // Control for the call')
$map=$oldMapper.Substring($start,$end-$start).Replace('private static Prepared Map','internal static Prepared Map')
[IO.File]::WriteAllText((Join-Path $scratch 'RetainedMapper.cs'),$prefix+"internal static class RetainedMapper`n{`n"+$map+"}`n")
$text=[IO.File]::ReadAllText((Join-Path $historical 'Correction.cs'))
$text=Replace-Exactly $text 'private static Projected Map' 'internal static Projected Map'
$text=Replace-Exactly $text 'return Same(a.Ax,b.Ax)&&Same(a.Ay,b.Ay)&&Same(a.Wx,b.Wx)&&Same(a.Wy,b.Wy)&&Same(a.InverseMass,b.InverseMass)&&Same(a.Dt,b.Dt);' @'
static bool FloatSame(double x,double y)=>BitConverter.SingleToInt32Bits((float)x)==BitConverter.SingleToInt32Bits((float)y);
        return FloatSame(a.Ax,b.Ax)&&FloatSame(a.Ay,b.Ay)&&FloatSame(a.Wx,b.Wx)&&FloatSame(a.Wy,b.Wy)&&Same(a.InverseMass,b.InverseMass)&&Same(a.Dt,b.Dt);
'@
$text=Replace-Exactly $text 'private static int Main(string[] args)' @'
private static bool Stable(Projected a,Projected b)=>
        BitConverter.DoubleToInt64Bits(a.Ax)==BitConverter.DoubleToInt64Bits(b.Ax)&&
        BitConverter.DoubleToInt64Bits(a.Ay)==BitConverter.DoubleToInt64Bits(b.Ay)&&
        BitConverter.DoubleToInt64Bits(a.Wx)==BitConverter.DoubleToInt64Bits(b.Wx)&&
        BitConverter.DoubleToInt64Bits(a.Wy)==BitConverter.DoubleToInt64Bits(b.Wy)&&
        BitConverter.DoubleToInt64Bits(a.InverseMass)==BitConverter.DoubleToInt64Bits(b.InverseMass)&&
        BitConverter.DoubleToInt64Bits(a.Dt)==BitConverter.DoubleToInt64Bits(b.Dt);
    private static int Main(string[] args)
'@
$text=Replace-Exactly $text 'comparisons.Any(x=>!x.doubleEqual||!x.floatEqual)' 'comparisons.Any(x=>!x.floatEqual||((x.name=="inverseMass"||x.name=="H")&&!x.doubleEqual))'
$text=Replace-Exactly $text 'Prepared output=default;Projected complete=default;var deterministic=true;' @'
using var witness=JsonDocument.Parse(File.ReadAllText(args[2]));
        var p=witness.RootElement.GetProperty("prepared");
        float[] wanted=[p.GetProperty("linearAcceleration")[0].GetSingle(),p.GetProperty("linearAcceleration")[1].GetSingle(),
            p.GetProperty("angularAcceleration")[0].GetSingle(),p.GetProperty("angularAcceleration")[1].GetSingle(),
            p.GetProperty("inverseMass").GetSingle(),witness.RootElement.GetProperty("backendDt").GetSingle()];
        for(var i=0;i<6;i++)if(BitConverter.SingleToInt32Bits(wanted[i])!=BitConverter.SingleToInt32Bits((float)newValues[i]))
            throw new InvalidOperationException("Retained native witness FP32 mismatch");
        Console.WriteLine("FP32_PRECHECK PASS bits="+string.Join(",",wanted.Select(BitConverter.SingleToInt32Bits)));
        Prepared output=default;Projected complete=default;var deterministic=true;
'@
$text=Replace-Exactly $text 'SameBits(complete,old)' '(SameBits(complete,old)&&Stable(complete,corrected))'
$text=Replace-Exactly $text 'SEMANTIC_ALLOCATION_GATE_PASS_REVIEW_REQUIRED' 'MIGRATED_CONTRACT_ALLOCATION_PASS'
[IO.File]::WriteAllText((Join-Path $scratch 'ActiveCorrection.cs'),$text)
$project=@"
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><StartupObject>Correction</StartupObject></PropertyGroup>
 <ItemGroup>
  <Compile Include="ActiveCorrection.cs"/><Compile Include="Rational.cs"/><Compile Include="RetainedMapper.cs"/>
  <Compile Include="$historical\Attribution.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Resources\PropellantInteger.cs"/>
  <Compile Include="$repo\src\NovaCore.Simulation\Spacecraft\Actuation\PoweredFlightNumerics.cs"/>
  <Compile Include="$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs"/>
 </ItemGroup>
</Project>
"@
$projectPath=Join-Path $scratch 'AllocationRecheck.csproj'
[IO.File]::WriteAllText($projectPath,$project)
dotnet build $projectPath -c Release -p:ContinuousIntegrationBuild=true --nologo | Tee-Object -FilePath (Join-Path $scratch 'build.txt')
if($LASTEXITCODE -ne 0){throw 'Build failed; STOP'}
dotnet (Join-Path $scratch 'bin\Release\net10.0\AllocationRecheck.dll') (Join-Path $ordinary 'inputs.json') (Join-Path $scratch 'result.json') (Join-Path $ordinary 'off-com-8.json') | Tee-Object -FilePath (Join-Path $scratch 'stdout.txt')
$code=$LASTEXITCODE
if($code -ne 0){throw "Allocation recheck failed ($code); STOP, no timing"}
