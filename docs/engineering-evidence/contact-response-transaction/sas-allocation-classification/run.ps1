param([string]$Variant='candidate',[string]$Configuration='Release',[string]$Mode='isolated',[int]$Run=1,[switch]$Plain,[string]$FxVersion='', [string]$Phase='')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$dll=Join-Path $root "$Variant/tests/NovaCore.Simulation.Tests/bin/$Configuration/net10.0/NovaCore.Simulation.Tests.dll"
$psi=[Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
if($FxVersion){$psi.ArgumentList.Add('--fx-version');$psi.ArgumentList.Add($FxVersion)}
$psi.ArgumentList.Add($dll)
if($Mode -eq 'isolated'){$psi.ArgumentList.Add('--sas-only')}
if($Mode -eq 'selfcheck'){$psi.ArgumentList.Add('--observer-selfcheck')}
$psi.WorkingDirectory='E:\NovaCore';$psi.UseShellExecute=$false;$psi.CreateNoWindow=$true
$psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true
if(!$Plain){
 $psi.Environment['CORECLR_ENABLE_PROFILING']='1'
 $psi.Environment['CORECLR_PROFILER']='{61D15631-E78F-45D1-A00E-20FC6F1CED24}'
 $psi.Environment['CORECLR_PROFILER_PATH']=Join-Path $root 'observer/profiler.dll'
 $psi.Environment['SAS_ATTRIBUTION']='1'
}
$p=[Diagnostics.Process]::Start($psi)
$outTask=$p.StandardOutput.ReadToEndAsync();$errTask=$p.StandardError.ReadToEndAsync()
if(!$p.WaitForExit(60000)){$p.Kill();throw 'Bounded process timed out'}
$out=$outTask.GetAwaiter().GetResult()+$errTask.GetAwaiter().GetResult();$code=$p.ExitCode;$pidValue=$p.Id;$p.Dispose()
$name="$Variant-$Configuration-$Mode-$Run"+$(if($Plain){'-plain'}else{'-attribution'})
if($Phase){$name="$Phase-$name"}
[IO.File]::WriteAllText((Join-Path $root "$name.txt"),$out)
$sas=@($out -split "`n" | Where-Object {$_.StartsWith('SAS_RESULT ')})
$row=[pscustomobject]@{variant=$Variant;configuration=$Configuration;mode=$Mode;run=$Run;profiled=!$Plain;exitCode=$code;pid=$pidValue;observation=$(if($sas.Count -eq 1){$sas[0].Substring(11) | ConvertFrom-Json}else{$null});testSha256=(Get-FileHash -LiteralPath $dll).Hash;simulationSha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $dll) 'NovaCore.Simulation.dll')).Hash}
$row | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root "$name.json")
$row | ConvertTo-Json -Depth 5 -Compress
if($Mode -eq 'selfcheck' -or ($row.observation -and $row.observation.delta -ne 0) -or !$row.observation){$out}
