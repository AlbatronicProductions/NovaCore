param([Parameter(Mandatory)][string]$Startup,[switch]$BeaconOnly)
$ErrorActionPreference='Stop'
$run=Get-Content -LiteralPath $Startup -Raw | ConvertFrom-Json
if($run.schema -eq 'NovaCore.CpuBeacon/1'){
    if(-not $BeaconOnly){throw 'Standalone beacon identity requires explicit -BeaconOnly'}
    if(-not $run.witness -or -not $run.probeId){throw 'Incomplete standalone beacon identity'}
    $run=[pscustomobject]@{schema='NovaCore.BlackoutWitness/1';witness=$run.witness;beacon=$run}
}elseif($run.schema -ne 'NovaCore.BlackoutWitness/1'){throw 'Unknown startup schema'}
$owned=@($run)
if($BeaconOnly){$owned=@()}
if($run.beacon){
    if($run.beacon.schema -ne 'NovaCore.CpuBeacon/1' -or $run.beacon.witness -ne $run.witness -or -not $run.beacon.probeId){throw 'Invalid independent beacon identity'}
    $owned+=@($run.beacon)
}elseif($BeaconOnly){throw 'No beacon identity in startup file'}
$handles=@()
try {
    foreach($item in $owned){
        if(-not $item.startUtcTicks){throw 'Missing process birth identity; refusing bare PID stop'}
        $p=Get-Process -Id $item.pid -ErrorAction SilentlyContinue
        if(-not $p){continue}
        # Open and retain a handle before comparing creation time; never reacquire by PID.
        $null=$p.Handle
        $handles+=@($p)
        if($p.StartTime.ToUniversalTime().Ticks.ToString() -ne [string]$item.startUtcTicks){throw "PID $($item.pid) was reused; refusing stop"}
    }
    foreach($p in $handles){if(-not $p.HasExited){$p.Kill();if(-not $p.WaitForExit(5000)){throw "Matching process $($p.Id) did not exit"}}}
} finally {foreach($p in $handles){$p.Dispose()}}
Write-Host 'Only matching witness process incarnations were stopped. Evidence was retained.'
