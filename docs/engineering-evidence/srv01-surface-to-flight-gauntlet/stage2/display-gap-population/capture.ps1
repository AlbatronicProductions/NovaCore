param([Parameter(Mandatory)][ValidateSet('A1','B1','C1','B2','C2','A2','C3','A3','B3')][string]$Id)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-display-gap-population'
$order=@('A1','B1','C1','B2','C2','A2','C3','A3','B3')
$position=[Array]::IndexOf($order,$Id)
if(Test-Path (Join-Path $scratch "$Id.process.json")){throw 'Capture already attempted; no replacement permitted'}
for($i=0;$i -lt $position;$i++){if(-not(Test-Path (Join-Path $scratch ($order[$i]+'.process.json')))){throw 'Fixed order violation'}}
$seal=Get-Content (Join-Path $PSScriptRoot 'preflight.json') -Raw|ConvertFrom-Json
foreach($s in $seal.sourceSeals){if((Get-FileHash (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw 'Candidate source not restored'}}
if((Get-FileHash (Join-Path $repo 'native/NovaCore.Native/NovaCoreNative.cpp')).Hash -ne $seal.nativeSha256){throw 'Native source not restored'}
$route=switch($Id[0]){'A'{'srv01-powered-support'}'B'{'srv01-supported-contact'}'C'{'stock-assembly'}}
$exe=Join-Path $scratch 'artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe'
$out=Join-Path $scratch "$Id.stdout.txt";$err=Join-Path $scratch "$Id.stderr.txt"
$record=[ordered]@{id=$Id;order=$position+1;route=$route;start=(Get-Date).ToString('o');arguments=@("--scene=$route",'--benchmark-frames=4000');executableSha256=(Get-FileHash $exe).Hash;interaction='none intentionally';freshProcess=$true}
$record|ConvertTo-Json|Set-Content (Join-Path $scratch "$Id.process.json")
$process=Start-Process -FilePath $exe -ArgumentList $record.arguments -WorkingDirectory $repo -PassThru -RedirectStandardOutput $out -RedirectStandardError $err
$record.pid=$process.Id
$record|ConvertTo-Json|Set-Content (Join-Path $scratch "$Id.process.json")
$finished=$process.WaitForExit(120000)
if(-not $finished){$record.timeout=$true;$record|ConvertTo-Json|Set-Content (Join-Path $scratch "$Id.process.json");throw 'Capture timed out; retain evidence, no replacement'}
$process.Refresh();$record.end=(Get-Date).ToString('o');$record.exitCode=$process.ExitCode
$record|ConvertTo-Json|Set-Content (Join-Path $scratch "$Id.process.json")
Write-Output "$Id finished PID=$($process.Id) exit=$($process.ExitCode)"
Get-Content $out|Where-Object{$_ -match 'STOCK_ASSEMBLY_END|STOCK_ASSEMBLY_FRAME|SRV01_LIVE_SERVICE|Frame pacing:|CPU timings:|GPU timing averages:|Average frame time:'}
if($process.ExitCode -ne 0){throw 'Capture failed; retain, no retry'}
