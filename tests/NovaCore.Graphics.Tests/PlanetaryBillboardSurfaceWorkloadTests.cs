using System.Text.RegularExpressions;
using NovaCore.Graphics;

internal static class PlanetaryBillboardSurfaceWorkloadTests
{
    private readonly record struct Field(uint Kind, uint Width, uint Components, bool Flat);

    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var compiled = Path.Combine(root, "build", "native-ninja", "shaders");
        var source = Path.Combine(root, "native", "NovaCore.Native", "shaders");
        var vertex = Interface(Path.Combine(compiled, "production_spherical_billboard.vert.spv"), 3);
        var controlIn = Interface(Path.Combine(compiled, "production_spherical_billboard.tesc.spv"), 1);
        var controlOut = Interface(Path.Combine(compiled, "production_spherical_billboard.tesc.spv"), 3);
        var evaluationIn = Interface(Path.Combine(compiled, "production_spherical_billboard.tese.spv"), 1);
        var evaluationOut = Interface(Path.Combine(compiled, "production_spherical_billboard.tese.spv"), 3);
        var fragmentIn = Interface(Path.Combine(compiled, "planetary_production.frag.spv"), 1);

        Match(vertex, controlIn, "vertex/control");
        Match(controlOut, evaluationIn, "control/evaluation");
        Match(evaluationOut, fragmentIn, "unchanged evaluation/fragment");
        Require(controlOut.Keys.Order().SequenceEqual(new uint[] { 1, 2, 5, 6, 7 }),
            "only physical normal, lighting direction, view vector, physical direction and height cross TCS outputs");
        Require(vertex.Keys.Order().SequenceEqual(new uint[] { 1, 2, 5, 6, 7, 17 }),
            "the additional camera-relative position is consumed only by edge-factor calculation");
        Require(controlOut.Values.Sum(field => field.Components) == 13 &&
                controlOut.Values.All(field => field.Width == 32),
            "compiled TCS user transport is bounded to 13 32-bit scalars per control point, independent of triangle count");
        Require(evaluationOut.Keys.Order().SequenceEqual(Enumerable.Range(0, 16).Select(i => (uint)i)),
            "all sixteen production fragment inputs remain supplied");

        // These are frame/body inputs, not values inferred from a triangle,
        // topology, resource residency, or a cached prior body. Check their
        // provenance alongside the compiled inter-stage transport budget.
        var evaluation = Regex.Replace(File.ReadAllText(Path.Combine(source,
            "production_spherical_billboard.tese")), @"\s+", "");
        foreach (var assignment in new[]
        {
            "Presentationp=presentations.values[0];",
            "color=vec4(1);", "material=uvec2(p.identity.w,p.identity.z);",
            "response=p.surface;", "bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;",
            "bodyCameraLow=inputData.cameraLowRadiusLow.xyz;", "localDetail=p.localDetail;",
            "productionLayer=0x40000000u;", "productionTransition=vec2(1,0);",
            "productionAddress=uvec4(face,level,cell);", "productionUv=local;"
        }) Require(evaluation.Contains(assignment, StringComparison.Ordinal),
            $"TES preserves authoritative output provenance: {assignment}");
        var vertexSource = File.ReadAllText(Path.Combine(source, "production_spherical_billboard.vert"));
        Require(!vertexSource.Contains("ProductionDirectionAddressD(", StringComparison.Ordinal),
            "VS does not compute addresses that TES replaces before fragment consumption");
        var traversal = File.ReadAllText(Path.Combine(root, "samples", "NovaCore.Triangle",
            "ProductionBillboardDesktopTraversal.cs"));
        foreach (var phase in new[] { "ScaleOut", "ScaleIn" })
        {
            var awaited = Regex.Match(traversal,
                $@"case Phase\.{phase} when SettledAt\(runtime, (\d+)\)");
            var requested = Regex.Match(traversal,
                $@"Phase\.{phase} => _representativeAltitude\[(\d+)\]");
            Require(awaited.Success && requested.Success &&
                    awaited.Groups[1].Value == requested.Groups[1].Value,
                $"{phase} requests the representative level its settle condition awaits");
        }
        Require(traversal.Contains("_fixedDiagnosticTime && !(_directionalDiagnosticOnly || _horizonDiagnosticOnly)",
                    StringComparison.Ordinal) &&
                traversal.Contains("if (_fixedDiagnosticTime && !scene.IsPaused) input.PauseToggle = 1;",
                    StringComparison.Ordinal),
            "fixed-time benchmarking uses the normal pause input and is excluded from full physical traversal");
        Console.WriteLine("P2S5G compiled surface interface: VS=16 scalars; TCS=13 scalars/control point; " +
            "32 redundant TCS scalars removed; fragment inputs=16; frame/body provenance preserved");
    }

    // Reflect actual compiled SPIR-V rather than counting source declarations.
    // Built-ins have no Location decoration and are deliberately outside this
    // user-payload budget. Tessellation's outer per-control-point array is not
    // another vector component.
    private static Dictionary<uint, Field> Interface(string path, uint storage)
    {
        var bytes = File.ReadAllBytes(path);
        Require(bytes.Length >= 20 && bytes.Length % 4 == 0, "valid SPIR-V size");
        var words = new uint[bytes.Length / 4];
        for (var i = 0; i < words.Length; i++) words[i] =
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * 4, 4));
        Require(words[0] == 0x07230203, "SPIR-V magic");
        var types = new Dictionary<uint, uint[]>();
        var locations = new Dictionary<uint, uint>();
        var flat = new HashSet<uint>();
        var variables = new List<(uint Type, uint Id, uint Storage)>();
        for (var offset = 5; offset < words.Length;)
        {
            var count = checked((int)(words[offset] >> 16));
            var op = words[offset] & 0xffff;
            Require(count > 0 && offset + count <= words.Length, "bounded SPIR-V instruction");
            var operands = words.AsSpan(offset + 1, count - 1);
            if (op is 21 or 22 or 23 or 28 or 32) types.Add(operands[0], words.AsSpan(offset, count).ToArray());
            if (op == 59) variables.Add((operands[0], operands[1], operands[2]));
            if (op == 71 && operands[1] == 30) locations.Add(operands[0], operands[2]);
            if (op == 71 && operands[1] == 14) flat.Add(operands[0]);
            offset += count;
        }
        var result = new Dictionary<uint, Field>();
        foreach (var variable in variables.Where(v => v.Storage == storage && locations.ContainsKey(v.Id)))
        {
            var type = types[types[variable.Type][3]]; // OpTypePointer's pointee
            if ((type[0] & 0xffff) == 28) type = types[type[2]];
            var components = 1u;
            if ((type[0] & 0xffff) == 23) { components = type[3]; type = types[type[2]]; }
            var scalar = type[0] & 0xffff;
            Require(scalar is 21 or 22, "32-bit scalar/vector user interface");
            var kind = scalar == 22 ? 0u : type[3] == 0 ? 1u : 2u;
            result.Add(locations[variable.Id], new(kind, type[2], components, flat.Contains(variable.Id)));
        }
        return result;
    }

    private static void Match(Dictionary<uint, Field> output, Dictionary<uint, Field> input, string label)
    {
        Require(output.Count == input.Count && output.All(pair =>
            input.TryGetValue(pair.Key, out var field) && field == pair.Value),
            $"compiled {label} interfaces match in location, scalar type, width, vector size and interpolation");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
