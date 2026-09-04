using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>
/// Offline generator for NovaCore's independent nested scale-mesh candidate.
/// Density changes only through centered rectilinear regions. Adjacent bands
/// are conformingly triangulated from their exact shared boundary vertices;
/// no leaf-center fan or runtime topology generation is used.
/// </summary>
public static class PlanetaryNestedScaleMeshTopologyGenerator
{
    public const int ScaleCount = 18, BaseFaceResolution = 48;
    public const double EarthRadiusMetres = PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
    public const float EntryPixels = 18f, ReturnPixels = 12f, UrgentPixels = 24f,
        TessellationTargetPixels = 3f;
    private const int MaximumLeafCount = 1_100_000;
    private const double VerticalFovRadians = Math.PI / 3d;
    private const int ReferenceViewportHeight = 1440;

    // Each level adds one centered 2x region. The early extents establish
    // progressively staged transition bands; later extents retain a bounded
    // footprint while physical spacing halves. The final 36-cell region gives
    // a roughly 200 m finest footprint and about 2 m spacing at Earth radius.
    private static readonly int[] HalfExtentCells =
        [20, 30, 45, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 36];

    public readonly record struct ScaleDefinition(string Identity, int RefinementStages,
        double MaximumAltitudeMetres);

    public sealed record LibraryReport(long SerializedBytes, ulong MaximumCurrentIncomingBytes,
        double MaximumEdgeMetres, double MaximumChordSagMetres, double FinestSpacingMetres,
        int FinestVertices, int FinestTriangles);

    private readonly record struct Cell(int Face, int Stage, long X0, long X1, long Y0, long Y1);
    private readonly record struct LineKey(int Face, bool Vertical, long Fixed);
    private readonly record struct GridPoint(long X, long Y);
    private readonly record struct CubeKey(long X, long Y, long Z);
    private sealed class MutableVertex(CubeKey key, ushort region)
    {
        public CubeKey Key = key;
        public ushort Region = region;
    }

    public static IReadOnlyList<ScaleDefinition> Definitions()
    {
        var result = new ScaleDefinition[ScaleCount];
        result[0] = new("NSM-00-S00", 0, double.PositiveInfinity);
        for (var scale = 1; scale < result.Length; scale++)
        {
            var previousSpacing = 2d / (BaseFaceResolution * (1L << (scale - 1)));
            result[scale] = new($"NSM-{scale:D2}-S{scale:D2}", scale,
                MaximumAltitudeForError(previousSpacing, TessellationTargetPixels));
        }
        return Array.AsReadOnly(result);
    }

    public static PlanetaryNestedScaleMeshTopology ApplyScaleDefinition(
        PlanetaryNestedScaleMeshTopology topology, ScaleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (topology.Scale != definition.RefinementStages)
            throw new ArgumentException("Scale definition does not match the topology.", nameof(definition));
        if (topology.Geometry.MaximumAltitudeMetres.Equals(definition.MaximumAltitudeMetres)) return topology;
        var geometry = topology.Geometry with { MaximumAltitudeMetres = definition.MaximumAltitudeMetres };
        var provisional = new PlanetaryNestedScaleMeshTopology(topology.Scale, topology.Vertices.ToArray(),
            topology.Indices.ToArray(), topology.NeighborOffsets.ToArray(), topology.Neighbors.ToArray(),
            topology.Regions.ToArray(), geometry, 0);
        var bytes = PlanetaryNestedScaleMeshTopology.Serialize(provisional);
        var hash = BitConverter.ToUInt64(bytes, 80);
        return new(topology.Scale, topology.Vertices.ToArray(), topology.Indices.ToArray(),
            topology.NeighborOffsets.ToArray(), topology.Neighbors.ToArray(), topology.Regions.ToArray(),
            geometry, hash);
    }

    public static IReadOnlyList<PlanetaryNestedScaleMeshTopology> GenerateProductionLibrary()
    {
        var definitions = Definitions();
        var result = new List<PlanetaryNestedScaleMeshTopology>(definitions.Count);
        for (var scale = 0; scale < definitions.Count; scale++) result.Add(Generate(scale, definitions[scale]));
        return result.AsReadOnly();
    }

