param()
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$scratch=Join-Path $repo 'build/srv01-startup-first-present'
New-Item -ItemType Directory -Force -Path $scratch|Out-Null
$relative=@('native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs','samples/NovaCore.Triangle/Program.cs')
$paths=@($relative|ForEach-Object{Join-Path $repo $_});$backups=@{};$hashes=@{}
foreach($p in $paths){$backups[$p]=[IO.File]::ReadAllBytes($p);$hashes[$p]=(Get-FileHash $p).Hash;[IO.File]::WriteAllBytes((Join-Path $scratch ([IO.Path]::GetFileName($p)+'.backup')),$backups[$p])}
$pre=Get-Content (Join-Path $PSScriptRoot 'preflight.json') -Raw|ConvertFrom-Json
foreach($s in $pre.sourceSeals){if((Get-FileHash (Join-Path $repo $s.path)).Hash -ne $s.sha256){throw 'Candidate mismatch'}}
if($hashes[$paths[0]] -ne $pre.nativeSha256){throw 'Native mismatch'}
function One([string]$t,[string]$n,[string]$r){if(([regex]::Matches($t,[regex]::Escape($n))).Count -ne 1){throw "Expected one marker: $n"};return $t.Replace($n,$r)}
function WriteUtf([string]$p,[string]$t){[IO.File]::WriteAllText($p,$t,[Text.UTF8Encoding]::new($false))}
function Gate {if($LASTEXITCODE -ne 0){throw "Build failed $LASTEXITCODE"}}
Push-Location $repo
try{
 $n=[Text.Encoding]::UTF8.GetString($backups[$paths[0]]).Replace("`r`n","`n")
 $header=(Join-Path $PSScriptRoot 'native-startup.h').Replace('\','/')
 $n=One $n "namespace {`n" "namespace {`n#include `"$header`"`n"
 $n=One $n '  ShowWindow(a.window, SW_SHOW);' '  StartupStamp(1);ShowWindow(a.window, SW_SHOW);StartupStamp(2);'
 # Window exists on return from CreateWindowExW, before raw-input registration/show.
 $n=One $n '  RAWINPUTDEVICE device{1, 2, RIDEV_INPUTSINK, a.window};' '  StartupStamp(3);RAWINPUTDEVICE device{1, 2, RIDEV_INPUTSINK, a.window};'
 $n=One $n '    Window(a);' '    StartupStamp(4);Window(a);'
 $n=One $n '    Instance(a);' '    Instance(a);StartupStamp(5);'
 $n=One $n '    Device(a);' '    Device(a);StartupStamp(6);'
 $n=One $n '    CreateVisualMeshes(a,meshes,meshCount);' '    CreateVisualMeshes(a,meshes,meshCount);StartupStamp(7);'
 $n=One $n '    Swap(a);' '    Swap(a);StartupStamp(8);'
 $n=One $n '    BootstrapProductionHierarchy(a);' '    BootstrapProductionHierarchy(a);StartupStamp(9);'
 $n=One $n '        auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;' '        startupFrame=static_cast<unsigned>(frames);StartupStamp(10);auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;'
 $n=One $n '        frames++;' '        StartupStamp(11);frames++;'
 $n=One $n '  const auto fenceEnd=std::chrono::steady_clock::now();' '  const auto fenceEnd=std::chrono::steady_clock::now();StartupStamp(12);'
 $n=One $n '  a.cb(&e, a.cbData);' '  if(startupPresented&&!startupPostPresentCallback){StartupStamp(13,a.submission->objectCount);startupPostPresentCallback=true;}StartupStamp(14,a.submission->objectCount);a.cb(&e, a.cbData);StartupStamp(15,a.submission->objectCount);'
 $n=One $n '                                      a.imageAvailable, {}, &image);' '                                      a.imageAvailable, {}, &image);StartupStamp(16,ar);'
 $n=One $n 'VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);a.presentSerial++;' 'StartupStamp(17);VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);StartupStamp(18,pr);if(pr==VK_SUCCESS)startupPresented=true;a.presentSerial++;'
 $n=One $n 'void Recreate(App &a) {' 'void Recreate(App &a) { StartupStamp(19);'
 $n=One $n "    vkDeviceWaitIdle(a.device);`n    char text[512];" "    vkDeviceWaitIdle(a.device);`n    StartupDump();std::printf(`"STARTUP_FRAMES [`");for(size_t i=0;i<a.frameTimeCount;i++)std::printf(`"%s%.9f`",i?`",`":`"`",a.frameTimesMs[i]);std::printf(`"]\n`");std::fflush(stdout);`n    char text[512];"
 WriteUtf $paths[0] $n
 $s=[Text.Encoding]::UTF8.GetString($backups[$paths[1]]).Replace("`r`n","`n")
 $s=One $s '    private readonly AssemblyApplicationSession session;' ((Get-Content (Join-Path $PSScriptRoot 'managed-startup.cs.txt') -Raw)+"`n    private readonly AssemblyApplicationSession session;")
 $s=One $s 'internal void Start(){if(started||Failed)return;started=true;timestamp=Stopwatch.GetTimestamp();}' 'internal void Start(){if(started||Failed)return;started=true;timestamp=Stopwatch.GetTimestamp();if(!startupReplay)startupRows[startupCount].Start=timestamp;}'
 $s=One $s "        UpdateTitle();`n        if(!started)" "        StartupEnter();UpdateTitle();startupRows[startupCount].AfterTitle=Stopwatch.GetTimestamp();try{`n        if(!started)"
 $s=One $s '        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;' '        var now=Stopwatch.GetTimestamp();var elapsed=now-timestamp;timestamp=now;startupRows[startupCount].Sample=now;startupRows[startupCount].Elapsed=elapsed;'
 $s=One $s '        var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));' '        startupRows[startupCount].Credit=(long)ticks;var serviceStart=Stopwatch.GetTimestamp();Advance(new((long)ticks));'
 $s=One $s '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;' '        serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;}finally{StartupExit();}'
 $s=One $s '        var service=supportedContact?' '        if(!startupReplay)startupRows[startupCount].ServiceIn=Stopwatch.GetTimestamp();var service=supportedContact?'
 $s=One $s '        if(service.Status is not(' '        if(!startupReplay)startupRows[startupCount].ServiceOut=Stopwatch.GetTimestamp();if(service.Status is not('
 $s=One $s '        Observe();Completed=service.Status==AssemblyFlightStatus.Completed;' '        Observe();if(!startupReplay)startupRows[startupCount].Observed=Stopwatch.GetTimestamp();Completed=service.Status==AssemblyFlightStatus.Completed;'
 $s=One $s '    public void Dispose(){session.Dispose();Visuals.Dispose();}' '    public void Dispose(){try{StartupDump();}finally{session.Dispose();Visuals.Dispose();}}'
 WriteUtf $paths[1] $s
 $program=[Text.Encoding]::UTF8.GetString($backups[$paths[2]])
 $program=One $program 'if(args.Contains("--scene=m12d-spherical-billboard-gpu-proof",StringComparer.OrdinalIgnoreCase))' 'StockAssemblyDevelopmentScene.StartupManagedEntry=Stopwatch.GetTimestamp();if(args.Contains("--scene=m12d-spherical-billboard-gpu-proof",StringComparer.OrdinalIgnoreCase))'
 WriteUtf $paths[2] $program
 @($paths|ForEach-Object{[ordered]@{path=[IO.Path]::GetRelativePath($repo,$_);original=$hashes[$_];temporary=(Get-FileHash $_).Hash}})|ConvertTo-Json|Set-Content (Join-Path $scratch 'temporary-identities.json')
 & 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation *> (Join-Path $scratch 'native-env.log')
 $env:VULKAN_SDK='C:/VulkanSDK/1.4.357.0'
 cmake -S native/NovaCore.Native -B (Join-Path $scratch 'native-release') -G Ninja -DCMAKE_BUILD_TYPE=Release *> (Join-Path $scratch 'native-configure.log');Gate
 cmake --build (Join-Path $scratch 'native-release') *> (Join-Path $scratch 'native-build.log');Gate
 dotnet build samples/NovaCore.Triangle -c Release --artifacts-path (Join-Path $scratch 'artifacts') '-p:NativeBuildDirectory=srv01-startup-first-present/native-release' -p:ContinuousIntegrationBuild=true --nologo -v:q *> (Join-Path $scratch 'managed-build.log');Gate
}finally{
 foreach($p in $paths){[IO.File]::WriteAllBytes($p,$backups[$p]);if((Get-FileHash $p).Hash -ne $hashes[$p]){throw 'Byte restoration mismatch'}}
 Pop-Location
 Write-Output 'All three temporary source edits restored exactly.'
}
