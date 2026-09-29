param([switch]$Fixture,[switch]$Recover,[string]$Bind,[ValidateRange(0,1800)][int]$Duration=0)
$ErrorActionPreference='Stop'
if($Fixture -and $Recover){throw 'Choose Fixture or Recover, not both'}
if($Duration -ne 0 -and (-not $Fixture -or $Duration -lt 10)){throw 'Real/recovery sessions run until stopped. A finite duration is fixture-only (10..1800 seconds).'}
if(-not $Bind){
    $route=Get-NetRoute -DestinationPrefix '0.0.0.0/0' | Where-Object {$_.NextHop -ne '0.0.0.0'} | Sort-Object RouteMetric | Select-Object -First 1
    $address=Get-NetIPAddress -InterfaceIndex $route.InterfaceIndex -AddressFamily IPv4 | Where-Object {$_.IPAddress -notlike '169.254.*'} | Select-Object -First 1
    if(-not $address){throw 'No LAN IPv4 found. Supply -Bind with the PC LAN address'}
    $Bind=$address.IPAddress
}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$output=Join-Path $root ('build\blackout-post-m16\witness-v2-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
$arguments=@((Join-Path $PSScriptRoot 'witness.py'),'--output',$output,'--bind',$Bind,'--duration',"$Duration",'--bookmark-file',(Join-Path $root 'build\blackout-witness\bookmark.json'))
if($Fixture){$arguments+='--fixture'}
if($Recover){$arguments+='--recover'}
Write-Host 'Standing CPU-only witness server (no play-session time limit). Keep this terminal open. Ctrl+C stops its owned helpers.'
Write-Host "If this terminal dies, stop the orphan CPU responder using Stop-Witness.ps1 -Startup '$output\startup.json'."
& (Get-Command python -ErrorAction Stop).Source @arguments
exit $LASTEXITCODE
