param()
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-display-gap-closure'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$native=Join-Path $repo 'native/NovaCore.Native/NovaCoreNative.cpp'
$scene=Join-Path $repo 'samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs'
$nativeBytes=[IO.File]::ReadAllBytes($native);$sceneBytes=[IO.File]::ReadAllBytes($scene)
$nativeHash=(Get-FileHash $native).Hash;$sceneHash=(Get-FileHash $scene).Hash
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
 $header=(Join-Path $PSScriptRoot 'native-probe.h').Replace('\','/')
 $n=Replace-One $n "namespace {`n" "namespace {`n#include `"$header`"`n"
 $n=Replace-One $n 'LRESULT CALLBACK Proc(HWND h, UINT m, WPARAM w, LPARAM l) {' 'LRESULT CALLBACK Proc(HWND h, UINT m, WPARAM w, LPARAM l) { GapScope gapProc(23,24,m);'
 $n=Replace-One $n 'void Window(App &a) {' 'void Window(App &a) { GapScope gapWindow(29,30);'
 $n=Replace-One $n 'void Swap(App &a) {' 'void Swap(App &a) { GapScope gapSwap(27,28);'
 $n=Replace-One $n 'void Recreate(App &a) {' 'void Recreate(App &a) { GapScope gapRecreate(17,18);'
 $n=Replace-One $n "  vkDeviceWaitIdle(a.device);`n  for (auto s : a.renderFinished)" "  GapStamp(19);vkDeviceWaitIdle(a.device);GapStamp(20);`n  for (auto s : a.renderFinished)"
 $n=Replace-One $n '      cb(&e, cbData);' '      GapStamp(31,cat);cb(&e, cbData);GapStamp(32,cat);'
 $n=Replace-One $n '  a.Check(vkWaitForFences(a.device, 1, &a.fence, VK_TRUE, UINT64_MAX), "frame fence wait failed");' '  GapStamp(5,reinterpret_cast<uintptr_t>(a.fence));a.Check(vkWaitForFences(a.device, 1, &a.fence, VK_TRUE, UINT64_MAX), "frame fence wait failed");GapStamp(6,reinterpret_cast<uintptr_t>(a.fence));'
 $n=Replace-One $n '  a.cb(&e, a.cbData);' '  GapStamp(7);a.cb(&e, a.cbData);GapStamp(8);GapStamp(25);'
 $n=Replace-One $n '  const auto updateEnd=std::chrono::steady_clock::now();' '  GapStamp(26);const auto updateEnd=std::chrono::steady_clock::now();'
 $n=Replace-One $n '  VkResult ar = vkAcquireNextImageKHR' '  GapStamp(9);VkResult ar = vkAcquireNextImageKHR'
 $n=Replace-One $n '                                      a.imageAvailable, {}, &image);' '                                      a.imageAvailable, {}, &image);GapStamp(10,static_cast<unsigned>(ar));'
 $n=Replace-One $n 'Record(a, image);const auto recordEnd' 'GapStamp(11);Record(a, image);GapStamp(12);const auto recordEnd'
 $n=Replace-One $n 'a.Check(vkQueueSubmit(a.graphicsQueue, 1, &si, a.fence), "submit failed");a.submitSerial++' 'GapStamp(13,a.submitSerial);a.Check(vkQueueSubmit(a.graphicsQueue, 1, &si, a.fence), "submit failed");GapStamp(14,a.submitSerial);a.submitSerial++'
 $n=Replace-One $n 'VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);a.presentSerial++' 'GapStamp(15,a.presentSerial);VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);GapStamp(16,static_cast<unsigned>(pr));a.presentSerial++'
 # Both outer and nested pumps retain exact message identity and dispatch spans.
 $n=$n.Replace('      DispatchMessage(&m);','      GapStamp(21,m.message);DispatchMessage(&m);GapStamp(22,m.message);')
 $n=Replace-One $n "    while (run) {`n      MSG m;" "    while (run) {`n      gapFrame=static_cast<unsigned>(frames);GapStamp(1);`n      MSG m;"
 $n=Replace-One $n "      if (run) {`n        auto frameBegin" "      GapStamp(2);`n      if (run) {`n        GapStamp(3);auto frameBegin"
 $n=Replace-One $n '        frames++;' '        GapStamp(4);frames++;'
 $n=Replace-One $n "    vkDeviceWaitIdle(a.device);`n    char text[512];" "    vkDeviceWaitIdle(a.device);`n    GapDump();`n    char text[512];"
 [IO.File]::WriteAllText($native,$n,[Text.UTF8Encoding]::new($false))

 $s=[Text.Encoding]::UTF8.GetString($sceneBytes).Replace("`r`n","`n")
 $fragment=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'managed-probe.cs.txt') -Raw
 $s=Replace-One $s '    private readonly AssemblyApplicationSession session;' ($fragment+"`n    private readonly AssemblyApplicationSession session;")
 $s=Replace-One $s "        UpdateTitle();`n        if(!started)" "        var gapEntry=Stopwatch.GetTimestamp();gapOrdinal++;var gapG0=GC.CollectionCount(0);var gapG1=GC.CollectionCount(1);var gapG2=GC.CollectionCount(2);var gapPause=GC.GetTotalPauseDuration().Ticks;var gapAlloc=GC.GetAllocatedBytesForCurrentThread();`n        UpdateTitle();var gapTitle=Stopwatch.GetTimestamp();`n        if(!started)"
 $s=Replace-One $s '        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;' '        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;gapActive=gapRows[frameCount];gapActive.Entry=gapEntry;gapActive.TitleEnd=gapTitle;gapActive.Sample=now;gapActive.Elapsed=elapsed;gapActive.Ordinal=gapOrdinal;gapActive.G0=gapG0;gapActive.G1=gapG1;gapActive.G2=gapG2;gapActive.PauseBefore=gapPause;gapActive.AllocBefore=gapAlloc;gapActive.Before=observation.State.Frontier;gapActive.DebtBefore=observation.Clock.Debt.Ticks;'
 $s=Replace-One $s '        var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));' '        var serviceStart=Stopwatch.GetTimestamp();gapActive.ServiceBegin=serviceStart;gapActive.Credit=(long)ticks;Advance(new((long)ticks));gapActive.ServiceEnd=Stopwatch.GetTimestamp();'
 $s=Replace-One $s '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;' '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;gapActive.After=observation.State.Frontier;gapActive.DebtAfter=observation.Clock.Debt.Ticks;gapActive.Observation=observation;gapActive.AllocAfter=GC.GetAllocatedBytesForCurrentThread();gapActive.PauseAfter=GC.GetTotalPauseDuration().Ticks;gapActive.H0=GC.CollectionCount(0);gapActive.H1=GC.CollectionCount(1);gapActive.H2=GC.CollectionCount(2);gapActive.Exit=Stopwatch.GetTimestamp();'
 $s=Replace-One $s '        var service=supportedContact?' '        if(gapActive is not null)gapActive.PowerBegin=Stopwatch.GetTimestamp();var service=supportedContact?'
 $s=Replace-One $s '        if(service.Status is not(' '        if(gapActive is not null)gapActive.PowerEnd=Stopwatch.GetTimestamp();if(service.Status is not('
 $s=Replace-One $s '    public void Dispose(){session.Dispose();Visuals.Dispose();}' '    public void Dispose(){try{GapDump();}finally{session.Dispose();Visuals.Dispose();}}'
 [IO.File]::WriteAllText($scene,$s,[Text.UTF8Encoding]::new($false))
 [ordered]@{nativeOriginal=$nativeHash;sceneOriginal=$sceneHash;nativeProbe=(Get-FileHash $native).Hash;sceneProbe=(Get-FileHash $scene).Hash}|ConvertTo-Json|Set-Content (Join-Path $scratch 'probe-source-identity.json')
 & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation *> (Join-Path $scratch 'native-env.log')
 $env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
 cmake -S native/NovaCore.Native -B (Join-Path $scratch 'native-release') -G Ninja -DCMAKE_BUILD_TYPE=Release *> (Join-Path $scratch 'native-configure.log');Gate
 cmake --build (Join-Path $scratch 'native-release') *> (Join-Path $scratch 'native-build.log');Gate
 dotnet build samples/NovaCore.Triangle -c Release --artifacts-path (Join-Path $scratch 'artifacts') '-p:NativeBuildDirectory=srv01-display-gap-closure/native-release' -p:ContinuousIntegrationBuild=true --nologo -v:q *> (Join-Path $scratch 'managed-build.log');Gate
} finally {
 [IO.File]::WriteAllBytes($native,$nativeBytes);[IO.File]::WriteAllBytes($scene,$sceneBytes)
 Pop-Location
 if((Get-FileHash $native).Hash -ne $nativeHash -or (Get-FileHash $scene).Hash -ne $sceneHash){throw 'Diagnostic source restoration mismatch'}
 Write-Output 'Temporary source restored byte-for-byte.'
}
