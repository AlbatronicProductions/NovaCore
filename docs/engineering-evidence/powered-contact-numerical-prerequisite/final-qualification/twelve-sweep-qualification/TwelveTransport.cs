internal static class GeneralizedTransport
{
    internal readonly record struct Witness(double LinearError,double AngularError,double Condition,double ErrorScreen);
    private static double Component(D3 v,int i)=>i==0?v.X:i==1?v.Y:v.Z;
    internal static bool Feasible(in ContactGeometry g,ReadOnlySpan<Scaled> x)
    {
        Scaled total=default,twist=default;
        for(int i=0;i<7;i++)if(!ScaleMath.Valid(x[i])||(i<4&&x[i].Mantissa<=0))return false;
        for(int i=0;i<4;i++){total=ScaleMath.Add(total,x[i].Times(.125));twist=ScaleMath.Add(twist,x[i].Times(.125*g.Radius(i)));}
        return ScaleMath.Compare(ScaleMath.Length(x[4],x[5]),total)<0&&ScaleMath.Compare(ScaleMath.Abs(x[6]),twist)<0;
    }
    private static double Det(ReadOnlySpan<double> a)=>a[0]*(a[4]*a[8]-a[5]*a[7])-a[1]*(a[3]*a[8]-a[5]*a[6])+a[2]*(a[3]*a[7]-a[4]*a[6]);
    internal static bool TryMove(in ContactGeometry old,in ContactGeometry next,ReadOnlySpan<Scaled> cache,
        Span<Scaled> result,out Witness witness)
    {
        witness=default;
        if(!old.Valid||!next.Valid||old.Features!=next.Features||D3.Dot(old.Normal,next.Normal)<=0||!FinalPolicy.HistoricalClosed(old,cache))return false;
        double length=0;for(int i=0;i<4;i++)length=Math.Max(length,Math.Max(old.Radius(i),next.Radius(i)));
        if(!double.IsFinite(length)||length<=0)return false;
        int exponent=int.MinValue;for(int i=0;i<7;i++)if(cache[i].Mantissa!=0)exponent=Math.Max(exponent,cache[i].Exponent);
        Span<double> x=stackalloc double[7],nullMatrix=stackalloc double[12],z=stackalloc double[4],minor=stackalloc double[9];
        D3 linear=default,angular=default;
        for(int i=0;i<7;i++){x[i]=Math.ScaleB(cache[i].Mantissa,cache[i].Exponent-exponent);linear+=x[i]*old.Linear(i);angular+=x[i]*old.Angular(i);}
        for(int i=0;i<4;i++)
        {nullMatrix[i]=1;nullMatrix[4+i]=D3.Dot(next.Angular(i),next.Tangent0)/length;nullMatrix[8+i]=D3.Dot(next.Angular(i),next.Tangent1)/length;}
        double max=0;
        for(int skip=0;skip<4;skip++)
        {
            for(int r=0;r<3;r++){int j=0;for(int c=0;c<4;c++)if(c!=skip)minor[3*r+j++]=nullMatrix[4*r+c];}
            z[skip]=(skip%2==0?1:-1)*Det(minor);max=Math.Max(max,Math.Abs(z[skip]));
        }
        if(max==0||!double.IsFinite(max))return false;
        for(int i=0;i<4;i++)z[i]/=max;
        // [A | I | b] permits the same infinity-condition screening as the accepted transport.
        Span<double> m=stackalloc double[105];m.Clear();
        for(int r=0;r<3;r++)
        {
            m[r*15+14]=Component(linear,r);m[(r+3)*15+14]=Component(angular,r)/length;
            for(int j=0;j<7;j++)
            {double scale=j==6?length:1;m[r*15+j]=scale*Component(next.Linear(j),r);m[(r+3)*15+j]=scale*Component(next.Angular(j),r)/length;}
        }
        for(int i=0;i<4;i++){m[90+i]=z[i];m[104]+=z[i]*x[i];}
        double norm=0;
        for(int i=0;i<7;i++){double sum=0;for(int j=0;j<7;j++)sum+=Math.Abs(m[i*15+j]);norm=Math.Max(norm,sum);m[i*15+7+i]=1;}
        for(int col=0;col<7;col++)
        {
            int pivot=col;for(int r=col+1;r<7;r++)if(Math.Abs(m[r*15+col])>Math.Abs(m[pivot*15+col]))pivot=r;
            if(m[pivot*15+col]==0||!double.IsFinite(m[pivot*15+col]))return false;
            if(pivot!=col)for(int j=0;j<15;j++)(m[col*15+j],m[pivot*15+j])=(m[pivot*15+j],m[col*15+j]);
            double divisor=m[col*15+col];for(int j=0;j<15;j++)m[col*15+j]/=divisor;
            for(int r=0;r<7;r++)if(r!=col){double f=m[r*15+col];for(int j=0;j<15;j++)m[r*15+j]-=f*m[col*15+j];}
        }
        double inv=0,largest=0;D3 afterLinear=default,afterAngular=default;
        for(int i=0;i<7;i++)
        {
            double sum=0;for(int j=0;j<7;j++)sum+=Math.Abs(m[i*15+7+j]);inv=Math.Max(inv,sum);
            largest=Math.Max(largest,Math.Abs(m[i*15+14]));x[i]=m[i*15+14]*(i==6?length:1);
            var s=Scaled.From(x[i]);result[i]=s.Mantissa==0?default:new(s.Mantissa,s.Exponent+exponent);
            afterLinear+=x[i]*next.Linear(i);afterAngular+=x[i]*next.Angular(i);
        }
        double condition=norm*inv,eps=Math.ScaleB(1d,-52),gamma=512*eps/(1-512*eps);
        double screen=gamma*condition*Math.Max(1,Math.ScaleB(largest,exponent))*Math.Max(1,length);
        double pe=Math.ScaleB((afterLinear-linear).Length,exponent),le=Math.ScaleB((afterAngular-angular).Length,exponent);
        witness=new(pe,le,condition,screen);
        return double.IsFinite(screen)&&screen<=1e-12&&pe<=1e-12&&le<=1e-12&&FinalPolicy.ArithmeticValid(result);
    }
}
