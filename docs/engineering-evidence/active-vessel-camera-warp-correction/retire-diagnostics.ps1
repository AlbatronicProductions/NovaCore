$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$root=[IO.Path]::GetFullPath((Join-Path $repo 'build/active-vessel-camera-warp-correction')).TrimEnd('\','/')
# Exact disposable copies only. Preserve candidate, source seals, logs and deployment.
$names=@('observer-entry/runtime','observer-candidate/runtime','probe-entry/runtime','probe-candidate/runtime','launcher')
$targets=@(foreach($name in $names){
 $path=[IO.Path]::GetFullPath((Join-Path $root $name))
 if(!$path.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Cleanup escaped workspace investigation'}
 $item=Get-Item -LiteralPath $path
 if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Reparse target refused'}
 if(@(Get-ChildItem -LiteralPath $path -Recurse -Force|Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}).Count){throw 'Reparse descendant refused'}
 [ordered]@{name=$name;path=$path;files=@(Get-ChildItem -LiteralPath $path -Recurse -File|ForEach-Object {[ordered]@{relative=$_.FullName.Substring($path.Length+1);bytes=$_.Length;sha256=(Get-FileHash $_.FullName).Hash}})}
})
foreach($process in @(Get-Process -Name NovaCore.Triangle,NovaCore.Graphics.Tests,NovaCore.Launcher.Tests -ErrorAction SilentlyContinue)){
 foreach($target in $targets){if($process.Path -and $process.Path.StartsWith($target.path+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Diagnostic runtime still in use'}}
}
$before=(Get-ChildItem -LiteralPath $root -Recurse -File|Measure-Object Length -Sum).Sum
$targets|ConvertTo-Json -Depth 8 -Compress|Set-Content -LiteralPath (Join-Path $root 'retired-file-manifest.json')
$manifestHash=(Get-FileHash (Join-Path $root 'retired-file-manifest.json')).Hash
$summary=@(foreach($target in $targets){[ordered]@{path=$target.path;files=$target.files.Count;bytes=($target.files|ForEach-Object {$_.bytes}|Measure-Object -Sum).Sum;runtimeBinaries=@($target.files|Where-Object {$_.relative -match '^NovaCore\.(Triangle|Graphics.Tests|Native)\.(dll|exe)$'})}})
foreach($target in $targets){Remove-Item -LiteralPath $target.path -Recurse -Force;if(Test-Path -LiteralPath $target.path){throw 'Retirement incomplete'}}
$retired=($summary|ForEach-Object {$_.bytes}|Measure-Object -Sum).Sum
[ordered]@{createdBytesAtClosure=$before;retiredBytes=$retired;targets=$summary;reparsePoints=0;activeConsumers=0;inventorySha256=$manifestHash;preserved=@('candidate Debug/Release exact qualified outputs for unbanked handoff','entry-source seals','raw logs and compact measurements','verified deployed Triangle/Launcher','all prior evidence/build packages');deploymentTouched=$false;policy='ENGINEERING_RULES.md diagnostic evidence lifecycle; resolved disposable runtime copies only'}|ConvertTo-Json -Depth 10 -Compress|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'retirement.json')
Write-Output "Retired $retired bytes of disposable runtimes; candidate and deployment preserved."
