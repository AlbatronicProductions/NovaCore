namespace NovaCore.Core.ReferenceFrames;

/// <summary>Immutable structural identity for one reference-frame graph node; it carries no evaluated transform or state.</summary>
public readonly record struct ReferenceFrameNode(
    ReferenceFrameId Id,
    ReferenceFrameId? ParentId,
    ReferenceFrameKind Kind,
    string DiagnosticName);
