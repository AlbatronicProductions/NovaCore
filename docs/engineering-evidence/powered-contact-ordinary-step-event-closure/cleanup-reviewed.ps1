param([switch]$Delete)
$ErrorActionPreference='Stop'
$paths=@(
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\active-set-convergence\bin',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\active-set-convergence\obj',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\friction-boundary-continuation\bin',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\friction-boundary-continuation\obj',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\tiny-active-friction\bin',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\tiny-active-friction\obj',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\tiny-active-friction\__pycache__',
    'E:\NovaCore\tests\NovaCore.ContactNumerics.Probe',
    'E:\NovaCore\tests\NovaCore.ContactNumerics.CoupledProbe',
    'E:\NovaCore\tests\NovaCore.ContactNumerics.ConvergenceProbe',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\convergence',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\active-set-convergence\baseline-reproduction',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\d-tiny-coasts',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\basis-compatibility\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\coast-accuracy\payoff\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\coast-accuracy\reproduce-payoff.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\coast-accuracy\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\coast-admission\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\duration-history\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\duration-provenance\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\active-set-convergence\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\friction-boundary-continuation\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\paired-response\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\tiny-active-friction\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\final-qualification\twelve-sweep-qualification\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\preconditioning\reproduce.ps1',
    'E:\NovaCore\docs\engineering-evidence\powered-contact-numerical-prerequisite\reproduce.ps1'
)
$repo=[IO.Path]::GetFullPath('E:\NovaCore')+[IO.Path]::DirectorySeparatorChar
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'consolidation-manifest.json') -Raw|ConvertFrom-Json
foreach($path in $paths){
    $full=[IO.Path]::GetFullPath($path)
    if(-not $full.StartsWith($repo,[StringComparison]::OrdinalIgnoreCase)){throw "Out of reviewed workspace: $full"}
    if(-not(Test-Path -LiteralPath $full)){Write-Output "ABSENT: $full";continue}
    $item=Get-Item -LiteralPath $full -Force
    $items=@($item)
    if($item.PSIsContainer){$items+=@(Get-ChildItem -LiteralPath $full -Recurse -Force)}
    if(@($items|Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}).Count){throw "Reparse point requires review: $full"}
    $files=@($items|Where-Object {-not $_.PSIsContainer})
    $bytes=($files|Measure-Object -Property Length -Sum).Sum
    Write-Output "$full | files=$($files.Count) | bytes=$bytes"
    if($Delete){
        $record=@($manifest.actions|Where-Object {$_.path -eq $full})
        if($record.Count -ne 1 -or $record[0].files -ne $files.Count -or $record[0].bytes -ne $bytes){throw "Reviewed inventory changed: $full"}
        foreach($original in $record[0].originalFiles){
            if((Get-FileHash -LiteralPath (Join-Path $repo $original.path)).Hash -ne $original.sha256){throw "Reviewed content changed: $($original.path)"}
        }
        Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop
    }
}
