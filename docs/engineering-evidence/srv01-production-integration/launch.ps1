param([switch]$Benchmark)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$exe = Join-Path $repo 'build/srv01-integration/artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build the Release application using the reproduction instructions first.' }
$output = Join-Path $repo 'build/srv01-integration'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$arguments = @('--scene=stock-assembly','--log=renderer,vulkan')
if ($Benchmark) { $arguments += '--benchmark-frames=480' }
Push-Location -LiteralPath $repo
$previousWidth=$env:NOVACORE_WINDOW_CLIENT_WIDTH
$previousHeight=$env:NOVACORE_WINDOW_CLIENT_HEIGHT
try {
    $env:NOVACORE_WINDOW_CLIENT_WIDTH='1280';$env:NOVACORE_WINDOW_CLIENT_HEIGHT='720'
    & $exe @arguments 2>&1 | Tee-Object -FilePath (Join-Path $output "manual-$stamp.log")
}
finally { $env:NOVACORE_WINDOW_CLIENT_WIDTH=$previousWidth;$env:NOVACORE_WINDOW_CLIENT_HEIGHT=$previousHeight;Pop-Location }
if ($LASTEXITCODE -ne 0) { throw "Application exited with $LASTEXITCODE" }
