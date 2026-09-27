using System.Collections.Immutable;
using System.Reflection;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.ReferenceFrames;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    private const string Assets="assets/vehicles/modular-starter";
    private static int checks;
    private static void Need(bool pass,string why){checks++;if(!pass)throw new InvalidDataException("Modular Florida: "+why);}
    private static void Refuse(Action action,string why){try{action();}catch(InvalidDataException){checks++;return;}throw new InvalidOperationException("Modular Florida accepted: "+why);}
    private static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    private static double Norm(Double3 p)=>Math.Sqrt(p.LengthSquared);
    private static (IPhysicalSurfacePointQuery Query,FloridaSlabSupport Slab) Terrain()
    {
        PlanetaryPhysicalSurface.ConfigureRuntimeGeneration(PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
        var root=GraphicsTestHarness.RepositoryPath();
        Need(EarthElevationDataset.TryLoad(Path.Combine(root,"assets","earth","runtime"),out var error),error);
        Need(TerrainAssetCache.TryResolveRequired(root,TerrainAssetCache.ProductionEarthLocalAssetId,null,out _,out var path,out error),error);
        Need(EarthLocalTerrainElevationDataset.TryLoad(path,out error),error);
        Need(PlanetaryPhysicalSurfacePointQuery.TryAcquire(6,root,out var query)==PhysicalSurfaceQueryStatus.Ready,"current physical Florida dataset");
        Need(FloridaLaunchSite.TryCreate(6,query!.Authority.ReferenceRadiusMetres,PlanetaryTerrainDefinition.EarthProductionCubeV5,out var authored),"authored Florida slab");
        return(query,authored.CreateSupportSlab(query));
    }
    private static ConstructionRuntimeState Observe(ConstructionApplicationSession s)
    {Need(s.Engine.ObserveConstructionServices(s.Authority,out var state)==ConstructionServiceStatus.Ready,"owned observation");return state!;}
    internal static void Admission()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));var catalogBefore=catalog.Save();
        foreach(var longer in new[]{false,true})foreach(var ticks in new[]{0L,123456789L})
        {
            var document=Craft(catalog,longer);var bytes=document.Save();var compiled=CraftCompiler.Compile(catalog,document.Data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(compiled,terrain.Query,terrain.Slab,new(ticks));
            var state=Observe(s);var binding=s.Binding;var physical=binding.Physical!;var site=physical.Site;var originalClock=s.Engine.CaptureContinuationClock();
            Need(ReferenceEquals(binding.Design,compiled.Design)&&ReferenceEquals(binding.Fuel,compiled.Fuel)&&ReferenceEquals(binding.Power,compiled.Power),"exact compiled source and service identities");
            Need(binding.Parts.Length==12&&binding.Actuators.Length==33&&s.Engine.State.Spacecraft.Count==1,"one authored twelve-instance craft, 32 jets and main");
            Need(state.ReferenceMass==compiled.InitialMass&&state.ReferenceMass!.Value.Mass==(longer?2164:1304),"authored cold mass");
            Need(state.Fuel.Save().SequenceEqual(compiled.Fuel.Initial().Save())&&state.Power.Save().SequenceEqual(compiled.Power.Initial().Save()),"exact authored inventory and battery");
            Need(state.Physical!.Motion==new AssemblyMotion(default,default,AssemblyContactProfile.Upright,default)&&state.Physical.Gimbal==default&&state.Physical.Consumer==AssemblyPhysicalConsumer.SupportedContact,"cold material origin and actuator state");
            Need(s.Engine.ObserveAssemblyControl(s.Control!,out var control)==AssemblyControlStatus.Ready&&control.Requested==default&&control.AdmissionCount==0&&control.Identity.Vessel==binding.Spacecraft.Id&&control.Identity.Generation==binding.Identity.Generation,"same M15.5 authority, cold neutral");
            Need(!s.Engine.State.Spacecraft.TryGetAssembly(binding.Spacecraft.Id,out _,out _),"no hidden stock registration");
            Need(SpacecraftMotionEvaluator.TryEvaluate(s.Engine.State,binding.Spacecraft.Id,state.Epoch,out _)==SpacecraftTranslationStatus.AssemblyMotionRequiresTypedView,"generic COM propagation cannot reinterpret material origin");
            Need(SpacecraftMotionEvaluator.TryEvaluateAssembly(s.Engine.State,binding.Spacecraft.Id,state.Epoch,out var inertial)==SpacecraftTranslationStatus.Success,"canonical typed physical endpoint");
            Need(SpacecraftMotionEvaluator.TryEvaluateAssembly(s.Engine.State,binding.Spacecraft.Id,new(ticks+1),out _)==SpacecraftTranslationStatus.OutsideQualifiedEndpoint,"no guessed future motion");
            Need(site.End.Ticks-ticks>86400_000000L&&site.At(new(ticks+21_000_000)).Epoch.Ticks==ticks+21_000_000,"coverage continues beyond diagnostic episode");
            Need(Norm(site.OriginBodyFixed+FloridaFacilitySupport.Region.Up*site.SupportPlane-terrain.Slab.TopBodyFixed)<1e-9,"support offset derives from actual foot plane");
            var native=s.Engine.ConstructionContactWorldForTest(s.Authority)!;var config=Field<LocalContactConfiguration>(native,"configuration");
            var simulation=Field<BepuPhysics.Simulation>(native,"simulation");var body=simulation.Bodies[Field<BepuPhysics.BodyHandle>(native,"body")];
            var shape=Field<BepuPhysics.Collidables.TypedIndex>(native,"bodyShape");var compound=simulation.Shapes.GetShape<BepuPhysics.Collidables.Compound>(shape.Index);
            Need(compound.Children.Length==compiled.Collision.Length&&compound.Children.Length==69&&config.Site==site&&config.CraftProfile==physical.Contact,"same canonical contact owner contains every authored region");
            Need(body.Pose.Orientation==Quaternion.Identity&&body.Velocity.Linear==Vector3.Zero&&body.Velocity.Angular==Vector3.Zero,"cold native orientation and velocities");
            Need(Math.Abs(1d/body.LocalInertia.InverseMass-state.ReferenceMass!.Value.Mass)<.0001,"native mass matches exact inventory-derived value");
            for(var childIndex=0;childIndex<compound.Children.Length;childIndex++)
            {
                var child=compound.Children[childIndex];ref var hull=ref simulation.Shapes.GetShape<BepuPhysics.Collidables.ConvexHull>(child.ShapeIndex.Index);
                hull.ComputeBounds(Quaternion.Identity,out var low,out var high);low+=child.LocalPosition+body.Pose.Position;high+=child.LocalPosition+body.Pose.Position;
                var points=compiled.Collision[childIndex].Vertices.Select(v=>new Double3(-v.Y,v.X-site.SupportPlane,v.Z)).ToArray();
                Need(Norm(new Double3(low.X,low.Y,low.Z)-new Double3(points.Min(v=>v.X),points.Min(v=>v.Y),points.Min(v=>v.Z)))<2e-6&&
                    Norm(new Double3(high.X,high.Y,high.Z)-new Double3(points.Max(v=>v.X),points.Max(v=>v.Y),points.Max(v=>v.Z)))<2e-6,"every actual native hull preserves authored world bounds");
            }
            foreach(var foot in compiled.Support)
            {
                var p=foot.MaterialFrame.Position;var relative=state.Physical.Motion.BodyToWorld.Rotate(p);
                Need(Math.Abs(relative.Y-site.SupportPlane)<1e-12,"each authored foot reaches its derived local support plane");
                var bodyFixed=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(relative);
                Need(Math.Abs(Double3.Dot(bodyFixed-terrain.Slab.TopBodyFixed,FloridaFacilitySupport.Region.Up))<2e-9,"each foot lies on authenticated rotating slab");
            }
            var frames=new ReferenceFrameEvaluation[s.Frames.Count];
            Need(SpacecraftReferenceFrameEvaluator.TryEvaluate(s.Engine.State.Spacecraft,s.Frames,state.Epoch,frames)==SpacecraftReferenceFrameEvaluationStatus.Success,"canonical site/body frame extraction");
            var expected=IndependentEarth(site,state.Epoch);
            Need(Norm(expected.Position-inertial.MaterialOriginMotion.PositionO)<5e-9&&Norm(expected.Velocity-inertial.MaterialOriginMotion.VelocityO)<1e-10,"independent rotating-Earth matrix/derivative transport");
            var absoluteOmega=inertial.MaterialOriginMotion.BodyToWorld.Rotate(inertial.MaterialOriginMotion.AngularVelocityBody);
            Need(Norm(expected.Omega-absoluteOmega)<1e-18&&Norm(inertial.MaterialVelocityAtCurrentComRoot-Double3.Cross(expected.Omega,inertial.CenterOfMassPositionRoot))<1e-10,"COM rigid velocity includes Earth angular transport");
            foreach(var axis in new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ})
                Need(Norm(inertial.MaterialOriginMotion.BodyToWorld.Rotate(axis)-expected.BodyRotation.Apply(axis))<1e-14,"independent material-body frame orientation");
            Need(s.Engine.BeginConstructionServices(binding,1,out _)==ConstructionServiceStatus.InvalidInput,"static owner cannot bind physical craft");
            Need(s.Engine.AdvanceConstructionServices(s.Authority,0,new(1,Enumerable.Repeat(false,33).ToImmutableArray(),[]))==ConstructionServiceStatus.InvalidInput,"static service route cannot strip physical state");
            Refuse(()=>s.Engine.SaveConstructionServices(s.Authority),"static save cannot silently drop physical state");
            Refuse(()=>SpacecraftStateStore.CreateConstruction(binding,s.Frames),"binding cannot register twice");
            Need(Task.Run(()=>s.Engine.ObserveConstructionServices(s.Authority,out _)).GetAwaiter().GetResult()==ConstructionServiceStatus.WrongOwnerThread,"foreign thread refusal");
            Need(Task.Run(()=>s.Engine.ObserveAssemblyControl(s.Control!,out _)).GetAwaiter().GetResult()==AssemblyControlStatus.WrongOwnerThread,"control foreign thread refusal");
            var phaseOwner=new object();Need(s.Clock.PublicationPhase.TryEnter(phaseOwner),"test publication phase");
            try{Need(s.Engine.ObserveConstructionServices(s.Authority,out _)==ConstructionServiceStatus.Reentrant&&s.Engine.ObserveAssemblyControl(s.Control!,out _)==AssemblyControlStatus.Reentrant,"no reentrant competing owner");}
            finally{s.Clock.PublicationPhase.Exit();}
            Need(ReferenceEquals(Observe(s),state)&&s.Engine.CaptureContinuationClock()==originalClock&&s.Engine.State.Revision.Value==0,"all refusals preserve canonical source");
            using(var other=ConstructionApplicationSession.CreateSupported(compiled,terrain.Query,terrain.Slab,new(ticks)))
            {
                Need(other.Binding.Identity!=binding.Identity&&other.Binding.Parts.All(p=>p.Vehicle==other.Binding.Identity),"fresh vessel and instance lifetime");
                Need(s.Engine.AdmitAssemblyControl(other.Control!,control.Identity,1,new(true)).Status==AssemblyControlStatus.InvalidAuthority,"foreign capability cannot use copied identity");
                Need(s.Engine.AdmitAssemblyControl(s.Control!,other.Control!.Identity,1,new(true)).Status==AssemblyControlStatus.InvalidIdentity,"wrong generation refused");
            }
            if(ticks==0)Camera(s);
            Need(s.Engine.AdmitAssemblyControl(s.Control!,control.Identity,1,new(true)).Status==AssemblyControlStatus.Admitted,"engine request admitted without changing physical cold source");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,control.Identity,1,new(true)).Status==AssemblyControlStatus.Duplicate,"identical retry returns admission");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,control.Identity,1,new(false)).Status==AssemblyControlStatus.InvalidSequence,"altered retry refused");
            Need(s.Engine.AdmitAssemblyControl(s.Control!,control.Identity,2,new(false)).Status==AssemblyControlStatus.Admitted,"cutoff admission");
            var off=s.Engine.AdmitAssemblyControl(s.Control!,control.Identity,3,new(true));Need(off.Status==AssemblyControlStatus.Admitted&&!off.Admission.Effective.MainOn,"OFF wins within same physical frontier");
            Need(ReferenceEquals(Observe(s),state)&&s.Engine.CaptureContinuationClock()==originalClock,"input admissions are not physical updates");
            s.Clock.Pause();Need(s.Engine.ObserveConstructionServices(s.Authority,out _)==ConstructionServiceStatus.StaleSource,"ordinary external clock change cannot rebind authority");
            Need(document.Save().SequenceEqual(bytes)&&catalog.Save().SequenceEqual(catalogBefore),"source document and catalog immutable");
            Console.WriteLine($"MODULAR_FLORIDA_COLD mass={state.ReferenceMass!.Value.Mass:R} support={site.SupportPlane:R} craft={compiled.Digest} site={site.Digest} earthP={Norm(expected.Position-inertial.MaterialOriginMotion.PositionO):R} earthV={Norm(expected.Velocity-inertial.MaterialOriginMotion.VelocityO):R} nativeBytes={native.PoolBytes}");
        }
        var original=Craft(catalog,false);var moved=original.Data with {Instances=original.Data.Instances.Select(p=>p with {Pose=p.Pose with {Position=p.Pose.Position+new Double3(30,-40,17)}}).ToImmutableArray()};
        var rebased=CraftCompiler.Compile(catalog,moved,Assets);
        using(var s=ConstructionApplicationSession.CreateSupported(rebased,terrain.Query,terrain.Slab))
        {
            Need(s.Binding.Initial.ReferenceMass==rebased.InitialMass&&Math.Abs(s.Binding.Physical!.Contact.SupportPlane+3.3)<1e-12,"design workspace translation never becomes launch pose/support offset");
            foreach(var a in rebased.Actuators)Need(s.Binding.Actuators.Single(v=>v.Part.DesignInstance==a.Key.Part&&v.LocalId==a.Key.Consumer).ForcePoint==a.Point,"runtime actuator index uses compiled material origin");
        }
        var empty=original.Data with {Configuration=original.Data.Configuration.Select(c=>c with {Stores=c.Stores.Select(v=>v with {QuantityKg=0}).ToImmutableArray()}).ToImmutableArray()};
        var noFuel=CraftCompiler.Compile(catalog,empty,Assets);Refuse(()=>ConstructionApplicationSession.CreateSupported(noFuel,terrain.Query,terrain.Slab),"nonfunctional empty craft refuses cold admission");
        var admitted=CraftCompiler.Compile(catalog,original.Data,Assets);
        Refuse(()=>ConstructionApplicationSession.CreateSupported(admitted,new UntrustedQuery(terrain.Query),terrain.Slab),"copied public terrain identity cannot grant graded-plane capability");
        var stale=new MutableProof(terrain.Query);
        using(var s=ConstructionApplicationSession.CreateSupported(admitted,stale,terrain.Slab))
        {
            var before=Observe(s);var world=s.Engine.ConstructionContactWorldForTest(s.Authority)!;var config=Field<LocalContactConfiguration>(world,"configuration");
            var storage=Field<object>(s.Engine,"_construction");var receipt=Field<LocalContactWorld.Receipt>(storage,"ContactReceipt");
            Need(world.Read(s.Engine,config,receipt,out _) == LocalContactStatus.Success,"issued initial contact receipt");
            Need(world.Read(s.Engine,config,new(world,0),out _) == LocalContactStatus.GenerationMismatch,"forged contact receipt refused");
            stale.Changed=true;
            Need(s.Engine.ObserveConstructionServices(s.Authority,out _) == ConstructionServiceStatus.StaleSource&&world.Read(s.Engine,config,receipt,out _)==LocalContactStatus.ChangedAuthority,"changed terrain invalidates canonical and private contact observations");
            Need(s.Engine.RetireConstructionServices(s.Authority)==ConstructionServiceStatus.Retired,"stale physical owner can retire and release private world");
            Need(world.Read(s.Engine,config,receipt,out _)==LocalContactStatus.Disposed&&s.Engine.ObserveAssemblyControl(s.Control!,out _)==AssemblyControlStatus.Retired&&
                s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(true)).Status==AssemblyControlStatus.Retired,"retired contact/control capabilities cannot revive");
            Need(s.Engine.State.Spacecraft.TryGetConstruction(s.Binding.Spacecraft.Id,out _,out var after)&&ReferenceEquals(before,after),"retirement leaves final canonical snapshot immutable");
        }
        foreach(var longer in new[]{false,true})foreach(var degrees in new[]{0,90,180,270})foreach(var partial in new[]{false,true})
        {
            using var editor=new ConstructionEditorSession(catalog);editor.Load(0,Craft(catalog,longer).Save());editor.PreviewClock(editor.Revision,"engine",degrees);editor.AcceptPreview(editor.Revision);
            var data=editor.Current!.Design.Data;var configured=data with {Configuration=data.Configuration.Select(c=>c with {
                Stores=c.Stores.Select((v,i)=>v with {QuantityKg=v.QuantityKg*(partial?(i==0?.125:.375):1)}).ToImmutableArray(),
                Electrical=c.Electrical.Select(v=>v with {ChargeJ=v.ChargeJ*.425}).ToImmutableArray()}).ToImmutableArray()};
            var variant=CraftCompiler.Compile(catalog,configured,Assets);var before=variant.Design.Save();
            Console.WriteLine($"MODULAR_GATE9_VARIANT longer={longer} clock={degrees} partial={partial}");
            using var s=ConstructionApplicationSession.CreateSupported(variant,terrain.Query,terrain.Slab,new(-123456789));var snapshot=Observe(s);
            var expectedMass=longer?(partial?1004:2164):(partial?724:1304);
            Need(snapshot.ReferenceMass!.Value.Mass==expectedMass&&snapshot.Fuel.Save().SequenceEqual(variant.Fuel.Initial().Save())&&snapshot.Power.Save().SequenceEqual(variant.Power.Initial().Save()),"legal clocks, unequal fills and partial battery preserve exact authored start");
            Camera(s);Need(variant.Design.Save().SequenceEqual(before),"variant admission and camera preserve source");
        }
        Console.WriteLine($"MODULAR_GATE9 PASS checks={checks}");
    }
    private sealed class UntrustedQuery(IPhysicalSurfacePointQuery source):IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority=>source.Authority;
        public PhysicalSurfacePointResult Query(ulong bodyId,in Double3 direction)=>source.Query(bodyId,direction);
    }
    // Test-only revocable trusted source. Production datasets are immutable;
    // the witness protects the authority-change branch of the common owner.
    private sealed class MutableProof(IPhysicalSurfacePointQuery source):IPhysicalGradingProofSource
    {
        internal bool Changed;
        public PhysicalSurfaceAuthorityIdentity Authority=>Changed?source.Authority with {QueryPolicyVersion=source.Authority.QueryPolicyVersion+1}:source.Authority;
        public FacilitySupportRegion GradingRegion=>((IPhysicalGradingProofSource)source).GradingRegion;
        public bool IsNumericalFullWeight(in Double3 direction)=>((IPhysicalGradingProofSource)source).IsNumericalFullWeight(direction);
        public PhysicalSurfacePointResult Query(ulong bodyId,in Double3 direction)=>source.Query(bodyId,direction);
    }
    private static (Double3 Position,Double3 Velocity,Double3 Omega,Matrix3 BodyRotation) IndependentEarth(AssemblyFloridaSite site,SimulationInstant epoch)
    {
        static Matrix3 Z(double a)=>new(Math.Cos(a),-Math.Sin(a),0,Math.Sin(a),Math.Cos(a),0,0,0,1);
        static Matrix3 X(double a)=>new(1,0,0,0,Math.Cos(a),-Math.Sin(a),0,Math.Sin(a),Math.Cos(a));
        var t=epoch.Ticks/1e6;var rad=Math.PI/180;const double century=86400d*36525;
        var z=Z((90-.641*t/century)*rad);var x=X(.557*t/century*rad);var w=Z((190.147+360.9856235*t/86400)*rad);var h=X(Math.PI/2);
        var ja=new Matrix3(0,.641*rad/century,0,-.641*rad/century,0,0,0,0,0);var jb=new Matrix3(0,0,0,0,0,-.557*rad/century,0,.557*rad/century,0);var jc=new Matrix3(0,-360.9856235*rad/86400,0,360.9856235*rad/86400,0,0,0,0,0);
        var earth=z*x*w*h;var derivative=ja*earth+z*jb*x*w*h+z*x*jc*w*h;
        var skew=derivative*earth.Transpose();var region=FloridaFacilitySupport.Region;
        var basis=new Matrix3(region.East.X,region.Up.X,-region.North.X,region.East.Y,region.Up.Y,-region.North.Y,region.East.Z,region.Up.Z,-region.North.Z);
        return(earth.Apply(site.OriginBodyFixed),derivative.Apply(site.OriginBodyFixed),new(skew.H,skew.C,skew.D),earth*basis*new Matrix3(0,-1,0,1,0,0,0,0,1));
    }
    private static void Camera(ConstructionApplicationSession s)
    {
        Need(SolarSystemScene.TryCreateAt(new(1),s.Clock.CurrentTime,out var solar,out _),"canonical solar presentation");
        var camera=new CameraState(new(new(1),default),DoubleQuaternion.Identity,new(Math.PI/3,16d/9,.01,1e15),CameraMode.Free);
        var presentation=new FloridaContactPresentation(s.Binding.Physical!.Site,new(1),solar);presentation.PrepareCamera(camera);
        var state=Observe(s);var initial=presentation.CraftFocus(s.Binding,state,s.Engine.State.Revision.Value,SceneObjectFocusStatus.Prepared);
        Need(initial.CanonicalId==s.Binding.Spacecraft.Id.Value&&initial.Generation==(ulong)s.Control!.Identity.Generation&&initial.BoundingRadius>0,"camera uses canonical identity and compiled bounds");
        foreach(var radius in new[]{double.NaN,double.PositiveInfinity,-1d,double.MaxValue,1e20})
            Need(!solar!.BindActiveVessel(initial with {BoundingRadius=radius}),"malformed/out-of-domain camera extent refuses before binding");
        Need(solar!.BindActiveVessel(initial)&&solar.RefocusActiveVessel(camera),"cold active-vessel binding");
        var distance=solar.OrbitDistance;
        solar.ApplyPresentationInput(camera,new(){LookActive=1,MouseDeltaX=23,MouseDeltaY=-7,MouseWheelDetents=1},out _,out _,deferVesselPose:true);
        Need(solar.RefreshActiveVessel(camera,presentation.CraftFocus(s.Binding,state,0,SceneObjectFocusStatus.Prepared))&&solar.OrbitDistance<distance,"camera orbit and zoom");
        foreach(var target in new[]{NativePresentationFocus.Earth,NativePresentationFocus.Moon,NativePresentationFocus.Sun,NativePresentationFocus.Mars})
            Need(solar.Focus(camera,target)&&solar.RefocusActiveVessel(camera),"celestial focus and F return preserve vessel binding");
        Need(ReferenceEquals(Observe(s),state)&&s.Engine.ObserveAssemblyControl(s.Control!,out var control)==AssemblyControlStatus.Ready&&control.Requested==default,"camera is never physical or control authority");
        var slab=presentation.Slab();var top=slab.RootPosition.Value+slab.RootOrientation.Rotate(new Double3(0,slab.Scale.Y*.5,0));
        Need(Norm(top-presentation.Position(new(0,s.Binding.Physical.Contact.SupportPlane,0)).Value)<1e-4,"generic visible slab and physical support share one plane");
    }
}
