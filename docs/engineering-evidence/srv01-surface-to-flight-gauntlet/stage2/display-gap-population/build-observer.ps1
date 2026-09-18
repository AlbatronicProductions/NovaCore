param()
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-display-gap-population'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$native=Join-Path $repo 'native/NovaCore.Native/NovaCoreNative.cpp'
$scene=Join-Path $repo 'samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs'
$nativeBytes=[IO.File]::ReadAllBytes($native);$sceneBytes=[IO.File]::ReadAllBytes($scene)
$nativeHash=(Get-FileHash $native).Hash;$sceneHash=(Get-FileHash $scene).Hash
if($nativeHash -ne '4C468B0302603B59397A1D82C6E6E08577A2853F166FFEE7D2552107363C99B6' -or $sceneHash -ne 'BACBE961FBCFF1F9D550A643A937D41EB095CABC4D5D70338AAE05F8519A1A63'){throw 'Candidate identity differs; do not patch'}
[IO.File]::WriteAllBytes((Join-Path $scratch 'native-source-backup.cpp'),$nativeBytes)
[IO.File]::WriteAllBytes((Join-Path $scratch 'scene-source-backup.cs'),$sceneBytes)
function Replace-One([string]$text,[string]$needle,[string]$replacement){
 if(([regex]::Matches($text,[regex]::Escape($needle))).Count -ne 1){throw "Expected one marker: $needle"}
 return $text.Replace($needle,$replacement)
}
function Gate {if($LASTEXITCODE -ne 0){throw "Build gate failed $LASTEXITCODE"}}
Push-Location $repo
try {
 $n=[Text.Encoding]::UTF8.GetString($nativeBytes).Replace("`r`n","`n")
 $dump='    std::printf("POPULATION_NATIVE [");for(size_t i=0;i<a.frameTimeCount;i++)std::printf("%s%.9f",i?",":"",a.frameTimesMs[i]);std::printf("]\n");std::fflush(stdout);'
 $n=Replace-One $n "    vkDeviceWaitIdle(a.device);`n    char text[512];" ("    vkDeviceWaitIdle(a.device);`n"+$dump+"`n    char text[512];")
 [IO.File]::WriteAllText($native,$n,[Text.UTF8Encoding]::new($false))
 $s=[Text.Encoding]::UTF8.GetString($sceneBytes).Replace("`r`n","`n")
 $fragment=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'managed-observer.cs.txt') -Raw
 $s=Replace-One $s '    private readonly AssemblyApplicationSession session;' ($fragment+"`n    private readonly AssemblyApplicationSession session;")
 $s=Replace-One $s "        UpdateTitle();`n        if(!started)" "        UpdateTitle();`n        PopulationBegin();try{`n        if(!started)"
 $s=Replace-One $s '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;' '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;}finally{PopulationEnd();}'
 $s=Replace-One $s '    public void Dispose(){session.Dispose();Visuals.Dispose();}' '    public void Dispose(){try{PopulationDump();}finally{session.Dispose();Visuals.Dispose();}}'
 [IO.File]::WriteAllText($scene,$s,[Text.UTF8Encoding]::new($false))
 [ordered]@{nativeOriginal=$nativeHash;sceneOriginal=$sceneHash;nativeObserver=(Get-FileHash $native).Hash;sceneObserver=(Get-FileHash $scene).Hash;patchUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content (Join-Path $scratch 'observer-source-identity.json')
 & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation *> (Join-Path $scratch 'native-env.log')
 $env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
 cmake -S native/NovaCore.Native -B (Join-Path $scratch 'native-release') -G Ninja -DCMAKE_BUILD_TYPE=Release *> (Join-Path $scratch 'native-configure.log');Gate
 cmake --build (Join-Path $scratch 'native-release') *> (Join-Path $scratch 'native-build.log');Gate
 dotnet build samples/NovaCore.Triangle -c Release --artifacts-path (Join-Path $scratch 'artifacts') '-p:NativeBuildDirectory=srv01-display-gap-population/native-release' -p:ContinuousIntegrationBuild=true --nologo -v:q *> (Join-Path $scratch 'managed-build.log');Gate
} finally {
 [IO.File]::WriteAllBytes($native,$nativeBytes);[IO.File]::WriteAllBytes($scene,$sceneBytes)
 Pop-Location
 if((Get-FileHash $native).Hash -ne $nativeHash -or (Get-FileHash $scene).Hash -ne $sceneHash){throw 'Temporary source restoration mismatch'}
 Write-Output 'Temporary source restored byte-for-byte.'
}
