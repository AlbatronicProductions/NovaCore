# USER-RUN ONLY. Automatic deletion was rejected; this script was not executed.
$ErrorActionPreference = 'Stop'
$repository = 'E:\NovaCore'
$controlRoot = 'E:\NovaCore\build\m13.4-surfaceanchor-classification'
$profileRoot = 'E:\NovaCore\build\post-m13.3-next-target'
$baseline = Join-Path $controlRoot 'baseline'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'disposable-manifest.json') -Raw | ConvertFrom-Json
$actual = @()
foreach ($root in @($controlRoot,$profileRoot)) {
    if ((Resolve-Path -LiteralPath $root).Path -ne $root -or -not $root.StartsWith($repository+'\build\')) { throw 'Unexpected root' }
    if ((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse root' }
    $items = @(Get-ChildItem -LiteralPath $root -Force -Recurse)
    if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Reparse entry' }
    $actual += @($items | Where-Object { -not $_.PSIsContainer })
}
if ($actual.Count -ne $manifest.Count) { throw 'Inventory changed; reclassify first' }
foreach ($file in $actual) {
    $expected = @($manifest | Where-Object path -EQ $file.FullName)
    if ($expected.Count -ne 1 -or $expected[0].bytes -ne $file.Length) { throw "Manifest mismatch: $($file.FullName)" }
}
if ((git -C $baseline rev-parse HEAD) -ne '180eaf150ba5db6364e17dd48336690778f058f9') { throw 'Unexpected control revision' }
if (@(git -C $baseline status --short).Count) { throw 'Control checkout changed' }
$assetMap = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'comparison.json') -Raw | ConvertFrom-Json).assets
foreach ($property in $assetMap.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $repository $property.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $property.Value.sha256) { throw 'Production asset identity changed' }
}
# Git removes only this checked detached worktree and its own administrative entry.
# Existing production assets keep their original hard links outside this worktree.
git -C $repository worktree remove $baseline
if ($LASTEXITCODE -ne 0) { throw 'Worktree removal failed; stop without fallback' }
foreach ($root in @($controlRoot,$profileRoot)) {
    $items = @(Get-ChildItem -LiteralPath $root -Force -Recurse)
    foreach ($file in @($items | Where-Object { -not $_.PSIsContainer })) { Remove-Item -LiteralPath $file.FullName -Force }
    foreach ($directory in @($items | Where-Object PSIsContainer | Sort-Object { $_.FullName.Length } -Descending)) {
        if (@(Get-ChildItem -LiteralPath $directory.FullName -Force).Count) { throw 'Unexpected nonempty directory' }
        Remove-Item -LiteralPath $directory.FullName
    }
    if (@(Get-ChildItem -LiteralPath $root -Force).Count) { throw 'Unexpected nonempty root' }
    Remove-Item -LiteralPath $root
}
foreach ($property in $assetMap.PSObject.Properties) {
    if ((Get-FileHash -LiteralPath (Join-Path $repository $property.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $property.Value.sha256) { throw 'Production asset verification failed' }
}
git -C $repository diff --check
git -C $repository status --short
Write-Output 'Classified control/scratch removed; production assets preserved.'
