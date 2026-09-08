$ErrorActionPreference='Stop'
$manifestPath=Join-Path $PSScriptRoot 'disposable-manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$workspace=[IO.Path]::GetFullPath('E:\NovaCore')
$expected=[IO.Path]::GetFullPath('E:\NovaCore\build\m13-regional-preparation-convergence')
$target=(Resolve-Path -LiteralPath $manifest.root).Path
if($target -ne $expected -or -not $target.StartsWith($workspace+'\')){throw 'Unexpected cleanup root'}
$entries=@(Get-ChildItem -LiteralPath $target -Recurse -Force)
if(@($entries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count){throw 'Reparse point in disposable host'}
$actual=@($entries | Where-Object { -not $_.PSIsContainer })
if($actual.Count -ne $manifest.files.Count){throw 'File inventory changed'}
$paths=@()
foreach($entry in $manifest.files){
    $file=[IO.Path]::GetFullPath((Join-Path $target $entry.relative))
    if(-not $file.StartsWith($target+'\')){throw 'Path escapes exact private host'}
    $item=Get-Item -LiteralPath $file
    if($item.Length -ne $entry.bytes -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256){throw "Changed disposable file: $file"}
    $paths+=$file
}
foreach($file in $paths){Remove-Item -LiteralPath $file -Force}
foreach($directory in ($entries | Where-Object PSIsContainer | Sort-Object { $_.FullName.Length } -Descending)){
    if(@(Get-ChildItem -LiteralPath $directory.FullName -Force).Count){throw 'Unexpected remaining directory content'}
    Remove-Item -LiteralPath $directory.FullName
}
Remove-Item -LiteralPath $target
if(Test-Path -LiteralPath $target){throw 'Private host still present'}
Write-Output "Removed $($paths.Count) classified private-host files; production deployment untouched."
