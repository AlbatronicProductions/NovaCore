using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private static void Refusal(Action action,string message)
    {
        try {action();}
        catch(InvalidDataException e){Check(e.Message==message,"refusal diagnostic: "+message);return;}
        throw new InvalidOperationException("CONSTRUCTION accepted arithmetic refusal: "+message);
    }
    private static int MagnitudeWidth(BigInteger value)
    {
        var bytes=BigInteger.Abs(value).ToByteArray(isUnsigned:true,isBigEndian:true);
        var first=bytes[0];var bits=0;while(first!=0){bits++;first>>=1;}
        return (bytes.Length-1)*8+bits;
    }
    private static void PowerWidthProof()
    {
        // Scalar admission boundary, independently constructed positive and signed values.
        // These are width-contract tests, not claims that every integer D is a reachable catalog.
        foreach(var d in new[]{1,499999,500000})
        {
            var widths=ConstructionPowerWidths.Derive(d);
            var boundaryPair=((BigInteger.One<<d)-1)*((BigInteger.One<<d)-2);
            var parserMinimum=-(BigInteger.One<<(4*((d+2199)/4+2)-1));
            Check(MagnitudeWidth(boundaryPair)<=widths.ScratchMagnitudeBits&&MagnitudeWidth(parserMinimum)<=widths.ScratchMagnitudeBits,"both expression families fit admitted magnitude");
            Check(boundaryPair<(BigInteger.One<<(widths.ScratchSignedBits-1))&&parserMinimum>=-(BigInteger.One<<(widths.ScratchSignedBits-1)),"signed endpoints fit without overflow");
            Check(d==1?MagnitudeWidth(parserMinimum)==widths.ScratchMagnitudeBits:MagnitudeWidth(boundaryPair)==widths.ScratchMagnitudeBits,"declared maximum attained by dominating family");
        }
        Check(ConstructionPowerWidths.Derive(499999).ScratchMagnitudeBits==999998&&
            ConstructionPowerWidths.Derive(500000).ScratchMagnitudeBits==1_000_000&&
            ConstructionPowerWidths.Derive(500000).ScratchSignedBits==1_000_001,"one below and exact magnitude-cap admission with sign headroom");
        Refusal(()=>ConstructionPowerWidths.Derive(500001),"Power arithmetic admission width exceeded.");
        Refusal(()=>ConstructionPowerWidths.Derive(0),"Power denominator admission width exceeded.");

        var parser=LocalPower([new("battery",ElectricalRole.Battery,1,0,null)],[new("battery",1,true)]);
        var original=parser.Initial();var originalBytes=original.Save();var save=AssemblyJson.Read<ConstructionPowerSave>(originalBytes);
        var digits=parser.Widths.HexDigits;
        foreach(var s in new[]{"7"+new string('f',digits-1),"8"+new string('0',digits-1)})
        {
            var value=BigInteger.Parse(s,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);
            Check(MagnitudeWidth(value)==(s[0]=='8'?2208:2207),"independent signed parser boundary reconstruction");
            Refusal(()=>ConstructionPowerState.Load(parser,AssemblyJson.Write(save with {Charge=[s]})),"Noncanonical energy integer.");
        }
        Refusal(()=>ConstructionPowerState.Load(parser,AssemblyJson.Write(save with {Charge=["0"+new string('f',digits)]})),"Invalid bounded energy integer.");
        foreach(var bits in new[]{parser.IntegerBits-1,parser.IntegerBits,parser.IntegerBits+1})
        {
            var encoded=((BigInteger.One<<bits)-1).ToString("x",CultureInfo.InvariantCulture);
            Refusal(()=>ConstructionPowerState.Load(parser,AssemblyJson.Write(save with {Charge=[encoded]})),
                bits<=parser.IntegerBits?"Battery energy outside capacity.":"Noncanonical energy integer.");
        }
        Check(originalBytes.SequenceEqual(original.Save()),"all parser refusals preserve source exactly");

        var fuel=LocalFuel([("a","F",1,true),("b","F",1,true),("c","F",1,true),("d","F",1,true)],
            [Consumer("engine",1,[new("F",1)],["a","b","c","d"])]);
        var net=WithPower(fuel,[new("battery",ElectricalRole.Battery,double.MaxValue,0,null),new("generator",ElectricalRole.EngineGenerator,0,double.MaxValue,"engine")],
            [new("battery",0,true),new("generator",0,true)]);
        var dmax=1+net.Fuel.InitiallyPositive*net.Fuel.RateBits;var m=(BigInteger.One<<dmax)-1;
        Check(net.Fuel.InitiallyPositive==4&&net.Fuel.RateBits==1078&&dmax==4313,"independent four-store width fixture");
        var empty=ConstructionFuelState.Create(net.Fuel,[0,0,0,0],1,4);
        var initial=net.Initial();var charge=initial.Charge.SetItem(Module(net,"battery"),BigInteger.One);
        var source=ConstructionPowerState.Create(net,charge,m,initial.Active,initial.Cursor,4);
        var sourceBytes=source.Save();var fuelBytes=empty.Save();
        var pair=m*(m-1);var below=(BigInteger.One<<(dmax-1))*m;
        Check(BigInteger.GreatestCommonDivisor(m,m-1)==1&&MagnitudeWidth(pair)==2*dmax&&MagnitudeWidth(below)==2*dmax-1,"pair maximum and one-bit-below independent products");
        Check(net.ScratchBits==MagnitudeWidth(pair)&&net.Widths.ScratchSignedBits==MagnitudeWidth(pair)+1,"pair exactly reaches declared magnitude; sign retained");
        Refusal(()=>ConstructionPowerSolver.Advance(source,1,new(empty,[new(1,m-1)])),"Electrical lifetime arithmetic bound exceeded.");
        Check(sourceBytes.SequenceEqual(source.Save())&&fuelBytes.SequenceEqual(empty.Save()),"maximum pair refusal cannot mutate either source");

        // Historic three-factor maximum, and the originally reported 9695-bit witness.
        var e=3;var historicalD=1+e*net.Fuel.RateBits;var top=(BigInteger.One<<historicalD)-1;
        foreach(var denominators in new[]{new[]{top,top-1,top-2},new[]{(BigInteger.One<<3232)+1,(BigInteger.One<<3232)-1,BigInteger.One<<3231}})
        {
            var a=denominators[0];var b=denominators[1];var c=denominators[2];
            Check(BigInteger.GreatestCommonDivisor(a,b)==1&&BigInteger.GreatestCommonDivisor(a*b,c)==1,"historic coprime triple");
            Check(MagnitudeWidth(a*b*c)==(a==top?9705:9695)&&MagnitudeWidth(a*b)<=net.ScratchBits,"historic triple exceeds capacity but initial pair fits");
            var fs=ConstructionFuelState.Create(net.Fuel,[0,0,0,1],b,e);
            var ps=ConstructionPowerState.Create(net,charge,a,initial.Active,initial.Cursor,e);
            var beforeP=ps.Save();var beforeF=fs.Save();
            Refusal(()=>ConstructionPowerSolver.Advance(ps,1,new(fs,[new(1,c)])),"Electrical lifetime arithmetic bound exceeded.");
            Refusal(()=>ConstructionPowerSolver.Advance(ps,1,new(fs,[new(-1,c)])),"Invalid generator active duration.");
            Check(beforeP.SequenceEqual(ps.Save())&&beforeF.SequenceEqual(fs.Save()),"historic and compound-invalid refusal immutable");
        }

        // Largest legal binary64 rate/capacity and Int64 interval. The composed snapshot is
        // scalar-admitted accounting input, not a claim of physical flight history.
        var decodedRate=((BigInteger.One<<53)-1)<<2045;
        var decodedCapacity=decodedRate*1_000_000;
        var timeNumerator=new BigInteger(long.MaxValue)*m;
        var wanted=decodedRate*timeNumerator;
        Check(MagnitudeWidth(decodedRate)==2098&&MagnitudeWidth(decodedCapacity)==2118&&MagnitudeWidth(wanted)==dmax+2161,"maximum valid correlated multiplication reconstructed independently");
        var full=ConstructionPowerSolver.Advance(source,long.MaxValue,new(empty,[new(timeNumerator,m)]));
        Check(full.State.InQ(Module(net,"battery"))==new ConstructionRatio(decodedCapacity,1),"maximal request saturates exact capacity without truncation");
        var delivered=full.Delivered[Module(net,"generator")];
        Check(delivered.Numerator*m==(decodedCapacity*m-1)*delivered.Denominator,"maximum valid delivery independent rational identity");
        Check(sourceBytes.SequenceEqual(source.Save())&&fuelBytes.SequenceEqual(empty.Save()),"valid maximum proposal also leaves sources immutable");
        Check(full.State.Save().SequenceEqual(ConstructionPowerState.Load(net,full.State.Save()).Save()),"maximum exact result canonical roundtrip");

        // A genuine solver-produced fuel interval at the full time/rate extremes.
        var tiny=LocalFuel([("f","F",1,true)],[Consumer("engine",double.Epsilon,[new("F",1)],["f"])]);
        var live=WithPower(tiny,[new("battery",ElectricalRole.Battery,double.MaxValue,0,null),new("generator",ElectricalRole.EngineGenerator,0,double.MaxValue,"engine")],
            [new("battery",0,true),new("generator",0,true)]);
        var evolution=ConstructionFuelSolver.Advance(live.Fuel.Initial(),long.MaxValue,[true]);
        var actual=ConstructionPowerSolver.Advance(live.Initial(),long.MaxValue,evolution);
        Check(evolution.ActiveTicks[0]==new ConstructionRatio(long.MaxValue,1)&&actual.Delivered[Module(live,"generator")]==new ConstructionRatio(decodedCapacity,1),"genuine maximum interval generator saturates exact battery");
    }
}
