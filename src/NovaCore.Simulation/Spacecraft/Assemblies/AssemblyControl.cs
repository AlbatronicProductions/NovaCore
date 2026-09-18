using NovaCore.Simulation.Spacecraft;
using System.Text.Json.Serialization;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyControlIdentity(SpacecraftId Vessel, long Generation);
// Right-handed body axes: roll X, pitch Y, yaw Z. Raw intent, not torque or rate.
internal readonly record struct AssemblyPilotDemand(sbyte Pitch, sbyte Yaw, sbyte Roll)
{
    [JsonIgnore] internal bool IsValid => Pitch is >= -1 and <= 1 && Yaw is >= -1 and <= 1 && Roll is >= -1 and <= 1;
    internal static AssemblyPilotDemand FromOpposingRequests(bool pitchPositive, bool pitchNegative,
        bool yawPositive, bool yawNegative, bool rollPositive, bool rollNegative) =>
        new((sbyte)((pitchPositive?1:0)-(pitchNegative?1:0)),
            (sbyte)((yawPositive?1:0)-(yawNegative?1:0)),
            (sbyte)((rollPositive?1:0)-(rollNegative?1:0)));
}
internal readonly record struct AssemblyControlRequest(bool MainOn,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] AssemblyPilotDemand Pilot=default,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] bool PilotOnly=false);

// A capability is deliberately not serializable. Restoring the same logical
// session creates a different object, so an old UI owner cannot submit to it.
internal sealed class AssemblyControlAuthority
{
    internal AssemblyControlIdentity Identity { get; }
    internal AssemblyControlAuthority(AssemblyControlIdentity identity) => Identity = identity;
}

internal readonly record struct AssemblyControlAdmission(AssemblyControlIdentity Identity,
    long Sequence, int Frontier, long HostSequence, AssemblyControlRequest Requested,
    AssemblyControlRequest Effective);

internal enum AssemblyControlStatus
{
    Ready, Admitted, Duplicate, InvalidAuthority, InvalidIdentity, InvalidSequence,
    InvalidInput, OutstandingProposal, Capacity, Terminal, Retired, StaleSource,
    WrongOwnerThread, Reentrant
}

internal readonly record struct AssemblyControlResult(AssemblyControlStatus Status,
    AssemblyControlAdmission Admission = default);
internal readonly record struct AssemblyControlObservation(AssemblyControlIdentity Identity,
    AssemblyControlRequest Requested, int AdmissionCount, int Capacity, int Frontier, bool Retired);
internal sealed record AssemblyControlSave(long Generation, int Capacity, AssemblyControlAdmission[] Admissions, string Digest,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] AssemblyControlExecution Execution=AssemblyControlExecution.DemandOnly);
