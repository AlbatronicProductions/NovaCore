using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // Read-only admission at an acknowledged frontier. Does not weaken the original source validator.
    internal LocalContactStatus PrepareService(SimulationTransactionEngine engine, LocalContactConfiguration config,
        Receipt receipt, out SimulationInstant target, out bool completed, out long inputSequence)
    {
        target = default; completed = false; inputSequence = 0;
        var status = Validate(engine, config, receipt, export.Motion.Time);
        if (status != LocalContactStatus.Success) return status;
        if (publication is not { } p) return LocalContactStatus.InvalidSource;
        if (p.Pending || frontier != p.AcknowledgedFrontier) return LocalContactStatus.PublicationPending;
        inputSequence = p.HostInputSequence;
        if (!source.TryEndpoint(frontier, out var current) || current != p.Clock.Time)
            return LocalContactStatus.FrontierMismatch;
        completed = frontier == long.MaxValue || !source.TryEndpoint(frontier + 1, out target);
        return LocalContactStatus.Success;
    }

    internal readonly struct HostCreditAcknowledgement
    {
        private readonly LocalContactWorld world;
        private readonly PublicationBinding binding;
        private readonly ContinuationClockState clock;
        private readonly long sequence;
        private HostCreditAcknowledgement(LocalContactWorld world, PublicationBinding binding,
            ContinuationClockState clock, long sequence)
        { this.world = world; this.binding = binding; this.clock = clock; this.sequence = sequence; }

        internal static bool TryPrepare(LocalContactWorld world, SimulationTransactionEngine engine,
            ContinuationClockState clock, long sequence, out HostCreditAcknowledgement result)
        {
            result = default;
            if (!engine.OwnsPersistentPublicationPhase || world.publication is not { } p ||
                !ReferenceEquals(p.Engine, engine) || p.Pending || world.disposed || world.invalidated ||
                p.HostInputSequence == long.MaxValue || sequence != p.HostInputSequence + 1) return false;
            result = new(world, p, clock, sequence); return true;
        }

        // No physical expected values or solver state change at the clock-accounting boundary.
        internal void Commit() { binding.Clock = clock; binding.HostInputSequence = sequence; }
        internal void Invalidate() => world.invalidated = true;
    }
}
