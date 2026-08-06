using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>One derived root-frame direction segment for the fixed celestial presentation overlay.</summary>
public readonly record struct ResolvedDirectionIndicator(UniversePosition Start, UniversePosition End)
{
    public bool IsValid(ReferenceFrameId rootFrame) =>
        Start.Frame == rootFrame && End.Frame == rootFrame && Start.Value.IsFinite && End.Value.IsFinite && Start.Value != End.Value;
}
