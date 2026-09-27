using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

internal static class EditorRenderCapacity
{
    // A refused placement can contain one entire new symmetry set beyond the
    // document bound. It replaces the current drawing, not appends to it.
    // Also reserve all socket markers and one free held-part drawing. These are
    // conservative simultaneous bounds; no accepted part/depth policy changes.
    // Flight reserves one plume per authored actuator and the single slab.
    // Catalog-derived dimensions change with authored hardware, not a jet cap.
    internal static int Required(int maximumMeshes,int maximumPlacementMembers,int maximumActuators)
    {
        if(maximumMeshes<1||maximumPlacementMembers<1||maximumActuators<0)throw new ArgumentOutOfRangeException();
        var editor=checked((CompiledConstructionDesign.MaximumParts+maximumPlacementMembers+1)*maximumMeshes+EditorSocketTargets.DisplayCapacity);
        var flight=checked(CompiledConstructionDesign.MaximumParts*(maximumMeshes+maximumActuators)+1);
        return Math.Max(editor,flight);
    }
}
