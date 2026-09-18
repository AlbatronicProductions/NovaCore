param([ValidateSet('entry','candidate')][string]$Version='entry')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location $repo
$output=Join-Path $repo "build/active-vessel-camera-warp-correction/probe-$Version"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$program=[IO.File]::ReadAllText((Join-Path $repo 'tests/NovaCore.Graphics.Tests/Program.cs'))
$program=$program.Replace('if(args.Contains("--florida-slab-camera-static"','if(args.Contains("--florida-slab-warp-probe",StringComparer.Ordinal)){WarpFrameProbe.Run();return 0;}'+"`n"+'if(args.Contains("--florida-slab-camera-static"')
[IO.File]::WriteAllText((Join-Path $output 'Program.cs'),$program)
Copy-Item (Join-Path $PSScriptRoot 'warp-probe.cs.txt') (Join-Path $output 'WarpFrameProbe.cs')
$items='<Compile Remove="$(MSBuildProjectDirectory)/Program.cs"/><Compile Include="'+$output+'/Program.cs"/><Compile Include="'+$output+'/WarpFrameProbe.cs"/>'
if($Version -eq 'entry'){
 $entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
 foreach($path in @('samples/NovaCore.Triangle/SolarSystemScene.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs','tests/NovaCore.Graphics.Tests/ActiveVesselCameraTests.cs')){
  $saved=Join-Path $repo ('build/active-vessel-camera-warp-correction/entry-source/'+$path)
  if((Get-FileHash $saved).Hash -ne ($entry.source|Where-Object {$_.path -eq $path}).sha256){throw 'Entry source seal mismatch'}
  $items+='<Compile Remove="'+(Join-Path $repo $path)+'"/><Compile Include="'+$saved+'"/>'
 }
}
[IO.File]::WriteAllText((Join-Path $output 'probe.targets'),'<Project><ItemGroup>'+$items+'</ItemGroup></Project>')
$artifacts=if($Version -eq 'entry'){'build/active-vessel-camera-free-correction/candidate'}else{'build/active-vessel-camera-warp-correction/candidate'}
dotnet build tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj -c Release --artifacts-path $artifacts -o "$output/runtime" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/probe.targets" -p:NativeBuildDirectory=active-vessel-camera-free-correction/native-release --nologo -v:q *> "$output/build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/build.txt" -Tail 20;throw 'Probe build failed'}
Get-ChildItem "$artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.*.dll"|Where-Object {$_.Name -notin @('NovaCore.Graphics.Tests.dll','NovaCore.Native.dll')}|Copy-Item -Destination "$output/runtime" -Force
& "$output/runtime/NovaCore.Graphics.Tests.exe" --florida-slab-warp-probe *> "$output/result.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/result.txt" -Tail 8;throw 'Probe failed'}
$lines=Get-Content "$output/result.txt"
[ordered]@{version=$Version;trace=@($lines|Where-Object {$_.StartsWith('WARP_TRACE ')}|ForEach-Object {$_.Substring(11)|ConvertFrom-Json});summary=@($lines|Where-Object {$_.StartsWith('WARP_SUMMARY ')}|ForEach-Object {$_.Substring(13)|ConvertFrom-Json})}|ConvertTo-Json -Depth 8 -Compress|Set-Content (Join-Path $PSScriptRoot "warp-$Version.json")
$lines|Where-Object {$_.StartsWith('WARP_SUMMARY ')}
