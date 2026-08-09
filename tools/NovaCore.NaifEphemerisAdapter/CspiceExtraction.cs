using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NovaCore.NaifEphemerisAdapter;

internal readonly record struct OfficialNaifFile(string CanonicalName, long Bytes, string Sha256, string Source, string Release);
internal static class OfficialNaifBundle
{
    internal static readonly OfficialNaifFile[] Required =
    [
        new("de440.bsp",119799808,"A4CE9BF9B3282BECC9F4B2AC3CEBE03A2AE7599981AABD7265FD8482FFF7C4B5","naif.jpl.nasa.gov/pub/naif/generic_kernels/spk/planets/de440.bsp","DE440"),
        new("gm_de440.tpc",12406,"924DDF4FB9EAD9FE8A1AA55780BCABDE40B09D00065D58226E24B68D8092F140","naif.jpl.nasa.gov/pub/naif/generic_kernels/pck/gm_de440.tpc","DE440"),
        new("pck00010.tpc",126143,"59468328349AA730D18BF1F8D7E86EFE6E40B75DFB921908F99321B3A7A701D2","naif.jpl.nasa.gov/pub/naif/generic_kernels/pck/pck00010.tpc","PCK00010"),
        new("moon_pa_de440_200625.bpc",12863488,"60CD55AA401EA2EA97360636F567554BFE4E37BB829F901B4460A455DFAF783F","naif.jpl.nasa.gov/pub/naif/generic_kernels/pck/moon_pa_de440_200625.bpc","DE440 lunar principal-axis orientation"),
        new("moon_de440_250416.tf",19478,"A47C71E9C9F33796BDAFB2C9D69A7EE447B6016ECAD80F71CD6F3E479F9CF768","naif.jpl.nasa.gov/pub/naif/generic_kernels/fk/satellites/moon_de440_250416.tf","DE440 lunar frames"),
        new("naif0012.tls",5257,"678E32BDB5A744117A467CD9601CD6B373F0E9BC9BBDE1371D5EEE39600A039B","naif.jpl.nasa.gov/pub/naif/generic_kernels/lsk/naif0012.tls","NAIF0012"),
        new("cspice.zip",36519028,"98D60B814B412FA55294AEAAEB7DAB46D849CC87A8B709FFE835D08DE17625DC","naif.jpl.nasa.gov/pub/naif/toolkit/C/PC_Windows_VisualC_64bit/packages/cspice.zip","N0067")
    ];
    internal static bool Verify(string directory) { foreach(var file in Required) { var path=Path.Combine(directory,file.CanonicalName); if(!File.Exists(path)||new FileInfo(path).Length!=file.Bytes||!Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).Equals(file.Sha256,StringComparison.Ordinal)) return false; } return true; }
    internal static bool VerifyRepositoryRoot(string root)
    {
        foreach(var file in Required)
        {
            var path=Path.Combine(root,"external","naif",file.CanonicalName=="cspice.zip"?"toolkit":"kernels",file.CanonicalName);
            if(!File.Exists(path)||new FileInfo(path).Length!=file.Bytes)return false;
            using var stream=File.OpenRead(path);
            if(!Convert.ToHexString(SHA256.HashData(stream)).Equals(file.Sha256,StringComparison.Ordinal))return false;
        }
        return true;
    }
}

[StructLayout(LayoutKind.Sequential)] internal struct CspiceStateKm { internal double X,Y,Z,Vx,Vy,Vz; }
[StructLayout(LayoutKind.Sequential)] internal struct CspiceMatrix3 { internal double M00,M01,M02,M10,M11,M12,M20,M21,M22; }
internal static partial class CspiceShim
{
    [LibraryImport("NovaCore.CSpiceShim",EntryPoint="NcspLoadKernel",StringMarshalling=StringMarshalling.Utf8)] [return:MarshalAs(UnmanagedType.I4)] internal static partial int Load(string path);
    [LibraryImport("NovaCore.CSpiceShim",EntryPoint="NcspClearKernels")] internal static partial int Clear();
    [LibraryImport("NovaCore.CSpiceShim",EntryPoint="NcspQueryGeometricState")] internal static partial int Query(int target,double et,out CspiceStateKm state);
}
