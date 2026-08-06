namespace NovaCore.EphemerisFormat;

/// <summary>Frozen v2 algorithm deliberately mirrors the existing runtime definition hash.</summary>
public static class NcpeV2RuntimeHash
{
    public static ulong Compute(NcpeV2Definition d)
    {
        ulong h=14695981039346656037UL; void M(ulong x){for(var i=0;i<8;i++){h^=(byte)x;h*=1099511628211UL;x>>=8;}} void S(string x){M((ulong)x.Length);foreach(var c in x)M(c);} void D(double x)=>M(unchecked((ulong)BitConverter.DoubleToInt64Bits(x))); var z=d.System; var root=d.Bodies.Single(x=>x.ParentId==0).Id;
        M(z.SystemId);M(root);M((ulong)d.Bindings.Count);foreach(var x in new[]{unchecked((ulong)z.SimulationAnchor),z.DomainId,unchecked((ulong)z.DomainAnchor),unchecked((ulong)z.DomainTicksPerSecond),unchecked((ulong)z.ScaleNumerator),unchecked((ulong)z.ScaleDenominator),z.SourceId,z.SourceVersion,z.DomainId,unchecked((ulong)z.CoverageStart),unchecked((ulong)z.CoverageEnd),z.FrameId,z.ConstantsVersion,z.ContentHashHigh,z.ContentHashLow,z.AuthoredHashHigh,z.AuthoredHashLow})M(x);
        M((ulong)d.Bodies.Count);foreach(var b in d.Bodies){M(b.Id);S(b.Name);M(b.Classification);M(b.ParentId);M(b.RotationReference);M(b.AtmosphereReference);M(b.VisualReference);M((ulong)b.Aliases.Count);foreach(var a in b.Aliases)S(a);D(b.Mu);D(b.MeanRadius);D(b.EquatorialRadius);D(b.PolarRadius);D(b.Flattening);M(b.SiderealReference);M(b.PhysicalAtmosphereReference);M(b.PhysicalVisualReference);M(b.PhysicalSource);M(b.PhysicalConstantsVersion);}
        M((ulong)d.Sources.Count);foreach(var s in d.Sources){M(s.Id);M(s.Model);foreach(var x in new[]{s.Id,s.Version,s.DomainId,unchecked((ulong)s.CoverageStart),unchecked((ulong)s.CoverageEnd),s.FrameId,s.ConstantsVersion,s.ContentHashHigh,s.ContentHashLow,s.AuthoredHashHigh,s.AuthoredHashLow})M(x);}
        var fixeds=d.Bindings.Where(x=>x.Model==1).ToArray();M((ulong)fixeds.Length);foreach(var b in fixeds){D(b.FixedPositionX);D(b.FixedPositionY);D(b.FixedPositionZ);D(b.FixedVelocityX);D(b.FixedVelocityY);D(b.FixedVelocityZ);D(b.FixedQuaternionX);D(b.FixedQuaternionY);D(b.FixedQuaternionZ);D(b.FixedQuaternionW);D(b.FixedAngularVelocityX);D(b.FixedAngularVelocityY);D(b.FixedAngularVelocityZ);}M(0);M(0);
        M((ulong)d.Payloads.Count);foreach(var p in d.Payloads){M(p.DomainId);M(unchecked((ulong)p.FirstSampleIndex));M(unchecked((ulong)p.SampleCount));M(p.Interpolation);M(unchecked((ulong)p.CoverageStart));M(unchecked((ulong)p.CoverageEnd));}M((ulong)d.Samples.Count);foreach(var s in d.Samples){M(unchecked((ulong)s.DomainTick));D(s.PositionX);D(s.PositionY);D(s.PositionZ);D(s.VelocityX);D(s.VelocityY);D(s.VelocityZ);}foreach(var b in d.Bindings.OrderBy(x=>x.BodyId)){M(b.BodyId);M(b.Model);M(b.SourceId);M(unchecked((ulong)b.PayloadIndex));}return h;
    }
}
