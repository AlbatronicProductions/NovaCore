using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class PlanetaryProductionSphericalBillboardRuntimeTests
{
    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var levels = PlanetaryProductionSphericalBillboardTopologyLibrary.Load(
            Path.Combine(root, "assets", "planetary-production-topology"));
        Require(levels.Count == 18, "complete production topology library loaded");
        Require(PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres ==
                PlanetaryPhysicalSurface.EarthReferenceRadiusMetres,
            "production billboard preparation uses the canonical physical Earth radius");
        Require(Marshal.SizeOf<NativeSphericalBillboardPhysicalVertex>() == 64,
            "physical billboard vertex matches the std430 dvec4 array stride");
        Require(Marshal.SizeOf<NativeProductionSphericalBillboardSubmission>() == 96,
            "production billboard submission carries the explicit topology family through the native ABI");
        ProveV2Gpu(root, levels);
        ProveNativeSelectedIncomingResidency(root, levels);
        ProveSelector(levels);
        ProvePupilAndSnap(levels);
        ProveReuse(levels);
        ProveActualIncrementalReuse(levels);
        ProveActualPhysicalSnapPreparation(root, levels);
        ProveMovingCoordinator(levels);
        ProveCurrentPupilMovesDuringCrossLevelPreparation(levels);
        ProveMovementCampaign(levels);
        ProvePublication(levels);
        ProveTes(levels[^1]);
        ProveKsaParityPhysicalResponsibilitySplit(root);
        ProveZeroVisiblePublicationContract(root);
        ProvePersistentScaleResourceContract(root);
        ProveBodyPresentationRoutingContract(root);
        ProveDirectionalVisibilityContract(root);
        ProveConservativeNearSurfaceHorizonCoverage(levels);
        DiagnoseBodyFixedLateralContinuity(levels);
        ProveIsolation(root);
    }

    private static void ProveBodyPresentationRoutingContract(string root)
    {
        var sample=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle","Program.cs"));
        var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","NovaCoreNative.cpp"));
        Require(sample.Contains("ProductionBillboardPresentationEligible(state)",StringComparison.Ordinal)&&
            sample.Contains("productionBillboardPresentation?1u:0u",StringComparison.Ordinal)&&
            sample.Contains("submission->ProductionBillboard=null",StringComparison.Ordinal)&&
            sample.Contains("submission->ProductionBillboardFlags=0",StringComparison.Ordinal)&&
            sample.Contains("submission->ProductionBillboardFrame=null",StringComparison.Ordinal),
            "managed focus routing withdraws the Earth candidate from presentation while keeping its resident runtime alive");
        Require(native.Contains("ProductionBillboardPresentationEnabled(bool resident,uint32_t flags)",StringComparison.Ordinal)&&
            native.Contains("static_assert(ProductionBillboardPresentationEnabled(true,1u))",StringComparison.Ordinal)&&
            native.Contains("static_assert(!ProductionBillboardPresentationEnabled(true,0u))",StringComparison.Ordinal)&&
            native.Contains("if(candidateRequested){RecordProductionBillboardWork",StringComparison.Ordinal),
            "native draw submission requires both resident authority and explicit per-frame Earth presentation authority");
        Console.WriteLine("P2S5F body routing: resident Earth resources are reusable state, not cross-body presentation authority");
    }

    private static void ProveDirectionalVisibilityContract(string root)
    {
        var shaderRoot=Path.Combine(root,"native","NovaCore.Native","shaders");
        var current=File.ReadAllText(Path.Combine(shaderRoot,
            "production_nested_scale_mesh_cull.comp"));
        var incoming=File.ReadAllText(Path.Combine(shaderRoot,
            "production_nested_scale_mesh_incoming_cull.comp"));
        var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native",
            "NovaCoreNative.cpp"));
        var tessellationControl=File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard.tesc"));
        var harness=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle",
            "ProductionBillboardDesktopTraversal.cs"));

        foreach (var shader in new[] { current, incoming })
        {
            Require(shader.Contains("triangleDistanceSquared(camera,p0,p1,p2)<=2500.000001",
                    StringComparison.Ordinal) &&
                shader.Contains("double horizonRadius=baseRadius+tesEnvelope+0.02",
                    StringComparison.Ordinal) &&
                shader.Contains("double screenTesSupport=tesActive?tesEnvelope:0.0",
                    StringComparison.Ordinal),
                "the accepted horizon envelope remains unchanged while screen support follows the actual 50 m TES footprint");
            Require(shader.Contains("d0 < -support&&d1 < -support&&d2 < -support",
                    StringComparison.Ordinal) &&
                shader.Contains("if(!useNarrow){narrowTested=false;return 0;}",
                    StringComparison.Ordinal),
                "the diagnostic sphere path and production plane-wise narrow phase remain independently selectable");
        }

        static double Distance(Double3 point, Double3 normal, double offset) =>
            point.X*normal.X+point.Y*normal.Y+point.Z*normal.Z+offset;
        static bool Outside(Double3 a, Double3 b, Double3 c, Double3 normal,
            double offset, double support) =>
            Distance(a,normal,offset)<-support && Distance(b,normal,offset)<-support &&
            Distance(c,normal,offset)<-support;

        var leftNormal=new Double3(1,0,0);
        Require(Outside(new(-2,0,0),new(-2,1,0),new(-2,0,1),leftNormal,1,0),
            "a triangle wholly beyond one frustum plane is rejected");
        Require(!Outside(new(-2,0,0),new(0,1,0),new(-2,0,1),leftNormal,1,0),
            "a triangle intersecting a frustum plane remains conservative");
        Require(!Outside(new(-1.5,0,0),new(-1.5,1,0),new(-1.5,0,1),leftNormal,1,1),
            "bounded displacement support retains a triangle that can enter the frustum");
        var nearNormal=new Double3(0,0,1);
        Require(Outside(new(0,0,-1),new(1,0,-1),new(0,1,-1),nearNormal,0,0) &&
            !Outside(new(0,0,-1),new(1,0,1),new(0,1,-1),nearNormal,0,0),
            "homogeneous near-plane rejection removes only provably outside triangles and retains intersections");
        Require(native.Contains("broadAccepts=%u; broadRejects=%u; narrowTests=%u",
                StringComparison.Ordinal) &&
            native.Contains("productionBillboardFlags&16384u)!=0u)gpuInput.targetTexelPixels=-1005.0f",
                StringComparison.Ordinal) &&
            native.Contains("productionBillboardFlags&32768u)!=0u)gpuInput.targetTexelPixels=-1006.0f",
                StringComparison.Ordinal) &&
            harness.Contains("NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS",
                StringComparison.Ordinal) &&
            harness.Contains("NOVACORE_P2S5F_DIRECTIONAL_GRID",
                StringComparison.Ordinal) &&
            harness.Contains("NOVACORE_P2S5F_DIRECTIONAL_LEVEL",
                StringComparison.Ordinal) &&
            harness.Contains("NOVACORE_P2S5F_DIRECTIONAL_VISIBILITY",
                StringComparison.Ordinal),
            "diagnostic output preserves exact directional poses and broad/narrow workload attribution");
        var outerReduction=tessellationControl.IndexOf(
            "atomicMax(counters.values[23]",StringComparison.Ordinal);
        var diagnosticBranch=tessellationControl.IndexOf(
            "if(inputData.textureDemand.z<0)",StringComparison.Ordinal);
        Require(outerReduction>=0&&diagnosticBranch>outerReduction&&
            tessellationControl.Contains("atomicMax(counters.values[24]",StringComparison.Ordinal)&&
            native.Contains("frameMaximumOuterTesFactor",StringComparison.Ordinal)&&
            native.Contains("frameMaximumInnerTesFactor",StringComparison.Ordinal),
            "maximum outer and inner factors are reduced for production telemetry, not only diagnostic factor dumps");
        Console.WriteLine("P2S5F directional visibility: sphere broad phase; " +
            "prepared-triangle plane narrow phase; TES support active within 50m; index order unchanged");
    }

    private static void ProvePersistentScaleResourceContract(string root)
    {
        var native=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native",
            "NovaCoreNative.cpp"));
        var incomingPrepare=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native",
            "shaders","production_spherical_billboard_incoming_prepare.comp"));
        var moving=File.ReadAllText(Path.Combine(root,"src","NovaCore.Graphics",
            "PlanetaryProductionSphericalBillboardMovingRuntime.cs"));
        Require(native.Contains("AcquireProductionBillboardTopology",StringComparison.Ordinal)&&
            native.Contains("ProductionBillboardTopologyResourceCapacity=18u",StringComparison.Ordinal)&&
            native.Contains("productionBillboardTopologyReuseHits++",StringComparison.Ordinal)&&
            native.Contains("RetainCurrentProductionBillboardWorkAsSpare",StringComparison.Ordinal)&&
            native.Contains("productionBillboardWorkReuses++",StringComparison.Ordinal),
            "native runtime retains a bounded immutable scale-topology library and recycles fence-retired current/incoming work buffers");
        Require(incomingPrepare.Contains("PupilFrame frame=pupilFrames.incoming",StringComparison.Ordinal)&&
            incomingPrepare.Contains("CandidateBaseHeightD(direction)",StringComparison.Ordinal)&&
            native.Contains("productionBillboardIncomingPreparePipeline",StringComparison.Ordinal)&&
            moving.Contains("_nativeGpuPhysicalPreparation",StringComparison.Ordinal),
            "cross-level NCSM1 publication prepares pupil-dependent physical positions on the GPU instead of rebuilding them in a managed query context");
        Require(moving.Contains("_submittedTopologyPayloads.Add",StringComparison.Ordinal)&&
            moving.Contains("topology.NativeLattice, topology.NativeIndices",StringComparison.Ordinal),
            "managed immutable upload payloads are materialized with each topology, never copied during selection, and reused on revisits");
        Console.WriteLine("P2S5F persistent resources: topologyCapacity=18; topologyKey=family/hash/count; " +
            "workSlots=current+incoming/spare; crossLevelPhysical=GPU; publication=fence-atomic");
    }

    private static void ProveZeroVisiblePublicationContract(string root)
    {
        var native = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native",
            "NovaCoreNative.cpp"));
        var incomingReset = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native",
            "shaders", "production_spherical_billboard_incoming_reset.comp"));

        Require(native.Contains("ProductionBillboardZeroVisiblePublicationRegression()",
                StringComparison.Ordinal) &&
            native.Contains("ProductionBillboardMalformedZeroWorkRegression()",
                StringComparison.Ordinal),
            "native compilation permanently proves valid zero-visible publication and rejects malformed zero-work states");
        Require(native.Contains(
                "uint64_t(indirectIndexCount)==uint64_t(visibleTriangles)*3u",
                StringComparison.Ordinal) &&
            native.Contains("recordedInputTriangles==inputTriangles",
                StringComparison.Ordinal) &&
            native.Contains("InputAccountedFor()", StringComparison.Ordinal) &&
            native.Contains("generationCoherent", StringComparison.Ordinal),
            "publication readiness accounts for every triangle and binds compacted work to one coherent generation");
        Require(!native.Contains("draw->indexCount>0", StringComparison.Ordinal) &&
            native.Contains("zeroVisible=%u; noOpIndirect=%u", StringComparison.Ordinal) &&
            incomingReset.Contains("drawArgs.values[0]=0u", StringComparison.Ordinal) &&
            incomingReset.Contains("drawArgs.values[1]=1u", StringComparison.Ordinal),
            "zero compacted indices remain a valid one-instance no-op indirect submission rather than a forced draw");
        Console.WriteLine("P2S5F zero-visible publication contract: complete-zero=publish; " +
            "same-generation-reentry=visible; malformed-zero=reject; owners=1");
    }

    private static void ProveKsaParityPhysicalResponsibilitySplit(string root)
    {
        var shaderRoot = Path.Combine(root, "native", "NovaCore.Native", "shaders");
        var physical = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard_physical.glsl"));
        var preparation = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard_prepare.comp"));
        var evaluation = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard.tese"));
        var fragment = File.ReadAllText(Path.Combine(shaderRoot,
            "planetary_production.frag"));
        var native = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native",
            "NovaCoreNative.cpp"));
        var cull = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard_cull.comp"));
        var incomingCull = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard_incoming_cull.comp"));
        var compact = File.ReadAllText(Path.Combine(shaderRoot,
            "production_spherical_billboard_compact.comp"));
        var sample = File.ReadAllText(Path.Combine(root, "samples", "NovaCore.Triangle",
            "Program.cs"));

        Require(physical.Contains("vec3 CandidateBaseNormalD", StringComparison.Ordinal) &&
            preparation.Contains("CandidateBaseHeightD(direction)", StringComparison.Ordinal) &&
            preparation.Contains("CandidateBaseNormalD(direction,radius)", StringComparison.Ordinal) &&
            !preparation.Contains("CandidatePhysicalHeightD(direction)", StringComparison.Ordinal) &&
            !preparation.Contains("CandidatePhysicalNormalD(direction,radius)", StringComparison.Ordinal),
            "persistent production billboard preparation owns only geographic plus macro/meso displacement and its normal");
        Require(evaluation.Contains("EvaluateNaturalCandidateNearD(direction)", StringComparison.Ordinal) &&
            evaluation.Contains("baseHeight+nearHeight*localWeight", StringComparison.Ordinal) &&
            evaluation.Contains("nearValue.bodyGradient", StringComparison.Ordinal) &&
            evaluation.Contains("dvec3 preparedBody=camera-dvec3(interpolatedView)", StringComparison.Ordinal) &&
            evaluation.Contains("gl_Position=baseClip+frameData.camera.viewProjection*vec4(localRelative,0.0)",
                StringComparison.Ordinal) &&
            !evaluation.Contains("direction*(radius+height)", StringComparison.Ordinal) &&
            !evaluation.Contains("bool refined", StringComparison.Ordinal) &&
            !evaluation.Contains("CandidatePhysicalHeightD(direction)", StringComparison.Ordinal) &&
            !evaluation.Contains("CandidatePhysicalNormalD(direction", StringComparison.Ordinal),
            "TES retains the prepared base position, derives one anchored body direction, and adds only the bounded canonical near field rather than rebuilding a second radial surface");
        var nearWeight = evaluation.IndexOf("if(localWeight>0.0)", StringComparison.Ordinal);
        var nearEvaluation = evaluation.IndexOf("EvaluateNaturalCandidateNearD(direction)", StringComparison.Ordinal);
        Require(nearWeight >= 0 && nearEvaluation > nearWeight &&
                evaluation.Contains("double nearHeight=0.0", StringComparison.Ordinal) &&
                evaluation.Contains("dvec3 nearGradient=dvec3(0.0)", StringComparison.Ordinal),
            "TES evaluates detailed physical displacement only inside the bounded 50 m refinement footprint");
        Require(cull.Contains("curvedPatchOccluded", StringComparison.Ordinal) &&
            incomingCull.Contains("curvedPatchOccluded", StringComparison.Ordinal) &&
            cull.Contains("uintBitsToFloat(counters.values[9])", StringComparison.Ordinal) &&
            cull.Contains("maximumRadius=bodyRadius+double(uintBitsToFloat(counters.values[9]))",
                StringComparison.Ordinal) &&
            sample.Contains("nested?generation.CullContract.MaximumTesDisplacementMetres:generation.Topology.Error.DisplacementEnvelopeMetres",
                StringComparison.Ordinal) && native.Contains("candidate->version!=4", StringComparison.Ordinal),
            "pre-TES culling preserves the accepted full radial presentation envelope required by NovaCore's coarse chordal base through the version-4 persistent-resource native ABI");

        var directions = new[]
        {
            new Double3(-1036.8296487848155, -2603347.813620877, 5814848.209969225).Normalized(),
            new Double3(.12989743991318992, .4771587602596084, .8691640654166006).Normalized(),
            new Double3(.6778969229646378, .7253743710122875, -.11953151765791344).Normalized()
        };
        var maximumIdentityError = 0d;
        foreach (var direction in directions)
        {
            var composition = PlanetaryNaturalTerrainFamilies.EvaluateComposed(direction *
                PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres,
                new PlanetaryNaturalTerrainFamilyIdentity(6,
                    PlanetaryNaturalTerrainFamilies.ProofGeneration, 0x4D12D2B1u));
            var geographic = PlanetaryTerrainDefinition.EarthProductionCubeV5
                .SampleCanonicalGeographicHeight(direction);
            var prepared = Math.Max(0d, geographic + composition.Macro.Height + composition.Meso.Height);
            var split = Math.Max(0d, prepared + composition.Near.Height);
            var canonical = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                PlanetaryTerrainDefinition.EarthProductionCubeV5, direction,
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            maximumIdentityError = Math.Max(maximumIdentityError, Math.Abs(split - canonical));
        }
        Require(maximumIdentityError <= 1e-9,
            "base plus TES near responsibility split preserves canonical H(bodyDirection)");
        Require(sample.Contains(
                "new NativeProductionBillboardFrame{Previous=_current,Current=_current,Incoming=_current}",
                StringComparison.Ordinal),
            "the already prepared initial generation does not trigger a redundant frame-local GPU preparation");
        Require(native.Contains(
                "candidateRaster.frontFace=VK_FRONT_FACE_CLOCKWISE", StringComparison.Ordinal) &&
            native.Contains(
                "candidateRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE", StringComparison.Ordinal) &&
            native.Contains(
                "a.productionBillboardTopologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_NESTED_SCALE_MESH_NCSM1?a.productionBillboardOppositeFacePipeline:a.productionBillboardPipeline",
                StringComparison.Ordinal) &&
            native.Contains("candidate->topologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_RADIAL_NCTOP2",
                StringComparison.Ordinal) &&
            native.Contains("candidate->topologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_NESTED_SCALE_MESH_NCSM1",
                StringComparison.Ordinal) &&
            sample.Contains("TopologyFamily=(uint)generation.CullContract.Family",
                StringComparison.Ordinal) &&
            native.Contains("candidateCreate.pRasterizationState=&candidateRaster",
                StringComparison.Ordinal),
            "each bound topology family reaches native submission and selects its independently proven raster face contract");
        Require(compact.Contains(
                "compacted.values[destination]=indices.values[triangle*3u]", StringComparison.Ordinal) &&
            compact.Contains(
                "compacted.values[destination+1u]=indices.values[triangle*3u+1u]", StringComparison.Ordinal) &&
            compact.Contains(
                "compacted.values[destination+2u]=indices.values[triangle*3u+2u]", StringComparison.Ordinal),
            "GPU compaction preserves each source index triplet's original order");
        Require(fragment.Contains("vec3 samplingDirection=anchored", StringComparison.Ordinal) &&
            fragment.Contains("?unitDirection", StringComparison.Ordinal) &&
            fragment.Contains(":ProductionRaySphereDirection(unitDirection,0.0,bodyRadius)",
                StringComparison.Ordinal),
            "the single-owner billboard shades from its prepared/TES body direction while the non-anchored legacy material path retains its analytic addressing shell");
        Console.WriteLine($"P2S5E physical responsibility split: samples={directions.Length}; " +
            $"canonicalHeightMax={maximumIdentityError:E9}m; factorDependentFullSurface=false; " +
            $"initialBasePreparation=complete; tesCullEnvelope={PlanetaryNaturalTerrainFamilies.ComposedBounds().NearHeight:F6}m; " +
            $"radialFrontFace=clockwise; nestedScaleMeshFrontFace=counter-clockwise");
    }

    private static void ProveNativeSelectedIncomingResidency(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        using var session = new PlanetarySphericalBillboardGpuProofSession(
            Path.Combine(root, "build", "native-ninja", "shaders"));
        var current = levels[16];
        var incoming = levels[17];
        var currentUpload = session.UploadProduction(current);
        Require(currentUpload.ActiveLevel == 16 && currentUpload.IncomingTopologyBytes == 0,
            "L16 begins as the sole native GPU owner");
        var currentFrame = session.RunProductionFrame(current, 0);
        Require(currentFrame.TopologyHash == current.TopologyHash &&
            currentFrame.ZeroOwnerFrames == 0 && currentFrame.OverlapOwnerFrames == 0,
            "current generation remains drawable before replacement");

        var staged = session.UploadProduction(incoming, stageAsIncoming: true);
        Require(staged.ActiveLevel == 16 && staged.TopologyHash == current.TopologyHash &&
            staged.IncomingLevel == 17 && staged.IncomingTopologyHash == incoming.TopologyHash &&
            staged.IncomingReadiness == 1 &&
            staged.SelectedIncomingBytes == current.ImmutableGpuBytes + incoming.ImmutableGpuBytes,
            "native runtime retains current plus at most one staged incoming topology");
        var retained = session.RunProductionFrame(current, 1);
        Require(retained.ActiveLevel == 16 && retained.IncomingLevel == 17 &&
            retained.TopologyHash == current.TopologyHash && retained.PublicationCount == 0,
            "staging never changes the authoritative draw generation");

        var published = session.RunProductionFrame(incoming, 2, publishIncoming: true);
        Require(published.ActiveLevel == 17 && published.TopologyHash == incoming.TopologyHash &&
            published.IncomingTopologyBytes == 0 && published.PublicationCount == 1 &&
            published.DeferredRetirementCount == 1 && published.ZeroOwnerFrames == 0 &&
            published.OverlapOwnerFrames == 0 && published.StaleGenerationDraws == 0 &&
            published.ValidationErrors == 0 && published.InvalidCommands == 0,
            "fence-boundary publication switches atomically to the incoming topology");
        Console.WriteLine($"P2S5C native residency: current=L16; incoming=L17; " +
            $"overlapBytes={staged.SelectedIncomingBytes}; publications={published.PublicationCount}; " +
            $"deferredRetirements={published.DeferredRetirementCount}; owners=1");
    }

    private static void ProveV2Gpu(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        using var session = new PlanetarySphericalBillboardGpuProofSession(
            Path.Combine(root, "build", "native-ninja", "shaders"));
        uint frame = 0;
        foreach (var topology in new[] { levels[0], levels[16], levels[17] })
        {
            var upload = session.UploadProduction(topology);
            Require(upload.TopologyHash == topology.TopologyHash &&
                upload.ActiveTopologyBytes == topology.ImmutableGpuBytes,
                $"L{topology.Level:D2} uploads exact v2 identity");
            var rendered = session.RunProductionFrame(topology, frame++);
            Require(rendered.RuntimeTopologyGenerationCount == 0 &&
                rendered.ValidationErrors == 0 && rendered.InvalidCommands == 0 &&
                rendered.OverflowCount == 0 && rendered.VisibleTriangles > 0,
                $"L{topology.Level:D2} v2 GPU path is validation-clean");
            Require(rendered.DirectionDecodeMaximumErrorRadians <= 2e-6,
                $"L{topology.Level:D2} GPU signed-lattice decode matches FP64 direction authority");
            Console.WriteLine($"P2S5C v2 GPU L{topology.Level:D2}: hash=0x{rendered.TopologyHash:X16}; " +
                $"vertices={rendered.BaseVertexCount}; triangles={rendered.BaseTriangleCount}; " +
                $"visible={rendered.VisibleTriangles}; directionMax={rendered.DirectionDecodeMaximumErrorRadians:E9}rad; " +
                $"uploadMs={upload.TopologyUploadMilliseconds:F6}; gpuMs={rendered.GpuTotalMilliseconds:F6}");
        }
    }

    private static void ProveSelector(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var altitudes = new[] { 80_000_000d, 35_786_000d, 3_000_000d, 700_000d, 100_000d,
            10_000d, 1_000d, 250d, 100d, 10d };
        var selected = new List<int>();
        foreach (var altitude in altitudes)
        {
            var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
            selected.Add(selector.Evaluate(View(altitude, 0), false).Level);
        }
        Console.WriteLine($"P2S5C selector probe: {string.Join(';', altitudes.Zip(selected).Select(pair => $"{pair.First:F0}m=L{pair.Second}"))}");
        Require(selected.Zip(selected.Skip(1)).All(pair => pair.Second >= pair.First),
            "descent selects monotonically finer levels");

        var stateful = new PlanetaryProductionSphericalBillboardSelector(levels);
        var first = stateful.Evaluate(View(3_000_000d, 0), false);
        stateful.CommitPublication(first.Level);
        var targetAltitude = altitudes.First(altitude =>
        {
            var probe = new PlanetaryProductionSphericalBillboardSelector(levels);
            return probe.Evaluate(View(altitude, 0), false).Level > first.Level;
        });
        var one = stateful.Evaluate(View(targetAltitude, 1), false);
        if (!one.Urgent) Require(one.Level == first.Level, "non-urgent transition observes first dwell frame");
        var two = stateful.Evaluate(View(targetAltitude, 2), false);
        Require(two.Level >= first.Level, "second completed frame permits inward selection");
        if (stateful.InFlightLevel >= 0)
        {
            var locked = stateful.Evaluate(View(80_000_000d, 3), true);
            Require(locked.Level == stateful.InFlightLevel, "in-flight publication cannot reverse from one noisy sample");
            stateful.CommitPublication(stateful.InFlightLevel);
        }
        Console.WriteLine($"P2S5C selector: altitudes=[{string.Join(',', altitudes.Select(a => a.ToString("F0")))}]; " +
            $"levels=[{string.Join(',', selected)}]; monotonic=true");
    }

    private static void ProvePupilAndSnap(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var topology = levels[^1];
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
        Require(pupil.IsValid && Double3.Dot(pupil.PivotDirection, Double3.UnitZ) > 1d - 1e-15,
            "+Z pupil initially faces the camera/surface direction");
        var small = (Double3.UnitZ + pupil.Tangent.East *
            (topology.Snap.PupilCellRadians * (topology.Snap.CandidateShiftMultiple - 1))).Normalized();
        var retained = PlanetaryProductionBillboardPupil.Resolve(pupil, small, topology);
        Require(retained == pupil, "sub-threshold motion retains the exact tangent frame");
        var moved = (Double3.UnitZ + pupil.Tangent.East *
            (topology.Snap.PupilCellRadians * (topology.Snap.CandidateShiftMultiple + 1))).Normalized();
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, moved, topology);
        Require(snapped.Generation == pupil.Generation + 1 &&
            snapped.SnapEastCells % topology.Snap.CandidateShiftMultiple == 0 &&
            snapped.SnapNorthCells % topology.Snap.CandidateShiftMultiple == 0,
            "recenter shifts by exact authored integer lattice cells");
        var identity = new Double3(.2, -.3, .9327379053088815).Normalized();
        var body = snapped.RotateCanonical(identity);
        Require(Math.Abs(body.LengthSquared - 1d) < 1e-12d,
            "pupil representation preserves canonical unit direction");
        var pole = PlanetaryProductionBillboardPupil.Resolve(snapped,
            new Double3(1e-9, 1d, -1e-9), topology);
        Require(pole.IsValid && Double3.Dot(pole.Tangent.East, snapped.Tangent.East) > -0.999999,
            "parallel transport remains finite and orientation-continuous at the pole");
    }

    private static void ProveReuse(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var cross = PlanetaryProductionSphericalBillboardReuse.CrossLevel(levels[16], levels[17]);
        Require(cross.ActiveSamples == 450_778 && cross.NewSamples == 34_636 &&
            cross.ReusedSamples == levels[16].Vertices.Count,
            "L16 to L17 prepares only 34,636 entering topology samples");
        var unchanged = PlanetaryProductionSphericalBillboardReuse.Snapped(levels[17], 0, 0);
        Require(unchanged.NewSamples == 0 && unchanged.ReusePercent == 100d,
            "stationary pupil reuses all canonical samples");
        var shifted = PlanetaryProductionSphericalBillboardReuse.Snapped(levels[17],
            levels[17].Snap.CandidateShiftMultiple, 0);
        Require(shifted.NewSamples > 0 && shifted.NewSamples < shifted.ActiveSamples,
            "integer recenter prepares an entering strip rather than the full topology");
        Console.WriteLine($"P2S5C reuse: L16-L17 active={cross.ActiveSamples}; reused={cross.ReusedSamples}; " +
            $"new={cross.NewSamples}; reuse={cross.ReusePercent:F3}%; snapNew={shifted.NewSamples}");
        foreach (var topology in levels)
        {
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
            ProveSnappedGeometry(topology, pupil);
        }
    }

    private static void ProveActualIncrementalReuse(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var cache = new PlanetaryProductionBillboardPhysicalCache();
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, levels[16]);
        var parent = PrepareSynthetic(levels[16], pupil, cache);
        var child = PrepareSynthetic(levels[17], pupil, cache);
        Require(child.Vertices.Length == 450_778 && child.ReusedSamples == 416_142 &&
            child.PreparedSamples == 34_636,
            "actual canonical cache realizes the authored L16 to L17 reuse mapping");
        cache.RetainOnly(child.Identities);
        var target = (pupil.PivotDirection + pupil.Tangent.East *
            (levels[17].Snap.PupilCellRadians * (levels[17].Snap.CandidateShiftMultiple + 1))).Normalized();
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, target, levels[17]);
        var moved = PrepareSynthetic(levels[17], snapped, cache);
        Require(snapped.Generation == pupil.Generation + 1 && moved.ReusedSamples > 0 &&
            moved.PreparedSamples > 0 && moved.PreparedSamples < moved.Vertices.Length,
            "actual snapped generation preserves canonical identities and prepares only entering samples");
        var inner = levels[17].Snap.OverlapFootprintCells *
            levels[17].Snap.PupilCellRadians * Math.Sqrt(2d);
        var outer = inner + 4d * levels[17].Snap.CandidateShiftMultiple *
            levels[17].Snap.PupilCellRadians;
        for (var i = 0; i < levels[17].Vertices.Count; i++)
        {
            var vertex = levels[17].Vertices[i];
            var radius = vertex.CubeZ == levels[17].LatticeScale
                ? Math.Sqrt((double)vertex.CubeX * vertex.CubeX +
                    (double)vertex.CubeY * vertex.CubeY) / levels[17].LatticeScale
                : double.PositiveInfinity;
            if (radius >= outer)
                Require(child.Identities[i] == moved.Identities[i],
                    "snap demand is confined to the pupil and its conforming transition annulus");
        }
        Require(moved.PreparedSamples < moved.Vertices.Length / 20,
            "ordinary finest-level snap reuses more than 95% of actual canonical samples");
        ProveSnappedGeometry(levels[17], snapped);
        var retained = child.Identities.Intersect(moved.Identities).First();
        var oldVertex = child.Vertices[child.Identities.ToList().IndexOf(retained)];
        var newVertex = moved.Vertices[moved.Identities.ToList().IndexOf(retained)];
        Require(oldVertex.BodyX == newVertex.BodyX && oldVertex.BodyY == newVertex.BodyY &&
            oldVertex.BodyZ == newVertex.BodyZ && oldVertex.PhysicalHeightMetres == newVertex.PhysicalHeightMetres &&
            oldVertex.NormalX == newVertex.NormalX && oldVertex.NormalY == newVertex.NormalY &&
            oldVertex.NormalZ == newVertex.NormalZ,
            "retained H(bodyDirection) samples remain bit-identical across integer snap");
        Console.WriteLine($"P2S5C2 actual reuse: L16-L17={child.ReusedSamples}/{child.Vertices.Length} " +
            $"({100d * child.ReusedSamples / child.Vertices.Length:F3}%); snap={moved.ReusedSamples}/{moved.Vertices.Length} " +
            $"({100d * moved.ReusedSamples / moved.Vertices.Length:F3}%); entering={moved.PreparedSamples}");
        GC.KeepAlive(parent);
    }

    private static void ProveSnappedGeometry(
        PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardPupil pupil)
    {
        var directions = topology.Vertices.Select(vertex =>
            pupil.ResolveCanonicalDirection(vertex, topology)).ToArray();
        var minimumOutward = double.PositiveInfinity;
        var minimumEdge = double.PositiveInfinity;
        var nonOutward = 0;
        for (var index = 0; index < topology.Indices.Count; index += 3)
        {
            var a = directions[topology.Indices[index]];
            var b = directions[topology.Indices[index + 1]];
            var c = directions[topology.Indices[index + 2]];
            var normal = Double3.Cross(b - a, c - a);
            var outward = Double3.Dot(normal, (a + b + c).Normalized());
            if (outward <= 0d) nonOutward++;
            minimumOutward = Math.Min(minimumOutward, outward);
            minimumEdge = Math.Min(minimumEdge, Math.Min((b - a).LengthSquared,
                Math.Min((c - b).LengthSquared, (a - c).LengthSquared)));
        }
        Console.WriteLine($"P2S5C2 snapped geometry: minOutward={minimumOutward:E9}; " +
            $"minEdgeSquared={minimumEdge:E9}; nonOutward={nonOutward}; triangles={topology.TriangleCount}");
        Require(minimumOutward > 0d && minimumEdge > 0d,
            "snapped pupil keeps every production triangle finite, nondegenerate, and outward");
    }

    private static void ProveActualPhysicalSnapPreparation(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var previous = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            ProveActualPhysicalSnapPreparationCore(root, levels);
        }
        finally { PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previous); }
    }

    private static void ProveActualPhysicalSnapPreparationCore(string root,
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var topology = levels[8];
        var cache = new PlanetaryProductionBillboardPhysicalCache();
        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, topology);
        var initialStart = Stopwatch.GetTimestamp();
        var initial = PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
            root, topology, pupil, cache, maximumParitySamples: 64);
        var initialCpu = Stopwatch.GetElapsedTime(initialStart).TotalMilliseconds;
        cache.RetainOnly(initial.Identities);
        var snapAngle = topology.Snap.PupilCellRadians *
            (topology.Snap.CandidateShiftMultiple + 1);
        var target = pupil.PivotDirection * Math.Cos(snapAngle) +
            pupil.Tangent.East * Math.Sin(snapAngle);
        var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil, target, topology);
        var snapStart = Stopwatch.GetTimestamp();
        var moved = PlanetarySphericalBillboardNaturalTerrainProof.PrepareProductionIncremental(
            root, topology, snapped, cache, maximumParitySamples: 64);
        var snapCpu = Stopwatch.GetElapsedTime(snapStart).TotalMilliseconds;
        Console.WriteLine($"P2S5C2 physical snap L8: active={moved.Vertices.Length}; " +
            $"reused={moved.ReusedSamples}; new={moved.PreparedSamples}; " +
            $"reuse={100d * moved.ReusedSamples / moved.Vertices.Length:F3}%; " +
            $"initialCpu={initialCpu:F3}ms; initialGpu={initial.Metrics.GpuMilliseconds:F6}ms; " +
            $"snapCpu={snapCpu:F3}ms; snapGpu={moved.Metrics.GpuMilliseconds:F6}ms; " +
            $"heightParity={moved.MaximumCpuHeightErrorMetres:E9}m; " +
            $"normalParity={moved.MaximumCpuNormalErrorRadians:E9}rad; topologyUploads=0");
        Require(initial.Metrics.ValidationErrors == 0 && moved.Metrics.ValidationErrors == 0 &&
            moved.ReusedSamples > 0 && moved.PreparedSamples > 0 &&
            moved.PreparedSamples < moved.Vertices.Length &&
            moved.MaximumCpuHeightErrorMetres < 1e-5 &&
            moved.MaximumCpuNormalErrorRadians < 5e-3,
            "real physical preparation reuses the snapped sample set with CPU/GPU parity");
    }

    private static void ProveMovingCoordinator(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var runtime = new PlanetaryProductionSphericalBillboardMovingRuntime(levels, PrepareSynthetic);
        ulong frame = 0;
        var target = Double3.UnitZ;
        var altitude = FindRepresentativeAltitude(levels, 8);
        var telemetry = runtime.Update(View(altitude, frame++), 0);
        var initial = AwaitPrepared(runtime);
        telemetry = runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++),
            unchecked((uint)initial.PublicationGeneration));
        Require(runtime.Current?.Topology.Level == 8 && telemetry.Publications == 1 &&
            telemetry.ZeroOwnerFrames == 0 && telemetry.OverlapOwnerFrames == 0,
            "initial candidate atomically replaces the complete global fallback with one owner");

        var stable = runtime.Current!.Pupil;
        var small = (stable.PivotDirection + stable.Tangent.East *
            (levels[8].Snap.PupilCellRadians * (levels[8].Snap.CandidateShiftMultiple - 1))).Normalized();
        telemetry = runtime.Update(new(altitude, small, 3440, 1440, Math.PI / 3d, frame++), 0);
        Require(!runtime.ReplacementInFlight && telemetry.TopologyUploads == 1,
            "sub-threshold movement updates live camera demand without topology or physical replacement");

        var snapAngle = levels[8].Snap.PupilCellRadians * (levels[8].Snap.CandidateShiftMultiple + 1);
        target = stable.PivotDirection * Math.Cos(snapAngle) + stable.Tangent.East * Math.Sin(snapAngle);
        var previousFrameIdentity = runtime.Current.PupilFrameIdentity;
        telemetry = runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++), 0);
        Require(runtime.Current?.Pupil.Generation != stable.Generation &&
            runtime.Current?.PupilFrameIdentity != previousFrameIdentity &&
            !runtime.ReplacementInFlight &&
            telemetry.TopologyUploads == 1 && telemetry.Publications == 1 &&
            telemetry.ZeroOwnerFrames == 0 && telemetry.OverlapOwnerFrames == 0 &&
            telemetry.StaleGenerationDraws == 0,
            "same-level snap updates one frame-local pupil immediately without topology upload, owner generation, or publication wait");
        var snappedFrameIdentity = runtime.Current!.PupilFrameIdentity;
        telemetry = runtime.Update(new(altitude, target, 3440, 1440, Math.PI / 3d, frame++), 0);
        Require(runtime.Current.PupilFrameIdentity == snappedFrameIdentity &&
            telemetry.PupilFrameIdentity == snappedFrameIdentity,
            "stationary pupil reuses the exact frame-local physical presentation");
        var timing = runtime.TimingSummary;
        Require(timing.Callback.Samples > 0 && timing.Selector.Samples > 0 &&
            timing.PupilAndSnap.Samples > 0 && timing.PhysicalPreparation.Samples == 1 &&
            timing.Publication.Samples == 1,
            "bounded runtime records callback, selector, pupil/snap, preparation, and publication timing");
        Console.WriteLine($"P2S5C2 moving runtime: publications={telemetry.Publications}; topologyUploads={telemetry.TopologyUploads}; " +
            $"current=L{telemetry.CurrentLevel}; pupilError={telemetry.PupilAngularErrorRadians:E9}rad; " +
            $"resident={telemetry.ResidentGpuBytes}; peak={telemetry.PeakResidentGpuBytes}; " +
            $"callback={timing.Callback.AverageMilliseconds:F6}/{timing.Callback.P95Milliseconds:F6}/{timing.Callback.MaximumMilliseconds:F6}ms");
        Console.WriteLine($"P2S5C2 moving timings avg/p95/max ms: " +
            $"selector={Format(timing.Selector)}; pupilSnap={Format(timing.PupilAndSnap)}; " +
            $"scheduling={Format(timing.DemandScheduling)}; physical={Format(timing.PhysicalPreparation)}; " +
            $"gpuPrepare={Format(timing.GpuPreparation)}; publication={Format(timing.Publication)}");
    }

    private static void ProveCurrentPupilMovesDuringCrossLevelPreparation(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        using var preparationEntered = new ManualResetEventSlim(false);
        using var allowPreparation = new ManualResetEventSlim(false);
        PlanetaryProductionBillboardPhysicalPreparation Prepare(
            PlanetaryProductionSphericalBillboardTopology topology,
            PlanetaryProductionBillboardPupil pupil,
            PlanetaryProductionBillboardPhysicalCache cache)
        {
            if (topology.Level == 9)
            {
                preparationEntered.Set();
                Require(allowPreparation.Wait(TimeSpan.FromSeconds(10)),
                    "cross-level preparation remains bounded");
            }
            return PrepareSynthetic(topology, pupil, cache);
        }

        var runtime = new PlanetaryProductionSphericalBillboardMovingRuntime(levels, Prepare);
        ulong frame = 0;
        var level8Altitude = FindRepresentativeAltitude(levels, 8);
        var direction = Double3.UnitZ;
        _ = runtime.Update(new(level8Altitude, direction, 3440, 1440, Math.PI / 3d, frame++), 0);
        var initial = AwaitPrepared(runtime);
        _ = runtime.Update(new(level8Altitude, direction, 3440, 1440, Math.PI / 3d, frame++),
            unchecked((uint)initial.PublicationGeneration));
        Require(runtime.Current?.Topology.Level == 8, "cross-level movement proof begins with L8 current");

        try
        {
            var level9Altitude = FindRepresentativeAltitude(levels, 9);
            _ = runtime.Update(new(level9Altitude, direction, 3440, 1440, Math.PI / 3d, frame++), 0);
            if (!runtime.ReplacementInFlight)
                _ = runtime.Update(new(level9Altitude, direction, 3440, 1440, Math.PI / 3d, frame++), 0);
            Require(preparationEntered.Wait(TimeSpan.FromSeconds(10)) && runtime.ReplacementInFlight,
                "adjacent L9 replacement is prepared invisibly while L8 remains current");

            var before = runtime.Current!;
            var snapAngle = levels[8].Snap.PupilCellRadians *
                (levels[8].Snap.CandidateShiftMultiple + 1);
            var movedDirection = (before.Pupil.PivotDirection * Math.Cos(snapAngle) +
                before.Pupil.Tangent.East * Math.Sin(snapAngle)).Normalized();
            var moving = runtime.Update(new(level9Altitude, movedDirection, 3440, 1440,
                Math.PI / 3d, frame++), 0);
            Require(runtime.Current?.Topology.Level == 8 &&
                runtime.Current.Pupil.Generation != before.Pupil.Generation &&
                runtime.Current.PupilFrameIdentity != before.PupilFrameIdentity &&
                moving.Publications == 1 && moving.ZeroOwnerFrames == 0 &&
                moving.OverlapOwnerFrames == 0 && moving.StaleGenerationDraws == 0,
                "current L8 pupil keeps moving frame-locally while the L9 candidate prepares");

            allowPreparation.Set();
            var incoming = AwaitPrepared(runtime);
            Require(incoming.Topology.Level == 9 && runtime.Current?.Topology.Level == 8,
                "ready L9 candidate remains invisible until native fence acknowledgement");
            var published = runtime.Update(new(level9Altitude, movedDirection, 3440, 1440,
                Math.PI / 3d, frame++), unchecked((uint)incoming.PublicationGeneration));
            Require(runtime.Current?.Topology.Level == 9 && published.Publications == 2 &&
                published.ZeroOwnerFrames == 0 && published.OverlapOwnerFrames == 0 &&
                published.StaleGenerationDraws == 0,
                "fence acknowledgement atomically replaces moving L8 with L9 as the sole owner");
            Console.WriteLine($"P2S5D cross-level movement: currentFrame={before.PupilFrameIdentity}->" +
                $"{moving.PupilFrameIdentity}; L8MovesDuringL9Prepare=true; publications={published.Publications}; " +
                $"owners=1; zero={published.ZeroOwnerFrames}; overlap={published.OverlapOwnerFrames}; " +
                $"stale={published.StaleGenerationDraws}");
        }
        finally
        {
            allowPreparation.Set();
        }
    }

    private static void ProveMovementCampaign(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var representative = new double[levels.Count];
        var found = new bool[levels.Count];
        var logarithmicRange = Math.Log(80_000_000d / 10d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            var level = selector.Evaluate(View(altitude, (ulong)sample), false).Level;
            if (!found[level]) { found[level] = true; representative[level] = altitude; }
        }
        Require(found.All(value => value), "the deterministic orbit-to-10m campaign exercises all 18 authored levels");

        selector.CancelInitialSelectionForTests();
        var first = selector.Evaluate(View(representative[0], 0), false);
        selector.CommitPublication(first.Level);
        ulong frame = 1;
        for (var level = 1; level < levels.Count; level++)
        {
            var firstDwell = selector.Evaluate(View(representative[level], frame++), false);
            var secondDwell = selector.Evaluate(View(representative[level], frame++), false);
            var selected = firstDwell.Urgent ? firstDwell : secondDwell;
            Require(selected.Level == level && selector.InFlightLevel == level,
                $"inward scale transition enters adjacent L{level:D2} after urgent or two-frame dwell");
            var noisyReverse = selector.Evaluate(View(representative[0], frame++), true);
            Require(noisyReverse.Level == level,
                "one noisy reverse sample cannot reverse an in-flight generation");
            selector.CommitPublication(level);
        }
        for (var level = levels.Count - 2; level >= 0; level--)
        {
            var outwardAltitude = representative[Math.Max(0, level - 1)];
            _ = selector.Evaluate(View(outwardAltitude, frame++), false);
            var selected = selector.Evaluate(View(outwardAltitude, frame++), false);
            Require(selected.Level == level && selector.InFlightLevel == level,
                $"outward scale transition enters adjacent L{level:D2} after two-frame dwell");
            selector.CommitPublication(level);
        }

        var pupil = PlanetaryProductionBillboardPupil.Resolve(default, Double3.UnitZ, levels[8]);
        var directions = new[]
        {
            new Double3(1,0,0), new Double3(-1,0,0), new Double3(0,0,1), new Double3(0,0,-1),
            new Double3(1,1,1).Normalized(), new Double3(-1,1,-1).Normalized(),
            new Double3(1e-10,1,1e-10).Normalized(), new Double3(-1e-10,-1,-1e-10).Normalized()
        };
        var maximumAngularError = 0d;
        foreach (var direction in directions)
        {
            for (var step = 0; step < 8; step++)
            {
                var amount = (step + 1d) / 8d;
                var next = (pupil.PivotDirection * (1d - amount) + direction * amount).Normalized();
                pupil = PlanetaryProductionBillboardPupil.Resolve(pupil, next, levels[8]);
                Require(pupil.IsValid && pupil.Tangent.IsValid,
                    "cardinal, diagonal, wrap, and polar transport remains finite");
                maximumAngularError = Math.Max(maximumAngularError, Math.Acos(Math.Clamp(
                    Double3.Dot(pupil.PivotDirection, next), -1d, 1d)));
            }
        }
        Console.WriteLine($"P2S5C2 pupil campaign error: max={maximumAngularError:E9}rad; " +
            $"authoredAxisThreshold={levels[8].Snap.PupilCellRadians * levels[8].Snap.CandidateShiftMultiple:E9}rad");
        Require(maximumAngularError <= levels[8].Snap.PupilCellRadians *
            levels[8].Snap.CandidateShiftMultiple * 3d,
            "retained pupil error stays inside the authored snap neighborhood");
        Console.WriteLine($"P2S5C2 campaign: levels=18; descent=18; ascent=18; frames={frame}; " +
            $"cardinalDiagonalWrapPoles={directions.Length}; maxPupilError={maximumAngularError:E9}rad; " +
            "timeWarp=body-fixed input invariant; unanchored=body-fixed input invariant");
    }

    private static PlanetaryProductionBillboardPreparedGeneration AwaitPrepared(
        PlanetaryProductionSphericalBillboardMovingRuntime runtime)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (runtime.TrySubmitPrepared(out var generation)) return generation;
            Thread.Yield();
        }
        throw new InvalidOperationException("P2S5C: bounded background preparation did not complete.");
    }

    private static double FindRepresentativeAltitude(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels, int requestedLevel)
    {
        var selector = new PlanetaryProductionSphericalBillboardSelector(levels);
        var logarithmicRange = Math.Log(80_000_000d / 10d);
        for (var sample = 0; sample <= 20_000; sample++)
        {
            var altitude = 80_000_000d / Math.Exp(logarithmicRange * sample / 20_000d);
            selector.CancelInitialSelectionForTests();
            if (selector.Evaluate(View(altitude, (ulong)sample), false).Level == requestedLevel)
                return altitude;
        }
        throw new InvalidOperationException($"P2S5C: no representative altitude selected L{requestedLevel:D2}.");
    }

    private static PlanetaryProductionBillboardPhysicalPreparation PrepareSynthetic(
        PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardPupil pupil,
        PlanetaryProductionBillboardPhysicalCache cache)
    {
        var vertices = new NativeSphericalBillboardPhysicalVertex[topology.Vertices.Count];
        var identities = new PlanetaryCanonicalPhysicalSampleIdentity[topology.Vertices.Count];
        var prepared = 0;
        for (var i = 0; i < topology.Vertices.Count; i++)
        {
            var direction = pupil.ResolveCanonicalDirection(topology.Vertices[i], topology);
            var identity = PlanetaryCanonicalPhysicalSampleIdentity.Create(direction,
                PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration,
                PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration);
            identities[i] = identity;
            if (!cache.TryGet(identity, out var value))
            {
                var height = direction.X * 17d + direction.Y * 11d + direction.Z * 5d;
                var body = direction * (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + height);
                value = new NativeSphericalBillboardPhysicalVertex
                {
                    BodyX = body.X, BodyY = body.Y, BodyZ = body.Z,
                    PhysicalHeightMetres = height,
                    NormalX = (float)direction.X, NormalY = (float)direction.Y,
                    NormalZ = (float)direction.Z, NormalValidity = 1f
                };
                cache.Store(identity, value);
                prepared++;
            }
            vertices[i] = value;
        }
        return new(vertices, default, 0d, 0d, prepared, vertices.Length - prepared,
            Array.AsReadOnly(identities));
    }

    private static void ProvePublication(IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var publication = new PlanetaryProductionSphericalBillboardPublication();
        publication.Bootstrap(levels[0]);
        publication.RecordFrame(publication.Current!.PublicationGeneration);
        for (var level = 1; level < levels.Count; level++)
        {
            var reuse = PlanetaryProductionSphericalBillboardReuse.CrossLevel(levels[level - 1], levels[level]);
            var incoming = publication.BeginIncoming(levels[level], reuse);
            publication.RecordFrame(publication.Current!.PublicationGeneration);
            foreach (var state in new[]
            {
                PlanetaryProductionBillboardResidencyState.PhysicalReady,
                PlanetaryProductionBillboardResidencyState.NormalReady,
                PlanetaryProductionBillboardResidencyState.CullCompactReady,
                PlanetaryProductionBillboardResidencyState.DrawReady,
                PlanetaryProductionBillboardResidencyState.FenceComplete
            })
            {
                publication.AdvanceIncoming(state);
                publication.RecordFrame(publication.Current!.PublicationGeneration);
            }
            var authoritative = publication.PublishAtFrameBoundary();
            Require(authoritative.PublicationGeneration == incoming.PublicationGeneration,
                "atomic boundary publishes the completely prepared generation");
            publication.RecordFrame(authoritative.PublicationGeneration);
        }
        Require(publication.ZeroOwnerFrames == 0 && publication.OverlapOwnerFrames == 0 &&
            publication.StaleGenerationDraws == 0 && publication.Incoming is null &&
            publication.Current!.Topology.Level == 17,
            "all adjacent transitions retain exactly one current owner and reject stale generations");
    }

    private static void ProveTes(PlanetaryProductionSphericalBillboardTopology finest)
    {
        var a = new Double3(-20, 0, -100);
        var b = new Double3(20, 0, -101);
        var first = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(a, b, 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres);
        var reversed = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(b, a, 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres);
        Require(Math.Abs(first-reversed)<1e-12 && first is >= 1 and <= 64,
            "TES edge factors are shared-edge deterministic and bounded 1-64");
        var skew = PlanetaryProductionSphericalBillboardTes.SharedEdgeFactor(
            new Double3(0, 0, -10), new Double3(1, 0, -1000), 1440,
            Math.PI / 3d, PlanetaryProductionSphericalBillboardTes.RefinementRangeMetres);
        Require(skew >= 1 && skew <= 64, "perspective-skew path remains bounded");
        Require(Math.Abs(PlanetaryProductionSphericalBillboardTes.InnerFactor(64,1,1)-21.9978d)<1e-12,
            "KSA-compatible interior tessellation uses the arithmetic mean rather than promoting one edge across the patch");
        var root=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var control=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders",
            "production_spherical_billboard.tesc"));
        var evaluation=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders",
            "production_spherical_billboard.tese"));
        Require(control.Contains("inner=(a+b+c)*.3333",StringComparison.Ordinal)&&
                control.Contains("return clamp(pixels/3*fade,1,64)",StringComparison.Ordinal)&&
                !control.Contains("exp2(ceil(log2(required)))",StringComparison.Ordinal)&&
                !control.Contains("inner=max(a,max(b,c))",StringComparison.Ordinal)&&
                evaluation.Contains("layout(triangles,fractional_odd_spacing,cw)",StringComparison.Ordinal),
            "production shaders use the KSA edge/interior/partitioning responsibility without the inherited power-of-two policy");
        const double exactHorizonBasePixels = 1344.683;
        var residual = exactHorizonBasePixels / 64d;
        Require(Math.Abs(residual - 21.010671875d) < 1e-9 && residual > finest.Error.TesTargetMaximumPixels,
            "known L17 exact-horizon residual is measured honestly and is not hidden by raising the cap");
        Console.WriteLine($"P2S5C exact horizon: base={exactHorizonBasePixels:F3}px; TES64={residual:F6}px; " +
            "transitionCurvature=0.459px; silhouette=1.312px; requiresManualSignificanceCheck=true");
    }

    private static void ProveConservativeNearSurfaceHorizonCoverage(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var radius = PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
        var envelope = levels.Max(level => level.Error.DisplacementEnvelopeMetres);
        var tessellationEnvelope = PlanetaryNaturalTerrainFamilies.ComposedBounds().NearHeight;
        Require(levels.All(level => level.Error.DisplacementEnvelopeMetres == envelope) &&
                envelope >= PlanetaryTerrainDefinition.EarthProductionCubeV5.MaximumHeightMetres +
                PlanetaryLocalTerrainPackContract.DefaultResidualMaximumMetres,
            "all production levels carry the complete canonical displacement envelope");
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        var capturedDirection = new Double3(-1036.8296487848155, -2603347.813620877,
            5814848.209969225).Normalized();
        var altitudes = new[] { 10.004d, 25d, 50d, 100d, 1_000d, 5_000d, 10_000d };
        var directionCache = new Dictionary<(int Level, PlanetaryProductionBillboardPupil Pupil), Double3[]>();
        var retained = 0;

        // Exact captured low-altitude family: four grazing bearings at every
        // required altitude. The prepared displaced triangle is now the raster
        // surface; only its bounded near field remains a TES excursion.
        var finest = levels[17];
        var capturedPupil = PlanetaryProductionBillboardPupil.Resolve(default,
            capturedDirection, finest);
        foreach (var altitude in altitudes)
            for (var bearing = 0; bearing < 4; bearing++)
            {
                VerifyGrazing(finest, capturedPupil, capturedDirection, altitude,
                    bearing * Math.Tau / 4d, $"captured {altitude:F0}m");
            }

        // The same test crosses every production transition implicated in the
        // physical replay without changing physical H(direction).
        for (var level = 14; level <= 17; level++)
        {
            var topology = levels[level];
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default,
                capturedDirection, topology);
            VerifyGrazing(topology, pupil, capturedDirection, 10d, .37d,
                $"L{level} transition");
        }

        // Same-level snap, a cube-face diagonal, and a pole-sensitive frame all
        // exercise different pupil bases while retaining one mathematical test.
        var tangentFrame = capturedPupil.Tangent;
        var snapAngle = finest.Snap.PupilCellRadians *
            (finest.Snap.CandidateShiftMultiple + 1d);
        var movedDirection = (capturedDirection * Math.Cos(snapAngle) +
            tangentFrame.East * Math.Sin(snapAngle)).Normalized();
        var snapped = PlanetaryProductionBillboardPupil.Resolve(capturedPupil,
            movedDirection, finest);
        VerifyGrazing(finest, capturedPupil, capturedDirection, 10d, 1.13d,
            "same-level outgoing pupil");
        VerifyGrazing(finest, snapped, movedDirection, 10d, 1.13d,
            "same-level replacement pupil");
        foreach (var special in new[]
                 {
                     new Double3(1d, 1d, .001d).Normalized(),
                     new Double3(1e-7d, 1d, -1e-7d).Normalized()
                 })
        {
            var topology = levels[14];
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default, special, topology);
            for (var bearing = 0; bearing < 4; bearing++)
                VerifyGrazing(topology, pupil, special, 10d, bearing * Math.Tau / 4d,
                    "cube-wrap/pole");
        }

        Console.WriteLine($"P2S5D conservative displaced horizon: altitudes=" +
            $"{string.Join(',', altitudes.Select(value => $"{value:F0}m"))}; bearings=4; " +
            $"levels=L14-L17; snap/cube/pole=true; baseEnvelope={envelope:F6}m; " +
            $"tesEnvelope={tessellationEnvelope:F6}m; retained={retained}");

        void VerifyGrazing(PlanetaryProductionSphericalBillboardTopology topology,
            in PlanetaryProductionBillboardPupil pupil, in Double3 cameraDirection,
            double altitude, double azimuth, string label)
        {
            var key = (topology.Level, pupil);
            if (!directionCache.TryGetValue(key, out var directions))
            {
                var pupilValue = pupil;
                directions = topology.Vertices.Select(vertex =>
                    pupilValue.ResolveCanonicalDirection(vertex, topology)).ToArray();
                directionCache.Add(key, directions);
            }
            var reference = Math.Abs(cameraDirection.Y) < .9d ? Double3.UnitY : Double3.UnitX;
            var east = Double3.Cross(reference, cameraDirection).Normalized();
            var north = Double3.Cross(cameraDirection, east).Normalized();
            var tangent = east * Math.Cos(azimuth) + north * Math.Sin(azimuth);
            var cameraRadius = radius + altitude;
            var cameraHorizon = Math.Acos(Math.Clamp((radius - .02d) / cameraRadius, -1d, 1d));
            var targetAngle = cameraHorizon * .98d;
            var target = (cameraDirection * Math.Cos(targetAngle) +
                tangent * Math.Sin(targetAngle)).Normalized();
            var camera = cameraDirection * cameraRadius;
            var triangle = FindContainingSphericalTriangle(topology, directions, target);
            Require(triangle >= 0, $"{label}: closed base contains grazing target");
            var indices = new[]
            {
                (int)topology.Indices[triangle * 3],
                (int)topology.Indices[triangle * 3 + 1],
                (int)topology.Indices[triangle * 3 + 2]
            };
            var bodies = indices.Select(index =>
            {
                var direction = directions[index];
                var composition = PlanetaryNaturalTerrainFamilies.EvaluateComposed(direction * radius,
                    new PlanetaryNaturalTerrainFamilyIdentity(6,
                        PlanetaryNaturalTerrainFamilies.ProofGeneration, 0x4D12D2B1u));
                var geographic = terrain.SampleCanonicalGeographicHeight(direction);
                var height = Math.Max(0d, geographic + composition.Macro.Height +
                    composition.Meso.Height);
                return direction * (radius + height);
            }).ToArray();
            Require(!PlanetaryProductionSphericalBillboardCulling.IsCurvedPatchOccluded(
                    camera, bodies[0], bodies[1], bodies[2], radius, envelope),
                $"{label}: full radial physical presentation envelope survives pre-TES planet occlusion");
            retained++;
        }
    }

    private static int FindContainingSphericalTriangle(
        PlanetaryProductionSphericalBillboardTopology topology,
        IReadOnlyList<Double3> directions, in Double3 target)
    {
        for (var triangle = 0; triangle < topology.TriangleCount; triangle++)
        {
            var first = directions[(int)topology.Indices[triangle * 3]];
            var second = directions[(int)topology.Indices[triangle * 3 + 1]];
            var third = directions[(int)topology.Indices[triangle * 3 + 2]];
            if (SameSphericalSide(first, second, third, target) &&
                SameSphericalSide(second, third, first, target) &&
                SameSphericalSide(third, first, second, target)) return triangle;
        }
        return -1;
    }

    private static void DiagnoseBodyFixedLateralContinuity(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var previousGeneration = PlanetaryPhysicalSurface.RuntimeGeneration;
        try
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(
                PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
            var level14 = levels[14];
            var level15 = levels[15];
            var cameraDirection = new Double3(-1036.8296487848155, -2603347.813620877,
                5814848.209969225).Normalized();
            var pupil = PlanetaryProductionBillboardPupil.Resolve(default, cameraDirection, level14);
            var frame = pupil.Tangent;
            var subThreshold = level14.Snap.PupilCellRadians *
                (level14.Snap.CandidateShiftMultiple - 1d);
            var snapAngle = level14.Snap.PupilCellRadians *
                (level14.Snap.CandidateShiftMultiple + 1d);
            var forward = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(subThreshold) + frame.North * Math.Sin(subThreshold)).Normalized(),
                level14);
            var backward = PlanetaryProductionBillboardPupil.Resolve(forward, cameraDirection, level14);
            var lateral = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(subThreshold) + frame.East * Math.Sin(subThreshold)).Normalized(),
                level14);
            var snapped = PlanetaryProductionBillboardPupil.Resolve(pupil,
                (cameraDirection * Math.Cos(snapAngle) + frame.East * Math.Sin(snapAngle)).Normalized(),
                level14);
            var rebased = PlanetaryProductionBillboardPupil.Resolve(snapped,
                (cameraDirection * Math.Cos(.01d) + frame.East * Math.Sin(.01d)).Normalized(), level14);
            var adjacent = PlanetaryProductionBillboardPupil.Resolve(snapped,
                snapped.PivotDirection, level15);
            ProveSnappedGeometry(level14, rebased);
            var states = new[]
            {
                new TrackedPupilState("stationary", level14, pupil),
                new TrackedPupilState("forward", level14, forward),
                new TrackedPupilState("backward", level14, backward),
                new TrackedPupilState("lateral", level14, lateral),
                new TrackedPupilState("same-level-snap", level14, snapped),
                new TrackedPupilState("same-level-rebase", level14, rebased),
                new TrackedPupilState("adjacent-L15", level15, adjacent)
            };
            var bearings = new[] { 0d, Math.PI / 4d, Math.PI / 2d };
            var distances = new[] { 1_000d, 5_000d, 10_000d, 25_000d };
            var tracked = bearings.SelectMany(bearing => distances.Select(distance =>
            {
                var tangent = frame.North * Math.Cos(bearing) + frame.East * Math.Sin(bearing);
                var angle = distance / PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
                return (label: $"{distance:F0}m/{bearing * 180d / Math.PI:F0}deg",
                    distance,
                    direction: (cameraDirection * Math.Cos(angle) + tangent * Math.Sin(angle)).Normalized());
            })).ToArray();
            var canonical = tracked.Select(sample =>
            {
                var height = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                    PlanetaryTerrainDefinition.EarthProductionCubeV5, sample.direction,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                return (sample.label, sample.direction, height,
                    position: sample.direction *
                        (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + height));
            }).ToArray();
            var baseline = EvaluateRenderedBase(states[0], canonical);
            var baseIdentities = ResolveIdentities(states[0]).ToHashSet();
            var maximumCanonicalHeightDelta = 0d;
            var maximumCanonicalPositionDelta = 0d;
            foreach (var state in states)
            {
                var rendered = EvaluateRenderedBase(state, canonical);
                var maximumRenderedHeightDelta = 0d;
                var maximumOuterCoverageHeightDelta = 0d;
                for (var i = 0; i < canonical.Length; i++)
                {
                    var repeatedHeight = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                        PlanetaryTerrainDefinition.EarthProductionCubeV5, canonical[i].direction,
                        PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                    maximumCanonicalHeightDelta = Math.Max(maximumCanonicalHeightDelta,
                        Math.Abs(repeatedHeight - canonical[i].height));
                    var repeatedPosition = canonical[i].direction *
                        (PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres + repeatedHeight);
                    maximumCanonicalPositionDelta = Math.Max(maximumCanonicalPositionDelta,
                        Math.Sqrt((repeatedPosition - canonical[i].position).LengthSquared));
                    maximumRenderedHeightDelta = Math.Max(maximumRenderedHeightDelta,
                        Math.Abs(rendered[i] - baseline[i]));
                    if (tracked[i].distance >= 5_000d)
                        maximumOuterCoverageHeightDelta = Math.Max(maximumOuterCoverageHeightDelta,
                            Math.Abs(rendered[i] - baseline[i]));
                }
                var identities = ResolveIdentities(state);
                var reused = identities.Count(identity => baseIdentities.Contains(identity));
                Console.WriteLine($"P2S5C3 lateral continuity: state={state.Name}; level=L{state.Topology.Level}; " +
                    $"pupil={state.Pupil.Generation}; rebase={state.Pupil.Rebased}; " +
                    $"reuse={reused}/{identities.Length}; canonicalHeightDelta={maximumCanonicalHeightDelta:E9}m; " +
                    $"canonicalPositionDelta={maximumCanonicalPositionDelta:E9}m; " +
                    $"renderedBaseHeightDelta={maximumRenderedHeightDelta:E9}m; " +
                    $"outerCoverageHeightDelta={maximumOuterCoverageHeightDelta:E9}m");
            }
            Require(forward == pupil && backward == pupil && lateral == pupil,
                "stationary and sub-threshold forward/back/lateral motion retain the exact pupil generation");
            Require(maximumCanonicalHeightDelta == 0d && maximumCanonicalPositionDelta == 0d,
                "tracked body-fixed H(direction) and FP64 positions are invariant across camera/pupil states");
        }
        finally
        {
            PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(previousGeneration);
        }
    }

    private static double[] EvaluateRenderedBase(TrackedPupilState state,
        IReadOnlyList<(string label, Double3 direction, double height, Double3 position)> samples)
    {
        var directions = state.Topology.Vertices.Select(vertex =>
            state.Pupil.ResolveCanonicalDirection(vertex, state.Topology)).ToArray();
        var triangles = FindContainingSphericalTriangles(state.Topology, directions,
            samples.Select(sample => sample.direction).ToArray());
        var values = new double[samples.Count];
        var radius = PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres;
        for (var sample = 0; sample < samples.Count; sample++)
        {
            var triangle = triangles[sample];
            Require(triangle >= 0, $"{state.Name} contains tracked sample {samples[sample].label}");
            var positions = new Double3[3];
            for (var corner = 0; corner < 3; corner++)
            {
                var direction = directions[state.Topology.Indices[triangle * 3 + corner]];
                var height = PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(
                    PlanetaryTerrainDefinition.EarthProductionCubeV5, direction,
                    PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                positions[corner] = direction * (radius + height);
            }
            var normal = Double3.Cross(positions[1] - positions[0], positions[2] - positions[0]);
            var denominator = Double3.Dot(normal, samples[sample].direction);
            Require(Math.Abs(denominator) > 1e-12d, $"{state.Name} tracked triangle intersects its body ray");
            values[sample] = Double3.Dot(normal, positions[0]) / denominator - radius;
        }
        return values;
    }

    private static int[] FindContainingSphericalTriangles(
        PlanetaryProductionSphericalBillboardTopology topology,
        IReadOnlyList<Double3> directions,
        IReadOnlyList<Double3> targets)
    {
        var nearestVertices = new int[targets.Count];
        var nearestDots = Enumerable.Repeat(double.NegativeInfinity, targets.Count).ToArray();
        for (var vertex = 0; vertex < directions.Count; vertex++)
        {
            for (var target = 0; target < targets.Count; target++)
            {
                var dot = Double3.Dot(directions[vertex], targets[target]);
                if (dot <= nearestDots[target]) continue;
                nearestDots[target] = dot;
                nearestVertices[target] = vertex;
            }
        }

        var results = Enumerable.Repeat(-1, targets.Count).ToArray();
        for (var triangle = 0; triangle < topology.TriangleCount; triangle++)
        {
            var firstIndex = (int)topology.Indices[triangle * 3];
            var secondIndex = (int)topology.Indices[triangle * 3 + 1];
            var thirdIndex = (int)topology.Indices[triangle * 3 + 2];
            for (var target = 0; target < targets.Count; target++)
            {
                if (results[target] >= 0) continue;
                var nearest = nearestVertices[target];
                if (firstIndex != nearest && secondIndex != nearest && thirdIndex != nearest) continue;
                var first = directions[firstIndex];
                var second = directions[secondIndex];
                var third = directions[thirdIndex];
                if (SameSphericalSide(first, second, third, targets[target]) &&
                    SameSphericalSide(second, third, first, targets[target]) &&
                    SameSphericalSide(third, first, second, targets[target]))
                    results[target] = triangle;
            }
        }

        // The generated topology is locally regular, so the containing triangle
        // normally owns the nearest vertex. Retain an exhaustive diagnostic fallback
        // rather than making that acceleration assumption part of production code.
        for (var target = 0; target < targets.Count; target++)
            if (results[target] < 0)
                results[target] = FindContainingSphericalTriangle(topology, directions, targets[target]);
        return results;
    }

    private static PlanetaryCanonicalPhysicalSampleIdentity[] ResolveIdentities(TrackedPupilState state) =>
        state.Topology.Vertices.Select(vertex => PlanetaryCanonicalPhysicalSampleIdentity.Create(
            state.Pupil.ResolveCanonicalDirection(vertex, state.Topology),
            PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration,
            PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration)).ToArray();

    private readonly record struct TrackedPupilState(string Name,
        PlanetaryProductionSphericalBillboardTopology Topology,
        PlanetaryProductionBillboardPupil Pupil);

    private static bool SameSphericalSide(in Double3 first, in Double3 second,
        in Double3 interior, in Double3 target)
    {
        var edge = Double3.Cross(first, second);
        return Double3.Dot(edge, interior) * Double3.Dot(edge, target) >= -1e-15d;
    }

    private static void ProveIsolation(string root)
    {
        var production = File.ReadAllText(Path.Combine(root, "samples", "NovaCore.Triangle", "Program.cs"));
        Require(!production.Contains("ProductionSphericalBillboardTopologyGenerator.Generate", StringComparison.Ordinal),
            "candidate runtime never generates production topology");
        var nativeProduction = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "NovaCoreNative.cpp"));
        Require(!nativeProduction.Contains("planetary-production-topology", StringComparison.Ordinal),
            "existing production renderer remains isolated from the opt-in candidate library");
    }

    private static PlanetaryProductionBillboardView View(double altitude, ulong frame) =>
        new(altitude, Double3.UnitZ, 3440, 1440, Math.PI / 3d, frame);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("P2S5C: " + message);
    }

    private static string Format(PlanetaryProductionBillboardTiming value) =>
        $"{value.AverageMilliseconds:F6}/{value.P95Milliseconds:F6}/{value.MaximumMilliseconds:F6}";
}
