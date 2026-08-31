param(
    [string] $Destination = ".novacore/cache/sources/usgs-3dep/USGS_13_n29w081_20221103.tif"
)

$ErrorActionPreference = "Stop"
$expectedHash = "532AB3A4ADE336D9A7D266E6745A12F043DB928BA8BF28A4576886DE421A74CD"
$sourceUrl = "https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/13/TIFF/historical/n29w081/USGS_13_n29w081_20221103.tif"
$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$destinationDirectory = [System.IO.Path]::GetDirectoryName($destinationPath)
[System.IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null

if (Test-Path -LiteralPath $destinationPath) {
    $existingHash = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
    if ($existingHash -eq $expectedHash) {
        Write-Host "Verified existing USGS 3DEP source: $destinationPath"
        exit 0
    }
    throw "Existing Florida source has unexpected SHA-256: $existingHash"
}

$incompletePath = "$destinationPath.incomplete-$([Guid]::NewGuid().ToString('N'))"
try {
    Invoke-WebRequest -Uri $sourceUrl -OutFile $incompletePath
    $actualHash = (Get-FileHash -LiteralPath $incompletePath -Algorithm SHA256).Hash
    if ($actualHash -ne $expectedHash) { throw "USGS 3DEP SHA-256 mismatch: $actualHash" }
    Move-Item -LiteralPath $incompletePath -Destination $destinationPath
    Write-Host "Acquired and verified USGS 3DEP source: $destinationPath"
}
finally {
    if (Test-Path -LiteralPath $incompletePath) { Remove-Item -LiteralPath $incompletePath -Force }
}
