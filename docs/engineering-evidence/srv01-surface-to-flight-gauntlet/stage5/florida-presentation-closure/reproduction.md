# Reproduction and cleanup

This is a stopped **ground-placement presentation candidate**, not a corrected pad-top manual route. Do not request renewed pad acceptance from this executable. The original physical site remains 48 m east on graded terrain.

## Source identity

Use the current unbanked campaign on baseline `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`. `entry-source-identities.json` and `source-identity-comparison.json` identify the captured original/current C# inputs. `presentation-source-seals.json` includes the nine changed source/test/native files. The predecessor ZIP uses unique filenames; its path mapping is `predecessor-seals.json`. The two additional predecessor text files preserve SampleOptions and the native header bytes. Do not restore them over the current candidate merely to reproduce historical evidence.

Required original terrain/BEPU/assets remain repository-owned inputs. No KSA/Blender material or downloaded replacement is involved.

## Build and focused checks

PowerShell 7, repository working directory, installed tools:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$env:VULKAN_SDK = 'C:\VulkanSDK\1.4.357.0'
foreach ($config in @('Debug','Release')) {
    $suffix = $config.ToLowerInvariant()
    cmake -S native/NovaCore.Native -B "build/srv01-stage5-florida-presentation/native-$suffix" -G Ninja "-DCMAKE_BUILD_TYPE=$config"
    if ($LASTEXITCODE -ne 0) { throw 'Native configuration failed' }
    cmake --build "build/srv01-stage5-florida-presentation/native-$suffix"
    if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
    dotnet build NovaCore.sln -c $config --artifacts-path build/srv01-stage5-florida-presentation/artifacts -p:ContinuousIntegrationBuild=true "-p:NativeBuildDirectory=srv01-stage5-florida-presentation/native-$suffix" --nologo -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Managed build failed' }
    dotnet "build/srv01-stage5-florida-presentation/artifacts/bin/NovaCore.Graphics.Tests/$suffix/NovaCore.Graphics.Tests.dll" --assembly-florida-presentation
    if ($LASTEXITCODE -ne 0) { throw 'Focused check failed' }
}
```

The focused test selects generation 4 explicitly, validates the admitted site/region, first/final transform and camera independence, all-1,200-history motion hash, exact stores/time/revisions, and separate zero-allocation/positive-control gates. It does not grant elevated-pad support.

The reproduction-only current window command is:

```powershell
& 'E:\NovaCore\build\srv01-stage5-florida-presentation\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe' '--scene=srv01-florida-support' '--log=renderer,vulkan'
```

It shows the existing Florida facility and the ground-placed craft, not the requested middle-pad-top placement. READY/Space remains the manual-start behavior. No manual acceptance is being requested until the physical admission decision is settled. The two historical characterization runs additionally supplied `--benchmark-frames=4800`, causing automatic start and close; do not confuse that with the manual route.

## Reviewed disposable cleanup

Exactly one new root, wholly within the repository's build directory: **1,708 files / 367,055,344 bytes**. All contents are rebuildable outputs, diagnostic logs or capture helpers. The capture and decisive results are retained in this evidence directory. No canonical source, assets or evidence are in the cleanup target. No deletion was attempted in this ticket.

Exact optional manual cleanup after Project Control no longer needs the prepared binaries:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\srv01-stage5-florida-presentation' -Recurse -Force -ErrorAction Stop
```

Non-destructive verification (expected False after cleanup):

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\srv01-stage5-florida-presentation'
```

Do not apply this command to any other campaign root. Historical Stage-5 output and unrelated work are intentionally outside its scope.
