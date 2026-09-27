using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Time;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Canonical physical endpoint and command/clock frontier. Native
/// handles, contacts and warmstarts are reconstructed, never persistent authority.</summary>
internal sealed record ConstructionFlightCheckpoint(string Schema,Guid FlightId,ConstructionDesignData Design,string Compiled,
    PhysicalSurfaceAuthorityIdentity Terrain,string Site,long SiteStartTicks,long EpochTicks,long Sequence,ulong Revision,
    byte[] Fuel,byte[] Power,AssemblyMass Mass,ConstructionPhysicalState Physical,long ControlSequence,AssemblyControlRequest Requested,
    long ArbitrationFrontier,bool OffAtFrontier,long HostSequence,long LastHostTicks,long DebtTicks)
{
    internal const string SchemaId="novacore.construction-flight/1";
    internal void Validate()
    {
        Require(Schema==SchemaId&&FlightId!=Guid.Empty&&Design is not null&&Compiled is not null&&Site is not null&&
            Fuel is not null&&Power is not null&&Physical is not null,"Invalid physical checkpoint schema or fields.");
        Require(Sequence>=0&&EpochTicks>=SiteStartTicks&&(Int128)EpochTicks-SiteStartTicks==(Int128)Sequence*15625&&
            ControlSequence>=0&&HostSequence>=0&&LastHostTicks>=0&&(HostSequence==0)==(LastHostTicks==0)&&DebtTicks>=0&&
            ArbitrationFrontier>=-1&&ArbitrationFrontier<=Sequence&&(!OffAtFrontier||ArbitrationFrontier>=0)&&
            Requested.Pilot.IsValid&&!Requested.PilotOnly,"Invalid physical checkpoint frontier.");
        Require((ControlSequence!=0||Requested==default&&ArbitrationFrontier==-1&&!OffAtFrontier)&&
            (ControlSequence==0||ArbitrationFrontier>=0)&&(!OffAtFrontier||!Requested.MainOn)&&
            (Sequence==0&&DebtTicks==0||HostSequence>0)&&Revision>=(ulong)Sequence,"Inconsistent physical checkpoint frontier.");
        var totalCredit=(Int128)EpochTicks-SiteStartTicks+DebtTicks;
        Require((HostSequence==0)==(totalCredit==0)&&(HostSequence==0||
            (Int128)LastHostTicks+HostSequence-1<=totalCredit&&
            (HostSequence!=1||LastHostTicks==totalCredit)),"Impossible physical checkpoint host credit.");
        Require(Physical.Motion.Finite&&Math.Abs(Physical.Motion.BodyToWorld.LengthSquared-1)<=1e-12&&
            Physical.Consumer is AssemblyPhysicalConsumer.FreeFlight or AssemblyPhysicalConsumer.SupportedContact or AssemblyPhysicalConsumer.SurfaceContact,
            "Invalid physical checkpoint motion or consumer.");
    }
    internal ConstructionRuntimeState RestoreState(ConstructionRuntimeBinding binding)
    {
        Validate();var physical=binding.Physical!;
        Require(physical.Craft.Digest==Compiled&&physical.Site.Digest==Site&&physical.Site.Authority==Terrain&&
            EpochTicks<=physical.Site.End.Ticks&&(Int128)EpochTicks+DebtTicks<=physical.Site.End.Ticks,"Physical checkpoint dependency or epoch mismatch.");
        var fuel=ConstructionFuelState.Load(binding.Fuel,Fuel);var power=ConstructionPowerState.Load(binding.Power,Power);
        Require(fuel.Events==power.FuelEvents,"Physical checkpoint resource event mismatch.");
        _=ConstructionPowerSolver.AdvanceContinuous(physical.Services,power,0); // Validate the joint, prepared physical bus contract without spending.
        // This physical profile has constant admitted loads and no generation
        // or runtime load switches. Its electrical endpoint is reconstructible
        // independently of pilot history and must not create battery credit.
        var expectedPower=ConstructionPowerSolver.AdvanceContinuous(physical.Services,binding.Power.Initial(),checked(EpochTicks-SiteStartTicks)).Finish(fuel.Events);
        Require(expectedPower.Save().SequenceEqual(Power),"Physical checkpoint electrical endpoint differs from elapsed authority time.");
        var mass=binding.ReferenceMass(fuel)!.Value;
        Require(mass==Mass,"Physical checkpoint resource-derived mass mismatch.");
        var g=Physical.Gimbal;var limits=physical.Control.Main.Gimbal!;
        Require(double.IsFinite(g.ActualY)&&double.IsFinite(g.ActualZ)&&double.IsFinite(g.TargetY)&&double.IsFinite(g.TargetZ)&&
            Math.Abs(g.ActualY)<=limits.LimitY&&Math.Abs(g.TargetY)<=limits.LimitY&&Math.Abs(g.ActualZ)<=limits.LimitZ&&Math.Abs(g.TargetZ)<=limits.LimitZ,
            "Physical checkpoint gimbal outside authored travel.");
        var motion=Physical.Motion;var frame=physical.Site.At(new(EpochTicks));
        var com=AssemblyContactProfile.ToCom(motion,mass.Com);
        var derivative=AssemblyDynamics.CraftSiteDerivative(motion,mass,default,physical.Site,frame);
        Require(double.IsFinite(motion.PositionO.LengthSquared+motion.VelocityO.LengthSquared+motion.AngularVelocityBody.LengthSquared)&&
            com.Position.IsFinite&&com.Velocity.IsFinite&&derivative.Finite&&
            double.IsFinite(derivative.VelocityO.LengthSquared+derivative.AngularVelocityBody.LengthSquared)&&
            Contact.Staging.LocalContactConfiguration.RootSpacingFits(motion.PositionO,physical.Contact.ContactTolerance),
            "Physical checkpoint exceeds the derived numerical endpoint domain.");
        return new(fuel,power,new(EpochTicks),Sequence,mass){Physical=Physical};
    }
}
