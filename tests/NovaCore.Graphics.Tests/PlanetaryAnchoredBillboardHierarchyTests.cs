using System.Diagnostics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Time;

internal static class PlanetaryAnchoredBillboardHierarchyTests
{
    private const double EarthRadiusMetres = 6_378_137d;

    public static void Run()
    {
        var root = new ReferenceFrameId(0x7E01);
        Require(SolarSystemScene.TryCreateAt(root, SimulationInstant.Zero, out var sceneValue,
            out var error) && sceneValue is not null, $"Florida proof scene: {error}");
        var scene = sceneValue!; var site = scene.FloridaLaunchSite;
        var footprintWatch = Stopwatch.StartNew();
        var footprint = PlanetaryAnchoredBillboardFootprint.AroundAnchor(site.Object.Anchor, 8);
        var hierarchy = new PlanetaryAnchoredBillboardHierarchy(footprint);
        footprintWatch.Stop();
        VerifyIdentityAndHierarchy(site, footprint, hierarchy);
        VerifyDemandReadinessOwnership(hierarchy);
        VerifyFailureFallbacks(footprint);
        VerifyRetirementAndReplay(footprint);
        VerifyCrossFaceEdgeAndCorner(site.Object.Anchor.TerrainAuthorityVersion);
        VerifyCameraAndFloridaStability(scene, site, footprint, hierarchy);
        VerifyStableAllocation(hierarchy);
        VerifyPriorContracts(site);

        var tier0 = hierarchy.Topology(0); var tier1 = hierarchy.Topology(1);
        var tier2 = hierarchy.Topology(2);
        Console.WriteLine($"11B-7E hierarchy: footprint=0x{footprint.DeterministicHash:X16}; " +
            $"tiers=0x{hierarchy.Identity(0).DeterministicHash:X16}/0x{hierarchy.Identity(1).DeterministicHash:X16}/0x{hierarchy.Identity(2).DeterministicHash:X16}; " +
            $"topology=0x{tier0.DeterministicHash:X16}/0x{tier1.DeterministicHash:X16}/0x{tier2.DeterministicHash:X16}; " +
            $"patches={hierarchy.Patches(0).Length}/{hierarchy.Patches(1).Length}/{hierarchy.Patches(2).Length}; " +
            $"vertices={tier0.Vertices.Length}/{tier1.Vertices.Length}/{tier2.Vertices.Length}; " +
            $"extentMetres={footprint.ApproximateDiameterMetres(EarthRadiusMetres):F3}; " +
            $"persistentBytes={hierarchy.PersistentBytes}; prepareMs={footprintWatch.Elapsed.TotalMilliseconds:F3}; " +
            $"coverage={hierarchy.HasCompleteCoverage}; liveRenderer=unchanged");
    }

    private static void VerifyIdentityAndHierarchy(in FloridaLaunchSite site,
        PlanetaryAnchoredBillboardFootprint footprint, PlanetaryAnchoredBillboardHierarchy hierarchy)
    {
        var replay = PlanetaryAnchoredBillboardFootprint.AroundAnchor(site.Object.Anchor, 8);
        var replayHierarchy = new PlanetaryAnchoredBillboardHierarchy(replay);
        Require(footprint.DeterministicHash == replay.DeterministicHash,
            "same physical Florida anchor produces the same footprint");
        for (var tier = 0; tier < 3; tier++)
        {
            Require(hierarchy.Identity(tier) == replayHierarchy.Identity(tier) &&
                hierarchy.Topology(tier).DeterministicHash == replayHierarchy.Topology(tier).DeterministicHash,
                $"tier {tier} identity and topology are deterministic");
            if (tier > 0) Require(PlanetaryAnchoredBillboardHierarchy.ExactReplacement(
                hierarchy.Patches(tier - 1), hierarchy.Patches(tier)),
                $"tier {tier} is an exact four-child replacement");
            VerifyOutwardWinding(hierarchy.Topology(tier));
        }
        Require(hierarchy.Patches(0).Length == 1 && hierarchy.Patches(1).Length == 4 &&
            hierarchy.Patches(2).Length == 16, "bounded T0/T1/T2 patch hierarchy");
        Require(footprint.MaximumAngularRadiusRadians > 0d &&
            footprint.ApproximateDiameterMetres(EarthRadiusMetres) > 0d,
            "Florida footprint exposes finite physical extent");
    }

