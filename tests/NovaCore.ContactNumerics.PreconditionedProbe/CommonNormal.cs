using NovaCore.Simulation.Spacecraft.Actuation;

// Numerical prerequisite only. No canonical state or retained BEPU authority is owned here.
internal readonly record struct D3(double X,double Y,double Z)
{
    public static D3 operator +(D3 a,D3 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static D3 operator -(D3 a,D3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    public static D3 operator *(double s,D3 v)=>new(s*v.X,s*v.Y,s*v.Z);
    internal static double Dot(D3 a,D3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    internal static D3 Cross(D3 a,D3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    internal bool Finite=>double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z);
    internal double Length=>Math.Sqrt(Dot(this,this));
}
internal readonly record struct InverseBody(double Mass,D3 Diagonal,D3 OffDiagonal)
{
    // OffDiagonal = XY, XZ, YZ. Full symmetric tensor, not a spherical-body assumption.
    internal D3 Apply(D3 v)=>new(Diagonal.X*v.X+OffDiagonal.X*v.Y+OffDiagonal.Y*v.Z,
        OffDiagonal.X*v.X+Diagonal.Y*v.Y+OffDiagonal.Z*v.Z,
        OffDiagonal.Y*v.X+OffDiagonal.Z*v.Y+Diagonal.Z*v.Z);
    internal bool Valid=>double.IsFinite(Mass)&&Mass>0&&Diagonal.Finite&&OffDiagonal.Finite&&
        Diagonal.X>0&&Diagonal.X*Diagonal.Y-OffDiagonal.X*OffDiagonal.X>0&&
        Diagonal.X*(Diagonal.Y*Diagonal.Z-OffDiagonal.Z*OffDiagonal.Z)-
        OffDiagonal.X*(OffDiagonal.X*Diagonal.Z-OffDiagonal.Y*OffDiagonal.Z)+
        OffDiagonal.Y*(OffDiagonal.X*OffDiagonal.Z-OffDiagonal.Y*Diagonal.Y)>0;
}
internal readonly record struct NormalRow(int Identity,D3 Normal,D3 Lever)
{
    internal D3 Moment=>D3.Cross(Lever,Normal);
}
internal readonly record struct Load(D3 Gravity,D3 Force,D3 Torque)
{
    internal bool Finite=>Gravity.Finite&&Force.Finite&&Torque.Finite;
}
internal readonly record struct Scaled(double Mantissa,int Exponent)
{
    internal static Scaled From(double x)
    {
        if(x==0)return default;
        var e=Math.ILogB(Math.Abs(x));return new(Math.ScaleB(x,-e),e);
    }
    internal static Scaled From(PoweredBinaryScale h)=>new(h.Significand,h.Exponent);
    internal Scaled Times(Scaled b)
    {
        if(Mantissa==0||b.Mantissa==0)return default;
        var m=From(Mantissa*b.Mantissa);return new(m.Mantissa,checked(Exponent+b.Exponent+m.Exponent));
    }
    internal Scaled Times(double b)=>Times(From(b));
    internal Scaled Divide(double d)
    {
        var b=From(d);var m=From(Mantissa/b.Mantissa);
        return Mantissa==0?default:new(m.Mantissa,checked(Exponent-b.Exponent+m.Exponent));
    }
    internal double Value=>Math.ScaleB(Mantissa,Exponent);
}
internal enum PredictionStatus { Ready,InvalidInput,UnsupportedDuration,DegenerateResponse,NegativeGuess,Overflow }
internal readonly record struct CommonPrediction(Scaled Shift,double SumResponse,double DiagonalResponse,double ProjectedAcceleration)
{
    // Input cache remains immutable; the shared offset is retained independently of its projection.
    internal double Materialize(double original)=>original+Shift.Value;
}
internal static class CommonNormal
{
    internal static PredictionStatus Prepare(ReadOnlySpan<NormalRow> rows,ReadOnlySpan<double> normalCache,
        in InverseBody body,in Load previous,in Load current,in PoweredBinaryScale duration,double omega,
        out CommonPrediction prediction)
    {
        prediction=default;
        if(rows.Length is <1 or >4||normalCache.Length!=rows.Length||!body.Valid||!previous.Finite||!current.Finite||
            !double.IsFinite(omega)||omega<=0)return PredictionStatus.InvalidInput;
        if(!double.IsFinite(duration.Significand)||duration.Significand<1||duration.Significand>=2||
            duration.Exponent is <-4096 or >0||duration.Value>16667d/1e6)return PredictionStatus.UnsupportedDuration;
        var linear=current.Gravity-previous.Gravity+body.Mass*(current.Force-previous.Force);
        var angular=body.Apply(current.Torque-previous.Torque);
        if(!linear.Finite||!angular.Finite)return PredictionStatus.Overflow;
        var n=default(D3);var moment=default(D3);var diagonal=0d;
        for(var i=0;i<rows.Length;i++)
        {
            var row=rows[i];var l2=D3.Dot(row.Normal,row.Normal);
            // FP32 narrow-phase unit normals have bounded rounding; do not renormalize them.
            if(!row.Normal.Finite||!row.Lever.Finite||Math.Abs(l2-1)>8*Math.ScaleB(1d,-23)||
                !double.IsFinite(normalCache[i])||normalCache[i]<0)return PredictionStatus.InvalidInput;
            for(var j=0;j<i;j++)if(row.Identity==rows[j].Identity)return PredictionStatus.InvalidInput;
            var r=row.Moment;n+=row.Normal;moment+=r;
            diagonal+=body.Mass*l2+D3.Dot(r,body.Apply(r));
        }
        var response=body.Mass*D3.Dot(n,n)+D3.Dot(moment,body.Apply(moment));
        var projected=D3.Dot(n,linear)+D3.Dot(moment,angular);
        if(!double.IsFinite(response)||!double.IsFinite(diagonal)||!double.IsFinite(projected))return PredictionStatus.Overflow;
        if(response<0||diagonal<=0)return PredictionStatus.DegenerateResponse;
        var h=Scaled.From(duration);var a=h.Times(omega);
        var k=a.Times(a.Value+2);
        var denominator=k.Value*response+diagonal;
        if(!double.IsFinite(denominator)||denominator<=0)return PredictionStatus.Overflow;
        // h*k*projected is kept exponent-scaled through the last quotient.
        var shift=h.Times(k).Times(-projected).Divide(denominator);
        if(!double.IsFinite(shift.Mantissa)||!double.IsFinite(shift.Value))return PredictionStatus.Overflow;
        for(var i=0;i<rows.Length;i++)
        {
            if(normalCache[i]==0&&shift.Mantissa<0)return PredictionStatus.NegativeGuess;
            var old=Scaled.From(normalCache[i]);
            if(shift.Mantissa<0&&(shift.Exponent>old.Exponent||
                (shift.Exponent==old.Exponent&&-shift.Mantissa>old.Mantissa)))return PredictionStatus.NegativeGuess;
            if(!double.IsFinite(normalCache[i]+shift.Value))return PredictionStatus.Overflow;
        }
        prediction=new(shift,response,diagonal,projected);return PredictionStatus.Ready;
    }
}
