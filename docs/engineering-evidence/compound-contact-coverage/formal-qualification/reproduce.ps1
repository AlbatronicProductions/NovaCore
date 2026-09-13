param(
    [ValidateSet('identity','selector','allocation','storage','schedules','centered','tilted','mirrored','moving','performance','selector-cost','cold','simulation','presentation','build')]
    [string]$Gate='identity',
    [ValidateSet('Debug','Release')][string]$Configuration='Release'
)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
Set-Location -LiteralPath $repository
$identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
foreach($file in $identity.inputs) {
    if((Get-FileHash -LiteralPath (Join-Path $repository $file.path)).Hash -ne $file.sha256) {
        throw ('Qualification input changed: '+$file.path)
    }
}
Write-Host ('PASS qualification identity: '+$identity.input_count+' source/test/sample/dependency inputs')
if($Gate -eq 'identity'){exit 0}
if($Gate -eq 'build'){& dotnet build NovaCore.sln -c $Configuration --nologo;exit $LASTEXITCODE}
if($Gate -eq 'presentation'){
    & dotnet ('tests/NovaCore.Graphics.Tests/bin/'+$Configuration+'/net10.0/NovaCore.Graphics.Tests.dll') '--case=Contact development canonical presentation'
    exit $LASTEXITCODE
}
$arguments=@{
    selector='--compound-selector-only'; allocation='--engineering-article-allocation'
    storage='--engineering-article-storage'; schedules='--engineering-article-schedules'
    centered='--engineering-article-physical-centered'; tilted='--engineering-article-physical-tilted'
    mirrored='--engineering-article-physical-mirrored'; moving='--engineering-article-physical-moving'
    performance='--engineering-article-performance'; 'selector-cost'='--compound-selector-cost'
    cold='--engineering-article-cold'
}
$executable='tests/NovaCore.Simulation.Tests/bin/'+$Configuration+'/net10.0/NovaCore.Simulation.Tests.dll'
if($Gate -eq 'simulation'){& dotnet $executable}
else {& dotnet $executable $arguments[$Gate]}
exit $LASTEXITCODE
