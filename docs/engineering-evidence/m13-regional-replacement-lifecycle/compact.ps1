$ErrorActionPreference='Stop'
$expected=[IO.Path]::GetFullPath('E:\NovaCore\docs\engineering-evidence\m13-regional-replacement-lifecycle')
$target=(Resolve-Path -LiteralPath $PSScriptRoot).Path
if($target -ne $expected){throw 'Unexpected evidence directory'}
$manifest=Get-Content -LiteralPath (Join-Path $target 'journal-manifest.json') -Raw | ConvertFrom-Json
if($manifest.files.Count -ne 3){throw 'Unexpected journal set'}
$paths=@()
foreach($entry in $manifest.files){
  $raw=[IO.Path]::GetFullPath((Join-Path $target $entry.original))
  $packed=[IO.Path]::GetFullPath((Join-Path $target $entry.compressed))
  if([IO.Path]::GetDirectoryName($raw) -ne $target -or [IO.Path]::GetDirectoryName($packed) -ne $target){throw 'Journal escapes target'}
  foreach($file in @($raw,$packed)){
    if((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Unexpected reparse point'}
  }
  if((Get-FileHash -LiteralPath $raw -Algorithm SHA256).Hash -ne $entry.originalSha256){throw 'Raw journal changed'}
  if((Get-FileHash -LiteralPath $packed -Algorithm SHA256).Hash -ne $entry.compressedSha256){throw 'Compressed journal changed'}
  if(-not $entry.losslessVerified){throw 'Lossless verification missing'}
  $paths+=$raw
}
foreach($file in $paths){Remove-Item -LiteralPath $file}
Write-Output 'Removed three verified uncompressed duplicates; all evidence retained losslessly.'