    private static void VerifyDemandReadinessOwnership(PlanetaryAnchoredBillboardHierarchy hierarchy)
    {
        var edge = Edge(hierarchy.Topology(0));
        var demand = PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(edge, 32d, 16d, 4);
        Require(hierarchy.Request(demand) == 1 && hierarchy.ActiveOwnerTier == 0 &&
            hierarchy.State(1) == PlanetaryAnchoredBillboardTierState.Demanded,
            "A demand alone does not retire the parent");
        hierarchy.BeginPreparation(1);
        hierarchy.MarkPatchResourcesReady(1, 0, PlanetaryAnchoredBillboardResource.Complete);
        Require(!hierarchy.TryPromote() && hierarchy.ActiveOwnerTier == 0 &&
            hierarchy.Telemetry.IncompleteChildCount == 3 && hierarchy.HasCompleteCoverage,
            "partial preparation retains complete parent coverage");
        for (var patch = 1; patch < hierarchy.Patches(1).Length; patch++)
            hierarchy.MarkPatchResourcesReady(1, patch, PlanetaryAnchoredBillboardResource.Complete);
        var transactionWatch = Stopwatch.StartNew();
        Require(hierarchy.TryPromote(), "complete child set promotes atomically");
        transactionWatch.Stop();
        Require(hierarchy.ActiveOwnerTier == 1 && hierarchy.State(0) ==
            PlanetaryAnchoredBillboardTierState.Ready && hierarchy.State(1) ==
            PlanetaryAnchoredBillboardTierState.Authoritative && hierarchy.HasCompleteCoverage,
            "post-promotion ownership has one complete owner");
        Console.WriteLine($"11B-7E promotion transaction: {transactionWatch.Elapsed.TotalMicroseconds:F3} us");
    }

    private static void VerifyFailureFallbacks(PlanetaryAnchoredBillboardFootprint footprint)
    {
        var failures = new[]
        {
            PlanetaryAnchoredBillboardFailure.MissingChild,
            PlanetaryAnchoredBillboardFailure.PhysicalHeight,
            PlanetaryAnchoredBillboardFailure.PhysicalNormals,
            PlanetaryAnchoredBillboardFailure.Visibility,
            PlanetaryAnchoredBillboardFailure.Synchronization
        };
        foreach (var failure in failures)
        {
            var hierarchy = new PlanetaryAnchoredBillboardHierarchy(footprint);
            hierarchy.RequestTier(1); hierarchy.BeginPreparation(1);
            for (var patch = 0; patch < hierarchy.Patches(1).Length - 1; patch++)
                hierarchy.MarkPatchResourcesReady(1, patch, PlanetaryAnchoredBillboardResource.Complete);
            hierarchy.FailPatch(1, hierarchy.Patches(1).Length - 1, failure);
            Require(!hierarchy.TryPromote() && hierarchy.ActiveOwnerTier == 0 &&
                hierarchy.HasCompleteCoverage && hierarchy.Telemetry.ParentFallbackCount > 0,
                $"{failure} retains the parent without a hole");
        }
        var pending = new PlanetaryAnchoredBillboardHierarchy(footprint);
        pending.RequestTier(1); pending.BeginPreparation(1);
        for (var patch = 0; patch < pending.Patches(1).Length; patch++)
            pending.MarkPatchResourcesReady(1, patch,
                PlanetaryAnchoredBillboardResource.Complete &
                ~PlanetaryAnchoredBillboardResource.Synchronization);
        Require(!pending.TryPromote() && pending.ActiveOwnerTier == 0 &&
            pending.Telemetry.IncompleteChildCount == 4,
            "synchronization-pending children cannot take ownership");
    }

    private static void VerifyRetirementAndReplay(PlanetaryAnchoredBillboardFootprint footprint)
    {
        var first = new PlanetaryAnchoredBillboardHierarchy(footprint);
        var ownerSequence = new List<int> { first.ActiveOwnerTier };
        Promote(first, 1); ownerSequence.Add(first.ActiveOwnerTier);
        Promote(first, 2); ownerSequence.Add(first.ActiveOwnerTier);
        first.RequestTier(0);
        var retirementWatch = Stopwatch.StartNew();
        Require(first.TryRetire() && first.ActiveOwnerTier == 1 && first.HasCompleteCoverage,
            "first coarsening transaction is atomic"); ownerSequence.Add(first.ActiveOwnerTier);
        Require(first.TryRetire() && first.ActiveOwnerTier == 0 && first.HasCompleteCoverage,
            "second coarsening transaction is atomic"); ownerSequence.Add(first.ActiveOwnerTier);
        retirementWatch.Stop();
        Console.WriteLine($"11B-7E retirement transactions: " +
            $"{retirementWatch.Elapsed.TotalMicroseconds / 2d:F3} us average");
        Promote(first, 1); ownerSequence.Add(first.ActiveOwnerTier);

        var second = new PlanetaryAnchoredBillboardHierarchy(footprint);
        var replay = new List<int> { second.ActiveOwnerTier };
        Promote(second, 1); replay.Add(second.ActiveOwnerTier);
        Promote(second, 2); replay.Add(second.ActiveOwnerTier);
        second.RequestTier(0); Require(second.TryRetire(), "replay first retirement"); replay.Add(second.ActiveOwnerTier);
        Require(second.TryRetire(), "replay second retirement"); replay.Add(second.ActiveOwnerTier);
        Promote(second, 1); replay.Add(second.ActiveOwnerTier);
        Require(ownerSequence.SequenceEqual(replay), "repeated promotion/coarsening sequence is deterministic");
    }

