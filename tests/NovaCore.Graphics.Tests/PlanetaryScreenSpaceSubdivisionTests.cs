using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

internal static class PlanetaryScreenSpaceSubdivisionTests
{
    private const double EarthRadiusMetres = 6_378_137d;

    public static void Run()
    {
        var baseMesh = PlanetaryReferenceDisplacedMesh.Create();
        Require(baseMesh.DeterministicHash == 0x7F3262E7C37D781Bul,
            "reference topology hash remains deterministic");
        var expected = new[] { (Factor: 1, Vertices: 98, Indices: 576),
            (Factor: 2, Vertices: 386, Indices: 2304),
            (Factor: 4, Vertices: 1538, Indices: 9216) };
        foreach (var value in expected)
        {
            var mesh = PlanetaryReferenceDisplacedMesh.Create(value.Factor);
            var replay = PlanetaryReferenceDisplacedMesh.Create(value.Factor);
            Require(mesh.Vertices.Length == value.Vertices && mesh.Indices.Length == value.Indices &&
                mesh.AdjacencyCount == value.Indices, $"factor {value.Factor} bounded topology counts");
            Require(mesh.DeterministicHash == replay.DeterministicHash,
                $"factor {value.Factor} topology is deterministic");
            VerifyWinding(mesh); VerifyCanonicalSharing(mesh);
        }

        var first = baseMesh.Vertices[(int)baseMesh.FaceVertex(CubeSphereFace.PositiveZ, 0, 0)];
        var second = baseMesh.Vertices[(int)baseMesh.FaceVertex(CubeSphereFace.PositiveZ,
            PlanetaryReferenceDisplacedMesh.ProofQuadsPerFaceSide, 0)];
        var edgeForward = PlanetaryAnchoredMeshEdgeId.Create(first, second);
        var edgeReverse = PlanetaryAnchoredMeshEdgeId.Create(second, first);
        var projection = new CameraProjection(Math.PI / 3d, 16d / 9d, 0.1d, 1e12d);
        var camera = new Double3(0d, 0d, EarthRadiusMetres * 3d);
        var firstPosition = first.BodyFixedDirection * EarthRadiusMetres;
        var secondPosition = second.BodyFixedDirection * EarthRadiusMetres;
        var demandForward = PlanetaryScreenSpaceSubdivision.Project(edgeForward, firstPosition,
            secondPosition, Double3.Zero, camera, DoubleQuaternion.Identity, projection, 1080d, 16d, 64);
        var demandReverse = PlanetaryScreenSpaceSubdivision.Project(edgeReverse, secondPosition,
            firstPosition, Double3.Zero, camera, DoubleQuaternion.Identity, projection, 1080d, 16d, 64);
        Require(demandForward == demandReverse && demandForward.BoundedFactor > 0,
            "canonical endpoint reversal produces identical projected demand");

        VerifyAllCubeEdges(baseMesh, projection);
        VerifyStability(edgeForward);
        VerifyFlorida(projection);
        PrintDistanceMatrix(baseMesh, projection);
        VerifyDebugIsolation();
        Require(PlanetaryPatchTopology.Shared.DeterministicHash == 0x98792D7EBC45FF6Dul &&
            PlanetaryProductionPatchTopology.Shared.DeterministicHash == 0x61C28B0A3B4F21FFul,
            "production topology hashes remain unchanged");
    }

