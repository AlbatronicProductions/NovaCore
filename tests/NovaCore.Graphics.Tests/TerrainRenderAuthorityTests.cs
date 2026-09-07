using System.Text.RegularExpressions;

// Architecture assertions inspect the candidate branch, never accidental
// matches from the temporary banked comparison branch. GPU tests and the live
// facility probe verify the actual preparation and contact behavior separately.
internal static class TerrainRenderAuthorityTests
{
    internal static string ReadCandidate(string path)
    {
        var text=File.ReadAllText(path);
        const string marker="#if NOVACORE_PREPARED_RENDER_TERRAIN";
        int first=text.IndexOf(marker,StringComparison.Ordinal);
        if(first<0)throw new InvalidOperationException("Missing bounded render-authority comparison branch");
        int start=text.IndexOf('\n',first)+1,alternate=text.IndexOf("#else",start,StringComparison.Ordinal),end=text.IndexOf("#endif",alternate,StringComparison.Ordinal);
        if(start<=first||alternate<start||end<alternate)throw new InvalidOperationException("Invalid render-authority comparison branch");
        return text[..first]+text[start..alternate]+text[(end+"#endif".Length)..];
    }
    internal static void VerifyDefaultAndSampling(string root)
    {
        var shader=File.ReadAllText(Path.Combine(root,"native","NovaCore.Native","shaders","production_spherical_billboard_physical.glsl"));
        if(!shader.Contains("#define NOVACORE_PREPARED_RENDER_TERRAIN 1",StringComparison.Ordinal))
            throw new InvalidOperationException("The candidate must be the development render default");
        var sample=File.ReadAllText(Path.Combine(root,"samples","NovaCore.Triangle","Program.cs"));
        if(Regex.Matches(sample,@"Math\.Max\(10d,gpu\.SurfaceAltitudeMetres\)").Count!=2)
            throw new InvalidOperationException("Startup and moving render selection must consume physical ground clearance");
    }
}
