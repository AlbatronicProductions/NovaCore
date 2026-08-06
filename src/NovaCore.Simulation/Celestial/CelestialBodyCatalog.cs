namespace NovaCore.Simulation.Celestial;

/// <summary>Permanent authored classification. It describes what a body is, never how it moves.</summary>
internal enum CelestialBodyClassification : byte
{
    Star = 0,
    Planet,
    DwarfPlanet,
    Moon,
    Asteroid,
    Comet,
    Barycenter,
    Artificial,
    Other,
}

/// <summary>Stable optional reference into a future authored metadata catalog. Zero means absent.</summary>
internal readonly record struct CelestialMetadataReferenceId(ulong Value)
{
    public bool IsPresent => Value != 0;
}

/// <summary>Immutable optional canonical aliases for one body. Construction copies caller storage.</summary>
internal sealed class CelestialBodyAliases
{
    private readonly string[] _values;
    internal static CelestialBodyAliases Empty { get; } = new([]);
    internal CelestialBodyAliases(ReadOnlySpan<string> values) { _values = values.ToArray(); }
    internal int Count => _values.Length;
    internal string Get(int index) => _values[index];
}

/// <summary>Reserved stable IDs for the initial Solar-System namespace. Other systems may use their own non-overlapping authored IDs.</summary>
internal static class SolarSystemBodyIds
{
    internal static readonly CelestialBodyId SolarSystemBarycenter = new(1);
    internal static readonly CelestialBodyId Sun = new(2);
    internal static readonly CelestialBodyId Mercury = new(3);
    internal static readonly CelestialBodyId Venus = new(4);
    internal static readonly CelestialBodyId EarthMoonBarycenter = new(5);
    internal static readonly CelestialBodyId Earth = new(6);
    internal static readonly CelestialBodyId Moon = new(7);
    internal static readonly CelestialBodyId Mars = new(8);
    internal static readonly CelestialBodyId Jupiter = new(9);
    internal static readonly CelestialBodyId Saturn = new(10);
    internal static readonly CelestialBodyId Uranus = new(11);
    internal static readonly CelestialBodyId Neptune = new(12);
}

/// <summary>Immutable body identity and hierarchy declaration. It deliberately has no trajectory data.</summary>
internal readonly record struct CelestialBodyIdentity(
    CelestialBodyId Id,
    string DisplayName,
    CelestialBodyClassification Classification,
    CelestialBodyId? ParentBody,
    CelestialMetadataReferenceId RotationReference,
    CelestialMetadataReferenceId AtmosphereReference,
    CelestialMetadataReferenceId VisualReference,
    CelestialBodyAliases? Aliases = null);

/// <summary>Immutable physical constants. Zero-valued radii and gravitational parameter mean unspecified.</summary>
internal readonly record struct CelestialPhysicalProperties(
    double GravitationalParameter,
    double MeanRadius,
    double EquatorialRadius,
    double PolarRadius,
    double Flattening,
    CelestialMetadataReferenceId SiderealRotationReference,
    CelestialMetadataReferenceId AtmosphereReference,
    CelestialMetadataReferenceId VisualReference)
{
    internal static CelestialPhysicalProperties Unspecified => default;
    internal bool IsValid =>
        double.IsFinite(GravitationalParameter) && GravitationalParameter >= 0d &&
        double.IsFinite(MeanRadius) && MeanRadius >= 0d &&
        double.IsFinite(EquatorialRadius) && EquatorialRadius >= 0d &&
        double.IsFinite(PolarRadius) && PolarRadius >= 0d &&
        double.IsFinite(Flattening) && Flattening >= 0d &&
        (EquatorialRadius == 0d || PolarRadius == 0d || PolarRadius <= EquatorialRadius);
}

/// <summary>One immutable catalog entry, combining permanent identity with physical constants only.</summary>
internal readonly record struct CelestialBodyCatalogEntry(CelestialBodyIdentity Identity, CelestialPhysicalProperties PhysicalProperties)
{
    public CelestialBodyId Id => Identity.Id;
}

