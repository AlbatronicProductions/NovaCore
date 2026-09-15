using System.Numerics;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

// Diagnostic private constrained kernel. Nothing here owns canonical/resource/actuator state.
// Seven unknowns: four normal impulses, two tangent components, one twist impulse.
internal static class KernelProbe
{
    private const int Rows=7;
    private const double H=1d/60;
    private const double ImpulseBar=1e-4, VelocityBar=1e-4;
    private readonly record struct V(double X,double Y,double Z)
    {
        public static V operator +(V a,V b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static V operator -(V a,V b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static V operator *(double a,V b)=>new(a*b.X,a*b.Y,a*b.Z);
        internal static V From(Vector3 v)=>new(v.X,v.Y,v.Z);
        internal static double Dot(V a,V b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        internal static V Cross(V a,V b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal double Length=>Math.Sqrt(Dot(this,this));
    }
    private readonly record struct State(V Linear,V Angular);
    private sealed class Patch
    {
        internal readonly V[] Linear=new V[Rows],Angular=new V[Rows],Offsets=new V[4];
        internal readonly double[] Depth=new double[4],Radii=new double[4];
        internal readonly double[,] K=new double[Rows,Rows];
        internal double InverseMass;
        internal Patch(Capture capture,double mass)
        {
            InverseMass=1/mass;
            var n=V.From(capture.Contacts[0].Normal);var center=default(V);var active=0;
            for(var i=0;i<4;i++)
            {
                Check(capture.Contacts[i].Normal==capture.Contacts[0].Normal,"convex shared normal");
                Offsets[i]=V.From(capture.Contacts[i].Offset);Depth[i]=capture.Contacts[i].Depth;
                Linear[i]=n;Angular[i]=V.Cross(Offsets[i],n);
                if(Depth[i]>=0){center+=Offsets[i];active++;}
            }
            if(active==0){for(var i=0;i<4;i++)center+=Offsets[i];active=4;}
            center=(1d/active)*center;
            Helpers.BuildOrthonormalBasis(capture.Contacts[0].Normal,out var t1,out var t2);
            Linear[4]=V.From(t1);Linear[5]=V.From(t2);
            Angular[4]=V.Cross(center,Linear[4]);Angular[5]=V.Cross(center,Linear[5]);Angular[6]=n;
            for(var i=0;i<4;i++)Radii[i]=(Offsets[i]-center).Length;
            for(var i=0;i<Rows;i++)for(var j=0;j<Rows;j++)
                K[i,j]=InverseMass*V.Dot(Linear[i],Linear[j])+.5*V.Dot(Angular[i],Angular[j]);
        }
        internal State Reconstruct(State free,ReadOnlySpan<double> impulses)
        {
            var v=free.Linear;var w=free.Angular;
            for(var i=0;i<Rows;i++){v+=(InverseMass*impulses[i])*Linear[i];w+=(.5*impulses[i])*Angular[i];}
            return new(v,w);
        }
        internal double J(int i,State state)=>V.Dot(Linear[i],state.Linear)+V.Dot(Angular[i],state.Angular);
    }
    private readonly record struct SweepResult(double[] Impulses,State Velocity,int NormalClamps,int TangentClamps,int TwistClamps);
    private static SweepResult Solve(Patch patch,State free,double[] initial,double omega,int sweeps)
    {
        var lambda=(double[])initial.Clone();var a=omega*H;var k=a*(a+2);var d=1+k;var c=k/d;
        var nc=0;var tc=0;var wc=0;
        for(var sweep=0;sweep<sweeps;sweep++)
        {
            for(var i=0;i<4;i++)
            {
                // Algebraically remove only the row's own warm kick; keep every other iterate.
                var others=patch.J(i,free);
                for(var j=0;j<Rows;j++)if(j!=i)others+=patch.K[i,j]*lambda[j];
                var depth=patch.Depth[i];
                var weightedBias=Math.Min(depth*omega*(a+2)/d,Math.Min(depth*omega*a/d,c*2));
                var next=(weightedBias-c*others)/patch.K[i,i];
                if(next<0)nc++;lambda[i]=Math.Max(0,next);
            }
            // Exact tangent block cancellation before solving; no nearly equal warm kick subtraction.
            var v0=patch.J(4,free);var v1=patch.J(5,free);
            for(var j=0;j<Rows;j++)if(j is not 4 and not 5)
            {v0+=patch.K[4,j]*lambda[j];v1+=patch.K[5,j]*lambda[j];}
            var determinant=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];
            var x=(-patch.K[5,5]*v0+patch.K[4,5]*v1)/determinant;
            var y=(patch.K[5,4]*v0-patch.K[4,4]*v1)/determinant;
            var cap=.125*(lambda[0]+lambda[1]+lambda[2]+lambda[3]);
            var length=Math.Sqrt(x*x+y*y);
            // Preserve the pinned physical-unit denominator guard as well as the Coulomb cap.
            var scale=Math.Min(1,cap/Math.Max((double)1e-16f,length));
            if(scale<1)tc++;lambda[4]=x*scale;lambda[5]=y*scale;
            var spin=patch.J(6,free);
            for(var j=0;j<6;j++)spin+=patch.K[6,j]*lambda[j];
            var twist=-spin/patch.K[6,6];var twistCap=0d;
            for(var j=0;j<4;j++)twistCap+=.125*lambda[j]*patch.Radii[j];
            if(Math.Abs(twist)>twistCap)wc++;lambda[6]=Math.Clamp(twist,-twistCap,twistCap);
        }
        return new(lambda,patch.Reconstruct(free,lambda),nc,tc,wc);
    }
    private readonly record struct OracleResult(double[] Impulses,State Velocity,double Residual,bool Admissible);
    private static double[] Eliminate(double[,] matrix,double[] rhs)
    {
        var a=new double[Rows,Rows+1];
        for(var i=0;i<Rows;i++){for(var j=0;j<Rows;j++)a[i,j]=matrix[i,j];a[i,Rows]=rhs[i];}
        for(var i=0;i<Rows;i++)
        {
            var p=i;for(var j=i+1;j<Rows;j++)if(Math.Abs(a[j,i])>Math.Abs(a[p,i]))p=j;
            Check(Math.Abs(a[p,i])>1e-15,"finite-reference factorization");
            if(p!=i)for(var j=i;j<=Rows;j++)(a[i,j],a[p,j])=(a[p,j],a[i,j]);
            for(var j=i+1;j<Rows;j++)
            {var factor=a[j,i]/a[i,i];for(var k=i;k<=Rows;k++)a[j,k]-=factor*a[i,k];}
        }
        var x=new double[Rows];
        for(var i=Rows-1;i>=0;i--){var r=a[i,Rows];for(var j=i+1;j<Rows;j++)r-=a[i,j]*x[j];x[i]=r/a[i,i];}
        return x;
    }
    private static object FiniteReference(Patch patch,State free,double omega,double[] initial,SweepResult first,SweepResult last)
    {
        var a=(double[,])patch.K.Clone();var rhs=new double[Rows];
        for(var i=0;i<Rows;i++)
        {
            rhs[i]=-patch.J(i,free);
            if(i<4){a[i,i]+=patch.K[i,i]/(omega*omega*H*H+2*omega*H);
                rhs[i]+=Math.Min(patch.Depth[i]/H,Math.Min(patch.Depth[i]/(H+2/omega),2));}
        }
        var lower=new double[Rows,Rows];
        for(var i=0;i<Rows;i++)for(var j=0;j<Rows;j++)if(j<=i||(i==4&&j==5))lower[i,j]=a[i,j];
        var affine=Eliminate(lower,rhs);var g=new double[Rows,Rows];
        for(var j=0;j<Rows;j++)
        {var column=new double[Rows];for(var i=0;i<Rows;i++)column[i]=lower[i,j]-a[i,j];
            var solution=Eliminate(lower,column);for(var i=0;i<Rows;i++)g[i,j]=solution[i];}
        var old=(double[])initial.Clone();var firstValues=new double[Rows];var valid=true;var norm=0d;
        for(var i=0;i<Rows;i++){var row=0d;for(var j=0;j<Rows;j++)row+=Math.Abs(g[i,j]);norm=Math.Max(norm,row);}
        for(var sweep=0;sweep<8;sweep++)
        {
            var next=(double[])affine.Clone();
            for(var i=0;i<Rows;i++)for(var j=0;j<Rows;j++)next[i]+=g[i,j]*old[j];
            var cap=0d;var twist=0d;
            for(var i=0;i<4;i++){valid&=next[i]>0;cap+=.125*next[i];twist+=.125*next[i]*patch.Radii[i];}
            valid&=Math.Sqrt(next[4]*next[4]+next[5]*next[5])<cap&&Math.Abs(next[6])<twist;
            if(sweep==0)next.CopyTo(firstValues,0);old=next;
        }
        var error=0d;for(var i=0;i<Rows;i++)error=Math.Max(error,Math.Max(Math.Abs(firstValues[i]-first.Impulses[i]),Math.Abs(old[i]-last.Impulses[i])));
        // Conservative bounded arithmetic crosscheck; physical acceptance remains separate.
        var arithmeticBar=2048*Math.ScaleB(1d,-52)*(1+initial.Sum(Math.Abs)+old.Sum(Math.Abs))*Math.Pow(1+norm,8);
        Check(valid&&error<=arithmeticBar&&first.NormalClamps+first.TangentClamps+first.TwistClamps+
            last.NormalClamps+last.TangentClamps+last.TwistClamps==0,"independent affine finite-iteration crosscheck/domain");
        return new {first=firstValues,eighth=old,error,arithmeticBar,transitionInfinityNorm=norm,valid};
    }
    private static OracleResult Reference(Patch patch,State free,double omega)
    {
        // Independent coupled linear-system solution, rather than a longer call to candidate PGS.
        var matrix=(double[,])patch.K.Clone();var rhs=new double[Rows];
        var softnessRatio=1/(omega*omega*H*H+2*omega*H);
        for(var i=0;i<Rows;i++)
        {
            rhs[i]=-patch.J(i,free);
            if(i<4)
            {
                matrix[i,i]+=softnessRatio*patch.K[i,i];
                rhs[i]+=Math.Min(patch.Depth[i]/H,Math.Min(patch.Depth[i]/(H+2/omega),2));
            }
        }
        var augmented=new double[Rows,Rows+1];
        for(var i=0;i<Rows;i++){for(var j=0;j<Rows;j++)augmented[i,j]=matrix[i,j];augmented[i,Rows]=rhs[i];}
        for(var col=0;col<Rows;col++)
        {
            var pivot=col;for(var row=col+1;row<Rows;row++)if(Math.Abs(augmented[row,col])>Math.Abs(augmented[pivot,col]))pivot=row;
            Check(Math.Abs(augmented[pivot,col])>1e-15,"independent oracle nonsingular");
            if(pivot!=col)for(var j=col;j<=Rows;j++)(augmented[col,j],augmented[pivot,j])=(augmented[pivot,j],augmented[col,j]);
            var divisor=augmented[col,col];for(var j=col;j<=Rows;j++)augmented[col,j]/=divisor;
            for(var row=0;row<Rows;row++)if(row!=col)
            {var factor=augmented[row,col];for(var j=col;j<=Rows;j++)augmented[row,j]-=factor*augmented[col,j];}
        }
        var lambda=new double[Rows];for(var i=0;i<Rows;i++)lambda[i]=augmented[i,Rows];
        var residual=0d;var valid=true;var cap=0d;var twistCap=0d;
        for(var i=0;i<Rows;i++)
        {var value=-rhs[i];for(var j=0;j<Rows;j++)value+=matrix[i,j]*lambda[j];residual=Math.Max(residual,Math.Abs(value));}
        for(var i=0;i<4;i++){valid&=lambda[i]>0;cap+=.125*lambda[i];twistCap+=.125*lambda[i]*patch.Radii[i];}
        valid&=Math.Sqrt(lambda[4]*lambda[4]+lambda[5]*lambda[5])<cap&&Math.Abs(lambda[6])<twistCap;
        return new(lambda,patch.Reconstruct(free,lambda),residual,valid);
    }
    private static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
    private static double SumNormals(double[] lambda)=>lambda[0]+lambda[1]+lambda[2]+lambda[3];
    internal static void Run(string? output)
    {
        const double sourceMass=8+1d/128;
        var pool=new BufferPool(16384);var capture=new Capture();
        using(var simulation=Simulation.Create(pool,new Callbacks(capture),new Integrator(),new SolveDescription(8,1)))
        {
            var shape=simulation.Shapes.Add(new Box(2,1,1));var slab=simulation.Shapes.Add(new Box(16,2,16));
            var inertia=new BodyInertia{InverseMass=(float)(1/sourceMass),InverseInertiaTensor=new Symmetric3x3{XX=.5f,YY=.5f,ZZ=.5f}};
            var body=simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,.5f,0)),default,inertia,
                new CollidableDescription(shape,.01f),new BodyActivityDescription(-1)));
            simulation.Statics.Add(new StaticDescription(new Vector3(0,-1,0),slab));
            for(var i=0;i<120;i++)simulation.Timestep(1f/60);
            // Refresh current geometry as the next real piece would. Prediction does not apply force twice.
            simulation.PredictBoundingBoxes(1f/60);simulation.CollisionDetection(1f/60);
            Check(capture.Count==4&&simulation.Bodies[body].Constraints.Count==1,"settled actual four-row patch");
            var handle=simulation.Bodies[body].Constraints[0].ConnectingConstraintHandle;
            var reader=new Extractor(capture,false);
            Check(simulation.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(handle,ref reader),"read genuine seven-slot warm cache");
            var initial=capture.Impulses.Select(v=>(double)v).ToArray();
            var velocity=simulation.Bodies[body].Velocity;
            var source=new State(V.From(velocity.Linear),V.From(velocity.Angular));
            var omega=(double)capture.Omega;
            Check(capture.Omega==new SpringSettings(30,1).AngularFrequency&&capture.TwiceDamping==2&&capture.Friction==.5f&&capture.Recovery==2,
                "actual retained material identity");
            var pose=simulation.Bodies[body].Pose;
            var deepest=double.NegativeInfinity;
            for(var x=-1;x<=1;x+=2)for(var y=-1;y<=1;y+=2)for(var z=-1;z<=1;z+=2)
                deepest=Math.Max(deepest,-(pose.Position+Vector3.Transform(new Vector3(x,.5f*y,.5f*z),pose.Orientation)).Y);
            Check(deepest<=.020&&source.Linear.Length<=.0005/(16667d/1e6),"cold reference is physically supported before local numerical gate");
            var results=new List<object>();var failed=(string?)null;
            foreach(var item in new[]{(Name:"unchanged_supported",End:sourceMass,Thrust:0d),
                (Name:"mass_only",End:8d,Thrust:0d),(Name:"mass_and_powered",End:8d,Thrust:32d)})
            {
                var mass=(sourceMass+item.End)/2;
                simulation.Bodies[body].LocalInertia.InverseMass=(float)(1/mass);
                var patch=new Patch(capture,mass);
                var free=new State(source.Linear+new V(0,(item.Thrust/mass-9.81)*H,0),source.Angular);
                var oracle=Reference(patch,free,omega);
                Check(oracle.Admissible&&oracle.Residual<=1e-12,"independent seven-row oracle applicability/residual");
                var first=Solve(patch,free,initial,omega,1);var last=Solve(patch,free,initial,omega,8);
                var finiteReference=FiniteReference(patch,free,omega,initial,first,last);
                var cold=Solve(patch,free,new double[Rows],omega,8);
                var impulseError=Math.Abs(SumNormals(last.Impulses)-SumNormals(oracle.Impulses));
                var linearError=(last.Velocity.Linear-oracle.Velocity.Linear).Length;
                var angularError=(last.Velocity.Angular-oracle.Velocity.Angular).Length;
                var pass=impulseError<=ImpulseBar&&linearError<=VelocityBar&&angularError<=VelocityBar;
                results.Add(new {item.Name,result=pass?"PASS":"FAIL",sourceMass,successorMass=item.End,stageMass=mass,
                    inverseMass=patch.InverseMass,transportInverseMass=simulation.Bodies[body].LocalInertia.InverseMass,item.Thrust,
                    source,free,initial,first,last,cold,oracle,finiteReference,impulseError,linearError,angularError,
                    firstImpulseError=Math.Abs(SumNormals(first.Impulses)-SumNormals(oracle.Impulses)),
                    coldImpulseError=Math.Abs(SumNormals(cold.Impulses)-SumNormals(oracle.Impulses))});
                if(!pass){failed=item.Name;break;}
            }
            var result=new {classification=failed is null?"RETAINED FOUR-CONTACT LOCAL KERNEL PASS; FULL OPERATOR UNQUALIFIED":"FINITE-ITERATION REFERENCE GATE FAILURE",
                firstFailure=failed,iterations=8,coldPreparationSteps=120,dt=H,omega,body=body.Value,constraint=handle.Value,
                features=capture.Features,contacts=capture.Contacts.Select(c=>new {offset=c.Offset.ToString(),normal=c.Normal.ToString(),c.Depth,c.FeatureId}),
                position=simulation.Bodies[body].Pose.Position.ToString(),deepest,sourceSpeed=source.Linear.Length,impulseBar=ImpulseBar,velocityBar=VelocityBar,
                results,poolBytes=pool.GetTotalAllocatedByteCount(),canonicalMutations=0};
            var json=JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true});
            if(output is not null)File.WriteAllText(output,json+Environment.NewLine);Console.WriteLine(json);
            if(failed is not null)Environment.ExitCode=1;
        }
        pool.Clear();
    }
}
