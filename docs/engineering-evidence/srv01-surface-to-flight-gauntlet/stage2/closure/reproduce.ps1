param([ValidateSet('Build','Focused','Regression','Performance')][string]$Mode='Focused',
      [ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-stage2-closure'
$artifacts=Join-Path $scratch 'artifacts'
function Gate {if($LASTEXITCODE -ne 0){throw "Gate failed: $LASTEXITCODE"}}
Push-Location $repo
try {
 if($Mode -eq 'Build') {
  New-Item -ItemType Directory -Force -Path $scratch | Out-Null
  & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
  $env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
  foreach($c in 'Debug','Release') {
   $lower=$c.ToLowerInvariant()
   cmake -S native/NovaCore.Native -B (Join-Path $scratch "native-$lower") -G Ninja "-DCMAKE_BUILD_TYPE=$c"; Gate
   cmake --build (Join-Path $scratch "native-$lower"); Gate
   dotnet build NovaCore.sln -c $c --artifacts-path $artifacts "-p:NativeBuildDirectory=srv01-stage2-closure/native-$lower" -p:ContinuousIntegrationBuild=true --nologo -v:q; Gate
  }
  return
 }
 $lower=$Configuration.ToLowerInvariant()
 $sim=Join-Path $artifacts "bin/NovaCore.Simulation.Tests/$lower/NovaCore.Simulation.Tests.exe"
 $graphics=Join-Path $artifacts "bin/NovaCore.Graphics.Tests/$lower/NovaCore.Graphics.Tests.exe"
 if($Mode -eq 'Performance') {
  if($Configuration -ne 'Release'){throw 'Characterization requires Release'}
  foreach($i in 1..3){ & $sim --assembly-powered-contact-performance; Gate }
  return # Isolated results alone do not grant integrated acceptance.
 }
 if($Mode -eq 'Regression') {
  & $sim; Gate
  & $graphics --assembly-presentation; Gate
  & $graphics --certified-continuation-regression; Gate
  foreach($name in 'NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests'){
   & (Join-Path $artifacts "bin/$name/$lower/$name.exe"); Gate
  }
  # Existing launcher tests resolve five parents from AppContext.BaseDirectory.
  # Preserve that normal output depth without editing tests or repository paths.
  $launcher=Join-Path $scratch "launcher/$lower/net10.0"
  New-Item -ItemType Directory -Force -Path $launcher | Out-Null
  Get-ChildItem -LiteralPath (Join-Path $artifacts "bin/NovaCore.Launcher.Tests/$lower") | Copy-Item -Destination $launcher -Recurse -Force
  if([IO.Path]::GetFullPath((Join-Path $launcher '../../../../..')).TrimEnd('\') -ne $repo.TrimEnd('\')){throw 'Launcher root mismatch'}
  & (Join-Path $launcher 'NovaCore.Launcher.Tests.exe'); Gate
  return
 }
 foreach($g in 'cheap','failures','physical','schedules','allocation','storage'){& $sim "--assembly-powered-contact-$g"; Gate}
 & $graphics --assembly-powered-support; Gate
 & $graphics --assembly-powered-storage; Gate
} finally {Pop-Location}
