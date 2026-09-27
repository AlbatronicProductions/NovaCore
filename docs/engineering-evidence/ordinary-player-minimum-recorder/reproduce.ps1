param([string]$Repository='E:\NovaCore',[string]$Output='E:\NovaCore\build\ordinary-player-recorder-reproduction')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $Repository
New-Item -ItemType Directory -Force -Path $Output | Out-Null
function Invoke-Checked([string]$Exe,[string[]]$Arguments,[string]$Log) {
  & $Exe @Arguments 2>&1 | Tee-Object -FilePath $Log
  if($LASTEXITCODE -ne 0){throw "$Exe failed with $LASTEXITCODE"}
}
& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$cmake='C:/Program Files/CMake/bin/cmake.exe'
foreach($configuration in @('Debug','Release')){
  $native=if($configuration -eq 'Debug'){'build/native-ninja'}else{'build/native-ninja-release'}
  Invoke-Checked $cmake @('-S','native/NovaCore.Native','-B',$native,'-G','Ninja',"-DCMAKE_BUILD_TYPE=$configuration") "$Output/configure-$configuration.log"
  Invoke-Checked $cmake @('--build',$native,'--target','NovaCore.Native','NovaCoreMinimumRecorderMock','-j','4') "$Output/native-build-$configuration.log"
  Invoke-Checked 'dotnet' @('build','NovaCore.sln','-c',$configuration,'--nologo') "$Output/build-$configuration.log"
  $worker=(Resolve-Path "tools/NovaCore.Recorder/bin/$configuration/net10.0-windows/NovaCore.Recorder.exe").Path
  Invoke-Checked 'dotnet' @('run','--project','tests/NovaCore.MinimumRecorder.Tests/NovaCore.MinimumRecorder.Tests.csproj','-c',$configuration,'--no-build','--',"$Output/$configuration-tests",$worker) "$Output/tests-$configuration.log"
  # CPU-only native executable. This is NOT NovaCore.exe or a GPU launch.
  Invoke-Checked "$native/NovaCoreMinimumRecorderMock.exe" @("$Output/$configuration-native",$worker) "$Output/native-$configuration.log"
}
Invoke-Checked 'python' @('tools/verify-player-package.py','--output',"$Output/package.json") "$Output/package.log"
# For an existing session, with the player stopped:
# NovaCore.Recorder.exe recover <absolute-session-directory> <session-guid>
# Do not invoke the ordinary player, heavy capture, GPU probes or reset tests here.
