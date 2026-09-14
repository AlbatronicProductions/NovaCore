using System.Numerics;
using System.Reflection;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Commands;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class FinitePropellantTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Finite propellant: " + message); }
    private static BigInteger Integer(in PropellantInteger value)
    {
        BigInteger result = 0;
        for (var i = PropellantInteger.LimbCount - 1; i >= 0; i--) result = (result << 64) + value.Limb(i);
        return result;
    }
    // Independent numerical decomposition through normalized binary64 and BigInteger, not limb/field decoding.
    private static BigInteger FlowOracle(double value)
    {
        if (value == 0) return 0;
        var exponent = Math.ILogB(value);
        var significand = new BigInteger(Math.ScaleB(value, -exponent) * 4503599627370496d);
        var shift = exponent + 1022;
        return shift >= 0 ? significand << shift : significand >> -shift;
    }
    private static BigInteger MassOracle(double value) => FlowOracle(value) * 1_000_000;
    private static PropellantInteger Exact(BigInteger value)
    {
        Check(value >= 0 && value.GetBitLength() <= 2176, "test exact domain");
        var bytes = value.ToByteArray(isUnsigned:true, isBigEndian:true);
        var result = default(PropellantInteger); BigInteger prefix = 0;
        foreach (var b in bytes)
        {
            Check(PropellantInteger.TryMultiply(result, 256, out var shifted) &&
                PropellantInteger.TryAdd(shifted, PropellantInteger.FromUInt64(b), out result), "test exact construction");
            prefix = prefix * 256 + b;
            Check(Integer(result) == prefix, "construction cross-check");
        }
        return result;
    }
    // Independent rounding oracle: membership in adjacent-double midpoint cells, not production bit rounding.
    private static void Projection(in PropellantInteger units)
    {
        var u = Integer(units);
        var overflow = ((BigInteger.One << 2098) - (BigInteger.One << 2044)) * 1_000_000;
        var ok = units.TryToKilograms(out var value);
        if (!ok) { Check(u >= overflow, "projection overflow boundary"); return; }
        Check(double.IsFinite(value) && value >= 0, "finite nonnegative projection");
        var at = MassOracle(value); var even = (BitConverter.DoubleToUInt64Bits(value) & 1) == 0;
        var lower = value == 0 ? BigInteger.Zero : at + MassOracle(Math.BitDecrement(value));
        var upper = value == double.MaxValue ? overflow * 2 : at + MassOracle(Math.BitIncrement(value));
        Check((u * 2 > lower || (u * 2 == lower && even)) &&
            (u * 2 < upper || (u * 2 == upper && even)), "nearest-even midpoint-cell oracle");
    }
    internal static void Arithmetic()
    {
        Check(PropellantInteger.LimbCount * 64 == 2176 &&
            PropellantInteger.TicksPerSecond == SimulationInstant.TicksPerSecond, "implementation capacity and canonical units");
        var cases = 0;
        for (var exponent = 0; exponent < 2047; exponent++)
        {
            foreach (var mantissa in new[] { 0UL, 1UL, 0x000F_FFFF_FFFF_FFFFUL })
            {
                var value = BitConverter.UInt64BitsToDouble(((ulong)exponent << 52) | mantissa);
                Check(PropellantInteger.TryDecodeFlow(value, out var flow) && Integer(flow) == FlowOracle(value), "all exponent decode oracle");
                Check(PropellantInteger.TryFromKilograms(value, out var mass) && Integer(mass) == MassOracle(value), "all exponent mass oracle");
                Check(mass.TryToKilograms(out var copied) && BitConverter.DoubleToUInt64Bits(copied) == BitConverter.DoubleToUInt64Bits(value), "exact authored roundtrip");
                Check(PropellantInteger.TryMultiply(flow, long.MaxValue, out var product) &&
                    Integer(product) == FlowOracle(value) * long.MaxValue && product.BitLength <= 2161, "maximum tick multiplication");
                cases++;
            }
        }
        foreach (var invalid in new[] { -double.Epsilon, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Check(!PropellantInteger.TryDecodeFlow(invalid, out var none) && none.IsZero, "invalid exact decode has no result");
        Check(PropellantInteger.TryDecodeFlow(-0d, out var zero) && zero.IsZero, "negative zero numeric canonicalization");
        for (var bit = 64; bit < 2176; bit += 64)
        {
            var one = Exact(BigInteger.One << bit); var lower = Exact((BigInteger.One << bit) - 1);
            Check(PropellantInteger.TrySubtract(one, PropellantInteger.FromUInt64(1), out var borrow) && borrow == lower, "full borrow chain");
            Check(PropellantInteger.TryAdd(lower, PropellantInteger.FromUInt64(1), out var carry) && carry == one, "full carry chain");
            Check(!PropellantInteger.TrySubtract(lower, one, out var underflow) && underflow.IsZero, "no wrapped negative result");
        }
        var maximum = Exact((BigInteger.One << 2176) - 1);
        Check(!PropellantInteger.TryAdd(maximum, PropellantInteger.FromUInt64(1), out var wrapped) && wrapped.IsZero, "capacity overflow refused");
        Check(!PropellantInteger.TryMultiply(maximum, 2, out wrapped) && wrapped.IsZero, "multiply final carry refused");
        foreach (var u in new BigInteger[] { 0, 499999, 500000, 500001, 1500000, 2500000,
            ((BigInteger.One << 52) - 1) * 1_000_000 + 500000,
            ((BigInteger.One << 53) - 1) * 1_000_000 + 500000,
            MassOracle(double.MaxValue), MassOracle(double.MaxValue) * 2 })
            Projection(Exact(u));
        var random = new Random(61329);
        for (var i = 0; i < 512; i++)
        {
            var bits = ((ulong)random.Next(2047) << 52) | ((ulong)random.NextInt64() & 0x000F_FFFF_FFFF_FFFFUL);
            var source = MassOracle(BitConverter.UInt64BitsToDouble(bits));
            var u = source + random.Next(1_000_000);
            Projection(Exact(u));
            var divisor = (ulong)random.NextInt64(1, long.MaxValue);
            var flow = FlowOracle(BitConverter.UInt64BitsToDouble(bits));
            var exactFlow = Exact(flow);
            Check(PropellantInteger.TryMultiply(exactFlow, divisor, out var multiplied) &&
                Integer(multiplied) == flow * divisor, "random independent multiplier oracle");
        }
        // Pure successor chaining only. This is not canonical fuel consumption.
        PropellantInteger.TryFromKilograms(double.Epsilon, out var remaining);
        for (long i = 1; i <= 60; i++)
        {
            var ticks = i * 1_000_000 / 60 - (i - 1) * 1_000_000 / 60;
            Calculate(remaining, double.Epsilon, true, ticks, out var next, out _, out var classification);
            Check(classification == (i == 60 ? PropellantClassification.EndpointExhaustion : PropellantClassification.FullPowered), "tiny lattice debit classification");
            remaining = next;
        }
        Check(remaining.IsZero, "60 exact tiny debits exhaust once");
        PropellantInteger.TryFromKilograms(1, out remaining); var original = remaining;
        for (var i = 0; i < 4096; i++)
        { Calculate(remaining, Math.ScaleB(1d,-60), true, 16666, out var next, out _, out _); remaining = next; }
        Check(Integer(remaining) == Integer(original) - 4096 * FlowOracle(Math.ScaleB(1d,-60)) * 16666, "exact repeated debit conservation");
        Projection(remaining);
        double naive = 1;
        for (var i = 0; i < 4096; i++) naive -= Math.ScaleB(1d,-60) * (16666d/1_000_000);
        Check(naive == 1 && remaining.TryToKilograms(out var accumulated) && accumulated == Math.BitDecrement(1d),
            "lost naive debits accumulate until even the rounded exact observation changes");
        foreach (var q in new[] { double.Epsilon, 1d, 2d, double.MaxValue })
        {
            var cost = FlowOracle(q) * 16666;
            foreach (var delta in new[] { -1, 0, 1 })
            {
                var u = Exact(cost + delta);
                Calculate(u, q, true, 16666, out _, out _, out var classification);
                Check(classification == (delta < 0 ? PropellantClassification.InteriorExhaustion :
                    delta == 0 ? PropellantClassification.EndpointExhaustion : PropellantClassification.FullPowered), "one-quantum boundary");
            }
        }
        foreach (var q in new[] { -double.Epsilon, -1d, double.NaN, double.PositiveInfinity })
            Check(PropellantSegmentation.Calculate(default,q,true,16666,out _,out _,out _,out _,out _,out _,out _) == PropellantPreparationStatus.IncompatibleFlow,"invalid flow refusal");
        Check(PropellantSegmentation.Calculate(default,1,true,0,out _,out _,out _,out _,out _,out _,out _) == PropellantPreparationStatus.InvalidInterval,"zero duration refusal");
        Console.WriteLine($"PROPELLANT_ARITHMETIC PASS binary64_cases={cases} random_cases=512 limb_bits=2176 maximum_product_bits=2161 exact_conservation=PASS");
    }
    private static void Calculate(in PropellantInteger u, double q, bool demand, long n,
        out PropellantInteger successor, out PropellantDuration powered, out PropellantClassification classification)
    {
        Check(PropellantSegmentation.Calculate(u,q,demand,n,out classification,out var v,out var c,out var consumed,
            out successor,out powered,out var unpowered)==PropellantPreparationStatus.Ready,"pure calculation");
        Check(Integer(consumed)+Integer(successor)==Integer(u),"exact mass conservation");
        Check(Integer(powered.Numerator)*Integer(unpowered.Denominator)+Integer(unpowered.Numerator)*Integer(powered.Denominator)==
            n*Integer(powered.Denominator)*Integer(unpowered.Denominator),"exact duration conservation");
        Check(powered.IsWithin(n)&&unpowered.IsWithin(n),"exact duration bounds");
        var oracleFlow=FlowOracle(q);var oracleCost=oracleFlow*n;var oracleSource=Integer(u);
        Check(Integer(c)==oracleCost && Integer(v)==oracleFlow,"exact demand oracle");
        var oracleConsumed=oracleFlow.IsZero?BigInteger.Zero:BigInteger.Min(oracleSource,oracleCost);
        Check(Integer(consumed)==oracleConsumed && Integer(successor)==oracleSource-oracleConsumed,"independent minimum consumption and successor");
        var oracleClass=oracleFlow.IsZero?PropellantClassification.NoDemand:oracleSource.IsZero?PropellantClassification.NoFeed:
            oracleSource>oracleCost?PropellantClassification.FullPowered:oracleSource==oracleCost?
            PropellantClassification.EndpointExhaustion:PropellantClassification.InteriorExhaustion;
        Check(classification==oracleClass,"independent complete classification");
        Check(oracleFlow.IsZero?powered.IsZero:
            Integer(powered.Numerator)*oracleFlow==oracleConsumed*Integer(powered.Denominator),"independent powered ratio (cannot swap durations)");
        Check(oracleFlow.IsZero?Integer(unpowered.Numerator)==n*Integer(unpowered.Denominator):
            Integer(unpowered.Numerator)*oracleFlow==(oracleCost-oracleConsumed)*Integer(unpowered.Denominator),"independent remainder ratio");
    }
    private static IdealEngineDefinition EngineDefinition(double q=2, bool available=true, double? thrust=null, double exhaust=1)
    {
        var mount = q == double.MaxValue ? Double3.Zero : new Double3(0,2,0);
        Check(IdealEngineDefinition.TryCreate(1,1,mount,Double3.UnitX,thrust??q,exhaust,available,out var d)==EnginePreparationStatus.Ready,"engine definition");
        return d!;
    }
    private static PropellantDefinition Definition(double fuel=1d/128, double dry=8)
    {
        Check(PropellantDefinition.TryCreate(new(1,1,1,1,1,1,1,PropellantMassLaw.CentralPointReservoirV1,dry,new(2,3,4)),
            fuel,out var d)==PropellantPreparationStatus.Ready,"dedicated resource definition");
        return d!;
    }
    private readonly record struct Snapshot(ContinuationClockState Clock, StateRevision Revision, TimelineRevision Timeline,
        SpacecraftTranslationState Linear, SpacecraftRigidBodyRotationState Angular, SpacecraftPhysicalProperties Mass,
        SpacecraftCommandObservation Commands, EnginePreparationProgress EngineProgress, int History, int Pending);
    private sealed class Fixture
    {
        internal static readonly SpacecraftId Craft = new(101);
        internal readonly SimulationClock Clock;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly SpacecraftCommandAuthority Commands;
        internal readonly EnginePreparationAuthority Parent;
        internal PropellantResourceAuthority Resource = null!;
        internal readonly PropellantDefinition Definition;
        internal EngineActuationProposal EngineToken;
        internal readonly SimulationInstant Target = new(16666);
        internal Fixture(double fuel=1d/128, IdealEngineDefinition? engine=null, bool bind=true, double? mass=null)
        {
            Definition=FinitePropellantTests.Definition(fuel);
            var root=new ReferenceFrameId(1);var body=new ReferenceFrameId(2);var graph=new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(root,null,ReferenceFrameKind.Ecl,"resource root"));graph.Add(new ReferenceFrameNode(body,root,ReferenceFrameKind.Ccf,"dry COM"));
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft,root,body,"declared dry-body resource fixture")],
                [new(Craft,default,DoubleQuaternion.Identity,new(0,0,.1),new(2,3,4),default,RigidBodyRotationModel.ConstantBodyTorqueV1)],
                [new(mass??(8+fuel))],[new(Craft,root,default,default,default,default)],graph.Build(),out var store,out _),"fixture");
            Clock=new(default,new SimulationTimeline(4));Engine=new(Clock,new SimulationState(spacecraft:store),4);
            Check(Engine.PrepareSpacecraftCommands(Craft,5000,out var commands)==SpacecraftCommandStatus.Accepted,"command binding");Commands=commands!;
            Check(Engine.BindSingleEnginePreparation(Commands,engine??EngineDefinition(),out var parent)==EnginePreparationStatus.Ready,"engine binding");Parent=parent!;
            if(bind) Bind();
        }
        internal void Bind()
        { Check(Engine.BindFinitePropellant(Parent,Definition,out var authority)==PropellantPreparationStatus.Ready,"resource binding");Resource=authority!; }
        internal void Seal(bool ignite=true, double throttle=1)
        {
            if(ignite)Send(SpacecraftCommandIntent.Ignite());
            Send(SpacecraftCommandIntent.ThrottlePosition(throttle));
            Check(Engine.CloseSpacecraftCommandBoundary(Commands,out _)==SpacecraftCommandStatus.BoundaryReady,"close engine boundary");
            Check(Engine.PrepareSingleEngineActuation(Parent,Target,out EngineToken)==EnginePreparationStatus.Prepared,"genuine engine proposal");
        }
        internal void Send(SpacecraftCommandIntent intent)
        {
            Engine.ObserveSpacecraftCommands(Commands,out var current);
            Check(Engine.AdmitSpacecraftCommand(Commands,1,current.LastAcceptedSequence+1,intent).Status==SpacecraftCommandStatus.Accepted,"command admission");
            Check(Engine.CommitNextSpacecraftCommand(Commands).Status is SpacecraftCommandStatus.Committed or SpacecraftCommandStatus.NoChange,"command commit");
        }
        internal Snapshot Capture()
        {
            Engine.State.Spacecraft.TryGetTranslation(Craft,out var l,out var m);Engine.State.Spacecraft.TryGetRigidBody(Craft,out var a);
            Engine.ObserveSpacecraftCommands(Commands,out var c);Engine.ObserveEnginePreparation(Parent,out var p);
            return new(Engine.CaptureContinuationClock(),Engine.State.Revision,Clock.Timeline.Revision,l,a,m,c,p,Engine.ProcessedCount,Clock.Timeline.PendingCount);
        }
        internal PropellantSourceObservation Source(out PropellantPreparationProgress progress)
        { Check(Engine.ObserveFinitePropellant(Resource,out var s,out progress)==PropellantPreparationStatus.Ready,"source read");return s; }
        internal PropellantSegmentationPreview Prepare(out PropellantProposal token)
        {
            var before=Capture();var source=Source(out _);
            Check(Engine.PrepareFinitePropellant(Resource,EngineToken,Target,out token)==PropellantPreparationStatus.Prepared,"resource prepare");
            Check(Engine.PreviewFinitePropellant(Resource,token,out var v)==PropellantPreparationStatus.Preview,"copied proposal");
            Check(Capture()==before && Source(out _)==source,"all canonical/engine/source state unchanged");
            return v;
        }
    }
    internal static void Cheap()
    {
        Arithmetic();Definitions();Boundaries();Authority();
        Console.WriteLine("PROPELLANT_CHEAP PASS analytical=PASS authority=PASS mass_law=PASS no_canonical_mutation=PASS");
    }
    private static void Definitions()
    {
        var values=Definition().Values;
        foreach(var fuel in new[]{-double.Epsilon,-1d,double.NaN,double.PositiveInfinity})
            Check(PropellantDefinition.TryCreate(values,fuel,out _)==PropellantPreparationStatus.InvalidDefinition,"invalid resource");
        foreach(var v in new[]{values with{ResourceId=0},values with{Version=0},values with{FeedId=0},values with{EngineId=0},
            values with{EngineVersion=0},values with{DryBodyId=0},values with{DryBodyVersion=0},values with{MassLaw=0},
            values with{DryMassKilograms=0},values with{DryMassKilograms=double.NaN},values with{DryInertia=new(0,1,1)},
            values with{DryInertia=new(double.PositiveInfinity,1,1)}})
            Check(PropellantDefinition.TryCreate(v,0,out _)==PropellantPreparationStatus.InvalidDefinition,"invalid mass-law identity/physics");
        Check(PropellantDefinition.TryCreate(values with{DryMassKilograms=double.MaxValue},double.MaxValue,out _)==PropellantPreparationStatus.InvalidDefinition,"nonfinite total mass refusal");
        var f=new Fixture(bind:false);var before=f.Capture();
        Check(f.Engine.BindFinitePropellant(f.Parent,null,out _)==PropellantPreparationStatus.InvalidDefinition && f.Capture()==before,"cold refusal nonmutation");
        PropellantDefinition.TryCreate(values with{EngineId=2},1d/128,out var wrongEngine);
        Check(f.Engine.BindFinitePropellant(f.Parent,wrongEngine,out _)==PropellantPreparationStatus.InvalidDefinition,"wrong engine definition");
        f.Bind();
        Check(f.Engine.BindFinitePropellant(f.Parent,f.Definition,out _)==PropellantPreparationStatus.AlreadyBound,"single resource/consumer only");
        var wet=new Fixture(bind:false,mass:8);
        Check(wet.Engine.BindFinitePropellant(wet.Parent,wet.Definition,out _)==PropellantPreparationStatus.InvalidDefinition,"no wet/dry double counting or mass edit");
        var late=new Fixture(bind:false);late.Seal();
        Check(late.Engine.BindFinitePropellant(late.Parent,late.Definition,out _)==PropellantPreparationStatus.LateBinding,"cold only");
    }
    private static void Boundaries()
    {
        foreach(var (fuel,q,expected) in new[]{
            (0d,2d,PropellantClassification.NoFeed),(1d,2d,PropellantClassification.FullPowered),
            (260.40625,15625d,PropellantClassification.EndpointExhaustion),(1d/128,3d,PropellantClassification.InteriorExhaustion),
            (double.Epsilon,2d,PropellantClassification.InteriorExhaustion),
            (16666d/1e6,1d,PropellantClassification.FullPowered),
            (Math.BitDecrement(16666d/1e6),1d,PropellantClassification.InteriorExhaustion),
            (double.Epsilon,double.Epsilon,PropellantClassification.FullPowered),
            (double.MaxValue,double.MaxValue,PropellantClassification.FullPowered)})
        {
            var f=new Fixture(fuel,EngineDefinition(q));f.Seal();var v=f.Prepare(out var token);
            Check(v.Classification==expected && v.Meaning==PropellantPreviewMeaning.ProposedIfApplied,"sealed exact classification");
            Calculate(v.Resource.RemainingUnits,q,true,16666,out var successor,out var powered,out var classification);
            Check(v.SuccessorUnits==successor&&v.PoweredDuration==powered&&v.Classification==classification,"sealed versus independent arithmetic");
            Check(Integer(v.Resource.RemainingUnits)==Integer(v.ConsumedUnits)+Integer(v.SuccessorUnits),"sealed conservation");
            Check(v.TryGetInteriorExhaustion(out var witness)==(expected==PropellantClassification.InteriorExhaustion),"interior identity only");
            if(expected==PropellantClassification.InteriorExhaustion)
                Check(witness.Offset==v.PoweredDuration && witness.Resource==v.Resource && witness.Engine==v.Engine,"source-bound exact witness");
            for(var i=0;i<v.SegmentCount;i++)
            {
                Check(v.TryGetSegment(i,out var segment),"bounded segment");
                Check(segment.Powered ? segment.EngineForceBodyNewtons==v.Engine.ProposedForceBodyNewtons &&
                    segment.EngineMomentBodyNewtonMetres==v.Engine.ProposedMomentBodyNewtonMetres :
                    segment.EngineForceBodyNewtons==default&&segment.EngineMomentBodyNewtonMetres==default,"unaltered body wrench then zero");
            }
            Check(!v.TryGetSegment(-1,out _)&&!v.TryGetSegment(v.SegmentCount,out _),"no dynamic segments");
            PropellantInteger.TryAdd(f.Definition.DryMassUnits,v.SuccessorUnits,out var total);
            Projection(total);total.TryToKilograms(out var projected);
            Check(v.ProposedSuccessorMass.TotalMassKilograms==projected&&v.ProposedSuccessorMass.Inertia==new PrincipalMomentsOfInertia(2,3,4),"conditional mass model");
            if(fuel==double.Epsilon&&q==2)
                Check(!v.PoweredDuration.IsZero&&fuel/q==0&&Integer(v.PoweredDuration.Numerator)>0,"positive exact tiny duration not omitted");
            Check(v.Engine.ProposedLatch==ProposedEngineLatch.Enabled,"starvation does not rewrite intent");
            Check(f.Engine.RetireFinitePropellant(f.Resource,token)==PropellantPreparationStatus.Retired,"explicit unused retirement");
        }
        foreach(var (ignite,throttle,available) in new[]{(false,1d,true),(true,0d,true),(true,1d,false)})
        {
            var f=new Fixture(0,EngineDefinition(available:available));f.Seal(ignite,throttle);var v=f.Prepare(out _);
            Check(v.Classification==PropellantClassification.NoDemand&&v.PoweredDuration.IsZero&&v.ConsumedUnits.IsZero&&
                !v.TryGetInteriorExhaustion(out _),"zero cases before equality/empty");
        }
        var underflow=new Fixture(engine:EngineDefinition(thrust:double.Epsilon,exhaust:2));underflow.Seal();
        var before=underflow.Capture();var source=underflow.Source(out var progress);
        Check(underflow.Engine.PrepareFinitePropellant(underflow.Resource,underflow.EngineToken,underflow.Target,out _)==PropellantPreparationStatus.IncompatibleFlow&&
            underflow.Capture()==before&&underflow.Source(out var after)==source&&after==progress,"positive thrust zero flow refuses without losing engine");
    }
    private static void SetField(object target,string name,object value) =>
        target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(target,value);
    private static void Authority()
    {
        var f=new Fixture();f.Seal();var before=f.Capture();var source=f.Source(out var progress);
        Check(f.Engine.RefuseFinitePropellantForTest(f.Resource,f.EngineToken,f.Target,out var none)==PropellantPreparationStatus.PreparationRefused&&
            f.Capture()==before&&f.Source(out var unchanged)==source&&unchanged==progress,"final refusal preserves all source/bookkeeping");
        Check(f.Engine.PreviewFinitePropellant(f.Resource,none,out _)==PropellantPreparationStatus.InvalidProposal,"refusal issues no seal");
        Check(f.Engine.PrepareFinitePropellant(f.Resource,default,f.Target,out _)==PropellantPreparationStatus.InvalidEngineProposal,"copied/default engine is not authority");
        Check(f.Engine.PrepareFinitePropellant(f.Resource,new(0,new object()),f.Target,out _)==PropellantPreparationStatus.InvalidEngineProposal,"fabricated parent");
        Check(f.Engine.PrepareFinitePropellant(f.Resource,f.EngineToken,new(16667),out _)==PropellantPreparationStatus.InvalidInterval,"interval mismatch");
        var first=f.Prepare(out var token);f.Source(out var sealedProgress);
        Check(f.Engine.PrepareFinitePropellant(f.Resource,f.EngineToken,f.Target,out _)==PropellantPreparationStatus.OutstandingProposal &&
            f.Source(out var stillSealed)==source&&stillSealed==sealedProgress&&f.Capture()==before,"duplicate preserves active slot");
        var foreign=new Fixture();foreign.Seal();
        Check(f.Engine.PrepareFinitePropellant(foreign.Resource,f.EngineToken,f.Target,out _)==PropellantPreparationStatus.InvalidAuthority&&
            foreign.Engine.PrepareFinitePropellant(foreign.Resource,f.EngineToken,foreign.Target,out _)==PropellantPreparationStatus.InvalidEngineProposal,"foreign owner and token");
        var fabricated=new PropellantResourceAuthority(f.Parent,f.Definition);
        Check(f.Engine.ObserveFinitePropellant(fabricated,out _,out _)==PropellantPreparationStatus.InvalidAuthority,"equal resource values cannot forge ownership");
        Check(f.Engine.PreviewFinitePropellant(f.Resource,new(token.Generation,new object()),out _)==PropellantPreparationStatus.InvalidProposal,"fabricated resource seal");
        Check(Task.Run(()=>f.Engine.PrepareFinitePropellant(f.Resource,f.EngineToken,f.Target,out _)).GetAwaiter().GetResult()==PropellantPreparationStatus.WrongOwnerThread,"wrong owner");
        Check(f.Clock.PublicationPhase.TryEnter(f),"hold owner phase");
        try
        {
            Check(f.Engine.PrepareFinitePropellant(f.Resource,f.EngineToken,f.Target,out _)==PropellantPreparationStatus.ReentrantOperation&&
                f.Engine.PreviewFinitePropellant(f.Resource,token,out _)==PropellantPreparationStatus.ReentrantOperation&&
                f.Engine.RetireFinitePropellant(f.Resource,token)==PropellantPreparationStatus.ReentrantOperation,"reentrant operations");
        }
        finally{f.Clock.PublicationPhase.Exit();}
        Check(f.Engine.RetireFinitePropellant(f.Resource,token)==PropellantPreparationStatus.Retired&&f.Capture()==before,"resource retirement does not retire parent");
        var repeat=f.Prepare(out var next);
        Check(first==repeat&&next.Generation!=token.Generation&&f.Engine.PreviewFinitePropellant(f.Resource,token,out _)==PropellantPreparationStatus.InvalidProposal,"same would-event new lease, old cannot replay");
        Check(f.Engine.DiscardSingleEngineProposal(f.Parent,f.EngineToken)==EnginePreparationStatus.Retired&&
            f.Engine.PreviewFinitePropellant(f.Resource,next,out _)==PropellantPreparationStatus.StaleSource,"parent retirement invalidates dependent");
        Check(f.Engine.RetireFinitePropellant(f.Resource,next)==PropellantPreparationStatus.Retired&&
            f.Engine.RetireFinitePropellant(f.Resource,next)==PropellantPreparationStatus.InvalidProposal,"stale cleanup once");
        Check(f.Engine.PrepareSingleEngineActuation(f.Parent,f.Target,out _)==EnginePreparationStatus.IntervalConsumed,"no parent cursor rewind");
        var equivalentA=new Fixture();equivalentA.Seal();var equivalentB=new Fixture();equivalentB.Seal();
        var valueA=equivalentA.Prepare(out _);var valueB=equivalentB.Prepare(out _);
        Check(valueA==valueB && valueA.TryGetInteriorExhaustion(out var eventA) &&
            valueB.TryGetInteriorExhaustion(out var eventB) && eventA==eventB,"fresh equivalent sources have identical deterministic value/event provenance");
        // Test-only reflection corrupts otherwise immutable source facts. No production mutation seam exists.
        foreach(var field in new[]{"ResourceRevision","RemainingUnits","Definition"})
        {
            var stale=new Fixture();stale.Seal();stale.Prepare(out var old);
            if(field=="ResourceRevision")SetField(stale.Resource,field,1UL);
            else if(field=="RemainingUnits")SetField(stale.Resource,field,PropellantInteger.FromUInt64(1));
            else
            {
                PropellantDefinition.TryCreate(stale.Definition.Values with{Version=2},1d/128,out var changed);
                SetField(stale.Resource,field,changed!);
            }
            var captured=stale.Capture();var state=stale.Source(out var stateProgress);
            Check(stale.Engine.PreviewFinitePropellant(stale.Resource,old,out _)==PropellantPreparationStatus.StaleSource&&
                stale.Capture()==captured&&stale.Source(out var p)==state&&p==stateProgress,"changed resource source refuses nonmutating");
        }
        foreach(var mutation in new[]{"debt","pause","rate","timeline","physical","command"})
        {
            var stale=new Fixture();stale.Seal();stale.Prepare(out var old);
            switch(mutation)
            {
                case "debt":stale.Clock.AdvanceByHostDuration(new(1));break;
                case "pause":stale.Clock.Pause();break;
                case "rate":stale.Clock.TrySetRate(SimulationRate.Two);break;
                case "timeline":stale.Clock.Timeline.Schedule(stale.Clock.CurrentTime,new(new(1),new(50000),0,SimulationEventKind.Marker));break;
                case "command":stale.Engine.SaturateCommandRevisionForTest(stale.Commands);break;
                case "physical":
                    var replacement=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(stale.Engine.State,new(Fixture.Craft,new(1,0,0),stale.Clock.CurrentTime));
                    Check(replacement.Succeeded&&stale.Engine.ValidateAndCommit(replacement.Transaction!.Value).Committed,"external mutation witness");break;
            }
            var captured=stale.Capture();var state=stale.Source(out var stateProgress);
            Check(stale.Engine.PreviewFinitePropellant(stale.Resource,old,out _)==PropellantPreparationStatus.StaleSource&&
                stale.Capture()==captured&&stale.Source(out var p)==state&&p==stateProgress,"external source change cannot authorize resource");
        }
        var exhausted=new Fixture();exhausted.Seal();
        var storage=typeof(SimulationTransactionEngine).GetField("_propellantPreparation",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(exhausted.Engine)!;
        SetField(storage,"Generation",ulong.MaxValue);
        Check(exhausted.Engine.PrepareFinitePropellant(exhausted.Resource,exhausted.EngineToken,exhausted.Target,out _)==PropellantPreparationStatus.GenerationExhausted,
            "private seal cannot wrap and revive stale token");
    }
}
