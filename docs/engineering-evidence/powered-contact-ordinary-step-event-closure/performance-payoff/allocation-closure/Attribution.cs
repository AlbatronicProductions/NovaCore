using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;

internal readonly record struct Projected(double Ax, double Ay, double Wx, double Wy, double InverseMass, double Dt)
{
    internal Prepared Floats => new((float)Ax, (float)Ay, (float)Wx, (float)Wy, (float)InverseMass, (float)Dt);
    internal double[] Values() => [Ax, Ay, Wx, Wy, InverseMass, Dt];
}

internal static class Attribution
{
    internal static readonly string[] Names = ["hp/H", "Fx*duty", "Fy*duty", "-1*e", "(-e)*effectiveFy", "e*effectiveFx",
        "effectiveFx/mass", "effectiveFy/mass", "ay-gravity", "torqueX/inertia", "torqueY/inertia",
        "ax.Value", "ay.Value", "wx.Value", "wy.Value", "1/mass", "inverseMass.Value", "H.Value"];

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Projected Trace(ExactInput x, Span<long> bytes)
    {
        long a;
        a=GC.GetAllocatedBytesForCurrentThread();var duty=x.Hp/x.H;bytes[0]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var fx=x.Fx*duty;bytes[1]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var fy=x.Fy*duty;bytes[2]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var negE=x.MinusOne*x.E;bytes[3]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var tx=negE*fy;bytes[4]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var ty=x.E*fx;bytes[5]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var ax=fx/x.Mass;bytes[6]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var fyMass=fy/x.Mass;bytes[7]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var ay=fyMass-x.Gravity;bytes[8]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var wx=tx/x.Inertia;bytes[9]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var wy=ty/x.Inertia;bytes[10]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var dax=ax.Value;bytes[11]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var day=ay.Value;bytes[12]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var dwx=wx.Value;bytes[13]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var dwy=wy.Value;bytes[14]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var inv=x.One/x.Mass;bytes[15]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var di=inv.Value;bytes[16]=GC.GetAllocatedBytesForCurrentThread()-a;
        a=GC.GetAllocatedBytesForCurrentThread();var dh=x.H.Value;bytes[17]=GC.GetAllocatedBytesForCurrentThread()-a;
        return new(dax,day,dwx,dwy,di,dh);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int EmptyScopes(ExactInput x, Span<long> bytes)
    {
        var sum=0;
        for(var i=0;i<18;i++){var a=GC.GetAllocatedBytesForCurrentThread();sum+=x.Hp.N.Sign;bytes[i]=GC.GetAllocatedBytesForCurrentThread()-a;}
        return sum;
    }

    // Cold IL inspection of the ACTUAL loaded runtime, not a version-assumed web source.
    private static object InspectRuntime()
    {
        var opcodes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(x=>(ushort)x.Value);
        var visited=new HashSet<MethodBase>();var queue=new Queue<MethodBase>();var allocations=new List<object>();
        var methods=typeof(BigInteger).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);
        foreach(var m in methods.Where(m=>m.Name is "op_Multiply" or "op_Division" or "op_Subtraction" or "op_RightShift" or "GreatestCommonDivisor"))queue.Enqueue(m);
        foreach(var c in typeof(BigInteger).GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))queue.Enqueue(c);
        while(queue.TryDequeue(out var m))
        {
            if(!visited.Add(m))continue;
            var il=m.GetMethodBody()?.GetILAsByteArray();if(il is null)continue;
            for(var p=0;p<il.Length;)
            {
                var offset=p;ushort key=il[p++];if(key==0xFE)key=(ushort)(0xFE00|il[p++]);var op=opcodes[key];var size=op.OperandType switch
                {OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
                 OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(il,p),_=>4};
                if(op.OperandType is OperandType.InlineMethod or OperandType.InlineType)
                {
                    try
                    {
                        var member=m.Module.ResolveMember(BitConverter.ToInt32(il,p),m.DeclaringType?.GetGenericArguments(),m.IsGenericMethod?((MethodInfo)m).GetGenericArguments():null);
                        if(op==OpCodes.Newarr)allocations.Add(new{method=$"{m.DeclaringType?.FullName}.{m}",offset,operation="newarr",type=member?.ToString()});
                        if(member is MethodBase called && called.DeclaringType==typeof(BigInteger))queue.Enqueue(called);
                        if(member is MethodBase arrayCall && arrayCall.Name=="ToArray")allocations.Add(new{method=$"{m.DeclaringType?.FullName}.{m}",offset,operation="call",type=arrayCall.ToString()});
                    }
                    catch(ArgumentException){ }
                }
                p+=size;
            }
        }
        return new{assembly=typeof(BigInteger).Assembly.Location,version=typeof(BigInteger).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            backingFields=typeof(BigInteger).GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Select(f=>new{name=f.Name,type=f.FieldType.FullName}).ToArray(),
            allocationSites=allocations,scope="Static actual-runtime IL sites; not branch-hit/object-count tracing"};
    }

    private static int Main(string[] args)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(args[0]));
        var row=document.RootElement.GetProperty("cases").EnumerateArray().Single(x=>x.GetProperty("input").GetProperty("name").GetString()=="off-com").GetProperty("input");
        var input=new ExactInput(row);Span<long> scopes=stackalloc long[18];
        Prepared original=default;Projected projected=default;
        for(var i=0;i<128;i++){original=RetainedMapper.Map(input);projected=Trace(input,scopes);_ = EmptyScopes(input,scopes);}
        if(original!=projected.Floats)throw new InvalidOperationException("Trace changed original outputs");
        OrdinaryAllocationMeasurement.PositiveControl();
        long emptyBytes,wholeBytes,traceBytes;
        int control;
        using(var m=new OrdinaryAllocationMeasurement("attribution-empty-scopes")){control=EmptyScopes(input,scopes);emptyBytes=m.Complete();}
        if(emptyBytes!=0||control!=18)throw new InvalidOperationException("Counter/control perturbation");
        using(var m=new OrdinaryAllocationMeasurement("attribution-original-map")){original=RetainedMapper.Map(input);wholeBytes=m.Complete();}
        using(var m=new OrdinaryAllocationMeasurement("attribution-scoped-map")){projected=Trace(input,scopes);traceBytes=m.Complete();}
        return Finish(args[1],original,projected,scopes,emptyBytes,wholeBytes,traceBytes);
    }

    private static int Finish(string output,Prepared original,Projected projected,ReadOnlySpan<long> scopes,long empty,long whole,long traced)
    {
        var copied=scopes.ToArray();var sum=copied.Sum();
        if(whole!=1936||traced!=whole||sum!=whole||original!=projected.Floats)throw new InvalidOperationException("Attribution failed reconciliation");
        var result=new{result="CAUSE_SCOPES_RECONCILED",runtime=Environment.Version.ToString(),thread=Environment.CurrentManagedThreadId,warmCallsEach=128,
            positiveControlBytes=152,emptyScopeBytes=empty,originalMapBytes=whole,scopedMapBytes=traced,scopeSumBytes=sum,
            scopes=Names.Select((name,i)=>new{scope=name,callsPerMap=1,bytes=copied[i]}).ToArray(),
            binary64Values=projected.Values(),binary64Bits=projected.Values().Select(BitConverter.DoubleToInt64Bits).ToArray(),
            fp32Bits=projected.Values().Select(x=>BitConverter.SingleToInt32Bits((float)x)).ToArray(),runtimeInspection=InspectRuntime()};
        File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine($"ATTRIBUTION reconciled={sum} original={whole} trace={traced} control={empty}");return 0;
    }
}
