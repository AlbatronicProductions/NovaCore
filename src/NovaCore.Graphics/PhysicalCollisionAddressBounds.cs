using NovaCore.Core;

namespace NovaCore.Graphics;

internal static class PhysicalCollisionAddressBounds
{
    private const double DifferenceStep=1e-6;
    internal static bool TryRegional(CollisionVector ray,out CubeSphereFace face,out CollisionJet u,out CollisionJet v,out double discrepancy)
        =>TryRegional(ray,out face,out u,out v,out discrepancy,out _);
    internal static bool TryRegional(CollisionVector ray,out CubeSphereFace face,out CollisionJet u,out CollisionJet v,out double discrepancy,out string reason)
        =>TryRegional(ray,new CollisionFiniteVector(new(ray.X.V),new(ray.Y.V),new(ray.Z.V)).Normalized().Normalized(),out face,out u,out v,out discrepancy,out reason);
    internal static bool TryRegional(CollisionVector ray,CollisionFiniteVector finiteDirection,out CubeSphereFace face,out CollisionJet u,out CollisionJet v,out double discrepancy,out string reason,(CubeSphereFace Face,double Error)? proved=null)
    {
        face=default;u=v=default;discrepancy=double.PositiveInfinity;reason="";
        var xs=finiteDirection.X.I.Inflate(finiteDirection.X.E);var ys=finiteDirection.Y.I.Inflate(finiteDirection.Y.E);var zs=finiteDirection.Z.I.Inflate(finiteDirection.Z.E);
        static double MinimumAbs(CollisionRange a)=>a.Low>0?a.Low:a.High<0?-a.High:0;
        CollisionJet a,b,dominant;CollisionFinite fa,fb,fd;
        if(MinimumAbs(xs)>Math.Max(ys.Magnitude,zs.Magnitude)){face=xs.Low>0?CubeSphereFace.PositiveX:CubeSphereFace.NegativeX;dominant=xs.Low>0?ray.X:-ray.X;a=xs.Low>0?-ray.Z:ray.Z;b=ray.Y;fa=xs.Low>0?-finiteDirection.Z:finiteDirection.Z;fb=finiteDirection.Y;fd=xs.Low>0?finiteDirection.X:-finiteDirection.X;}
        else if(MinimumAbs(ys)>Math.Max(xs.Magnitude,zs.Magnitude)){face=ys.Low>0?CubeSphereFace.PositiveY:CubeSphereFace.NegativeY;dominant=ys.Low>0?ray.Y:-ray.Y;a=ray.X;b=ys.Low>0?-ray.Z:ray.Z;fa=finiteDirection.X;fb=ys.Low>0?-finiteDirection.Z:finiteDirection.Z;fd=ys.Low>0?finiteDirection.Y:-finiteDirection.Y;}
        else if(MinimumAbs(zs)>Math.Max(xs.Magnitude,ys.Magnitude)){face=zs.Low>0?CubeSphereFace.PositiveZ:CubeSphereFace.NegativeZ;dominant=zs.Low>0?ray.Z:-ray.Z;a=zs.Low>0?ray.X:-ray.X;b=ray.Y;fa=zs.Low>0?finiteDirection.X:-finiteDirection.X;fb=finiteDirection.Y;fd=zs.Low>0?finiteDirection.Z:-finiteDirection.Z;}
        else{reason="face";return false;}
        var targetA=a/dominant;var targetB=b/dominant;var length=ray.Length;
        var x=a/length;var y=b/length;
        static CollisionJet Inverse(CollisionJet x,CollisionJet y){var c=3+2*(x.Square()-y.Square());var sqrt12=new CollisionJet(((CollisionRange)12).Sqrt(),0,0,0,0,0);return sqrt12*x/(c+(c.Square()-24*x.Square()).Sqrt()).Sqrt();}
        var rootA=Inverse(x,y);var rootB=Inverse(y,x);
        // The caller may retain the already-proved finite Newton discrepancy
        // on a containing patch of the same immutable frame. Recompute ideal
        // value/derivative jets for this smaller domain, not the identical
        // eight-iteration numerical proof. No finite H sample is replaced.
        if(proved is {} prior&&prior.Face==face&&double.IsFinite(prior.Error)&&prior.Error>=0&&prior.Error<1e-8)
        {u=(rootA+1)*.5;v=(rootB+1)*.5;discrepancy=prior.Error;return true;}
        var targetFiniteA=fa/fd;var targetFiniteB=fb/fd;
        var initialA=targetA.V.Inflate(targetFiniteA.E);var initialB=targetB.V.Inflate(targetFiniteB.E);
        var da=initialA-rootA.V;var db=initialB-rootB.V;var error=CollisionRange.UpperNorm(da.Magnitude,db.Magnitude);
        for(var i=0;i<8;i++)
        {
            var boxA=i==0?initialA:rootA.V.Inflate(error);var boxB=i==0?initialB:rootB.V.Inflate(error);
            var hullA=boxA.Hull(rootA.V).Inflate(DifferenceStep);var hullB=boxB.Hull(rootB.V).Inflate(DifferenceStep);
            if(hullA.Low<=-1||hullA.High>=1||hullB.Low<=-1||hullB.High>=1){reason="iterate domain";return false;}
            var (hA,hB)=Forward(hullA,hullB);var lip=CollisionRange.UpperNorm(hA.HessianBound,hB.HessianBound);
            var (fA,fB)=Forward(boxA,boxB);var det=fA.X*fB.Y-fA.Y*fB.X;
            if(det.Low<=1e-14&&det.High>=-1e-14){reason="determinant";return false;}
            var ia=fB.Y/det;var ib=-fA.Y/det;var ic=-fB.X/det;var id=fA.X/det;
            var inverse=CollisionRange.UpperNorm(ia.Magnitude,ib.Magnitude,ic.Magnitude,id.Magnitude);
            var baseline=FiniteForward(boxA,boxB,0,0);var shiftedA=FiniteForward(boxA,boxB,DifferenceStep,0);var shiftedB=FiniteForward(boxA,boxB,0,DifferenceStep);
            double DerivativeError(double first,double second,double derivative,double hessian)
            {
                var difference=DifferenceStep*(CollisionRange)derivative;
                var subtraction=CollisionFinite.Round((difference+first+second).High);
                var division=CollisionFinite.Round(((difference+first+second+subtraction)/DifferenceStep).High);
                return (((CollisionRange)first+second+subtraction)/DifferenceStep+division+.5*(CollisionRange)DifferenceStep*hessian).High;
            }
            var e00=DerivativeError(baseline.A.E,shiftedA.A.E,hA.X.Magnitude,hA.HessianBound);
            var e10=DerivativeError(baseline.B.E,shiftedA.B.E,hB.X.Magnitude,hB.HessianBound);
            var e01=DerivativeError(baseline.A.E,shiftedB.A.E,hA.Y.Magnitude,hA.HessianBound);
            var e11=DerivativeError(baseline.B.E,shiftedB.B.E,hB.Y.Magnitude,hB.HessianBound);
            var jacobianError=CollisionRange.UpperNorm(e00,e01,e10,e11);
            CollisionFinite j00=new(fA.X.Inflate(e00)),j01=new(fA.Y.Inflate(e01)),j10=new(fB.X.Inflate(e10)),j11=new(fB.Y.Inflate(e11));
            var finiteDeterminant=j00*j11-j10*j01;var finiteDet=finiteDeterminant.I.Inflate(finiteDeterminant.E);
            if(finiteDet.Low<=1e-14&&finiteDet.High>=-1e-14){reason="finite-difference determinant";return false;}
            if((((CollisionRange)inverse)*jacobianError).High>=1){reason="Jacobian perturbation";return false;}
            double ResidualError(CollisionRange residual,double source,double target)=>((CollisionRange)source+target+CollisionFinite.Round(((CollisionRange)residual.Magnitude+source+target).High)).High;
            var residualA=fA.V-targetA.V;var residualB=fB.V-targetB.V;
            var erA=ResidualError(residualA,baseline.A.E,targetFiniteA.E);var erB=ResidualError(residualB,baseline.B.E,targetFiniteB.E);
            CollisionFinite ru=new(residualA.Inflate(erA)),rv=new(residualB.Inflate(erB));
            var updateA=new CollisionFinite(boxA)-(ru*j11-rv*j01)/finiteDeterminant;
            var updateB=new CollisionFinite(boxB)-(j00*rv-j10*ru)/finiteDeterminant;
            var updateError=CollisionRange.UpperNorm(updateA.E,updateB.E);
            var next=(((CollisionRange)inverse)/(1-((CollisionRange)inverse)*jacobianError)*(jacobianError*(CollisionRange)error+.5*lip*((CollisionRange)error).Square()+CollisionRange.UpperNorm(erA,erB))+updateError).High;
            // Each source iteration has its own proved successor enclosure.
            // At an exact root it can grow from zero to the finite roundoff
            // floor; strict contraction is neither necessary nor physically
            // meaningful there. Domain/determinant and final-error guards stay.
            if(!double.IsFinite(next)){reason=$"nonfinite iteration {i}";return false;}
            error=next;
        }
        u=(rootA+1)*.5;v=(rootB+1)*.5;discrepancy=Math.Max((new CollisionFinite(rootA.V,error)*.5+.5).E,(new CollisionFinite(rootB.V,error)*.5+.5).E);
        if(!double.IsFinite(discrepancy)||discrepancy>=1e-8){reason="terminal residual";return false;}return true;
    }
    private static (CollisionFinite A,CollisionFinite B) FiniteForward(CollisionRange a,CollisionRange b,double shiftA,double shiftB)
    {
        CollisionFinite aa=new(a),bb=new(b);
        if(shiftA!=0)aa+=shiftA;if(shiftB!=0)bb+=shiftB;
        var ca=(2*((aa+1)*.5)-1).Tighten(aa.I);var cb=(2*((bb+1)*.5)-1).Tighten(bb.I);
        var a2=ca.Square();var b2=cb.Square();CollisionFinite one=1;var third=((CollisionRange)1/3).Low;
        var x=ca*(1-.5*(b2+one)+b2*one/3).Tighten(new(third,.5)).Sqrt();
        var y=cb*(1-.5*(one+a2)+one*a2/3).Tighten(new(third,.5)).Sqrt();
        var z=(1-.5*(a2+b2)+a2*b2/3).Tighten(new(third,1)).Sqrt();
        // ProjectCube multiplies all components by radius=1, then normalizes.
        x*=1;y*=1;z*=1;
        var x2=x.Square();var y2=y.Square();var z2=z.Square();
        var lengthError=Math.Max((x2+y2+z2).E,Math.Max((x2+z2+y2).E,(y2+z2+x2).E));
        var length=new CollisionFinite(new(1,1),lengthError).Sqrt();
        x/=length;y/=length;z/=length;
        return(x/z,y/z);
    }
    private static (CollisionJet A,CollisionJet B) Forward(CollisionRange a,CollisionRange b)
    {
        var x=CollisionJet.Variable(a,true);var y=CollisionJet.Variable(b,false);
        var d=1-.5*(x.Square()+y.Square())+x.Square()*y.Square()/3;
        return(x*((.5-y.Square()/6)/d).Sqrt(),y*((.5-x.Square()/6)/d).Sqrt());
    }
}
