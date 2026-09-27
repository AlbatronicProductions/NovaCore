using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class ModularFloridaTests
{
    // Isolated native conformance witness. All three tessellations represent
    // the same inclined plane; this is not an alternative game terrain owner.
    internal static void SurfacePlane()
    {
        checks=0;var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,true).Data,Assets);var mass=craft.InitialMass;
        const float h=1f/8192,g=9.81f;
        var normal=Vector3.Normalize(new Vector3(.03661941f,.9993293f,.000114363924f));
        foreach(var tessellation in new[]{0,1,2})
        {
            var pool=new BufferPool();var metrics=new LocalContactMetrics();
            var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-g,0)),new SolveDescription(8,1));
            try
            {
                var triangles=new List<Triangle>();
                Vector3 P(float x,float z)=>new(x,-(normal.X*x+normal.Z*z)/normal.Y,z);
                void Add(Vector3 a,Vector3 b,Vector3 c)=>triangles.Add(new(a,b,c));
                for(var x=-40;x<40;x++)for(var z=-40;z<40;z++)
                {
                    var a=P(x*.25f,z*.25f);var b=P((x+1)*.25f,z*.25f);var c=P((x+1)*.25f,(z+1)*.25f);var d=P(x*.25f,(z+1)*.25f);
                    if(tessellation==0){Add(a,b,c);Add(a,c,d);}
                    else if(tessellation==1){Add(a,b,d);Add(b,c,d);}
                    else{var m=P((x+.5f)*.25f,(z+.5f)*.25f);Add(m,a,b);Add(m,b,c);Add(m,c,d);Add(m,d,a);}
                }
                // BEPU cross(C-A,B-A) points up for these X/Z windings.
                pool.Take<Triangle>(triangles.Count,out var buffer);for(var i=0;i<triangles.Count;i++)buffer[i]=triangles[i];
                var surface=sim.Shapes.Add(new Mesh(buffer,Vector3.One,pool));var ground=sim.Statics.Add(new StaticDescription(Vector3.Zero,surface));
                var shape=LocalContactWorld.CreateCraftShape(sim.Shapes,pool,new CompiledCraftContact(craft),mass,out _);
                var q=Quaternion.Normalize(new(.009084984f,-.008180488f,-.7415845f,.6707481f));
                var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(1.52342f,.8535537f,-.68927222f),q),new BodyVelocity(new(-.9071263f,-.020020222f,-.08612492f),new(-.12527353f,-.015057887f,.9970617f)),LocalContactWorld.CraftInertia(mass),new CollidableDescription(shape,.05f),new(-1)));
                metrics.Terrain=new(sim,body,craft.Collision.Length);metrics.Terrain.Register(ground,triangles.Count,Vector3.UnitY);
                double Energy()
                {
                    var b=sim.Bodies[body];var fq=b.Pose.Orientation;var materialQ=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W)*AssemblyContactProfile.Upright;
                    var w=materialQ.Conjugate().Rotate(new(b.Velocity.Angular.X,b.Velocity.Angular.Y,b.Velocity.Angular.Z));
                    return .5*mass.Mass*b.Velocity.Linear.LengthSquared()+.5*Double3.Dot(w,mass.Inertia.Apply(w))+mass.Mass*g*b.Pose.Position.Y;
                }
                var initial=Energy();var maximumGain=double.NegativeInfinity;var penetration=0d;var maxRaw=0d;var contacts=0;var impulse=Double3.Zero;
                for(var step=0;step<2048;step++)
                {
                    metrics.Terrain.Begin();sim.Timestep(h);metrics.Terrain.Observe();contacts+=metrics.Terrain.ContactCount;impulse+=metrics.Terrain.LinearImpulse;
                    maxRaw=Math.Max(maxRaw,metrics.Terrain.MaximumDepth);maximumGain=Math.Max(maximumGain,Energy()-initial);
                    Need(double.IsFinite(maximumGain)&&maximumGain<=mass.Mass*g*.004,"equivalent plane tessellation introduces no energy beyond existing spatial precision");
                    if(step%16==0)
                    {
                        var b=sim.Bodies[body];var fq=b.Pose.Orientation;var materialQ=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W)*AssemblyContactProfile.Upright;
                        foreach(var v in craft.Collision.SelectMany(c=>c.Vertices))
                        {
                            var p=new Double3(b.Pose.Position.X,b.Pose.Position.Y,b.Pose.Position.Z)+materialQ.Rotate(v-mass.Com);
                            penetration=Math.Max(penetration,-Double3.Dot(p,new(normal.X,normal.Y,normal.Z)));
                            Need(Math.Abs(p.X)<9&&Math.Abs(p.Z)<9,"whole hull remains inside identical finite plane coverage");
                        }
                    }
                }
                Need(contacts>0&&penetration<CraftSurfaceImpact.MinimumRecoverySaturationDepth,"exact planar hull penetration remains bounded across equivalent tessellations");
                Console.WriteLine($"SURFACE_PLANE tessellation={tessellation} contacts={contacts} rawDepth={maxRaw:R} planePenetration={penetration:R} maximumSampledGainJ={maximumGain:R} spatialAllowanceJ={mass.Mass*g*.004:R} totalImpulse={impulse}");
            }
            finally{sim.Dispose();pool.Clear();}
        }
        Console.WriteLine($"SURFACE_PLANE_PASS checks={checks}");
    }
}