    private static void VerifyCrossFaceEdgeAndCorner(in TerrainAuthorityVersion terrain)
    {
        var edgeDirection = RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, 1d, 0d));
        Require(SurfaceAnchor.TryCreate(6, terrain, edgeDirection, 0d, out var edgeAnchor) ==
            SurfaceAnchorCreationStatus.Success, "cube-edge SurfaceAnchor");
        Require(PlanetaryAnchoredMeshAnchor.TryCreate(edgeAnchor, out var edgeMeshAnchor),
            "cube-edge mesh anchor");
        Span<PlanetarySurfacePatchId> edgeRoots = stackalloc PlanetarySurfacePatchId[2]
        {
            new(6, terrain.Version, CubeSphereFace.PositiveX, 0, 0, 0),
            new(6, terrain.Version, CubeSphereFace.PositiveY, 0, 0, 0)
        };
        var edge = new PlanetaryAnchoredBillboardHierarchy(
            PlanetaryAnchoredBillboardFootprint.Create(edgeMeshAnchor, edgeRoots));
        Require(edge.Topology(0).Vertices.Length == 45 &&
            PlanetaryAnchoredBillboardHierarchy.ExactReplacement(edge.Patches(0), edge.Patches(1)),
            "two cube faces share one canonical five-vertex edge");
        Promote(edge, 1); Promote(edge, 2); Require(edge.HasCompleteCoverage,
            "edge footprint promotes without chart-local holes");

        var cornerDirection = RelaxedCubeSphereProjection.UnitDirectionFromCubeSurfacePoint(new(1d, 1d, 1d));
        Require(SurfaceAnchor.TryCreate(6, terrain, cornerDirection, 0d, out var cornerAnchor) ==
            SurfaceAnchorCreationStatus.Success, "cube-corner SurfaceAnchor");
        Require(PlanetaryAnchoredMeshAnchor.TryCreate(cornerAnchor, out var cornerMeshAnchor),
            "cube-corner mesh anchor");
        Span<PlanetarySurfacePatchId> cornerRoots = stackalloc PlanetarySurfacePatchId[3]
        {
            new(6, terrain.Version, CubeSphereFace.PositiveX, 0, 0, 0),
            new(6, terrain.Version, CubeSphereFace.PositiveY, 0, 0, 0),
            new(6, terrain.Version, CubeSphereFace.PositiveZ, 0, 0, 0)
        };
        var corner = new PlanetaryAnchoredBillboardHierarchy(
            PlanetaryAnchoredBillboardFootprint.Create(cornerMeshAnchor, cornerRoots));
        var canonicalCorner = PlanetaryAnchoredMeshVertexId.Create(1, 1, 1, 1);
        var cornerOccurrences = 0;
        foreach (ref readonly var vertex in corner.Topology(0).Vertices)
            if (vertex == canonicalCorner) cornerOccurrences++;
        Require(cornerOccurrences == 1,
            "three-face corner has one canonical vertex identity");
        Promote(corner, 1); Promote(corner, 2); Require(corner.HasCompleteCoverage,
            "three-face corner promotes without a geographic ownership gap");
    }

    private static void VerifyCameraAndFloridaStability(SolarSystemScene scene,
        in FloridaLaunchSite site, PlanetaryAnchoredBillboardFootprint footprint,
        PlanetaryAnchoredBillboardHierarchy hierarchy)
    {
        var identity = footprint.DeterministicHash; var topology = hierarchy.Topology(2).DeterministicHash;
        var anchor = footprint.Anchor.SurfaceAnchor; var camera = new CameraState(
            new FramePosition(scene.FocusedBody.Position.Frame, scene.FocusedBody.Position.Value),
            DoubleQuaternion.Identity, scene.Projection, CameraMode.Free);
        foreach (var focus in new[] { NativePresentationFocus.Earth, NativePresentationFocus.Mars,
                     NativePresentationFocus.Earth })
        {
            Require(scene.Focus(camera, focus), $"focus {focus}");
            camera.Position = new(camera.Position.Frame, camera.Position.Value +
                new Double3(718_231.25d, -351_991.5d, 96_173.75d));
            camera.Orientation = (DoubleQuaternion.FromAxisAngle(Double3.UnitY, .43d) *
                DoubleQuaternion.FromAxisAngle(Double3.UnitX, -.27d)).Normalized();
            Require(PlanetaryAnchoredBillboardFootprint.AroundAnchor(anchor, 8).DeterministicHash == identity &&
                new PlanetaryAnchoredBillboardHierarchy(
                    PlanetaryAnchoredBillboardFootprint.AroundAnchor(anchor, 8)).Topology(2).DeterministicHash == topology,
                "camera/focus changes do not mutate tier geography");
        }
        var terrain = new PlanetaryPhysicalTerrainAuthority(6,
            PlanetaryTerrainDefinition.EarthProductionCubeV5);
        Require(terrain.TrySampleHeight(6, site.Object.Anchor.NormalizedBodyFixedDirection, out var before) &&
            terrain.TrySampleHeight(6, footprint.Anchor.BodyFixedDirection, out var after) &&
            BitConverter.DoubleToInt64Bits(before) == BitConverter.DoubleToInt64Bits(after),
            "Florida physical height remains bit-identical");
        Require(SurfaceEnuFrame.TryCreate(site.Object.Anchor, out var beforeEnu) &&
            SurfaceEnuFrame.TryCreate(footprint.Anchor.SurfaceAnchor, out var afterEnu) && beforeEnu == afterEnu,
            "Florida ENU orientation remains exact");
    }

    private static void VerifyStableAllocation(PlanetaryAnchoredBillboardHierarchy hierarchy)
    {
        _ = hierarchy.Telemetry; _ = hierarchy.ActivePatches.Length;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = 0;
        for (var frame = 0; frame < 10_000; frame++)
        {
            var telemetry = hierarchy.Telemetry;
            checksum ^= telemetry.ActiveOwnerTier ^ hierarchy.ActivePatches.Length;
        }
        var allocations = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocations == 0 && checksum == 0, "stable ownership telemetry is allocation-free");
    }

    private static void VerifyPriorContracts(in FloridaLaunchSite site)
    {
        Require(PlanetaryAnchoredMeshTierId.TryCreate(site.Object.Anchor, 2, 8, out var tier) &&
            tier.DeterministicHash == 0x55A75314ECE5FB2Bul &&
            PlanetaryAnchoredMeshReferenceTopology.Create(tier).DeterministicHash == 0x3621FBFD89675DD4ul,
            "11B-7A hashes remain unchanged");
        Require(PlanetaryDormantDisplacedMesh.Create().DeterministicHash == 0x7F3262E7C37D781Bul,
            "11B-7C displaced topology remains unchanged");
        var topology = PlanetaryDormantDisplacedMesh.Create();
        var edge = PlanetaryAnchoredMeshEdgeId.Create(topology.Vertices[0], topology.Vertices[1]);
        Require(PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(edge, 73.125d, 12d, 64).BoundedFactor == 7,
            "11B-7D subdivision demand remains unchanged");
        Require(PlanetaryPatchTopology.Shared.DeterministicHash == 0x98792D7EBC45FF6Dul &&
            PlanetaryProductionPatchTopology.Shared.DeterministicHash == 0x61C28B0A3B4F21FFul,
            "production topology hashes remain unchanged");
    }

    private static void Promote(PlanetaryAnchoredBillboardHierarchy hierarchy, int tier)
    {
        hierarchy.RequestTier(tier);
        if (hierarchy.State(tier) is PlanetaryAnchoredBillboardTierState.Demanded or
            PlanetaryAnchoredBillboardTierState.Failed)
        {
            hierarchy.BeginPreparation(tier); hierarchy.PrepareAll(tier);
        }
        Require(hierarchy.TryPromote() && hierarchy.ActiveOwnerTier == tier && hierarchy.HasCompleteCoverage,
            $"tier {tier} atomic promotion");
    }

    private static PlanetaryAnchoredMeshEdgeId Edge(PlanetaryAnchoredBillboardTopology topology) =>
        PlanetaryAnchoredMeshEdgeId.Create(topology.Vertices[0], topology.Vertices[1]);

    private static void VerifyOutwardWinding(PlanetaryAnchoredBillboardTopology topology)
    {
        for (var index = 0; index < topology.Indices.Length; index += 3)
        {
            var p0 = topology.Vertices[(int)topology.Indices[index]].BodyFixedDirection;
            var p1 = topology.Vertices[(int)topology.Indices[index + 1]].BodyFixedDirection;
            var p2 = topology.Vertices[(int)topology.Indices[index + 2]].BodyFixedDirection;
            Require(Double3.Dot(Double3.Cross(p1 - p0, p2 - p0),
                (p0 + p1 + p2).Normalized()) > 0d, "anchored tier winding is outward");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
