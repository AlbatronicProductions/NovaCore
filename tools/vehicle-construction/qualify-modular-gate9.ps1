param([switch]$IncludePreservedRegressions)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $repoRoot
$results=[Collections.Generic.List[object]]::new()
try {
    function Run([string]$kind,[string[]]$arguments) {
        $watch=[Diagnostics.Stopwatch]::StartNew()
        $output=@(& dotnet @arguments 2>&1 | ForEach-Object {"$_"})
        $code=$LASTEXITCODE
        $results.Add(@{kind=$kind;arguments=$arguments;exitCode=$code;elapsedMs=$watch.Elapsed.TotalMilliseconds;output=$output})
        Write-Output "$kind $($arguments[-1]): exit $code"
        if($code -ne 0){throw ($output -join "`n")}
    }
    foreach($configuration in @('Debug','Release')) {
        $native='-p:NativeBuildDirectory=modular-craft-first-playable/native-'+$configuration.ToLowerInvariant()
        Run 'solution-build' @('build','NovaCore.sln','-c',$configuration,'--no-restore','-v:q',$native)
        Run 'editor-build' @('build','tools/NovaCore.ConstructionEditor','-c',$configuration,'--no-restore','-v:q',$native)
        $dll="tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
        foreach($flag in @('--modular-gate2','--modular-gate3','--modular-gate4','--modular-gate5-operations','--modular-gate6','--modular-gate7','--modular-gate8')) {
            Run $configuration @($dll,$flag)
        }
        Run $configuration @("tests/NovaCore.Graphics.Tests/bin/$configuration/net10.0/NovaCore.Graphics.Tests.dll",'--modular-gate9')
    }
    $graphics='tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll'
    foreach($flag in @('--florida-slab-correctness','--florida-slab-allocation','--florida-slab-camera-static','--florida-slab-camera-warp',
        '--florida-slab-camera-moving','--florida-slab-camera-costs','--player-engine','--player-attitude','--player-integrated')) {
        Run 'preserved-graphics' @($graphics,$flag)
    }
    if($IncludePreservedRegressions){& "$PSScriptRoot/qualify-modular-gate1.ps1" -OutputPath 'build/modular-craft-first-playable/gate9-regressions.json'}
} finally {
    [IO.File]::WriteAllText((Join-Path $repoRoot 'build/modular-craft-first-playable/gate9-qualification.json'),($results | ConvertTo-Json -Depth 7))
    Pop-Location
}
