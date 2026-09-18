$ErrorActionPreference='Stop'
Set-Location -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')))
$output='build/active-vessel-camera-warp-correction'
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach($configuration in @('Debug','Release')) {
 $suffix=$configuration.ToLowerInvariant()
 # This correction changes no native source; reuse the paired prior qualified native output.
 dotnet build NovaCore.sln -c $configuration --artifacts-path "$output/candidate" "-p:NativeBuildDirectory=active-vessel-camera-free-correction/native-$suffix" --nologo -v:q *> "$output/build-$suffix.txt"
 if($LASTEXITCODE -ne 0){Get-Content "$output/build-$suffix.txt" -Tail 20;throw 'Candidate build failed'}
 Write-Output "Build $configuration PASS"
}
