param([ValidateSet('native-hold-observer','native-hold-observer-stage6')][string]$Variant='native-hold-observer')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
Set-Location -LiteralPath $repo
python (Join-Path $PSScriptRoot 'build-native-observer.py') $Variant
if($LASTEXITCODE -ne 0){throw 'Observer generation failed'}
$output=Join-Path $repo "build/player-flight-controls-gauntlet/$Variant"
& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation | Out-Null
$env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
cmake -S "$output/native-source" -B "$output/native-release" -G Ninja -DCMAKE_BUILD_TYPE=Release *> "$output/cmake.txt"
if($LASTEXITCODE -ne 0){throw 'Observer configure failed'}
cmake --build "$output/native-release" *> "$output/native-build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/native-build.txt" -Tail 12;throw 'Observer native build failed'}
dotnet build samples/NovaCore.Triangle/NovaCore.Triangle.csproj -c Release --artifacts-path build/player-flight-controls-gauntlet/candidate -o "$output/runtime" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/observer.targets" "-p:NativeBuildDirectory=player-flight-controls-gauntlet/$Variant/native-release" --nologo -v:q *> "$output/managed-build.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/managed-build.txt" -Tail 12;throw 'Observer managed build failed'}
Get-ChildItem build/player-flight-controls-gauntlet/candidate/bin/NovaCore.Triangle/release/NovaCore.*.dll | Where-Object {$_.Name -notin @('NovaCore.Triangle.dll','NovaCore.Native.dll')} | Copy-Item -Destination "$output/runtime" -Force
Get-Content "$output/managed-build.txt" -Tail 5
