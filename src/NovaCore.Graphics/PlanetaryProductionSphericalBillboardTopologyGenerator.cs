using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaCore.Core;

namespace NovaCore.Graphics;

public static class PlanetaryProductionSphericalBillboardTopologyGenerator
{
    public const int BaseFaceResolution = 9, MaximumRefinementDepth = 20;
    public const double EarthRadiusMetres = 6_371_008.8, VerticalFovRadians = Math.PI / 3d;
    public const int ViewportHeight = 1440;
    // The linear term controls the area of the finest pupil while the quadratic
    // term grades it into the outer mesh.  These values are NovaCore-authored;
    // they keep the production mesh inside the already-approved P2S3 envelope.
    private const double DensityLinear = .0195, DensityQuadratic = .05725;
    private const int MaximumProductionLeafCount = 275_000;
    private static readonly int CommonScale = BaseFaceResolution << MaximumRefinementDepth;

    public readonly record struct LevelDefinition(string Identity, int PupilDepth, int OuterDepth,
        double MaximumAltitudeMetres, double MinimumAltitudeMetres, PlanetaryProductionTesResponsibility TesResponsibility);

    public sealed record CandidateReport(string Identity,
        double MaximumBasePixels, double MaximumTransitionPixels, double MaximumSilhouettePixels,
        uint MaximumRequiredTesFactor, double LargestAdjacentDensityJump, long SerializedBytes,
        ulong MaximumSelectedAndIncomingGpuBytes, double FinestMinimumAngleDegrees, double FinestMaximumAspectRatio);

    public static IReadOnlyList<LevelDefinition> SixteenLevelDefinitions() => BuildDefinitions(Enumerable.Range(2, 15).Append(19), "N16");
    public static IReadOnlyList<LevelDefinition> EighteenLevelDefinitions() => BuildDefinitions(Enumerable.Range(2, 18), "N18");

    public static IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> GenerateProductionLibrary() => GenerateLibrary(EighteenLevelDefinitions());

    public static IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> GenerateLibrary(IReadOnlyList<LevelDefinition> definitions)
    {
        var result = new List<PlanetaryProductionSphericalBillboardTopology>(definitions.Count);
        PlanetaryProductionSphericalBillboardTopology? parent = null;
        for (var level = 0; level < definitions.Count; level++) { var generated = Generate(level, definitions[level], parent); result.Add(generated); parent = generated; }
        return result;
    }

