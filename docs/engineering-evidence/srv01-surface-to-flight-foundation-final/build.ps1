param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$env:VULKAN_SDK='C:\VulkanSDK\1.4.357.0'
$suffix=$Configuration.ToLowerInvariant()
$output='build/srv01-foundation-final'
New-Item -ItemType Directory -Force -Path $output | Out-Null
cmake -S native/NovaCore.Native -B "$output/native-$suffix" -G Ninja "-DCMAKE_BUILD_TYPE=$Configuration" *> "$output/native-config-$suffix.txt"
if ($LASTEXITCODE -ne 0) { throw 'Native configuration failed' }
cmake --build "$output/native-$suffix" *> "$output/native-build-$suffix.txt"
if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
dotnet build NovaCore.sln -c $Configuration --artifacts-path "$output/artifacts" -p:ContinuousIntegrationBuild=true "-p:NativeBuildDirectory=srv01-foundation-final/native-$suffix" --nologo -v:q *> "$output/full-build-$suffix.txt"
if ($LASTEXITCODE -ne 0) { throw 'Full solution build failed' }
Get-Content "$output/full-build-$suffix.txt"
