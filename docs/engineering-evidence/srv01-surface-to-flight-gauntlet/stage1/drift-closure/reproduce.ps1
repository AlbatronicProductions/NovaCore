param([ValidateSet('Build','Focused','Full','Performance','Diagnostic')][string]$Mode='Focused',
      [ValidateSet('Debug','Release')][string]$Configuration='Debug',
      [ValidateSet('baseline','nearest-canonical','fixed-basis')][string]$Arm='baseline')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-drift-closure'
$artifacts=Join-Path $scratch 'qualification'
$lower=$Configuration.ToLowerInvariant()
function Gate { if($LASTEXITCODE -ne 0){throw "Command failed: $LASTEXITCODE"} }
Push-Location $repo
try {
 if($Mode -eq 'Build') {
  & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
  $env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
  foreach($c in 'Debug','Release') {
   $native=Join-Path $scratch ('native-'+$c.ToLowerInvariant())
   cmake -S native/NovaCore.Native -B $native -G Ninja "-DCMAKE_BUILD_TYPE=$c"; Gate
   cmake --build $native; Gate
   dotnet build NovaCore.sln -c $c --artifacts-path $artifacts "-p:NativeBuildDirectory=srv01-drift-closure/native-$($c.ToLowerInvariant())" -p:ContinuousIntegrationBuild=true --nologo -v:q; Gate
  }
  return
 }
 if($Mode -eq 'Diagnostic') {
  # Only isolated compiler inputs change. The current production checkout is read-only.
  $d=Join-Path $scratch ('reproduction-'+$Arm); New-Item -ItemType Directory -Force -Path $d | Out-Null
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'diagnostic-probe.cs.txt') -Destination (Join-Path $d 'Probe.cs')
  $reference=Join-Path $repo 'src/NovaCore.Simulation/NovaCore.Simulation.csproj'
  $project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>NovaCore.Simulation.Tests</AssemblyName><NovaCoreUsesBepu>true</NovaCoreUsesBepu><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><ProjectReference Include="'+$reference+'"/><Compile Include="Probe.cs"/></ItemGroup></Project>'
  [IO.File]::WriteAllText((Join-Path $d 'Probe.csproj'),$project)
  $options=@()
  if($Arm -ne 'fixed-basis') {
   $w=[IO.File]::ReadAllText((Join-Path $repo 'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs')).Replace("`r`n","`n")
   $a=[IO.File]::ReadAllText((Join-Path $repo 'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Assembly.cs')).Replace("`r`n","`n")
   if(!$w.Contains('AssemblyToNativeBody*profile.Mass.Inertia.Inverse()*AssemblyToNativeBody.Transpose()')){throw 'Source changed; review diagnostic reversal'}
   $w=$w.Replace('AssemblyToNativeBody*profile.Mass.Inertia.Inverse()*AssemblyToNativeBody.Transpose()','profile.Mass.Inertia.Inverse()')
   $w=$w.Replace('        if(configuration.AssemblyProfile is not null) q *= AssemblyToNativeRotation.Conjugate();'+"`n",'')
   $w=$w.Replace('        var bodyToRoot = configuration.LocalToRoot * new DoubleQuaternion(fq.X, fq.Y, fq.Z, fq.W);'+"`n",'')
   $w=$w.Replace('        if(configuration.AssemblyProfile is not null) bodyToRoot *= AssemblyToNativeRotation;'+"`n",'')
   $w=$w.Replace('TryCanonicalize(bodyToRoot, out var q)','TryCanonicalize(configuration.LocalToRoot * new DoubleQuaternion(fq.X, fq.Y, fq.Z, fq.W), out var q)')
   $a=[regex]::Replace($a,'(?s)    // Fixed coordinate convention.*?    private sealed class AssemblyBinding','    private sealed class AssemblyBinding')
   $a=$a.Replace('AssemblyRotation(AssemblyToNativeBody*child.AtOrigin.Rotation)','AssemblyRotation(child.AtOrigin.Rotation)').Replace('ToFloat(AssemblyToNativeBody.Apply(child.AtOrigin.Position-profile.Mass.Com))','ToFloat(child.AtOrigin.Position-profile.Mass.Com)')
   [IO.File]::WriteAllText((Join-Path $d 'World.cs'),$w); [IO.File]::WriteAllText((Join-Path $d 'Assembly.cs'),$a)
   $targets=@'
<Project><Target Name="DiagnosticBasis" BeforeTargets="CoreCompile" Condition="'$(MSBuildProjectName)'=='NovaCore.Simulation'"><ItemGroup><Compile Remove="@(Compile)" Condition="'%(Compile.Filename)%(Compile.Extension)'=='LocalContactWorld.cs' Or '%(Compile.Filename)%(Compile.Extension)'=='LocalContactWorld.Assembly.cs'"/><Compile Include="__ROOT__/World.cs;__ROOT__/Assembly.cs"/></ItemGroup></Target></Project>
'@
   $targetPath=Join-Path $d 'Diagnostic.targets'; [IO.File]::WriteAllText($targetPath,$targets.Replace('__ROOT__',$d.Replace('\','/')))
   $options=@("-p:CustomAfterMicrosoftCommonTargets=$targetPath")
  }
  dotnet build (Join-Path $d 'Probe.csproj') -c Debug --artifacts-path (Join-Path $d 'artifacts') @options --nologo -v:q; Gate
  $argument=if($Arm -eq 'nearest-canonical'){'nearest-canonical'}else{'unchanged'}
  & (Join-Path $d 'artifacts/bin/Probe/debug/NovaCore.Simulation.Tests.exe') $argument > (Join-Path $d 'result.json'); Gate
  return
 }
 $sim=Join-Path $artifacts "bin/NovaCore.Simulation.Tests/$lower/NovaCore.Simulation.Tests.exe"
 $graphics=Join-Path $artifacts "bin/NovaCore.Graphics.Tests/$lower/NovaCore.Graphics.Tests.exe"
 if($Mode -eq 'Full'){ & $sim; Gate; return }
 if($Mode -eq 'Performance') {
  # Three new processes. Stop at the first failed ceiling; no automatic retries.
  foreach($i in 1..3){& $sim --assembly-contact-performance; Gate}; return
 }
 foreach($flag in 'cheap','authority','failures','physical','schedules','allocation','storage') { & $sim "--assembly-contact-$flag"; Gate }
 & $graphics --assembly-presentation; Gate
 & $graphics --assembly-supported-storage; Gate
 & $graphics --certified-continuation-regression; Gate
} finally { Pop-Location }
