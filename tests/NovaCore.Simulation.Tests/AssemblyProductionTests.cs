using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

// Permanent production-path checks. The only independent BigInteger work below
// is a test oracle; resource authority stays in the production fixed-width type.
internal static class AssemblyProductionTests
{
    private static int _checks;
    private static readonly AssemblyMotion Rest=new(Double3.Zero,Double3.Zero,DoubleQuaternion.Identity,Double3.Zero);
    private static readonly AssemblyMotion Moving=new(Double3.Zero,new(.15,-.2,0),DoubleQuaternion.Identity,new(.03,-.04,0));
    private static void Check(bool condition,string label)
    {if(!condition)throw new InvalidOperationException("ASSEMBLY PRODUCTION: "+label);_checks++;}
    private static void Reject(Action action,string label)
    {
        try{action();}catch(Exception e) when(e is InvalidDataException or InvalidOperationException or JsonException or OverflowException or ArgumentException)
        {_checks++;return;}
        throw new InvalidOperationException("ASSEMBLY ACCEPTED MUTATION: "+label);
    }
    private static bool Near(double a,double b,double tolerance=1e-10)=>Math.Abs(a-b)<=tolerance;
    private static double Norm(Double3 a)=>Math.Sqrt(a.LengthSquared);
    private static BigInteger Big(PropellantInteger value)
    {BigInteger n=0;for(var i=PropellantInteger.LimbCount-1;i>=0;i--)n=(n<<64)+value.Limb(i);return n;}
    private static CatalogDesignData Resolve(CatalogDesignData raw)
    {
        // Explicit test fixture authoring, never a runtime digest repair.
        var defs=raw.Definitions.Select(d=>d with {Attachments=d.Attachments.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),Stores=d.Stores.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray();
        return raw with {Definitions=defs,Design=raw.Design with {Instances=raw.Design.Instances.Select(p=>p with {Definition=p.Definition with {Digest=AssemblyJson.Digest(defs.Single(d=>d.Id==p.Definition.Id))}}).ToImmutableArray()}};
    }
    private static CompiledAssemblyDesign Load(CatalogDesignData data)=>CompiledAssemblyDesign.Compile(AssemblyJson.Write(data));
    private static CatalogDesignData ChangeDef(CatalogDesignData d,AssemblyRole role,Func<PartDefinitionData,PartDefinitionData> change)=>Resolve(d with {Definitions=d.Definitions.Select(p=>p.Role==role?change(p):p).ToImmutableArray()});
    private static CompiledAssemblyDesign LoadAmount(CompiledAssemblyDesign d,double total)=>Load(d.Data with {Design=d.Data.Design with {InitialFuelKg=total*2/5,InitialOxidizerKg=total*3/5}});
    private static AssemblyStockCatalog CatalogOf(CompiledAssemblyDesign d)=>new([(d.Save(),d.Digest)]);
    private static AssemblyLaunch Launch(CompiledAssemblyDesign d,AssemblyCommand[] commands,AssemblyMotion? motion=null,AssemblyGimbal g=default,string id="production")=>
        new(d,new(new(17),new(1),new(2),"Assembly test article"),id,motion??Rest,g,commands);
    private static AssemblyApplicationSession Session(CompiledAssemblyDesign d,AssemblyCommand[] commands,AssemblyMotion? motion=null,AssemblyGimbal g=default,string id="production",int capacity=128)=>
        AssemblyApplicationSession.Create(Launch(d,commands,motion,g,id),capacity);
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {var status=s.Engine.ObserveAssemblyFlight(s.Authority,out var value);Check(status is AssemblyFlightStatus.Ready or AssemblyFlightStatus.Invalidated,"copied canonical observation");return value;}
    private static void Credit(AssemblyApplicationSession s,long ticks)
    {var v=Observe(s);Check(s.Engine.AdmitAssemblyHostTime(s.Authority,v.HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"normal exact host credit");}
    private static AssemblyRuntimeState Next(AssemblyApplicationSession s)
    {
        var v=Observe(s);Credit(s,s.Launch.Plan[v.State.Frontier].Request.Ticks);
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var p)==AssemblyFlightStatus.Prepared,"production preparation");
        Check(s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.Published,"production publication");
        return Observe(s).State;
    }
    private static AssemblyRuntimeState One(CompiledAssemblyDesign d,AssemblyCommand c,AssemblyMotion? m=null,AssemblyGimbal g=default)=>Next(Session(d,[c],m,g));
    private static AssemblyFlightRecord Record(AssemblyApplicationSession s,int i)
    {Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out var r),"canonical history record");return r;}
    private static (BigInteger N,BigInteger D) TotalPowered(AssemblyApplicationSession s)
    {
        BigInteger n=0,d=1;for(var i=0;i<Observe(s).HistoryCount;i++)
        {var r=Record(s,i);var rn=Big(r.Powered.Numerator);var rd=Big(r.Powered.Denominator)*1_000_000;n=n*rd+rn*d;d*=rd;var g=BigInteger.GreatestCommonDivisor(n,d);n/=g;d/=g;}
        return(n,d);
    }
    private static void Catalog(CompiledAssemblyDesign d)
    {
        Check(d.Data.Definitions.Length==4&&d.Parts.Length==7&&d.Jets.Length==4,"G1-3 four definitions/seven ordinary instances");
        Check(d.Jets.Select(p=>p.Definition.Id).Distinct().Count()==1&&d.Jets.Select(p=>p.Instance.Id).Distinct().Count()==4,"one reusable jet definition/four distinct placements");
        Check(d.Data.Design.Attachments.Length==6&&d.Data.Design.Feeds.Length==10,"explicit structural tree and ten species feeds");
        Check(d.Data.Definitions.Count(p=>p.Gimbal is not null)==1&&d.Tank.Definition.Stores.Length==2,"one actuator/two separately identified stores");
        Check(d.Save().SequenceEqual(CompiledAssemblyDesign.Compile(d.Save()).Save()),"G4 canonical design save/reload/resave bytes");
        var shuffled=d.Data with {Definitions=d.Data.Definitions.Reverse().ToImmutableArray(),Design=d.Data.Design with {Instances=d.Data.Design.Instances.Reverse().ToImmutableArray(),Attachments=d.Data.Design.Attachments.Reverse().ToImmutableArray(),Feeds=d.Data.Design.Feeds.Reverse().ToImmutableArray(),Pairs=d.Data.Design.Pairs.Reverse().ToImmutableArray()}};
        Check(Load(shuffled).Digest==d.Digest,"input enumeration does not alter canonical identity");
        // Author-time symmetry witness: sign/permutation operations on one
        // saved quarter-ring frame, with alternating cant, create ordinary
        // instances. There is no symmetry operation in the runtime schema.
        var seed=d.Jets[0].Instance;var r=seed.Pose.Rotation;var b=seed.Pose.Position.Y;
        var rotations=new[]{r,new Matrix3(r.A,0,-r.C,-r.D,-r.E,r.F,r.G,r.H,-r.I),
            new Matrix3(r.A,0,r.C,-r.D,-r.E,-r.F,-r.G,-r.H,-r.I),
            new Matrix3(r.A,0,-r.C,r.D,r.E,-r.F,-r.G,-r.H,r.I)};
        var signs=new[]{(1,1),(-1,1),(-1,-1),(1,-1)};
        var expanded=d.Parts.Take(3).Select(p=>p.Instance).Concat(Enumerable.Range(0,4).Select(i=>seed with {
            Id="rcs_"+(i+1).ToString("D2"),Order=i+3,Pose=new(new(0,signs[i].Item1*b,signs[i].Item2*b),rotations[i])})).ToImmutableArray();
        Check(Load(d.Data with {Design=d.Data.Design with {Instances=expanded}}).Digest==d.Digest,"explicit versus pre-save symmetry expansion identity");
        var baseline=d.Save();var json=Encoding.UTF8.GetString(baseline);
        Reject(()=>CompiledAssemblyDesign.Compile(Encoding.UTF8.GetBytes(json.Replace("\"schema\":","\"arbitraryPhysics\":0,\"schema\":",StringComparison.Ordinal))),"arbitrary metadata");
        Reject(()=>CompiledAssemblyDesign.Compile(Encoding.UTF8.GetBytes(json.Replace("\"schema\":","\"schema\":\"x\",\"schema\":",StringComparison.Ordinal))),"duplicate JSON field");
        Reject(()=>CompiledAssemblyDesign.Compile(Encoding.UTF8.GetBytes(json.Replace("\"profile\":\"assembled-point-stores/1\",","",StringComparison.Ordinal))),"missing required profile");
        Check(CompiledAssemblyDesign.Compile(Encoding.UTF8.GetBytes(json.Replace("\"x\":0,","\"x\":-0.0,",StringComparison.Ordinal))).Digest==d.Digest,"negative-zero normalization");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {Instances=d.Data.Design.Instances.SetItem(6,d.Data.Design.Instances[5])}}),"duplicate placed identity");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {Instances=d.Data.Design.Instances.RemoveAt(6)}}),"missing jet");
        Reject(()=>Load(d.Data with {Definitions=d.Data.Definitions.Select(p=>p.Role==AssemblyRole.Command?p with {DryMassKg=481}:p).ToImmutableArray()}),"altered definition with stale digest");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.MainEngine,p=>p with {Propulsion=p.Propulsion! with {FuelIdentity="other"}})),"incompatible consumer chemical identity");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.Tank,p=>p with {Stores=p.Stores.Select(s=>s with {ResourceIdentity="other"}).ToImmutableArray()})),"incompatible store chemical identity");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.MainEngine,p=>p with {Gimbal=p.Gimbal! with {Id=p.Propulsion!.Id}})),"capability ID alias");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.MainEngine,p=>p with {Gimbal=p.Gimbal! with {NozzleOffset=new(-1e20,0,0)},Propulsion=p.Propulsion! with {Point=new(-1e20,0,0)}})),"unbounded nozzle datum");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.Command,p=>p with {LocalInertia=new(1,0,0,0,1,0,0,0,3)})),"unphysical inertia inequalities");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.RcsJet,p=>p with {Propulsion=p.Propulsion! with {Axis=-Double3.UnitX}})),"wrong jet force direction");
        Reject(()=>Load(ChangeDef(d.Data,AssemblyRole.RcsJet,p=>p with {Propulsion=p.Propulsion! with {FullThrustN=7.5,ExtentRateKgS=1d/2048}})),"frozen-A insufficient angular authority");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {Feeds=d.Data.Design.Feeds.RemoveAt(0)}}),"structural attachment creates no implicit feed");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {InitialFuelKg=31}}),"unmatched load");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {InitialFuelKg=42,InitialOxidizerKg=63}}),"capacity overflow");
        foreach(var rotation in new[]{new Matrix3(2,0,0,0,1,0,0,0,1),new Matrix3(-1,0,0,0,1,0,0,0,1)})
            Reject(()=>Load(d.Data with {Design=d.Data.Design with {Instances=d.Data.Design.Instances.SetItem(3,d.Data.Design.Instances[3] with {Pose=d.Data.Design.Instances[3].Pose with {Rotation=rotation}})}}),"unsupported scale/reflection");
        Reject(()=>Load(d.Data with {Design=d.Data.Design with {Instances=d.Data.Design.Instances.SetItem(3,d.Data.Design.Instances[3] with {Pose=d.Data.Design.Instances[3].Pose with {Position=d.Data.Design.Instances[3].Pose.Position+Double3.UnitX}})}}),"moved RCS station/mating mismatch");
        var visual=Load(ChangeDef(d.Data,AssemblyRole.Command,p=>p with {VisualReference="different.visual/2"}));
        var x=One(d,new(true,"+ROLL",0,0,15625));var y=One(visual,new(true,"+ROLL",0,0,15625));
        Check(x.Motion==y.Motion&&x.Stores==y.Stores&&x.Mass==y.Mass&&d.Digest!=visual.Digest,"visual reference has no physical authority");
        var rename=Load(d.Data with {Design=d.Data.Design with {Id="independent.design.name"}});
        Check(One(rename,new(true,"+ROLL",0,0,15625)).Motion==x.Motion,"no vehicle-specific design-ID dynamics branch");
    }
    private static void MassAndActuators(CompiledAssemblyDesign d)
    {
        var dry=d.ObserveMass(630);var full=d.ObserveMass(730);var initial=d.ObserveMass(705);
        Check(Near(d.DryMass,630)&&Near(dry.Com.X,1.1664919015056454)&&Near(initial.Com.X,1.042397018366747)&&Near(full.Com.X,1.006698490340489),"dry/initial/capacity mass and independent off-store COM");
        Check(Near(dry.Inertia.A,190.4217166666667)&&Near(dry.Inertia.E,639.755404039546)&&Near(dry.Inertia.I,639.062583716518),"full dry tensor matches independent numeric fixture");
        Check(Near(full.Inertia.E,757.18596766356)&&Near(full.Inertia.I,756.493147340532),"capacity tensor matches independent fixture");
        Check(dry.Com.X!=0&&d.Tank.Definition.Stores.All(x=>x.Datum==Double3.Zero),"frozen datums not retuned to fixed COM");
        foreach(var pair in d.Data.Design.Pairs)
        {
            var a=AssemblyActuation.Jet(d.Part(pair.First));var b=AssemblyActuation.Jet(d.Part(pair.Second));var f=a.Force+b.Force;
            var tau=a.MomentAtOrigin+b.MomentAtOrigin-Double3.Cross(initial.Com,f);var alpha=initial.Inertia.Inverse().Apply(tau);var v=new[]{alpha.X,alpha.Y,alpha.Z};
            Check(v[pair.Axis]*pair.Sign>=.005&&Norm(f)>0&&Enumerable.Range(0,3).Where(i=>i!=pair.Axis).All(i=>Math.Abs(v[i])<1e-12),"signed current-COM wrench "+pair.Name);
            var s=One(d,new(false,pair.Name,0,0,15625));
            Check(BitOperations.PopCount(s.Actual.Jets)==2&&!s.Actual.MainOn&&Norm(s.Motion.VelocityO)>0,"isolated "+pair.Name+" exactly two full jets and actual translation");
        }
        foreach(var jet in d.Jets)
        {var w=AssemblyActuation.Jet(jet);var point=jet.Instance.Pose.Point(jet.Definition.Propulsion!.Point);Check(Near(Norm(w.Force),22.5,1e-12)&&w.MomentAtOrigin==Double3.Cross(point,w.Force),"single-jet diagnostic "+jet.Instance.Id);}
        var neutral=AssemblyActuation.Main(d,0,0);
        Check(neutral.Force==new Double3(600,0,0)&&neutral.MomentAtOrigin==Double3.Zero,"neutral main physical origin/force");
        foreach(var x in new[]{-.05,.05})
        {var p=AssemblyActuation.Main(d,x,0);var y=AssemblyActuation.Main(d,0,x);Check(p.Force.Z*x<0&&p.MomentAtOrigin.Y*x<0&&y.Force.Y*x>0&&y.MomentAtOrigin.Z*x<0,"signed gimbal force/moment "+x);}
        var commands=Enumerable.Range(0,34).Select(i=>new AssemblyCommand(i==0,null,i==33?-1:1,i==33?1:-1,15625)).ToArray();
        var session=Session(d,commands);var first=Next(session);
        Check(Record(session,0).Wrench==neutral&&first.Gimbal.ActualY==.0015625&&first.Gimbal.ActualZ==-.0015625&&first.Gimbal.TargetY==.05&&first.Gimbal.TargetZ==-.05,"held actual drives force; desired clamps and actual slews");
        var second=Next(session);
        Check(!second.Actual.MainOn&&second.Gimbal.ActualY==.003125&&second.Stores==first.Stores,"engine-off gimbal slews without resource debit");
        for(var i=2;i<33;i++)Next(session);
        Check(Observe(session).State.Gimbal.ActualY==.05&&Observe(session).State.Gimbal.ActualZ==-.05,"gimbal saturation no overshoot");
        var reverse=Next(session);Check(reverse.Gimbal.ActualY==.05-.0015625&&reverse.Gimbal.ActualZ==-.05+.0015625,"actual slew on target reversal");
        Check(first.Mass==AssemblyLaunch.ObserveMass(d,first.Stores)&&first.Mass.Com!=initial.Com&&first.Mass.Inertia!=initial.Inertia,"exact successor updates full mass/COM/tensor at publication");
    }
    private static void Resources(CompiledAssemblyDesign d)
    {
        var start=new AssemblyStores(AssemblyResources.Mass(30),AssemblyResources.Mass(45));var km=AssemblyResources.Rate(5d/128);var kr=AssemblyResources.Rate(3d/2048);
        var commands=new[]{new AssemblyCommand(false,null,0,0,15625),new(true,null,0,0,15625),new(false,"+ROLL",0,0,15625),new(true,"+ROLL",0,0,15625)};
        var rates=new[]{default(PropellantInteger),km,AssemblyResources.Times(kr,2),AssemblyResources.Add(km,AssemblyResources.Times(kr,2))};
        for(var i=0;i<commands.Length;i++)
        {
            var s=One(d,commands[i]);var k=rates[i];
            Check(Big(start.Fuel)-Big(s.Stores.Fuel)==Big(k)*2*15625&&Big(start.Oxidizer)-Big(s.Stores.Oxidizer)==Big(k)*3*15625,"production two-store exact debit row "+i);
            Check(AssemblyResources.Matched(s.Stores)&&s.ResourceRevision==(i==0?0UL:1UL),"matched residue and resource revision row "+i);
        }
        var tiny=AssemblyResources.Calculate(d,new(PropellantInteger.FromUInt64(2),PropellantInteger.FromUInt64(3)),km,1);
        Check(tiny.Classification==PropellantClassification.InteriorExhaustion&&tiny.After==default&&tiny.Powered.Numerator==PropellantInteger.FromUInt64(2),"smallest matched exact parcel");
        Reject(()=>AssemblyResources.Times(AssemblyResources.ParseHex(new string('f',544)),2),"checked bounded multiply overflow");
        Reject(()=>AssemblyResources.Add(AssemblyResources.ParseHex(new string('f',544)),PropellantInteger.FromUInt64(1)),"checked bounded add overflow");
        Reject(()=>AssemblyResources.Validate(d,new(AssemblyResources.Mass(42),AssemblyResources.Mass(63))),"capacity violation");
        Reject(()=>AssemblyResources.Validate(d,new(AssemblyResources.Mass(30),AssemblyResources.Mass(46))),"species mismatch");
        Reject(()=>AssemblyResources.ParseHex("0"),"noncanonical resource encoding");
        var near=LoadAmount(d,25d/1024);var endpoint=Session(near,Enumerable.Repeat(new AssemblyCommand(true,null,0,0,15625),9).ToArray(),Moving);
        var mixed=Session(near,Enumerable.Repeat(new AssemblyCommand(true,"+ROLL",.05,0,15625),9).ToArray(),Moving,new(.05,0,.05,0));
        for(var i=0;i<8;i++){Next(endpoint);Next(mixed);}
        Check(TotalPowered(endpoint)==(new BigInteger(1),new BigInteger(8)),"production exact 1/8s endpoint depletion");
        Check(TotalPowered(mixed)==(new BigInteger(5),new BigInteger(43)),"production exact 5/43s interior exhaustion");
        Check(Record(endpoint,7).Classification==PropellantClassification.EndpointExhaustion&&Record(mixed,7).Classification==PropellantClassification.InteriorExhaustion,"qualified endpoint/interior interval classifications");
        foreach(var s in new[]{endpoint,mixed})
        {
            var depleted=Observe(s).State;Check(depleted.Stores==default&&!depleted.Actual.MainOn&&depleted.Actual.Jets==0&&depleted.Mass==d.ObserveMass(630),"all outputs and resources exhaust atomically");
            var after=Next(s);Check(Record(s,8).Classification==PropellantClassification.NoFeed&&after.Stores==default&&after.ResourceRevision==depleted.ResourceRevision,"exhausted no-feed does not debit");
        }
    }
    private static void Ownership(CompiledAssemblyDesign d)
    {
        var c=new AssemblyCommand(true,"+ROLL",.05,0,15625);var s=Session(d,[c,c]);var other=Session(d,[c,c],id:"different");
        Check(!s.Launch.PartKeys.Intersect(other.Launch.PartKeys).Any()&&s.Launch.PartKeys.Length==7&&s.Launch.CapabilityKeys.Length==(d.HasIndependentBlockJets?18:6)&&s.Launch.StoreKeys.Length==2,"distinct runtime part/capability/store identities");
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.AwaitingDebt,"cannot step without canonical clock debt");
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,2,new(15625)).Status==AssemblyFlightStatus.InvalidSequence,"out-of-order host credit refusal");
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(-1)).Status==AssemblyFlightStatus.InvalidInput,"negative host interval refusal");
        Credit(s,15625);var before=Observe(s);var borrowed=s.Engine.State;
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _,true)==AssemblyFlightStatus.PreparationRefused&&Observe(s)==before,"refused preparation leaves all canonical records unchanged");
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var p)==AssemblyFlightStatus.Prepared&&Observe(s)==before,"private prepared state does not publish/spend");
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.OutstandingProposal,"one outstanding proposal");
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,2,new(1)).Status==AssemblyFlightStatus.OutstandingProposal,"host credit cannot overlap proposal");
        Check(other.Engine.PublishAssemblyFlight(other.Authority,p).Status==AssemblyFlightStatus.InvalidProposal&&s.Engine.PublishAssemblyFlight(other.Authority,p).Status==AssemblyFlightStatus.InvalidAuthority,"foreign proposal and authority rejected");
        Check(s.Engine.PublishAssemblyFlight(s.Authority,default).Status==AssemblyFlightStatus.InvalidProposal,"unissued proposal rejected");
        Check(s.Engine.AbortAssemblyFlight(s.Authority,p)==AssemblyFlightStatus.Ready&&Observe(s)==before,"abort retains full canonical source");
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out var next)==AssemblyFlightStatus.Prepared&&s.Engine.PublishAssemblyFlight(s.Authority,p).Status==AssemblyFlightStatus.InvalidProposal,"expired generation cannot publish");
        Check(s.Engine.PublishAssemblyFlight(s.Authority,next).Status==AssemblyFlightStatus.Published&&s.Engine.PublishAssemblyFlight(s.Authority,next).Status==AssemblyFlightStatus.InvalidProposal,"one publication/no replay");
        Check(!borrowed.Spacecraft.TryGetAssembly(s.Launch.Spacecraft.Id,out _,out _),"prior canonical borrowed view expires after publication");
        var value=Observe(s);Check(value.StateRevision.Value==1&&value.HistoryCount==1&&value.State.Frontier==1&&value.Clock.Time.Ticks==15625&&value.Clock.Debt.Ticks==0,"state/history/frontier/clock commit together");
        var wrongThread=Task.Run(()=>s.Engine.PrepareAssemblyFlight(s.Authority,out _)).GetAwaiter().GetResult();Check(wrongThread==AssemblyFlightStatus.WrongOwnerThread,"one writer enforced");
        var stale=Session(d,[c]);Credit(stale,15625);stale.Clock.Pause();var paused=Observe(stale);
        Check(stale.Engine.PrepareAssemblyFlight(stale.Authority,out _)==AssemblyFlightStatus.StaleSource&&Observe(stale)==paused,"external canonical clock change refuses stale source");
        var fail=Session(d,[c,c]);Credit(fail,15625);Check(fail.Engine.PrepareAssemblyFlight(fail.Authority,out var pf)==AssemblyFlightStatus.Prepared,"ack-fault source prepared");
        Check(fail.Engine.PublishAssemblyFlight(fail.Authority,pf,true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"postcommit private fault classified");
        var committed=Observe(fail);Check(committed.State==value.State&&committed.StateRevision==value.StateRevision&&committed.HistoryCount==1&&committed.PrivateInvalidated,"ack fault preserves complete canonical successor");
        Check(fail.Engine.PublishAssemblyFlight(fail.Authority,pf).Status==AssemblyFlightStatus.Invalidated&&fail.Engine.PrepareAssemblyFlight(fail.Authority,out _)==AssemblyFlightStatus.Invalidated,"invalidated continuation cannot spend again");
        var creditFail=Session(d,[c]);Check(creditFail.Engine.AdmitAssemblyHostTime(creditFail.Authority,1,new(15625),true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"host acknowledgement fault classified");
        var cv=Observe(creditFail);Check(cv.Clock.Debt.Ticks==15625&&cv.HostSequence==1&&cv.State.Frontier==0&&cv.State.Stores==creditFail.Launch.Initial.Stores,"host debt/sequence commit together without physical advance");
        var capacity=Session(d,[c,c],capacity:1);Next(capacity);Credit(capacity,15625);var full=Observe(capacity);
        Check(capacity.Engine.PrepareAssemblyFlight(capacity.Authority,out _)==AssemblyFlightStatus.HistoryCapacity&&Observe(capacity)==full,"fixed history capacity fails without partial successor");
        Reject(()=>Launch(d,[c with {Ticks=15626}]),"oversized physical interval");
        Reject(()=>Launch(d,[c with {Pair="unknown"}]),"undeclared RCS pair");
        Reject(()=>Launch(d,[c with {GimbalTargetY=double.NaN}]),"nonfinite actuator command");
        Reject(()=>Launch(d,Enumerable.Repeat(c,129).ToArray()),"unbounded episode");
        Reject(()=>new AssemblyStockCatalog([(d.Save(),"wrong")] ),"catalog requires pinned design digest");
        Reject(()=>new AssemblyStockCatalog([(d.Save(),d.Digest),(d.Save(),d.Digest)]),"duplicate stock catalog identity");
        Check(AssemblyStockCatalog.LoadDefault().Resolve(d.Data.Design.Id).Digest==d.Digest,"production stock catalog resolves pinned qualified design");
        Check(s.Engine.State.Spacecraft.Count==1&&s.Engine.State.Spacecraft.TryGetDefinition(s.Launch.Spacecraft.Id,out var registered)&&registered==s.Launch.Spacecraft,"assembly is one ordinary registered spacecraft");
        Check(s.Engine.BeginAssemblyFlight(s.Launch,128,out _)==AssemblyFlightStatus.InvalidInput,"canonical spacecraft cannot acquire a duplicate physical owner");
        var mutableCommands=new[]{c};var immutableLaunch=Launch(d,mutableCommands);mutableCommands[0]=c with {MainOn=false};
        Check(immutableLaunch.Plan[0].Request==c,"launch plan copies caller-owned command array");
    }
    private static void TypedMotion(CompiledAssemblyDesign d)
    {
        var s=Session(d,[new(false,"+YAW",0,0,15625)],Moving);var value=Next(s);var view=s.Engine.State;var id=s.Launch.Spacecraft.Id;
        Check(SpacecraftMotionEvaluator.TryEvaluate(view,id,value.Epoch,out _)==SpacecraftTranslationStatus.AssemblyMotionRequiresTypedView,"legacy COM segment evaluator refuses material-origin state");
        Check(SpacecraftMotionEvaluator.TryEvaluateAssembly(view,id,value.Epoch,out var m)==SpacecraftTranslationStatus.Success&&m.MaterialOriginMotion==value.Motion&&m.CurrentMass==value.Mass,"typed published material-origin motion");
        Check(m.CenterOfMassPositionRoot==value.Motion.PositionO+value.Motion.BodyToWorld.Rotate(value.Mass.Com)&&m.CenterOfMassPositionRoot!=value.Motion.PositionO,"explicit current COM position conversion");
        Check(m.MaterialVelocityAtCurrentComRoot==value.Motion.VelocityO+value.Motion.BodyToWorld.Rotate(Double3.Cross(value.Motion.AngularVelocityBody,value.Mass.Com)),"explicit rigid material velocity at migrating COM");
        Check(SpacecraftMotionEvaluator.TryEvaluateAssembly(view,id,new(value.Epoch.Ticks+1),out _)==SpacecraftTranslationStatus.OutsideQualifiedEndpoint,"no unauthorized endpoint extrapolation");
        Check(!view.Spacecraft.TryGetTranslation(id,out _,out _)&&!view.Spacecraft.TryGetRigidBody(id,out _),"no duplicate legacy physical authority");
        var frames=new ReferenceFrameEvaluation[s.Frames.Count];
        Check(SpacecraftReferenceFrameEvaluator.TryEvaluate(view.Spacecraft,s.Frames,value.Epoch,frames)==SpacecraftReferenceFrameEvaluationStatus.Success,"normal frame evaluator supports published assembly");
        Check(s.Frames.TryGetIndex(s.Launch.Spacecraft.BodyFrame,out var index)&&frames[index].Value.LocalToParent.Translation==value.Motion.PositionO&&frames[index].Value.LocalToParent.Rotation==value.Motion.BodyToWorld&&frames[index].Value.OriginVelocityInParent==value.Motion.VelocityO,"normal body frame carries material origin rather than COM");
    }
    private static void SaveResume(CompiledAssemblyDesign d)
    {
        var catalog=CatalogOf(d);var command=new AssemblyCommand(true,"-PITCH",.05,-.05,15625);
        var plan=Enumerable.Range(0,128).Select(i=>new AssemblyCommand(i%3!=0,d.Data.Design.Pairs[i%6].Name,i%4<2?.05:-.05,i%5<3?-.05:.05,15625)).ToArray();var source=Session(d,plan,Moving);
        for(var i=0;i<61;i++)Next(source);
        var bytes=source.Save();var restored=AssemblyApplicationSession.Restore(catalog,bytes);
        Check(bytes.SequenceEqual(restored.Save())&&Observe(source)==Observe(restored),"live snapshot roundtrips exact identity/resources/actuators/motion/revisions");
        Check(Observe(restored).State.Stores!=source.Launch.Initial.Stores&&restored.Launch.PartKeys.SequenceEqual(source.Launch.PartKeys)&&restored.Launch.CapabilityKeys.SequenceEqual(source.Launch.CapabilityKeys)&&restored.Launch.StoreKeys.SequenceEqual(source.Launch.StoreKeys),"resume preserves drained resources and recorded runtime identities");
        for(var i=61;i<128;i++){Next(source);Next(restored);}
        Check(source.Save().SequenceEqual(restored.Save()),"resumed continuation bitwise matches uninterrupted complete episode");
        var full=AssemblyApplicationSession.Restore(catalog,source.Save());
        Check(full.Save().SequenceEqual(source.Save())&&Observe(full).State.Frontier==128&&full.Engine.ServiceAssemblyFlightDebt(full.Authority).Status==AssemblyFlightStatus.Completed,"full-capacity save restores terminal frontier without refill");
        var pending=AssemblyApplicationSession.Create(Launch(d,plan,Moving),128,new SimulationRate(1,3));Credit(pending,47_000);Drain(pending);
        var p=Observe(pending);Check(p.State.Frontier==1&&p.Clock.Debt.Ticks==41&&p.Clock.RateRemainder==2,"nonintegral simulation rate retains exact pending debt/remainder");
        var pendingRestored=AssemblyApplicationSession.Restore(catalog,pending.Save());
        Check(Observe(pendingRestored)==p&&pending.Save().SequenceEqual(pendingRestored.Save()),"save/resume preserves debt/rate/remainder/host sequence");
        Credit(pending,46_875);Credit(pendingRestored,46_875);Drain(pending);Drain(pendingRestored);
        Check(pending.Save().SequenceEqual(pendingRestored.Save()),"fractional-rate resumed continuation identical");
        Check(pendingRestored.Engine.AdmitAssemblyHostTime(pendingRestored.Authority,1,new(10)).Status==AssemblyFlightStatus.InvalidSequence,"restored host-credit identity rejects replay");
        var dto=AssemblyJson.Read<AssemblySaveData>(bytes);
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {DesignDigest=new string('0',64)})),"snapshot cannot substitute design digest");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Current=dto.Current with {Stores=source.Launch.Initial.Stores,Mass=source.Launch.Initial.Mass}})),"forged refill with matching mass rejected by replay");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Current=dto.Current with {Motion=dto.Current.Motion with {PositionO=Double3.UnitX}}})),"forged canonical motion rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Current=dto.Current with {Actual=default}})),"forged current RCS realization rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Current=dto.Current with {Gimbal=dto.Current.Gimbal with {ActualY=dto.Current.Gimbal.ActualY==0?.001:0}}})),"forged actual gimbal rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Current=dto.Current with {ResourceRevision=0}})),"forged resource revision rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {StateRevision=dto.StateRevision+1})),"forged canonical revision rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {HistoryDigest=new string('0',64)})),"forged history digest rejected");
        var edited=plan.ToArray();edited[0]=edited[0] with {MainOn=!edited[0].MainOn};
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Plan=edited})),"rewritten past command rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {HostSequence=0})),"missing committed host identity rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {DebtTicks=-1})),"negative debt rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {RateNumerator=2,RateDenominator=2})),"noncanonical rate rejected");
        var text=Encoding.UTF8.GetString(bytes);
        Reject(()=>AssemblyApplicationSession.Restore(catalog,Encoding.UTF8.GetBytes(text.Replace("\"schema\":","\"unexpected\":1,\"schema\":",StringComparison.Ordinal))),"arbitrary runtime metadata rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,Encoding.UTF8.GetBytes(text.Replace("\"schema\":","\"schema\":\"other\",\"schema\":",StringComparison.Ordinal))),"duplicate runtime JSON member rejected");
        var outstanding=Session(d,[command]);Credit(outstanding,15625);Check(outstanding.Engine.PrepareAssemblyFlight(outstanding.Authority,out var lease)==AssemblyFlightStatus.Prepared,"save-proposal test prepared");
        Reject(()=>outstanding.Save(),"save refuses an outstanding uncommitted proposal");
        Check(outstanding.Engine.AbortAssemblyFlight(outstanding.Authority,lease)==AssemblyFlightStatus.Ready,"save-proposal cleanup");
        var fresh=AssemblyApplicationSession.Restore(catalog,outstanding.Save());
        Check(fresh.Engine.PublishAssemblyFlight(fresh.Authority,lease).Status==AssemblyFlightStatus.InvalidProposal,"private lease never transfers through save/resume");
        var fault=Session(d,[command,command]);Credit(fault,15625);Check(fault.Engine.PrepareAssemblyFlight(fault.Authority,out var failLease)==AssemblyFlightStatus.Prepared,"save-after-ack-fault prepare");
        Check(fault.Engine.PublishAssemblyFlight(fault.Authority,failLease,true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"save-after-ack-fault commit");
        var recovered=AssemblyApplicationSession.Restore(catalog,fault.Save());var faultState=Observe(fault);
        Check(Observe(recovered).State==faultState.State&&!Observe(recovered).PrivateInvalidated&&fault.Save().SequenceEqual(recovered.Save()),"physical acknowledgement fault resumes from already committed endpoint");
        var reference=Session(d,[command,command]);Next(reference);Next(reference);Next(recovered);
        Check(Observe(recovered).State==Observe(reference).State,"acknowledgement recovery consumes each physical interval exactly once");
        var hostFault=Session(d,[command]);Check(hostFault.Engine.AdmitAssemblyHostTime(hostFault.Authority,1,new(15625),true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"save-after-host-ack fault");
        var hostRecovered=AssemblyApplicationSession.Restore(catalog,hostFault.Save());
        Check(hostRecovered.Engine.AdmitAssemblyHostTime(hostRecovered.Authority,1,new(15625)).Status==AssemblyFlightStatus.InvalidSequence,"committed host-credit replay refused after recovery");
        Drain(hostRecovered);Check(Observe(hostRecovered).State.Frontier==1&&Observe(hostRecovered).Clock.Debt.Ticks==0,"already credited debt advances once after recovery");
        var external=Session(d,[command]);external.Clock.Pause();Reject(()=>external.Save(),"save refuses externally changed/stale clock");
        Check(AssemblyLaunch.RuntimeKey("capability","launch","a/b","c")!=AssemblyLaunch.RuntimeKey("capability","launch","a","b/c")&&
            AssemblyLaunch.RuntimeKey("part","launch","a")!=AssemblyLaunch.RuntimeKey("store","launch","a"),"kind and length-prefixed runtime tuples prevent delimiter aliasing");
    }
    private static void SavedCreditContract(CompiledAssemblyDesign d)
    {
        var catalog=CatalogOf(d);var command=new AssemblyCommand(true,"+ROLL",0,0,15625);
        var pristine=Session(d,[command,command]);var original=AssemblyJson.Read<AssemblySaveData>(pristine.Save());
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(original with {HostSequence=1})),"untouched zero-funded snapshot cannot claim a positive host-credit count");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(original with {HostSequence=1,RateNumerator=1,RateDenominator=long.MaxValue,DebtTicks=2})),"one positive Int64 host credit cannot fund two ticks at reciprocal-Int64-max rate");
        // Each host receipt and the pending debt fit Int64. Total lifetime credit
        // need not: a negative epoch permits representable canonical endpoints
        // after a cumulative funded duration greater than Int64.MaxValue.
        var launch=new AssemblyLaunch(d,pristine.Launch.Spacecraft,"negative_origin",Rest,default,[command,command],new(-15625));
        var negative=AssemblyApplicationSession.Create(launch);Credit(negative,long.MaxValue);
        Check(negative.Engine.PrepareAssemblyFlight(negative.Authority,out var lease)==AssemblyFlightStatus.Prepared&&
            negative.Engine.PublishAssemblyFlight(negative.Authority,lease).Status==AssemblyFlightStatus.Published,"negative-origin interval publishes with representable canonical debt");
        Credit(negative,10_000);var live=Observe(negative);
        Check((Int128)live.Clock.Time.Ticks-launch.Initial.Epoch.Ticks+live.Clock.Debt.Ticks>long.MaxValue&&
            live.Clock.Time.Ticks==0&&live.Clock.Debt.Ticks==long.MaxValue-5625&&live.HostSequence==2,"valid cumulative funding exceeds Int64 without overflowing current clock or receipt");
        var restored=AssemblyApplicationSession.Restore(catalog,negative.Save());
        Check(Observe(restored)==live&&negative.Save().SequenceEqual(restored.Save()),"valid negative-origin large cumulative credit restores exactly");
        foreach(var physicalFault in new[]{false,true})
        {
            var fault=Session(d,[command,command]);
            if(physicalFault)
            {
                Credit(fault,15625);
                Check(fault.Engine.PrepareAssemblyFlight(fault.Authority,out var p)==AssemblyFlightStatus.Prepared&&
                    fault.Engine.PublishAssemblyFlight(fault.Authority,p,true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"physical acknowledgement fault retains committed receipt");
            }
            else Check(fault.Engine.AdmitAssemblyHostTime(fault.Authority,1,new(15625),true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"host acknowledgement fault retains committed receipt");
            var saved=fault.Save();var recovered=AssemblyApplicationSession.Restore(catalog,saved);
            Check(saved.SequenceEqual(recovered.Save()),"matching committed fault receipt remains recoverable "+physicalFault);
            Check(fault.Clock.AdvanceByHostDuration(new(100)).Reason==NovaCore.Simulation.Clock.SimulationHostAdvanceStopReason.Accepted,"adversarial external host credit reaches canonical clock "+physicalFault);
            Reject(()=>fault.Save(),"invalidated private continuation cannot bless externally changed canonical clock "+physicalFault);
        }
    }
    private static void CanonicalCreditTrace(CompiledAssemblyDesign d)
    {
        var catalog=CatalogOf(d);var command=new AssemblyCommand(true,"+ROLL",0,0,15625);
        var capacity=Session(d,[command,command]);
        for(var i=1;i<=4096;i++)Check(capacity.Engine.AdmitAssemblyHostTime(capacity.Authority,i,new(1)).Status==AssemblyFlightStatus.AcceptedCredit,"positive host-credit receipt accepted within fixed capacity");
        var full=capacity.Save();var dto=AssemblyJson.Read<AssemblySaveData>(full,1_048_576);var before=Observe(capacity);
        Check(dto.Schema=="novacore.assembly-runtime/2"&&dto.Credits.Length==4096&&dto.HostSequence==4096&&dto.Credits.All(x=>x.HostTicks==1&&x.Frontier==0)&&before.Clock.Debt.Ticks==4096,"runtime/2 records all exact receipts at the physical frontier");
        Check(capacity.Engine.AdmitAssemblyHostTime(capacity.Authority,4097,new(1)).Status==AssemblyFlightStatus.HistoryCapacity&&capacity.Save().SequenceEqual(full)&&Observe(capacity)==before,"receipt capacity refuses before debt/identity/history mutation");
        Check(capacity.Engine.AdmitAssemblyHostTime(capacity.Authority,4097,new(0)).Status==AssemblyFlightStatus.NoWork&&capacity.Save().SequenceEqual(full),"zero duration consumes no receipt slot at full capacity");
        Check(capacity.Engine.AdmitAssemblyHostTime(capacity.Authority,4097,new(-1)).Status==AssemblyFlightStatus.InvalidInput&&capacity.Save().SequenceEqual(full),"negative host duration rejected without changing full receipt trace");
        Check(full.Length<1_048_576&&AssemblyApplicationSession.Restore(catalog,full).Save().SequenceEqual(full),"complete 4096-credit trace fits supported 1MiB save and restores exactly");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Credits=[..dto.Credits,new(1,0)],HostSequence=4097,DebtTicks=4097})),"oversized saved receipt trace rejected");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Schema="novacore.assembly-runtime/1"})),"retired aggregate-feasibility runtime/1 schema explicitly refused");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(dto with {Credits=null!})),"missing canonical receipt array refused");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,full.AsSpan(0,full.Length-1)),"truncated canonical trace JSON refused");

        // Distinct recorded frontiers witness admission interleaved with ordinary
        // physical publication. Restoration must reproduce this order, not just
        // a final sum of elapsed durations or host-credit count.
        var source=Session(d,[command,command,command]);Next(source);Next(source);Credit(source,100);
        var saved=source.Save();var trace=AssemblyJson.Read<AssemblySaveData>(saved);
        Check(trace.Credits.Select(x=>x.Frontier).SequenceEqual(new[]{0,1,2})&&trace.Credits.Select(x=>x.HostTicks).SequenceEqual(new long[]{15625,15625,100}),"credit trace records actual physical frontier before each admission");
        Check(AssemblyApplicationSession.Restore(catalog,saved).Save().SequenceEqual(saved),"interleaved physical publications and pending credit restore through canonical path");
        void Mutate(int index,AssemblyHostCredit replacement,string label)
        {var credits=trace.Credits.ToArray();credits[index]=replacement;Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(trace with {Credits=credits})),label);}
        Mutate(2,trace.Credits[2] with {Frontier=0},"retrograde saved credit frontier rejected");
        Mutate(0,trace.Credits[0] with {Frontier=1},"unfunded future physical frontier rejected before host receipt");
        Mutate(2,trace.Credits[2] with {Frontier=3},"credit frontier beyond saved endpoint rejected");
        Mutate(1,trace.Credits[1] with {HostTicks=0},"zero elapsed saved host receipt rejected");
        Mutate(1,trace.Credits[1] with {HostTicks=-1},"negative elapsed saved host receipt rejected");
        Mutate(1,trace.Credits[1] with {HostTicks=15624},"altered receipt cannot fund recorded physical prefix");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(trace with {Credits=[trace.Credits[0],trace.Credits[2]],HostSequence=2})),"removed interior credit cannot be repaired by changing sequence count");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(trace with {Credits=trace.Credits[..^1],HostSequence=2})),"truncated credit trace cannot reproduce retained pending debt");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(trace with {Credits=[..trace.Credits,new(1,2)],HostSequence=4})),"extra credit cannot match saved complete clock state");
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(trace with {HostSequence=trace.HostSequence+1})),"host sequence must equal canonical receipt count");

        var doubleRate=AssemblyApplicationSession.Create(Launch(d,[command,command]),128,SimulationRate.Two);var untouched=doubleRate.Save();
        Check(doubleRate.Engine.AdmitAssemblyHostTime(doubleRate.Authority,1,new(long.MaxValue)).Status==AssemblyFlightStatus.Overflow&&doubleRate.Save().SequenceEqual(untouched),"ordinary admission rejects one overflowing doubled-rate receipt without mutation");
        var impossible=AssemblyJson.Read<AssemblySaveData>(untouched) with {Credits=[new(long.MaxValue,0)],HostSequence=1,DebtTicks=long.MaxValue};
        Reject(()=>AssemblyApplicationSession.Restore(catalog,AssemblyJson.Write(impossible)),"restore reuses ordinary per-credit overflow refusal instead of aggregate feasibility");
    }
    private static void Drain(AssemblyApplicationSession s)
    {
        for(var i=0;i<40;i++)
        {var r=s.Engine.ServiceAssemblyFlightDebt(s.Authority);if(r.Status is AssemblyFlightStatus.Completed or AssemblyFlightStatus.AwaitingDebt)return;Check(r.Status==AssemblyFlightStatus.BudgetExhausted&&r.PublishedCount==4,"bounded normal service budget");}
        throw new InvalidOperationException("Assembly service failed to terminate.");
    }
    private static void Cadence(CompiledAssemblyDesign d)
    {
        var plan=Enumerable.Repeat(new AssemblyCommand(true,"+ROLL",.05,-.05,15625),128).ToArray();AssemblyRuntimeState? expected=null;AssemblyFlightRecord[]? records=null;
        foreach(var hz in new[]{20,60,144,240})
        {
            var s=Session(d,plan,Moving);long previous=0;
            for(var frame=1;frame<=2*hz;frame++)
            {var now=frame*1_000_000L/hz;Credit(s,now-previous);previous=now;var result=s.Engine.ServiceAssemblyFlightDebt(s.Authority);Check(result.PublishedCount<=4&&result.Status is AssemblyFlightStatus.Completed or AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.BudgetExhausted,"frame cadence keeps fixed servicing work");}
            Drain(s);var end=Observe(s);Check(end.State.Frontier==128&&end.Clock.Time.Ticks==2_000_000&&end.Clock.Debt.Ticks==0,"cadence completes exact horizon "+hz);
            if(expected is null){expected=end.State;records=Enumerable.Range(0,128).Select(i=>Record(s,i)).ToArray();}
            else Check(end.State==expected&&Enumerable.Range(0,128).All(i=>Record(s,i)==records![i]),"physical state and exact interval history independent of host cadence "+hz);
        }
        var warped=AssemblyApplicationSession.Create(Launch(d,plan,Moving),128,SimulationRate.Ten);Credit(warped,200_000);
        var first=warped.Engine.ServiceAssemblyFlightDebt(warped.Authority);Check(first.Status==AssemblyFlightStatus.BudgetExhausted&&first.PublishedCount==4,"warp accrues debt without multiplying per-service budget");
        Drain(warped);Check(Observe(warped).State==expected,"warp changes pacing only");
    }
    private static double Rational(string s){var p=s.Split('/');return double.Parse(p[0],CultureInfo.InvariantCulture)/(p.Length==2?double.Parse(p[1],CultureInfo.InvariantCulture):1);}
    private static double[] Vector(AssemblyMotion m)=>[m.PositionO.X,m.PositionO.Y,m.PositionO.Z,m.VelocityO.X,m.VelocityO.Y,m.VelocityO.Z,m.BodyToWorld.X,m.BodyToWorld.Y,m.BodyToWorld.Z,m.BodyToWorld.W,m.AngularVelocityBody.X,m.AngularVelocityBody.Y,m.AngularVelocityBody.Z];
    private static double Error(double[] a,double[] b,int start,int count)=>Math.Sqrt(Enumerable.Range(start,count).Sum(i=>(a[i]-b[i])*(a[i]-b[i])));
    private static void Trajectories(CompiledAssemblyDesign stock)
    {
        using var stream=typeof(AssemblyProductionTests).Assembly.GetManifestResourceStream("NovaCore.Tests.AssemblyReferenceEndpoints");
        using var doc=stream is not null&&!stock.HasIndependentBlockJets?JsonDocument.Parse(stream):JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Data",stock.HasIndependentBlockJets?"Assembly-FourHorn-Reference-Endpoints.json":"Assembly-Reference-Endpoints.json")));
        if(stock.HasIndependentBlockJets)Check(doc.RootElement.GetProperty("design_sha256").GetString()==stock.Digest,"independent oracle belongs to current physical definition");
        var count=0;
        foreach(var row in doc.RootElement.GetProperty("trajectory_rows").EnumerateArray())
        {
            var c=row.GetProperty("case");var name=c.GetProperty("name").GetString()!;var d=LoadAmount(stock,Rational(c.GetProperty("load").GetString()!));
            var angles=c.GetProperty("angles").EnumerateArray().Select(x=>x.GetDouble()).ToArray();var jets=c.GetProperty("pair").EnumerateArray().Select(x=>"rcs_"+x.GetInt32().ToString("D2")).ToArray();
            var pair=jets.Length==0?null:d.Data.Design.Pairs.Single(p=>new[]{p.First,p.Second}.Order().SequenceEqual(jets.Order())).Name;
            var motion=c.GetProperty("moving").GetBoolean()?Moving:Rest;var g=new AssemblyGimbal(angles[0],angles[1],angles[0],angles[1]);
            var end=(long)(Rational(c.GetProperty("duration").GetString()!)*1_000_000);var plan=new List<AssemblyCommand>();
            for(long t=0;t<end;t+=15625)plan.Add(new(c.GetProperty("main").GetInt32()!=0,pair,angles[0],angles[1],Math.Min(15625,end-t)));
            var a=Session(d,plan.ToArray(),motion,g);var b=Session(d,plan.ToArray(),motion,g);
            Credit(a,end);Credit(b,end);Drain(a);Drain(b);var actual=Vector(Observe(a).State.Motion);var reference=row.GetProperty("endpoint").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
            var qerr=2*Math.Min(Error(actual,reference,6,4),Math.Sqrt(Enumerable.Range(6,4).Sum(i=>(actual[i]+reference[i])*(actual[i]+reference[i]))));
            Check(Error(actual,reference,0,3)<1e-6&&Error(actual,reference,3,3)<1e-6&&qerr<1e-9&&Error(actual,reference,10,3)<1e-9,"production trajectory vs independently retained Gate0 endpoint: "+name);
            Check(Observe(a)==Observe(b)&&Enumerable.Range(0,plan.Count).All(i=>Record(a,i)==Record(b,i)),"bitwise production replay: "+name);
            count++;
        }
        Check(count==24,"all 24 independent numerical reference trajectories exercised");
    }
    private static void CanonicalHexEncoding()
    {
        // Independent arithmetic construction covers every digit at every nibble
        // position, including leading zeroes and the highest 2176-bit limb.
        const string digits="0123456789abcdef";
        var power=PropellantInteger.FromUInt64(1);
        var expected=new string('0',544).ToCharArray();
        Check(AssemblyResources.Hex(default)==new string(expected),"canonical zero encoding");
        for(var position=543;position>=0;position--)
        {
            for(var digit=1;digit<16;digit++)
            {
                Check(PropellantInteger.TryMultiply(power,(ulong)digit,out var value),"hex oracle digit representable");
                expected[position]=digits[digit];var text=new string(expected);
                Check(AssemblyResources.Hex(value)==text,"canonical fixed-width digit and position");
                Check(AssemblyResources.ParseHex(text)==value,"canonical exact encoding roundtrip");
            }
            expected[position]='0';
            if(position!=0)Check(PropellantInteger.TryMultiply(power,16,out power),"hex oracle place value");
        }
        foreach(var digit in digits)
        {
            var text=new string(digit,544);var value=AssemblyResources.ParseHex(text);
            Check(AssemblyResources.Hex(value)==text,"repeated digits including full maximum");
            Check(Encoding.UTF8.GetString(AssemblyJson.Write(value))=="\""+text+"\"","unchanged exact JSON representation");
        }
        var culture=CultureInfo.CurrentCulture;
        try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("tr-TR");Check(AssemblyResources.Hex(PropellantInteger.FromUInt64(0xabcdef))==new string('0',538)+"abcdef","culture-independent hex");}
        finally{CultureInfo.CurrentCulture=culture;}
    }
    internal static void Run()
    {
        _checks=0;var stock=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.G0B");
        CanonicalHexEncoding();Catalog(stock);MassAndActuators(stock);Resources(stock);Ownership(stock);TypedMotion(stock);SaveResume(stock);SavedCreditContract(stock);CanonicalCreditTrace(stock);Cadence(stock);Trajectories(stock);
        Console.WriteLine($"ASSEMBLY_PRODUCTION PASS checks={_checks} independent_trajectories=24 ordinary_owner=SimulationTransactionEngine");
    }
    internal static void ValidateFourHorn(CompiledAssemblyDesign stock)
    {
        _checks=0;Resources(stock);Ownership(stock);TypedMotion(stock);SaveResume(stock);SavedCreditContract(stock);CanonicalCreditTrace(stock);Cadence(stock);Trajectories(stock);
        Console.WriteLine($"ASSEMBLY_FOUR_HORN_OWNERSHIP PASS checks={_checks} independent_trajectories=24");
    }
}
