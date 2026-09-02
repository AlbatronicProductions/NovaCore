using System.Runtime.InteropServices;
using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Immutable identity and manifest for one explicitly selected P2C1 candidate physical-field generation.</summary>
public readonly record struct PlanetaryNaturalTerrainPhysicalFieldGeneration(
    ulong BodyId,
    ulong GenerationId,
    uint HashVersion,
    uint CompositionVersion,
    uint Seed,
    ulong FamilyManifestHash,
    PlanetaryNaturalTerrainFamilyBounds Bounds)
{
    public bool IsValid => BodyId != 0 && GenerationId != 0 && HashVersion != 0 && CompositionVersion != 0 &&
        FamilyManifestHash != 0 && Bounds.IsFinite;

    public static PlanetaryNaturalTerrainPhysicalFieldGeneration EarthProof(uint seed = 0x4D12D2B1u) =>
        new(PlanetaryPhysicalSurface.EarthBodyId, PlanetaryNaturalTerrainFamilies.ProofGeneration,
            PlanetaryNaturalTerrainField.HashVersion, PlanetaryNaturalTerrainFamilies.CompositionVersion,
            seed, PlanetaryNaturalTerrainFamilies.ComputeManifestHash(seed),
            PlanetaryNaturalTerrainFamilies.ComposedBounds());
}

/// <summary>Measured logical descriptor ABI; not yet a production native contract.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PlanetaryNaturalTerrainPhysicalCellDescriptor
{
    public long CellX, CellY, CellZ;
    public uint ControlHashX, ControlHashY, FirstFamily, SecondFamily;
    public uint Seed, DescriptorGeneration;
    public double SecondWeight;
    public double WeightGradientX, WeightGradientY, WeightGradientZ;
    public double OrientationX, OrientationY, OrientationZ;
    public double HeightBound, GradientBound;
}

/// <summary>Measured logical prepared-vertex ABI; the P2C1 shader uses the existing proof transport ABI.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PlanetaryNaturalTerrainPreparedPatchVertex
{
    public double MacroHeight, MesoHeight, PreparedHeight;
    public double GradientX, GradientY, GradientZ;
    public double SecondWeight;
    public double OrientationX, OrientationY, OrientationZ;
    public double HeightBound, GradientBound;
    public uint FirstFamily, SecondFamily, DescriptorGeneration, CompositionVersion;
    public ulong PhysicalGeneration;
}

public readonly record struct PlanetaryNaturalTerrainPreparedPatchKey(
    PlanetarySurfacePatchId Patch,
    ulong PhysicalFieldGeneration,
    uint RepresentabilityContext)
{
    public bool IsValid => Patch.IsValid && PhysicalFieldGeneration != 0 && RepresentabilityContext != 0;
}

public readonly record struct PlanetaryNaturalTerrainPreparationRequest(
    PlanetaryNaturalTerrainPreparedPatchKey Key,
    int VertexCount)
{
    public bool IsValid => Key.IsValid && VertexCount is > 0 and <= 4096;
}

public readonly record struct PlanetaryNaturalTerrainPreparationWork(
    int Slot,
    uint SlotGeneration,
    uint DescriptorGeneration,
    PlanetaryNaturalTerrainPreparationRequest Request);

public enum PlanetaryNaturalTerrainPreparedSlotState : byte
{
    Empty,
    Preparing,
    Ready,
    Authoritative,
    Cached
}

public sealed class PlanetaryNaturalTerrainPreparedPatchGeneration
{
    internal PlanetaryNaturalTerrainPreparedPatchGeneration(uint transactionGeneration,
        in PlanetaryNaturalTerrainPhysicalFieldGeneration physicalField,
        PlanetaryNaturalTerrainPreparationRequest[] requests,
        PlanetaryNaturalTerrainPreparationWork[] changed,
        int reused)
    {
        TransactionGeneration = transactionGeneration; PhysicalField = physicalField;
        Requests = Array.AsReadOnly(requests); ChangedSlots = Array.AsReadOnly(changed); ReusedSlots = reused;
    }

    public uint TransactionGeneration { get; }
    public PlanetaryNaturalTerrainPhysicalFieldGeneration PhysicalField { get; }
    public IReadOnlyList<PlanetaryNaturalTerrainPreparationRequest> Requests { get; }
    public IReadOnlyList<PlanetaryNaturalTerrainPreparationWork> ChangedSlots { get; }
    public int ReusedSlots { get; }
    public bool Published { get; internal set; }
    public double ReusePercentage => Requests.Count == 0 ? 100d : ReusedSlots * 100d / Requests.Count;
}

public readonly record struct PlanetaryNaturalTerrainPreparationCacheMetrics(
    int Capacity,
    int Resident,
    int Preparing,
    int Ready,
    int Authoritative,
    int Cached,
    long ChangedSlotsPrepared,
    long ReusedSlots,
    long VerticesPrepared,
    long DescriptorCount,
    long BytesWritten,
    uint AuthoritativeTransactionGeneration,
    ulong AuthoritativePhysicalGeneration);

