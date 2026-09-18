using System.Diagnostics;
using System.Numerics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static class AssemblyPilotAllocationTests
{
    private static int checks;
    private static readonly AssemblyStockCatalog Catalog=AssemblyStockCatalog.LoadDefault();
    private static readonly CompiledAssemblyDesign Design=Catalog.Resolve("novacore.stock.SRV01.FourHorn");
    private static void Check(bool pass,string message){if(!pass)throw new InvalidOperationException("PILOT ALLOCATION: "+message);checks++;}
    private static AssemblyLaunch Launch(CompiledAssemblyDesign? d=null)=>new(d??Design,new(new(201),new(1),new(2),"SRV"),"allocation-proof",new(default,default,DoubleQuaternion.Identity,default),default,Enumerable.Repeat(new AssemblyCommand(false,null,0,0,15625),128).ToArray());
    private static AssemblyApplicationSession Create(CompiledAssemblyDesign? d=null)
    {var s=AssemblyApplicationSession.Create(Launch(d));s.EnableLiveControl(execution:AssemblyControlExecution.PhysicalActuators);return s;}
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var o)==AssemblyFlightStatus.Ready,"observation");return o;}
    private static void Admit(AssemblyApplicationSession s,AssemblyControlRequest r)
    {s.Engine.ObserveAssemblyControl(s.Control!,out var c);Check(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,c.AdmissionCount+1,r).Status==AssemblyControlStatus.Admitted,"admission");}
    private static AssemblyFlightRecord Next(AssemblyApplicationSession s,bool fragmented=false)
    {
        var before=Observe(s);
        void Credit(long ticks){s.Engine.ObserveAssemblyFlight(s.Authority,out var o);Check(s.Engine.AdmitAssemblyHostTime(s.Authority,o.HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"credit");}
        if(fragmented){Credit(625);Credit(15000);}else Credit(15625);
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed,"service");
        Check(s.Engine.TryGetAssemblyHistory(s.Authority,before.State.Frontier,out var record),"record");return record;
    }
    // Independent authored-nozzle oracle. No production pair lookup or allocator.
    private static ushort OracleMask(CompiledAssemblyDesign d,bool main,AssemblyPilotDemand p)
    {
        var nozzles=new HashSet<(string,string)>();
        void Add(string a,string b,string nozzle){nozzles.Add((a,nozzle));nozzles.Add((b,nozzle));}
        if(p.Roll>0)Add("rcs_01","rcs_03","left");else if(p.Roll<0)Add("rcs_02","rcs_04","right");
        if(!main)
        {
            if(p.Pitch>0)Add("rcs_01","rcs_02","bottom");else if(p.Pitch<0)Add("rcs_03","rcs_04","bottom");
            if(p.Yaw>0)Add("rcs_02","rcs_03","bottom");else if(p.Yaw<0)Add("rcs_01","rcs_04","bottom");
        }
        ushort mask=0;for(var j=0;j<d.Jets.Length;j++)if(nozzles.Contains((d.Jets[j].Instance.Id,d.Jets[j].Propulsion.Id)))mask|=(ushort)(1<<j);return mask;
    }
    private static AssemblyWrench OracleWrench(CompiledAssemblyDesign d,bool main,ushort mask,AssemblyGimbal held)
    {
        var force=Double3.Zero;var moment=Double3.Zero;
        if(main)
        {
            // Quaternion geometry independent of production's trigonometric matrix.
            var nozzle=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,held.ActualZ)*DoubleQuaternion.FromAxisAngle(Double3.UnitY,held.ActualY);
            var f=d.Main.Instance.Pose.Rotation.Apply(nozzle.Rotate(Double3.UnitX))*600;
            force+=f;moment+=Double3.Cross(d.Main.Instance.Pose.Point(d.Main.Definition.Gimbal!.Pivot),f);
        }
        for(var j=0;j<d.Jets.Length;j++)if((mask&(1<<j))!=0)
        {
            var jet=d.Jets[j];var point=jet.Instance.Pose.Position+jet.Instance.Pose.Rotation.Apply(jet.Propulsion.Point);
            var f=jet.Instance.Pose.Rotation.Apply(jet.Propulsion.Axis)*22.5;
            force+=f;moment+=Double3.Cross(point,f);
        }
        return new(force,moment);
    }
    private static void Near(Double3 a,Double3 b,string label)=>Check((a-b).LengthSquared<1e-18,label);
    internal static void Run()
    {
        var maximumTorque=0d;var maximumForce=0d;var maximumJets=0;
        foreach(var main in new[]{false,true})for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++)
        {
            var p=new AssemblyPilotDemand(pitch,yaw,roll);var mask=OracleMask(Design,main,p);var count=BitOperations.PopCount((uint)mask);
            using var s=Create();Admit(s,new(main,p));
            for(var step=0;step<34;step++)
            {
                var before=Observe(s).State;var r=Next(s);var after=r.Successor;
                Check(r.Command.JetMask==mask&&after.Actual.Jets==mask&&r.Command.Pair is null,"unique geometry-derived jets");
                var expected=OracleWrench(Design,main,mask,before.Gimbal);Near(r.Wrench.Force,expected.Force,"physical force oracle");Near(r.Wrench.MomentAtOrigin,expected.MomentAtOrigin,"physical moment oracle");
                Check(r.HeldGimbal==before.Gimbal,"interval holds source nozzle pose");
                var targetY=main?-pitch*.05:0;var targetZ=main?-yaw*.05:0;
                Check(after.Gimbal.TargetY==targetY&&after.Gimbal.TargetZ==targetZ&&Math.Abs(after.Gimbal.ActualY-before.Gimbal.ActualY)<=.0015625+1e-15&&Math.Abs(after.Gimbal.ActualZ-before.Gimbal.ActualZ)<=.0015625+1e-15,"per-axis limits and 0.1rad/s slew");
                var extent=(main?5d/128:0)+count*3d/2048;
                Check(AssemblyResources.Subtract(before.Stores.Fuel,after.Stores.Fuel)==AssemblyResources.Mass(extent*2/64)&&AssemblyResources.Subtract(before.Stores.Oxidizer,after.Stores.Oxidizer)==AssemblyResources.Mass(extent*3/64),"each unique nozzle charged exactly once");
                if(step==1)
                {
                    var delta=after.Motion.AngularVelocityBody-before.Motion.AngularVelocityBody;
                    Check((pitch==0||delta.Y*pitch>0)&&(yaw==0||delta.Z*yaw>0)&&(roll==0||delta.X*roll>0),"initial physical angular acceleration sign");
                }
                if(step==33)
                {
                    var tau=expected.MomentAtOrigin-Double3.Cross(before.Mass.Com,expected.Force);
                    Check((roll==0?Math.Abs(tau.X)<1e-9:tau.X*roll>0)&&(pitch==0?Math.Abs(tau.Y)<1e-9:tau.Y*pitch>0)&&(yaw==0?Math.Abs(tau.Z)<1e-9:tau.Z*yaw>0),"all demand signs and absent unintended COM torque");
                    if(!main){maximumTorque=Math.Max(maximumTorque,Math.Sqrt(tau.LengthSquared));maximumForce=Math.Max(maximumForce,Math.Sqrt(expected.Force.LengthSquared));maximumJets=Math.Max(maximumJets,count);}
                    if(!main&&pitch!=0&&yaw!=0)Check(Math.Abs(Math.Abs(tau.Y)-12.839291379394737)<1e-10&&Math.Abs(Math.Abs(tau.Z)-12.839291379394737)<1e-10,"shared-jet saturation halves transverse torque, no hidden normalization");
                }
            }
        }
        Check(maximumJets==5&&maximumForce<=67.5+1e-10&&maximumTorque<=44.47661000458556+1e-10,"all OFF unions inside existing global bounds");
        Transitions();Depletion();Refusal();Measure();
        Console.WriteLine($"PILOT_ALLOCATION PASS checks={checks} off_max_jets={maximumJets} off_max_force={maximumForce:R} off_max_torque={maximumTorque:R}");
    }
    private static AssemblyControlRequest Sequence(int i)=>i switch
    {<32=>new(true,new(1,1,1)),<64=>new(true,new(-1,-1,-1)),<96=>new(false,new(1,1,1)),_=>new(true,default)};
    private static void Transitions()
    {
        using var a=Create();using var b=Create();
        for(var i=0;i<128;i++)
        {
            var request=Sequence(i);if(i%32==0){Admit(a,request);Admit(b,request);}
            var before=Observe(a).State;var ar=Next(a);var br=Next(b,true);
            Check(ar==br&&Observe(a).State==Observe(b).State,"physical replay host fragmentation");
            Check(ar.Command.JetMask==OracleMask(Design,request.MainOn,request.Pilot),"current admitted ON/OFF discriminator even at first ignition and cutoff");
            var expected=OracleWrench(Design,request.MainOn,ar.Command.JetMask,before.Gimbal);Near(ar.Wrench.MomentAtOrigin,expected.MomentAtOrigin,"reversal/cutoff source-held wrench");
            if(i==32)Check(ar.Successor.Gimbal.ActualY<0&&ar.Successor.Gimbal.TargetY>0&&ar.Wrench.MomentAtOrigin.Y>0,"reversal does not teleport nozzle or promise instantaneous torque reversal");
            if(i==64)Check(ar.Successor.Actual.Jets==OracleMask(Design,false,request.Pilot)&&!ar.Successor.Actual.MainOn,"engine OFF retains physical attitude availability");
            if(i==96)Check(ar.Successor.Gimbal.TargetY==0&&ar.Successor.Gimbal.TargetZ==0&&ar.Successor.Actual.Jets==0&&ar.Successor.Motion.AngularVelocityBody!=default,"release clears requests without erasing angular momentum");
            if(i==47)
            {
                var bytes=a.Save();var data=AssemblyJson.Read<AssemblySaveData>(bytes,1_048_576);
                Check(data.Schema=="novacore.assembly-runtime/5"&&data.Live!.Execution==AssemblyControlExecution.PhysicalActuators,"versioned allocator execution");
                using var restored=AssemblyApplicationSession.Restore(Catalog,bytes);Check(restored.Save().AsSpan().SequenceEqual(bytes),"allocated midflight exact save replay");
            }
        }
        using var replay=AssemblyApplicationSession.Restore(Catalog,a.Save());Check(replay.Save().AsSpan().SequenceEqual(a.Save()),"terminal allocated replay");
    }
    private static void Depletion()
    {
        foreach(var amount in new[]{0d,25d/2048})foreach(var main in new[]{false,true})
        {
            var d=CompiledAssemblyDesign.Compile(AssemblyJson.Write(Design.Data with {Design=Design.Data.Design with {InitialFuelKg=amount*2/5,InitialOxidizerKg=amount*3/5}}));
            using var s=Create(d);Admit(s,new(main,new(1,1,1)));
            for(var i=0;i<128;i++)
            {
                var before=Observe(s).State;var r=Next(s);
                if(before.Stores.Fuel.IsZero)Check(r.Wrench==default&&r.Powered.IsZero&&r.Successor.Stores==default&&r.Successor.Actual.Jets==0&&!r.Successor.Actual.MainOn&&r.Successor.ResourceRevision==before.ResourceRevision,"no-feed never bypasses resource authority");
                Check(AssemblyResources.Matched(r.Successor.Stores),"overlapping demand no double debit or unmatched species");
            }
            Check(Observe(s).State.Stores==default,"combined demand depletes exact finite supply");
            var catalog=new AssemblyStockCatalog([(d.Save(),d.Digest)]);using var r2=AssemblyApplicationSession.Restore(catalog,s.Save());Check(r2.Save().AsSpan().SequenceEqual(s.Save()),"depleted replay");
        }
    }
    private static void Refusal()
    {
        using var legacy=AssemblyApplicationSession.Create(Launch(Catalog.Resolve("novacore.stock.SRV01.G0B")));
        Check(legacy.Engine.BeginAssemblyControl(legacy.Authority,256,out var rejected,AssemblyControlExecution.PhysicalActuators)==AssemblyControlStatus.InvalidInput&&rejected is null,"legacy geometry physical allocation refused atomically");
        legacy.EnableLiveControl();Check(legacy.Control is not null,"cold refusal leaves owner available for supported mode");
        // Original individual-pair proof admits this smaller main, but its
        // main-plus-largest-pair budget does not include simultaneous RCS rows.
        var definitions=Design.Data.Definitions.Select(p=>p.Role==AssemblyRole.MainEngine?p with {Propulsion=p.Propulsion! with {FullThrustN=15,ExtentRateKgS=1d/1024}}:p).ToImmutableArray();
        var altered=Design.Data with {Definitions=definitions,Design=Design.Data.Design with {Instances=Design.Data.Design.Instances.Select(p=>p with {Definition=p.Definition with {Digest=AssemblyJson.Digest(definitions.Single(d=>d.Id==p.Definition.Id))}}).ToImmutableArray()}};
        var admitted=CompiledAssemblyDesign.Compile(AssemblyJson.Write(altered));
        using var weak=AssemblyApplicationSession.Create(Launch(admitted));
        Check(weak.Engine.BeginAssemblyControl(weak.Authority,256,out var unsupported,AssemblyControlExecution.PhysicalActuators)==AssemblyControlStatus.InvalidInput&&unsupported is null,"numeric cold proof rejects unqualified custom combined torque");
        weak.EnableLiveControl();Check(weak.Control is not null,"numeric cold refusal publishes no partial capability");
        using var s=Create();Admit(s,new(false,new(1,1,1)));
        var saved=AssemblyJson.Read<AssemblySaveData>(s.Save(),1_048_576);
        void Reject(AssemblySaveData data){try{AssemblyApplicationSession.Restore(Catalog,AssemblyJson.Write(data));}catch(InvalidDataException){checks++;return;}throw new InvalidOperationException("execution downgrade accepted");}
        Reject(saved with {Schema="novacore.assembly-runtime/4"});Reject(saved with {Live=saved.Live! with {Execution=AssemblyControlExecution.DemandOnly}});
        try{_ = AssemblyLaunch.CompileCommand(Design,new(false,null,0,0,15625,ushort.MaxValue));throw new InvalidOperationException("arbitrary mask accepted");}catch(InvalidDataException){checks++;}
    }
    private static void Measure()
    {
        void Work(AssemblyApplicationSession s,int i)
        {
            Admit(s,Sequence(i));Next(s);
        }
        using(var warm=Create())for(var i=0;i<128;i++)Work(warm,i);
        var timed=Enumerable.Range(0,8).Select(_=>Create()).ToArray();var isolated=Enumerable.Range(0,8).Select(_=>Create()).ToArray();
        var samples=new double[1024];var index=0;var before=GC.GetAllocatedBytesForCurrentThread();
        var gc0=GC.CollectionCount(0);var gc1=GC.CollectionCount(1);var gc2=GC.CollectionCount(2);
        foreach(var s in timed)for(var i=0;i<128;i++){var start=Stopwatch.GetTimestamp();Work(s,i);samples[index++]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;}
        var raw=GC.GetAllocatedBytesForCurrentThread()-before;var collections=new[]{GC.CollectionCount(0)-gc0,GC.CollectionCount(1)-gc1,GC.CollectionCount(2)-gc2};
        using var region=new OrdinaryAllocationMeasurement("pilot-allocation-service");
        foreach(var s in isolated)for(var i=0;i<128;i++)Work(s,i);
        var bytes=region.Complete();Check(bytes==0,"warmed admitted allocation/resource/dynamics/publication zero allocation");
        Array.Sort(samples);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {stage=4,samples=index,medianMs=samples[511],p95Ms=samples[972],p99Ms=samples[1013],maxMs=samples[^1],rawCounterBytes=raw,collections,isolatedBytes=bytes,tableRows=54,tablePayloadBytes=54*Unsafe.SizeOf<CompiledAssemblyCommand>()}));
        foreach(var s in timed)s.Dispose();foreach(var s in isolated)s.Dispose();OrdinaryAllocationMeasurement.PositiveControl();
    }
}
