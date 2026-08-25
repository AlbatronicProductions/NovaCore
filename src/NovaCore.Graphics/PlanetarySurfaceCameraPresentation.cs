using NovaCore.Core;

namespace NovaCore.Graphics;

public enum PlanetaryCameraPresentationMode { Orbital, Transition, SurfaceLocal }

public readonly record struct PlanetarySurfaceFocus(ulong BodyId,Double3 BodyLocalAnchor,PlanetarySurfaceFrame TangentFrame,Double3 LocalCameraOffset)
{
    public bool IsValid=>BodyId!=0&&BodyLocalAnchor.IsFinite&&TangentFrame.Direction.IsFinite&&LocalCameraOffset.IsFinite;
    public static PlanetarySurfaceFocus AtDirection(ulong bodyId,in Double3 direction,double surfaceRadius,double altitudeMetres)
    {
        if(bodyId==0||!double.IsFinite(surfaceRadius)||surfaceRadius<=0d||!double.IsFinite(altitudeMetres)||altitudeMetres<0d)throw new ArgumentOutOfRangeException();
        var frame=PlanetarySurfaceFrame.AtDirection(direction);return new(bodyId,frame.Direction*surfaceRadius,frame,frame.Up*altitudeMetres);
    }
}

public static class PlanetarySurfaceCameraPolicy
{
    public const double OrbitalTransitionAltitudeMetres=1_000_000d,SurfaceLocalAltitudeMetres=100_000d,
        MinimumPitchRadians=-88.5d*Math.PI/180d,MaximumPitchRadians=88.5d*Math.PI/180d,
        MinimumNearClipMetres=.05d,MaximumNearClipMetres=1_000_000d,NearClipAltitudeFraction=.02d,
        NormalSurfaceSpeedMetresPerSecond=60d,MaximumNormalSurfaceSpeedMetresPerSecond=1_000d,
        FastSurfaceSpeedMultiplier=5d,SlowSurfaceSpeedMultiplier=.2d;
    public static double SurfaceBlend(double altitudeMetres){if(!double.IsFinite(altitudeMetres))throw new ArgumentOutOfRangeException(nameof(altitudeMetres));var t=Math.Clamp((OrbitalTransitionAltitudeMetres-altitudeMetres)/(OrbitalTransitionAltitudeMetres-SurfaceLocalAltitudeMetres),0d,1d);return t*t*(3d-2d*t);}
    public static PlanetaryCameraPresentationMode Mode(double altitudeMetres)=>altitudeMetres<=SurfaceLocalAltitudeMetres?PlanetaryCameraPresentationMode.SurfaceLocal:altitudeMetres<OrbitalTransitionAltitudeMetres?PlanetaryCameraPresentationMode.Transition:PlanetaryCameraPresentationMode.Orbital;
    public static double ZoomFactor(double altitudeMetres)=>altitudeMetres switch{>1_000_000d=>1.6d,>100_000d=>1.35d,>10_000d=>1.22d,_=>1.12d};
    public static double NearClipMetres(double surfaceAltitudeMetres){if(!double.IsFinite(surfaceAltitudeMetres))throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));return Math.Clamp(Math.Max(0d,surfaceAltitudeMetres)*NearClipAltitudeFraction,MinimumNearClipMetres,MaximumNearClipMetres);}
    public static double TranslationSpeedMetresPerSecond(double altitudeMetres,bool fast=false,bool slow=false)
    {
        if(!double.IsFinite(altitudeMetres)||altitudeMetres<0d)throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        var normal=Math.Clamp(NormalSurfaceSpeedMetresPerSecond+altitudeMetres*.025d,
            NormalSurfaceSpeedMetresPerSecond,MaximumNormalSurfaceSpeedMetresPerSecond);
        return normal*(fast==slow?1d:fast?FastSurfaceSpeedMultiplier:SlowSurfaceSpeedMultiplier);
    }

    public static double ApplyPitchDelta(double currentPitchRadians,double deltaRadians)
    {
        if(!double.IsFinite(currentPitchRadians)||!double.IsFinite(deltaRadians)||
           currentPitchRadians < -Math.PI*.5d||currentPitchRadians > Math.PI*.5d)
            throw new ArgumentOutOfRangeException();
        // An exact pose-preserving attach may inherit a nadir/zenith view.  It can leave that
        // boundary continuously, but input may never drive it farther through the singularity.
        if(currentPitchRadians<MinimumPitchRadians)
            return deltaRadians<=0d?currentPitchRadians:Math.Min(currentPitchRadians+deltaRadians,MaximumPitchRadians);
        if(currentPitchRadians>MaximumPitchRadians)
            return deltaRadians>=0d?currentPitchRadians:Math.Max(currentPitchRadians+deltaRadians,MinimumPitchRadians);
        return Math.Clamp(currentPitchRadians+deltaRadians,MinimumPitchRadians,MaximumPitchRadians);
    }
}
