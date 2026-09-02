using System.Diagnostics;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryPhysicalSurfaceModifierTests
{
    private const double EarthRadius = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;

    public static void Run()
    {
        VerifyContractAndDeterminism();
        VerifyBiomeControlAndFamilies();
        VerifyGradientAndFootprint();
        VerifyBoundaryAndOwnerParity();
        VerifyM12CPresentationWeightParity();
        VerifyTransactionalPublication();
        VerifyFrequencyAndCost();
    }

    private static void VerifyContractAndDeterminism()
    {
        var generation = PlanetaryPhysicalSurface.EarthGeneration;
        Require(generation.IsComplete && generation.GenerationId == 3 &&
            generation.SchemaVersion == 2 && generation.BodyId == 6 && generation.TerrainVersion == 5,
            "M12C Earth modifier generation identity");
        Require(generation.Modifiers.Length == 7 &&
            generation.Modifiers[0].Id.Type == PlanetaryTerrainModifierType.TiledDetail &&
            generation.Modifiers[1].Id.Type == PlanetaryTerrainModifierType.RollingGrassland &&
            generation.Modifiers[2].Id.Type == PlanetaryTerrainModifierType.RockyMountain &&
            generation.Modifiers[3].Id.Type == PlanetaryTerrainModifierType.DesertDunes &&
            generation.Modifiers[4].Id.Type == PlanetaryTerrainModifierType.CoastalWetland &&
            generation.Modifiers[5].Id.Type == PlanetaryTerrainModifierType.SnowGlacial &&
            generation.Modifiers[6].Id.Type == PlanetaryTerrainModifierType.ErosionLike &&
            generation.Modifiers[0].Order < generation.Modifiers[6].Order,
            "explicit stable modifier ordering");

        var probes = new[]
        {
            BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
                FloridaLaunchSite.Longitude*Math.PI/180d),
            RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveX,.37d,.61d),
            RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.NegativeY,.22d,.73d),
            new Double3(1d,1d,1d).Normalized()
        };
        foreach (var direction in probes)
        {
            var geographic = PlanetaryTerrainDefinition.EarthProductionCubeV5.SampleBaseHeight(direction);
            var first = PlanetaryPhysicalSurface.EvaluateModifiers(direction, geographic);
            for (var repeat = 0; repeat < 32; repeat++)
                Require(PlanetaryPhysicalSurface.EvaluateModifiers(direction, geographic) == first,
                    "modifier evaluation is bit-stable across frames/order");
            Require(first.IsFinite && Math.Abs(first.TiledHeightMetres) <= PlanetaryPhysicalSurface.TiledAmplitudeMetres + 1e-12d &&
                Math.Abs(first.ErosionHeightMetres) <= PlanetaryPhysicalSurface.ErosionAmplitudeMetres + 1e-12d &&
                Math.Abs(first.MesoHeightMetres) <= PlanetaryPhysicalSurface.RollingMaximumAmplitudeMetres +
                    PlanetaryPhysicalSurface.RockyMaximumAmplitudeMetres + PlanetaryPhysicalSurface.DesertMaximumAmplitudeMetres +
                    PlanetaryPhysicalSurface.CoastalMaximumAmplitudeMetres + PlanetaryPhysicalSurface.GlacialMaximumAmplitudeMetres + 1e-12d &&
                Math.Abs(first.NearHeightMetres) <= PlanetaryPhysicalSurface.NearMaximumAmplitudeMetres + 1e-12d,
                "modifier amplitudes are finite and bounded");
        }

        PlanetaryPhysicalSurface.EvaluateModifiers(probes[0]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            PlanetaryPhysicalSurface.EvaluateModifiers(probes[index & 3], 600d);
        var allocations = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocations == 0, $"modifier evaluation allocates no per-sample memory: {allocations}");
    }

    private static void VerifyBiomeControlAndFamilies()
    {
        var matrix = new (string Name, double Latitude, double Longitude, double Height)[]
        {
            ("Florida coast", 28.50d, -80.60d, 5d),
            ("Appalachian rolling", 35.60d, -83.50d, 850d),
            ("Rocky mountain", 39.10d, -106.80d, 2_900d),
            ("Sahara desert", 24.00d, 13.00d, 420d),
            ("Siberian snow", 70.00d, 105.00d, 900d),
            ("remote grassland", 46.00d, -101.00d, 540d),
            ("no regional data", -32.00d, 135.00d, 280d)
        };
        var observed = new bool[10];
        foreach (var sample in matrix)
        {
            var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(sample.Latitude * Math.PI / 180d,
                sample.Longitude * Math.PI / 180d);
            var blend = PlanetaryBiomeControlAuthority.Sample(direction, sample.Height);
            Require(blend.IsFinite && Math.Abs(blend.TotalWeight - 1d) <= 1e-12d,
                $"{sample.Name} has a normalized four-contributor biome blend");
            for (var biome = PlanetarySurfaceBiome.OceanCoastal; biome <= PlanetarySurfaceBiome.DevelopedReserved; biome++)
                observed[(int)biome] |= blend.Weight(biome) > 0d;
            var modifiers = PlanetaryPhysicalSurface.EvaluateModifiers(direction, sample.Height);
            Require(modifiers.IsFinite && Math.Abs(modifiers.HeightMetres) <= 116.4d,
                $"{sample.Name} physical synthesis is finite and budgeted");
            var material = PlanetaryTerrainMaterialSynthesis.Classify(blend, sample.Height > 0d ? 1f : 0f);
            Require(material.IsFinite && Math.Abs(material.Total - 1f) < 2e-5f,
                $"{sample.Name} material contributors are normalized");
        }
        // The named physical probes validate representative finished outputs;
        // a bounded planet-wide sweep proves completeness of the procedural
        // control authority without pretending seven points are a biome atlas.
        foreach (var height in new[] { 5d, 60d, 520d, 1_800d, 3_500d })
        for (var latitude = -80; latitude <= 80; latitude += 10)
        for (var longitude = -180; longitude < 180; longitude += 15)
        {
            var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(latitude * Math.PI / 180d,
                longitude * Math.PI / 180d);
            var blend = PlanetaryBiomeControlAuthority.Sample(direction, height);
            for (var biome = PlanetarySurfaceBiome.OceanCoastal; biome <= PlanetarySurfaceBiome.DevelopedReserved; biome++)
                observed[(int)biome] |= blend.Weight(biome) > 0d;
        }
        Require(observed[(int)PlanetarySurfaceBiome.BeachSand] && observed[(int)PlanetarySurfaceBiome.Wetland] &&
            observed[(int)PlanetarySurfaceBiome.GrassRolling] && observed[(int)PlanetarySurfaceBiome.ScrubDry] &&
            observed[(int)PlanetarySurfaceBiome.Desert] && observed[(int)PlanetarySurfaceBiome.RockyMountain] &&
            observed[(int)PlanetarySurfaceBiome.Alpine] && observed[(int)PlanetarySurfaceBiome.SnowGlacial],
            "arbitrary-Earth matrix exercises every natural land biome family");
    }

    private static void VerifyGradientAndFootprint()
    {
        var florida = BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
            FloridaLaunchSite.Longitude*Math.PI/180d);
        var probes = new[]
        {
            florida,
            (florida*EarthRadius+PlanetarySurfaceFrame.AtDirection(florida).East*4_000d).Normalized(),
            (florida*EarthRadius+PlanetarySurfaceFrame.AtDirection(florida).North*12_000d).Normalized()
        };
        var maximumEastError = 0d; var maximumNorthError = 0d;
        foreach (var direction in probes)
        {
            var frame = PlanetarySurfaceFrame.AtDirection(direction); const double step = .001d;
            var left = (direction*EarthRadius-frame.East*step).Normalized();
            var right = (direction*EarthRadius+frame.East*step).Normalized();
            var down = (direction*EarthRadius-frame.North*step).Normalized();
            var up = (direction*EarthRadius+frame.North*step).Normalized();
            const double geographic = 600d;
            var value = PlanetaryPhysicalSurface.EvaluateModifiers(direction, geographic);
            var east = (PlanetaryPhysicalSurface.EvaluateModifiers(right, geographic).HeightMetres-
                PlanetaryPhysicalSurface.EvaluateModifiers(left, geographic).HeightMetres)/(2d*step);
            var north = (PlanetaryPhysicalSurface.EvaluateModifiers(up, geographic).HeightMetres-
                PlanetaryPhysicalSurface.EvaluateModifiers(down, geographic).HeightMetres)/(2d*step);
            maximumEastError = Math.Max(maximumEastError, Math.Abs(value.EastGradient-east));
            maximumNorthError = Math.Max(maximumNorthError, Math.Abs(value.NorthGradient-north));
        }
        Require(maximumEastError < 2e-3d && maximumNorthError < 2e-3d,
            $"analytic modifier gradient parity: east={maximumEastError:R}; north={maximumNorthError:R}");

        var floridaFrame = PlanetarySurfaceFrame.AtDirection(florida);
        var boundary = (florida*EarthRadius+floridaFrame.East*PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres).Normalized();
        var outside = (florida*EarthRadius+floridaFrame.East*(PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres+10d)).Normalized();
        var boundaryValue = PlanetaryPhysicalSurface.EvaluateModifiers(boundary, 5d);
        var outsideValue = PlanetaryPhysicalSurface.EvaluateModifiers(outside, 5d);
        Require(Math.Abs(boundaryValue.ErosionHeightMetres) < 1e-10d &&
            Math.Abs(outsideValue.ErosionHeightMetres) < 1e-12d && outsideValue.GeographicWeight == 0d,
            "bounded erosion contribution and weight close continuously at the physical footprint");
    }

    private static void VerifyBoundaryAndOwnerParity()
    {
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        var shared = RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, 1d, .25d));
        var sameFace = RelaxedCubeSphereProjection.UnitDirection(CubeSphereFace.PositiveZ, .5d, .5d);
        foreach (var direction in new[] { shared, sameFace })
        {
            var global = terrain.SamplePhysicalSurface(direction);
            var anchored = terrain.SamplePhysicalSurface(direction);
            Require(global == anchored,
                "global and dynamic hierarchy consume one body-fixed physical result");
            Require(global.IsFinite && Math.Abs(global.PhysicalNormal.LengthSquared-1d) < 1e-12d,
                "shared physical normal is finite and unit length");
        }

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var globalShader = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary.vert"));
        var anchoredShader = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","anchored_physical_surface.glsl"));
        var canonicalShader = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary_physical_authority.glsl"));
        var fragment = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary_production.frag"));
        var hierarchy = File.ReadAllText(Path.Combine(root,"src","NovaCore.Graphics","PlanetaryDynamicAnchoredSurface.cs"));
        Require(globalShader.Contains("planetary_physical_authority.glsl",StringComparison.Ordinal) &&
            anchoredShader.Contains("planetary_physical_authority.glsl",StringComparison.Ordinal) &&
            canonicalShader.Contains("CanonicalGeographicHeight",StringComparison.Ordinal) &&
            fragment.Contains("physical_surface.glsl",StringComparison.Ordinal) &&
            hierarchy.Contains(".SamplePhysicalSurface(",StringComparison.Ordinal),
            "global GPU geometry, anchored GPU geometry, dynamic CPU selection, and fragment presentation route through one canonical physical-surface contract");
    }

    private static void VerifyTransactionalPublication()
    {
        var initial = new PlanetaryTerrainModifierGeneration(1, 6, 5, 2,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        var publication = new PlanetaryTerrainModifierPublication(initial);
        var retainedHash = publication.Authoritative.DeterministicHash;

        var delayed = new PlanetaryTerrainModifierGeneration(2, 6, 5, 2,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        delayed.BeginPreparation();
        Require(!publication.TryPublish(delayed) && publication.Authoritative.DeterministicHash == retainedHash,
            "delayed generation cannot replace complete authority");
        Require(delayed.TryCompletePreparation() && publication.TryPublish(delayed) &&
            publication.Authoritative.GenerationId == 2,
            "one complete compatible generation publishes atomically");

        var unavailable = new PlanetaryTerrainModifierGeneration(3, 6, 5, 2,
            ReadOnlySpan<PlanetaryTerrainModifierDefinition>.Empty);
        Require(!publication.TryPublish(unavailable) && publication.Authoritative.GenerationId == 2,
            "unavailable modifier configuration retains previous complete generation");
        var incompatible = new PlanetaryTerrainModifierGeneration(4, 6, 6, 2,
            PlanetaryPhysicalSurface.EarthGeneration.Modifiers);
        Require(!publication.TryPublish(incompatible) && publication.Authoritative.GenerationId == 2,
            "incompatible terrain generation retains previous complete generation");
    }

    private static void VerifyM12CPresentationWeightParity()
    {
        var maximumIndividualError = 0f;
        var maximumL1Error = 0f;
        foreach (var height in new[] { -10f, 0f, 5f, 60f, 520f, 1_800f, 3_500f })
        for (var latitude = -80; latitude <= 80; latitude += 5)
        for (var longitude = -180; longitude < 180; longitude += 10)
        {
            var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(latitude*Math.PI/180d,longitude*Math.PI/180d);
            var canonical = PlanetaryTerrainMaterialSynthesis.Classify(PlanetaryBiomeControlAuthority.Sample(direction,height),1f);
            var presentation = EvaluatePresentationWeightsF(new((float)direction.X,(float)direction.Y,(float)direction.Z),height,1f);
            var l1 = 0f;
            for (var biome = PlanetaryTerrainMaterialKind.VegetatedSoil; biome <= PlanetaryTerrainMaterialKind.SnowIce; biome++)
            {
                var error = MathF.Abs(canonical[biome]-presentation[biome]);
                maximumIndividualError = MathF.Max(maximumIndividualError,error); l1 += error;
            }
            maximumL1Error = MathF.Max(maximumL1Error,l1);
            var canonicalTop=Dominant(canonical,out var canonicalMargin);
            var presentationTop=Dominant(presentation,out _);
            Require(canonicalTop==presentationTop||canonicalMargin<=2e-4f,
                $"FP32 presentation dominant biome is stable away from canonical ties at {latitude},{longitude},{height}");
            Require(MathF.Abs(presentation.Total-1f)<=2e-5f,"FP32 presentation weights are normalized");
        }
        Require(maximumIndividualError<=1e-4f && maximumL1Error<=2e-4f,
            $"FP64 canonical / FP32 presentation weight parity: max={maximumIndividualError:E3}; l1={maximumL1Error:E3}");
        Console.WriteLine($"M12C Candidate D presentation parity: maxWeightError={maximumIndividualError:E3}; maxL1={maximumL1Error:E3}");

        var physicalDirection=BodyFixedGeography.DirectionFromLatitudeLongitude(28.5d*Math.PI/180d,-80.6d*Math.PI/180d);
        var physicalBefore=PlanetaryPhysicalSurface.EvaluateModifiers(physicalDirection,5d);
        var physicalAfter=PlanetaryPhysicalSurface.EvaluateModifiers(physicalDirection,5d);
        Require(physicalBefore.HeightMetres==physicalAfter.HeightMetres && physicalBefore.EastGradient==physicalAfter.EastGradient &&
            physicalBefore.NorthGradient==physicalAfter.NorthGradient,"presentation weights leave physical height and normals unchanged");
        Require(PlanetaryPhysicalSurface.EarthGeneration.DeterministicHash==0xC6CDFF4A0E5468E2ul,
            "Candidate D preserves the frozen M12C physical generation hash");

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var physical = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","physical_surface.glsl"));
        var fragment = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","planetary_production.frag"));
        var material = File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","production_terrain_material.glsl"));
        Require(physical.Contains("PhysicalModifierEvaluationD EvaluateTerrainModifiersD",StringComparison.Ordinal) &&
            !physical.Contains("EvaluateTerrainModifierMaterialD",StringComparison.Ordinal) &&
            fragment.Contains("EvaluatePresentationBiomeWeightsF(samplingDirection,baseHeight,visible.land)",StringComparison.Ordinal) &&
            fragment.Contains("float sampledHeight=max(0.0,terrainHeight)",StringComparison.Ordinal),
            "fragment material uses only FP32 presentation weights while FP64 physical authority remains complete");
        Require(material.Contains("EvaluatePresentationBiomeWeightsF",StringComparison.Ordinal) &&
            material.Contains("TerrainBiplanarWeights(geometricNormal)",StringComparison.Ordinal) &&
            material.Contains("broadAttenuation>0.0",StringComparison.Ordinal),
            "material synthesis reuses body-fixed projection weights and skips fully band-limited frequency fields");
    }

    private static PlanetaryTerrainMaterialKind Dominant(in PlanetaryTerrainMaterialWeights weights,out float margin)
    {
        var first=PlanetaryTerrainMaterialKind.VegetatedSoil;var firstWeight=weights[first];var second=0f;
        for(var biome=PlanetaryTerrainMaterialKind.WetGround;biome<=PlanetaryTerrainMaterialKind.SnowIce;biome++)
            if(weights[biome]>firstWeight){second=firstWeight;first=biome;firstWeight=weights[biome];}else second=MathF.Max(second,weights[biome]);
        margin=firstWeight-second;return first;
    }

    private static PlanetaryTerrainMaterialWeights EvaluatePresentationWeightsF(Vector3 direction,float height,float landMask)
    {
        direction=Vector3.Normalize(direction);var point=direction*6_371_008.8f;
        var a=new Vector3(.74278135f,.557086f,-.37139067f);var b=new Vector3(-.43643578f,.21821789f,.87287156f);var c=new Vector3(.26726124f,-.80178374f,.5345225f);
        var latitude=MathF.Abs(direction.Y);var temperature=Saturate(1f-latitude*.82f-MathF.Max(height,0f)/8500f);
        var climateA=.5f+.5f*MathF.Sin(Vector3.Dot(point,a)*(MathF.Tau/1_850_000f)+.37f);
        var climateB=.5f+.5f*MathF.Sin(Vector3.Dot(point,b)*(MathF.Tau/620_000f)+2.11f);
        var climateC=.5f+.5f*MathF.Sin(Vector3.Dot(point,c)*(MathF.Tau/210_000f)-1.43f);
        var moisture=Saturate(.18f+.46f*climateA+.24f*climateB+.12f*climateC-.18f*temperature);var aridity=Saturate((1f-moisture)*(.55f+.45f*temperature));
        var coast=1f-Smooth(18f,420f,MathF.Abs(height));var land=Smooth(-2f,8f,height);var highland=Smooth(420f,2400f,height);var alpine=Smooth(1400f,3600f,height);
        var snow=Smooth(.72f,.94f,Saturate(latitude*.9f+MathF.Max(height,0f)/7500f+(1f-temperature)*.25f));var wet=Smooth(.58f,.86f,moisture)*(1f-Smooth(130f,900f,height));
        Span<float> raw=stackalloc float[10];raw[0]=1f-land+land*coast*.18f;raw[1]=land*coast*(1f-.55f*wet)*(1f-snow);raw[2]=land*wet*(1f-.6f*highland);raw[3]=land*moisture*temperature*(1f-coast)*(1f-highland)*(1f-snow);raw[4]=land*(1f-MathF.Abs(moisture-.38f)*1.8f)*temperature*(1f-.7f*highland)*(1f-coast);raw[5]=land*Smooth(.48f,.82f,aridity)*(1f-highland)*(1f-coast)*(1f-snow);raw[6]=land*highland*(1f-.55f*snow);raw[7]=land*alpine*(1f-snow);raw[8]=land*snow*(.35f+.65f*highland);raw[9]=land*(1f-highland)*Smooth(.78f,.94f,.5f+.5f*MathF.Sin(Vector3.Dot(point,b)*(MathF.Tau/145_000f)+.91f))*.18f;
        Span<bool> selected=stackalloc bool[10];Span<float> weights=stackalloc float[10];var total=0f;
        for(var slot=0;slot<4;slot++){var best=0;var value=-1f;for(var index=0;index<10;index++)if(!selected[index]&&raw[index]>value){best=index;value=raw[index];}selected[best]=true;weights[best]=value;total+=value;}
        if(!(total>1e-7f)){weights[4]=1f;total=1f;}for(var index=0;index<10;index++)weights[index]/=total;
        var materialLand=Smooth(.45f,.55f,landMask);var vegetation=(weights[3]+.28f*weights[4])*materialLand;var soil=(weights[2]+.55f*weights[4]+.65f*weights[9])*materialLand;var beach=weights[1]*materialLand;var rock=(weights[6]+.35f*weights[9])*materialLand;var alpineWeight=weights[7]*materialLand;var desert=weights[5]*materialLand;var snowWeight=weights[8]*materialLand;var materialTotal=vegetation+soil+beach+rock+alpineWeight+desert+snowWeight;
        if(!(materialTotal>1e-7f))return new(0,1,0,0,0,0,0);return new(vegetation/materialTotal,soil/materialTotal,beach/materialTotal,rock/materialTotal,alpineWeight/materialTotal,desert/materialTotal,snowWeight/materialTotal);
    }

    private static float Saturate(float value)=>Math.Clamp(value,0f,1f);
    private static float Smooth(float start,float end,float value){var t=Saturate((value-start)/(end-start));return t*t*(3f-2f*t);}

    private static void VerifyFrequencyAndCost()
    {
        var globalL0Spacing = EarthRadius*Math.PI*.5d/(PlanetaryTerrainDefinition.GridResolution);
        var globalL2Spacing = globalL0Spacing/4d;
        var anchoredFinestSpacing = EarthRadius*Math.PI*.5d/(1 << PlanetaryDynamicAnchoredSurface.MaximumLevel) /
            (PlanetaryDynamicAnchoredSurface.GpuBaseGridResolution *
             PlanetaryDynamicAnchoredSurface.GpuMaximumTessellationFactor);
        Require(PlanetaryPhysicalSurface.TiledWavelengthMetres/globalL0Spacing >= 3.9d &&
            PlanetaryPhysicalSurface.TiledWavelengthMetres/globalL2Spacing >= 15d &&
            PlanetaryPhysicalSurface.ErosionWavelengthMetres/anchoredFinestSpacing >= 4d,
            "global shaping is representable by L0-L2 while sub-source detail is representable by the bounded GPU-refined hierarchy");

        var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(FloridaLaunchSite.Latitude*Math.PI/180d,
            FloridaLaunchSite.Longitude*Math.PI/180d);
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < 100_000; index++) PlanetaryPhysicalSurface.EvaluateModifiers(direction, 5d);
        stopwatch.Stop();
        Require(stopwatch.Elapsed.TotalMilliseconds < 2_000d,
            $"bounded modifier cost: {stopwatch.Elapsed.TotalMilliseconds:F3} ms/100k");
        Console.WriteLine($"7H modifier foundation: generation=0x{PlanetaryPhysicalSurface.EarthGeneration.DeterministicHash:X16}; " +
            $"tiled={PlanetaryPhysicalSurface.TiledAmplitudeMetres:R}m/{PlanetaryPhysicalSurface.TiledWavelengthMetres:R}m; " +
            $"erosion={PlanetaryPhysicalSurface.ErosionAmplitudeMetres:R}m/{PlanetaryPhysicalSurface.ErosionWavelengthMetres:R}m/" +
            $"R{PlanetaryPhysicalSurface.ErosionFootprintRadiusMetres:R}m; gradientError={maximumGradientPlaceholder:F0}; " +
            $"cost100k={stopwatch.Elapsed.TotalMilliseconds:F3}ms; allocations=0");
    }

    private const double maximumGradientPlaceholder = 0d;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
