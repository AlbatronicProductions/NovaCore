param([string]$OutputPath = 'build/modular-craft-first-playable/gate1-autonomous-results.json')
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
try {
    $results=[Collections.Generic.List[object]]::new()
    function Run-Qualification([string]$kind,[string[]]$arguments) {
        $watch=[Diagnostics.Stopwatch]::StartNew()
        $lines=@(& dotnet @arguments 2>&1 | ForEach-Object { "$_" })
        $code=$LASTEXITCODE
        $results.Add([ordered]@{kind=$kind;arguments=$arguments;exitCode=$code;elapsedMs=$watch.Elapsed.TotalMilliseconds;output=$lines})
        Write-Host "$kind $($arguments[-1]): exit $code"
        if($code -ne 0){throw "Qualification failed: $($arguments -join ' ')`n$($lines -join "`n")"}
    }
    foreach($configuration in @('Debug','Release')) {
        Run-Qualification 'managed-build' @('build','tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj','-c',$configuration,'--no-restore','-v','quiet')
    }
    $dll='tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll'
    foreach($flag in @('--modular-gate1','--modular-gate1-redteam','--modular-gate1-closure',
        '--modular-gate1-admission-audit','--modular-gate1-numerics','--modular-gate1-json',
        '--modular-gate1-aggregate','--modular-gate1-size','--modular-gate1-predicates',
        '--construction-stage1','--construction-stage2','--construction-stage3','--construction-stage4',
        '--construction-stage5','--construction-stage6','--construction-stage7','--construction-stage8-reuse',
        '--srv01-integration','--assembly-control','--pilot-demand','--pilot-allocation')) {
        Run-Qualification 'qualification' @($dll,$flag)
    }
} finally {
    $fullOutput=[IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($fullOutput)) | Out-Null
    [IO.File]::WriteAllText($fullOutput,($results | ConvertTo-Json -Depth 8))
    Pop-Location
}
