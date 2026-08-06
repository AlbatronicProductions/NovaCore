using NovaCore.EphemerisFormat;

var input = Fixture();
Check(EphemerisArtifactCodec.TryBuild(input, out var first) == EphemerisArtifactStatus.Success && first is not null, "build");
Check(EphemerisArtifactCodec.TryBuild(input with { Bodies = input.Bodies.Reverse().ToArray() }, out var reordered) == EphemerisArtifactStatus.Success && reordered is not null && first!.Bytes.SequenceEqual(reordered.Bytes), "canonical order");
Check(EphemerisArtifactCodec.TryRead(first!.Bytes, out var roundTrip) == EphemerisArtifactStatus.Success && roundTrip is not null && first.Bytes.SequenceEqual(roundTrip.Bytes), "round trip");
var altered=first.Bytes.ToArray(); altered[40]^=1; Check(EphemerisArtifactCodec.TryRead(altered,out _) == EphemerisArtifactStatus.HashMismatch,"hash mismatch");
var duplicate=input with { Bodies=[..input.Bodies, input.Bodies[0]]}; Check(EphemerisArtifactCodec.TryBuild(duplicate,out _) == EphemerisArtifactStatus.InvalidMetadata,"duplicate body");
var before=GC.GetAllocatedBytesForCurrentThread(); ulong hash=0; for(var i=0;i<1000;i++)hash^=EphemerisHash.Compute(first.Bytes).Value; var allocated=GC.GetAllocatedBytesForCurrentThread()-before; Console.WriteLine($"Ephemeris format: PASS hash=0x{first.Manifest.ArtifactHash} warmHashAllocation={allocated} bytes"); return 0;
static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
static NormalizedEphemerisInput Fixture()=>new(1,2,3,4,5,6,7,8,-20,20,9,10,[Body(1,0,0),Body(2,1,10),Body(3,1,-10)]);
static NormalizedEphemerisBody Body(ulong id,ulong parent,double d)=>new(id,parent,id,EphemerisInterpolationModel.CubicHermitePositionVelocityV1,[new(-20,d-20,1,2,1,0,0),new(-5,d-5,2,3,1,0,0),new(5,d+5,3,4,1,0,0),new(20,d+20,4,5,1,0,0)],.1,.01);
