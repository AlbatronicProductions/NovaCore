using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;

// Independent decimal reference arithmetic. Never issues a production certificate.
internal static class ContactKinematicsOracle
{
    internal const decimal Pi=3.1415926535897932384626433833m;
    internal static decimal D(double x)
    {
        if(x==0)return 0;
        var bits=BitConverter.DoubleToUInt64Bits(x);var e=(int)((bits>>52)&2047);
        decimal value=(bits&0xfffffffffffffUL)|(e==0?0:1UL<<52);
        var power=(e==0?1:e)-1023-52;
        while(power<0){value/=2;power++;}while(power>0){value*=2;power--;}
        return (bits>>63)==0?value:-value;
    }
    internal readonly record struct V(decimal X,decimal Y,decimal Z)
    {
        internal static V From(Double3 v)=>new(D(v.X),D(v.Y),D(v.Z));
        public static V operator +(V a,V b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static V operator -(V a,V b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static V operator *(V a,decimal b)=>new(a.X*b,a.Y*b,a.Z*b);
        public static V operator /(V a,decimal b)=>a*(1/b);
        internal decimal Norm=>Sqrt(Dot(this,this));
        internal static decimal Dot(V a,V b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        internal static V Cross(V a,V b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    }
    internal static decimal Sqrt(decimal x)
    {if(x==0)return 0;var y=(decimal)Math.Sqrt((double)x);for(var i=0;i<12;i++)y=(y+x/y)/2;return y;}
    private static (decimal S,decimal C) Trig(decimal x)
    {
        x-=decimal.Round(x/(2*Pi))*2*Pi;
        decimal s=x,c=1,st=x,ct=1;
        for(var k=1;k<=32;k++){st*=-x*x/(2*k*(2*k+1));ct*=-x*x/((2*k-1)*2*k);s+=st;c+=ct;}
        return(s,c);
    }
    private static V Z(V p,decimal a){var(s,c)=Trig(a);return new(c*p.X-s*p.Y,s*p.X+c*p.Y,p.Z);}
    private static V X(V p,decimal a){var(s,c)=Trig(a);return new(p.X,c*p.Y-s*p.Z,s*p.Y+c*p.Z);}
    internal static (V Normal,V Omega) Orientation(in EarthContactOrientation model,decimal t,Double3 up)
    {
        var rad=Pi/180;var day=D(model.SecondsPerDay);var century=day*D(model.DaysPerCentury);
        var a=(D(model.Ra0)+90+D(model.RaT)*t/century)*rad;
        var b=(90-D(model.Dec0)-D(model.DecT)*t/century)*rad;
        var c=(D(model.W0)+D(model.Wd)*t/day)*rad;
        var u=V.From(up);u/=u.Norm;
        var normal=Z(X(Z(new(u.X,-u.Z,u.Y),c),b),a);
        // Independent angular-velocity composition in root axes, not a finite stencil.
        var omega=new V(0,0,D(model.RaT)*rad/century)+Z(new(1,0,0),a)*(-D(model.DecT)*rad/century)+
            Z(X(new(0,0,1),b),a)*(D(model.Wd)*rad/day);
        return(normal,omega);
    }
    internal static V Lever(DoubleQuaternion attitude,Double3 offset)
    {
        // Independent quaternion rotation matrix divided by exact stored norm squared.
        var x=D(attitude.X);var y=D(attitude.Y);var z=D(attitude.Z);var w=D(attitude.W);var n=x*x+y*y+z*z+w*w;var r=V.From(offset);
        return new(((w*w+x*x-y*y-z*z)*r.X+2*(x*y-w*z)*r.Y+2*(x*z+w*y)*r.Z)/n,
            (2*(x*y+w*z)*r.X+(w*w-x*x+y*y-z*z)*r.Y+2*(y*z-w*x)*r.Z)/n,
            (2*(x*z-w*y)*r.X+2*(y*z+w*x)*r.Y+(w*w-x*x-y*y+z*z)*r.Z)/n);
    }
    internal static bool Contains(FloridaVector b,V v,decimal error=0)=>Contains(b.X,v.X,error)&&Contains(b.Y,v.Y,error)&&Contains(b.Z,v.Z,error);
    internal static bool Contains(FloridaBound b,decimal x,decimal error=0)=>D(b.Lower)<=x-error&&D(b.Upper)>=x+error;

    // Near-seed independent ODE derivatives and material velocity. No runtime Kepler evaluator used.
    // The production certificate remains the existence authority; this is an independent numerical witness.
    internal static (decimal Gap,decimal Speed,V Normal,V Lever) EarthPoint(in SpacecraftTranslationState linear,
        in SpacecraftRigidBodyRotationState angular,double mass,Double3 feature,decimal t)
    {
        if(t<0||t>1||linear.Epoch.Ticks!=0)throw new InvalidOperationException("oracle restricted to seed cell");
        var system=SolAnalyticalDefinition.Instance;system.TryGetNode(SolarSystemBodyIds.Earth,out var node);
        system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex,out var trajectory);system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun,out var sun);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        var p0=V.From(trajectory.StateAtEpoch.Position);var v0=V.From(trajectory.StateAtEpoch.Velocity);var radius=p0.Norm;var mu=D(sun.GravitationalParameter);
        var a=p0*(-mu/radius/radius/radius);
        var jerk=(v0-p0*(3*V.Dot(p0,v0)/radius/radius))*(-mu/radius/radius/radius);
        var force=V.From(linear.ConstantForceRoot)/D(mass);var lever=Lever(angular.OrientationLocalToParent,feature);
        var relative=V.From(linear.PositionRoot)-p0+lever+(V.From(linear.VelocityRoot)-v0)*t+(force-a)*(t*t/2)-jerk*(t*t*t/6);
        var velocity=V.From(linear.VelocityRoot)-v0+(force-a)*t-jerk*(t*t/2);
        var site=FloridaFacilitySupport.Region;var(n,omega)=Orientation(model,t,site.Up);var u=V.From(site.Up).Norm;
        return(u*V.Dot(n,relative)-(D(site.RadiusMetres)+D(site.PlaneAltitudeMetres)),
            V.Dot(n,velocity-V.Cross(omega,relative)),n,lever);
    }
}
