param([ValidateSet('baseline','candidate')][string]$Version='candidate')
# Reproducible disposable compile overlay. Production source is never rewritten.
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo "build/active-vessel-camera/observer-$Version"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$items=[Collections.Generic.List[string]]::new()
foreach($name in @('Program','SolarSystemScene','StockAssemblyDevelopmentScene')) {
    $path="samples/NovaCore.Triangle/$name.cs"
    $source=if($Version -eq 'baseline'){(& git show "45b1bbcdd8d0e1d42f79f3bb5124e80753ce0e8f:$path") -join "`n"}else{[IO.File]::ReadAllText((Join-Path $repo $path))}
    if($name -eq 'Program') {
        $source=$source.Replace('    s.Contact?.AdvanceLive(', '    CameraProbe.Begin(s,ref e->Input);'+"`n"+'    s.Contact?.AdvanceLive(')
        $source=$source.Replace('s.Assembly?.AdvanceLive(s.Assembly?.FloridaView?.SolarNavigation==true?s.AssemblyAutoStart:e->Input.PauseToggle!=0||s.AssemblyAutoStart);s.AssemblyAutoStart=false;', 'CameraProbe.ServiceAssembly(s);s.AssemblyAutoStart=false;')
        $source=$source.Replace('if(s.BenchmarkFramesRemaining>0&&--s.BenchmarkFramesRemaining==0)', 'CameraProbe.End(s);if(s.BenchmarkFramesRemaining>0&&--s.BenchmarkFramesRemaining==0)')
        $source+="`n"+[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'camera-observer.cs.txt'))
        $source=$source.Replace('PROBE_VERSION', $Version)
    }
    [IO.File]::WriteAllText((Join-Path $output "$name.cs"),$source,[Text.UTF8Encoding]::new($false))
    $items.Add('<Compile Remove="$(MSBuildProjectDirectory)/'+$name+'.cs"/><Compile Include="'+$output+'/'+$name+'.cs"/>')
}
$define=if($Version -eq 'candidate'){'<PropertyGroup><DefineConstants>$(DefineConstants);CAMERA_CANDIDATE</DefineConstants></PropertyGroup>'}else{''}
[IO.File]::WriteAllText((Join-Path $output 'observer.targets'),'<Project>'+$define+'<ItemGroup>'+($items -join '')+'</ItemGroup></Project>')
$native=if($Version -eq 'baseline'){'native-baseline'}else{'native-release'}
dotnet build samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Release --artifacts-path "build/active-vessel-camera/$Version" -o "$output/runtime" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/observer.targets" "-p:NativeBuildDirectory=active-vessel-camera/$native" --nologo -v:q *> "$output/build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/build.txt" -Tail 30;throw 'Observer build failed'}
# BuildProjectReferences=false intentionally uses the already qualified dependency
# assemblies. MSBuild does not copy them to this alternate output automatically.
Get-ChildItem "build/active-vessel-camera/$Version/bin/NovaCore.Triangle/release/NovaCore.*.dll" |
    Where-Object {$_.Name -ne 'NovaCore.Triangle.dll' -and $_.Name -ne 'NovaCore.Native.dll'} |
    Copy-Item -Destination "$output/runtime" -Force
Write-Output "Built $Version observer. It is diagnostic, not the manual acceptance executable."
