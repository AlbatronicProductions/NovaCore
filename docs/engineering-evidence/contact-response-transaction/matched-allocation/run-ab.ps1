$ErrorActionPreference = 'Stop'
$comparisonRoot = $PSScriptRoot
$environmentText = (Get-ChildItem Env: | Sort-Object Name | ForEach-Object { $_.Name + '=' + $_.Value }) -join "`n"
$environmentHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($environmentText)))
$runtimeSettings = @(Get-ChildItem Env: | Where-Object Name -Match '^(DOTNET_|COMPlus_|CORECLR_)' | Sort-Object Name | Select-Object Name,Value)
[pscustomobject]@{ environmentSha256=$environmentHash; runtimeSettings=$runtimeSettings; dotnetPath=(Get-Command dotnet).Source; workingDirectory='E:\NovaCore'; plan='Debug then Release; baseline/candidate alternating; exactly 3 repetitions per cell; no profiler, no GC/tiering changes' } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $comparisonRoot 'environment.json') -Encoding utf8
$results = [Collections.Generic.List[object]]::new()
foreach ($configuration in @('Debug','Release')) {
    for ($run = 1; $run -le 3; $run++) {
        foreach ($variant in @('baseline','candidate')) {
            $dll = Join-Path $comparisonRoot "$variant/tests/NovaCore.Simulation.Tests/bin/$configuration/net10.0/NovaCore.Simulation.Tests.dll"
            $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
            $start.ArgumentList.Add($dll)
            $start.WorkingDirectory = 'E:\NovaCore'
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($start)
            $stdoutTask = $process.StandardOutput.ReadToEndAsync()
            $stderrTask = $process.StandardError.ReadToEndAsync()
            if (!$process.WaitForExit(60000)) { $process.Kill(); throw 'Bounded A/B process timeout; no repeats added.' }
            $stdout = $stdoutTask.GetAwaiter().GetResult()
            $stderr = $stderrTask.GetAwaiter().GetResult()
            $exitCode = $process.ExitCode
            $process.Dispose()
            [IO.File]::WriteAllText((Join-Path $comparisonRoot "$variant-$configuration-$run.txt"), $stdout + $stderr)
            $line = @($stdout -split "`n" | Where-Object { $_.StartsWith('AB_RESULT ') })
            if ($line.Count -ne 1) { throw "Missing unique A/B result: $variant $configuration $run" }
            $observation = $line[0].Substring(10) | ConvertFrom-Json
            $result = [pscustomobject]@{ variant=$variant; configuration=$configuration; run=$run; exitCode=$exitCode; environmentSha256=$environmentHash; testAssemblySha256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash; simulationAssemblySha256=(Get-FileHash -LiteralPath $observation.simulationAssembly -Algorithm SHA256).Hash; observation=$observation }
            $results.Add($result)
            $results.ToArray() | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $comparisonRoot 'results.json') -Encoding utf8
            $result | ConvertTo-Json -Depth 8 -Compress
        }
    }
}
if ($results.Count -ne 12) { throw 'Run plan incomplete' }