/// <summary>Immutable, sorted-lookup catalog owned by exactly one celestial-system definition.</summary>
internal sealed class CelestialBodyCatalog
{
    private readonly CelestialBodyCatalogEntry[] _entries;
    private readonly ulong[] _lookupIds;
    private readonly int[] _lookupIndices;

    private CelestialBodyCatalog(CelestialBodyCatalogEntry[] entries, ulong[] lookupIds, int[] lookupIndices)
    { _entries = entries; _lookupIds = lookupIds; _lookupIndices = lookupIndices; }

    public int Count => _entries.Length;
    internal CelestialBodyCatalogEntry GetEntry(int index) => _entries[index];

    internal bool TryGet(CelestialBodyId id, out CelestialBodyCatalogEntry entry)
    {
        var index = Array.BinarySearch(_lookupIds, id.Value);
        if (index >= 0) { entry = _entries[_lookupIndices[index]]; return true; }
        entry = default; return false;
    }

    internal bool TryGetPhysicalProperties(CelestialBodyId id, out CelestialPhysicalProperties properties)
    {
        if (TryGet(id, out var entry)) { properties = entry.PhysicalProperties; return true; }
        properties = default; return false;
    }

    internal static bool TryCreate(ReadOnlySpan<CelestialBodyCatalogEntry> entries, out CelestialBodyCatalog? catalog, out CelestialSystemValidationResult validation)
    {
        catalog = null;
        if (entries.Length == 0) { validation = new(CelestialSystemValidationStatus.EmptyBodyCatalog); return false; }
        var names = new string[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            var identity = entries[index].Identity;
            if (!identity.Id.IsValid) { validation = new(CelestialSystemValidationStatus.InvalidBodyId); return false; }
            if (!Enum.IsDefined(identity.Classification)) { validation = new(CelestialSystemValidationStatus.InvalidBodyClassification); return false; }
            if (string.IsNullOrWhiteSpace(identity.DisplayName)) { validation = new(CelestialSystemValidationStatus.InvalidBodyDisplayName); return false; }
            if (identity.ParentBody == identity.Id) { validation = new(CelestialSystemValidationStatus.SelfParent); return false; }
            if (!entries[index].PhysicalProperties.IsValid) { validation = new(CelestialSystemValidationStatus.InvalidPhysicalProperties); return false; }
            for (var prior = 0; prior < index; prior++)
            {
                if (entries[prior].Id == identity.Id) { validation = new(CelestialSystemValidationStatus.DuplicateBodyId); return false; }
                if (string.Equals(names[prior], identity.DisplayName, StringComparison.Ordinal)) { validation = new(CelestialSystemValidationStatus.DuplicateBodyDisplayName); return false; }
            }
            var aliases = identity.Aliases;
            if (aliases is not null) for (var alias = 0; alias < aliases.Count; alias++)
            {
                if (string.IsNullOrWhiteSpace(aliases.Get(alias)) || string.Equals(aliases.Get(alias), identity.DisplayName, StringComparison.Ordinal)) { validation = new(CelestialSystemValidationStatus.InvalidBodyAlias); return false; }
                for (var prior = 0; prior < alias; prior++) if (string.Equals(aliases.Get(prior), aliases.Get(alias), StringComparison.Ordinal)) { validation = new(CelestialSystemValidationStatus.DuplicateBodyAlias); return false; }
            }
            names[index] = identity.DisplayName;
        }
        for (var index = 0; index < entries.Length; index++)
            if (entries[index].Identity.ParentBody is { } parent && !Contains(entries, parent)) { validation = new(CelestialSystemValidationStatus.MissingCatalogParent); return false; }
        var copy = entries.ToArray(); var ids = new ulong[copy.Length]; var indices = new int[copy.Length];
        for (var index = 0; index < copy.Length; index++) { ids[index] = copy[index].Id.Value; indices[index] = index; }
        Array.Sort(ids, indices);
        catalog = new(copy, ids, indices); validation = new(CelestialSystemValidationStatus.Success); return true;
    }

    private static bool Contains(ReadOnlySpan<CelestialBodyCatalogEntry> entries, CelestialBodyId id)
    { for (var index = 0; index < entries.Length; index++) if (entries[index].Id == id) return true; return false; }
}
