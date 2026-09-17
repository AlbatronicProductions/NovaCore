param([switch]$BuildOnly)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$out=Join-Path $repo 'build/srv01-bank-preparation'
New-Item -ItemType Directory -Force -Path $out | Out-Null
function Run([string]$name,[string]$exe,[string[]]$arguments) {
    & $exe @arguments *> (Join-Path $out "$name.log")
    if($LASTEXITCODE -ne 0){throw "$name failed; inspect $out/$name.log"}
    Write-Output "PASS $name"
}
Push-Location -LiteralPath $repo
try {
 foreach($config in @('Debug','Release')) {
  $lower=$config.ToLowerInvariant(); $native=Join-Path $out "native-$lower"
  $ninja='C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
  Run "configure-$lower" cmake @('-S','native/NovaCore.Native','-B',$native,'-G','Ninja',"-DCMAKE_BUILD_TYPE=$config","-DCMAKE_MAKE_PROGRAM=$ninja")
  Run "native-$lower" cmake @('--build',$native,'--target','NovaCore.Native','NovaCorePreparedSubmissionTests','NovaCoreExhaustPresentationTests')
  Run "solution-$lower" dotnet @('build','NovaCore.sln','-c',$config,'-p:Platform=Any CPU','--artifacts-path',"$out/artifacts","-p:NativeBuildDirectory=srv01-bank-preparation/native-$lower")
  if($BuildOnly){continue}
  Run "capacity-$lower" "$native/NovaCorePreparedSubmissionTests.exe" @()
  $oldLayers=$env:VK_IMPLICIT_LAYER_PATH
  try {
   $empty=Join-Path $out 'empty-implicit-layers'
   New-Item -ItemType Directory -Force -Path $empty | Out-Null
   if(Get-ChildItem -LiteralPath $empty -Force){throw 'Expected empty implicit-layer directory'}
   $env:VK_IMPLICIT_LAYER_PATH=$empty
   Run "exhaust-gpu-$lower" "$native/NovaCoreExhaustPresentationTests.exe" @("$native/shaders")
  } finally { $env:VK_IMPLICIT_LAYER_PATH=$oldLayers }
  $graphics="$out/artifacts/bin/NovaCore.Graphics.Tests/$lower/NovaCore.Graphics.Tests.exe"
  Run "presentation-$lower" $graphics @('--assembly-presentation')
  foreach($case in @('Transport layout','Powered free-flight canonical presentation','Powered contact canonical presentation','Contact development canonical presentation')) {
   Run "graphics-$lower-$($case.Replace(' ','-'))" $graphics @("--case=$case")
  }
  $sim="$out/artifacts/bin/NovaCore.Simulation.Tests/$lower/NovaCore.Simulation.Tests.exe"
  foreach($gate in @('srv01-integration','assembly-production','assembly-allocation','finite-propellant-cheap','engine-preparation-cheap','spacecraft-command-cheap','powered-free-flight-arithmetic','powered-free-flight-physics','powered-free-flight-authority','powered-free-flight-schedules','powered-free-flight-allocation','powered-contact-cheap','powered-contact-physics','powered-contact-work','powered-contact-authority','powered-contact-sequences','powered-contact-lifecycle','powered-contact-allocation')) {
   Run "$gate-$lower" $sim @("--$gate")
  }
 }
} finally { Pop-Location }
