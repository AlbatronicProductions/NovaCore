using System.Collections.Immutable;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyCollisionBox(string Part, string DefinitionDigest,
    Double3 Dimensions, AssemblyPose PartLocal, AssemblyPose AtOrigin);

/// <summary>
/// Authored physical envelope profile, separate from rendering and mass authority.
/// The boxes include curved corners/nozzle cavities; only the declared upright
/// unpowered placement is admitted. No mesh loader or inertia builder supplies geometry.
/// </summary>
internal sealed class AssemblyContactProfile
{
    internal const string ProfileId = "srv01-fourhorn-upright-box-envelopes/1";
    internal CompiledAssemblyDesign Design { get; }
    internal ImmutableArray<AssemblyCollisionBox> Children { get; }
    internal string Digest { get; }
    internal double SmallestFeature { get; }
    internal double BoundingRadius { get; }
    internal AssemblyMass Mass { get; }
    internal static DoubleQuaternion Upright => new(0, 0, Math.Sqrt(.5), Math.Sqrt(.5));
    internal const double SupportPlaneAtOrigin = -1.7;

    private AssemblyContactProfile(CompiledAssemblyDesign design)
    {
        if (design.Digest != "ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf")
            throw new InvalidDataException("Collision profile requires the unchanged registered FourHorn design.");
        Design = design;
        Mass = AssemblyLaunch.ObserveMass(design, new(AssemblyResources.Mass(design.Data.Design.InitialFuelKg), AssemblyResources.Mass(design.Data.Design.InitialOxidizerKg)));
        var children = ImmutableArray.CreateBuilder<AssemblyCollisionBox>(8);
        foreach (var part in design.Parts)
        {
            void Add(Double3 dimensions, Double3 centre)
            {
                var local = new AssemblyPose(centre, Matrix3.Identity);
                children.Add(new(part.Instance.Id, part.Instance.Definition.Digest, dimensions, local, part.Instance.Pose.Then(local)));
            }
            switch (part.Definition.Role)
            {
                case AssemblyRole.Command: Add(new(1.354, 1.440, 1.456), new(.677, 0, -.008)); break;
                case AssemblyRole.Tank: Add(new(2, 1.4, 1.4), Double3.Zero); break;
                case AssemblyRole.MainEngine:
                    Add(new(.639, .52, .52), new(-.3805, 0, 0));
                    Add(new(.061, .56, .56), new(-.0305, 0, 0)); break;
                case AssemblyRole.RcsBlock: Add(new(.368, .185, .368), new(0, -.2075, 0)); break;
                default: throw new InvalidDataException("Unsupported contact part.");
            }
        }
        Children = children.MoveToImmutable();
        SmallestFeature = double.MaxValue;
        foreach (var child in Children)
        {
            SmallestFeature = Math.Min(SmallestFeature, Math.Min(child.Dimensions.X, Math.Min(child.Dimensions.Y, child.Dimensions.Z)));
            for (var corner = 0; corner < 8; corner++)
                BoundingRadius = Math.Max(BoundingRadius, Math.Sqrt((CornerAtOrigin(child, corner) - Mass.Com).LengthSquared));
        }
        Digest = AssemblyJson.Digest(new { ProfileId, DesignDigest = design.Digest, Children });
    }

    internal static AssemblyContactProfile Create(CompiledAssemblyDesign design) => new(design);
    internal static Double3 CornerAtOrigin(AssemblyCollisionBox child, int corner) => child.AtOrigin.Point(new(
        ((corner & 1) == 0 ? -.5 : .5) * child.Dimensions.X,
        ((corner & 2) == 0 ? -.5 : .5) * child.Dimensions.Y,
        ((corner & 4) == 0 ? -.5 : .5) * child.Dimensions.Z));

    internal static (Double3 Position, Double3 Velocity) ToCom(in AssemblyMotion motion, Double3 c) =>
        (motion.PositionO + motion.BodyToWorld.Rotate(c),
         motion.VelocityO + motion.BodyToWorld.Rotate(Double3.Cross(motion.AngularVelocityBody, c)));

    internal static AssemblyMotion ToOrigin(Double3 p, Double3 v, DoubleQuaternion q, Double3 w, Double3 c) =>
        new(p - q.Rotate(c), v - q.Rotate(Double3.Cross(w, c)), q, w);

    internal double RadiusAbout(Double3 com)
    {
        var radius=0d;
        foreach(var child in Children)for(var i=0;i<8;i++)
            radius=Math.Max(radius,Math.Sqrt((CornerAtOrigin(child,i)-com).LengthSquared));
        return radius;
    }
    internal bool AdmitsSupportedEndpoint(in AssemblyMotion motion,Double3 frameDisplacement,double tolerance,Double3? currentCom=null)
    {
        var minimum=double.MaxValue;var q=motion.BodyToWorld;var p=motion.PositionO-frameDisplacement;
        AssemblyCollisionBox aft=default;
        foreach(var child in Children)
        {
            if(child.Part==Design.Main.Instance.Id&&child.Dimensions.X==.639)aft=child;
            for(var i=0;i<8;i++)
            {
                var point=p+q.Rotate(CornerAtOrigin(child,i));
                if(!point.IsFinite||Math.Abs(point.X)>=8||Math.Abs(point.Z)>=8)return false;
                minimum=Math.Min(minimum,point.Y-SupportPlaneAtOrigin);
            }
        }
        if(minimum < -.020||minimum>2*tolerance)return false;
        // Project the canonical COM into the declared aft support parallelogram.
        // Loss of this bounded support domain poisons continuation, never releases ownership.
        var a=p+q.Rotate(CornerAtOrigin(aft,0));
        var u=q.Rotate(aft.AtOrigin.Rotation.Apply(new(0,aft.Dimensions.Y,0)));
        var v=q.Rotate(aft.AtOrigin.Rotation.Apply(new(0,0,aft.Dimensions.Z)));
        var r=p+q.Rotate(currentCom??Mass.Com)-a;var determinant=u.X*v.Z-u.Z*v.X;
        if(!double.IsFinite(determinant)||Math.Abs(determinant)<double.Epsilon)return false;
        var s=(r.X*v.Z-r.Z*v.X)/determinant;var t=(u.X*r.Z-u.Z*r.X)/determinant;
        return s>=0&&s<=1&&t>=0&&t<=1;
    }
}
