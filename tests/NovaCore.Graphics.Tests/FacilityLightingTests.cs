using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class FacilityLightingTests
{
    internal static void FlightEnvelope()
    {
        FacilitySupportTests.LoadPhysicalData();
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        TerrainAssetRepository.TryFindRoot(out var repository);
        var assets=Path.Combine(repository!,"assets","vehicles","modular-starter");
        var catalog=NovaCore.Simulation.Spacecraft.Assemblies.AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(assets,"catalog.json")));
        SolarSystemScene.TryCreateAt(new(1),SimulationInstant.Zero,out var solar,out _);
        PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,repository!,out var query);
        var slab=solar!.FloridaLaunchSite.CreateSupportSlab(query!);
        foreach(var depth in new[]{1,2,3,4,8}){
            using var editor=new NovaCore.Simulation.Spacecraft.Assemblies.ConstructionEditorSession(catalog);
            editor.Load(0,NovaCore.Simulation.Spacecraft.Assemblies.AssemblyJson.Write(NovaCore.ConstructionEditor.StabilizationCraftFixture.Create(catalog,depth,shortOnly:true)),true);
            foreach(var host in new[]{depth/2,depth-1}.Distinct().Where(i=>i!=0))editor.Remove(editor.Revision,$"r{host}-0");
            editor.FillForLaunch(editor.Revision);
            var craft=NovaCore.Simulation.Spacecraft.Assemblies.CraftCompiler.Compile(catalog,editor.Current!.Design.Data,assets);
            var before=editor.Save(editor.Revision);
            try{using var session=NovaCore.Simulation.Spacecraft.Assemblies.ConstructionApplicationSession.CreateSupported(craft,query!,slab,SimulationInstant.Zero);if(depth>=8)throw new Exception("Overload witness unexpectedly admitted");Console.WriteLine($"PAD_FLIGHT_ENVELOPE depth={depth} shortOnly=true ADMITTED maxMass={craft.Mass.MaximumMass:R}");}
            catch(InvalidDataException e){if(depth<8||!e.Message.Contains("support total load capacity",StringComparison.Ordinal))throw;Console.WriteLine($"PAD_FLIGHT_ENVELOPE depth={depth} shortOnly=true REFUSED reason={e.Message}");if(!editor.Save(editor.Revision).SequenceEqual(before))throw new Exception("Refusal mutated source");}
        }
    }
    internal static void Run()
    {
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        Require(Marshal.SizeOf<NativeFacilityCasterDefinition>()==160,"caster ABI size");
        Require(Marshal.OffsetOf<NativeFacilityCasterDefinition>(nameof(NativeFacilityCasterDefinition.OriginX)).ToInt32()==32&&
            Marshal.OffsetOf<NativeFacilityCasterDefinition>(nameof(NativeFacilityCasterDefinition.MaximumRayDistance)).ToInt32()==152,"caster ABI offsets");
        Require(Marshal.SizeOf<NativeFrameSubmission>()==800&&Marshal.OffsetOf<NativeFrameSubmission>(nameof(NativeFrameSubmission.FacilityCaster)).ToInt32()==792,"existing frame ABI offsets/size must be preserved");
        FacilitySupportTests.LoadPhysicalData();
        var previous=PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            var region=FloridaFacilitySupport.Region;var terrain=PlanetaryTerrainDefinition.EarthProductionCubeV5;
            var height=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(terrain,region.Up);
            Require(SolarSystemScene.TryCreateAt(new ReferenceFrameId(1),SimulationInstant.Zero,out var scene,out var error),error??"scene");
            var site=scene!.FloridaLaunchSite;var caster=scene.FacilityCaster;
            Require(caster.BodyId==site.Object.Anchor.BodyId&&caster.ObjectId==site.Object.Id.Value&&caster.FacilityId==site.Object.Id.Value,"authored body/site/object identity");
            Require(caster.GeometrySet==MeshHandle.FloridaSupportSlab.Value&&caster.Version==1&&caster.MaximumRayDistance==2048,"bounded authored geometry definition");
            Require(site.LocalPhysicalSurfaceRadiusMetres==6371030.770461536d&&site.FoundationDepthMetres==6.835569277405739d,"lighting must not move facility transforms");
            Require(caster.FoundationScaleX==site.SupportSlabScale.X&&caster.FoundationScaleY==site.SupportSlabScale.Z&&caster.FoundationScaleZ==site.SupportSlabScale.Y,"caster matches single slab union in ENU");
            var origin=new Double3(caster.OriginX,caster.OriginY,caster.OriginZ);
            Require(origin==site.Object.Anchor.NormalizedBodyFixedDirection*(site.LocalPhysicalSurfaceRadiusMetres+site.SupportSlabCenterUp),"FP64 authored body origin");
            Require(SurfaceEnuFrame.TryCreate(site.Object.Anchor,out var frame),"site ENU");
            Require(new Double3(caster.EastX,caster.EastY,caster.EastZ)==frame.East&&new Double3(caster.NorthX,caster.NorthY,caster.NorthZ)==frame.North&&new Double3(caster.UpX,caster.UpY,caster.UpZ)==frame.Up,"FP64 authored basis");
            for(var i=0;i<100;i++)Require(scene.FacilityCaster.Equals(caster),"static caster identity must be reused");
            var maxError=0d;
            foreach(var ticks in new[]{0L,1_000_000L,86_400_000_000L})
            {
                var camera=new NovaCore.Core.Camera.CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1000),NovaCore.Core.Camera.CameraMode.Free);
                Require(scene.TryPresentPhysicalEpoch(new(ticks),camera,out var epochError),epochError??"epoch");
                var slab=scene.FloridaSupportSlab();
                Require(slab.Mesh==MeshHandle.FloridaSupportSlab&&slab.Scale==new Double3(64,site.FoundationDepthMetres+1.5,48),"one canonical current slab");
                Require(scene.Presentation.TryGetBody(caster.BodyId,out var earth),"Earth");
                for(var i=0;i<8;i++)
                {
                    var local=new Double3((i&1)==0?-32:32,(i&2)==0?-slab.Scale.Y*.5:slab.Scale.Y*.5,(i&4)==0?-24:24);
                    var body=frame.East*local.X-frame.North*local.Z+frame.Up*(site.LocalPhysicalSurfaceRadiusMetres+site.SupportSlabCenterUp+local.Y);
                    var expected=earth.Position.Value+earth.BodyFixedToRoot.Rotate(body);
                    var actual=slab.RootPosition.Value+slab.RootOrientation.Rotate(local);
                    maxError=Math.Max(maxError,Math.Sqrt((expected-actual).LengthSquared));
                    Require(maxError<.0001,"physical slab corners remain within root FP64 precision");
                }
                Require(scene.FacilityCaster.Equals(caster),"site caster immutable across epochs");
            }
            Console.WriteLine($"FLORIDA_PAD_AUTHORITY PASS mesh=7 singleSlab=true physicalCorners=24 maxRootErrorMetres={maxError:R} caster=1 legacyCasterRefused=true");
            Require(PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(terrain,region.Up)==height,"caster setup changes physical H");
            Require(SampleOptions.TryParse(["--scene=sol","--focus=earth","--surface-site=florida-launch","--physical-surface=m12d-natural-candidate"],out var options,out _)&&options.UseProductionEarth,"caster must not require the anchored renderer");
        }
        finally{PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous);}
    }
}
