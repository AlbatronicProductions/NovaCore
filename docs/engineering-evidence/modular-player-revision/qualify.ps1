param([string]$Configuration = 'Both')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$output = Join-Path $repo 'build/modular-player-revision'
$configurations = if ($Configuration -eq 'Both') { @('Debug', 'Release') } elseif ($Configuration -in @('Debug','Release')) { @($Configuration) } else { throw 'Use Debug, Release or Both.' }
foreach ($config in $configurations) {
    $exe = Join-Path $repo "tools/NovaCore.App/bin/$config/net10.0-windows/NovaCore.exe"
    foreach ($tank in @('short','long')) {
        $name = 'app-' + $tank + '-' + $config.ToLowerInvariant() + '-final'
        $base = Join-Path $output $name
        $candidate = Start-Process -FilePath $exe -ArgumentList @('--qualify-editor',($base+'.json'),'--qualification-tank',('nc.tank.'+$tank+'-2')) -WorkingDirectory $repo -WindowStyle Hidden -RedirectStandardOutput ($base+'.log') -RedirectStandardError ($base+'.err') -PassThru
        $peakPrivate = 0L; $peakWorking = 0L; $peakHandles = 0; $samples = 0
        while (-not $candidate.HasExited) {
            $candidate.Refresh()
            if (-not $candidate.HasExited) {
                $peakPrivate = [Math]::Max($peakPrivate, $candidate.PrivateMemorySize64)
                $peakWorking = [Math]::Max($peakWorking, $candidate.PeakWorkingSet64)
                $peakHandles = [Math]::Max($peakHandles, $candidate.HandleCount)
                $samples++
            }
            Start-Sleep -Seconds 1
        }
        $candidate.WaitForExit()
        $report = [IO.File]::ReadAllText($base+'.json') | ConvertFrom-Json
        [ordered]@{ process=$candidate.Id; exitCode=$candidate.ExitCode; configuration=$config; tank=$tank; samples=$samples; sampleIntervalSeconds=1; peakPrivateBytesObserved=$peakPrivate; peakWorkingSetBytes=$peakWorking; peakHandlesObserved=$peakHandles; boundary='Whole process including startup, editor, live flight and handler qualification; no idle baseline subtraction'; executableSha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant() } | ConvertTo-Json | Set-Content -LiteralPath ($base+'.process.json') -Encoding utf8
        Write-Output ($name+' '+$report.judgment+' exit='+$candidate.ExitCode)
        if ($candidate.ExitCode -ne 0 -or $report.judgment -ne 'APPLICATION_INTEGRATION_PASS') { throw ('Qualification failed: '+$base+'.json') }
    }
}
