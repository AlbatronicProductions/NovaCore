param([ValidateSet('scratch','journals')][string]$Mode='scratch')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath('E:\NovaCore')
$evidence=[IO.Path]::GetFullPath((Join-Path $repo 'docs\engineering-evidence\m13-final-exit'))
if ($Mode -eq 'scratch') {
    $manifest=Get-Content -LiteralPath (Join-Path $evidence 'disposable-manifest.json') -Raw | ConvertFrom-Json
    $expected=[IO.Path]::GetFullPath((Join-Path $repo 'build\m13-final-exit'))
    if ($manifest.roots.Count -ne 1 -or [IO.Path]::GetFullPath($manifest.roots[0].root) -ne $expected) { throw 'Unexpected scratch root' }
    if ((Get-FileHash -LiteralPath $manifest.protectedHardlink.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.protectedHardlink.sha256) { throw 'Protected asset changed' }
    $actual=@(Get-ChildItem -LiteralPath $expected -Recurse -Force)
    if ((Get-Item -LiteralPath $expected -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse root' }
    foreach ($item in $actual) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse descendant' }
        if (-not [IO.Path]::GetFullPath($item.FullName).StartsWith($expected+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Outside scratch' }
    }
    if (@($actual | Where-Object { -not $_.PSIsContainer }).Count -ne $manifest.count) { throw 'Unclassified files' }
    $paths=@()
    foreach ($file in $manifest.roots[0].files) {
        $path=[IO.Path]::GetFullPath((Join-Path $expected $file.relative))
        if (-not $path.StartsWith($expected+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Outside target' }
        $item=Get-Item -LiteralPath $path -Force
        if ($item.Length -ne $file.bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw 'Scratch file changed' }
        $paths+=$path
    }
    foreach ($path in $paths) { Remove-Item -LiteralPath $path -Force }
    foreach ($dir in @($actual | Where-Object PSIsContainer | Sort-Object { $_.FullName.Length } -Descending)) {
        if (@(Get-ChildItem -LiteralPath $dir.FullName -Force).Count -ne 0) { throw 'Directory not empty' }
        Remove-Item -LiteralPath $dir.FullName -Force
    }
    Remove-Item -LiteralPath $expected -Force
    if ((Get-FileHash -LiteralPath $manifest.protectedHardlink.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.protectedHardlink.sha256) { throw 'Protected asset changed after cleanup' }
    Write-Output "Removed $($paths.Count) classified scratch files; protected production asset unchanged."
} else {
    $manifest=Get-Content -LiteralPath (Join-Path $evidence 'compressed-journals.json') -Raw | ConvertFrom-Json
    $paths=@()
    foreach ($file in $manifest.files) {
        $source=[IO.Path]::GetFullPath((Join-Path $evidence $file.source))
        $destination=[IO.Path]::GetFullPath((Join-Path $evidence $file.destination))
        if ([IO.Path]::GetDirectoryName($source) -ne $evidence -or [IO.Path]::GetDirectoryName($destination) -ne $evidence) { throw 'Outside evidence directory' }
        if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sourceSha256) { throw 'Journal changed' }
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.compressedSha256) { throw 'Compressed stream changed' }
        $paths+=$source
    }
    foreach ($path in $paths) { Remove-Item -LiteralPath $path -Force }
    Write-Output "Retired $($paths.Count) verified uncompressed journal duplicates."
}
