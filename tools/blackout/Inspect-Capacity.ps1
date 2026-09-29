# Read-only: exact deployed admission accounting, under its existing mutex.
# Never calls PrepareCapacity, Run, Notice, or any maintenance/publication API.
param([Parameter(Mandatory=$true)][string]$Diagnostics)
$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($Diagnostics))
$type=$assembly.GetType('NovaCore.Diagnostics.RuntimeRetention',$true)
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$constructor=$type.GetConstructor($flags,$null,[Type[]]@([Nullable[guid]]),$null)
$owner=$constructor.Invoke([object[]]@($null))
$method=$type.GetMethod('InspectAdmission',$flags)
if(-not $method){throw 'Deployed read-only admission method unavailable'}
$state=$method.Invoke($owner,@())
@{schema='NovaCore.ReadOnlyAdmission/1';root=(Join-Path $env:LOCALAPPDATA 'NovaCore\MinimumRecorder');utcTicks=[DateTime]::UtcNow.Ticks.ToString();diagnosticsSha256=(Get-FileHash -LiteralPath $Diagnostics -Algorithm SHA256).Hash;available=$state.Available;totalBytes=$state.TotalBytes.ToString();outstandingBytes=$state.OutstandingBytes.ToString();sessionReservationBytes=$state.SessionReservationBytes.ToString();headroomBytes=$state.HeadroomBytes.ToString();semantics='Read-only admission snapshot, not a reservation. No maintenance performed.'} | ConvertTo-Json -Compress
