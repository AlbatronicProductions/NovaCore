using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void PhysicalServicesGate()
    {
        checks=0;var catalog=StarterCatalog();
        static BigInteger Q(double kg)=>ConstructionFuelNetwork.Decode(kg,true);
        static bool Equal(ConstructionRatio value,BigInteger n,BigInteger d)=>value.Numerator*d==n*value.Denominator;
        static ConstructionRatio Sum(IEnumerable<ConstructionRatio> values){BigInteger n=0,d=1;foreach(var x in values){n=n*x.Denominator+x.Numerator*d;d*=x.Denominator;var g=BigInteger.GreatestCommonDivisor(n,d);n/=g;d/=g;}return new(n,d);}
        foreach(var longer in new[]{false,true}){
            var source=StarterCraft(catalog,longer);var bytes=source.Save();var craft=CraftCompiler.Compile(catalog,source.Data,"assets/vehicles/modular-starter");
            var physical=new ConstructionPhysicalServices(craft);var allocation=new CompiledCraftControl(craft);var row=allocation.Resolve(new(true));
            var initialFuel=craft.Fuel.Initial();var initialPower=craft.Power.Initial();var fuelSave=initialFuel.Save();var powerSave=initialPower.Save();
            var battery=craft.Power.Modules.FindIndex(m=>m.Role==ElectricalRole.Battery);var main=craft.Actuators.Single(a=>a.Gimbal is not null);
            var full=physical.Advance(initialFuel,initialPower,15625,row.Consumers.AsSpan());
            Check(full.Phases.Length==1&&Equal(Sum(full.Phases.Select(p=>p.Ticks)),15625,1),"full physical interval coverage");
            Check(Equal(full.Fuel.InQ(0),initialFuel.InQ(0).Numerator*16-Q(1),16)&&Equal(full.Fuel.InQ(1),initialFuel.InQ(1).Numerator*32-Q(3),32),"independent main mixture 4/6 kg per second");
            Check(Equal(full.Power.InQ(battery),Q(90000)*8-Q(7),8),"56 W continuous electrical debit");
            var halfJ=ConstructionPowerState.Create(craft.Power,initialPower.Charge.SetItem(battery,Q(.5)),1,initialPower.Active,initialPower.Cursor,0);
            var outage=physical.Advance(initialFuel,halfJ,15625,row.Consumers.AsSpan());
            Check(outage.Phases.Length==2&&Equal(outage.Phases[0].Ticks,1_000_000,112)&&Equal(outage.Phases[1].Ticks,46875,7),"half-joule power cut exactly 1/112 second with complete coast tail");
            Check(Equal(outage.Fuel.InQ(0),initialFuel.InQ(0).Numerator*28-Q(1),28)&&Equal(outage.Fuel.InQ(1),initialFuel.InQ(1).Numerator*56-Q(3),56),"no fuel beyond exact electrical boundary");
            Check(outage.Power.Charge[battery]==0&&craft.Power.Modules.Select((m,i)=>(m,i)).Where(x=>x.m.Role==ElectricalRole.Load).All(x=>!outage.Power.Active[x.i]),"all fixed bus loads stop at outage");
            var coast=physical.Advance(outage.Fuel,outage.Power,15625,row.Consumers.AsSpan());
            Check(coast.Phases.Length==1&&coast.Phases[0].Active.All(a=>!a)&&coast.Fuel.Save().SequenceEqual(outage.Fuel.Save()),"latched intent cannot debit without power; coast covers time");
            var exactEnd=ConstructionPowerState.Create(craft.Power,initialPower.Charge.SetItem(battery,Q(.875)),1,initialPower.Active,initialPower.Cursor,0);
            var endpoint=physical.Advance(initialFuel,exactEnd,15625,row.Consumers.AsSpan());
            Check(endpoint.Phases.Length==1&&endpoint.Power.Charge[battery]==0&&endpoint.Power.Active.Where((_,i)=>craft.Power.Modules[i].Role==ElectricalRole.Load).All(a=>!a),"outage at exact host endpoint disables now");
            var first=physical.Advance(initialFuel,halfJ,8000,row.Consumers.AsSpan());var second=physical.Advance(first.Fuel,first.Power,7625,row.Consumers.AsSpan());
            Check(second.Fuel.Save().SequenceEqual(outage.Fuel.Save())&&second.Power.Save().SequenceEqual(outage.Power.Save()),"host fragmentation preserves exact final inventories");
            var empty=ConstructionFuelState.Create(craft.Fuel,[0,initialFuel.Quantities[1]],1,0);
            var noMixture=physical.Advance(empty,initialPower,15625,row.Consumers.AsSpan());
            Check(noMixture.Phases.Length==1&&noMixture.Phases[0].Active.All(x=>!x)&&noMixture.Fuel.Save().SequenceEqual(empty.Save()),"one missing reactant produces no thrust/debit and full coast");
            var tiny=ConstructionFuelState.Create(craft.Fuel,[Q(.03125)*craft.Fuel.Scale,initialFuel.Quantities[1]],1,0);
            var depleted=physical.Advance(tiny,halfJ,15625,row.Consumers.AsSpan());
            Check(depleted.Phases.Length==3&&Equal(depleted.Phases[0].Ticks,15625,2)&&depleted.Fuel.Quantities[0]==0&&Equal(Sum(depleted.Phases.Select(p=>p.Ticks)),15625,1),"fuel event precedes power event without missing either coast portion");
            var load=craft.Power.Modules.FindIndex(m=>m.Role==ElectricalRole.Load);
            Reject(()=>physical.Advance(initialFuel,initialPower.SetLoadActive(load,false),1,row.Consumers.AsSpan()),"partial load change invalidates fixed physical profile");
            Reject(()=>ConstructionFuelSolver.AdvanceExact(initialFuel,new(1,3),row.Consumers.AsSpan()),"off-lattice duration refuses");
            Reject(()=>ConstructionFuelSolver.AdvanceExact(initialFuel,new(-1,1),row.Consumers.AsSpan()),"negative exact time refuses");
            Reject(()=>ConstructionFuelSolver.AdvanceExact(initialFuel,new(BigInteger.One<<(craft.Fuel.TimeNumeratorBits+1),1),row.Consumers.AsSpan()),"oversized numerator refuses before multiplication");
            Check(initialFuel.Save().SequenceEqual(fuelSave)&&initialPower.Save().SequenceEqual(powerSave)&&source.Save().SequenceEqual(bytes),"proposals and refusals preserve source state/document");
            var oldDigest=AssemblyJson.Digest(new{Design=craft.Design.Digest,Unavailable=Array.Empty<string>()});
            var oldSave=System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(fuelSave).Replace(craft.Fuel.Digest,oldDigest,StringComparison.Ordinal));
            Reject(()=>ConstructionFuelState.Load(craft.Fuel,oldSave),"pre-lattice snapshot cannot be silently reinterpreted");
            var start=new AssemblyMotion(default,new(4,5,6),DoubleQuaternion.Identity,new(1,0,0));
            var advanced=AssemblyDynamics.Evaluate(allocation,start,full,default,row);
            var h=.015625;var fraction=10*h/craft.InitialMass.Mass;var v=0d;var x=0d;var power=fraction;
            for(var term=1;term<=12;term++){v+=3072*power/term;x+=3072*h*power/(term*(term+1));power*=fraction;}
            Check((advanced.Motion.AngularVelocityBody-start.AngularVelocityBody).LengthSquared<1e-25,"co-moving finite-volume depletion has no artificial spin acceleration");
            Check((advanced.Motion.VelocityO-(start.VelocityO+new Double3(v,0,0))).LengthSquared<1e-24&&
                (advanced.Motion.PositionO-(start.VelocityO*h+new Double3(x,0,0))).LengthSquared<1e-24,"independent rocket-equation velocity and integrated displacement");
            var powerMotion=AssemblyDynamics.Evaluate(allocation,start,outage,default,row,new(-9.81,0,0));
            var burn=1d/112;fraction=10*burn/craft.InitialMass.Mass;v=0;x=0;power=fraction;
            for(var term=1;term<=12;term++){v+=3072*power/term;x+=3072*burn*power/(term*(term+1));power*=fraction;}
            Check(Math.Abs(powerMotion.Motion.VelocityO.X-(4+v-9.81*h))<1e-12&&Math.Abs(powerMotion.Motion.PositionO.X-(4*h+x+v*(h-burn)-9.81*h*h/2))<1e-12,
                "power cut retains momentum and gravity through remaining physical interval");
            var genericStart=new AssemblyMotion(new(1,2,3),new(4,5,6),DoubleQuaternion.FromAxisAngle(new Double3(1,2,3).Normalized(),.3),new(.2,-.15,.1));
            var command=allocation.Resolve(new(true,new(1,-1,1)));var active=physical.Advance(initialFuel,initialPower,15625,command.Consumers.AsSpan());
            var regular=AssemblyDynamics.Evaluate(allocation,genericStart,active,default,command,new(0,-9.81,0));
            var fine=AssemblyDynamics.Evaluate(allocation,genericStart,active,default,command,new(0,-9.81,0),3);
            Check((regular.Motion.PositionO-fine.Motion.PositionO).LengthSquared<1e-22&&(regular.Motion.VelocityO-fine.Motion.VelocityO).LengthSquared<1e-21&&
                (regular.Motion.AngularVelocityBody-fine.Motion.AngularVelocityBody).LengthSquared<1e-22,"independent step-refinement rotating/variable tensor and gimbal response");
            foreach(var fill in new[]{0d,.125,.5,1}){
                var successor=craft.Mass.Evaluate(craft.Mass.Stores.Select(s=>s.CapacityKg*fill).ToArray());
                var original=AssemblyContactProfile.ToCom(genericStart,craft.InitialMass.Com);
                var delta=genericStart.BodyToWorld.Rotate(successor.Com-craft.InitialMass.Com);
                var shifted=AssemblyContactProfile.ToOrigin(original.Position+delta,original.Velocity+Double3.Cross(genericStart.BodyToWorld.Rotate(genericStart.AngularVelocityBody),delta),genericStart.BodyToWorld,genericStart.AngularVelocityBody,successor.Com);
                Check((shifted.PositionO-genericStart.PositionO).LengthSquared<1e-27&&(shifted.VelocityO-genericStart.VelocityO).LengthSquared<1e-27,"COM recenter preserves material origin pose and velocity across unequal distributions");
            }
            for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++){
                var request=new AssemblyControlRequest(false,new(pitch,yaw,roll));var resolved=allocation.Resolve(request);
                var w=AssemblyActuation.Resolve(craft,resolved.Consumers.AsSpan(),default);var tau=w.MomentAtOrigin-Double3.Cross(craft.InitialMass.Com,w.Force);
                Check(w.Force.LengthSquared<1e-20&&(pitch==0?Math.Abs(tau.Y)<1e-10:tau.Y*pitch>0)&&(yaw==0?Math.Abs(tau.Z)<1e-10:tau.Z*yaw>0)&&(roll==0?Math.Abs(tau.X)<1e-10:tau.X*roll>0),"physical 32-jet simultaneous request signs/net translation");
                var powered=allocation.Resolve(request with {MainOn=true});var g=new AssemblyGimbal(powered.TargetY,powered.TargetZ,powered.TargetY,powered.TargetZ);
                var mainW=AssemblyActuation.Resolve(craft,powered.Consumers.AsSpan(),g);tau=mainW.MomentAtOrigin-Double3.Cross(craft.InitialMass.Com,mainW.Force);
                Check((pitch==0?Math.Abs(tau.Y)<1e-9:tau.Y*pitch>0)&&(yaw==0?Math.Abs(tau.Z)<1e-9:tau.Z*yaw>0)&&(roll==0?Math.Abs(tau.X)<1e-9:tau.X*roll>0),"physical gimbal and roll signs");
            }
            var slewed=AssemblyActuation.Next(main,default,-.05,.05,.015625,true);
            Check(slewed.ActualY==-.000625&&slewed.ActualZ==.000625&&AssemblyActuation.Next(main,slewed,.05,-.05,.1,false)==slewed,"gimbal slew and frozen unpowered mechanism");
            Console.WriteLine($"{(longer?"long":"short")} timeBits={craft.Fuel.TimeScale.GetBitLength()} rateBits={craft.Fuel.RateBits} quantityBits={craft.Fuel.QuantityBits} fuelScratch={craft.Fuel.ScratchBits} physicalScratch={physical.ScratchMagnitudeBits}");
            using(var clocked=new ConstructionEditorSession(catalog)){
                clocked.Load(0,source.Save());foreach(var degrees in new[]{0,90,180,270,180,90,0}){
                    clocked.PreviewClock(clocked.Revision,"engine",degrees);clocked.AcceptPreview(clocked.Revision);
                    var compiled=CraftCompiler.Compile(catalog,clocked.Current!.Design.Data,"assets/vehicles/modular-starter");var controls=new CompiledCraftControl(compiled);
                    for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++){
                        var c=controls.Resolve(new(true,new(pitch,yaw,roll)));var wrench=AssemblyActuation.Resolve(compiled,c.Consumers.AsSpan(),new(c.TargetY,c.TargetZ,c.TargetY,c.TargetZ));
                        var tau=wrench.MomentAtOrigin-Double3.Cross(compiled.InitialMass.Com,wrench.Force);
                        Check((pitch==0?Math.Abs(tau.Y)<1e-9:tau.Y*pitch>0)&&(yaw==0?Math.Abs(tau.Z)<1e-9:tau.Z*yaw>0)&&(roll==0?Math.Abs(tau.X)<1e-9:tau.X*roll>0),"legal clock retains physical signed gimbal authority");
                    }
                }
            }
            PhysicalContactChecks(craft);
        }
        var definition=catalog.Data.Definitions.Single(d=>d.Id=="nc.engine.main-1");
        foreach(var zeroSlew in new[]{false,true}){
            var altered=definition with{Construction=definition.Construction! with{Consumers=definition.Construction.Consumers.Select(c=>c with{Gimbal=zeroSlew?c.Gimbal! with{SlewRate=0}:c.Gimbal! with{LimitY=0}}).ToImmutableArray()}};
            var zero=AssemblyDefinitionCatalog.Compile(catalog.Data with{Definitions=catalog.Data.Definitions.Select(d=>d.Id==definition.Id?altered:d).ToImmutableArray()});
            var craft=CraftCompiler.Compile(zero,StarterCraft(zero,false).Data,"assets/vehicles/modular-starter");
            Reject(()=>new CompiledCraftControl(craft),"physical profile refuses zero gimbal authority/slew before rows");
        }
        foreach(var limit in new[]{4d,double.Epsilon}){
            var altered=definition with{Construction=definition.Construction! with{Consumers=definition.Construction.Consumers.Select(c=>c with{Gimbal=c.Gimbal! with{LimitY=limit,LimitZ=limit}}).ToImmutableArray()}};
            var invalid=AssemblyDefinitionCatalog.Compile(catalog.Data with{Definitions=catalog.Data.Definitions.Select(d=>d.Id==definition.Id?altered:d).ToImmutableArray()});
            var craft=CraftCompiler.Compile(invalid,StarterCraft(invalid,false).Data,"assets/vehicles/modular-starter");
            Reject(()=>new CompiledCraftControl(craft),"reversed or numerically absent gimbal authority refuses");
        }
        using(var draft=new ConstructionEditorSession(catalog)){
            draft.Load(0,StarterCraft(catalog,false).Save());draft.Remove(draft.Revision,"adapter");draft.Remove(draft.Revision,draft.Current!.Design.Data.Symmetry.Single().BasePart);
            var craft=CraftCompiler.Compile(catalog,draft.Current!.Design.Data,"assets/vehicles/modular-starter");var net=craft.Fuel;
            var digits=net.QuantityBits/4+2;var malformed="8"+new string('0',digits-1);
            var parsed=BigInteger.Parse(malformed,System.Globalization.NumberStyles.AllowHexSpecifier);
            Check(BigInteger.Abs(parsed).GetBitLength()<=net.ScratchBits&&4*digits<=net.ScratchBits,"incomplete draft malformed signed-hex intermediate is bounded");
            var state=net.Initial();var data=AssemblyJson.Read<ConstructionFuelSave>(state.Save());
            Reject(()=>ConstructionFuelState.Load(net,AssemblyJson.Write(data with{Quantities=data.Quantities.SetItem(0,malformed)})),"oversized signed parser witness refuses without truncation");
            Reject(()=>new ConstructionPhysicalServices(craft),"incomplete draft cannot enter physical service profile");
        }
        var block=catalog.Data.Definitions.Single(d=>d.Id=="nc.rcs.block-r1");
        var sparse=block with{Standard=block.Standard! with{Ports=block.Standard.Ports.AddRange(Enumerable.Range(0,4000).Select(i=>new PartServicePort("isolated-"+i,ConstructionService.Electricity,null,null,null,block.Construction!.Consumers[0].Id,null,false)))}};
        var sparseCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with{Definitions=catalog.Data.Definitions.Select(d=>d.Id==block.Id?sparse:d).ToImmutableArray()});
        var sparseCraft=CraftCompiler.Compile(sparseCatalog,StarterCraft(sparseCatalog,false).Data,"assets/vehicles/modular-starter");
        var sparsePower=sparseCraft.Power.Initial();var sparseBytes=sparsePower.Save();
        Check(sparseCraft.Power.Power.Count==32001&&sparseBytes.Length<=sparseCraft.Power.MaximumSnapshotBytes&&ConstructionPowerState.Load(sparseCraft.Power,sparseBytes).Save().SequenceEqual(sparseBytes),"maximum explicit-bus cursor inventory survives its own bounded save/load");
        Check(ConstructionNumerics.Observe(1,2)==.5&&ConstructionNumerics.Observe(1,3)==1d/3&&ConstructionNumerics.Observe((BigInteger.One<<53)+1,BigInteger.One<<53)==1,"independent rational nearest-even observations");
        Check(ConstructionNumerics.Observe(1,BigInteger.One<<1074)==double.Epsilon&&ConstructionNumerics.Observe(1,BigInteger.One<<1075)==0&&ConstructionNumerics.Observe(3,BigInteger.One<<1075)==2*double.Epsilon,"subnormal exact rounding boundaries");
        PhysicalBoundaryChecks(catalog);
        Console.WriteLine($"Modular Gate 8 physical service checks PASS: {checks}; full gate qualification pending");
    }
    private static void PhysicalBoundaryChecks(AssemblyDefinitionCatalog catalog)
    {
        var craft=CraftCompiler.Compile(catalog,StarterCraft(catalog,false).Data,"assets/vehicles/modular-starter");var services=new ConstructionPhysicalServices(craft);var control=new CompiledCraftControl(craft);
        var fuel=craft.Fuel.Initial();var power=craft.Power.Initial();var on=control.Resolve(new(true));var off=control.Resolve(new(false));
        var zero=services.Advance(fuel,power,0,on.Consumers.AsSpan());Check(zero.Phases.IsEmpty&&zero.Fuel.Save().SequenceEqual(fuel.Save())&&zero.Power.Save().SequenceEqual(power.Save()),"zero physical interval preserves both ledgers");
        var maximum=services.Advance(fuel,power,long.MaxValue,on.Consumers.AsSpan());
        Check(maximum.Fuel.Quantities.All(n=>n==0)&&maximum.Power.Charge.All(n=>n==0)&&maximum.Phases[^1].Active.All(x=>!x),"maximum integer interval remains event bounded with coast tail");
        var canonical=ConstructionFuelSolver.AdvanceExact(fuel,new(1,1),on.Consumers.AsSpan());var equivalent=ConstructionFuelSolver.AdvanceExact(fuel,new(2,2),on.Consumers.AsSpan());
        Check(canonical.State.Save().SequenceEqual(equivalent.State.Save()),"equivalent on-lattice duration representation has exact same inventory");
        var battery=craft.Power.Modules.FindIndex(m=>m.Role==ElectricalRole.Battery);
        Reject(()=>ConstructionPowerState.Create(craft.Power,power.Charge,1,power.Active.SetItem(battery,false),power.Cursor,0),"runtime cannot change immutable battery enablement");
        var coast=services.Advance(fuel,power,15625,off.Consumers.AsSpan());
        foreach(var omega in new[]{10d,100d}){
            var initial=new AssemblyMotion(default,default,DoubleQuaternion.Identity,new(omega,0,0));var end=AssemblyDynamics.Evaluate(control,initial,coast,default,off);
            var exact=DoubleQuaternion.FromAxisAngle(Double3.UnitX,omega*.015625);var probe=end.Motion.BodyToWorld.Rotate(Double3.UnitY)-exact.Rotate(Double3.UnitY);
            Check(Math.Sqrt(probe.LengthSquared)<2e-11&&end.Motion.AngularVelocityBody.X==omega,"adaptive axial-spin oracle at high admitted angular rate");
        }
        foreach(var omega in new[]{1000d,5000d})Reject(()=>AssemblyDynamics.Evaluate(control,new(default,default,DoubleQuaternion.Identity,new(omega,0,0)),coast,default,off),"unresolved high-rate numerical work refuses bounded preparation");
        var request=control.Resolve(new(true,new(-1,0,0)));var active=services.Advance(fuel,power,15625,request.Consumers.AsSpan());var near=new AssemblyGimbal(.0499,0,.05,0);
        var normal=AssemblyDynamics.Evaluate(control,new(default,default,DoubleQuaternion.Identity,default),active,near,request);
        var refined=AssemblyDynamics.Evaluate(control,new(default,default,DoubleQuaternion.Identity,default),active,near,request,refinement:5);
        Check((normal.Motion.VelocityO-refined.Motion.VelocityO).LengthSquared<1e-24&&normal.Gimbal.ActualY==.05,"adaptive split resolves gimbal target arrival inside physical interval");
        var engine=catalog.Data.Definitions.Single(d=>d.Id=="nc.engine.main-1");var e=engine.Construction!;var s=engine.Standard!;
        var load=e.Electrical.Single(m=>m.Role==ElectricalRole.Load);var loadPort=s.Ports.Single(p=>p.Electrical==load.Id);
        var separate=engine with{Construction=e with{Electrical=e.Electrical.Add(new("local-battery",ElectricalRole.Battery,1,0,null))},Standard=s with{
            Ports=s.Ports.Add(new("local-power",ConstructionService.Electricity,null,null,null,null,"local-battery",false)),
            Routes=s.Routes.Where(r=>r.From!=loadPort.Id&&r.To!=loadPort.Id).Append(new PartInternalRoute("local-delivery","local-power",loadPort.Id,true)).ToImmutableArray()}};
        var twoCatalog=AssemblyDefinitionCatalog.Compile(catalog.Data with{Definitions=catalog.Data.Definitions.Select(d=>d.Id==engine.Id?separate:d).ToImmutableArray()});
        foreach(var tied in new[]{false,true}){
            var document=StarterCraft(twoCatalog,false).Data;document=document with{Configuration=document.Configuration.Select(c=>c with{Electrical=c.Electrical.Select(m=>
                c.Part=="core"&&m.ChargeJ>0?m with{ChargeJ=tied?46d/128:3d/16}:c.Part=="engine"&&m.Module=="local-battery"?m with{ChargeJ=tied?10d/128:1d/16}:m).ToImmutableArray()}).ToImmutableArray()};
            var two=CraftCompiler.Compile(twoCatalog,document,"assets/vehicles/modular-starter");var profile=new ConstructionPhysicalServices(two);var row=new CompiledCraftControl(two).Resolve(new(true));
            var start=two.Fuel.Initial();var end=profile.Advance(start,two.Power.Initial(),15625,row.Consumers.AsSpan());
            var seconds=tied?new ConstructionRatio(1,128):new ConstructionRatio(3,736);var q=ConstructionFuelNetwork.Decode(1,true);
            var removed=ConstructionRatio.Create(start.InQ(0).Numerator*end.Fuel.InQ(0).Denominator-end.Fuel.InQ(0).Numerator*start.InQ(0).Denominator,start.InQ(0).Denominator*end.Fuel.InQ(0).Denominator);
            Check(end.Phases.Length==(tied?2:3)&&removed.Numerator*seconds.Denominator==q*4*seconds.Numerator*removed.Denominator,"independent bus cut ordering/tie and minimum required delivery gate");
            var a=profile.Advance(start,two.Power.Initial(),4000,row.Consumers.AsSpan());var b=profile.Advance(a.Fuel,a.Power,11625,row.Consumers.AsSpan());
            Check(end.Fuel.Save().SequenceEqual(b.Fuel.Save())&&end.Power.Save().SequenceEqual(b.Power.Save()),"multiple electrical buses preserve exact host fragmentation");
        }
    }
}