    public static PlanetaryProductionSphericalBillboardTopology Generate(int level, LevelDefinition definition,
        PlanetaryProductionSphericalBillboardTopology? parent)
    {
        if (definition.PupilDepth is < 1 or > MaximumRefinementDepth || definition.OuterDepth is < 0 or > 3 ||
            definition.OuterDepth > definition.PupilDepth) throw new ArgumentOutOfRangeException(nameof(definition));
        var leaves = BuildLeaves(definition); var linePoints = BuildLinePoints(leaves);
        var vertices = new List<MutableVertex>(); var vertexLookup = new Dictionary<CubeKey, int>(); var indices = new List<uint>();
        foreach (var cell in leaves.OrderBy(c => c.Face).ThenBy(c => c.Depth).ThenBy(c => c.Y).ThenBy(c => c.X))
        {
            var shift = MaximumRefinementDepth - cell.Depth; var x0 = cell.X << shift; var x1 = (cell.X + 1) << shift;
            var y0 = cell.Y << shift; var y1 = (cell.Y + 1) << shift;
            var boundary = Boundary(cell.Face, x0, x1, y0, y1, linePoints);
            var center = Cube(cell.Face, -CommonScale + x0 + x1, -CommonScale + y0 + y1);
            var centerIndex = Vertex(center, cell.Depth);
            for (var i = 0; i < boundary.Count; i++)
            {
                var a = Vertex(Cube(cell.Face, -CommonScale + 2 * boundary[i].X, -CommonScale + 2 * boundary[i].Y), cell.Depth);
                var b = Vertex(Cube(cell.Face, -CommonScale + 2 * boundary[(i + 1) % boundary.Count].X, -CommonScale + 2 * boundary[(i + 1) % boundary.Count].Y), cell.Depth);
                AddTriangle(centerIndex, a, b);
            }
        }
        var immutableVertices = vertices.Select(v => new PlanetaryProductionSphericalBillboardTopology.Vertex(v.Key.X, v.Key.Y, v.Key.Z,
            checked((byte)Math.Min(255, v.Depth)), checked((byte)v.Depth))).ToArray();
        var (offsets, neighbors) = BuildAdjacency(immutableVertices.Length, indices);
        var parentMap = ParentMap(parent, vertexLookup); var mappingHash = HashMapping(parentMap);
        var metrics = Measure(immutableVertices, offsets, neighbors, definition.PupilDepth, definition.OuterDepth);
        var regions = BuildRegions(immutableVertices, indices, metrics.VertexSpacing);
        var snap = new PlanetaryProductionSphericalBillboardTopology.SnapMetadata(Hash64($"NovaCore-production-cube-tangent-v2:{BaseFaceResolution}:{MaximumRefinementDepth}"), metrics.PupilSpacing,
            definition.PupilDepth >= 17 ? 8 : 12, definition.PupilDepth >= 17 ? 96 : 64, definition.PupilDepth >= 17 ? 8 : 4);
        var error = new PlanetaryProductionSphericalBillboardTopology.ErrorMetadata(definition.MaximumAltitudeMetres, metrics.PupilSpacing,
            metrics.TransitionSpacing, metrics.OuterSpacing, PhysicalDisplacementEnvelope(), 18f, 12f, 24f, 4f, 6f,
            2d * EarthRadiusMetres * metrics.PupilSpacing, 0f, 1u, definition.TesResponsibility);
        var provisional = new PlanetaryProductionSphericalBillboardTopology(level, CommonScale, immutableVertices, indices.ToArray(), offsets,
            neighbors, regions, parentMap, snap, error, 0, mappingHash);
        var visibleErrors = MeasureVisibleScreenErrors(provisional, definition.MinimumAltitudeMetres);
        error = error with { MaximumExpectedBaseErrorPixels = (float)visibleErrors.PupilPixels,
            MaximumTesFactor = Math.Min(64u, Math.Max(1u, (uint)Math.Ceiling(visibleErrors.PupilPixels / 6d))) };
        var final = new PlanetaryProductionSphericalBillboardTopology(level, CommonScale, immutableVertices, indices.ToArray(), offsets,
            neighbors, regions, parentMap, snap, error, 0, mappingHash);
        var bytes = PlanetaryProductionSphericalBillboardTopology.Serialize(final); var hash = BitConverter.ToUInt64(bytes, 80);
        return new(level, CommonScale, immutableVertices, indices.ToArray(), offsets, neighbors, regions, parentMap, snap, error, hash, mappingHash);

        int Vertex(CubeKey key, int depth)
        {
            if (vertexLookup.TryGetValue(key, out var found)) { if (vertices[found].Depth < depth) vertices[found].Depth = depth; return found; }
            var index = vertices.Count; vertices.Add(new(key, depth)); vertexLookup.Add(key, index); return index;
        }
        void AddTriangle(int a, int b, int c)
        {
            var da = Direction(vertices[a].Key); var db = Direction(vertices[b].Key); var dc = Direction(vertices[c].Key);
            var ab = db - da; var ac = dc - da; var cross = Double3.Cross(ab, ac); var centroid = (da + db + dc).Normalized();
            if (Double3.Dot(cross, centroid) < 0d) (b, c) = (c, b);
            indices.Add((uint)a); indices.Add((uint)b); indices.Add((uint)c);
        }
    }

