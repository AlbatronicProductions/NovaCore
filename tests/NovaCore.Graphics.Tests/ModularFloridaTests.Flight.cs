using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;
using NovaCore.Core;

internal static partial class ModularFloridaTests
{
    internal static void Flight()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            var host=0L;var control=0L;ConstructionRuntimeState? departure=null;
            void Advance(int n)
            {
                for(var i=0;i<n;i++)
                {
                    Need(s.Engine.AdmitConstructionHostTime(s.Authority,++host,new(15625))==ConstructionServiceStatus.AcceptedCredit,"physical host credit");
                    var status=s.Engine.ServiceConstructionDebt(s.Authority,out var count);
                    if(status!=ConstructionServiceStatus.Published)Console.WriteLine($"flight fail long={longer} host={host} status={status} state={s.Engine.State.Revision}");
                    Need(status==ConstructionServiceStatus.Published&&count==1,"one canonical interval "+status);
                    if(departure is null&&Observe(s).Physical!.Consumer==AssemblyPhysicalConsumer.FreeFlight)departure=Observe(s);
                }
            }
            Advance(64);var cold=Observe(s);
            Need(cold.Physical!.Consumer==AssemblyPhysicalConsumer.SupportedContact,"cold contact persists");
            Need(cold.Fuel.Save().SequenceEqual(craft.Fuel.Initial().Save()),"cold no fuel debit");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,++control,new(true,default)).Status==AssemblyControlStatus.Admitted,"ignition admitted");
            var initialServices=s.Binding.Physical!.Services.Advance(cold.Fuel,cold.Power,15625,s.Binding.Physical.Control.Resolve(new(true)).Consumers.AsSpan());
            var firstOracle=AssemblyDynamics.Evaluate(s.Binding.Physical.Control,cold.Physical.Motion,initialServices,cold.Physical.Gimbal,s.Binding.Physical.Control.Resolve(new(true)),site:s.Binding.Physical.Site,epoch:cold.Epoch);
            var native=s.Engine.ConstructionContactWorldForTest(s.Authority)!;native.CaptureCraftDiagnosticsForTest();
            Advance(1);var first=Observe(s);var contactP=Norm(first.Physical!.Motion.PositionO-firstOracle.Motion.PositionO);var contactV=Norm(first.Physical.Motion.VelocityO-firstOracle.Motion.VelocityO);
            Console.WriteLine($"CONTACT_ORACLE long={longer} p={contactP:R} v={contactV:R} actual={first.Physical.Motion.VelocityO} oracle={firstOracle.Motion.VelocityO}");
            Need(contactP<s.Binding.Physical.Contact.ContactTolerance,"native first-order unloading position stays inside contact tolerance");
            var contactVelocity=Double3.Zero;var residualMaximum=0d;var nativeIndex=0;
            foreach(var sample in native.CraftDiagnosticsForTest)
            {
                var mass=(longer?2164:1304)-10*(++nativeIndex-.5)/1024;
                Need(Math.Abs(sample.Mass.Mass-mass)<1e-10,"independent midpoint mass at native solve");
                var gravity=s.Binding.Physical.Site.LinearAcceleration(sample.Frame,sample.Position,sample.Before);
                var thrust=sample.Orientation.Rotate(new Double3(30720,0,0));
                var recovered=(sample.After-sample.Before)*mass-sample.ContactImpulse;
                var expected=(gravity*mass+thrust)*sample.Seconds;
                var residual=Norm(recovered-expected);residualMaximum=Math.Max(residualMaximum,residual);
                var rounding=64*Math.ScaleB(1d,-23)*(mass*Norm(sample.Before)+Norm(expected)+Norm(sample.ContactImpulse));
                Need(residual<rounding&&Norm(recovered-gravity*mass*sample.Seconds)>30000*sample.Seconds,"actual solved impulse balance recovers installed thrust and rejects omitted thrust");
                contactVelocity+=sample.ContactImpulse/mass;
            }
            Need(nativeIndex==16&&Norm(first.Physical.Motion.VelocityO-firstOracle.Motion.VelocityO-contactVelocity)<1e-6,"resting penetration recovery explains ignition velocity difference");
            Console.WriteLine($"CONTACT_IMPULSE long={longer} residualNs={residualMaximum:R} recoveryVelocity={Norm(contactVelocity):R} slices={nativeIndex}");
            Advance(1279);var flight=Observe(s);
            Console.WriteLine($"flight long={longer} consumer={flight.Physical!.Consumer} p={flight.Physical.Motion.PositionO} v={flight.Physical.Motion.VelocityO}");
            Need(flight.Physical.Consumer==AssemblyPhysicalConsumer.FreeFlight&&flight.Physical.Motion.PositionO.Y>1,"thrust-driven departure");
            Need(s.Engine.ConstructionContactWorldForTest(s.Authority) is null,"retired private contact at handoff");
            Need(departure is not null&&departure.Epoch.Ticks<2_000000L,"finite early physical handoff");
            var oracle=FlightOracle(s,departure!,flight);var actual=s.Binding.Physical.Site.ToEarth(flight.Physical.Motion,flight.Epoch);
            var dp=Norm(actual.PositionO-oracle.PositionO);var dv=Norm(actual.VelocityO-oracle.VelocityO);
            Console.WriteLine($"FLIGHT_ORACLE long={longer} position={dp:R} velocity={dv:R} contactPosition={contactP:R} contactVelocity={contactV:R} handoffTicks={departure!.Epoch.Ticks}");
            Need(dp<1e-4&&dv<1e-7,"long arc agrees with independent inertial RK4");
            var kg=ConstructionNumerics.Quantities(flight.Fuel);var initialKg=ConstructionNumerics.Quantities(craft.Fuel.Initial());
            for(var i=0;i<kg.Length;i++)Need(kg[i]==initialKg[i]-(craft.Fuel.Stores[i].Resource.EndsWith("a",StringComparison.OrdinalIgnoreCase)?80:120),"20-second exact fuel mass budget");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control.Identity,++control,new(false,default)).Status==AssemblyControlStatus.Admitted,"cutoff admitted");
            var fuel=flight.Fuel.Save();Advance(640);var coast=Observe(s);
            Need(coast.Fuel.Save().SequenceEqual(fuel),"cutoff stops fuel");
            Need(coast.Physical!.Motion.VelocityO!=flight.Physical.Motion.VelocityO&&coast.Physical.Motion.PositionO!=flight.Physical.Motion.PositionO,"coast continues gravity/motion");
            Need(coast.Sequence>256&&s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records.Length<=256,"continuous rolling history");
        }
        FlightBoundaries(catalog,terrain);
        FlightControls(catalog,terrain);
        FlightClearance(catalog,terrain);
        Console.WriteLine($"MODULAR_GATE10_FLIGHT_PASS checks={checks}");
    }
}
