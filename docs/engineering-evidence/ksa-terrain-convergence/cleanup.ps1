# Manual, ticket-specific cleanup. Automatic execution was rejected by policy.
# Run with -Preview to verify identities without deleting anything.
param([switch]$Preview)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath 'E:\NovaCore').Path
$taskScratch=(Resolve-Path -LiteralPath 'E:\NovaCore\build\ksa-terrain-convergence').Path
$taskLoose=(Resolve-Path -LiteralPath 'E:\NovaCore\build\ksa-convergence-build.log').Path
$taskOracle='E:\NovaCore\assets\earth\runtime\earth_elevation_8192x4096.r16'
$taskOracleHash='4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317'
if ($taskScratch -ne 'E:\NovaCore\build\ksa-terrain-convergence' -or
    $taskLoose -ne 'E:\NovaCore\build\ksa-convergence-build.log' -or
    -not $taskScratch.StartsWith($taskWorkspace+'\',[StringComparison]::OrdinalIgnoreCase)) {
    throw 'Unexpected deletion target'
}
$taskItems=@(Get-Item -LiteralPath $taskScratch)+@(Get-ChildItem -LiteralPath $taskScratch -Recurse -Force)
if ($taskItems | Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}) {
    throw 'Reparse point in scratch scope'
}
$taskFiles=@($taskItems | Where-Object {-not $_.PSIsContainer})+@(Get-Item -LiteralPath $taskLoose)
$taskManifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'scratch-retirement-manifest.json') -Raw | ConvertFrom-Json
if ($taskFiles.Count -ne $taskManifest.fileCount) {throw 'Scratch inventory changed'}
foreach ($taskRow in $taskManifest.files) {
    $taskPath=[IO.Path]::GetFullPath((Join-Path $taskWorkspace $taskRow.path))
    if (-not ($taskPath.StartsWith($taskScratch+'\',[StringComparison]::OrdinalIgnoreCase) -or $taskPath -eq $taskLoose)) {
        throw 'Manifest path outside exact scope'
    }
    $taskFile=Get-Item -LiteralPath $taskPath
    if ($taskFile.Length -ne $taskRow.bytes -or (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskRow.sha256) {
        throw ('Evidence identity changed: '+$taskPath)
    }
}
if ((Get-FileHash -LiteralPath $taskOracle -Algorithm SHA256).Hash -ne $taskOracleHash) {throw 'Production oracle identity changed'}
if ($Preview) {
    Write-Output ('Verified disposable files: '+$taskFiles.Count+'; logical bytes: '+$taskManifest.logicalBytes+'; reclaimable allocated bytes: '+$taskManifest.allocatedBytesReclaimable)
    return
}
Remove-Item -LiteralPath $taskScratch -Recurse -Force
Remove-Item -LiteralPath $taskLoose -Force
if ((Get-FileHash -LiteralPath $taskOracle -Algorithm SHA256).Hash -ne $taskOracleHash) {throw 'Production oracle identity changed after cleanup'}
Write-Output ('Removed '+$taskFiles.Count+' verified diagnostic files. Production oracle and normal deployment retained.')