/// <summary>
/// Isolated P2C1 transactional cache. Scheduling, GPU completion, and publication
/// are separate operations so an incomplete replacement cannot suppress the
/// previously authoritative complete generation.
/// </summary>
public sealed class PlanetaryNaturalTerrainPreparationCache
{
    public const int DefaultCapacity = 2048;
    public const int WorstExpectedCapacity = 4096;
    public const int VerticesPerPatch = PlanetaryDynamicAnchoredSurface.GpuBaseVerticesPerPatch;

    private sealed class Entry
    {
        internal PlanetaryNaturalTerrainPreparedPatchKey Key;
        internal PlanetaryNaturalTerrainPreparedSlotState State;
        internal uint SlotGeneration;
        internal uint DescriptorGeneration;
        internal uint TransactionGeneration;
        internal long LastUse;
    }

    private readonly Entry[] _entries;
    private readonly Dictionary<PlanetaryNaturalTerrainPreparedPatchKey, int> _slots;
    private readonly HashSet<PlanetaryNaturalTerrainPreparedPatchKey> _authoritative;
    private long _serial;
    private uint _nextTransaction;
    private long _changedSlotsPrepared, _reusedSlots, _verticesPrepared, _descriptorCount, _bytesWritten;
    private uint _authoritativeTransaction;
    private uint _activePreparationTransaction;
    private ulong _authoritativePhysicalGeneration;

    public PlanetaryNaturalTerrainPreparationCache(int capacity = DefaultCapacity)
    {
        if (capacity is < 2 or > WorstExpectedCapacity) throw new ArgumentOutOfRangeException(nameof(capacity));
        _entries = new Entry[capacity]; _slots = new(capacity); _authoritative = new(capacity);
        for (var index = 0; index < capacity; index++) _entries[index] = new();
    }

    public bool TryBeginGeneration(in PlanetaryNaturalTerrainPhysicalFieldGeneration physicalField,
        IReadOnlyList<PlanetaryNaturalTerrainPreparationRequest> requests,
        out PlanetaryNaturalTerrainPreparedPatchGeneration? generation)
    {
        generation = null;
        if (!physicalField.IsValid || requests.Count == 0) return false;
        var unique = new HashSet<PlanetaryNaturalTerrainPreparedPatchKey>();
        var requestCopy = new PlanetaryNaturalTerrainPreparationRequest[requests.Count];
        var missing = new List<PlanetaryNaturalTerrainPreparationRequest>();
        var reused = 0;
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            if (!request.IsValid || request.Key.Patch.BodyId != physicalField.BodyId ||
                request.Key.PhysicalFieldGeneration != physicalField.GenerationId || !unique.Add(request.Key)) return false;
            requestCopy[index] = request;
            if (_slots.TryGetValue(request.Key, out var slot))
            {
                if (_entries[slot].State is PlanetaryNaturalTerrainPreparedSlotState.Ready or
                    PlanetaryNaturalTerrainPreparedSlotState.Authoritative or PlanetaryNaturalTerrainPreparedSlotState.Cached)
                { reused++; _entries[slot].LastUse = ++_serial; }
                else if (_entries[slot].State == PlanetaryNaturalTerrainPreparedSlotState.Preparing)
                    return false;
                else missing.Add(request);
            }
            else missing.Add(request);
        }
        if (missing.Count > 0 && _activePreparationTransaction != 0) return false;

        var candidates = new List<int>();
        for (var index = 0; index < _entries.Length && candidates.Count < missing.Count; index++)
        {
            var entry = _entries[index];
            if (entry.State == PlanetaryNaturalTerrainPreparedSlotState.Empty) candidates.Add(index);
        }
        if (candidates.Count < missing.Count)
        {
            foreach (var candidate in Enumerable.Range(0, _entries.Length)
                .Where(index => _entries[index].State == PlanetaryNaturalTerrainPreparedSlotState.Cached &&
                    !_authoritative.Contains(_entries[index].Key))
                .OrderBy(index => _entries[index].LastUse))
            {
                if (!candidates.Contains(candidate)) candidates.Add(candidate);
                if (candidates.Count == missing.Count) break;
            }
        }
        if (candidates.Count < missing.Count) return false;

