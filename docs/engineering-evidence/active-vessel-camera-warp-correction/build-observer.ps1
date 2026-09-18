param([ValidateSet('entry','candidate')][string]$Version='candidate')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo "build/active-vessel-camera-warp-correction/observer-$Version"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$items=[Collections.Generic.List[string]]::new()
$entry=Get-Content (Join-Path $PSScriptRoot 'entry-identity.json') -Raw|ConvertFrom-Json
foreach($name in @('Program','SolarSystemScene','StockAssemblyDevelopmentScene')) {
 $path="samples/NovaCore.Triangle/$name.cs"
 $sourcePath=if($Version -eq 'entry'){Join-Path $repo "build/active-vessel-camera-warp-correction/entry-source/$path"}else{Join-Path $repo $path}
 if($Version -eq 'entry' -and (Get-FileHash $sourcePath).Hash -ne ($entry.source|Where-Object path -eq $path).sha256){throw 'Entry source seal mismatch'}
 $source=[IO.File]::ReadAllText($sourcePath)
 if($name -eq 'Program') {
  $source=$source.Replace('    s.Contact?.AdvanceLive(', '    CameraProbe.Begin(s,ref e->Input);'+"`n"+'    s.Contact?.AdvanceLive(')
  $source=$source.Replace('s.Assembly?.AdvanceLive(s.Assembly?.FloridaView?.SolarNavigation==true?s.AssemblyAutoStart:e->Input.PauseToggle!=0||s.AssemblyAutoStart);s.AssemblyAutoStart=false;', 'CameraProbe.ServiceAssembly(s);s.AssemblyAutoStart=false;')
  $source=$source.Replace('vesselSolar.RefreshActiveVessel(s.Camera,s.Assembly.PrepareFocusObservation());','{CameraProbe.BeginFocus();vesselSolar.RefreshActiveVessel(s.Camera,s.Assembly.PrepareFocusObservation());CameraProbe.EndFocus();}')
  $source=$source.Replace('if(s.BenchmarkFramesRemaining>0&&--s.BenchmarkFramesRemaining==0)', 'CameraProbe.End(s);if(s.BenchmarkFramesRemaining>0&&--s.BenchmarkFramesRemaining==0)')
  $source+="`n"+[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'camera-observer.cs.txt'))
  $source=$source.Replace('PROBE_VERSION',$Version)
 }
 [IO.File]::WriteAllText((Join-Path $output "$name.cs"),$source)
 $items.Add('<Compile Remove="$(MSBuildProjectDirectory)/'+$name+'.cs"/><Compile Include="'+$output+'/'+$name+'.cs"/>')
}
[IO.File]::WriteAllText((Join-Path $output 'observer.targets'),'<Project><ItemGroup>'+($items -join '')+'</ItemGroup></Project>')
$artifacts=if($Version -eq 'entry'){'build/active-vessel-camera-free-correction/candidate'}else{'build/active-vessel-camera-warp-correction/candidate'}
dotnet build samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Release --artifacts-path $artifacts -o "$output/runtime" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/observer.targets" -p:NativeBuildDirectory=active-vessel-camera-free-correction/native-release --nologo -v:q *> "$output/build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/build.txt" -Tail 20;throw 'Observer build failed'}
Get-ChildItem "$artifacts/bin/NovaCore.Triangle/release/NovaCore.*.dll"|Where-Object {$_.Name -notin @('NovaCore.Triangle.dll','NovaCore.Native.dll')}|Copy-Item -Destination "$output/runtime" -Force
Write-Output "Built isolated $Version observer; manual runtime untouched."
