using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>GPU transport encoding only; never authoritative simulation state.</summary>
public readonly record struct EncodedPosition(float HighX, float HighY, float HighZ, float LowX, float LowY, float LowZ)
{
    public static EncodedPosition Encode(in Double3 value)
    {
        var highX = (float)value.X; var highY = (float)value.Y; var highZ = (float)value.Z;
        return new(highX, highY, highZ, (float)(value.X - highX), (float)(value.Y - highY), (float)(value.Z - highZ));
    }

    public Double3 Reconstruct() => new((double)HighX + LowX, (double)HighY + LowY, (double)HighZ + LowZ);
    public static RelativePosition Resolve(in EncodedPosition objectPosition, in EncodedPosition cameraPosition) =>
        new(new Double3((objectPosition.HighX - cameraPosition.HighX) + (objectPosition.LowX - cameraPosition.LowX),
                        (objectPosition.HighY - cameraPosition.HighY) + (objectPosition.LowY - cameraPosition.LowY),
                        (objectPosition.HighZ - cameraPosition.HighZ) + (objectPosition.LowZ - cameraPosition.LowZ)));
}
