# Stage verified build outputs at the existing launcher's normal executable locations.
# No alternative resolver, scenario implementation or source build is introduced.
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
$output=Join-Path $PWD 'build/srv01-stage5-simplified-slab/artifacts/bin'
foreach($entry in @(
    @{source='NovaCore.Triangle';destination='samples/NovaCore.Triangle/bin/Release/net10.0'},
    @{source='NovaCore.Launcher';destination='tools/NovaCore.Launcher/bin/Release/net10.0-windows'}
)){
    $source=Join-Path $output ($entry.source+'/release')
    if(!(Test-Path -LiteralPath "$source/$($entry.source).exe")){throw 'Build the uninstrumented Release solution first'}
    New-Item -ItemType Directory -Force -Path $entry.destination | Out-Null
    Copy-Item "$source/*" -Destination $entry.destination -Recurse -Force
    foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File){
        $relative=[IO.Path]::GetRelativePath($source,$file.FullName)
        if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $entry.destination $relative)).Hash){throw "Deployment identity mismatch: $relative"}
    }
}
Write-Output 'Launcher and sample deployment match qualified Release output.'
