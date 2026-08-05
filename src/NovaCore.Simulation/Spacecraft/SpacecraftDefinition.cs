using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft;

/// <summary>Immutable spacecraft identity and its sole existing frame ownership relationship.</summary>
internal readonly record struct SpacecraftDefinition(
    SpacecraftId Id,
    ReferenceFrameId CarrierFrame,
    ReferenceFrameId BodyFrame,
    string DiagnosticName);