    private static void VerifyDebugIsolation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sample = File.ReadAllText(Path.Combine(root, "samples", "NovaCore.Triangle", "Program.cs"));
        var vertex = File.ReadAllText(Path.Combine(root, "native", "NovaCore.Native", "shaders", "planetary.vert"));
        Require(sample.Contains("ColorA=-1", StringComparison.Ordinal) &&
            sample.Contains("options.Scene==\"planetary-subdivision-diagnostic\"", StringComparison.Ordinal) &&
            vertex.Contains("bool subdivisionDebug=p.color.a<0.0", StringComparison.Ordinal) &&
            vertex.Contains("subdivisionDebug?ProductionProjectGridD", StringComparison.Ordinal),
            "relaxed-cube density projection is reachable only through the explicit subdivision diagnostic");
        Require(!sample.Contains("earth?.Patches??CreateDiagnosticPatches(true)", StringComparison.Ordinal) &&
            !sample.Contains("sol?.DistantBodies??CreateDiagnosticPatches(true)", StringComparison.Ordinal),
            "normal Earth and Solar submission never enable the subdivision diagnostic marker");
    }

    private static void VerifyAllCubeEdges(PlanetaryReferenceDisplacedMesh mesh,
        in CameraProjection projection)
    {
        var groups = new Dictionary<PlanetaryAnchoredMeshEdgeId, List<(Double3 A, Double3 B)>>();
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
        {
            var r = mesh.QuadsPerFaceSide;
            Add(mesh.FaceVertex(face, 0, 0), mesh.FaceVertex(face, r, 0));
            Add(mesh.FaceVertex(face, 0, r), mesh.FaceVertex(face, r, r));
            Add(mesh.FaceVertex(face, 0, 0), mesh.FaceVertex(face, 0, r));
            Add(mesh.FaceVertex(face, r, 0), mesh.FaceVertex(face, r, r));
        }
        Require(groups.Count == 12 && groups.Values.All(values => values.Count == 2),
            "all twelve canonical cube edges have two incident owners");
        uint maximumMismatch = 0;
        foreach (var (id, owners) in groups)
        {
            var first = PlanetaryScreenSpaceSubdivision.Project(id, owners[0].A, owners[0].B,
                Double3.Zero, new(2.1d * EarthRadiusMetres, 1.3d * EarthRadiusMetres,
                    3.4d * EarthRadiusMetres), DoubleQuaternion.Identity, projection, 1440d, 8d, 64);
            var second = PlanetaryScreenSpaceSubdivision.Project(id, owners[1].B, owners[1].A,
                Double3.Zero, new(2.1d * EarthRadiusMetres, 1.3d * EarthRadiusMetres,
                    3.4d * EarthRadiusMetres), DoubleQuaternion.Identity, projection, 1440d, 8d, 64);
            maximumMismatch = Math.Max(maximumMismatch,
                (uint)Math.Abs((long)first.BoundedFactor - second.BoundedFactor));
        }
        Require(maximumMismatch == 0, "maximum cube-edge factor mismatch is zero");

        void Add(uint a, uint b)
        {
            var va = mesh.Vertices[(int)a]; var vb = mesh.Vertices[(int)b];
            var id = PlanetaryAnchoredMeshEdgeId.Create(va, vb);
            var pa = va.BodyFixedDirection * EarthRadiusMetres;
            var pb = vb.BodyFixedDirection * EarthRadiusMetres;
            if (!groups.TryGetValue(id, out var values)) { values = []; groups.Add(id, values); }
            values.Add((pa, pb));
        }
    }

    private static void VerifyStability(in PlanetaryAnchoredMeshEdgeId edge)
    {
        const double target = 16d; const uint maximum = 64; uint factor = 4;
        var changes = 0; var prior = factor;
        for (var sample = 0; sample < 240; sample++)
        {
            var length = 64d + Math.Sin(sample * Math.PI / 12d) * 0.8d;
            var desired = PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(edge, length, target, maximum).BoundedFactor;
            factor = PlanetaryScreenSpaceSubdivision.Stabilize(desired, factor, length, target, maximum);
            if (factor != prior) changes++; prior = factor;
        }
        Require(changes == 0 && factor == 4, "12.5% factor hysteresis rejects sub-pixel threshold chatter");
        var raised = PlanetaryScreenSpaceSubdivision.Stabilize(5, factor, 70d, target, maximum);
        var committed = PlanetaryScreenSpaceSubdivision.Stabilize(5, factor, 74d, target, maximum);
        Require(raised == 4 && committed == 5, "factor promotion occurs only beyond the bounded margin");
    }

    private static void VerifyFlorida(in CameraProjection projection)
    {
        Require(FloridaLaunchSite.TryCreate(6, EarthRadiusMetres,
            PlanetaryTerrainDefinition.EarthProductionCubeV5, out var site), "Florida site authority");
        Require(PlanetaryAnchoredMeshTierId.TryCreate(site.Object.Anchor, 2, 8, out var tier),
            "Florida anchored tier");
        var topology = PlanetaryAnchoredMeshReferenceTopology.Create(tier);
        var edge = topology.EdgeIdentity(PlanetaryPatchEdge.PositiveU);
        var a = edge.First.BodyFixedDirection * EarthRadiusMetres;
        var b = edge.Second.BodyFixedDirection * EarthRadiusMetres;
        var camera = site.Object.Anchor.NormalizedBodyFixedDirection * (EarthRadiusMetres + 50_000d);
        var first = PlanetaryScreenSpaceSubdivision.Project(edge, a, b, Double3.Zero, camera,
            DoubleQuaternion.Identity, projection, 1440d, 8d, 64);
        var second = PlanetaryScreenSpaceSubdivision.Project(edge, b, a, Double3.Zero, camera,
            DoubleQuaternion.Identity, projection, 1440d, 8d, 64);
        Require(first == second, "Florida-local edge demand is endpoint-symmetric");
        Require(tier.IsValid && topology.DeterministicHash ==
            PlanetaryAnchoredMeshReferenceTopology.Create(tier).DeterministicHash,
            "Florida anchored identity/topology is deterministic without camera input");
    }

    private static void PrintDistanceMatrix(PlanetaryReferenceDisplacedMesh mesh,
        in CameraProjection projection)
    {
        var edges = new Dictionary<PlanetaryAnchoredMeshEdgeId, (Double3 First, Double3 Second)>();
        for (var triangle = 0; triangle < mesh.Indices.Length; triangle += 3)
            for (var corner = 0; corner < 3; corner++)
            {
                var first = mesh.Vertices[(int)mesh.Indices[triangle + corner]];
                var second = mesh.Vertices[(int)mesh.Indices[triangle + (corner + 1) % 3]];
                var edge = PlanetaryAnchoredMeshEdgeId.Create(first, second);
                edges.TryAdd(edge, (edge.First.BodyFixedDirection * EarthRadiusMetres,
                    edge.Second.BodyFixedDirection * EarthRadiusMetres));
            }
        var distances = new[] { 100_000_000d, 20_000_000d, 3_000_000d, 700_000d, 100_000d, 10_000d, 10d };
        var policies = new[] { 32d, 16d, 8d, 4d };
        foreach (var altitude in distances)
        {
            var camera = new Double3(0d, 0d, EarthRadiusMetres + altitude);
            for (var policy = 0; policy < policies.Length; policy++)
            {
                uint minimum = uint.MaxValue, maximum = 0; ulong sum = 0;
                foreach (var (edge, endpoints) in edges)
                {
                    var factor = PlanetaryScreenSpaceSubdivision.Project(edge, endpoints.First,
                        endpoints.Second, Double3.Zero, camera, DoubleQuaternion.Identity,
                        projection, 1440d, policies[policy], 64).BoundedFactor;
                    minimum = Math.Min(minimum, factor); maximum = Math.Max(maximum, factor); sum += factor;
                }
                var materialized = maximum <= 1 ? 1 : maximum <= 2 ? 2 : 4;
                Console.WriteLine($"Subdivision demand: altitude={altitude:R}m; target={policies[policy]:R}px; " +
                    $"edges={edges.Count}; min={minimum}; max={maximum}; avg={sum / (double)edges.Count:F3}; " +
                    $"boundedProofFactor={materialized}; boundedProofTriangles={192 * materialized * materialized}");
            }
        }
    }

    private static void VerifyWinding(PlanetaryReferenceDisplacedMesh mesh)
    {
        for (var index = 0; index < mesh.Indices.Length; index += 3)
        {
            var p0 = mesh.Vertices[(int)mesh.Indices[index]].BodyFixedDirection;
            var p1 = mesh.Vertices[(int)mesh.Indices[index + 1]].BodyFixedDirection;
            var p2 = mesh.Vertices[(int)mesh.Indices[index + 2]].BodyFixedDirection;
            Require(Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) > 0d,
                $"factor {mesh.SubdivisionFactor} triangle {index / 3} is outward");
        }
    }

    private static void VerifyCanonicalSharing(PlanetaryReferenceDisplacedMesh mesh)
    {
        var occurrences = new int[mesh.Vertices.Length];
        foreach (CubeSphereFace face in Enum.GetValues<CubeSphereFace>())
            for (var y = 0; y <= mesh.QuadsPerFaceSide; y++)
                for (var x = 0; x <= mesh.QuadsPerFaceSide; x++)
                    occurrences[mesh.FaceVertex(face, x, y)]++;
        Require(occurrences.Count(value => value == 3) == 8 &&
            occurrences.Count(value => value == 2) == 12 * (mesh.QuadsPerFaceSide - 1),
            $"factor {mesh.SubdivisionFactor} edges/corners retain one canonical owner");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