    public static CandidateReport Characterize(string identity, IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        var maxBase = 0d; var maxTransition = 0d; var maxSilhouette = 0d; var maxTes = 1u; var jump = 1d; long bytes = 0; ulong overlap = 0;
        for (var i = 0; i < levels.Count; i++)
        {
            var level = levels[i]; var lower = i == levels.Count - 1 ? 10d : levels[i + 1].Error.MaximumAltitudeMetres;
            if (!double.IsFinite(lower) || lower <= 0d) lower = 10d;
            var (basePixels, transition, silhouette) = MeasureVisibleScreenErrors(level, lower);
            maxBase = Math.Max(maxBase, basePixels); maxTransition = Math.Max(maxTransition, transition); maxSilhouette = Math.Max(maxSilhouette, silhouette);
            maxTes = Math.Max(maxTes, (uint)Math.Ceiling(basePixels / 6d)); bytes += PlanetaryProductionSphericalBillboardTopology.Serialize(level).Length;
            var selected = ActiveGpuBytes(level); var incoming = i + 1 < levels.Count ? ActiveGpuBytes(levels[i + 1]) : 0ul; overlap = Math.Max(overlap, selected + incoming);
            if (i > 0) jump = Math.Max(jump, levels[i - 1].Error.PupilSpacingRadians / level.Error.PupilSpacingRadians);
        }
        var quality = Quality(levels[^1]);
        return new(identity, maxBase, maxTransition, maxSilhouette, maxTes, jump, bytes, overlap, quality.MinimumAngle, quality.MaximumAspect);
    }

