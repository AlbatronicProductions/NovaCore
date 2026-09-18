$ErrorActionPreference='Stop'
Set-Location -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')))
$output='build/player-flight-controls-gauntlet'
$results=[Collections.Generic.List[object]]::new()
function Gate([string]$name,[string]$exe,[string[]]$arguments) {
    & $exe @arguments *> "$output/$name.txt"
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{name=$name;exit=$code;executable=$exe;arguments=$arguments;log="$output/$name.txt"})
    $results | ConvertTo-Json -Depth 5 | Set-Content "$output/stage1-results.json"
    Write-Output "$name exit=$code"
    if($code -ne 0){Get-Content "$output/$name.txt" -Tail 12;throw "Gate failed: $name"}
}
foreach($configuration in @('debug','release')) {
    $bin="$output/candidate/bin"
    $sim="$bin/NovaCore.Simulation.Tests/$configuration/NovaCore.Simulation.Tests.exe"
    foreach($gate in @('assembly-control','assembly-control-measure','assembly-production','srv01-integration','assembly-contact-cheap','assembly-powered-contact-cheap','assembly-departure-cheap','assembly-allocation')) {
        Gate "stage1-$gate-$configuration" $sim @("--$gate")
    }
    $graphics="$bin/NovaCore.Graphics.Tests/$configuration/NovaCore.Graphics.Tests.exe"
    foreach($gate in @('florida-slab-camera-static','florida-slab-camera-warp','florida-slab-camera-moving','florida-slab-camera-costs','florida-slab-correctness','florida-slab-solar','florida-slab-allocation','assembly-florida-qualification')) {
        Gate "stage1-$gate-$configuration" $graphics @("--$gate")
    }
    foreach($project in @('Simulation','ReferenceFrames','Precision','Camera','BepuDependency')) {
        Gate "stage1-$project-$configuration" "$bin/NovaCore.$project.Tests/$configuration/NovaCore.$project.Tests.exe" @()
    }
}
