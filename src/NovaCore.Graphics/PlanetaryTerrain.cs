using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Immutable presentation-only definition for deterministic spherical displacement.</summary>
public readonly record struct PlanetaryTerrainDefinition(uint SourceId,uint Version,double MaximumHeightMetres)
{
    public const int GridResolution=16;
    public const int GridVertexCount=(GridResolution+1)*(GridResolution+1);
    public const int MaximumDetailOctaves=7;
    public static PlanetaryTerrainDefinition EarthProductionCubeV4=>new(2,4,EarthElevationDataset.MaximumElevationMetres);
    public bool IsValid=>SourceId!=0&&Version!=0&&double.IsFinite(MaximumHeightMetres)&&MaximumHeightMetres>0;

    public double SampleHeight(in Double3 bodyDirection,int patchLevel)
    {
        if(!IsValid||!bodyDirection.IsFinite||bodyDirection.LengthSquared<=0||patchLevel is <0 or >24)throw new ArgumentOutOfRangeException();
        if(SourceId==EarthProductionCubeV4.SourceId&&Version==EarthProductionCubeV4.Version)
            return Math.Max(0d,EarthElevationDataset.SampleHeight(bodyDirection)+EarthLocalTerrainElevationDataset.SampleResidual(bodyDirection));
        var direction=bodyDirection.Normalized();
        var continental=.46d*Math.Sin(Double3.Dot(direction,new(.8017837257372732,.2672612419124244,.5345224838248488))*3.1d+.7d)
            +.31d*Math.Sin(Double3.Dot(direction,new(-.4082482904638631,.8164965809277261,.4082482904638631))*5.3d-1.2d)
            +.23d*Math.Sin(Double3.Dot(direction,new(.1825741858350554,-.3651483716701107,.9128709291752769))*8.7d+.35d);
        var land=Math.Max(0d,continental-.02d);var height=land*land*5_200d;
        // Generic procedural terrain truth is level/topology independent. The
        // level parameter remains only for the reusable cache/query contract.
        for(var octave=0;octave<MaximumDetailOctaves;octave++)
        {
            var frequency=64d*(1<<(2*octave));var amplitude=900d*Math.Pow(.52d,octave);
            var waveA=Math.Sin(Double3.Dot(direction,new(.8728715609439696,.4364357804719848,-.2182178902359924))*frequency
                +Math.Sin(Double3.Dot(direction,new(-.1690308509457033,.50709255283711,.8451542547285166))*frequency*.73d)*.65d);
            var waveB=Math.Sin(Double3.Dot(direction,new(.3903600291794133,-.6506000486323555,.6506000486323555))*frequency*1.31d+octave*1.7d);
            var detail=.5d+.3d*waveA+.2d*waveB;height+=detail*amplitude*(.2d+.8d*land);
        }
        return Math.Clamp(height,0d,MaximumHeightMetres);
    }
}

/// <summary>Stable cache identity. Body/material identity is presentation-only and never enters celestial hashes.</summary>
public readonly record struct PlanetaryTerrainPatchKey(ulong BodyId,CubeSphereFace Face,int Level,int X,int Y,uint TerrainVersion,uint SourceId);

public readonly record struct PlanetaryTerrainCacheStatistics(long Hits,long Misses,long Generated,long Evictions,int ResidentCount,int Capacity,long ResidentBytes);

public sealed class PlanetaryTerrainTile
{
    private readonly float[] _heights;
    internal PlanetaryTerrainTile(PlanetaryTerrainPatchKey key,float[] heights){Key=key;_heights=heights;}
    public PlanetaryTerrainPatchKey Key { get; }
    public ReadOnlySpan<float> Heights=>_heights;
}

/// <summary>Bounded deterministic LRU used by CPU queries/tests; native Vulkan owns the equivalent GPU residency cache.</summary>
public sealed class PlanetaryTerrainResidencyCache
{
    private sealed class Entry { internal PlanetaryTerrainTile? Tile; internal long LastUse; }
    private readonly Entry[] _entries;private long _serial,_hits,_misses,_generated,_evictions;
    public PlanetaryTerrainResidencyCache(int capacity)
    {
        if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));_entries=Enumerable.Range(0,capacity).Select(_=>new Entry()).ToArray();
    }
    public PlanetaryTerrainTile Acquire(in PlanetaryTerrainPatchKey key,in PlanetaryTerrainDefinition definition)
    {
        if(!definition.IsValid||key.TerrainVersion!=definition.Version||key.SourceId!=definition.SourceId||key.Level is <0 or >24)throw new ArgumentOutOfRangeException();
        var use=checked(++_serial);for(var index=0;index<_entries.Length;index++)if(_entries[index].Tile?.Key==key){_entries[index].LastUse=use;_hits++;return _entries[index].Tile!;}
        _misses++;var selected=-1;var oldest=long.MaxValue;for(var index=0;index<_entries.Length;index++){if(_entries[index].Tile is null){selected=index;break;}if(_entries[index].LastUse<oldest){oldest=_entries[index].LastUse;selected=index;}}
        if(_entries[selected].Tile is not null)_evictions++;var patch=new PlanetaryPatch(key.Face,key.Level,key.X,key.Y);var values=new float[PlanetaryTerrainDefinition.GridVertexCount];var cursor=0;
        for(var y=0;y<=PlanetaryTerrainDefinition.GridResolution;y++)for(var x=0;x<=PlanetaryTerrainDefinition.GridResolution;x++)
        {
            var (u,v)=patch.GridCoordinate(x,y);
            values[cursor++]=(float)definition.SampleHeight(CubeSphereProjection.Project(key.Face,u,v,1d),24);
        }
        var tile=new PlanetaryTerrainTile(key,values);_entries[selected].Tile=tile;_entries[selected].LastUse=use;_generated++;return tile;
    }
    public PlanetaryTerrainCacheStatistics Statistics
    {
        get{var resident=_entries.Count(entry=>entry.Tile is not null);return new(_hits,_misses,_generated,_evictions,resident,_entries.Length,(long)resident*PlanetaryTerrainDefinition.GridVertexCount*sizeof(float));}
    }
}

