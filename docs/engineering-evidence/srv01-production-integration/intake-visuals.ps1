param([string]$Repository='E:\NovaCore',[string]$VisualRoot='E:\NovaCore-Blender-Visual-StepB',[switch]$Resume)
$ErrorActionPreference='Stop'
$stage=Join-Path $Repository 'build\srv01-integration\visual-intake'
$blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$python='C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe'
$publicationPath=Join-Path $VisualRoot 'visual-evidence\SRV01\current-publication.json'
if((Get-FileHash -LiteralPath $publicationPath).Hash.ToLowerInvariant() -ne '32f930b93b79d4692561956ec6d66d6e70a790ea3f6ce1fcba2583a940f98e58'){throw 'Accepted publication changed'}
$publication=Get-Content -LiteralPath $publicationPath -Raw | ConvertFrom-Json
function SharedHash([string]$Path){
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try { return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() } finally { $stream.Dispose() }
}
foreach($entry in $publication.recipe.PSObject.Properties){if((Get-FileHash -LiteralPath (Join-Path $VisualRoot $entry.Name)).Hash.ToLowerInvariant() -ne $entry.Value){throw "Recipe changed: $($entry.Name)"}}
if((Test-Path -LiteralPath $stage) -and -not $Resume){throw 'Intake requires a fresh output directory or explicit verified resume'}
[IO.Directory]::CreateDirectory((Join-Path $stage 'sources')) | Out-Null
$rows=@()
foreach($asset in @('NC_SRV_Capsule_A','NC_SRV_Tank_A','NC_SRV_Main_A','NC_SRV_Rcs_A')){
    $source=Join-Path $VisualRoot "assets\visual\SRV01\parts\$asset.blend"
    $copy=Join-Path $stage "sources\$asset.blend"
    if(-not(Test-Path -LiteralPath $copy)){[IO.File]::Copy($source,$copy,$false)}
    $expected=$publication.sources.$asset
    if((Get-FileHash -LiteralPath $copy).Hash.ToLowerInvariant() -ne $expected){throw "Source mismatch: $asset"}
    $export=Join-Path $stage "exports\$asset"
    if(-not(Test-Path -LiteralPath (Join-Path $export 'manifest.json'))){
        & pwsh -NoProfile -File "$VisualRoot\tools\blender\visual\launch.ps1" -Task export -Candidate $copy -OutputDirectory $export -BlenderPath $blender
        if($LASTEXITCODE -ne 0){throw "Export failed: $asset"}
    }
    $glb=Join-Path $export 'visual.glb'
    & $python -B "$VisualRoot\tools\blender\srv_parts\verify_parts.py" $glb "$VisualRoot\tools\blender\srv_parts\visual_contract.json" (Join-Path $stage "independent\$asset.json") --asset $asset
    if($LASTEXITCODE -ne 0){throw "Independent verification failed: $asset"}
    $hash=(Get-FileHash -LiteralPath $glb).Hash.ToLowerInvariant()
    if($hash -ne $publication.glb_hashes.$asset){throw "Accepted GLB mismatch: $asset"}
    $manifest=Get-Content -LiteralPath (Join-Path $export 'manifest.json') -Raw | ConvertFrom-Json
    if($manifest.identity.source_sha256 -ne $expected -or -not $manifest.roundtrip_pass){throw "Export provenance mismatch: $asset"}
    if((SharedHash $source) -ne $expected -or (SharedHash $copy) -ne $expected){throw "Authoring source changed: $asset"}
    $rows += [pscustomobject]@{asset=$asset;sourceSha256=$expected;glbSha256=$hash;bytes=(Get-Item -LiteralPath $glb).Length;verified=$true}
}
$rows | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Repository 'docs\engineering-evidence\srv01-production-integration\visual-intake.json') -Encoding utf8
