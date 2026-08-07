using NovaCore.Core;
using NovaCore.Core.Camera;

namespace NovaCore.Graphics;

/// <summary>Root-resolved, non-authoritative input supplied by a celestial presentation bridge.</summary>
public readonly record struct EvaluatedPlanetaryBody(ulong BodyId,UniversePosition Position,double RadiusMetres,Float3 Color,string? Label,bool Visible);

/// <summary>Immutable renderer-side planet presentation data. It contains no hierarchy, ephemeris, or time authority.</summary>
public readonly record struct PlanetRenderProxy(ulong BodyId,UniversePosition Position,double RadiusMetres,Float3 Color,string? Label,bool Visible)
{
    internal bool IsValid=>BodyId!=0&&Position.Value.IsFinite&&double.IsFinite(RadiusMetres)&&RadiusMetres>0&&Color.IsFinite;
}

/// <summary>Immutable copied body-proxy publication consumed by planetary rendering and presentation controls only.</summary>
public sealed class PlanetaryPresentationSnapshot
{
    private readonly PlanetRenderProxy[] _bodies;
    private PlanetaryPresentationSnapshot(ReferenceFrameId rootFrame,PlanetRenderProxy[] bodies){RootFrame=rootFrame;_bodies=bodies;}
    public ReferenceFrameId RootFrame{get;}
    public int Count=>_bodies.Length;
    public ReadOnlySpan<PlanetRenderProxy> Bodies=>_bodies;
    public bool TryGetBody(ulong bodyId,out PlanetRenderProxy body){foreach(var candidate in _bodies)if(candidate.BodyId==bodyId){body=candidate;return true;}body=default;return false;}
    public static bool TryCreate(ReadOnlySpan<PlanetRenderProxy> bodies,out PlanetaryPresentationSnapshot? snapshot)
    {
        snapshot=null;if(bodies.Length==0)return false;var root=bodies[0].Position.Frame;
        for(var index=0;index<bodies.Length;index++){if(!bodies[index].IsValid||bodies[index].Position.Frame!=root)return false;for(var prior=0;prior<index;prior++)if(bodies[prior].BodyId==bodies[index].BodyId)return false;}
        snapshot=new(root,bodies.ToArray());return true;
    }
}

/// <summary>Generic bridge from evaluated root-resolved bodies into renderer-only proxies. It has no body-specific rules.</summary>
public static class PlanetaryBodyPresentationProvider
{
    public static bool TryCreateSnapshot(ReadOnlySpan<EvaluatedPlanetaryBody> bodies,out PlanetaryPresentationSnapshot? snapshot)
    {
        var proxies=new PlanetRenderProxy[bodies.Length];for(var index=0;index<bodies.Length;index++){var body=bodies[index];proxies[index]=new(body.BodyId,body.Position,body.RadiusMetres,body.Color,body.Label,body.Visible);}return PlanetaryPresentationSnapshot.TryCreate(proxies,out snapshot);
    }
}

/// <summary>Builds the reusable whole-body proxy used only when presentation policy selects the far field.</summary>
public static class FarFieldPlanetaryRenderProxyProvider
{
    public static bool TryBuild(PlanetaryPresentationSnapshot snapshot,double radiusScale,double minimumApparentRadius,Span<ResolvedRenderObject> destination,out int count)
    {
        ArgumentNullException.ThrowIfNull(snapshot);count=0;if(!double.IsFinite(radiusScale)||radiusScale<=0||!double.IsFinite(minimumApparentRadius)||minimumApparentRadius<0||destination.Length<snapshot.Count)return false;
        foreach(ref readonly var body in snapshot.Bodies){if(!body.Visible)continue;var radius=Math.Max(body.RadiusMetres*radiusScale,minimumApparentRadius);destination[count++]=new(new RenderObjectId(checked((uint)body.BodyId)),body.Position,DoubleQuaternion.Identity,new Double3(radius,radius,radius),MeshHandle.Sphere);}return true;
    }
}

/// <summary>Presentation-only body focus. It changes the camera pose and never writes a celestial or snapshot value.</summary>
public static class PlanetaryCameraFocus
{
    public static bool TryFocus(CameraState camera,PlanetaryPresentationSnapshot snapshot,ulong bodyId,double distanceMetres)
    {
        ArgumentNullException.ThrowIfNull(camera);ArgumentNullException.ThrowIfNull(snapshot);if(!double.IsFinite(distanceMetres)||distanceMetres<=0||!snapshot.TryGetBody(bodyId,out var body))return false;
        camera.Position=new(new ReferenceFrameId(snapshot.RootFrame.Value),body.Position.Value+new Double3(0,0,distanceMetres));return true;
    }
}
