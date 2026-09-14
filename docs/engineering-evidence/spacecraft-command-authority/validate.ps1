param([string]$OutputPath = (Join-Path $PSScriptRoot 'validation.json'))
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$rows = [Collections.Generic.List[object]]::new()
Push-Location -LiteralPath $repo
try {
    function Invoke-Gate([string]$Name, [string[]]$Arguments) {
        Write-Host "RUN $Name"
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $output = @(& dotnet @Arguments 2>&1 | ForEach-Object { "$_" })
        $code = $LASTEXITCODE
        $timer.Stop()
        $witness = @($output | Where-Object {
            $_ -match '^COMMAND_|^ARTICLE_CONTACT |^ORDINARY_CONTROL |^ORDINARY_ALLOCATION gate=command|^PASS |Build succeeded|Warning\(s\)|Error\(s\)|BEPU|Bepu|SHA|sha256|identity|verified' })
        $row = [ordered]@{ gate=$Name; arguments=$Arguments; exit_code=$code; seconds=$timer.Elapsed.TotalSeconds; witness=$witness }
        if ($code -ne 0) { $row.failure=$output }
        $rows.Add($row)
        [ordered]@{ baseline='8c291881b323be3e3038d57d2ac180eff826f065'; sdk=(& dotnet --version); gates=$rows.ToArray() } |
            ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding utf8
        if ($code -ne 0) { $output | Write-Host; throw "$Name failed; bounded plan stops, no retries." }
        Write-Host "PASS $Name ($($timer.Elapsed.TotalSeconds.ToString('F2')) s)"
        $witness | Select-Object -Last 3 | Write-Host
    }
    # Predeclared: two builds, eight focused fresh processes, two full suites,
    # six dependency/reference/precision processes, one Release cost process. First failure stops.
    foreach ($configuration in @('Debug','Release')) {
        Invoke-Gate "build-$configuration" @('build','NovaCore.sln','-c',$configuration,'--nologo','-v:q')
    }
    foreach ($configuration in @('Debug','Release')) {
        $simulation = "tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
        foreach ($gate in @('cheap','schedules','article','allocation')) {
            Invoke-Gate "command-$gate-$configuration" @($simulation,"--spacecraft-command-$gate")
        }
    }
    foreach ($configuration in @('Debug','Release')) {
        Invoke-Gate "simulation-full-$configuration" @("tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll")
    }
    foreach ($configuration in @('Debug','Release')) {
        foreach ($project in @('NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests')) {
            Invoke-Gate "$project-$configuration" @("tests/$project/bin/$configuration/net10.0/$project.dll")
        }
    }
    Invoke-Gate 'command-cost-Release' @('tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll','--spacecraft-command-cost')
}
finally { Pop-Location }
