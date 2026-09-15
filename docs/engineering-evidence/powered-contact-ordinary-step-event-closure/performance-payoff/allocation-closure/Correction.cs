using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Actuation;

// Raw source operands only. No per-event duty/wrench/projection is cached here.
internal readonly struct FixedInput
{
    internal readonly PropellantInteger HpN,HpD,MassN,MassD,FxN,FxD,FyN,FyD,EN,ED,InertiaN,InertiaD,GravityN,GravityD;
    internal readonly uint HN,HD;
    internal readonly int FxSign,FySign,ESign;
    internal FixedInput(ExactInput x)
    {
        HpN=Import(x.Hp.N);HpD=Import(x.Hp.D);MassN=Import(x.Mass.N);MassD=Import(x.Mass.D);
        FxN=Import(System.Numerics.BigInteger.Abs(x.Fx.N));FxD=Import(x.Fx.D);FxSign=x.Fx.N.Sign;
        FyN=Import(System.Numerics.BigInteger.Abs(x.Fy.N));FyD=Import(x.Fy.D);FySign=x.Fy.N.Sign;
        EN=Import(System.Numerics.BigInteger.Abs(x.E.N));ED=Import(x.E.D);ESign=x.E.N.Sign;
        InertiaN=Import(x.Inertia.N);InertiaD=Import(x.Inertia.D);GravityN=Import(x.Gravity.N);GravityD=Import(x.Gravity.D);
        HN=checked((uint)x.H.N);HD=checked((uint)x.H.D);
        if(HN==0||HD==0)throw new InvalidOperationException("Invalid ordinary duration");
    }
    // Cold diagnostic import using existing fixed operations. No answer projection.
    private static PropellantInteger Import(System.Numerics.BigInteger value)
    {
        if(value.Sign<0)throw new InvalidOperationException("Unsigned fixed input required");
        var result=default(PropellantInteger);
        foreach(var digit in value.ToByteArray(isUnsigned:true,isBigEndian:true))
        {
            if(!PropellantInteger.TryMultiply(result,256,out var shifted)||
               !PropellantInteger.TryAdd(shifted,PropellantInteger.FromUInt64(digit),out result))
                throw new InvalidOperationException("Source exceeds banked fixed input range");
        }
        return result;
    }
}

