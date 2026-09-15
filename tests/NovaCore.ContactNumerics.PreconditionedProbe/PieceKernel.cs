// Mechanically derived from unchanged KernelProbe: same equations/order, explicit piece h, typed diagnostic access.
using System.Numerics;
using System.Text.Json;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

// Diagnostic internal constrained kernel. Nothing here owns canonical/resource/actuator state.
// Seven unknowns: four normal impulses, two tangent components, one twist impulse.
internal static class PieceKernel
{
    internal const int Rows=7;


    internal readonly record struct V(double X,double Y,double Z)
    {
        public static V operator +(V a,V b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static V operator -(V a,V b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static V operator *(double a,V b)=>new(a*b.X,a*b.Y,a*b.Z);
        internal static V From(Vector3 v)=>new(v.X,v.Y,v.Z);
        internal static double Dot(V a,V b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        internal static V Cross(V a,V b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal double Length=>Math.Sqrt(Dot(this,this));
    }
    internal readonly record struct State(V Linear,V Angular);
    internal sealed class Patch
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
    internal readonly record struct SweepResult(double[] Impulses,State Velocity,int NormalClamps,int TangentClamps,int TwistClamps);
    internal static SweepResult Solve(Patch patch,State free,double[] initial,double omega,int sweeps,double h)
    {
        var lambda=(double[])initial.Clone();var a=omega*h;var k=a*(a+2);var d=1+k;var c=k/d;
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
    internal readonly record struct OracleResult(double[] Impulses,State Velocity,double Residual,bool Admissible);
    internal static OracleResult Reference(Patch patch,State free,double omega,double h)
    {
        // Independent coupled linear-system solution, rather than a longer call to candidate PGS.
        var matrix=(double[,])patch.K.Clone();var rhs=new double[Rows];
        var softnessRatio=1/(omega*omega*h*h+2*omega*h);
        for(var i=0;i<Rows;i++)
        {
            rhs[i]=-patch.J(i,free);
            if(i<4)
            {
                matrix[i,i]+=softnessRatio*patch.K[i,i];
                rhs[i]+=Math.Min(patch.Depth[i]/h,Math.Min(patch.Depth[i]/(h+2/omega),2));
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
    internal static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
}
