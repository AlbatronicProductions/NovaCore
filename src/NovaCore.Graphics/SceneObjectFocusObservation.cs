using NovaCore.Core;

namespace NovaCore.Graphics;

public enum SceneObjectFocusStatus : byte { Prepared, Active, Held, Failed, Retired }

/// <summary>
/// Orbit-view basis, separate from focus translation and environmental terrain.
/// ParentBodyId zero names the presentation root; otherwise LocalToParent is a
/// fixed basis in that body's rotating frame. The publisher owns frame changes.
/// Resolve both sides of a change at the observation's display epoch and rebase
/// the retained view, preserving its complete world orientation (including roll).
/// </summary>
public readonly record struct SceneObjectFocusReferenceFrame(ulong ParentBodyId, DoubleQuaternion LocalToParent)
{
    public static SceneObjectFocusReferenceFrame Root => new(0, DoubleQuaternion.Identity);
    public bool IsValid => LocalToParent.IsFinite && Math.Abs(LocalToParent.LengthSquared - 1d) <= 1e-10;
}

/// <summary>
/// One copied presentation endpoint. Identity names the canonical object; generation
/// scopes it to one publisher lifetime. Physical and display epochs are deliberately
/// separate: this record grants no simulation or command authority.
/// </summary>
public readonly record struct SceneObjectFocusObservation(
    ulong CanonicalId, ulong Generation, ulong EnvironmentalBodyId,
    UniversePosition MaterialOrigin, long PublicationRevision,
    long ObservationTicks, long DisplayTicks, SceneObjectFocusStatus Status)
{
    public SceneObjectFocusReferenceFrame ReferenceFrame { get; init; } = SceneObjectFocusReferenceFrame.Root;
    public bool IsAvailable => CanonicalId != 0 && Generation != 0 &&
        EnvironmentalBodyId != 0 && MaterialOrigin.Value.IsFinite && ReferenceFrame.IsValid &&
        Status is SceneObjectFocusStatus.Prepared or SceneObjectFocusStatus.Active or SceneObjectFocusStatus.Held;
}
