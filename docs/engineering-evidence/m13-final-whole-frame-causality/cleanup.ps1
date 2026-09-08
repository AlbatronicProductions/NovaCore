$ErrorActionPreference = 'Stop'
$evidenceRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if ($evidenceRoot -ne 'E:\NovaCore\docs\engineering-evidence\m13-final-whole-frame-causality') { throw 'Evidence boundary' }
$manifest = Get-Content -LiteralPath (Join-Path $evidenceRoot 'disposable-manifest.json') -Raw | ConvertFrom-Json
$expectedRoots = @('E:\NovaCore\build\m13-final-whole-frame-causality','E:\NovaCore\build\regional-live-tests\c035332a73f94fed8db68c247b03628a')
foreach ($entry in $manifest.roots) {
    $targetRoot = [IO.Path]::GetFullPath($entry.root)
    if ($targetRoot -notin $expectedRoots) { throw 'Unexpected cleanup root' }
    $all = @(Get-ChildItem -LiteralPath $targetRoot -Force -Recurse)
    if ((Get-Item -LiteralPath $targetRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Root reparse point' }
    if ($all | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw 'Reparse point' }
    if (@($all | Where-Object { -not $_.PSIsContainer }).Count -ne $entry.files.Count) { throw 'File inventory changed' }
    foreach ($file in $entry.files) {
        $targetFile = [IO.Path]::GetFullPath((Join-Path $targetRoot $file.relative))
        if (-not $targetFile.StartsWith($targetRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'File escaped root' }
        if ((Get-Item -LiteralPath $targetFile).Length -ne $file.bytes) { throw 'File size changed' }
        if ((Get-FileHash -LiteralPath $targetFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw 'File contents changed' }
    }
}
$running = Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($expectedRoots[0] + '\',[StringComparison]::OrdinalIgnoreCase) }
if ($running) { throw 'Private runtime still running' }
foreach ($entry in $manifest.roots) {
    $targetRoot = [IO.Path]::GetFullPath($entry.root)
    foreach ($file in $entry.files) { Remove-Item -LiteralPath (Join-Path $targetRoot $file.relative) -Force }
    Get-ChildItem -LiteralPath $targetRoot -Directory -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
        if (Get-ChildItem -LiteralPath $_.FullName -Force) { throw 'Directory not empty' }
        Remove-Item -LiteralPath $_.FullName -Force
    }
    if (Get-ChildItem -LiteralPath $targetRoot -Force) { throw 'Root not empty' }
    Remove-Item -LiteralPath $targetRoot -Force
}
$protected = $manifest.protectedHardlink
if ((Get-FileHash -LiteralPath $protected.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $protected.sha256) { throw 'Protected production asset changed' }
Write-Output "Guarded cleanup PASS: $($manifest.count) files; production elevation asset preserved."
