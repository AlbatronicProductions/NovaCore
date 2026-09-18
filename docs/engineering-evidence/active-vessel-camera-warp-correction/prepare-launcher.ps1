param([switch]$VerifyOnly)
# Deploy or verify the qualified, uninstrumented Release build at existing paths.
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo 'build/active-vessel-camera-warp-correction/candidate/bin'
$identities=[Collections.Generic.List[object]]::new()
foreach($entry in @(
    @{source='NovaCore.Triangle';destination='samples/NovaCore.Triangle/bin/Release/net10.0'},
    @{source='NovaCore.Launcher';destination='tools/NovaCore.Launcher/bin/Release/net10.0-windows'}
)){
    $source=[IO.Path]::GetFullPath((Join-Path $output ($entry.source+'/release'))).TrimEnd('\','/')
    if(!(Test-Path -LiteralPath "$source/$($entry.source).exe")){throw 'Build the uninstrumented Release solution first'}
    if(!$VerifyOnly){
        New-Item -ItemType Directory -Force -Path $entry.destination | Out-Null
        Copy-Item "$source/*" -Destination $entry.destination -Recurse -Force
    }
    foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File){
        # Windows PowerShell/.NET Framework has no Path.GetRelativePath.
        # Files came from this explicit root; verify containment before slicing.
        if(!$file.FullName.StartsWith($source+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Source containment mismatch'}
        $relative=$file.FullName.Substring($source.Length+1)
        $sha=(Get-FileHash -LiteralPath $file.FullName).Hash
        if($sha -ne (Get-FileHash -LiteralPath (Join-Path $entry.destination $relative)).Hash){throw "Deployment mismatch: $relative"}
        $identities.Add([pscustomobject]@{path="$($entry.destination)/$relative";sha256=$sha})
    }
}
$identities | ConvertTo-Json -Depth 4 | Set-Content 'build/active-vessel-camera-warp-correction/deployment.json'
Write-Output "Launcher and sample deployment match qualified Release output. VerifyOnly=$VerifyOnly files=$($identities.Count)"
