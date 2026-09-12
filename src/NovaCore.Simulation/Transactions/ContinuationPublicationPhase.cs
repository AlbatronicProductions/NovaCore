namespace NovaCore.Simulation.Transactions;

/// <summary>
/// The timeline's existing single-writer ownership, made explicit for continuation publication.
/// This is a thread/phase guard, not a scheduler, lock, transaction framework or rollback service.
/// Checked reads may execute during preflight; ordinary mutations and nested publication may not.
/// </summary>
internal sealed class ContinuationPublicationPhase
{
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private object? _publisher;

    internal bool IsOwnerThread => Environment.CurrentManagedThreadId == _thread;
    internal bool IsActive => _publisher is not null;

    internal void VerifyRead()
    {
        if (!IsOwnerThread) throw new InvalidOperationException("Simulation authority belongs to its owning thread.");
    }

    internal void VerifyOrdinaryMutation()
    {
        VerifyRead();
        if (_publisher is not null) throw new InvalidOperationException("Ordinary mutation cannot interleave continuation publication.");
    }

    internal bool TryEnter(object publisher)
    {
        if (!IsOwnerThread || _publisher is not null) return false;
        _publisher = publisher;
        return true;
    }

    internal bool IsOwnedBy(object publisher) => IsOwnerThread && ReferenceEquals(_publisher, publisher);

    // Only the publisher's finally clause releases the phase. No user callback runs here.
    internal void Exit() => _publisher = null;
}
