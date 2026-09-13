using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

// Dependency/deployment smoke only: no world, solver, contact or static BEPU work.
var approved = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["BepuPhysics"] = "77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7",
    ["BepuUtilities"] = "E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68"
};
var result = new List<object>();
foreach (var (name, hash) in approved)
{
    var path = Path.Combine(AppContext.BaseDirectory, name + ".dll");
    Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == hash, name + " output hash");
}
foreach (var (name, hash) in approved)
{
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(name));
    Require(Path.GetFullPath(assembly.Location) == Path.Combine(AppContext.BaseDirectory, name + ".dll"), name + " resolved path");
    Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))) == hash, name + " resolved hash");
    Require(assembly.GetName().Version == new Version(2, 5, 0, 0), name + " assembly version");
    var references = assembly.GetReferencedAssemblies();
    foreach (var reference in references)
    {
        var dependency = AssemblyLoadContext.Default.LoadFromAssemblyName(reference);
        if (!approved.ContainsKey(reference.Name!))
            Require(Path.GetFullPath(dependency.Location).StartsWith(RuntimeEnvironment.GetRuntimeDirectory(), StringComparison.OrdinalIgnoreCase), "unqualified dependency: " + reference.FullName);
    }
    var types = assembly.GetTypes(); // Resolve all type metadata, without creating solver state.
    result.Add(new { name, identity = assembly.FullName, sha256 = hash, typeCount = types.Length, references = references.Select(r => r.FullName).ToArray() });
}
foreach (var name in approved.Keys)
    Require(AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == name) == 1, name + " single loaded identity");
Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "third-party", "Bepu", "LICENSE.txt")), "deployed license");
Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "third-party", "Bepu", "ATTRIBUTION.txt")), "deployed attribution");
Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", assemblies = result }, new JsonSerializerOptions { WriteIndented = true }));

static void Require(bool condition, string contract)
{
    if (!condition) throw new InvalidOperationException("BEPU dependency contract failed: " + contract);
}
