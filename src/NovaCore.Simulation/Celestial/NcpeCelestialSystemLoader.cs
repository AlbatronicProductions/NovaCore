using NovaCore.Core;
using NovaCore.EphemerisFormat;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

internal enum NcpeCelestialSystemLoadStatus : byte { Success, UnsupportedV1Reconstruction, FormatFailure, RuntimeValidationFailure, DefinitionHashMismatch }
internal readonly record struct NcpeCelestialSystemLoadIdentity(CelestialSystemId SystemId, ulong DefinitionHash, ulong ArtifactHash, CelestialEphemerisSourceId Source, CelestialEphemerisVersionId Version, ulong ConverterId, ulong ConverterVersion, CelestialContentHash ContentHash, ulong PolicyHash, CelestialContentHash AuthoredHash, long CoverageStart, long CoverageEnd);
internal readonly record struct NcpeCelestialSystemLoadResult(NcpeCelestialSystemLoadStatus Status, NcpeCelestialSystemLoadIdentity Identity) { internal bool Succeeded => Status == NcpeCelestialSystemLoadStatus.Success; }

/// <summary>Pure byte-to-immutable-contract adapter. It retains no caller artifact buffer.</summary>
internal static class NcpeCelestialSystemLoader
{
    internal static NcpeCelestialSystemLoadResult TryRead(ReadOnlySpan<byte> bytes, out CelestialSystemDefinition? definition)
    {
        definition = null;
        var decode = NcpeV2Codec.TryRead(bytes, out var semantic, out var artifactHash, out var expected);
        if (decode != NcpeV2Status.Success || semantic is null) return new(decode == NcpeV2Status.UnsupportedV1Reconstruction ? NcpeCelestialSystemLoadStatus.UnsupportedV1Reconstruction : NcpeCelestialSystemLoadStatus.FormatFailure, default);
        var s=semantic.System; var identity=new NcpeCelestialSystemLoadIdentity(new(s.SystemId),expected,artifactHash,new(s.SourceId),new(s.SourceVersion),s.ConverterId,s.ConverterVersion,new(s.ContentHashHigh,s.ContentHashLow),s.ConversionPolicyHash,new(s.AuthoredHashHigh,s.AuthoredHashLow),s.CoverageStart,s.CoverageEnd);
        var bodies=new CelestialBodyCatalogEntry[semantic.Bodies.Count];
        for(var i=0;i<bodies.Length;i++){var b=semantic.Bodies[i];bodies[i]=new(new(new(b.Id),b.Name,(CelestialBodyClassification)b.Classification,b.ParentId==0?null:new CelestialBodyId(b.ParentId),new(b.RotationReference),new(b.AtmosphereReference),new(b.VisualReference),b.Aliases.Count==0?null:new CelestialBodyAliases(b.Aliases.ToArray())),new(b.Mu,b.MeanRadius,b.EquatorialRadius,b.PolarRadius,b.Flattening,new(b.SiderealReference),new(b.PhysicalAtmosphereReference),new(b.PhysicalVisualReference),new(b.PhysicalSource),new(b.PhysicalConstantsVersion)));}
        var sources=new CelestialEphemerisSource[semantic.Sources.Count];for(var i=0;i<sources.Length;i++){var x=semantic.Sources[i];sources[i]=new(new(x.Id),(CelestialTrajectoryModel)x.Model,new(new(x.Id),new(x.Version),new(x.DomainId),x.CoverageStart,x.CoverageEnd,new(x.FrameId),new(x.ConstantsVersion),new(x.ContentHashHigh,x.ContentHashLow),new(x.AuthoredHashHigh,x.AuthoredHashLow)));}
        var nodes=new CelestialHierarchyNode[semantic.Bindings.Count];var fixedBodies=new List<FixedBodyEphemerisPayload>();for(var i=0;i<nodes.Length;i++){var b=semantic.Bindings[i];var payload=b.PayloadIndex;if(b.Model==1){payload=fixedBodies.Count;fixedBodies.Add(new(new(b.FixedPositionX,b.FixedPositionY,b.FixedPositionZ),new(b.FixedVelocityX,b.FixedVelocityY,b.FixedVelocityZ),new(b.FixedQuaternionX,b.FixedQuaternionY,b.FixedQuaternionZ,b.FixedQuaternionW),new(b.FixedAngularVelocityX,b.FixedAngularVelocityY,b.FixedAngularVelocityZ)));}nodes[i]=new CelestialHierarchyNode(new CelestialBodyId(b.BodyId),new CelestialEphemerisBinding((CelestialTrajectoryModel)b.Model,new CelestialEphemerisSourceId(b.SourceId),payload));}
        var payloads=new SampledEphemerisPayload[semantic.Payloads.Count];for(var i=0;i<payloads.Length;i++){var p=semantic.Payloads[i];payloads[i]=new(new(p.DomainId),p.FirstSampleIndex,p.SampleCount,(SampledEphemerisInterpolationModel)p.Interpolation,p.CoverageStart,p.CoverageEnd);}
        var samples=new CelestialEphemerisSample[semantic.Samples.Count];for(var i=0;i<samples.Length;i++){var x=semantic.Samples[i];samples[i]=new(x.DomainTick,new(x.PositionX,x.PositionY,x.PositionZ),new(x.VelocityX,x.VelocityY,x.VelocityZ));}
        var mapping=new CelestialSystemTimeMapping(new(s.SimulationAnchor),new(new(s.DomainId),s.DomainAnchor,s.DomainTicksPerSecond),s.ScaleNumerator,s.ScaleDenominator);var metadata=new CelestialEphemerisMetadata(new(s.SourceId),new(s.SourceVersion),new(s.DomainId),s.CoverageStart,s.CoverageEnd,new(s.FrameId),new(s.ConstantsVersion),new(s.ContentHashHigh,s.ContentHashLow),new(s.AuthoredHashHigh,s.AuthoredHashLow));
        if(!CelestialSystemDefinition.TryCreate(new(s.SystemId),bodies,nodes,mapping,metadata,sources,fixedBodies.ToArray(),[],[],payloads,samples,out definition,out _)){definition=null;return new(NcpeCelestialSystemLoadStatus.RuntimeValidationFailure,identity);}var actual=CelestialSystemDefinitionHash.Compute(definition!);if(actual!=expected){definition=null;return new(NcpeCelestialSystemLoadStatus.DefinitionHashMismatch,identity);}return new(NcpeCelestialSystemLoadStatus.Success,identity);
    }
}
