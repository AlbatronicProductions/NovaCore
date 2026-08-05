using System.Runtime.InteropServices;

namespace NovaCore.Graphics;

/// <summary>Camera-relative FP32 presentation vertex for the single optional orbit curve.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct OrbitLineVertex { public float X, Y, Z; }
