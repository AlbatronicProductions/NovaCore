// Single oracle-free diagnostic residual-removal witness. No production correction or installation.
using System.Reflection;
using System.Numerics;
using System.Text.Json;
using V=PieceKernel.V;
using S=PieceKernel.State;
using P=PieceKernel.Patch;

internal static class CoastPayoff
{
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true,IncludeFields=true};
    static V Vec(JsonElement x)=>new(x.GetProperty("X").GetDouble(),x.GetProperty("Y").GetDouble(),x.GetProperty("Z").GetDouble());
    static S State(JsonElement x)=>new(Vec(x.GetProperty("Linear")),Vec(x.GetProperty("Angular")));
    static double[] Values(JsonElement x)=>x.EnumerateArray().Select(x=>x.GetDouble()).ToArray();
    static bool Bits(double[] a,double[] b)=>a.Zip(b,(x,y)=>BitConverter.DoubleToUInt64Bits(x)==BitConverter.DoubleToUInt64Bits(y)).All(x=>x);
    static T Call<T>(string method,params object[] args)=>(T)typeof(CoastAccuracy).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
    static void Check(bool b,string why){if(!b)throw new InvalidOperationException(why);}
    // Critically this API has no oracle/reference parameter. Current equation residual alone supplies delta.
    internal static double[] RemoveCommonResidual(P patch,S free,double omega,double h,double[] initial,out double delta,out double numerator,out double denominator)
    {
        var a=omega*h;double alpha=1/(a*(a+2));numerator=0;denominator=0;
        for(int i=0;i<4;i++)
        {
            double rhs=Math.Min(patch.Depth[i]/h,Math.Min(patch.Depth[i]/(h+2/omega),2))-patch.J(i,free);
            double residual=-rhs;for(int j=0;j<7;j++)
            {double coefficient=patch.K[i,j]+(i==j?alpha*patch.K[i,i]:0);residual+=coefficient*initial[j];if(j<4)denominator+=coefficient;}
            numerator+=residual;
        }
        Check(double.IsFinite(denominator)&&denominator>0&&double.IsFinite(numerator),"diagnostic common equation valid");
        delta=-numerator/denominator;var result=(double[])initial.Clone();for(int i=0;i<4;i++)result[i]+=delta;
        Check(result.Take(4).All(x=>x>0&&double.IsFinite(x)),"diagnostic positive normals, no clamp");
        double cap=.125*result.Take(4).Sum(),twist=0;for(int i=0;i<4;i++)twist+=.125*result[i]*patch.Radii[i];
        Check(Math.Sqrt(result[4]*result[4]+result[5]*result[5])<cap&&Math.Abs(result[6])<twist,"diagnostic current friction caps, no clamp");return result;
    }
    static CoastAccuracy.System System(P p,S free,double h,double omega,double[] guess)
    {
        var equation=Call<(double[,] A,double[] Rhs)>("Equation",p,free,omega,h);
        var reference=PieceKernel.Reference(p,free,omega,h);Check(reference.Admissible&&reference.Residual<=1e-12,"unchanged reference applicable");
        return new(p,free,omega,h,guess,equation.A,equation.Rhs,reference,new{h,omega});
    }
    static object Run(string name,P p,S free,double h,double omega,double[] initial,JsonElement original,bool replay)
    {
        // D is constructed before reference evaluation and cannot read its answer.
        var diagnostic=RemoveCommonResidual(p,free,omega,h,initial,out var delta,out var numerator,out var denominator);
        var s=System(p,free,h,omega,initial);
        object? baseline=null;if(replay)
        {
            var b=Call<CoastAccuracy.Observation>("Observe",s,initial,8);Check(Bits(b.Impulses,Values(original.GetProperty("solved").GetProperty("Impulses"))),"synthetic exact replay");baseline=b;
        }
        var d=Call<CoastAccuracy.Observation>("Observe",s,diagnostic,8);
        var signed=diagnostic.Zip(initial,(a,b)=>a-b).ToArray();
        return new{name,baseline,delta,numerator,denominator,initial,diagnostic,change=signed,
            residualBefore=Call<double[]>("Residual",s,initial),residualAfter=Call<double[]>("Residual",s,diagnostic),
            result=d,reference=s.Reference,referenceNotUsedToConstructD=true};
    }
    public static void Main(string[] args)
    {
        using var convergence=JsonDocument.Parse(File.ReadAllText(args[0]));using var synthetic=JsonDocument.Parse(File.ReadAllText(args[1]));
        var coast=convergence.RootElement.GetProperty("coast");var input=coast.GetProperty("inputs");
        var cap=new Capture{Count=4,Omega=(float)input.GetProperty("omega").GetDouble(),TwiceDamping=2,Friction=.5f,Recovery=2};
        var n=Vec(input.GetProperty("normal"));for(int i=0;i<4;i++)cap.Contacts[i]=new(){Normal=new((float)n.X,(float)n.Y,(float)n.Z),
            Offset=new((float)Vec(input.GetProperty("offsets")[i]).X,(float)Vec(input.GetProperty("offsets")[i]).Y,(float)Vec(input.GetProperty("offsets")[i]).Z),Depth=(float)input.GetProperty("depths")[i].GetDouble()};
        var p=new P(cap,input.GetProperty("Mass").GetDouble());double h=input.GetProperty("h").GetDouble(),omega=input.GetProperty("omega").GetDouble();
        var source=State(input.GetProperty("Source"));var free=new S(source.Linear+h*new V(0,-9.81,0),source.Angular);
        var initial=Values(input.GetProperty("guess"));var report=Run("original-coast",p,free,h,omega,initial,default,false);
        var results=new List<object>();
        foreach(var item in synthetic.RootElement.GetProperty("cases").EnumerateArray())
        {
            if(item.GetProperty("transport").GetProperty("Status").GetString()!="Ready")continue;
            string name=item.GetProperty("Name").GetString()!;Vector3 normal=Vector3.UnitY;double angle=0;V shift=default;
            if(name is "tiny-normal" or "moderate-normal" or "normal-and-tangent")
            {double rotation=name=="tiny-normal"?1e-8:.08;normal=new(0,(float)Math.Cos(rotation),(float)Math.Sin(rotation));}
            if(name is "tangent-only" or "normal-and-tangent")angle=.7;if(name=="lever-only")shift=new(0,-.0001,0);
            var sp=(P)typeof(BasisProbe).GetMethod("Synthetic",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{normal,angle,shift,false})!;
            double sh=17707d/2000000;var sf=new S((-9.81*sh)*sp.Linear[0],default);
            results.Add(Run(name,sp,sf,sh,omega,Values(item.GetProperty("transport").GetProperty("Cache")),item.GetProperty("eightSweep"),true));
        }
        File.WriteAllText(args[2],JsonSerializer.Serialize(new{coast=report,synthetics=results,existingA=coast.GetProperty("result"),
            existingBCold=convergence.RootElement.GetProperty("cold"),existingCReference=convergence.RootElement.GetProperty("referenceInitialized"),
            noProductionCorrection=true,noCacheInstall=true,noWorldStep=true},Json)+Environment.NewLine);
        Console.WriteLine("One coast D and six same-policy synthetic D witnesses recorded; no adoption/installation.");
    }
}