internal static class Correction
{
    // Cold oracle inspection only; never used by Map or allocation windows.
    private static System.Numerics.BigInteger Reconstruct(in PropellantInteger value)
    {
        var result=System.Numerics.BigInteger.Zero;
        for(var i=PropellantInteger.LimbCount-1;i>=0;i--)result=(result<<64)+value.Limb(i);
        return result;
    }
    private static double Ratio(in PropellantInteger n,in PropellantInteger d)
    {
        if(!PoweredFlightNumerics.TryRatio(n,d,1,out var value))throw new InvalidOperationException("Ratio refused");
        return value.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Projected Map(in FixedInput x)
    {
        if(!PropellantInteger.TryMultiply(x.HpN,PropellantInteger.TicksPerSecond,out var ticks))throw new InvalidOperationException("Duration overflow");
        var duration=new PropellantDuration(ticks,x.HpD);
        if(!PropellantInteger.TryMultiply(duration.Numerator,x.HD,out var dutyNumerator)||
           !PoweredFlightNumerics.TryRatio(dutyNumerator,duration.Denominator,checked(PropellantInteger.TicksPerSecond*x.HN),out var duty))
            throw new InvalidOperationException("Duty refused");
        var mass=Ratio(x.MassN,x.MassD);
        var fx=x.FxSign*Ratio(x.FxN,x.FxD);var fy=x.FySign*Ratio(x.FyN,x.FyD);var e=x.ESign*Ratio(x.EN,x.ED);
        var inertia=Ratio(x.InertiaN,x.InertiaD);var gravity=Ratio(x.GravityN,x.GravityD);
        var hn=PropellantInteger.FromUInt64(x.HN);var hd=PropellantInteger.FromUInt64(x.HD);
        return new(duty.MultiplyDivide(fx,mass),duty.MultiplyDivide(fy,mass)-gravity,
            duty.MultiplyDivide(-e*fy,inertia),duty.MultiplyDivide(e*fx,inertia),1/mass,Ratio(hn,hd));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadControl(in FixedInput x)=>x.HpN.IsZero?0:1;

    private static bool SameBits(Projected a,Projected b)
    {
        static bool Same(double x,double y)=>BitConverter.DoubleToInt64Bits(x)==BitConverter.DoubleToInt64Bits(y)&&BitConverter.SingleToInt32Bits((float)x)==BitConverter.SingleToInt32Bits((float)y);
        return Same(a.Ax,b.Ax)&&Same(a.Ay,b.Ay)&&Same(a.Wx,b.Wx)&&Same(a.Wy,b.Wy)&&Same(a.InverseMass,b.InverseMass)&&Same(a.Dt,b.Dt);
    }

    private static int Main(string[] args)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(args[0]));
        var row=document.RootElement.GetProperty("cases").EnumerateArray().Single(r=>r.GetProperty("input").GetProperty("name").GetString()=="off-com").GetProperty("input");
        var oldInput=new ExactInput(row);var input=new FixedInput(oldInput);
        Span<long> oldScopes=stackalloc long[18];var old=Attribution.Trace(oldInput,oldScopes);var corrected=Map(input);
        var ledger=oldInput.Q*oldInput.Hp;
        var exactUnchanged=ledger.N==oldInput.Fuel.N&&ledger.D==oldInput.Fuel.D&&Reconstruct(input.HpN)==oldInput.Hp.N&&Reconstruct(input.HpD)==oldInput.Hp.D&&input.HN==oldInput.H.N&&input.HD==oldInput.H.D&&Reconstruct(input.MassN)==oldInput.Mass.N&&Reconstruct(input.MassD)==oldInput.Mass.D;
        var names=new[]{"ax","ay","wx","wy","inverseMass","H"};var oldValues=old.Values();var newValues=corrected.Values();
        var comparisons=names.Select((name,i)=>new{name,oldValue=oldValues[i],newValue=newValues[i],oldDoubleBits=BitConverter.DoubleToInt64Bits(oldValues[i]),newDoubleBits=BitConverter.DoubleToInt64Bits(newValues[i]),
            doubleEqual=BitConverter.DoubleToInt64Bits(oldValues[i])==BitConverter.DoubleToInt64Bits(newValues[i]),oldFloatBits=BitConverter.SingleToInt32Bits((float)oldValues[i]),newFloatBits=BitConverter.SingleToInt32Bits((float)newValues[i]),
            floatEqual=BitConverter.SingleToInt32Bits((float)oldValues[i])==BitConverter.SingleToInt32Bits((float)newValues[i])}).ToArray();
        if(!exactUnchanged||comparisons.Any(x=>!x.doubleEqual||!x.floatEqual))
        {
            File.WriteAllText(args[1],JsonSerializer.Serialize(new{result="STOP_SEMANTIC_MISMATCH_REVISE_FAILED",correctionClass="B: unchanged banked numerics reuse",runtime=Environment.Version.ToString(),
                exactLedgerUnchanged=exactUnchanged,comparisons,correctedWarmAllocation="NOT RUN: semantic hard stop",timingProcesses=0,solverCalls=0,secondCorrection=false},new JsonSerializerOptions{WriteIndented=true})+"\n");
            Console.WriteLine("STOP: corrected projections differ. No allocation qualification, second correction or timing.");return 3;
        }
        Prepared output=default;Projected complete=default;var deterministic=true;
        for(var i=0;i<128;i++){complete=Map(input);output=complete.Floats;deterministic&=SameBits(complete,old);}
        if(!deterministic)throw new InvalidOperationException("Warm double/float bits changed");
        OrdinaryAllocationMeasurement.PositiveControl();
        long readBytes;int read;
        _=ReadControl(input);
        using(var m=new OrdinaryAllocationMeasurement("corrected-immutable-read")){read=ReadControl(input);readBytes=m.Complete();}
        if(read!=1)throw new InvalidOperationException("Read failed");OrdinaryAllocationMeasurement.RequireZero(readBytes,"read");
        long[] single=new long[3];
        for(var i=0;i<3;i++)
        {
            using var m=new OrdinaryAllocationMeasurement("corrected-single");complete=Map(input);output=complete.Floats;single[i]=m.Complete();
            OrdinaryAllocationMeasurement.RequireZero(single[i],"corrected-single");
            if(!SameBits(complete,old))throw new InvalidOperationException("Repeated double/float bits changed");
        }
        long repeated;
        using(var m=new OrdinaryAllocationMeasurement("corrected-repeat128")){for(var i=0;i<128;i++){complete=Map(input);output=complete.Floats;deterministic&=SameBits(complete,old);}repeated=m.Complete();}
        OrdinaryAllocationMeasurement.RequireZero(repeated,"corrected-repeat128");
        if(!deterministic||!SameBits(complete,old))throw new InvalidOperationException("Repeated double/float bits changed");
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{result="SEMANTIC_ALLOCATION_GATE_PASS_REVIEW_REQUIRED",exactLedgerUnchanged=exactUnchanged,comparisons,readBytes,positiveControl=152,single,repeated,timingProcesses=0},new JsonSerializerOptions{WriteIndented=true})+"\n");
        return 0;
    }
}
