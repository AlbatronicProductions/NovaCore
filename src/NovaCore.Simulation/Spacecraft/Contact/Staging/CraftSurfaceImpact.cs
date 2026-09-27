using BepuPhysics.CollisionDetection;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Post-flight rigid contact qualification, separate from the authored
/// launch-support foot rating. This does not predict structural damage.</summary>
internal static class CraftSurfaceImpact
{
    internal const double MaximumNativeSliceSeconds=1d/1024;
    // Each trial retains the existing 256-slice work capacity. At most five
    // trials (four dyadic refinements) therefore permit 1280 private slices
    // including resource-phase rounding and discarded trials. Refinement
    // never changes 64Hz publication, exact resource events or velocity.
    internal const int MaximumRefinement=4;
    // Current KSA's ordinary spacecraft pair: Coulomb friction and BEPU's
    // compliant, critically damped normal constraint. Launch-support material
    // remains unchanged in its separate owner.
    // There is no elastic restitution impulse or velocity clamp. Recovery is
    // the penetration-bias limit, not a requested rebound speed.
    internal static PairMaterialProperties Material=>new(.95f,1.5f,new(30,1));
    // The pinned spring bias coefficient is omega/(omega*h+2*zeta).
    // Its recovery cap first engages at R*(h+2*zeta/omega). Taking the
    // infimum over h>0 describes the earliest recovery-saturation transition.
    // Saturation is ordinary BEPU behavior, not an admission or strength limit.
    internal static double MinimumRecoverySaturationDepth
    {
        get{var m=Material;return Math.BitDecrement((double)m.MaximumRecoveryVelocity*m.SpringSettings.TwiceDampingRatio/m.SpringSettings.AngularFrequency);}
    }
    // Motion is bounded by each actual hull's width and the native slice;
    // the ordinary recovery bias remains solver owned. Neither is an invented
    // touchdown-strength coefficient. Structural damage is not implemented.
    internal static bool FiniteContactSpeed(double closingSpeed)=>double.IsFinite(closingSpeed);
}
