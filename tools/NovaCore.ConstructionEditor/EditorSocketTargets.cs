using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

// Presentation over instance-owned authored endpoints. Marker position is only
// a camera-ray lift; Point remains the sole assembly-space mating authority.
internal readonly record struct EditorSocketTarget(string Parent,string Target,string Mount,Double3 Point,
    double X,double Y,double Depth,bool Available,bool Visible,Double3 Marker,double Scale,int Count=1);

internal static class EditorSocketTargets
{
    internal const int DisplayCapacity=1024;
    internal static bool Rebuild(List<EditorSocketTarget> result,CompiledConstructionDesign design,
        PartDefinitionData child,int count,int clock,string? selected,ImmutableArray<string> moving,
        EditorCamera camera,double width,double height,IReadOnlyDictionary<string,PartVisualAsset> meshes)
    {
        result.Clear();var overflow=false;
        // Reconnect preserves the selected group's membership. The global selector
        // describes a new placement only; PrepareReconnect uses this same count.
        if(!moving.IsDefaultOrEmpty)count=moving.Length;
        // The selected host remains reachable even at the bounded display limit.
        for(var pass=0;pass<2;pass++)foreach(var p in design.Parts){
            if((p.Instance.Id==selected)!=(pass==0))continue;
            foreach(var a in p.Definition.Standard!.Mechanical){
                var mount=child.Standard!.Mechanical.FirstOrDefault(m=>m.Role==MateRole.Plug&&PartStandard.CanMate(a,m,clock));
                if(mount is null)continue;
                var targets=a.Kind==MechanicalKind.Radial?p.Definition.Standard.SocketGroups.SelectMany(g=>g.Placements)
                    .FirstOrDefault(set=>set.Anchor==a.Interface&&set.Count==count)?.Sockets??[]:[a.Interface];
                var available=targets.Length>0&&!moving.Contains(p.Instance.Id)&&targets.All(target=>!design.Data.Connections.Any(edge=>
                    !moving.Contains(edge.Child)&&((edge.Parent==p.Instance.Id&&edge.ParentEndpoint==target)||(edge.Child==p.Instance.Id&&edge.ChildEndpoint==target))));
                var point=p.Instance.Pose.Then(p.Definition.Attachments.Single(x=>x.Id==a.Interface).Frame).Position;
                var screen=camera.Project(point,width,height);
                if(screen.Depth<=.02||screen.X<0||screen.Y<0||screen.X>width||screen.Y>height)continue;
                if(result.Count==DisplayCapacity){overflow=true;continue;}
                var scale=Math.Clamp(screen.Depth*.012,.045,.16);
                var visible=TryMarker(design.Parts,meshes,p.Instance.Id,point,camera.Eye,scale,out var marker,out var markerScale);
                result.Add(new(p.Instance.Id,a.Interface,mount.Interface,point,screen.X,screen.Y,screen.Depth,available,visible,marker,markerScale,a.Kind==MechanicalKind.Radial?count:1));
            }
        }
        return overflow;
    }

    internal static EditorSocketTarget? Pick(IReadOnlyList<EditorSocketTarget> targets,double x,double y)
    {
        EditorSocketTarget? best=null;var bestDistance=double.PositiveInfinity;
        foreach(var candidate in targets){
            if(!candidate.Visible)continue;
            var distance=Math.Pow(candidate.X-x,2)+Math.Pow(candidate.Y-y,2);if(distance>18*18)continue;
            var score=Math.Round(distance*4);
            // An occupied foreground endpoint must not steal a usable endpoint.
            // Retain an unavailable fallback for the existing precise refusal path.
            if(best is {} b){
                if(b.Available!=candidate.Available){if(!candidate.Available)continue;}
                else if(score>bestDistance)continue;
                else if(score==bestDistance){
                    if(candidate.Depth>b.Depth)continue;
                    if(candidate.Depth==b.Depth&&CompareIdentity(candidate,b)>=0)continue;
                }
            }
            best=candidate;bestDistance=score;
        }
        return best;
    }
    private static int CompareIdentity(EditorSocketTarget a,EditorSocketTarget b)
    {var c=StringComparer.Ordinal.Compare(a.Parent,b.Parent);return c!=0?c:StringComparer.Ordinal.Compare(a.Target,b.Target);}

    internal static bool TryMarker(ImmutableArray<CompiledPart> parts,IReadOnlyDictionary<string,PartVisualAsset> meshes,
        string host,Double3 point,Double3 eye,double scale,out Double3 marker,out double markerScale)
    {
        marker=point;markerScale=scale;
        var delta=point-eye;var distance=Math.Sqrt(delta.LengthSquared);if(distance<=.02)return false;
        var ray=delta/distance;var front=distance;var other=double.PositiveInfinity;
        foreach(var part in parts){
            var inverse=part.Instance.Pose.Rotation.Transpose();
            var hit=PartVisualPicking.Intersect(meshes[part.Definition.Id],inverse.Apply(eye-part.Instance.Pose.Position),inverse.Apply(ray));
            if(hit is not {} d)continue;
            if(part.Instance.Id==host)front=Math.Min(front,d);else other=Math.Min(other,d);
        }
        // A host cannot hide its own connector gizmo. Other craft parts still
        // occlude it. This does not change mating, occupancy, collision or clearance.
        if(other<front-Math.Sqrt(3)*.065/2)return false;
        // Lift only along the projection ray, by enough for the active 1.35x cube.
        // Scale by the same depth ratio so the marker keeps its screen footprint.
        var displayDistance=Math.BitDecrement(front/(1+Math.Sqrt(3)*scale*1.35/(2*distance)));
        if(displayDistance<=.02)return false;
        marker=eye+ray*displayDistance;markerScale=scale*displayDistance/distance;return true;
    }
}
