param([ValidateSet('selector','tilted','regressions','rest')][string]$Case='selector')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
Push-Location -LiteralPath $repo
try {
    if ((git rev-parse HEAD) -ne $identity.baseline) { throw 'Baseline changed; review before reproduction.' }
    foreach ($item in @($identity.changed)+@($identity.protected)) {
        if ((Get-FileHash -LiteralPath $item.path -Algorithm SHA256).Hash -ne $item.sha256) {
            throw "Input changed: $($item.path)"
        }
    }
    $paths=@(git ls-files --cached --others --exclude-standard -- src tests external/bepu) | Sort-Object -Unique
    $rows=($paths | ForEach-Object { $_+' '+(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }) -join "`n"
    $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($rows)))
    if ($hash -ne $identity.current_tree_sha256) { throw 'Candidate input tree changed; review before reproduction.' }
    dotnet build tests/NovaCore.Simulation.Tests -c Debug --no-restore -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $route=@{selector='--compound-selector-only';tilted='--compound-coverage-tilted';regressions='--compound-coverage-regressions';rest='--compound-coverage-rest-witness'}[$Case]
    dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll $route
    if ($LASTEXITCODE -ne 0) { throw "Gate failed ($route). Selector stability failure is the retained blocker, not acceptance." }
} finally { Pop-Location }
