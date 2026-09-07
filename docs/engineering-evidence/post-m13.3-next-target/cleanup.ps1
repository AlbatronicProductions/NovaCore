param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$scratchRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'build\post-m13.3-next-target'))
$bytecodeRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '__pycache__'))
if ($scratchRoot -ne (Join-Path $repoRoot 'build\post-m13.3-next-target')) { throw 'Unexpected scratch root' }
$manifestPath = Join-Path $PSScriptRoot 'cleanup-manifest.json'
$roots = @($scratchRoot, $bytecodeRoot)
foreach ($root in $roots) {
    if (-not $root.StartsWith($repoRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Outside workspace' }
    if (Test-Path -LiteralPath $root) {
        $item = Get-Item -LiteralPath $root
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse root' }
    }
}
$files = @()
$directories = @()
foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    $items = @(Get-ChildItem -LiteralPath $root -Force -Recurse)
    if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Reparse entry' }
    $directories += @($items | Where-Object PSIsContainer | ForEach-Object FullName)
    $directories += $root
    foreach ($item in ($items | Where-Object { -not $_.PSIsContainer })) {
        $absolute = [IO.Path]::GetFullPath($item.FullName)
        if (-not $absolute.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Escaped path' }
        $relative = [IO.Path]::GetRelativePath($repoRoot, $absolute).Replace('\','/')
        $tracked = @(git -C $repoRoot ls-files -- $relative)
        if ($tracked.Count) { throw "Tracked file: $relative" }
        $files += [pscustomobject][ordered]@{ path = $absolute; relative = $relative; bytes = $item.Length; sha256 = (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash.ToLowerInvariant(); hardLink = ($item.LinkType -eq 'HardLink') }
    }
}
if (-not $Apply) {
    [ordered]@{ roots = $roots; files = $files; directories = $directories; logicalBytes = ($files | Measure-Object bytes -Sum).Sum; fileCount = $files.Count; reason = 'Private host, bounded raw readbacks, private test copy, route probe, layer manifests and generated Python bytecode. Source/reproduction and important logs/hashes retained. Oracle hard link unlinked only; original production asset preserved.' } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    Write-Output "CLASSIFIED: $($files.Count) files; $(($files | Measure-Object bytes -Sum).Sum) logical bytes"
    exit 0
}
if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Missing reviewed manifest' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($files.Count -ne $manifest.files.Count) { throw 'File inventory changed' }
foreach ($file in $files) {
    $expected = @($manifest.files | Where-Object path -EQ $file.path)
    if ($expected.Count -ne 1 -or $expected[0].sha256 -ne $file.sha256 -or $expected[0].bytes -ne $file.bytes) { throw "Manifest mismatch: $($file.path)" }
}
$oracle = Join-Path $repoRoot 'assets\earth\runtime\earth_elevation_8192x4096.r16'
$oracleHash = (Get-FileHash -LiteralPath $oracle -Algorithm SHA256).Hash
# All paths, identities, tracked status and roots were checked before mutation.
foreach ($file in $files) { Remove-Item -LiteralPath $file.path -Force }
foreach ($directory in ($directories | Sort-Object Length -Descending)) {
    if (@(Get-ChildItem -LiteralPath $directory -Force).Count) { throw "Directory not empty: $directory" }
    Remove-Item -LiteralPath $directory
}
if ((Get-FileHash -LiteralPath $oracle -Algorithm SHA256).Hash -ne $oracleHash) { throw 'Production oracle changed' }
Write-Output "DISPOSED: $($files.Count) files; $(($files | Measure-Object bytes -Sum).Sum) logical bytes; original oracle unchanged"
