$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
$output=Join-Path $repository 'build\powered-contact-final-qualification'
$executable=Join-Path $output 'artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe'
if(!(Test-Path -LiteralPath $executable)){throw 'Build the Release solution using the final qualification artifacts path first.'}
$start=[Diagnostics.ProcessStartInfo]::new($executable)
$start.WorkingDirectory=$repository
$start.UseShellExecute=$false
$start.ArgumentList.Add('--scene=powered-contact')
$start.Environment['NOVACORE_WINDOW_CLIENT_WIDTH']='1280'
$start.Environment['NOVACORE_WINDOW_CLIENT_HEIGHT']='720'
$start.Environment['NOVACORE_WINDOW_BORDERLESS']='0'
$start.RedirectStandardOutput=$true
$start.RedirectStandardError=$true
$process=[Diagnostics.Process]::Start($start)
$stdout=$process.StandardOutput.ReadToEndAsync()
$stderr=$process.StandardError.ReadToEndAsync()
Write-Host 'READY: Space once for POWERED HELD; inspect. Space again for EXHAUSTED HELD; inspect. Space a third time for live dry continuation. Close after COMPLETED to save logs.'
$process.WaitForExit()
[IO.File]::WriteAllText((Join-Path $output 'manual.stdout.txt'),$stdout.GetAwaiter().GetResult())
[IO.File]::WriteAllText((Join-Path $output 'manual.stderr.txt'),$stderr.GetAwaiter().GetResult())
exit $process.ExitCode
