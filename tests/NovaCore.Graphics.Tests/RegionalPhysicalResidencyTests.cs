using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using NovaCore.Core;
using NovaCore.Graphics;

internal static class RegionalPhysicalResidencyTests
{
    internal static void Run()
    {
        Require(TerrainAssetRepository.TryFindRoot(out var root),"repository root");
        VerifyPreparationScheduling(root);
        var output=Path.Combine(root,"build","regional-live-tests",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var sample=Path.Combine(root,"samples","NovaCore.Triangle","bin","Debug","net10.0","NovaCore.Triangle.exe");
        Require(File.Exists(sample),"Build the Debug Triangle sample before the live regional Graphics regression.");
        // Leave a bounded drain interval after frame-660 reentry for every
        // complete staged generation; readiness is asserted from publication.
        var log=RunSample(sample,root,output,"--scene=sol --focus=earth --surface-site=florida-launch --solar-epoch=j2000 --benchmark-frames=1000 --log=vulkan --log=validation","regional");
        Require(log.Contains("required=670; anchoredPatches=0; unresolved=1"),"live NCSM1 demand disappeared or requires anchored patches");
        var ready=log.IndexOf("NCSM1 regional ready:",StringComparison.Ordinal);
        var publication=log.IndexOf("Production spherical billboard publication:",StringComparison.Ordinal);
        Require(ready>=0&&publication>ready,"NCSM1 geometry published before regional physical readiness");
        VerifySlicePublication(log);
        Require(log.Contains("requests=670;")&&log.Contains("loaded=670;")&&log.Contains("uploadedBytes=93392640;"),"physical requests/uploads changed; remeasure workload");
        Require(log.Contains("body=8; earthEligible=False")&&log.Contains("body=10; earthEligible=False"),"missing non-Earth focus coverage");
        Require(!log.Contains("zeroOwner=1")&&!log.Contains("overlapOwner=1")&&!log.Contains("staleGenerationDraws=1")&&!log.Contains("device lost",StringComparison.OrdinalIgnoreCase),"ownership/device regression");
        foreach(var line in log.Split('\n').Where(l=>l.Contains("VUID-")))
            Require(line.Contains("VUID-VkMemoryAllocateInfo-memoryTypeIndex-00645"),"new Vulkan validation error: "+line);
        Analyze(root,output);
        var outside=Path.Combine(output,"outside-earth");Directory.CreateDirectory(outside);
        var outsideLog=RunSample(sample,root,outside,"--scene=sol --focus=earth --surface-site=land --altitude=3000000 --solar-epoch=j2000 --benchmark-frames=120 --log=validation",null);
        Require(outsideLog.Contains("anchoredPatches=0;")&&outsideLog.Contains("requests=5;")&&outsideLog.Contains("loaded=5;"),"outside Earth demand must follow bounded NCSM1 sample coverage");
        var isolated=Path.Combine(output,"non-earth");Directory.CreateDirectory(isolated);
        var away=RunSample(sample,root,isolated,"--scene=sol --solar-epoch=j2000 --benchmark-frames=60 --log=validation","regional-isolation");
        Require(away.Contains("NCSM1 regional physical totals: requests=0;"),"non-Earth body requested Florida physical data");
        Console.WriteLine("Live regional residency evidence: "+output);
    }
    private static string RunSample(string sample,string root,string output,string arguments,string? mode)
    {
        var start=new ProcessStartInfo(sample,arguments){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.Environment["NOVACORE_WINDOW_CLIENT_WIDTH"]="3440";start.Environment["NOVACORE_WINDOW_CLIENT_HEIGHT"]="1440";
        start.Environment["NOVACORE_WINDOW_BORDERLESS"]="1";start.Environment["NOVACORE_REGIONAL_PHYSICAL_PROBE"]=output;
        start.Environment.Remove("NOVACORE_EARTH_ROUTE_VALIDATION");if(mode!=null)start.Environment["NOVACORE_EARTH_ROUTE_VALIDATION"]=mode;
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        if(!process.WaitForExit(600000)){process.Kill(entireProcessTree:true);throw new InvalidOperationException("live regional scenario timed out");}
        var log=stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult();File.WriteAllText(Path.Combine(output,"runtime.log"),log);
        Require(process.ExitCode==0,"native regional scenario failed: "+output);return log;
    }
    internal static void Analyze(string root,string output)
    {
        Require(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Require(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Require(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        var previous=PlanetaryPhysicalSurface.RuntimeGeneration;
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        try
        {
            var files=Directory.GetFiles(output,"frame-*.json");Require(files.Length>0,"no live physical capture");
            var footprint=new List<object>();var levels=new HashSet<int>();var pupils=new HashSet<int>();bool reentered=false;int checkedVertices=0;
            var anchor=new Double3(.1433224599406355,.4788205718227514,.8661348234979923);
            foreach(var file in files)
            {
                using var json=JsonDocument.Parse(File.ReadAllText(file));var f=json.RootElement;
                int frame=f.GetProperty("frame").GetInt32(),level=f.GetProperty("level").GetInt32();levels.Add(level);pupils.Add(f.GetProperty("pupilFrameIdentity").GetInt32());
                Require(f.GetProperty("physicalGeneration").GetInt32()==4&&f.GetProperty("surfaceMode").GetInt32()==2,"ordinary production route changed");
                Require(f.GetProperty("regionalRequests").GetInt32()>0&&f.GetProperty("regionalPublishedLayers").GetInt32()==670,"missing live residency");
                Require(f.GetProperty("gpuOutsideCatalogRecords").EnumerateArray().All(v=>v.GetDouble()==-1),"live demand lookup falsely selects Florida in no-regional areas, including the same cube face");
                var c=f.GetProperty("gpuExactAnchorComponents").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
                Require(Math.Abs(c[0]-EarthElevationDataset.SampleElevation(anchor))<.002,"live global component parity");
                Require(Math.Abs(c[1]-EarthLocalTerrainElevationDataset.SampleResidual(anchor))<1e-7,"live regional component parity");
                Require(Math.Abs(c[5]-Height(anchor))<.002,"live composed height parity");
                int found=0;
                foreach(var s in f.GetProperty("samples").EnumerateArray())
                {
                    if(!s.GetProperty("found").GetBoolean())continue;
                    if(Math.Abs(s.GetProperty("east").GetDouble())<=32&&Math.Abs(s.GetProperty("north").GetDouble())<=24)found++;
                    var direction=Vector(s.GetProperty("direction"));double h=Height(direction),render=s.GetProperty("height").GetDouble();
                    foreach(var vertex in s.GetProperty("vertices").EnumerateArray()){
                        var d=Vector(vertex.GetProperty("position")).Normalized();var expected=PlanetaryPhysicalSurface.EvaluateBaseHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,d);
                        Require(Math.Abs(vertex.GetProperty("preparedHeight").GetDouble()-expected)<.003,"live prepared vertex omitted/changed authoritative physical data");checkedVertices++;
                    }
                    footprint.Add(new {frame,level,east=s.GetProperty("east").GetDouble(),north=s.GetProperty("north").GetDouble(),canonicalH=h,regional=EarthLocalTerrainElevationDataset.SampleResidual(direction),rendered=render,error=render-h,baseHeight=PlanetaryPhysicalSurface.EvaluateBaseHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,direction)});
                }
                // Publication is deliberately deferred; inspect the completed L17
                // return, including a publication after the old frame-740 checkpoint.
                if(frame>=740&&level==17){Require(f.GetProperty("maxOuter").GetDouble()==1&&f.GetProperty("maxInner").GetDouble()<=1,"reentry factor-1 workload changed");Require(found==9,"Florida reentry footprint incomplete");reentered=true;}
            }
            Require(levels.Contains(16)&&levels.Contains(17)&&pupils.Count>2&&reentered&&checkedVertices>50,"missing adjacent-level/pupil/reentry physical coverage");
            FacilitySupportTests.AnalyzeLive(output);
            File.WriteAllText(Path.Combine(output,"footprint.json"),JsonSerializer.Serialize(footprint,new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"Live regional CPU/GPU parity PASS: captures={files.Length}; checkedVertices={checkedVertices}; levels={string.Join(',',levels.Order())}; pupils={pupils.Count}");
        }
        finally {PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous);}
    }
    internal static void VerifyPreparationScheduling(string root)
    {
        var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","NovaCoreNative.cpp"));
        var staged=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","RegionalPhysicalPreparation.inl"));
        var residency=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","RegionalPhysicalResidency.inl"));
        Require(staged.Contains("std::min(RegionalPreparationVertexBudget,total-job.cursor)")&&
            staged.Contains("job.fencePending=job.cursor==total")&&staged.Contains("return job.fencePending;"),"whole-generation preparation or early slice completion returned");
        Require(native.Contains("RegionalPhysicalEnabled(a)&&!RecordRegionalPreparation(a,c,true))return;")&&
            native.Contains("a.regionalPreparation[1].fencePending)&&counters"),"incomplete physical slices can reach incoming compaction/publication");
        Require(!staged.Contains("vkWaitForFences")&&!staged.Contains("vkQueueWaitIdle")&&!staged.Contains("vkDeviceWaitIdle"),"staged work synchronously waits on the foreground path");
        Require(staged.Contains("frames.current=a.regionalPublishedPupil")&&
            staged.Contains("std::swap(a.productionBillboardPhysicalBuffer,a.regionalScratchBuffer)")&&
            staged.Contains("if(!job.fencePending)return;"),"pupil updates can mutate published geometry before their fence");
        Require(residency.Contains("job.cursor==job.frame.metadata[2]?2u:0u")&&
            residency.Contains("job.phase==2&&a.regionalPhysical->Complete(job.mask)"),"partial geographic demand became ready");
        int update=native.IndexOf("void Update(App &a, float dt)",StringComparison.Ordinal);
        int wait=native.IndexOf("vkWaitForFences",update,StringComparison.Ordinal);
        int inspect=native.IndexOf("InspectRegionalPhysical(a)",update,StringComparison.Ordinal);
        Require(wait>update&&inspect>wait,"staged pupil inspection moved before the completed frame fence");
        var shader=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","production_spherical_billboard_prepare.comp"));
        Require(shader.Contains("stagedPhysical.values[vertex]=physical.values[vertex]")&&shader.Contains("preparation.previous"),"exact unchanged physical vertices lost safe reuse from the frozen prior pupil");
    }
    private static void VerifySlicePublication(string log)
    {
        ulong owner=0;uint cursor=0,total=0;ulong incoming=0;int delayedCurrentFrames=0,completed=0;
        foreach(var line in log.Split('\n'))
        {
            if(line.Contains("NCSM1 physical slice: incoming=1;"))
            {
                var fields=Regex.Matches(line,@"(\w+)=(\d+)").ToDictionary(m=>m.Groups[1].Value,m=>ulong.Parse(m.Groups[2].Value));
                if(fields["first"]==0){Require(incoming==0,"second incoming transaction started before publication");incoming=fields["generation"];cursor=0;total=(uint)fields["total"];}
                Require(fields["generation"]==incoming&&fields["first"]==cursor&&fields["count"] is >0 and <=65536,"physical slices overlap, skip input, or exceed the submission budget");
                Require(fields["currentOwner"]==owner,"outgoing owner changed while incoming physical work was incomplete");
                cursor+=(uint)fields["count"];Require((fields["complete"]==1)==(cursor==total),"slice completion was declared early");
                if(cursor<total&&owner!=0)delayedCurrentFrames++;
            }
            if(line.Contains("Production spherical billboard publication:"))
            {
                var generation=ulong.Parse(Regex.Match(line,@"generation=(\d+)").Groups[1].Value);
                Require(incoming==generation&&cursor==total&&total>0,"generation published without all physical slices");
                owner=generation;incoming=0;completed++;
            }
        }
        Require(completed>2&&delayedCurrentFrames>10,"live run did not exercise current rendering through delayed physical readiness");
    }
    private static Double3 Vector(JsonElement v)=>new(v[0].GetDouble(),v[1].GetDouble(),v[2].GetDouble());
    private static double Height(Double3 d)=>PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,d);
    private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
}
