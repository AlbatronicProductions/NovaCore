param([string]$Repository = 'E:\NovaCore')
$ErrorActionPreference = 'Stop'
$Repository = (Resolve-Path -LiteralPath $Repository).Path.TrimEnd('\')
$evidence = Join-Path $Repository 'docs\engineering-evidence\release-package-owner'
$backup = Join-Path $Repository 'build\release-package-owner\before'
if (Test-Path -LiteralPath $backup) { throw 'Preservation directory already exists; do not repeat this step.' }
if (Get-Process NovaCore,NovaCore.ConstructionEditor,NovaCore.Triangle,cmake,ninja,dotnet -ErrorAction SilentlyContinue) { throw 'A potentially related process is running.' }
New-Item -ItemType Directory -Path $backup -Force | Out-Null
git -C $Repository status --porcelain=v1 | Set-Content (Join-Path $evidence 'status-before.txt')
git -C $Repository rev-parse HEAD | Set-Content (Join-Path $evidence 'head-before.txt')
git -C $Repository worktree list --porcelain | Set-Content (Join-Path $evidence 'worktrees-before.txt')
$source = @(git -C $Repository ls-files --cached --others --exclude-standard | Sort-Object -Unique | Where-Object { $_ -notlike 'docs/engineering-evidence/release-package-owner/*' })
$sourceManifest = @(foreach ($relative in $source) {
    $path = Join-Path $Repository $relative
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        [pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
    }
})
$sourceManifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $evidence 'source-before.json')
function Manifest([string]$Root) {
    @(Get-ChildItem -LiteralPath $Root -File -Recurse | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{path=[IO.Path]::GetRelativePath($Root,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
    })
}
Manifest (Join-Path $Repository 'tools\NovaCore.App\bin\Release\net10.0-windows') | ConvertTo-Json | Set-Content (Join-Path $evidence 'package-before.json')
Manifest (Join-Path $Repository 'build\earth-blackout-closure\native-adaptive-evidence\resume-20260925\candidate') | ConvertTo-Json | Set-Content (Join-Path $evidence 'qualified-candidate.json')
Manifest (Join-Path $Repository 'build\native-ninja-release') | ConvertTo-Json | Set-Content (Join-Path $evidence 'native-before.json')
$projects = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
function Visit([string]$Project) {
    $Project = [IO.Path]::GetFullPath($Project)
    if (!$projects.Add($Project)) { return }
    [xml]$xml = Get-Content -LiteralPath $Project -Raw
    foreach ($reference in $xml.Project.ItemGroup.ProjectReference) {
        if ($reference.Include) { Visit (Join-Path (Split-Path $Project) $reference.Include) }
    }
}
Visit (Join-Path $Repository 'tools\NovaCore.App\NovaCore.App.csproj')
$paths = @('build\native-ninja-release')
foreach ($project in ($projects | Sort-Object)) {
    $directory = Split-Path $project
    foreach ($suffix in @('bin\Release','obj\Release')) {
        $path = Join-Path $directory $suffix
        if (Test-Path -LiteralPath $path) { $paths += [IO.Path]::GetRelativePath($Repository,$path) }
    }
}
$moves = @(foreach ($relative in $paths) {
    $from = (Resolve-Path -LiteralPath (Join-Path $Repository $relative)).Path
    $to = [IO.Path]::GetFullPath((Join-Path $backup $relative))
    if (!$from.StartsWith($Repository+'\',[StringComparison]::OrdinalIgnoreCase) -or !$to.StartsWith($backup+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Path escaped its intended root.' }
    if ((Get-Item -LiteralPath $from).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing reparse point.' }
    [pscustomobject]@{from=$from;to=$to}
})
$moves | ConvertTo-Json | Set-Content (Join-Path $evidence 'preserved-output-paths.json')
foreach ($relative in @('samples\NovaCore.Triangle\NovaCore.Triangle.csproj','tools\NovaCore.App\NovaCore.App.csproj','tools\NovaCore.ConstructionEditor\NovaCore.ConstructionEditor.csproj')) {
    $to = Join-Path $backup $relative
    New-Item -ItemType Directory -Path (Split-Path $to) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $Repository $relative) -Destination $to
}
foreach ($move in $moves) {
    New-Item -ItemType Directory -Path (Split-Path $move.to) -Force | Out-Null
    Move-Item -LiteralPath $move.from -Destination $move.to
}
[pscustomobject]@{sourceFiles=$sourceManifest.Count;projects=$projects.Count;preservedOutputDirectories=$moves.Count;backup=$backup} | ConvertTo-Json
