param(
    [Parameter(Mandatory)][ValidateSet('OriginalWorld','SelectedPaired','SelectedTrace')][string]$Mode,
    [Parameter(Mandatory)][string]$OutputRoot
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$target=[IO.Path]::GetFullPath($OutputRoot)
$allowed=[IO.Path]::GetFullPath((Join-Path $repo 'build'))+[IO.Path]::DirectorySeparatorChar
if(-not $target.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Use a new bounded directory under repository build/.'}
if(Test-Path -LiteralPath $target){throw 'Existing output: no overwrite or retry.'}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'candidate-a-reproduction-inputs.json') -Raw|ConvertFrom-Json
foreach($row in $manifest.inputs){
    if((Get-FileHash -LiteralPath (Join-Path $repo $row.path)).Hash -ne $row.sha256){throw "Reproduction input changed: $($row.path)"}
}
Push-Location -LiteralPath $repo
try {
    if((git rev-parse HEAD) -ne $manifest.baseline){throw 'Baseline changed; review before reproducing historical numerical evidence.'}
    if(git diff --name-only HEAD -- src native tests/NovaCore.Simulation.Tests Directory.Build.props Directory.Build.targets external/bepu){throw 'Tracked dependency changes require review.'}
    if((dotnet --version) -ne '10.0.303'){throw 'SDK10.0.303 required.'}
    $versions=@(dotnet --list-runtimes|ForEach-Object {if($_ -match '^Microsoft.NETCore.App (10\.0\.\d+) '){[version]$Matches[1]}}|Sort-Object -Descending)
    if(!$versions.Count -or $versions[0].ToString() -ne '10.0.12'){throw 'Runtime10.0.12 required.'}
    $prior=Join-Path $repo 'docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/paired-response'
    if($Mode -eq 'OriginalWorld'){
        $project=Join-Path $repo 'tests/NovaCore.ContactNumerics.WorldProbe/NovaCore.ContactNumerics.WorldProbe.csproj'
        $assembly='NovaCore.ContactNumerics.WorldProbe'
    }else{
        $assembly=if($Mode -eq 'SelectedPaired'){'Reproduce'}else{'Trace'}
        $project=Join-Path $prior "$assembly.csproj"
    }
    $bin=Join-Path $target 'bin/';$obj=Join-Path $target 'obj/'
    dotnet build $project -c Debug --nologo "-p:BaseIntermediateOutputPath=$obj" "-p:OutputPath=$bin"
    if($LASTEXITCODE -ne 0){throw 'Build failure; stop.'}
    if($Mode -eq 'OriginalWorld'){
        $output=Join-Path $target 'kernel-debug.json'
        dotnet (Join-Path $bin "$assembly.dll") kernel $output
        if($LASTEXITCODE -ne 1){throw 'Original numerical failure did not reproduce.'}
        if((Get-FileHash -LiteralPath $output).Hash -ne 'C4F6EFA2C2F7F60F0DCDBFFAFF847AFD145FE275951F751AE3F9C2D94E526E22'){throw 'Original complete witness changed.'}
    }else{
        $folder=if($Mode -eq 'SelectedPaired'){'reproduction'}else{'diagnostics'}
        if($Mode -eq 'SelectedTrace'){
            # The unchanged trace compares against the frozen selected coast outputs.
            # These are copied input witnesses, not newly executed paired results.
            $frozen=Join-Path $target 'reproduction'
            [void](New-Item -ItemType Directory -Path $frozen)
            foreach($file in @('baseline-coast.json','powered-coast.json','paired-response.json')){
                Copy-Item -LiteralPath (Join-Path $prior "reproduction/$file") -Destination (Join-Path $frozen $file)
            }
        }
        dotnet (Join-Path $bin "$assembly.dll") $repo (Join-Path $target $folder)
        $expected=if($Mode -eq 'SelectedPaired'){1}else{0}
        if($LASTEXITCODE -ne $expected){throw 'Unexpected numerical result; stop.'}
        $files=if($Mode -eq 'SelectedPaired'){@('baseline-coast.json','powered-coast.json','paired-response.json')}else{
            # Process identity records are not deterministic numerical outputs.
            @('baseline-historical.json','powered-historical.json','stage-delta-inputs.json','identical-control.json','null-event-control.json','sweep-results.json','downstream-sensitivity.json','completion.json')
        }
        foreach($file in $files){
            $saved=Join-Path $prior "$folder/$file"
            if((Get-FileHash -LiteralPath (Join-Path $target "$folder/$file")).Hash -ne (Get-FileHash -LiteralPath $saved).Hash){throw "Complete saved witness changed: $file"}
        }
    }
}finally{Pop-Location}
Write-Output 'Historical failure/attribution reproduced. No qualification resumes; no production acceptance.'
$escaped=$target.Replace("'","''")
Write-Output "Verification: Get-ChildItem -LiteralPath '$escaped' -Recurse -File | Measure-Object -Property Length -Sum"
Write-Output "Reviewed future scratch cleanup: Remove-Item -LiteralPath '$escaped' -Recurse -Force"
