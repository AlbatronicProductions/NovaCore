param([ValidateSet('Identity','Focused','Full','Cost','Visible')][string]$Mode='Identity')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $repository
function Invoke-Checked([string[]]$CommandArgs) {
    & dotnet @CommandArgs
    if($LASTEXITCODE -ne 0){throw "First failure; stop without retry. dotnet $($CommandArgs -join ' ')"}
}
# Reproduction modes are recipes, not permission to resume a stopped qualification.
# Obtain Project Control's next scope before choosing Full or beginning failure attribution.
switch($Mode) {
    'Identity' {
        $identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
        foreach($entry in @($identity.candidate_files)+@($identity.architecture_files)) {
            $actual=(Get-FileHash -LiteralPath (Join-Path $repository $entry.path) -Algorithm SHA256).Hash
            if($actual -ne $entry.sha256){throw "Candidate byte mismatch: $($entry.path)"}
        }
        git status --short
        git rev-parse HEAD main origin/main
        git diff --check
        if($LASTEXITCODE -ne 0){throw 'Git whitespace check failed'}
        Write-Output 'IDENTITY PASS for recorded candidate and accepted architecture files'
    }
    'Focused' {
        foreach($configuration in 'Debug','Release') {
            $simulation="tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
            foreach($gate in 'physics','authority','allocation','schedules') {
                Invoke-Checked @($simulation,"--powered-free-flight-$gate")
            }
            Invoke-Checked @("tests/NovaCore.Graphics.Tests/bin/$configuration/net10.0/NovaCore.Graphics.Tests.dll",'--powered-free-flight-presentation')
        }
    }
    'Full' {
        foreach($configuration in 'Debug','Release') {
            Invoke-Checked @('build','NovaCore.sln','-c',$configuration,'--no-restore')
            Invoke-Checked @("tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll")
        }
    }
    'Cost' {
        if(Get-Process NovaCore.Triangle -ErrorAction SilentlyContinue){throw 'Close the visible sample before isolated timing.'}
        foreach($run in 1..3) {
            Write-Output "Fresh Release timing process $run of 3"
            Invoke-Checked @('tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll','--powered-free-flight-cost')
        }
    }
    'Visible' { & (Join-Path $PSScriptRoot 'launch.ps1') }
}
