namespace NovaCore.Core.Surface;

/// <summary>
/// Trusted assembly capability, implemented on the acquired physical-query owner itself.
/// A public query wrapper with copied provenance cannot acquire this capability.
/// It attests the current canonical composition reduces mathematically to this grading
/// plane on its full-weight branch, with finite earlier terms. Not a replacement collider.
/// </summary>
internal interface IPhysicalGradingProofSource : IPhysicalSurfacePointQuery
{
    FacilitySupportRegion GradingRegion { get; }
    bool IsNumericalFullWeight(in Double3 direction);
}
