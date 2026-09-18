param([switch]$Resume)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')))
$output='build/player-flight-controls-gauntlet'
$results=[Collections.Generic.List[object]]::new()
if($Resume){foreach($r in (Get-Content "$output/stage4-results.json" -Raw | ConvertFrom-Json)){if($r.exit -eq 0){$results.Add($r)}}}
function Gate([string]$name,[string]$exe,[string[]]$arguments) {
    if($Resume -and @($results | Where-Object name -eq $name).Count -ne 0){return}
    & $exe @arguments *> "$output/$name.txt"
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{name=$name;exit=$code;executable=$exe;arguments=$arguments;log="$output/$name.txt"})
    $results | ConvertTo-Json -Depth 5 | Set-Content "$output/stage4-results.json"
    Write-Output "$name exit=$code"
    if($code -ne 0){Get-Content "$output/$name.txt" -Tail 12;throw "Gate failed: $name"}
}
foreach($configuration in @('debug','release')) {
    $bin="$output/candidate/bin"
    $sim="$bin/NovaCore.Simulation.Tests/$configuration/NovaCore.Simulation.Tests.exe"
    foreach($gate in @('pilot-allocation','pilot-demand','assembly-control','assembly-control-measure','assembly-production','srv01-integration','assembly-contact-cheap','assembly-powered-contact-cheap','assembly-departure-cheap','assembly-allocation')) {
        Gate "stage4-$gate-$configuration" $sim @("--$gate")
    }
    $graphics="$bin/NovaCore.Graphics.Tests/$configuration/NovaCore.Graphics.Tests.exe"
    foreach($gate in @('player-engine','assembly-presentation','florida-slab-camera-static','florida-slab-camera-warp','florida-slab-camera-moving','florida-slab-camera-costs','florida-slab-correctness','florida-slab-solar','florida-slab-allocation','assembly-florida-qualification')) {
        Gate "stage4-$gate-$configuration" $graphics @("--$gate")
    }
    Gate "stage4-layout-$configuration" $graphics @('--case=Transport layout')
    Gate "stage4-launcher-$configuration" "tests/NovaCore.Launcher.Tests/bin/$configuration/net10.0-windows/NovaCore.Launcher.Tests.exe" @()
    foreach($project in @('Simulation','ReferenceFrames','Precision','Camera','BepuDependency')) {
        Gate "stage4-$project-$configuration" "$bin/NovaCore.$project.Tests/$configuration/NovaCore.$project.Tests.exe" @()
    }
}
