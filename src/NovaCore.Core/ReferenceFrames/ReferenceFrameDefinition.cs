namespace NovaCore.Core.ReferenceFrames;

/// <summary>Immutable identity metadata; no evaluated state or time-varying transform belongs here.</summary>
public readonly record struct ReferenceFrameDefinition(ReferenceFrameId Id, ReferenceFrameId? ParentId, ReferenceFrameKind Kind, string DiagnosticName);
