using System.Runtime.InteropServices;
namespace NovaCore.Graphics;
[StructLayout(LayoutKind.Sequential)]
public struct Float4x4 { public float C0R0,C0R1,C0R2,C0R3,C1R0,C1R1,C1R2,C1R3,C2R0,C2R1,C2R2,C2R3,C3R0,C3R1,C3R2,C3R3; }
/// <summary>96-byte std430 camera transport: encoded root position then column-major view/projection.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCameraData
{
    public EncodedPosition Position;
    // Complete the two std430 vec4 position lanes; EncodedPosition itself carries xyz only.
    public float PositionHighPadding, PositionLowPadding;
    public Float4x4 ViewProjection;
}
