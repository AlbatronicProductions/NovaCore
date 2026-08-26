using System.Runtime.InteropServices;
using System.Text;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;

internal static unsafe class GpuDisplacedMeshPreparationTests
{
    private const ulong EarthBodyId = 6;
    private const double EarthRadiusMetres = 6_378_137d;
    private const ulong ExpectedTopologyHash = 0x7F3262E7C37D781Bul;

    public static void Run()
    {
        VerifyAbi(); var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthAssetId, null,
            out _, out var terrainPath, out var terrainError), $"terrain-v5 asset: {terrainError}");
        Require(TerrainAssetCache.TryResolveRequired(root, TerrainAssetCache.ProductionEarthLocalAssetId, null,
            out _, out var localPath, out var localError), $"local-v2 asset: {localError}");
        var runtime = Path.Combine(root, "assets", "earth", "runtime");
        Require(EarthElevationDataset.TryLoad(runtime, out var oracleError), $"elevation oracle: {oracleError}");
        Require(EarthLocalTerrainElevationDataset.TryLoad(localPath, out var localLoadError), $"local-v2 oracle: {localLoadError}");
        var topology = PlanetaryDormantDisplacedMesh.Create(); var replayTopology = PlanetaryDormantDisplacedMesh.Create();
        Require(topology.Vertices.Length == 98 && topology.Indices.Length == 576 && topology.AdjacencyCount == 576,
            "bounded whole-body topology is 98 vertices / 192 triangles / 576 incidences");
        Require(topology.DeterministicHash == ExpectedTopologyHash && topology.DeterministicHash == replayTopology.DeterministicHash,
            $"whole-body topology hash is stable: 0x{ExpectedTopologyHash:X16}");
        var faceOccurrences = new int[topology.Vertices.Length];
        for (var face = 0; face < 6; face++)
            for (var y = 0; y <= PlanetaryDormantDisplacedMesh.ProofQuadsPerFaceSide; y++)
                for (var x = 0; x <= PlanetaryDormantDisplacedMesh.ProofQuadsPerFaceSide; x++)
                    faceOccurrences[topology.FaceVertex((CubeSphereFace)face, x, y)]++;
        Require(faceOccurrences.Count(value => value == 3) == 8 && faceOccurrences.Count(value => value == 2) == 36 &&
            faceOccurrences.Count(value => value == 1) == 54 && faceOccurrences.All(value => value is >= 1 and <= 3),
            "all 8 cube corners and 36 interior cube-edge samples have one canonical vertex owner");
        var global = Build(topology.Vertices, topology.Indices, topology.AdjacencyWords);
        for (var triangle = 0; triangle < global.Indices.Length; triangle += 3)
        {
            var p0 = global.Directions[global.Indices[triangle]];
            var p1 = global.Directions[global.Indices[triangle + 1]];
            var p2 = global.Directions[global.Indices[triangle + 2]];
            Require(Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) > 0d,
                $"reference topology outward triangle {triangle / 3}");
        }

        Require(FloridaLaunchSite.TryCreate(EarthBodyId, EarthRadiusMetres, PlanetaryTerrainDefinition.EarthProductionCubeV5,
            out var floridaSite), "Florida canonical launch-site authority");
        var florida = floridaSite.Object.Anchor; var floridaDirection = florida.NormalizedBodyFixedDirection;
        Require(PlanetaryAnchoredMeshTierId.TryCreate(florida, 2, 8, out var floridaTier), "Florida anchored tier");
        var floridaReference = PlanetaryAnchoredMeshReferenceTopology.Create(floridaTier);
        var floridaMesh = BuildFlorida(florida, floridaReference.DeterministicHash);
        var localPackage = File.ReadAllBytes(localPath);
        Require(PlanetaryLocalTerrainPackContract.TryReadRecordHeader(
            localPackage.AsSpan(PlanetaryLocalTerrainPackContract.HeaderBytes, PlanetaryLocalTerrainPackContract.RecordHeaderBytes),
            out var firstLocalRecord), "local-v2 proof record");
        var localCells = 1 << firstLocalRecord.Sector.Level;
        var localDirection = RelaxedCubeSphereProjection.UnitDirection(firstLocalRecord.Sector.Face,
            (firstLocalRecord.Sector.X + .5d) / localCells, (firstLocalRecord.Sector.Y + .5d) / localCells);
        Require(SurfaceAnchor.TryCreate(EarthBodyId, new(2, 5), localDirection, 0d, out var localAnchor) == SurfaceAnchorCreationStatus.Success,
            "local-v2 proof anchor");
        var localMesh = BuildNeighborhood(localAnchor);

        var oraclePath = Path.Combine(runtime, "earth_elevation_8192x4096.r16");
        var displaceShader = Path.Combine(root, "build", "native-ninja", "shaders", "planetary_mesh_displace.comp.spv");
        var normalShader = Path.Combine(root, "build", "native-ninja", "shaders", "planetary_mesh_normals.comp.spv");
        var initialization = Initialize(oraclePath, terrainPath, localPath, displaceShader, normalShader,
            256, 2048, 2048); Require(initialization.InitializationCount == 1 && initialization.PipelineCreationCount == 2 &&
            initialization.ShaderModuleCreationCount == 2 && initialization.PersistentBufferBytes > 0 && initialization.ValidationErrors == 0,
            "persistent native session creates two pipelines/modules once and is validation-clean");
        try
        {
            var cameraA = new Double3(EarthRadiusMetres + 1_000_000d, 2_000d, -3_000d);
            var cameraB = new Double3(EarthRadiusMetres + 1_010_000d, -4_000d, 8_000d);
            var first = Prepare(global, cameraA, out var firstMetrics); var repeat = Prepare(global, cameraA, out var repeatMetrics);
            var moved = Prepare(global, cameraB, out var movedMetrics); var floridaResult = Prepare(floridaMesh, cameraA, out _);
            var localResult = Prepare(localMesh, cameraA, out _); var reversed = Reverse(global);
            var reversedResult = Prepare(reversed, cameraA, out _);
            Require(firstMetrics.PreparationCount == 1 && repeatMetrics.PreparationCount == 2 && movedMetrics.PreparationCount == 3 &&
                movedMetrics.PipelineCreationCount == 2 && movedMetrics.ShaderModuleCreationCount == 2 &&
                movedMetrics.PersistentBufferBytes == initialization.PersistentBufferBytes, "repeated dispatch reuses stable resources");
            Require(OutputsEqual(first, repeat), "identical dispatch is bit-stable");
            for (var index = 0; index < first.Vertices.Length; index++)
            {
                var reversedIndex = first.Vertices.Length - 1 - index;
                Require(PhysicalBitsEqual(first.Vertices[index], reversedResult.Vertices[reversedIndex]) &&
                    NormalBitsEqual(first.Normals[index], reversedResult.Normals[reversedIndex]),
                    $"body-fixed output is invariant under valid reversed input order {index}");
            }

            var terrain = new PlanetaryPhysicalTerrainAuthority(EarthBodyId, PlanetaryTerrainDefinition.EarthProductionCubeV5);
            var maxHeightError = 0d; var maxRadiusError = 0d; var maxNormalError = 0d; var maxCameraError = 0d;
            var positions = new Double3[first.Vertices.Length];
            for (var index = 0; index < first.Vertices.Length; index++)
            {
                var direction = global.Directions[index]; Require(terrain.TrySampleHeight(EarthBodyId, direction, out var height), $"CPU height {index}");
                var vertex = first.Vertices[index]; var body = Body(vertex); positions[index] = body;
                maxHeightError = Math.Max(maxHeightError, Math.Abs(vertex.PhysicalHeightMetres - height));
                maxRadiusError = Math.Max(maxRadiusError, Math.Abs(Math.Sqrt(body.LengthSquared) - (EarthRadiusMetres + height)));
                Require(vertex.Valid == 1 && first.Normals[index].Validity == 1f && Finite(body) && Finite(first.Normals[index]),
                    $"finite valid output {index}: vertex={vertex.Valid}; normal={first.Normals[index].Validity}; n=({first.Normals[index].X:R},{first.Normals[index].Y:R},{first.Normals[index].Z:R}); body={body}");
                Require(PhysicalBitsEqual(first.Vertices[index], moved.Vertices[index]) && NormalBitsEqual(first.Normals[index], moved.Normals[index]),
                    $"body-fixed output ignores camera {index}");
                var expectedCamera = body - cameraA; var actualCamera = new Double3(vertex.CameraRelativeX, vertex.CameraRelativeY, vertex.CameraRelativeZ);
                maxCameraError = Math.Max(maxCameraError, Math.Sqrt((expectedCamera - actualCamera).LengthSquared));
            }
            var cpuNormals = CpuNormals(positions, global.Indices, global.Adjacency); var maximumAdjacentNormalAngle = 0d;
            for (var index = 0; index < cpuNormals.Length; index++)
            {
                var gpu = new Double3(first.Normals[index].X, first.Normals[index].Y, first.Normals[index].Z).Normalized();
                var dot = Math.Clamp(Double3.Dot(cpuNormals[index], gpu), -1d, 1d); maxNormalError = Math.Max(maxNormalError, Math.Acos(dot));
                Require(Double3.Dot(gpu, positions[index].Normalized()) > 0d, $"outward normal {index}");
            }
            for (var triangle = 0; triangle < global.Indices.Length; triangle += 3)
                for (var edge = 0; edge < 3; edge++)
                {
                    var a = global.Indices[triangle + edge]; var b = global.Indices[triangle + (edge + 1) % 3];
                    maximumAdjacentNormalAngle = Math.Max(maximumAdjacentNormalAngle,
                        Math.Acos(Math.Clamp(Double3.Dot(cpuNormals[a], cpuNormals[b]), -1d, 1d)));
                }
            var minimumWinding = double.MaxValue;
            for (var triangle = 0; triangle < global.Indices.Length; triangle += 3)
            {
                var p0 = positions[global.Indices[triangle]]; var p1 = positions[global.Indices[triangle + 1]]; var p2 = positions[global.Indices[triangle + 2]];
                var normal = Double3.Cross(p1 - p0, p2 - p0); var outward = (p0 + p1 + p2).Normalized();
                minimumWinding = Math.Min(minimumWinding, Double3.Dot(normal, outward));
            }
            Require(minimumWinding > 0d, "every physical triangle is outward wound");
            Require(maxHeightError <= 1e-3d && maxRadiusError <= 2e-6d,
                $"physical displacement parity: FP32 diagnostic height={maxHeightError:R}; split-position radius={maxRadiusError:R}");
            Require(maxNormalError <= 5e-7d, $"GPU adjacency normal parity: {maxNormalError:R} rad");
            Require(maxCameraError <= 1d, $"camera-relative FP32 finalization remains bounded: {maxCameraError:R} m");
            Require(firstMetrics.ValidationErrors == 0 && movedMetrics.ValidationErrors == 0, "explicit synchronized dispatch has zero validation errors");

            const int floridaCenter = 4;
            var floridaVertexDirection = floridaMesh.Directions[floridaCenter];
            Require(terrain.TrySampleHeight(EarthBodyId, floridaVertexDirection, out var floridaHeight), "Florida CPU physical height");
            var floridaVertex = floridaResult.Vertices[floridaCenter]; var floridaRadial = Math.Sqrt(Body(floridaVertex).LengthSquared) - EarthRadiusMetres;
            Require(Math.Abs(floridaRadial - floridaHeight) <= 2e-6d,
                $"Florida physical displacement matches CPU authority: radial={floridaRadial:R}; cpu={floridaHeight:R}; error={Math.Abs(floridaRadial - floridaHeight):R}");
            Require(terrain.TrySampleHeight(EarthBodyId, localMesh.Directions[4], out var localHeight), "local-v2 CPU physical height");
            var localRadial = Math.Sqrt(Body(localResult.Vertices[4]).LengthSquared) - EarthRadiusMetres;
            Require(localResult.Vertices[4].SourceHasLocal == 1 && Math.Abs(localRadial - localHeight) <= 2e-6d,
                "local-v2 physical displacement matches CPU authority");
            Require(first.Vertices.Any(value => value.SourceHasLocal == 0) && localResult.Vertices.Any(value => value.SourceHasLocal == 1),
                "global-only and local-v2 source paths both participate");
            Require(PlanetaryPatchTopology.Shared.DeterministicHash == 0x98792D7EBC45FF6Dul &&
                PlanetaryProductionPatchTopology.Shared.DeterministicHash == 0x61C28B0A3B4F21FFul,
                "live topology hashes remain unchanged");
            Console.WriteLine($"GPU displaced mesh: hash=0x{topology.DeterministicHash:X16}; vertices={positions.Length}; triangles={global.Indices.Length / 3}; adjacency={topology.AdjacencyCount}; " +
                $"heightMax={maxHeightError:E17}m; radialMax={maxRadiusError:E17}m; cameraMax={maxCameraError:E17}m; normalAngleMax={maxNormalError:E17}rad; adjacentNormalMax={maximumAdjacentNormalAngle:E17}rad; sharedPositionGap=0m; sharedNormalGap=0rad; windingMin={minimumWinding:E17}; " +
                $"persistentBytes={firstMetrics.PersistentBufferBytes}; setup={firstMetrics.SetupMilliseconds:F3}ms; displacement={firstMetrics.DisplacementMilliseconds:F6}ms; normals={firstMetrics.NormalMilliseconds:F6}ms; total={firstMetrics.TotalMilliseconds:F3}ms; validation=0");
        }
        finally { Require(NativeRuntime.ShutdownPlanetaryMeshPreparation() == NativeResult.Success, "persistent mesh preparation shutdown"); }
        var fallbackInitialization = Initialize(oraclePath, terrainPath, string.Empty, displaceShader, normalShader,
            256, 2048, 2048);
        try
        {
            var fallbackResult = Prepare(localMesh, Double3.Zero, out var fallbackMetrics);
            var fallbackVertex = fallbackResult.Vertices[4];
            var fallbackRadial = Math.Sqrt(Body(fallbackVertex).LengthSquared) - EarthRadiusMetres;
            var fallbackHeight = EarthElevationDataset.SampleHeight(localMesh.Directions[4]);
            Require(fallbackInitialization.ValidationErrors == 0 && fallbackMetrics.ValidationErrors == 0,
                "local-v2-unavailable fallback is validation-clean");
            Require(fallbackVertex.SourceHasLocal == 0 && fallbackVertex.LocalResidualMetres == 0f,
                "local-v2-unavailable dispatch reports oracle-only source ownership");
            Require(Math.Abs(fallbackRadial - fallbackHeight) <= 2e-6d,
                $"local-v2-unavailable physical displacement falls back to oracle: radial={fallbackRadial:R}; oracle={fallbackHeight:R}");
        }
        finally { Require(NativeRuntime.ShutdownPlanetaryMeshPreparation() == NativeResult.Success, "fallback mesh preparation shutdown"); }
        var unavailableMetrics = new NativePlanetaryMeshPreparationMetrics { Size = (uint)Marshal.SizeOf<NativePlanetaryMeshPreparationMetrics>(), Version = 1 };
        fixed (NativePlanetaryHeightQuery* q = global.Queries) fixed (uint* i = global.Indices) fixed (uint* a = global.Adjacency)
        fixed (NativePlanetaryDisplacedVertex* v = new NativePlanetaryDisplacedVertex[global.Queries.Length]) fixed (NativePlanetaryPhysicalNormal* n = new NativePlanetaryPhysicalNormal[global.Queries.Length])
        { var dispatch = Dispatch(global, Double3.Zero); Require(NativeRuntime.PreparePlanetaryMesh(q, i, a, &dispatch, v, n, &unavailableMetrics) == NativeResult.InvalidArgument, "use-after-shutdown is rejected"); }
    }

    private static MeshInput Build(ReadOnlySpan<PlanetaryAnchoredMeshVertexId> ids, ReadOnlySpan<uint> indices, ReadOnlySpan<uint> adjacency,
        uint anchoredTier = 2)
    {
        var idArray = ids.ToArray(); var queries = new NativePlanetaryHeightQuery[idArray.Length];
        for (var index = 0; index < idArray.Length; index++) Require(PlanetaryGpuPhysicalHeightQuery.TryCreate(EarthBodyId, 5, anchoredTier,
            idArray[index].BodyFixedDirection * EarthRadiusMetres, Double3.Zero, out queries[index], out _), $"mesh query {index}");
        return new(idArray.Select(value => value.BodyFixedDirection).ToArray(), queries, indices.ToArray(), adjacency.ToArray());
    }

    private static MeshInput BuildFlorida(in SurfaceAnchor anchor, ulong retainedReferenceHash)
    {
        Require(retainedReferenceHash == 0x3621FBFD89675DD4ul, "11B-7A Florida reference topology hash retained");
        return BuildNeighborhood(anchor);
    }

    private static MeshInput BuildNeighborhood(in SurfaceAnchor anchor)
    {
        Require(SurfaceEnuFrame.TryCreate(anchor, out var enu), "Florida ENU frame");
        const int row = 3; const double spacingMetres = 100d;
        var anchorPoint = anchor.NormalizedBodyFixedDirection * EarthRadiusMetres;
        var directions = new Double3[row * row]; var queries = new NativePlanetaryHeightQuery[row * row];
        for (var y = 0; y < row; y++) for (var x = 0; x < row; x++)
        {
            var delta = enu.East * ((x - 1) * spacingMetres) + enu.North * ((y - 1) * spacingMetres);
            Require(PlanetaryGpuPhysicalHeightQuery.TryCreate(EarthBodyId, 5, 3, anchorPoint, delta,
                out queries[y * row + x], out var reconstructed), $"Florida ENU mesh query {x}/{y}");
            directions[y * row + x] = reconstructed.Normalized();
        }
        var indices = new uint[24]; var cursor = 0;
        for (var y = 0; y < row - 1; y++) for (var x = 0; x < row - 1; x++)
        {
            var lowerLeft = (uint)(y * row + x); var lowerRight = lowerLeft + 1;
            var upperLeft = lowerLeft + row; var upperRight = upperLeft + 1;
            AddOutward(lowerLeft, upperRight, lowerRight); AddOutward(lowerLeft, upperLeft, upperRight);
        }
        return new(directions, queries, indices, BuildAdjacency(directions.Length, indices));

        void AddOutward(uint a, uint b, uint c)
        {
            var p0 = directions[a]; var p1 = directions[b]; var p2 = directions[c];
            if (Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) < 0d) (b, c) = (c, b);
            indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = c;
        }
    }

    private static uint[] BuildAdjacency(int vertexCount, ReadOnlySpan<uint> indices)
    {
        var lists = new List<uint>[vertexCount]; for (var i = 0; i < lists.Length; i++) lists[i] = [];
        for (uint triangle = 0; triangle < indices.Length / 3; triangle++) for (var corner = 0; corner < 3; corner++) lists[indices[(int)triangle * 3 + corner]].Add(triangle);
        var result = new uint[vertexCount * 2 + indices.Length]; var cursor = 0;
        for (var vertex = 0; vertex < vertexCount; vertex++) { result[vertex * 2] = (uint)cursor; result[vertex * 2 + 1] = (uint)lists[vertex].Count; foreach (var triangle in lists[vertex]) result[vertexCount * 2 + cursor++] = triangle; }
        return result;
    }

    private static MeshInput Reverse(MeshInput source)
    {
        var count = source.Queries.Length; var directions = new Double3[count]; var queries = new NativePlanetaryHeightQuery[count];
        for (var index = 0; index < count; index++) { directions[count - 1 - index] = source.Directions[index]; queries[count - 1 - index] = source.Queries[index]; }
        var indices = new uint[source.Indices.Length];
        for (var index = 0; index < indices.Length; index++) indices[index] = (uint)(count - 1) - source.Indices[index];
        return new(directions, queries, indices, BuildAdjacency(count, indices));
    }

    private static NativePlanetaryMeshPreparationMetrics Initialize(string oracle, string terrain, string local,
        string displace, string normal, uint vertices, uint indices, uint adjacency)
    {
        var paths = new[] { Utf8(oracle), Utf8(terrain), Utf8(local), Utf8(displace), Utf8(normal) };
        var metrics = new NativePlanetaryMeshPreparationMetrics { Size = (uint)Marshal.SizeOf<NativePlanetaryMeshPreparationMetrics>(), Version = 1 };
        fixed (byte* p0 = paths[0]) fixed (byte* p1 = paths[1]) fixed (byte* p2 = paths[2]) fixed (byte* p3 = paths[3]) fixed (byte* p4 = paths[4])
        { var assets = new NativePlanetaryMeshPreparationAssets { Size = (uint)Marshal.SizeOf<NativePlanetaryMeshPreparationAssets>(), Version = 1, ElevationOraclePathUtf8 = p0, ProductionTerrainPathUtf8 = p1, LocalTerrainPathUtf8 = p2, DisplacementShaderPathUtf8 = p3, NormalShaderPathUtf8 = p4, MaximumVertexCount = vertices, MaximumIndexCount = indices, MaximumAdjacencyCount = adjacency }; Require(NativeRuntime.InitializePlanetaryMeshPreparation(&assets, &metrics) == NativeResult.Success, "persistent mesh preparation initializes"); }
        return metrics;
    }

    private static MeshOutput Prepare(MeshInput input, in Double3 camera, out NativePlanetaryMeshPreparationMetrics metrics)
    {
        var vertices = new NativePlanetaryDisplacedVertex[input.Queries.Length]; var normals = new NativePlanetaryPhysicalNormal[input.Queries.Length];
        var value = new NativePlanetaryMeshPreparationMetrics { Size = (uint)Marshal.SizeOf<NativePlanetaryMeshPreparationMetrics>(), Version = 1 }; var dispatch = Dispatch(input, camera);
        fixed (NativePlanetaryHeightQuery* q = input.Queries) fixed (uint* i = input.Indices) fixed (uint* a = input.Adjacency) fixed (NativePlanetaryDisplacedVertex* v = vertices) fixed (NativePlanetaryPhysicalNormal* n = normals)
            Require(NativeRuntime.PreparePlanetaryMesh(q, i, a, &dispatch, v, n, &value) == NativeResult.Success, "persistent displaced mesh dispatch");
        metrics = value; return new(vertices, normals);
    }

    private static NativePlanetaryMeshPreparationDispatch Dispatch(MeshInput input, in Double3 camera)
    {
        var encoded = EncodedPosition.Encode(camera); return new() { Size = (uint)Marshal.SizeOf<NativePlanetaryMeshPreparationDispatch>(), Version = 1,
            VertexCount = (uint)input.Queries.Length, IndexCount = (uint)input.Indices.Length, AdjacencyCount = (uint)(input.Adjacency.Length - input.Queries.Length * 2),
            TopologyVersion = PlanetaryDormantDisplacedMesh.TopologyVersion, TerrainVersion = 5, SourcePolicy = 1,
            CameraHighX = encoded.HighX, CameraHighY = encoded.HighY, CameraHighZ = encoded.HighZ,
            CameraLowX = encoded.LowX, CameraLowY = encoded.LowY, CameraLowZ = encoded.LowZ, BodyRadiusMetres = EarthRadiusMetres };
    }

    private static Double3[] CpuNormals(Double3[] positions, uint[] indices, uint[] adjacency)
    {
        var result = new Double3[positions.Length]; var baseOffset = positions.Length * 2;
        for (var vertex = 0; vertex < result.Length; vertex++) { var sum = Double3.Zero; var start = adjacency[vertex * 2]; var count = adjacency[vertex * 2 + 1]; for (var incidence = 0; incidence < count; incidence++) { var triangle = adjacency[baseOffset + start + incidence] * 3; sum += Double3.Cross(positions[indices[triangle + 1]] - positions[indices[triangle]], positions[indices[triangle + 2]] - positions[indices[triangle]]); } result[vertex] = sum.Normalized(); }
        return result;
    }

    private static bool OutputsEqual(MeshOutput a, MeshOutput b) => MemoryMarshal.AsBytes(a.Vertices.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(b.Vertices.AsSpan())) && MemoryMarshal.AsBytes(a.Normals.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(b.Normals.AsSpan()));
    private static bool PhysicalBitsEqual(NativePlanetaryDisplacedVertex a, NativePlanetaryDisplacedVertex b) { a.CameraRelativeX = b.CameraRelativeX; a.CameraRelativeY = b.CameraRelativeY; a.CameraRelativeZ = b.CameraRelativeZ; return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1)).SequenceEqual(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref b, 1))); }
    private static bool NormalBitsEqual(NativePlanetaryPhysicalNormal a, NativePlanetaryPhysicalNormal b) => MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1)).SequenceEqual(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref b, 1)));
    private static Double3 Body(in NativePlanetaryDisplacedVertex value) => new((double)value.BodyHighX + value.BodyLowX, (double)value.BodyHighY + value.BodyLowY, (double)value.BodyHighZ + value.BodyLowZ);
    private static bool Finite(in Double3 value) => double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Z);
    private static bool Finite(in NativePlanetaryPhysicalNormal value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + '\0');
    private static void VerifyAbi() { Require(Marshal.SizeOf<NativePlanetaryDisplacedVertex>() == 112 && Marshal.OffsetOf<NativePlanetaryDisplacedVertex>(nameof(NativePlanetaryDisplacedVertex.FaceU)).ToInt32() == 48 && Marshal.OffsetOf<NativePlanetaryDisplacedVertex>(nameof(NativePlanetaryDisplacedVertex.Valid)).ToInt32() == 96, "displaced ABI 112/48/96"); Require(Marshal.SizeOf<NativePlanetaryPhysicalNormal>() == 16 && Marshal.SizeOf<NativePlanetaryMeshPreparationAssets>() == 64 && Marshal.SizeOf<NativePlanetaryMeshPreparationDispatch>() == 80 && Marshal.SizeOf<NativePlanetaryMeshPreparationMetrics>() == 88, "mesh preparation ABI 16/64/80/88"); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed record MeshInput(Double3[] Directions, NativePlanetaryHeightQuery[] Queries, uint[] Indices, uint[] Adjacency);
    private sealed record MeshOutput(NativePlanetaryDisplacedVertex[] Vertices, NativePlanetaryPhysicalNormal[] Normals);
}