    public static PlanetaryNestedScaleMeshTopology Generate(int scale, ScaleDefinition definition)
    {
        if (scale is < 0 or >= ScaleCount || definition.RefinementStages != scale ||
            definition.RefinementStages > HalfExtentCells.Length)
            throw new ArgumentOutOfRangeException(nameof(definition));
        var denominator = checked((long)BaseFaceResolution << definition.RefinementStages);
        var baseStep = checked(2L * denominator / BaseFaceResolution);
        var leaves = new List<Cell>(BaseFaceResolution * BaseFaceResolution * 6);
        for (var face = 0; face < 6; face++)
        for (var y = 0; y < BaseFaceResolution; y++)
        for (var x = 0; x < BaseFaceResolution; x++)
            leaves.Add(new(face, 0, -denominator + x * baseStep,
                -denominator + (x + 1L) * baseStep, -denominator + y * baseStep,
                -denominator + (y + 1L) * baseStep));

        for (var stage = 0; stage < definition.RefinementStages; stage++)
        {
            var cellStep = baseStep >> stage;
            var bound = checked((long)HalfExtentCells[stage] * cellStep);
            var next = new List<Cell>(leaves.Count + HalfExtentCells[stage] * HalfExtentCells[stage] * 12);
            foreach (var cell in leaves)
            {
                if (cell.Face == 0 && cell.Stage == stage && cell.X0 >= -bound && cell.X1 <= bound &&
                    cell.Y0 >= -bound && cell.Y1 <= bound)
                {
                    var xm = (cell.X0 + cell.X1) / 2; var ym = (cell.Y0 + cell.Y1) / 2;
                    var childStage = stage + 1;
                    next.Add(new(0, childStage, cell.X0, xm, cell.Y0, ym));
                    next.Add(new(0, childStage, xm, cell.X1, cell.Y0, ym));
                    next.Add(new(0, childStage, cell.X0, xm, ym, cell.Y1));
                    next.Add(new(0, childStage, xm, cell.X1, ym, cell.Y1));
                }
                else next.Add(cell);
            }
            leaves = next;
            if (leaves.Count > MaximumLeafCount)
                throw new InvalidOperationException($"Nested scale mesh exceeds the {MaximumLeafCount:N0}-leaf capacity guard.");
        }

        var linePoints = BuildLinePoints(leaves);
        var vertices = new List<MutableVertex>(leaves.Count);
        var vertexLookup = new Dictionary<CubeKey, int>(leaves.Count);
        var indices = new List<uint>(leaves.Count * 6);
        var triangleRegions = new List<ushort>(leaves.Count * 2);
        foreach (var cell in leaves.OrderBy(value => value.Face).ThenBy(value => value.Stage)
                     .ThenBy(value => value.Y0).ThenBy(value => value.X0))
        {
            var boundary = Boundary(cell, linePoints);
            TriangulateBoundary(cell, boundary);
        }

        ValidateClosedManifold(vertices, indices, denominator);
        var (offsets, neighbors) = BuildAdjacency(vertices.Count, indices);
        var immutableVertices = vertices.Select(value => new PlanetaryNestedScaleMeshTopology.Vertex(
            value.Key.X, value.Key.Y, value.Key.Z, denominator, value.Region, value.Region)).ToArray();
        var measured = Measure(immutableVertices, indices, triangleRegions, definition, denominator);
        var provisional = new PlanetaryNestedScaleMeshTopology(scale, immutableVertices, indices.ToArray(),
            offsets, neighbors, measured.Regions, measured.Geometry, 0);
        var bytes = PlanetaryNestedScaleMeshTopology.Serialize(provisional);
        var hash = BitConverter.ToUInt64(bytes, 80);
        return new(scale, immutableVertices, indices.ToArray(), offsets, neighbors, measured.Regions,
            measured.Geometry, hash);

        int Vertex(int face, GridPoint point, ushort region)
        {
            var key = Cube(face, point.X, point.Y, denominator);
            if (vertexLookup.TryGetValue(key, out var found))
            {
                if (vertices[found].Region < region) vertices[found].Region = region;
                return found;
            }
            var index = vertices.Count; vertices.Add(new(key, region)); vertexLookup.Add(key, index); return index;
        }

        void TriangulateBoundary(Cell cell, List<GridPoint> boundary)
        {
            if (boundary.Count < 4) throw new InvalidOperationException("A rectilinear leaf boundary is incomplete.");
            var polygon = boundary.ToList();
            while (polygon.Count > 3)
            {
                var clipped = false;
                for (var i = 0; i < polygon.Count; i++)
                {
                    var previous = polygon[(i + polygon.Count - 1) % polygon.Count];
                    var current = polygon[i]; var next = polygon[(i + 1) % polygon.Count];
                    var cross = checked((current.X - previous.X) * (next.Y - current.Y) -
                                        (current.Y - previous.Y) * (next.X - current.X));
                    if (cross <= 0) continue;
                    // Transition boundaries deliberately retain collinear shared-edge
                    // vertices. Do not clip the final non-collinear corner and leave
                    // three collinear vertices as the terminal polygon.
                    if (polygon.Count > 3 && !HasAreaAfterRemoving(polygon, i)) continue;
                    AddTriangle(cell.Face, previous, current, next, (ushort)cell.Stage);
                    polygon.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new InvalidOperationException("A transition polygon could not be triangulated without a center fan.");
            }
            AddTriangle(cell.Face, polygon[0], polygon[1], polygon[2], (ushort)cell.Stage);

            static bool HasAreaAfterRemoving(List<GridPoint> points, int removed)
            {
                long twiceArea = 0;
                GridPoint? first = null;
                GridPoint? previous = null;
                for (var index = 0; index < points.Count; index++)
                {
                    if (index == removed) continue;
                    var point = points[index];
                    first ??= point;
                    if (previous is { } value)
                        twiceArea = checked(twiceArea + value.X * point.Y - value.Y * point.X);
                    previous = point;
                }
                if (previous is { } last && first is { } start)
                    twiceArea = checked(twiceArea + last.X * start.Y - last.Y * start.X);
                return twiceArea != 0;
            }
        }

        void AddTriangle(int face, GridPoint a, GridPoint b, GridPoint c, ushort region)
        {
            var ia = Vertex(face, a, region); var ib = Vertex(face, b, region); var ic = Vertex(face, c, region);
            if (ia == ib || ib == ic || ic == ia) throw new InvalidOperationException("Degenerate scale-mesh triangle identity.");
            var da = Direction(vertices[ia].Key); var db = Direction(vertices[ib].Key); var dc = Direction(vertices[ic].Key);
            if (Double3.Dot(Double3.Cross(db - da, dc - da), da + db + dc) < 0d) (ib, ic) = (ic, ib);
            indices.Add((uint)ia); indices.Add((uint)ib); indices.Add((uint)ic); triangleRegions.Add(region);
        }
    }

    public static LibraryReport Characterize(IReadOnlyList<PlanetaryNestedScaleMeshTopology> levels)
    {
        if (levels.Count != ScaleCount) throw new ArgumentException("The complete 18-scale library is required.", nameof(levels));
        long bytes = 0; ulong maximumPair = 0;
        foreach (var level in levels) bytes += PlanetaryNestedScaleMeshTopology.Serialize(level).Length;
        for (var i = 0; i < levels.Count; i++)
        {
            var pair = ActiveGpuBytes(levels[i]);
            if (i + 1 < levels.Count) pair += ActiveGpuBytes(levels[i + 1]);
            maximumPair = Math.Max(maximumPair, pair);
        }
        var finest = levels[^1];
        return new(bytes, maximumPair, levels.Max(value => value.Geometry.MaximumEdgeMetres),
            levels.Max(value => value.Geometry.MaximumChordSagMetres), finest.Geometry.MinimumEdgeMetres,
            finest.Vertices.Count, finest.TriangleCount);
    }

    public static void WriteProductionLibrary(string directory,
        IReadOnlyList<PlanetaryNestedScaleMeshTopology> levels)
    {
        if (levels.Count != ScaleCount) throw new ArgumentException("The complete 18-scale library is required.", nameof(levels));
        Directory.CreateDirectory(directory);
        var entries = new List<object>(levels.Count);
        foreach (var level in levels)
        {
            var file = $"nested-scale-{level.Scale:D2}.ncsm1";
            var bytes = PlanetaryNestedScaleMeshTopology.Serialize(level);
            File.WriteAllBytes(Path.Combine(directory, file), bytes);
            entries.Add(new
            {
                scale = level.Scale,
                file,
                bytes = bytes.Length,
                vertices = level.Vertices.Count,
                triangles = level.TriangleCount,
                immutableGpuBytes = level.ImmutableGpuBytes,
                topologyHash = $"0x{level.TopologyHash:X16}",
                maximumAltitudeMetres = double.IsFinite(level.Geometry.MaximumAltitudeMetres)
                    ? level.Geometry.MaximumAltitudeMetres
                    : (double?)null,
                maximumEdgeMetres = level.Geometry.MaximumEdgeMetres,
                maximumChordSagMetres = level.Geometry.MaximumChordSagMetres,
                supportRadiusMetres = level.Geometry.PlanetOcclusionSupportRadiusMetres,
                maximumTesDisplacementMetres = level.Geometry.MaximumTesDisplacementMetres,
            });
        }
        var manifest = new
        {
            format = "NovaCoreNestedScaleMeshTopology",
            formatVersion = PlanetaryNestedScaleMeshTopology.FormatVersion,
            generatorVersion = PlanetaryNestedScaleMeshTopology.GeneratorVersion,
            coordinateEncoding = nameof(PlanetaryNestedScaleMeshCoordinateEncoding.SignedCubeRationalInt64),
            scaleCount = levels.Count,
            scales = entries,
        };
        File.WriteAllText(Path.Combine(directory, PlanetaryNestedScaleMeshTopologyLibrary.ManifestFileName),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    private static ulong ActiveGpuBytes(PlanetaryNestedScaleMeshTopology topology)
    {
        // Immutable topology/adjacency is shared. Current and incoming duplicate
        // only frame-local prepared attributes and cull/compact payloads.
        var prepared = checked((ulong)topology.Vertices.Count * 64ul);
        var culling = checked((ulong)topology.TriangleCount * 16ul + 32ul);
        return checked(topology.ImmutableGpuBytes + prepared + culling);
    }

    private static double MaximumAltitudeForError(double angularSpacing, double targetPixels)
    {
        var halfAngle = targetPixels * VerticalFovRadians / ReferenceViewportHeight;
        return EarthRadiusMetres * Math.Sin(angularSpacing) / Math.Tan(halfAngle) -
            EarthRadiusMetres + EarthRadiusMetres * Math.Cos(angularSpacing);
    }

    private static Dictionary<LineKey, long[]> BuildLinePoints(List<Cell> leaves)
    {
        var values = new Dictionary<LineKey, SortedSet<long>>();
        foreach (var cell in leaves)
        {
            Add(new(cell.Face, false, cell.Y0), cell.X0, cell.X1);
            Add(new(cell.Face, false, cell.Y1), cell.X0, cell.X1);
            Add(new(cell.Face, true, cell.X0), cell.Y0, cell.Y1);
            Add(new(cell.Face, true, cell.X1), cell.Y0, cell.Y1);
        }
        return values.ToDictionary(value => value.Key, value => value.Value.ToArray());

        void Add(LineKey key, long a, long b)
        {
            if (!values.TryGetValue(key, out var set)) values.Add(key, set = []);
            set.Add(a); set.Add(b);
        }
    }

    private static List<GridPoint> Boundary(Cell cell, Dictionary<LineKey, long[]> lines)
    {
        var result = new List<GridPoint>();
        foreach (var x in Range(lines[new(cell.Face, false, cell.Y0)], cell.X0, cell.X1, false, false, false)) result.Add(new(x, cell.Y0));
        foreach (var y in Range(lines[new(cell.Face, true, cell.X1)], cell.Y0, cell.Y1, true, false, false)) result.Add(new(cell.X1, y));
        foreach (var x in Range(lines[new(cell.Face, false, cell.Y1)], cell.X0, cell.X1, false, true, true)) result.Add(new(x, cell.Y1));
        foreach (var y in Range(lines[new(cell.Face, true, cell.X0)], cell.Y0, cell.Y1, true, true, true)) result.Add(new(cell.X0, y));
        return result;

        static IEnumerable<long> Range(long[] values, long low, long high,
            bool excludeLow, bool excludeHigh, bool descending)
        {
            var first = Array.BinarySearch(values, low); if (first < 0) first = ~first; else if (excludeLow) first++;
            var last = Array.BinarySearch(values, high); if (last < 0) last = ~last - 1; else if (excludeHigh) last--;
            if (!descending) for (var i = first; i <= last; i++) yield return values[i];
            else for (var i = last; i >= first; i--) yield return values[i];
        }
    }

    private static CubeKey Cube(int face, long u, long v, long scale) => face switch
    {
        0 => new(u, v, scale), 1 => new(-u, v, -scale), 2 => new(scale, v, -u),
        3 => new(-scale, v, u), 4 => new(u, scale, -v), _ => new(u, -scale, v),
    };

    private static Double3 Direction(CubeKey key) => new Double3(key.X, key.Y, key.Z).Normalized();

    private static (int[] Offsets, int[] Neighbors) BuildAdjacency(int vertexCount, List<uint> indices)
    {
        var counts = new int[vertexCount];
        for (var i = 0; i < indices.Count; i += 3)
        {
            counts[indices[i]] += 2; counts[indices[i + 1]] += 2; counts[indices[i + 2]] += 2;
        }
        var rawOffsets = new int[vertexCount + 1];
        for (var i = 0; i < vertexCount; i++) rawOffsets[i + 1] = checked(rawOffsets[i] + counts[i]);
        var cursors = rawOffsets[..^1].ToArray(); var raw = new int[rawOffsets[^1]];
        for (var i = 0; i < indices.Count; i += 3)
        {
            var a = (int)indices[i]; var b = (int)indices[i + 1]; var c = (int)indices[i + 2];
            raw[cursors[a]++] = b; raw[cursors[a]++] = c;
            raw[cursors[b]++] = a; raw[cursors[b]++] = c;
            raw[cursors[c]++] = a; raw[cursors[c]++] = b;
        }
        var offsets = new int[vertexCount + 1]; var neighbors = new List<int>(raw.Length / 2);
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            var first = rawOffsets[vertex]; var length = rawOffsets[vertex + 1] - first;
            Array.Sort(raw, first, length); offsets[vertex] = neighbors.Count; var previous = -1;
            for (var i = first; i < first + length; i++) if (raw[i] != previous) { neighbors.Add(raw[i]); previous = raw[i]; }
        }
        offsets[^1] = neighbors.Count; return (offsets, neighbors.ToArray());
    }

    private static void ValidateClosedManifold(List<MutableVertex> vertices, List<uint> indices, long denominator)
    {
        var edges = new ulong[indices.Count]; var triangles = new HashSet<(uint, uint, uint)>();
        for (var i = 0; i < indices.Count; i += 3)
        {
            var a = indices[i]; var b = indices[i + 1]; var c = indices[i + 2];
            var sorted = new[] { a, b, c }; Array.Sort(sorted);
            if (!triangles.Add((sorted[0], sorted[1], sorted[2]))) throw new InvalidOperationException("Duplicate scale-mesh triangle.");
            edges[i] = Edge(a, b); edges[i + 1] = Edge(b, c); edges[i + 2] = Edge(c, a);
        }
        Array.Sort(edges);
        for (var i = 0; i < edges.Length;)
        {
            var end = i + 1; while (end < edges.Length && edges[end] == edges[i]) end++;
            if (end - i != 2) throw new InvalidOperationException($"Nested scale-mesh edge 0x{edges[i]:X16} has {end - i} incidents.");
            i = end;
        }
        if (vertices.Any(value => Math.Max(Math.Abs(value.Key.X),
                Math.Max(Math.Abs(value.Key.Y), Math.Abs(value.Key.Z))) != denominator))
            throw new InvalidOperationException("Nested scale-mesh cube identity is invalid.");
        static ulong Edge(uint a, uint b) { if (a > b) (a, b) = (b, a); return ((ulong)a << 32) | b; }
    }

    private static (PlanetaryNestedScaleMeshTopology.DensityRegion[] Regions,
        PlanetaryNestedScaleMeshTopology.GeometricContract Geometry) Measure(
        PlanetaryNestedScaleMeshTopology.Vertex[] vertices, List<uint> indices,
        List<ushort> triangleRegions, ScaleDefinition definition, long denominator)
    {
        var directions = vertices.Select(value => value.Direction).ToArray();
        var uniqueEdges = new HashSet<ulong>(); double edgeSum = 0d, edgeMin = double.PositiveInfinity, edgeMax = 0d;
        double spanSum = 0d, spanMin = double.PositiveInfinity, spanMax = 0d, maximumSag = 0d;
        for (var i = 0; i < indices.Count; i += 3)
        {
            var ia = indices[i]; var ib = indices[i + 1]; var ic = indices[i + 2];
            var a = directions[ia]; var b = directions[ib]; var c = directions[ic];
            if (Double3.Dot(Double3.Cross(b - a, c - a), a + b + c) <= 0d)
                throw new InvalidOperationException($"Nested scale-mesh winding is not outward at triangle {i / 3}.");
            var ab = Angle(a, b); var bc = Angle(b, c); var ca = Angle(c, a);
            var span = Math.Max(ab, Math.Max(bc, ca)); spanSum += span; spanMin = Math.Min(spanMin, span); spanMax = Math.Max(spanMax, span);
            AddEdge(ia, ib, ab); AddEdge(ib, ic, bc); AddEdge(ic, ia, ca);
            var closest = ClosestPointToOrigin(a, b, c);
            maximumSag = Math.Max(maximumSag, EarthRadiusMetres * Math.Max(0d,
                1d - Math.Sqrt(closest.LengthSquared)));
        }
        var maximumTes = PlanetaryNaturalTerrainFamilies.ComposedBounds().NearHeight;
        var supportRadius = EarthRadiusMetres + EarthElevationDataset.MinimumElevationMetres;
        if (maximumSag + maximumTes > EarthRadiusMetres - supportRadius + 1e-7d)
            throw new InvalidOperationException($"Scale {definition.Identity} violates displaced-triangle support: sag={maximumSag:R}; TES={maximumTes:R}; inset={EarthRadiusMetres - supportRadius:R}.");

        var regionCount = definition.RefinementStages + 1;
        var regions = new PlanetaryNestedScaleMeshTopology.DensityRegion[regionCount];
        for (var region = 0; region < regionCount; region++)
        {
            var selectedVertices = vertices.Select((value, index) => (value, index)).Where(value => value.value.DensityRegion == region).ToArray();
            var radii = selectedVertices.Where(value => value.value.CubeZ == denominator)
                .Select(value => Angle(Double3.UnitZ, directions[value.index])).ToArray();
            var triangleCount = triangleRegions.Count(value => value == region);
            regions[region] = new(region, region - 1, region, region == 0 ? 1 : 2,
                region == 0 ? BaseFaceResolution / 2 : HalfExtentCells[region - 1], selectedVertices.Length,
                triangleCount, radii.Length == 0 ? 0d : radii.Min(), radii.Length == 0 ? Math.PI : radii.Max());
        }
        var pupilRadius = regions[^1].MaximumRadiusRadians;
        var geometry = new PlanetaryNestedScaleMeshTopology.GeometricContract(EarthRadiusMetres,
            supportRadius, maximumTes, edgeMin * EarthRadiusMetres,
            edgeSum / uniqueEdges.Count * EarthRadiusMetres, edgeMax * EarthRadiusMetres,
            spanMin, spanSum / (indices.Count / 3), spanMax, maximumSag, pupilRadius,
            definition.MaximumAltitudeMetres, EntryPixels, ReturnPixels, UrgentPixels,
            TessellationTargetPixels, definition.RefinementStages == ScaleCount - 1 ? 64u : 1u);
        return (regions, geometry);

        void AddEdge(uint first, uint second, double angle)
        {
            if (first > second) (first, second) = (second, first);
            var key = ((ulong)first << 32) | second;
            if (!uniqueEdges.Add(key)) return;
            edgeSum += angle; edgeMin = Math.Min(edgeMin, angle); edgeMax = Math.Max(edgeMax, angle);
        }
    }

    private static double Angle(in Double3 a, in Double3 b) =>
        Math.Acos(Math.Clamp(Double3.Dot(a, b), -1d, 1d));

    private static Double3 ClosestPointToOrigin(in Double3 a, in Double3 b, in Double3 c)
    {
        var ab = b - a; var ac = c - a; var ap = -a;
        var d1 = Double3.Dot(ab, ap); var d2 = Double3.Dot(ac, ap);
        if (d1 <= 0d && d2 <= 0d) return a;
        var bp = -b; var d3 = Double3.Dot(ab, bp); var d4 = Double3.Dot(ac, bp);
        if (d3 >= 0d && d4 <= d3) return b;
        var vc = d1 * d4 - d3 * d2;
        if (vc <= 0d && d1 >= 0d && d3 <= 0d) return a + ab * (d1 / (d1 - d3));
        var cp = -c; var d5 = Double3.Dot(ab, cp); var d6 = Double3.Dot(ac, cp);
        if (d6 >= 0d && d5 <= d6) return c;
        var vb = d5 * d2 - d1 * d6;
        if (vb <= 0d && d2 >= 0d && d6 <= 0d) return a + ac * (d2 / (d2 - d6));
        var va = d3 * d6 - d5 * d4;
        if (va <= 0d && d4 - d3 >= 0d && d5 - d6 >= 0d)
            return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        var denominator = 1d / (va + vb + vc); var v = vb * denominator; var w = vc * denominator;
        return a + ab * v + ac * w;
    }
}
