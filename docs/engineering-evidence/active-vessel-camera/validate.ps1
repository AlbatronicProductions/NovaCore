$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
$output='build/active-vessel-camera'
$results=[Collections.Generic.List[object]]::new()
function Gate([string]$name,[string]$executable,[string[]]$arguments) {
    & $executable @arguments *> "$output/$name.txt"
    $code=$LASTEXITCODE
    $results.Add([pscustomobject]@{name=$name;exit=$code;executable=$executable;arguments=$arguments;log="$output/$name.txt"})
    $results | ConvertTo-Json -Depth 6 | Set-Content "$output/validation-results.json"
    Write-Output "$name exit=$code"
    if ($code -ne 0) {Get-Content "$output/$name.txt" -Tail 15;throw "Gate failed: $name"}
}
foreach($configuration in @('Debug','Release')) {
    $suffix=$configuration.ToLowerInvariant()
    $bin="$output/candidate/bin"
    $graphics="$bin/NovaCore.Graphics.Tests/$suffix/NovaCore.Graphics.Tests.exe"
    foreach($gate in @('florida-slab-camera-static','florida-slab-camera-moving','florida-slab-camera-costs','florida-slab-correctness','florida-slab-solar','florida-slab-allocation','assembly-florida-presentation','assembly-florida-presentation-storage','assembly-florida-qualification')) {
        Gate "$gate-$suffix" $graphics @("--$gate")
    }
    foreach($gate in @('assembly-contact-storage','assembly-powered-contact-storage','assembly-departure-storage','assembly-allocation','powered-contact-allocation')) {
        Gate "$gate-$suffix" "$bin/NovaCore.Simulation.Tests/$suffix/NovaCore.Simulation.Tests.exe" @("--$gate")
    }
    foreach($gate in @('assembly-presentation','assembly-powered-support','certified-continuation-regression','powered-contact-presentation')) {
        Gate "$gate-$suffix" $graphics @("--$gate")
    }
    foreach($project in @('Simulation','ReferenceFrames','Precision','Camera','BepuDependency')) {
        Gate "$project-$suffix" "$bin/NovaCore.$project.Tests/$suffix/NovaCore.$project.Tests.exe" @()
    }
    $cases=@('Transport layout','Focus target authority','Camera relative','Camera snapshot allocation','Planetary camera terrain exclusion','Anchored Florida launch site','Earth route convergence','Florida facility support','Surface-relative camera authority','Near-surface inertial free-look','SurfaceAnchor acquisition, ENU, and handoff','Camera focus-position continuity','Camera SurfaceAnchor handoff monotonicity','Solar preset camera-path convergence','Zoom motion-profile continuity','Solar camera bounded-domain crash regression','Surface visual-aim continuity','Inertial visual-aim authority','Terrain-v5 seams, mixed-LOD authority, and Florida classification','Production surface body eligibility and transition ownership','Camera drag isolation','Sol system presentation and focus','Florida foundation seating')
    $i=0
    foreach($case in $cases) {Gate "graphics-$suffix-$i" $graphics @("--case=$case");$i++}
    # Existing launcher tests locate the repository five parents above their output.
    $launcher="$output/launcher/$configuration/net10.0-windows"
    New-Item -ItemType Directory -Force -Path $launcher | Out-Null
    Copy-Item "$bin/NovaCore.Launcher.Tests/$suffix/*" -Destination $launcher -Recurse -Force
    Gate "Launcher-$suffix" "$launcher/NovaCore.Launcher.Tests.exe" @()
}
