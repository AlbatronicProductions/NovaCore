using System.Buffers.Binary;
using System.Security.Cryptography;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum ContactFeatureRole : byte { LandingTip = 1, SupportHardpoint, LowerBodyContactPoint }

/// <summary>A zero-radius authored physical location in spacecraft body axes, relative to COM.</summary>
internal readonly record struct SpacecraftContactFeature(ulong Id, Double3 OffsetFromComMetres, ContactFeatureRole Role);

internal readonly record struct ContactGeometryIdentity(ulong DefinitionId, uint Version, string ContentSha256);

/// <summary>Admission allocates and copies. Evaluation exposes no mutable geometry storage.</summary>
internal sealed class SpacecraftContactGeometry
{
    private readonly SpacecraftContactFeature[] _features;
    private SpacecraftContactGeometry(SpacecraftId spacecraft, ContactGeometryIdentity identity, SpacecraftContactFeature[] features)
    { Spacecraft = spacecraft; Identity = identity; _features = features; }
    internal SpacecraftId Spacecraft { get; }
    internal ContactGeometryIdentity Identity { get; }
    internal int Count => _features.Length;
    internal SpacecraftContactFeature GetFeature(int index) => _features[index];

    internal static bool TryCreate(SpacecraftId spacecraft, ulong definitionId, uint version,
        ReadOnlySpan<SpacecraftContactFeature> features, out SpacecraftContactGeometry? geometry)
    {
        geometry = null;
        if (spacecraft.Value == 0 || definitionId == 0 || version == 0 || features.IsEmpty) return false;
        foreach (var feature in features)
            if (feature.Id == 0 || !feature.OffsetFromComMetres.IsFinite ||
                feature.Role is < ContactFeatureRole.LandingTip or > ContactFeatureRole.LowerBodyContactPoint) return false;
        var owned = features.ToArray();
        Array.Sort(owned, static (a, b) => a.Id.CompareTo(b.Id));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> encoded = stackalloc byte[33];
        for (var i = 0; i < owned.Length; i++)
        {
            if (i != 0 && owned[i - 1].Id == owned[i].Id) return false;
            var p = owned[i].OffsetFromComMetres;
            p = new(p.X == 0 ? 0 : p.X, p.Y == 0 ? 0 : p.Y, p.Z == 0 ? 0 : p.Z);
            owned[i] = owned[i] with { OffsetFromComMetres = p };
            BinaryPrimitives.WriteUInt64LittleEndian(encoded, owned[i].Id);
            BinaryPrimitives.WriteDoubleLittleEndian(encoded[8..], p.X);
            BinaryPrimitives.WriteDoubleLittleEndian(encoded[16..], p.Y);
            BinaryPrimitives.WriteDoubleLittleEndian(encoded[24..], p.Z);
            encoded[32] = (byte)owned[i].Role;
            hash.AppendData(encoded);
        }
        geometry = new(spacecraft, new(definitionId, version, Convert.ToHexString(hash.GetHashAndReset())), owned);
        return true;
    }
}
