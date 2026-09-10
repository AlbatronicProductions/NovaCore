using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum FloridaKinematicsStatus : byte { Unresolved, Qualified, Unsupported, Stale }
internal enum FloridaKinematicsFailure : byte { None, InvalidProof, InvalidRequest, ChangedAuthority, NumericalResolution, RefinementBudget }

/// <summary>Maximum full interval widths, not contact tolerances. Vector limits apply to every component.</summary>
internal readonly record struct FloridaKinematicsRequest(double RootSeconds,double NormalComponent,
    double NormalVelocityMetresPerSecond,double LeverComponentMetres,int RefinementBudget)
{
    internal bool IsValid=>Positive(RootSeconds)&&Positive(NormalComponent)&&Positive(NormalVelocityMetresPerSecond)&&
        Positive(LeverComponentMetres)&&RefinementBudget is >=0 and <=FloridaContactProvider.MaximumRefinements;
    private static bool Positive(double x)=>double.IsFinite(x)&&x>0;
    internal static double Width(FloridaBound x)=>(FloridaBound.Point(x.Upper)-x.Lower).Upper;
    internal static bool Fits(FloridaVector v,double width)=>Width(v.X)<=width&&Width(v.Y)<=width&&Width(v.Z)<=width;
}

/// <summary>
/// A checked snapshot of functions of one alpha. Component intervals are not independently selectable states.
/// The stored attitude denotes its exact normalization, not a new rounded quaternion authority.
/// No field authorizes impulse application or authoritative replacement.
/// </summary>
internal readonly record struct FloridaContactKinematics(FloridaContactProvider.Proof OriginalRoot,
    FloridaBound RootEnclosure,FloridaVector NormalRoot,FloridaBound NormalVelocityMetresPerSecond,
    Double3 AuthoredLeverBodyMetres,FloridaVector LeverRootMetres,DoubleQuaternion FixedAttitude,
    double MassKilograms,PrincipalMomentsOfInertia PrincipalInertia,PhysicalSurfaceAuthorityIdentity TerrainAuthority,
    int AdditionalRefinements)
{
    internal uint ProviderVersion=>FloridaContactProvider.ProviderVersion;
    internal uint RelationVersion=>FloridaContactProvider.RelationVersion;
}

internal readonly record struct FloridaKinematicsResult(FloridaKinematicsStatus Status,FloridaKinematicsFailure Failure,
    FloridaContactProvider.Proof.Kinematics Witness=default);

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal FloridaKinematicsResult QualifyKinematics(in FloridaKinematicsRequest request,in FloridaContactUse current)=>
            Kinematics.Qualify(this,request,current);

        /// <summary>Provider-controlled evidence. Every consumption rechecks authority and exact issuing-root lineage.</summary>
        internal readonly partial struct Kinematics
        {
            private readonly Proof original;
            private readonly FloridaContactKinematics values;
            private Kinematics(in Proof original,in FloridaContactKinematics values)
            {this.original=original;this.values=values;}

            private static bool Applicable(FloridaContactProvider owner,in FloridaContactUse current)=>
                current.Engine is not null&&current.Geometry is not null&&current.System is not null&&current.Graph is not null&&
                current.Terrain is not null&&owner.IsApplicable(current.Engine,current.Geometry,current.System,current.Graph,current.Terrain);

            internal FloridaKinematicsStatus Read(in Proof expectedRoot,in FloridaContactUse current,out FloridaContactKinematics result)
            {
                result=default;
                if(!original.IsRoot||!expectedRoot.IsRoot||!ReferenceEquals(original.owner,expectedRoot.owner))
                    return FloridaKinematicsStatus.Unsupported;
                if(!Applicable(original.owner!,current))return FloridaKinematicsStatus.Stale;
                result=values;return FloridaKinematicsStatus.Qualified;
            }

            // Public-to-this-internal-type entry still performs the complete checked issuance path.
            internal static FloridaKinematicsResult Qualify(in Proof root,in FloridaKinematicsRequest request,in FloridaContactUse current)
            {
                if(!root.IsRoot)return new(FloridaKinematicsStatus.Unsupported,FloridaKinematicsFailure.InvalidProof);
                var owner=root.owner!;
                if(!Applicable(owner,current))return new(FloridaKinematicsStatus.Stale,FloridaKinematicsFailure.ChangedAuthority);
                if(!request.IsValid)return new(FloridaKinematicsStatus.Unsupported,FloridaKinematicsFailure.InvalidRequest);
                var bracket=root.RootEnclosure;
                // At most budget+1 value evaluations and budget sign/domain evaluations; no mutable workspace.
                for(var count=0;count<=request.RefinementBudget;count++)
                {
                    if(!owner.motion.ContactKinematics(bracket,owner.region.Up,out var normal,out var speed,out var lever))
                        return new(FloridaKinematicsStatus.Unresolved,FloridaKinematicsFailure.NumericalResolution);
                    if(!FloridaKinematicsRequest.Fits(lever,request.LeverComponentMetres))
                        return new(FloridaKinematicsStatus.Unresolved,FloridaKinematicsFailure.NumericalResolution); // fixed, cannot improve by time refinement
                    if(FloridaKinematicsRequest.Width(bracket)<=request.RootSeconds&&
                        FloridaKinematicsRequest.Fits(normal,request.NormalComponent)&&
                        FloridaKinematicsRequest.Width(speed)<=request.NormalVelocityMetresPerSecond)
                    {
                        var data=new FloridaContactKinematics(root,bracket,normal,speed,owner.geometry.GetFeature(0).OffsetFromComMetres,
                            lever,owner.angular.OrientationLocalToParent,owner.properties.MassKilograms,owner.angular.PrincipalInertia,
                            owner.authority,count);
                        return new(FloridaKinematicsStatus.Qualified,FloridaKinematicsFailure.None,new(root,data));
                    }
                    if(count==request.RefinementBudget)return new(FloridaKinematicsStatus.Unresolved,FloridaKinematicsFailure.RefinementBudget);
                    if(!TryNarrow(owner,bracket,out bracket))return new(FloridaKinematicsStatus.Unresolved,FloridaKinematicsFailure.NumericalResolution);
                }
                return new(FloridaKinematicsStatus.Unresolved,FloridaKinematicsFailure.RefinementBudget);
            }
        }
    }
}
