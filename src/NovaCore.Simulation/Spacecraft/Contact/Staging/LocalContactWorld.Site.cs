using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    internal LocalContactStatus PrepareAssemblySite(SimulationTransactionEngine engine,AssemblyMass mass,SimulationInstant time)
    {
        var status=CheckAssemblyReady(engine);
        if(status!=LocalContactStatus.Success)return status;
        if(!engine.OwnsPersistentPublicationPhase||assembly is not {Pending:false}||assemblyInput?.Site is not {} site||
            mass!=assemblyMass||assemblyPreparedFrontier!=0||time!=export.Motion.Time||!site.Applicable)
            return LocalContactStatus.InvalidSource;
        assemblyInput.SiteFrame=site.At(time);assemblyInput.SiteInertia=mass.Inertia;
        var b=simulation.Bodies[body];
        var linear=site.LinearAcceleration(assemblyInput.SiteFrame,FromFloat(b.Pose.Position)+configuration.OriginRoot,FromFloat(b.Velocity.Linear));
        if(!linear.IsFinite||!Finite(ToFloat(linear)))return LocalContactStatus.PrecisionEnvelopeExceeded;
        assemblyInput.Linear=ToFloat(linear);
        metrics.Coverage?.SetPreparedAcceleration(assemblyInput.Linear);
        assemblyPreparedFrontier=frontier+1;
        return LocalContactStatus.Success;
    }
}
