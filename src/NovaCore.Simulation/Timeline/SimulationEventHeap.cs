namespace NovaCore.Simulation.Timeline;

/// <summary>Array-backed canonical min-heap with an EventId-to-index lookup. It never depends on map enumeration.</summary>
internal sealed class SimulationEventHeap
{
    private ScheduledSimulationEvent[] _items;
    private readonly Dictionary<SimulationEventId, int> _indices;
    private int _count;

    public SimulationEventHeap(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = capacity == 0 ? Array.Empty<ScheduledSimulationEvent>() : new ScheduledSimulationEvent[capacity];
        _indices = new Dictionary<SimulationEventId, int>(capacity);
    }

    public int Count => _count;
    public bool Contains(SimulationEventId id) => _indices.ContainsKey(id);
    public bool TryGet(SimulationEventId id, out ScheduledSimulationEvent value)
    {
        if (_indices.TryGetValue(id, out var index)) { value = _items[index]; return true; }
        value = default; return false;
    }

    public void Add(ScheduledSimulationEvent value)
    {
        EnsureCapacity(_count + 1);
        var index = _count++;
        _items[index] = value;
        _indices.Add(value.Header.Id, index);
        SiftUp(index);
    }

    public bool TryRemove(SimulationEventId id, out ScheduledSimulationEvent removed)
    {
        if (!_indices.TryGetValue(id, out var index)) { removed = default; return false; }
        removed = _items[index];
        _indices.Remove(id);
        var last = --_count;
        if (index != last)
        {
            _items[index] = _items[last];
            _indices[_items[index].Header.Id] = index;
            if (index > 0 && Compare(index, Parent(index)) < 0) SiftUp(index); else SiftDown(index);
        }
        _items[last] = default;
        return true;
    }

    public bool TryPeek(out ScheduledSimulationEvent value)
    {
        if (_count != 0) { value = _items[0]; return true; }
        value = default; return false;
    }

    public int CopyTo(Span<ScheduledSimulationEvent> destination)
    {
        if (destination.Length < _count) throw new ArgumentException("Destination is too small.", nameof(destination));
        _items.AsSpan(0, _count).CopyTo(destination);
        return _count;
    }

    public bool ValidateInvariants()
    {
        if (_indices.Count != _count) return false;
        for (var index = 0; index < _count; index++)
        {
            var id = _items[index].Header.Id;
            if (!_indices.TryGetValue(id, out var mappedIndex) || mappedIndex != index) return false;
            var left = Left(index); var right = left + 1;
            if (left < _count && Compare(index, left) > 0) return false;
            if (right < _count && Compare(index, right) > 0) return false;
        }
        return true;
    }

    private void EnsureCapacity(int required)
    {
        if (_items.Length >= required) return;
        var capacity = _items.Length == 0 ? 4 : checked(_items.Length * 2);
        if (capacity < required) capacity = required;
        Array.Resize(ref _items, capacity);
    }
    private void SiftUp(int index) { while (index > 0) { var parent = Parent(index); if (Compare(index, parent) >= 0) break; Swap(index, parent); index = parent; } }
    private void SiftDown(int index) { while (true) { var left = Left(index); if (left >= _count) return; var right = left + 1; var child = right < _count && Compare(right, left) < 0 ? right : left; if (Compare(index, child) <= 0) return; Swap(index, child); index = child; } }
    private void Swap(int left, int right) { (_items[left], _items[right]) = (_items[right], _items[left]); _indices[_items[left].Header.Id] = left; _indices[_items[right].Header.Id] = right; }
    private int Compare(int left, int right) => SimulationEventHeaderComparer.Compare(_items[left].Header, _items[right].Header);
    private static int Parent(int index) => (index - 1) / 2;
    private static int Left(int index) => (index * 2) + 1;
}
