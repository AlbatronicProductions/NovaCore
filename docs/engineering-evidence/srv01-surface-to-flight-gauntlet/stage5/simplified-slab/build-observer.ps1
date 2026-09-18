# Disposable compile overlay: same candidate work, indexed existing timing arrays and GC snapshots.
# No production file is rewritten. Reporting occurs after the completed episode.
$ErrorActionPreference='Stop'
Set-Location -LiteralPath 'E:\NovaCore'
$output=Join-Path $PWD 'build/srv01-stage5-simplified-slab'
$source=[IO.File]::ReadAllText((Join-Path $PWD 'samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs'))
$source=$source.Replace('private long sequence,timestamp,remainder;', 'private long sequence,timestamp,remainder; private int probeWarm=-1,probeGc0,probeGc1,probeGc2,probeWarmGc0,probeWarmGc1,probeWarmGc2;')
$source=$source.Replace('started=true;timestamp=Stopwatch.GetTimestamp();','started=true;probeGc0=GC.CollectionCount(0);probeGc1=GC.CollectionCount(1);probeGc2=GC.CollectionCount(2);timestamp=Stopwatch.GetTimestamp();')
$source=$source.Replace('serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;',@'
serviceMilliseconds[frameCount-1]=(Stopwatch.GetTimestamp()-serviceStart)*1000d/Stopwatch.Frequency;
        if(probeWarm<0&&observation.State.Frontier>=128){probeWarm=frameCount;probeWarmGc0=GC.CollectionCount(0);probeWarmGc1=GC.CollectionCount(1);probeWarmGc2=GC.CollectionCount(2);}
'@)
$source=$source.Replace('if(frameCount==0)return;Array.Sort(frameMilliseconds,0,frameCount);',@'
if(frameCount==0)return;
        var gc0=GC.CollectionCount(0);var gc1=GC.CollectionCount(1);var gc2=GC.CollectionCount(2);
        Console.WriteLine($"SLAB_LIVE_GC total={gc0-probeGc0},{gc1-probeGc1},{gc2-probeGc2} warm={gc0-probeWarmGc0},{gc1-probeWarmGc1},{gc2-probeWarmGc2} warmStart={probeWarm}");
        for(var i=0;i<Math.Min(8,frameCount);i++)Console.WriteLine($"SLAB_LIVE_FIRST index={i} displayMs={frameMilliseconds[i]:R} serviceMs={serviceMilliseconds[i]:R}");
        if(probeWarm>=0&&probeWarm<frameCount)
        {
            foreach(var pair in new[]{("display",frameMilliseconds),("service",serviceMilliseconds)})
            {
                var rows=pair.Item2.Take(frameCount).Skip(probeWarm).Select((ms,i)=>new{ms,index=i+probeWarm}).ToArray();
                var sorted=rows.Select(x=>x.ms).Order().ToArray();double Percent(double q)=>sorted[Math.Clamp((int)Math.Ceiling(q*sorted.Length)-1,0,sorted.Length-1)];
                Console.WriteLine("SLAB_LIVE_WARM "+System.Text.Json.JsonSerializer.Serialize(new{population=pair.Item1,samples=sorted.Length,median=Percent(.5),p95=Percent(.95),p99=Percent(.99),maximum=Percent(1),tails=rows.OrderByDescending(x=>x.ms).Take(8)}));
            }
        }
        Array.Sort(frameMilliseconds,0,frameCount);
'@)
[IO.File]::WriteAllText((Join-Path $output 'StockAssemblyDevelopmentScene.observer.cs'),$source)
$targets='<Project><ItemGroup><Compile Remove="$(MSBuildProjectDirectory)/StockAssemblyDevelopmentScene.cs"/><Compile Include="'+$output+'/StockAssemblyDevelopmentScene.observer.cs" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $output 'observer.targets'),$targets)
New-Item -ItemType Directory -Force -Path "$output/observer" | Out-Null
Copy-Item "$output/artifacts/bin/NovaCore.Triangle/release/*" -Destination "$output/observer" -Recurse -Force
dotnet build samples/NovaCore.Triangle -c Release --artifacts-path "$output/artifacts" -o "$output/observer" -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$output/observer.targets" -p:NativeBuildDirectory=srv01-stage5-simplified-slab/native-release --nologo -v:q *> "$output/observer-build.txt"
if($LASTEXITCODE -ne 0){throw 'Observer build failed'}
