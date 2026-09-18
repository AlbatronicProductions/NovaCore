param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$suffix=$Configuration.ToLowerInvariant()
$output='build/player-flight-controls-gauntlet'
& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation | Out-Null
$env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
cmake -S native/NovaCore.Native -B "$output/native-$suffix" -G Ninja "-DCMAKE_BUILD_TYPE=$Configuration" *> "$output/cmake-$suffix.txt"
if($LASTEXITCODE -ne 0){throw 'CMake configure failed'}
cmake --build "$output/native-$suffix" *> "$output/native-$suffix.txt"
if($LASTEXITCODE -ne 0){throw 'Native build failed'}
dotnet build NovaCore.sln -c $Configuration --artifacts-path "$output/candidate" "-p:NativeBuildDirectory=player-flight-controls-gauntlet/native-$suffix" --nologo -v:q *> "$output/solution-$suffix.txt"
if($LASTEXITCODE -ne 0){Get-Content "$output/solution-$suffix.txt" -Tail 25;throw 'Solution build failed'}
Get-Content "$output/solution-$suffix.txt" -Tail 5
