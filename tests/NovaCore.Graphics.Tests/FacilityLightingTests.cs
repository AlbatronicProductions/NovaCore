using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class FacilityLightingTests
{
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
            Require(caster.GeometrySet==MeshHandle.FloridaLaunchPad.Value&&caster.Version==1&&caster.MaximumRayDistance==2048,"bounded authored geometry definition");
            Require(site.LocalPhysicalSurfaceRadiusMetres==6371030.770461536d&&site.FoundationDepthMetres==6.835569277405739d,"lighting must not move facility transforms");
            Require(caster.FoundationScaleX==site.FoundationScale.X&&caster.FoundationScaleY==site.FoundationScale.Y&&caster.FoundationScaleZ==site.FoundationScale.Z,"caster matches unchanged foundation scale");
            var origin=new Double3(caster.OriginX,caster.OriginY,caster.OriginZ);
            Require(origin==site.Object.Anchor.NormalizedBodyFixedDirection*site.LocalPhysicalSurfaceRadiusMetres,"FP64 authored body origin");
            Require(SurfaceEnuFrame.TryCreate(site.Object.Anchor,out var frame),"site ENU");
            Require(new Double3(caster.EastX,caster.EastY,caster.EastZ)==frame.East&&new Double3(caster.NorthX,caster.NorthY,caster.NorthZ)==frame.North&&new Double3(caster.UpX,caster.UpY,caster.UpZ)==frame.Up,"FP64 authored basis");
            for(var i=0;i<100;i++)Require(scene.FacilityCaster.Equals(caster),"static caster identity must be reused");
            Require(PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(terrain,region.Up)==height,"caster setup changes physical H");
            Require(SampleOptions.TryParse(["--scene=sol","--focus=earth","--surface-site=florida-launch","--physical-surface=m12d-natural-candidate"],out var options,out _)&&options.UseProductionEarth,"caster must not require the anchored renderer");
        }
        finally{PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous);}
    }
}