        if (_nextTransaction == uint.MaxValue) return false;
        var transaction = ++_nextTransaction;
        if (missing.Count > 0) _activePreparationTransaction = transaction;
        var changed = new PlanetaryNaturalTerrainPreparationWork[missing.Count];
        for (var index = 0; index < missing.Count; index++)
        {
            var slot = candidates[index]; var entry = _entries[slot];
            if (entry.State != PlanetaryNaturalTerrainPreparedSlotState.Empty) _slots.Remove(entry.Key);
            entry.Key = missing[index].Key; entry.State = PlanetaryNaturalTerrainPreparedSlotState.Preparing;
            entry.SlotGeneration = entry.SlotGeneration == uint.MaxValue ? 1u : entry.SlotGeneration + 1u;
            entry.DescriptorGeneration = entry.DescriptorGeneration == uint.MaxValue ? 1u : entry.DescriptorGeneration + 1u;
            entry.TransactionGeneration = transaction; entry.LastUse = ++_serial; _slots[entry.Key] = slot;
            changed[index] = new(slot, entry.SlotGeneration, entry.DescriptorGeneration, missing[index]);
        }
        _reusedSlots += reused;
        generation = new(transaction, physicalField, requestCopy, changed, reused);
        return true;
    }

    public bool CompleteChangedSlot(PlanetaryNaturalTerrainPreparedPatchGeneration generation,
        in PlanetaryNaturalTerrainPreparationWork work, int descriptorsPrepared,
        int verticesPrepared, long bytesWritten)
    {
        if (generation.Published || _activePreparationTransaction != generation.TransactionGeneration ||
            work.Slot < 0 || work.Slot >= _entries.Length ||
            descriptorsPrepared <= 0 || verticesPrepared != work.Request.VertexCount || bytesWritten <= 0) return false;
        var entry = _entries[work.Slot];
        if (entry.State != PlanetaryNaturalTerrainPreparedSlotState.Preparing || entry.Key != work.Request.Key ||
            entry.SlotGeneration != work.SlotGeneration || entry.DescriptorGeneration != work.DescriptorGeneration ||
            entry.TransactionGeneration != generation.TransactionGeneration) return false;
        entry.State = PlanetaryNaturalTerrainPreparedSlotState.Ready; entry.LastUse = ++_serial;
        _changedSlotsPrepared++; _descriptorCount += descriptorsPrepared; _verticesPrepared += verticesPrepared; _bytesWritten += bytesWritten;
        return true;
    }

    public bool TryPublish(PlanetaryNaturalTerrainPreparedPatchGeneration generation)
    {
        if (generation.Published || generation.TransactionGeneration <= _authoritativeTransaction ||
            (generation.ChangedSlots.Count > 0 && _activePreparationTransaction != generation.TransactionGeneration)) return false;
        foreach (var request in generation.Requests)
            if (!_slots.TryGetValue(request.Key, out var slot) || _entries[slot].State is
                PlanetaryNaturalTerrainPreparedSlotState.Empty or PlanetaryNaturalTerrainPreparedSlotState.Preparing)
                return false;
        foreach (var key in _authoritative)
            if (_slots.TryGetValue(key, out var slot) && _entries[slot].State == PlanetaryNaturalTerrainPreparedSlotState.Authoritative)
                _entries[slot].State = PlanetaryNaturalTerrainPreparedSlotState.Cached;
        _authoritative.Clear();
        foreach (var request in generation.Requests)
        {
            var slot = _slots[request.Key]; _entries[slot].State = PlanetaryNaturalTerrainPreparedSlotState.Authoritative;
            _entries[slot].LastUse = ++_serial; _authoritative.Add(request.Key);
        }
        generation.Published = true; _authoritativeTransaction = generation.TransactionGeneration;
        if (_activePreparationTransaction == generation.TransactionGeneration) _activePreparationTransaction = 0;
        _authoritativePhysicalGeneration = generation.PhysicalField.GenerationId; return true;
    }

    public bool IsAuthoritative(in PlanetaryNaturalTerrainPreparedPatchKey key) => _authoritative.Contains(key);
    public bool IsPreparing(int slot) => slot >= 0 && slot < _entries.Length &&
        _entries[slot].State == PlanetaryNaturalTerrainPreparedSlotState.Preparing;

    public PlanetaryNaturalTerrainPreparationCacheMetrics Metrics
    {
        get
        {
            var resident = 0; var preparing = 0; var ready = 0; var authoritative = 0; var cached = 0;
            foreach (var entry in _entries)
            {
                if (entry.State != PlanetaryNaturalTerrainPreparedSlotState.Empty) resident++;
                preparing += entry.State == PlanetaryNaturalTerrainPreparedSlotState.Preparing ? 1 : 0;
                ready += entry.State == PlanetaryNaturalTerrainPreparedSlotState.Ready ? 1 : 0;
                authoritative += entry.State == PlanetaryNaturalTerrainPreparedSlotState.Authoritative ? 1 : 0;
                cached += entry.State == PlanetaryNaturalTerrainPreparedSlotState.Cached ? 1 : 0;
            }
            return new(_entries.Length, resident, preparing, ready, authoritative, cached,
                _changedSlotsPrepared, _reusedSlots, _verticesPrepared, _descriptorCount, _bytesWritten,
                _authoritativeTransaction, _authoritativePhysicalGeneration);
        }
    }
}
