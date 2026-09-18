$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo 'build/active-vessel-camera-free-correction/probe'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
$failingSolar=Join-Path $repo 'build/active-vessel-camera-free-correction/entry-source/samples/NovaCore.Triangle/SolarSystemScene.cs'
$seal=$entry.source|Where-Object {$_.path -eq 'samples/NovaCore.Triangle/SolarSystemScene.cs'}
if((Get-FileHash -LiteralPath $failingSolar).Hash -ne $seal.sha256){throw 'Failing Solar source seal mismatch'}
$program=[IO.File]::ReadAllText((Join-Path $repo 'tests/NovaCore.Graphics.Tests/Program.cs'))
$program=$program.Replace('if(args.Contains("--florida-slab-camera-static"','if(args.Contains("--florida-slab-home-probe",StringComparer.Ordinal)){HomeTransitionProbe.Run();return 0;}'+"`n"+'if(args.Contains("--florida-slab-camera-static"')
[IO.File]::WriteAllText((Join-Path $output 'Program.cs'),$program)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'home-probe.cs.txt') -Destination (Join-Path $output 'HomeTransitionProbe.cs')
$targets='<Project><ItemGroup><Compile Remove="$(MSBuildProjectDirectory)/Program.cs"/><Compile Remove="$(MSBuildProjectDirectory)/../../samples/NovaCore.Triangle/SolarSystemScene.cs"/><Compile Include="'+$failingSolar+'"/><Compile Include="'+$output+'/Program.cs"/><Compile Include="'+$output+'/HomeTransitionProbe.cs"/><Compile Include="'+$repo+'/samples/NovaCore.Triangle/DebugCameraInput.cs"/></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $output 'probe.targets'),$targets)
dotnet build tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj -c Release --artifacts-path build/active-vessel-camera/candidate -o "$output/runtime" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/probe.targets" -p:NativeBuildDirectory=active-vessel-camera/native-release --nologo -v:q *> "$output/build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/build.txt" -Tail 20;throw 'Probe build failed'}
Get-ChildItem 'build/active-vessel-camera/candidate/bin/NovaCore.Graphics.Tests/release/NovaCore.*.dll'|Where-Object {$_.Name -notin @('NovaCore.Graphics.Tests.dll','NovaCore.Native.dll')}|Copy-Item -Destination "$output/runtime" -Force
& "$output/runtime/NovaCore.Graphics.Tests.exe" --florida-slab-home-probe *> "$output/result.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/result.txt" -Tail 10;throw 'Probe failed'}
Get-Content "$output/result.txt" | Where-Object {$_.StartsWith('HOME_PROBE ')} | ForEach-Object {$_.Substring(11)|ConvertFrom-Json}|ConvertTo-Json -Depth 6 -Compress|Set-Content (Join-Path $PSScriptRoot 'home-failure.json')
Write-Output 'HOME transition reproduced; production source unchanged.'