    public static void WriteProductionLibrary(string directory, IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        Directory.CreateDirectory(directory); var manifestLevels = new List<object>();
        foreach (var level in levels)
        {
            var file = $"production-{level.Level:D2}.nctop2"; var bytes = PlanetaryProductionSphericalBillboardTopology.Serialize(level);
            File.WriteAllBytes(Path.Combine(directory, file), bytes); manifestLevels.Add(new { level = level.Level, file, bytes = bytes.Length,
                topologyHash = $"0x{level.TopologyHash:X16}", parentMappingHash = $"0x{level.ParentMappingHash:X16}" });
        }
        var manifest = new { format = "NovaCoreProductionSphericalBillboardTopology", formatVersion = 2, generatorVersion = 1,
            coordinateEncoding = "SignedCubeLatticeInt32", levelCount = levels.Count, levels = manifestLevels };
        File.WriteAllText(Path.Combine(directory, "production-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    }

    public static double ProjectedPixels(double altitudeMetres, double angularSpacing)
    {
        var half = Math.Atan2(EarthRadiusMetres * Math.Sin(angularSpacing), EarthRadiusMetres + altitudeMetres - EarthRadiusMetres * Math.Cos(angularSpacing));
        return half / (VerticalFovRadians * .5d) * (ViewportHeight * .5d);
    }

    private static (double PupilPixels, double TransitionPixels, double SilhouettePixels) MeasureVisibleScreenErrors(
        PlanetaryProductionSphericalBillboardTopology topology, double altitudeMetres)
    {
        var directions = topology.Vertices.Select(vertex => vertex.Direction(topology.LatticeScale)).ToArray();
        var cameraRadius = EarthRadiusMetres + altitudeMetres; var horizonDot = EarthRadiusMetres / cameraRadius;
        var focalPixels = ViewportHeight / (2d * Math.Tan(VerticalFovRadians / 2d));
        var projectedRadius = focalPixels * EarthRadiusMetres / Math.Sqrt(cameraRadius * cameraRadius - EarthRadiusMetres * EarthRadiusMetres);
        var maximumDepth = topology.Regions.Max(region => region.RefinementDepth); var minimumDepth = topology.Regions.Min(region => region.RefinementDepth);
        var pupil = 0d; var transition = 0d; var silhouette = 0d;
        for (var a = 0; a < topology.Vertices.Count; a++)
        {
            for (var cursor = topology.NeighborOffsets[a]; cursor < topology.NeighborOffsets[a + 1]; cursor++)
            {
                var b = topology.Neighbors[cursor]; if (b <= a) continue;
                var da = directions[a]; var db = directions[b]; var visibleA = da.Z >= horizonDot; var visibleB = db.Z >= horizonDot;
                if (visibleA && visibleB)
                {
                    var ax = focalPixels * EarthRadiusMetres * da.X / (cameraRadius - EarthRadiusMetres * da.Z);
                    var ay = focalPixels * EarthRadiusMetres * da.Y / (cameraRadius - EarthRadiusMetres * da.Z);
                    var bx = focalPixels * EarthRadiusMetres * db.X / (cameraRadius - EarthRadiusMetres * db.Z);
                    var by = focalPixels * EarthRadiusMetres * db.Y / (cameraRadius - EarthRadiusMetres * db.Z);
                    var pixels = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                    var depth = Math.Max(topology.Vertices[a].RefinementDepth, topology.Vertices[b].RefinementDepth);
                    if (depth >= maximumDepth - 1) pupil = Math.Max(pupil, pixels);
                    else if (depth > minimumDepth)
                    {
                        var edgeAngle = Math.Acos(Math.Clamp(Double3.Dot(da, db), -1d, 1d));
                        transition = Math.Max(transition, projectedRadius * edgeAngle * edgeAngle / 8d);
                    }
                }
                if (visibleA != visibleB)
                {
                    var edgeAngle = Math.Acos(Math.Clamp(Double3.Dot(da, db), -1d, 1d));
                    silhouette = Math.Max(silhouette, projectedRadius * edgeAngle * edgeAngle / 8d);
                }
            }
        }
        return (pupil, transition, silhouette);
    }

    public static (double MinimumAngle, double FirstPercentileAngle, double FifthPercentileAngle, double MedianAngle,
        double MaximumAspect, double WorstPupilAngle, double WorstTransitionAngle, double WorstFarAngle) Quality(PlanetaryProductionSphericalBillboardTopology value)
    {
        var directions = value.Vertices.Select(v => v.Direction(value.LatticeScale)).ToArray();
        var triangleCount = value.TriangleCount; var angles = new double[value.Indices.Count]; var aspects = new double[triangleCount]; var minimumAngles = new double[triangleCount];
        Parallel.For(0, triangleCount, triangle =>
        {
            var i = triangle * 3;
            var ia = (int)value.Indices[i]; var ib = (int)value.Indices[i + 1]; var ic = (int)value.Indices[i + 2];
            var a = directions[ia]; var b = directions[ib]; var c = directions[ic];
            var ab = Math.Sqrt((b - a).LengthSquared); var bc = Math.Sqrt((c - b).LengthSquared); var ca = Math.Sqrt((a - c).LengthSquared);
            aspects[triangle] = Math.Max(ab, Math.Max(bc, ca)) / Math.Min(ab, Math.Min(bc, ca));
            var angleA = Angle(bc, ab, ca); var angleB = Angle(ca, ab, bc); var angleC = Angle(ab, bc, ca);
            angles[i] = angleA; angles[i + 1] = angleB; angles[i + 2] = angleC;
            minimumAngles[triangle] = Math.Min(angleA, Math.Min(angleB, angleC));
        });
        var maxAspect = aspects.Max(); var pupil = 180d; var transition = 180d; var far = 180d; var maximumRegionDepth = value.Regions.Max(r => r.RefinementDepth);
        for (var triangle = 0; triangle < triangleCount; triangle++)
        {
            var i = triangle * 3; var ia = (int)value.Indices[i]; var ib = (int)value.Indices[i + 1]; var ic = (int)value.Indices[i + 2];
            var min = minimumAngles[triangle]; var depth = Math.Max(value.Vertices[ia].RefinementDepth, Math.Max(value.Vertices[ib].RefinementDepth, value.Vertices[ic].RefinementDepth));
            if (depth >= maximumRegionDepth - 1) pupil = Math.Min(pupil, min); else if (depth > 3) transition = Math.Min(transition, min); else far = Math.Min(far, min);
        }
        Array.Sort(angles); double P(double q) => angles[(int)Math.Clamp(Math.Floor((angles.Length - 1) * q), 0, angles.Length - 1)];
        return (angles[0], P(.01), P(.05), P(.5), maxAspect, pupil, transition, far);
        static double Angle(double opposite, double side1, double side2) => Math.Acos(Math.Clamp(
            (side1 * side1 + side2 * side2 - opposite * opposite) / (2d * side1 * side2), -1d, 1d)) * 180d / Math.PI;
    }

    private static IReadOnlyList<LevelDefinition> BuildDefinitions(IEnumerable<int> depths, string prefix)
    {
        var values = depths.ToArray(); var maximumAltitudes = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var depth = values[i]; var nominal = 1d / (3d * (1 << depth));
            var altitude = i == 0 ? double.PositiveInfinity : EntryAltitude(nominal, 12d);
            if (i == values.Length - 1)
            {
                var previous = 1d / (3d * (1 << values[i - 1])); altitude = EntryAltitude(previous, 24d);
            }
            maximumAltitudes[i] = altitude;
        }
        var result = new List<LevelDefinition>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            var depth = values[i]; var minimumAltitude = i + 1 < values.Length ? maximumAltitudes[i + 1] : 10d;
            var outerDepth = Math.Min(1, i); var tes = i < 5 ? PlanetaryProductionTesResponsibility.None :
                i < 10 ? PlanetaryProductionTesResponsibility.ExceptionalEdges : i < values.Length - 3 ? PlanetaryProductionTesResponsibility.BoundedLocal : PlanetaryProductionTesResponsibility.NearCamera;
            result.Add(new($"{prefix}-{i:D2}-D{depth:D2}", depth, outerDepth, maximumAltitudes[i], minimumAltitude, tes));
        }
        return result;
    }

    private static double EntryAltitude(double theta, double pixels)
    {
        var focal = ViewportHeight / (2d * Math.Tan(VerticalFovRadians / 2d)); return focal * EarthRadiusMetres * theta / pixels;
    }

    private static List<Cell> BuildLeaves(LevelDefinition definition)
    {
        var leaves = new List<Cell>(); var n = BaseFaceResolution << definition.OuterDepth;
        for (var face = 0; face < 6; face++) for (var y = 0; y < n; y++) for (var x = 0; x < n; x++) Refine(new(face, definition.OuterDepth, x, y));
        return leaves;
        void Refine(Cell cell)
        {
            var cellsAtDepth = BaseFaceResolution << cell.Depth;
            var preservesCubeBoundary = cell.Depth == definition.OuterDepth &&
                (cell.X == 0 || cell.Y == 0 || cell.X == cellsAtDepth - 1 || cell.Y == cellsAtDepth - 1);
            if (cell.Face != 0 || cell.Depth >= definition.PupilDepth || preservesCubeBoundary)
            {
                leaves.Add(cell);
                if (leaves.Count > MaximumProductionLeafCount)
                    throw new InvalidOperationException($"Production topology exceeds the {MaximumProductionLeafCount:N0}-leaf capacity guard.");
                return;
            }
            var n0 = cellsAtDepth; var u = -1d + 2d * (cell.X + .5d) / n0; var v = -1d + 2d * (cell.Y + .5d) / n0;
            var radius = Math.Sqrt(u * u + v * v); var cellSize = 2d / n0; var minimum = 2d / (BaseFaceResolution << definition.PupilDepth);
            var outer = 2d / (BaseFaceResolution << definition.OuterDepth);
            var desired = Math.Max(minimum, Math.Min(outer, DensityLinear * radius + DensityQuadratic * radius * radius));
            if (cellSize <= desired * 1.20d)
            {
                leaves.Add(cell);
                if (leaves.Count > MaximumProductionLeafCount)
                    throw new InvalidOperationException($"Production topology exceeds the {MaximumProductionLeafCount:N0}-leaf capacity guard.");
                return;
            }
            var x2 = cell.X * 2; var y2 = cell.Y * 2; var d = cell.Depth + 1;
            Refine(new(cell.Face, d, x2, y2)); Refine(new(cell.Face, d, x2 + 1, y2)); Refine(new(cell.Face, d, x2, y2 + 1)); Refine(new(cell.Face, d, x2 + 1, y2 + 1));
        }
    }

    private static Dictionary<LineKey, int[]> BuildLinePoints(List<Cell> leaves)
    {
        var values = new Dictionary<LineKey, SortedSet<int>>();
        foreach (var c in leaves)
        {
            var shift = MaximumRefinementDepth - c.Depth; var x0 = c.X << shift; var x1 = (c.X + 1) << shift; var y0 = c.Y << shift; var y1 = (c.Y + 1) << shift;
            Add(new(c.Face, false, y0), x0, x1); Add(new(c.Face, false, y1), x0, x1); Add(new(c.Face, true, x0), y0, y1); Add(new(c.Face, true, x1), y0, y1);
        }
        return values.ToDictionary(x => x.Key, x => x.Value.ToArray());
        void Add(LineKey key, int a, int b) { if (!values.TryGetValue(key, out var set)) { set = new(); values.Add(key, set); } set.Add(a); set.Add(b); }
    }

    private static List<GridPoint> Boundary(int face, int x0, int x1, int y0, int y1, Dictionary<LineKey, int[]> lines)
    {
        var result = new List<GridPoint>();
        foreach (var x in Range(lines[new(face, false, y0)], x0, x1, false, false, false)) result.Add(new(x, y0));
        foreach (var y in Range(lines[new(face, true, x1)], y0, y1, true, false, false)) result.Add(new(x1, y));
        foreach (var x in Range(lines[new(face, false, y1)], x0, x1, false, true, true)) result.Add(new(x, y1));
        foreach (var y in Range(lines[new(face, true, x0)], y0, y1, true, true, true)) result.Add(new(x0, y));
        return result;
        static IEnumerable<int> Range(int[] values, int low, int high, bool excludeLow, bool excludeHigh, bool descending)
        {
            var first = Array.BinarySearch(values, low); if (first < 0) first = ~first; else if (excludeLow) first++;
            var last = Array.BinarySearch(values, high); if (last < 0) last = ~last - 1; else if (excludeHigh) last--;
            if (!descending) { for (var i = first; i <= last; i++) yield return values[i]; }
            else { for (var i = last; i >= first; i--) yield return values[i]; }
        }
    }

    private static CubeKey Cube(int face, int u, int v) => face switch
    {
        0 => new(u, v, CommonScale), 1 => new(-u, v, -CommonScale), 2 => new(CommonScale, v, -u),
        3 => new(-CommonScale, v, u), 4 => new(u, CommonScale, -v), _ => new(u, -CommonScale, v),
    };

    private static Double3 Direction(CubeKey key) => new Double3(key.X, key.Y, key.Z).Normalized();
    private static (int[] Offsets, int[] Neighbors) BuildAdjacency(int count, List<uint> indices)
    {
        var counts = new int[count];
        for (var i = 0; i < indices.Count; i += 3)
        {
            counts[indices[i]] += 2; counts[indices[i + 1]] += 2; counts[indices[i + 2]] += 2;
        }
        var rawOffsets = new int[count + 1];
        for (var i = 0; i < count; i++) rawOffsets[i + 1] = checked(rawOffsets[i] + counts[i]);
        var cursors = rawOffsets[..^1].ToArray(); var raw = new int[rawOffsets[^1]];
        for (var i = 0; i < indices.Count; i += 3)
        {
            var a = (int)indices[i]; var b = (int)indices[i + 1]; var c = (int)indices[i + 2];
            raw[cursors[a]++] = b; raw[cursors[a]++] = c;
            raw[cursors[b]++] = a; raw[cursors[b]++] = c;
            raw[cursors[c]++] = a; raw[cursors[c]++] = b;
        }
        var offsets = new int[count + 1]; var compact = new List<int>(raw.Length / 2);
        for (var vertex = 0; vertex < count; vertex++)
        {
            var first = rawOffsets[vertex]; var length = rawOffsets[vertex + 1] - first;
            Array.Sort(raw, first, length); offsets[vertex] = compact.Count; var previous = -1;
            for (var j = first; j < first + length; j++) if (raw[j] != previous) { compact.Add(raw[j]); previous = raw[j]; }
        }
        offsets[^1] = compact.Count; return (offsets, compact.ToArray());
    }

    private static int[] ParentMap(PlanetaryProductionSphericalBillboardTopology? parent, Dictionary<CubeKey, int> lookup)
    { if (parent is null) return []; var result = new int[parent.Vertices.Count]; for (var i = 0; i < result.Length; i++) { var v = parent.Vertices[i]; if (!lookup.TryGetValue(new(v.CubeX, v.CubeY, v.CubeZ), out result[i])) throw new InvalidOperationException("A production parent vertex was not inherited exactly."); } return result; }
    private static ulong HashMapping(int[] values) { var bytes = new byte[values.Length * 4]; for (var i = 0; i < values.Length; i++) BitConverter.GetBytes(values[i]).CopyTo(bytes, i * 4); return BitConverter.ToUInt64(SHA256.HashData(bytes)); }
    private static ulong Hash64(string value) => BitConverter.ToUInt64(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static double PhysicalDisplacementEnvelope() => EarthElevationDataset.MaximumElevationMetres + PlanetaryLocalTerrainPackContract.DefaultResidualMaximumMetres + PlanetaryNaturalTerrainFamilies.ComposedBounds().TotalHeight;
    private static ulong ActiveGpuBytes(PlanetaryProductionSphericalBillboardTopology level) => checked(level.ImmutableGpuBytes + (ulong)level.Vertices.Count * 48ul + (ulong)level.Vertices.Count * 96ul + (ulong)level.TriangleCount * 48ul);

    private static (double[] VertexSpacing, double PupilSpacing, double TransitionSpacing, double OuterSpacing) Measure(
        PlanetaryProductionSphericalBillboardTopology.Vertex[] vertices, int[] offsets, int[] neighbors, int pupilDepth, int outerDepth)
    {
        var directions = vertices.Select(v => v.Direction(CommonScale)).ToArray(); var spacing = new double[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var sum = 0d; for (var j = offsets[i]; j < offsets[i + 1]; j++) sum += Math.Acos(Math.Clamp(Double3.Dot(directions[i], directions[neighbors[j]]), -1d, 1d));
            spacing[i] = sum / (offsets[i + 1] - offsets[i]);
        }
        double Median(IEnumerable<double> source) { var a = source.Order().ToArray(); return a[a.Length / 2]; }
        var pupil = Median(vertices.Select((v, i) => (v, i)).Where(x => x.v.RefinementDepth >= pupilDepth - 1).Select(x => spacing[x.i]));
        var outer = Median(vertices.Select((v, i) => (v, i)).Where(x => x.v.RefinementDepth <= outerDepth).Select(x => spacing[x.i]));
        var transitionValues = vertices.Select((v, i) => (v, i)).Where(x => x.v.RefinementDepth > 3 && x.v.RefinementDepth < pupilDepth - 1).Select(x => spacing[x.i]).ToArray();
        return (spacing, pupil, transitionValues.Length == 0 ? pupil : transitionValues.Max(), outer);
    }

    private static PlanetaryProductionSphericalBillboardTopology.DensityRegion[] BuildRegions(PlanetaryProductionSphericalBillboardTopology.Vertex[] vertices,
        List<uint> indices, double[] spacing)
    {
        var directions = vertices.Select(v => v.Direction(CommonScale)).ToArray();
        var depths = vertices.Select(v => (int)v.RefinementDepth).Distinct().Order().ToArray(); var result = new List<PlanetaryProductionSphericalBillboardTopology.DensityRegion>();
        var triangleCounts = new int[MaximumRefinementDepth + 1]; var maximumEdges = new double[MaximumRefinementDepth + 1];
        for (var i = 0; i < indices.Count; i += 3)
        {
            var ia = (int)indices[i]; var ib = (int)indices[i + 1]; var ic = (int)indices[i + 2];
            var depth = Math.Max(vertices[ia].RefinementDepth, Math.Max(vertices[ib].RefinementDepth, vertices[ic].RefinementDepth));
            triangleCounts[depth]++;
            maximumEdges[depth] = Math.Max(maximumEdges[depth], Math.Max(Math.Acos(Math.Clamp(Double3.Dot(directions[ia], directions[ib]), -1d, 1d)),
                Math.Max(Math.Acos(Math.Clamp(Double3.Dot(directions[ib], directions[ic]), -1d, 1d)), Math.Acos(Math.Clamp(Double3.Dot(directions[ic], directions[ia]), -1d, 1d)))));
        }
        foreach (var depth in depths)
        {
            var members = vertices.Select((v, i) => (v, i)).Where(x => x.v.RefinementDepth == depth).Select(x => spacing[x.i]).Order().ToArray();
            result.Add(new(depth, depth, members.Length, triangleCounts[depth], members[members.Length / 2], maximumEdges[depth]));
        }
        return result.ToArray();
    }

    private readonly record struct Cell(int Face, int Depth, int X, int Y);
    private readonly record struct GridPoint(int X, int Y);
    private readonly record struct LineKey(int Face, bool Vertical, int Coordinate);
    private readonly record struct CubeKey(int X, int Y, int Z);
    private sealed class MutableVertex(CubeKey key, int depth) { public CubeKey Key { get; } = key; public int Depth { get; set; } = depth; }
}
