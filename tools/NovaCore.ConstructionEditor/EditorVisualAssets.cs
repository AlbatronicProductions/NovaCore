using System.Collections.Immutable;
using NovaCore.Graphics;

namespace NovaCore.ConstructionEditor;

internal static class EditorVisualAssets
{
    // Definition references may share verified art. Upload each exact asset once;
    // conflicting content under one identity still fails the upload lease.
    internal static ReusablePartVisuals Prepare(IEnumerable<PartVisualAsset> references)=>
        new(references.DistinctBy(a=>(a.Identity,a.Sha256)).ToImmutableArray());
}
