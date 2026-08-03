using NovaCore.Core;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public static class RenderSubmission
{
    public static NativeRelativePosition ForNative(in RelativePosition position) => new() { X = position.Value.X, Y = position.Value.Y, Z = position.Value.Z };
}
