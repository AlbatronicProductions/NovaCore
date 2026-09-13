param([ValidateSet('centered','tilted')][string]$Case='centered')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$executable=Join-Path $repository 'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe'
if(!(Test-Path -LiteralPath $executable)){throw 'Build the Release solution and native library first.'}
$output=Join-Path $repository 'build/engineering-article-formal-live'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$start=[Diagnostics.ProcessStartInfo]::new($executable)
$start.WorkingDirectory=$repository
$start.UseShellExecute=$false
$start.ArgumentList.Add('--scene=article-'+$Case)
$start.ArgumentList.Add('--log=vulkan')
$start.Environment['NOVACORE_WINDOW_CLIENT_WIDTH']='1280'
$start.Environment['NOVACORE_WINDOW_CLIENT_HEIGHT']='720'
$start.Environment['NOVACORE_WINDOW_BORDERLESS']='0'
$start.Environment['NOVACORE_CONTACT_TAIL_PROBE']='1'
$start.RedirectStandardOutput=$true
$start.RedirectStandardError=$true
$process=[Diagnostics.Process]::Start($start)
$stdout=$process.StandardOutput.ReadToEndAsync()
$stderr=$process.StandardError.ReadToEndAsync()
Write-Host "Observe READY in the $Case ENGINEERING ARTICLE window; Space starts live timing. Close after COMPLETED and final-hold checks."
$process.WaitForExit()
[IO.File]::WriteAllText((Join-Path $output ($Case+'.stdout.txt')),$stdout.GetAwaiter().GetResult())
[IO.File]::WriteAllText((Join-Path $output ($Case+'.stderr.txt')),$stderr.GetAwaiter().GetResult())
exit $process.ExitCode