/// <summary>Presentation-only body-fixed radial and local east/north/up frame.</summary>
public readonly record struct PlanetarySurfaceFrame(Double3 Direction,Double3 East,Double3 North,Double3 Up)
{
    public static PlanetarySurfaceFrame AtDirection(in Double3 bodyDirection)
    {
        if(!bodyDirection.IsFinite||bodyDirection.LengthSquared<=0d)throw new ArgumentOutOfRangeException(nameof(bodyDirection));
        var up=bodyDirection.Normalized();
        var eastCandidate=Double3.Cross(Double3.UnitY,up);
        // Geographic east is defined from the spin axis. At the exact poles the
        // tangent is mathematically non-unique, so use one deterministic axis.
        var east=eastCandidate.LengthSquared>1e-24d?eastCandidate.Normalized():Double3.Cross(Double3.UnitZ,up).Normalized();
        var north=Double3.Cross(up,east).Normalized();return new(up,east,north,up);
    }
    public DoubleQuaternion HorizonViewOrientation(double downwardRadians=Math.PI/12d)
    {
        if(!double.IsFinite(downwardRadians)||downwardRadians<0||downwardRadians>=Math.PI*.5d)throw new ArgumentOutOfRangeException(nameof(downwardRadians));
        return LookOrientation(0d,-downwardRadians);
    }
    public DoubleQuaternion LookOrientation(double yawRadians,double pitchRadians)
    {
        if(!double.IsFinite(yawRadians)||!double.IsFinite(pitchRadians)||pitchRadians<=-Math.PI*.5d||pitchRadians>=Math.PI*.5d)throw new ArgumentOutOfRangeException();
        var horizontal=(North*Math.Cos(yawRadians)+East*Math.Sin(yawRadians)).Normalized();var forward=(horizontal*Math.Cos(pitchRadians)+Up*Math.Sin(pitchRadians)).Normalized();var right=Double3.Cross(forward,Up).Normalized();var cameraUp=Double3.Cross(right,forward).Normalized();return QuaternionFromBasis(right,cameraUp,-forward);
    }
    private static DoubleQuaternion QuaternionFromBasis(in Double3 x,in Double3 y,in Double3 z)
    {
        var trace=x.X+y.Y+z.Z;double qx,qy,qz,qw;
        if(trace>0){var s=Math.Sqrt(trace+1d)*2d;qw=.25d*s;qx=(y.Z-z.Y)/s;qy=(z.X-x.Z)/s;qz=(x.Y-y.X)/s;}
        else if(x.X>y.Y&&x.X>z.Z){var s=Math.Sqrt(1d+x.X-y.Y-z.Z)*2d;qw=(y.Z-z.Y)/s;qx=.25d*s;qy=(y.X+x.Y)/s;qz=(z.X+x.Z)/s;}
        else if(y.Y>z.Z){var s=Math.Sqrt(1d+y.Y-x.X-z.Z)*2d;qw=(z.X-x.Z)/s;qx=(y.X+x.Y)/s;qy=.25d*s;qz=(z.Y+y.Z)/s;}
        else{var s=Math.Sqrt(1d+z.Z-x.X-y.Y)*2d;qw=(x.Y-y.X)/s;qx=(z.X+x.Z)/s;qy=(z.Y+y.Z)/s;qz=.25d*s;}
        return new DoubleQuaternion(qx,qy,qz,qw).Normalized();
    }
}

public static class PlanetaryTerrainQuery
{
    public static double SurfaceRadius(double physicalRadius,in Double3 bodyDirection,in PlanetaryTerrainDefinition terrain)
    {
        if(!double.IsFinite(physicalRadius)||physicalRadius<=0)throw new ArgumentOutOfRangeException(nameof(physicalRadius));return physicalRadius+terrain.SampleHeight(bodyDirection,24);
    }
    public static double VisibleSurfaceRadius(double physicalRadius,in Double3 bodyDirection,in PlanetaryTerrainDefinition terrain,double seaLevelMetres)
    {
        if(!double.IsFinite(physicalRadius)||physicalRadius<=0)throw new ArgumentOutOfRangeException(nameof(physicalRadius));
        if(!double.IsFinite(seaLevelMetres))throw new ArgumentOutOfRangeException(nameof(seaLevelMetres));
        return physicalRadius+Math.Max(terrain.SampleHeight(bodyDirection,24),seaLevelMetres);
    }
}
