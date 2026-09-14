param(
    [switch]$FullSuites,
    [switch]$Cost
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Arguments -join ' ')" }
}
Push-Location -LiteralPath $repositoryRoot
try {
    foreach ($configuration in @('Debug','Release')) {
        $buildTarget = if ($FullSuites) { 'NovaCore.sln' } else { 'tests/NovaCore.Simulation.Tests' }
        Invoke-Dotnet -Arguments @('build',$buildTarget,'-c',$configuration,'--no-restore','-v','minimal')
        foreach ($gate in @('--finite-propellant-cheap','--finite-propellant-allocation','--finite-propellant-article',
            '--engine-preparation-cheap','--engine-preparation-allocation','--spacecraft-command-cheap')) {
            Invoke-Dotnet -Arguments @('run','--project','tests/NovaCore.Simulation.Tests','-c',$configuration,'--no-build','--',$gate)
        }
        if ($FullSuites) {
            Invoke-Dotnet -Arguments @('run','--project','tests/NovaCore.Simulation.Tests','-c',$configuration,'--no-build')
            foreach ($project in @('NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests')) {
                Invoke-Dotnet -Arguments @('run','--project',"tests/$project",'-c',$configuration,'--no-build')
            }
        }
    }
    # Optional ONE fresh report-only cost run. No timing inside allocation isolation.
    if ($Cost) {
        Invoke-Dotnet -Arguments @('run','--project','tests/NovaCore.Simulation.Tests','-c','Release','--no-build','--','--finite-propellant-cost')
    }
    & git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Git whitespace check failed' }
}
finally { Pop-Location }
