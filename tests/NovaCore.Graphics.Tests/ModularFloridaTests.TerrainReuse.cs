using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void TerrainReuse()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        foreach(bool longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            Need(s.Engine.AdmitConstructionHostTime(s.Authority,1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"reuse fixture credit");
            Need(s.Engine.ServiceConstructionDebt(s.Authority,out var count)==ConstructionServiceStatus.Published&&count==1,"reuse fixture native world");
            var world=s.Engine.ConstructionContactWorldForTest(s.Authority)!;
            var sim=Field<BepuPhysics.Simulation>(world,"simulation");var handle=Field<BepuPhysics.BodyHandle>(world,"body");
            var shape=Field<BepuPhysics.Collidables.TypedIndex>(world,"bodyShape");var config=Field<LocalContactConfiguration>(world,"configuration");
            var body=sim.Bodies[handle];var old=body.Pose;var saved=s.Save();var random=new Random(80341);
            var graded=CraftTerrainFootprint.Union(sim,handle,shape,s.Binding.Physical!.Site,config.OriginRoot,0);
            Need(graded.AboveGrade,"no-mesh graded slab uses cheap positive proof");
            body.Pose.Position=old.Position+new Vector3((float)(2*NovaCore.Core.Surface.FloridaFacilitySupport.Region.InnerEastMetres),0,0);
            Need(!CraftTerrainFootprint.Union(sim,handle,shape,s.Binding.Physical!.Site,config.OriginRoot,0).AboveGrade,"crossing grade edge invalidates graded proof");
            body.Pose=old;
            ref var compound=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Compound>(shape.Index);
            var child=compound.Children[0];
            var hits=0;
            for(int i=0;i<1536;i++)
            {
                var scale=i<1024?400:200000;
                body.Pose.Position=new((float)(random.NextDouble()*scale-scale/2),(float)(random.NextDouble()*40-20),(float)(random.NextDouble()*scale-scale/2));
                if(i%64==0)body.Pose.Position=new(i%128==0?float.Epsilon:MathF.BitDecrement(65536),0,MathF.BitIncrement(-65536));
                body.Pose.Orientation=Quaternion.CreateFromYawPitchRoll((float)(random.NextDouble()*6),(float)(random.NextDouble()*6),(float)(random.NextDouble()*6));
                // The query must follow current offsets, including COM recentering.
                compound.Children[0].LocalPosition=child.LocalPosition+new Vector3((float)(random.NextDouble()*.1),-.05f,.02f);
                var reach=i%3==0?0:random.NextDouble()*4;
                var full=CraftTerrainFootprint.Compute(sim,handle,shape,s.Binding.Physical!.Site,config.OriginRoot,reach);
                var union=CraftTerrainFootprint.Union(sim,handle,shape,s.Binding.Physical!.Site,config.OriginRoot,reach);
                Need(!union.AboveGrade||full.AboveGrade,"cheap graded-surface proof implies original proof");
                // Independent original per-child oracle, with no rounded tolerance.
                Need(union.MinX<=full.MinX&&union.MaxX>=full.MaxX&&union.MinY<=full.MinY&&union.MaxY>=full.MaxY,$"union encloses every original child interval i={i} long={longer} full={full} union={union}");
                int x0=(int)Math.Floor(union.MinX/4),x1=(int)Math.Floor(union.MaxX/4),y0=(int)Math.Floor(union.MinY/4),y1=(int)Math.Floor(union.MaxY/4);
                if(CraftTerrainColliders.Covered(union,x0,x1,y0,y1))hits++;
                Need(!CraftTerrainColliders.Covered(union,x0,x1,y0,y1)||CraftTerrainColliders.Covered(full,x0,x1,y0,y1),"positive reuse proves all original tile requests");
                Need(!CraftTerrainColliders.Covered(union,x0+1,x1,y0,y1),"crossing missing boundary invalidates reuse");
            }
            Need(hits>1000,"positive reuse coverage independently exercised");
            compound.Children[0]=child;body.Pose=old;
            Need(saved.SequenceEqual(s.Save()),"private query evaluation cannot mutate canonical source");
        }
        foreach(double edge in new[]{-8d,-4d,0d,4d,8d})
        {
            int high=(int)(edge/4)-1;
            Need(!CraftTerrainColliders.Covered(new(edge-1,0,edge,1,false),high,high,0,0),"closed high boundary needs successor tile");
            // At zero, division of a negative subnormal can round to -0. This
            // follows the existing inclusive floor contract and safely misses.
            if(edge!=0)Need(CraftTerrainColliders.Covered(new(edge-1,0,Math.BitDecrement(edge),1,false),high,high,0,0),"one ULP inside covered tile");
            Need(!CraftTerrainColliders.Covered(new(edge-1,0,Math.BitIncrement(edge),1,false),high,high,0,0),"one ULP outside invalidates reuse");
        }
        Need(!CraftTerrainColliders.Covered(new(double.NaN,0,1,1,false),0,0,0,0),"nonfinite union refuses");
        Need(!CraftTerrainColliders.Covered(new(0,0,1,1,false),1,0,0,0),"empty rectangle refuses");
        Need(!CraftTerrainColliders.Covered(new(-1e20,0,1e20,1,false),int.MinValue,int.MaxValue,0,0),"out of domain refuses");
        Console.WriteLine($"TERRAIN_REUSE_PASS checks={checks}");
    }
}

