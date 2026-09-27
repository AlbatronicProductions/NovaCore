# Export data from the canonical permanent fixture. This is not a player or test
# substitute: the resulting ordinary stock CraftDocument is loaded by NovaCore.exe.
$ErrorActionPreference='Stop'
Set-Location (Resolve-Path "$PSScriptRoot/../../..")
$fixtureRoot=Join-Path (Get-Location) 'build/rcs-canonicalization/fixture-export'
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' | Set-Content -LiteralPath (Join-Path $fixtureRoot 'FixtureExport.csproj')
@'
using System.Reflection;
using System.Runtime.Loader;
var root=Directory.GetCurrentDirectory();
var directory=Path.Combine(root,"tests/NovaCore.Simulation.Tests/bin/Release/net10.0");
AssemblyLoadContext.Default.Resolving+=(_,name)=>{var path=Path.Combine(directory,name.Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
var assembly=Assembly.LoadFrom(Path.Combine(directory,"NovaCore.Simulation.Tests.dll"));
var fixture=assembly.GetType("RcsScalabilityFixture")!;
const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
var catalog=fixture.GetMethod("Catalog",flags)!.Invoke(null,[false]);
var document=fixture.GetMethod("Document",flags)!.Invoke(null,[catalog,2,8,"nc.tank.short-2"])!;
var bytes=(byte[])document.GetType().GetMethod("Save",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(document,null)!;
var output=Path.Combine(root,"build/rcs-canonicalization/Canonical-RCS64.craft.json");
File.WriteAllBytes(output,bytes);Console.WriteLine(output);
'@ | Set-Content -LiteralPath (Join-Path $fixtureRoot 'Program.cs')
dotnet run --project (Join-Path $fixtureRoot 'FixtureExport.csproj') -c Release
if($LASTEXITCODE -ne 0){throw 'Canonical fixture export failed'}
