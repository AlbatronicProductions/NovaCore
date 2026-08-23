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
    public const double OrbitalTransitionAltitudeMetres=1_000_000d,SurfaceLocalAltitudeMetres=100_000d,MinimumPitchRadians=-1.45d,MaximumPitchRadians=1.35d,MinimumNearClipMetres=.05d,MaximumNearClipMetres=1_000_000d,NearClipAltitudeFraction=.02d;
    public static double SurfaceBlend(double altitudeMetres){if(!double.IsFinite(altitudeMetres))throw new ArgumentOutOfRangeException(nameof(altitudeMetres));var t=Math.Clamp((OrbitalTransitionAltitudeMetres-altitudeMetres)/(OrbitalTransitionAltitudeMetres-SurfaceLocalAltitudeMetres),0d,1d);return t*t*(3d-2d*t);}
    public static PlanetaryCameraPresentationMode Mode(double altitudeMetres)=>altitudeMetres<=SurfaceLocalAltitudeMetres?PlanetaryCameraPresentationMode.SurfaceLocal:altitudeMetres<OrbitalTransitionAltitudeMetres?PlanetaryCameraPresentationMode.Transition:PlanetaryCameraPresentationMode.Orbital;
    public static double ZoomFactor(double altitudeMetres)=>altitudeMetres switch{>1_000_000d=>1.6d,>100_000d=>1.35d,>10_000d=>1.22d,_=>1.12d};
    public static double NearClipMetres(double surfaceAltitudeMetres){if(!double.IsFinite(surfaceAltitudeMetres))throw new ArgumentOutOfRangeException(nameof(surfaceAltitudeMetres));return Math.Clamp(Math.Max(0d,surfaceAltitudeMetres)*NearClipAltitudeFraction,MinimumNearClipMetres,MaximumNearClipMetres);}
    public static double TranslationSpeedMetresPerSecond(double altitudeMetres){if(!double.IsFinite(altitudeMetres)||altitudeMetres<0d)throw new ArgumentOutOfRangeException(nameof(altitudeMetres));return Math.Clamp(12d+altitudeMetres*.02d,12d,2_000d);}
}
