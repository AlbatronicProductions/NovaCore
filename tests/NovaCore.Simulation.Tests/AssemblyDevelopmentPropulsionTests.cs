using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Transactions;

internal static class AssemblyDevelopmentPropulsionTests
{
    private static readonly SpacecraftDefinition Vehicle=new(new(301),new(1),new(2),"SRV-01 provisional development qualification");
    private static void Check(bool x,string why){if(!x)throw new InvalidOperationException("DEVELOPMENT PROPULSION: "+why);}
    private static void Reject(Action a,string why)
    {try{a();}catch(InvalidDataException){return;}throw new InvalidOperationException("Accepted "+why);}
    private static CompiledAssemblyDesign Stock()=>AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn");
    private static CompiledAssemblyDesign Full()=>AssemblyDevelopmentPropulsion.LoadDefault().Apply(Stock());
    private static AssemblyCommand On(long ticks=15625)=>new(true,null,0,0,ticks);
    private static AssemblyApplicationSession Session(CompiledAssemblyDesign d,AssemblyCommand[] commands)=>
        AssemblyApplicationSession.Create(AssemblyLaunch.CreateDevelopmentQualification(d,Vehicle,"development-v1",commands),commands.Length);
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"observe");return o;}
    private static void Complete(AssemblyApplicationSession s)
    {
        var before=Observe(s);var ticks=s.Launch.Plan[before.State.Frontier].Request.Ticks;
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");
        var result=s.Engine.ServiceAssemblyFlightDebt(s.Authority);
        Check(result.PublishedCount==1&&result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"complete operation");
        var after=Observe(s);Check(after.HistoryCount==before.HistoryCount+1&&after.Clock.Debt.Ticks==0&&after.StateRevision.Value==before.StateRevision.Value+1,"atomic accounting");
    }
    private static BigInteger Big(PropellantInteger x)
    {BigInteger n=0;for(var i=PropellantInteger.LimbCount-1;i>=0;i--)n=(n<<64)+x.Limb(i);return n;}
    private static readonly BigInteger Unit=(BigInteger.One<<1074)*1_000_000;

    internal static void Cheap()
    {
        var stock=Stock();var profile=AssemblyDevelopmentPropulsion.LoadDefault();var d=profile.Apply(stock);
        Check(profile.Digest==AssemblyDevelopmentPropulsion.LoadDefault().Digest&&d.Digest==profile.Apply(Stock()).Digest,"deterministic identity");
        Check(d.Data.Design.Id==stock.Data.Design.Id&&d.Digest!=stock.Digest&&d.Development!.Data.Id=="novacore.SRV01.development-propulsion/1","vehicle versus configuration identity");
        Check(d.Parts.Length==7&&d.Jets.Length==16&&d.DryMass==630&&d.FirstMoment==stock.FirstMoment&&d.OriginInertia==stock.OriginInertia,"same physical topology/dry properties");
        for(var i=0;i<7;i++)
        {
            var a=stock.Parts[i];var b=d.Parts[i];
            Check(a.Instance.Id==b.Instance.Id&&a.Instance.Pose==b.Instance.Pose&&a.Com==b.Com&&a.InertiaAtOwnCom==b.InertiaAtOwnCom&&
                a.Definition.VisualReference==b.Definition.VisualReference&&a.Definition.LocalCom==b.Definition.LocalCom&&a.Definition.LocalInertia==b.Definition.LocalInertia,"unaltered geometry/part identity");
        }
        Check(stock.Main.Definition.Propulsion!.FullThrustN==600&&stock.Data.Design.InitialFuelKg==30&&stock.Data.Design.InitialOxidizerKg==45,"stock immutable");
        Check(d.Data.Design.Feeds.SequenceEqual(stock.Data.Design.Feeds)&&d.Jets.SequenceEqual(stock.Jets)&&d.Main.Definition.Gimbal==stock.Main.Definition.Gimbal,"feeds/gimbal/RCS owners unchanged");
        using var s=Session(d,Enumerable.Repeat(On(),8).ToArray());
        var initial=Observe(s);Check(initial.State.Mass.Mass==2018900&&Big(initial.State.Stores.Fuel)==807308*Unit&&Big(initial.State.Stores.Oxidizer)==1210962*Unit,"independent exact initial stores/wet mass");
        var f=AssemblyActuation.Main(d,0,0);Check(f.Force==new Double3(29708160,0,0)&&f.MomentAtOrigin==Double3.Zero,"physical axial realization");
        var maxVelocityError=0d;var maxPositionError=0d;
        for(var n=1;n<=8;n++)
        {
            Complete(s);var o=Observe(s);double h=n/64d;var mass=2018900-9670.625*h;
            var expectedV=3072*Math.Log(2018900/mass)-9.81*h;
            var expectedP=3072*(h-mass/9670.625*Math.Log(2018900/mass))-9.81*h*h/2;
            maxVelocityError=Math.Max(maxVelocityError,Math.Abs(o.State.Motion.VelocityO.Y-expectedV));
            maxPositionError=Math.Max(maxPositionError,Math.Abs(o.State.Motion.PositionO.Y-expectedP));
            Check(o.State.Motion.VelocityO.Y>0&&o.State.Mass.Mass==mass&&o.State.Actual.MainOn&&o.State.Actual.Feed==AssemblyFeedState.Available,"positive surface-gravity acceleration and changing mass");
            Check(Big(o.State.Stores.Fuel)==807308*Unit-Unit*15473*n/256&&Big(o.State.Stores.Oxidizer)==1210962*Unit-Unit*46419*n/512,"independent exact species withdrawal");
            Check(o.State.Epoch.Ticks==15625*n&&o.State.Frontier==n&&o.StateRevision.Value==(ulong)n&&o.TimelineRevision.Value==0&&o.HistoryCount==n,"canonical successor identity");
            Check(s.Engine.TryGetAssemblyHistory(s.Authority,n-1,out var r)&&r.Wrench==f&&Big(r.Powered.Numerator)*64==Big(r.Powered.Denominator)*1_000_000,"independent exact thrust/impulse duration");
        }
        Check(maxVelocityError<2e-10&&maxPositionError<2e-9,"closed-form rocket velocity/position oracle");
        // Independent scalar aggregation at dry/full and intermediate finite loads; do not use production matrix helpers.
        for(var k=0;k<=32;k++)
        {
            var mass=630+2018270*k/32d;var m=d.ObserveMass(mass);double dry=0,sx=0,sy=0,sz=0,ix=0,iy=0,iz=0;
            foreach(var part in stock.Parts)
            {
                var dm=part.Definition.DryMassKg;var p=part.Com;
                dry+=dm;sx+=dm*p.X;sy+=dm*p.Y;sz+=dm*p.Z;
                ix+=part.InertiaAtOwnCom.A+dm*(p.Y*p.Y+p.Z*p.Z);
                iy+=part.InertiaAtOwnCom.E+dm*(p.X*p.X+p.Z*p.Z);
                iz+=part.InertiaAtOwnCom.I+dm*(p.X*p.X+p.Y*p.Y);
            }
            Check(dry==630&&m.Com==new Double3(sx/mass,sy/mass,sz/mass)&&Math.Abs(m.Inertia.A-(ix-(sy*sy+sz*sz)/mass))<1e-10&&
                Math.Abs(m.Inertia.E-(iy-(sx*sx+sz*sz)/mass))<1e-10&&Math.Abs(m.Inertia.I-(iz-(sx*sx+sy*sy)/mass))<1e-10&&m.Inertia.PhysicalInertia,"mass/COM/full tensor oracle");
        }
        Check(AssemblyResources.Times(AssemblyResources.Mass(2018900),2).BitLength<=1116&&PropellantInteger.LimbCount*64==2176,"exact stage mass width");
        // Exact endpoint exhaustion at 1/64 second is a resource-kernel proof; its dry thrust
        // velocity exceeds this short launch slice, so canonical admission must reject it.
        var endpoint=profile.Apply(stock,15473d/256,46419d/512);
        var used=AssemblyResources.Calculate(endpoint,new(AssemblyResources.Mass(15473d/256),AssemblyResources.Mass(46419d/512)),AssemblyResources.Rate(1934.125),15625);
        Check(used.Classification==PropellantClassification.EndpointExhaustion&&used.After.Fuel.IsZero&&used.After.Oxidizer.IsZero&&used.Unpowered.IsZero,"endpoint exhaustion exact");
        Reject(()=>Session(endpoint,[On()]),"out-of-envelope high-acceleration dry-out trajectory");
        Exhaustion(profile.Apply(stock,1d/32,3d/64));
        Reject(()=>profile.Apply(stock,1,1),"unbalanced species");Reject(()=>profile.Apply(stock,-1,0),"negative store");
        Reject(()=>profile.Apply(stock,807310,1210965),"capacity excess");Reject(()=>profile.Apply(d),"profile stacking/refill");
        Reject(()=>profile.Apply(AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.G0B")),"foreign base");
        Reject(()=>new AssemblyLaunch(d,Vehicle,"implicit",new(default,default,DoubleQuaternion.Identity,default),default,[On()]),"implicit environment/profile admission");
        Reject(()=>Session(d,[On() with {Pair="+ROLL"}]),"unqualified development RCS combination");
        Reject(()=>Session(d,[On() with {GimbalTargetY=.05}]),"unqualified development gimbal combination");
        Reject(()=>Session(d,Enumerable.Repeat(On(),9).ToArray()),"horizon overrun");
        Reject(()=>s.Save(),"runtime/2 loses profile/environment");Reject(()=>d.Save(),"stock-only serialization");
        Check(Observe(s).State.Frontier==8&&s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status==AssemblyFlightStatus.Completed,"completion hold without refill");
        var dryMass=630d;var wetMass=2018900d;var dv=3072*Math.Log(wetMass/dryMass);
        Console.WriteLine("DEVELOPMENT_CHEAP "+JsonSerializer.Serialize(new{profile=profile.Data.Id,profileDigest=profile.Digest,configuration=d.Digest,dryMass,wetMass,
            fuelKg=807308,oxidizerKg=1210962,thrustN=29708160,totalFlowKgS=9670.625,tw=29708160/(wetMass*9.81),isp=3072/9.80665,deltaV=dv,burnSeconds=3229232d/15473,
            maxVelocityError,maxPositionError,finalVelocity=Observe(s).State.Motion.VelocityO.Y,finalPosition=Observe(s).State.Motion.PositionO.Y}));
    }

    private static void Exhaustion(CompiledAssemblyDesign d)
    {
        using var s=Session(d,[On(10),On(10),new(false,null,0,0,10)]);
        var m0=s.Launch.Initial.Mass.Mass;Complete(s);var a=Observe(s);
        Check(s.Engine.TryGetAssemblyHistory(s.Authority,0,out var r)&&r.Classification==PropellantClassification.InteriorExhaustion,"interior exhaustion through actual transaction");
        Check(a.State.Stores.Fuel.IsZero&&a.State.Stores.Oxidizer.IsZero&&a.State.Mass.Mass==630&&!a.State.Actual.MainOn&&a.State.Actual.Feed==AssemblyFeedState.Exhausted,"atomic dry/off successor");
        Check(Big(r.Powered.Numerator)*123784==Big(r.Powered.Denominator)*1_000_000,"independent rational exhaustion 1/123784 second");
        var v=3072*Math.Log(m0/630)-9.81*.00001;
        Check(Math.Abs(a.State.Motion.VelocityO.Y-v)<2e-10,"exhaustion includes gravity throughout powered and unpowered phases");
        Complete(s);var b=Observe(s);Check(!b.State.Actual.MainOn&&b.State.Stores==a.State.Stores&&Math.Abs(b.State.Motion.VelocityO.Y-(v-.0000981))<2e-10,"dry request produces only gravity");
        Complete(s);Check(Observe(s).State.Actual.Feed==AssemblyFeedState.NoDemand,"explicit shutdown");
    }

    internal static void Determinism()
    {
        static bool Number(double a,double b)=>BitConverter.DoubleToUInt64Bits(a)==BitConverter.DoubleToUInt64Bits(b);
        static bool Vector(Double3 a,Double3 b)=>Number(a.X,b.X)&&Number(a.Y,b.Y)&&Number(a.Z,b.Z);
        static bool Motion(AssemblyMotion a,AssemblyMotion b)=>Vector(a.PositionO,b.PositionO)&&Vector(a.VelocityO,b.VelocityO)&&Vector(a.AngularVelocityBody,b.AngularVelocityBody)&&
            Number(a.BodyToWorld.X,b.BodyToWorld.X)&&Number(a.BodyToWorld.Y,b.BodyToWorld.Y)&&Number(a.BodyToWorld.Z,b.BodyToWorld.Z)&&Number(a.BodyToWorld.W,b.BodyToWorld.W);
        var d=Full();var expected=new AssemblyRuntimeState[8];
        foreach(var partitions in new[]{1,3,8,19,30})
        {
            using var s=Session(d,Enumerable.Repeat(On(),8).ToArray());long elapsed=0;
            for(var n=1;n<=partitions;n++)
            {
                var target=125000L*n/partitions;
                Check(s.Engine.AdmitAssemblyHostTime(s.Authority,n,new(target-elapsed)).Status==AssemblyFlightStatus.AcceptedCredit,"partition credit");elapsed=target;
                var r=s.Engine.ServiceAssemblyFlightDebt(s.Authority);Check(r.PublishedCount<=4,"bounded service");
                while(r.Status==AssemblyFlightStatus.BudgetExhausted)r=s.Engine.ServiceAssemblyFlightDebt(s.Authority);
            }
            var o=Observe(s);Check(o.State.Epoch.Ticks==125000&&o.Clock.Debt.Ticks==0&&o.HistoryCount==8,"partition accounting");
            for(var i=0;i<8;i++)
            {
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out var r),"partition history");
                if(partitions==1)expected[i]=r.Successor;
                else Check(r.Successor==expected[i]&&Motion(r.Successor.Motion,expected[i].Motion),"identical physical bits at every matching frontier, including signed zero");
            }
        }
        Console.WriteLine("DEVELOPMENT_DETERMINISM PASS five host partitions / same exact stores and endpoint bits");
    }

    internal static void Allocation()
    {
        var full=Full();var partial=AssemblyDevelopmentPropulsion.LoadDefault().Apply(Stock(),1d/32,3d/64);
        var dry=AssemblyDevelopmentPropulsion.LoadDefault().Apply(Stock(),0,0);
        foreach(var d in new[]{full,partial,dry})
        {
            var label=d==full?"development-powered":d==partial?"development-exhaustion":"development-dry";
            var step=d==full?On():On(10);
            var warm=Enumerable.Range(0,128).Select(_=>Session(d,[step])).ToArray();
            var measured=Enumerable.Range(0,128).Select(_=>Session(d,[step])).ToArray();
            foreach(var s in warm)Complete(s);
            using(var gate=new OrdinaryAllocationMeasurement(label))
            {foreach(var s in measured)Complete(s);OrdinaryAllocationMeasurement.RequireZero(gate.Complete(),label);}
            foreach(var s in warm)s.Dispose();foreach(var s in measured)s.Dispose();
        }
        OrdinaryAllocationMeasurement.PositiveControl();
    }

    internal static void Performance()
    {
        var profile=AssemblyDevelopmentPropulsion.LoadDefault();var stock=Stock();
        foreach(var item in new[]{("wet-powered",profile.Apply(stock),15625L),("interior-exhaustion",profile.Apply(stock,1d/32,3d/64),10L),("dry",profile.Apply(stock,0,0),10L)})
        {
            var samples=new double[1024];var gc=new int[3];double cold=0;using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
            for(var i=-128;i<1024;i++)
            {
                var start=Stopwatch.GetTimestamp();using var s=Session(item.Item2,[On(item.Item3)]);if(i==-128)cold=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var a=GC.CollectionCount(0);var b=GC.CollectionCount(1);var c=GC.CollectionCount(2);
                start=Stopwatch.GetTimestamp();Complete(s);var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if(i>=0){samples[i]=ms;gc[0]+=GC.CollectionCount(0)-a;gc[1]+=GC.CollectionCount(1)-b;gc[2]+=GC.CollectionCount(2)-c;}
            }
            var sorted=(double[])samples.Clone();Array.Sort(sorted);
            Console.WriteLine("DEVELOPMENT_PERFORMANCE "+JsonSerializer.Serialize(new{population=item.Item1,warm=128,count=1024,median=(sorted[511]+sorted[512])/2,p95=sorted[972],p99=sorted[1013],maximum=sorted[^1],gc,cold,
                wholeHarnessCpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds,tails=samples.Select((ms,index)=>new{index,ms}).OrderByDescending(x=>x.ms).Take(8)}));
        }
    }
}
