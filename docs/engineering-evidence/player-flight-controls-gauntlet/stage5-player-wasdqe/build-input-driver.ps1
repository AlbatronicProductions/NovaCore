$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
Set-Location -LiteralPath $repo
$output=Join-Path $repo 'build/player-flight-controls-gauntlet/native-input-driver'
New-Item -ItemType Directory -Force -Path $output | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'native-input-driver.cs.txt') -Destination "$output/Program.cs"
[IO.File]::WriteAllText("$output/NativeInputDriver.csproj",'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><PlatformTarget>x64</PlatformTarget></PropertyGroup></Project>')
dotnet build "$output/NativeInputDriver.csproj" -c Release --nologo -v:q
if($LASTEXITCODE -ne 0){throw 'Driver build failed'}
