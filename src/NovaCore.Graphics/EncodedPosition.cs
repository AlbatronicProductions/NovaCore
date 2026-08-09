using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>High/low GPU encoding only; callers decide whether the encoded value is root or camera relative.</summary>
public readonly record struct EncodedPosition(float HighX, float HighY, float HighZ, float LowX, float LowY, float LowZ)
{
    public static EncodedPosition Encode(in Double3 value)
    {
        var highX = (float)value.X; var highY = (float)value.Y; var highZ = (float)value.Z;
        return new(highX, highY, highZ, (float)(value.X - highX), (float)(value.Y - highY), (float)(value.Z - highZ));
    }

    public Double3 Reconstruct() => new((double)HighX + LowX, (double)HighY + LowY, (double)HighZ + LowZ);
}
